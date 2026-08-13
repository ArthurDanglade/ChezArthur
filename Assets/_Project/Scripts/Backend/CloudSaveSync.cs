using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;
using ChezArthur.Core;
using ChezArthur.Meta;

namespace ChezArthur.Backend
{
    /// <summary>
    /// État de synchronisation cloud save (lecture debug / UI).
    /// </summary>
    public enum CloudSyncState
    {
        Idle = 0,
        Syncing = 1,
        Conflict = 2,
        Error = 3
    }

    /// <summary>
    /// Backup cloud UGS : meta + chunks gzip, comparaison fingerprint, politique MT4-D2.
    /// SaveSystem/SaveMigrator intouchés — apply passe par SaveSystem.Save.
    /// </summary>
    public static class CloudSaveSync
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string KEY_META = "save_meta";
        private const string KEY_CHUNK_PREFIX = "save_chunk_";
        private const int CHUNK_MAX_CHARS = 3000;
        private const float DEBOUNCE_SECONDS = 30f;
        private const int SCHEMA_VERSION = 1;

        // ═══════════════════════════════════════════
        // ÉTAT
        // ═══════════════════════════════════════════
        private static bool _dirty;
        private static bool _debounceRunning;
        private static bool _conflictPending;
        private static bool _uploading;
        private static CloudSyncState _state = CloudSyncState.Idle;
        private static DateTime _lastUploadUtc;
        private static int _lastUploadBytes;
        private static string _lastLocalFp = "";
        private static string _lastCloudFp = "";
        private static SaveSummary _lastCloudSummary;
        private static bool _warnedOnce;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS
        // ═══════════════════════════════════════════
        public static CloudSyncState State => _state;
        public static bool HasPendingConflict => _conflictPending;
        public static DateTime LastUploadUtc => _lastUploadUtc;
        public static int LastUploadBytes => _lastUploadBytes;
        public static string LastLocalFingerprint => _lastLocalFp;
        public static string LastCloudFingerprint => _lastCloudFp;
        public static SaveSummary LastCloudSummary => _lastCloudSummary;

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Marque la save sale + débounce 30 s → upload.
        /// </summary>
        public static void NotifyDirty()
        {
            _dirty = true;
            BackendService.EnsureHostPublic();
            if (_debounceRunning)
                return;
            MonoBehaviour host = BackendService.HostBehaviour;
            if (host == null)
                return;
            _debounceRunning = true;
            host.StartCoroutine(DebounceThenUpload());
        }

        /// <summary>
        /// Flush immédiat (pause app).
        /// </summary>
        public static void FlushIfDirty()
        {
            if (!_dirty && !_debounceRunning)
                return;
            _dirty = false;
            _ = UploadAsync(forcePlayerChoice: false);
        }

        /// <summary>
        /// Compare meta cloud vs local et résout (auto ou dialogue).
        /// </summary>
        public static async void CompareAndResolveAsync()
        {
            try
            {
                await CompareAndResolveInternalAsync();
            }
            catch (Exception e)
            {
                WarnOnce("Compare échouée : " + e.Message);
                _state = CloudSyncState.Error;
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary> Compare awaitable (suite G2). </summary>
        public static async Task CompareAndResolveAwaitable()
        {
            try
            {
                await CompareAndResolveInternalAsync();
            }
            catch (Exception e)
            {
                WarnOnce("Compare échouée : " + e.Message);
                _state = CloudSyncState.Error;
            }
        }

        /// <summary> Lève le flag conflit sans choix UI (après test simulate). </summary>
        public static void DebugClearConflictFlag()
        {
            _conflictPending = false;
            if (_state == CloudSyncState.Conflict)
                _state = CloudSyncState.Idle;
        }
#endif

        /// <summary>
        /// Upload local → cloud. forcePlayerChoice = choix explicite « Ce téléphone ».
        /// </summary>
        public static async Task UploadAsync(bool forcePlayerChoice)
        {
            if (_uploading)
                return;

            if (!BackendService.IsSignedIn)
                return;

            if (_conflictPending && !forcePlayerChoice)
            {
                Debug.Log("[Cloud] Upload bloqué — conflit non résolu.");
                return;
            }

            string jsonPath = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(jsonPath))
                return;

            string json;
            try
            {
                json = File.ReadAllText(jsonPath);
            }
            catch (Exception e)
            {
                WarnOnce("Lecture save.json échouée : " + e.Message);
                return;
            }

            if (string.IsNullOrEmpty(json))
                return;

            SaveSummary localSummary = PersistentManager.Instance != null
                ? PersistentManager.Instance.GetSaveSummary()
                : default;

            if (!forcePlayerChoice)
            {
                CloudSaveMetaDto cloudMeta = await TryLoadMetaAsync();
                if (cloudMeta != null
                    && cloudMeta.summary.IsRich
                    && localSummary.IsVirgin)
                {
                    Debug.Log("[Cloud] Upload refusé — local vierge n'écrase pas un cloud riche.");
                    return;
                }
            }

            _uploading = true;
            _state = CloudSyncState.Syncing;
            try
            {
                string fingerprint = Sha1Hex(json);
                string compressed = GzipBase64(json);
                List<string> chunks = SplitChunks(compressed, CHUNK_MAX_CHARS);

                var meta = new CloudSaveMetaDto
                {
                    schemaVersion = SCHEMA_VERSION,
                    fingerprint = fingerprint,
                    serverUploadUtcTicks = GameClock.UtcNowGuarded.Ticks,
                    chunkCount = chunks.Count,
                    summary = localSummary
                };

                var payload = new Dictionary<string, object>
                {
                    { KEY_META, JsonUtility.ToJson(meta) }
                };
                for (int i = 0; i < chunks.Count; i++)
                    payload[KEY_CHUNK_PREFIX + i] = chunks[i];

                await CloudSaveService.Instance.Data.Player.SaveAsync(payload);

                _lastUploadUtc = GameClock.UtcNowGuarded;
                _lastUploadBytes = Encoding.UTF8.GetByteCount(compressed);
                _lastLocalFp = fingerprint;
                _lastCloudFp = fingerprint;
                _lastCloudSummary = localSummary;
                _dirty = false;
                _conflictPending = false;
                _state = CloudSyncState.Idle;
                Debug.Log(
                    "[Cloud] Upload OK — " + _lastUploadBytes + " o compressés, " +
                    chunks.Count + " chunk(s), fp=" + ShortFp(fingerprint));
            }
            catch (Exception e)
            {
                WarnOnce("Upload échoué : " + e.Message);
                _state = CloudSyncState.Error;
            }
            finally
            {
                _uploading = false;
            }
        }

        /// <summary>
        /// Télécharge chunks → SaveSystem.Save → LoadGame.
        /// </summary>
        public static async Task ApplyCloudAsync()
        {
            _state = CloudSyncState.Syncing;
            try
            {
                CloudSaveMetaDto meta = await TryLoadMetaAsync();
                if (meta == null || meta.chunkCount <= 0)
                {
                    Debug.LogError("[Cloud] Apply impossible — meta absente ou chunkCount=0.");
                    _state = CloudSyncState.Error;
                    return;
                }

                var keys = new HashSet<string>();
                for (int i = 0; i < meta.chunkCount; i++)
                    keys.Add(KEY_CHUNK_PREFIX + i);

                Dictionary<string, Unity.Services.CloudSave.Models.Item> loaded =
                    await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                var sb = new StringBuilder(meta.chunkCount * CHUNK_MAX_CHARS);
                for (int i = 0; i < meta.chunkCount; i++)
                {
                    string key = KEY_CHUNK_PREFIX + i;
                    if (!loaded.TryGetValue(key, out Unity.Services.CloudSave.Models.Item item)
                        || item?.Value == null)
                    {
                        Debug.LogError("[Cloud] Chunk manquant : " + key + " — local conservé.");
                        _state = CloudSyncState.Error;
                        return;
                    }

                    sb.Append(item.Value.GetAsString());
                }

                string json;
                try
                {
                    json = GunzipBase64(sb.ToString());
                }
                catch (Exception e)
                {
                    Debug.LogError("[Cloud] Décompression échouée — local conservé : " + e.Message);
                    _state = CloudSyncState.Error;
                    return;
                }

                ChezArthur.Core.SaveData parsed;
                try
                {
                    parsed = JsonUtility.FromJson<ChezArthur.Core.SaveData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError("[Cloud] Parse JSON échoué — local conservé : " + e.Message);
                    _state = CloudSyncState.Error;
                    return;
                }

                if (parsed == null)
                {
                    Debug.LogError("[Cloud] SaveData null — local conservé.");
                    _state = CloudSyncState.Error;
                    return;
                }

                SaveMigrator.MigrateToCurrent(parsed);
                SaveSystem.Save(parsed);

                if (PersistentManager.Instance != null)
                {
                    PersistentManager.Instance.LoadGame();
                    PersistentManager.Instance.GetType(); // keep analyzer calm
                }

                _conflictPending = false;
                _lastCloudFp = meta.fingerprint ?? "";
                _lastLocalFp = meta.fingerprint ?? "";
                _lastCloudSummary = meta.summary;
                _state = CloudSyncState.Idle;
                Debug.Log("[Cloud] Apply OK — save locale remplacée (atomic + .bak).");
            }
            catch (Exception e)
            {
                Debug.LogError("[Cloud] Apply échoué — local conservé : " + e.Message);
                _state = CloudSyncState.Error;
            }
        }

        /// <summary> Debug : wipe toutes les clés save_*. </summary>
        public static async Task WipeCloudAsync()
        {
            try
            {
                if (!BackendService.IsSignedIn)
                    return;

                var keys = await CloudSaveService.Instance.Data.Player.ListAllKeysAsync();
                for (int i = 0; i < keys.Count; i++)
                {
                    string k = keys[i].Key;
                    if (k == KEY_META || (k != null && k.StartsWith(KEY_CHUNK_PREFIX, StringComparison.Ordinal)))
                    {
                        await CloudSaveService.Instance.Data.Player.DeleteAsync(
                            k,
                            new Unity.Services.CloudSave.Models.Data.Player.DeleteOptions());
                    }
                }

                _lastCloudFp = "";
                _lastCloudSummary = default;
                _conflictPending = false;
                _state = CloudSyncState.Idle;
                Debug.Log("[Cloud] Wipe cloud OK.");
            }
            catch (Exception e)
            {
                WarnOnce("Wipe cloud échoué : " + e.Message);
            }
        }

        /// <summary> Debug : force le dialogue conflit avec résumés réels. </summary>
        public static async void DebugSimulateConflict()
        {
            try
            {
                SaveSummary local = PersistentManager.Instance != null
                    ? PersistentManager.Instance.GetSaveSummary()
                    : default;
                CloudSaveMetaDto meta = await TryLoadMetaAsync();
                SaveSummary cloud = meta != null ? meta.summary : default;
                if (meta != null)
                    _lastCloudFp = meta.fingerprint ?? "";
                _lastLocalFp = ComputeLocalFingerprint();
                OpenConflict(local, cloud);
            }
            catch (Exception e)
            {
                WarnOnce("Simuler conflit échoué : " + e.Message);
            }
        }

        // ═══════════════════════════════════════════
        // INTERNE
        // ═══════════════════════════════════════════

        private static IEnumerator DebounceThenUpload()
        {
            yield return new WaitForSecondsRealtime(DEBOUNCE_SECONDS);
            _debounceRunning = false;
            if (_dirty)
                _ = UploadAsync(forcePlayerChoice: false);
        }

        private static async Task CompareAndResolveInternalAsync()
        {
            if (!BackendService.IsSignedIn)
                return;

            _state = CloudSyncState.Syncing;
            CloudSaveMetaDto cloudMeta = await TryLoadMetaAsync();
            SaveSummary local = PersistentManager.Instance != null
                ? PersistentManager.Instance.GetSaveSummary()
                : default;
            string localFp = ComputeLocalFingerprint();
            _lastLocalFp = localFp;

            if (cloudMeta == null)
            {
                if (local.IsRich)
                    await UploadAsync(forcePlayerChoice: false);
                _state = CloudSyncState.Idle;
                return;
            }

            _lastCloudFp = cloudMeta.fingerprint ?? "";
            _lastCloudSummary = cloudMeta.summary;

            if (!string.IsNullOrEmpty(localFp)
                && string.Equals(localFp, cloudMeta.fingerprint, StringComparison.Ordinal))
            {
                _state = CloudSyncState.Idle;
                Debug.Log("[Cloud] Fingerprint identique — rien à faire.");
                return;
            }

            // Fingerprints divergents mais mêmes chiffres « joueur » → dernier écrivain (pas de dialogue).
            if (SummariesEquivalent(local, cloudMeta.summary))
            {
                long localTicks = local.lastPlayedUtcTicks;
                long cloudTicks = cloudMeta.serverUploadUtcTicks;
                Debug.Log("[Cloud] Divergence mineure — dernier écrivain retenu.");
                if (localTicks >= cloudTicks)
                    await UploadAsync(forcePlayerChoice: false);
                else
                    await ApplyCloudAsync();
                return;
            }

            bool cloudRich = cloudMeta.summary.IsRich;
            bool localVirgin = local.IsVirgin;
            bool cloudVirgin = cloudMeta.summary.IsVirgin;
            bool localRich = local.IsRich;

            if (localVirgin && cloudRich)
            {
                Debug.Log("[Cloud] Pull auto — local vierge, cloud riche.");
                await ApplyCloudAsync();
                return;
            }

            if (cloudVirgin && localRich)
            {
                Debug.Log("[Cloud] Push auto — cloud vierge, local riche.");
                await UploadAsync(forcePlayerChoice: false);
                return;
            }

            if (localRich && cloudRich)
            {
                OpenConflict(local, cloudMeta.summary);
                return;
            }

            _state = CloudSyncState.Idle;
        }

        /// <summary>
        /// Résumés « équivalents » pour le joueur (ignore lastPlayed / fingerprint).
        /// </summary>
        private static bool SummariesEquivalent(SaveSummary a, SaveSummary b)
        {
            return a.ownedCount == b.ownedCount
                && a.tals == b.tals
                && a.bestStage == b.bestStage
                && a.bestScoreThisSeason == b.bestScoreThisSeason;
        }

        private static void OpenConflict(SaveSummary local, SaveSummary cloud)
        {
            _conflictPending = true;
            _state = CloudSyncState.Conflict;
            SaveConflictDialog.Show(
                local,
                cloud,
                chooseLocal: () =>
                {
                    _ = UploadAsync(forcePlayerChoice: true);
                },
                chooseCloud: () =>
                {
                    _ = ApplyCloudAsync();
                });
        }

        private static async Task<CloudSaveMetaDto> TryLoadMetaAsync()
        {
            try
            {
                var keys = new HashSet<string> { KEY_META };
                Dictionary<string, Unity.Services.CloudSave.Models.Item> loaded =
                    await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
                if (!loaded.TryGetValue(KEY_META, out Unity.Services.CloudSave.Models.Item item)
                    || item?.Value == null)
                    return null;

                string json = item.Value.GetAsString();
                if (string.IsNullOrEmpty(json))
                    return null;
                return JsonUtility.FromJson<CloudSaveMetaDto>(json);
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeLocalFingerprint()
        {
            string path = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(path))
                return "";
            try
            {
                return Sha1Hex(File.ReadAllText(path));
            }
            catch
            {
                return "";
            }
        }

        private static List<string> SplitChunks(string data, int maxChars)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(data))
            {
                list.Add("");
                return list;
            }

            for (int i = 0; i < data.Length; i += maxChars)
            {
                int len = Math.Min(maxChars, data.Length - i);
                list.Add(data.Substring(i, len));
            }

            return list;
        }

        private static string GzipBase64(string text)
        {
            byte[] raw = Encoding.UTF8.GetBytes(text);
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress, true))
                    gz.Write(raw, 0, raw.Length);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static string GunzipBase64(string b64)
        {
            byte[] compressed = Convert.FromBase64String(b64);
            using (var input = new MemoryStream(compressed))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gz.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }

        private static string Sha1Hex(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
            using (SHA1 sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static string ShortFp(string fp)
        {
            if (string.IsNullOrEmpty(fp))
                return "—";
            return fp.Length > 8 ? fp.Substring(0, 8) : fp;
        }

        private static void WarnOnce(string message)
        {
            if (_warnedOnce)
                return;
            _warnedOnce = true;
            Debug.LogWarning("[Cloud] " + message);
        }

        [Serializable]
        private class CloudSaveMetaDto
        {
            public int schemaVersion;
            public string fingerprint;
            public long serverUploadUtcTicks;
            public int chunkCount;
            public SaveSummary summary;
        }
    }
}
