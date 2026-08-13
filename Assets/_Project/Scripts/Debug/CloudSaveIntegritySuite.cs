#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ChezArthur.Backend;
using ChezArthur.Core;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Suite d'intégrité MT4-G2-P1 : push / pull / conflit cloud (log [G2Suite]).
    /// Backup/restore save locale. Dialogue UI + offline réel = MANUAL.
    /// </summary>
    public static class CloudSaveIntegritySuite
    {
        private const string TAG = "[G2Suite]";
        private static readonly List<string> _lines = new List<string>(40);
        private static int _pass;
        private static int _fail;
        private static int _manual;
        private static string _backupPath;

        public static async void Run()
        {
            _lines.Clear();
            _pass = 0;
            _fail = 0;
            _manual = 0;
            _backupPath = null;

            Log("═══════════════════════════════════════════════");
            Log("MT4-G2-P1 — suite cloud save (push/pull/conflit)");
            Log("═══════════════════════════════════════════════");

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
            {
                Log("ABORT — PersistentManager absent (Hub Play Mode).");
                Flush();
                return;
            }

            try
            {
                BackupSave(pm);
                await EnsureSignedInAsync();

                if (!BackendService.IsSignedIn)
                {
                    Assert(1, false, "pas de session Auth — active Cloud Save + réseau");
                    return;
                }

                await Run01_ForceUploadAsync(pm);
                await Run02_FingerprintMatchAsync();
                await Run03_ApplyRestoresCloudAsync(pm);
                await Run04_VirginPullAsync(pm);
                await Run05_SimulateConflictAsync(pm);
                Run06_Manual();
            }
            catch (Exception e)
            {
                _fail++;
                Log("ABORT — " + e.GetType().Name + " : " + e.Message);
            }
            finally
            {
                CloudSaveSync.DebugClearConflictFlag();
                if (!string.IsNullOrEmpty(_backupPath) && File.Exists(_backupPath))
                    RestoreFromPath(pm, _backupPath);
                // Ré-aligne le cloud sur la vraie save restaurée
                if (BackendService.IsSignedIn)
                    await CloudSaveSync.UploadAsync(forcePlayerChoice: true);

                Log("───────────────────────────────────────────────");
                Log($"RÉSULTAT auto : {_pass} PASS · {_fail} FAIL · {_manual} MANUAL");
                Log("Colle tout le bloc [G2Suite] à Cursor.");
                Log("═══════════════════════════════════════════════");
                Flush();
            }
        }

        private static async Task EnsureSignedInAsync()
        {
            if (BackendService.IsSignedIn)
                return;
            BackendService.ForceReinitialize();
            await Task.Delay(3000);
        }

        private static async Task Run01_ForceUploadAsync(PersistentManager pm)
        {
            SaveSummary s = pm.GetSaveSummary();
            if (s.IsVirgin)
            {
                // Save de test minimale pour ne pas bloquer la suite
                pm.AddTals(10);
                pm.SaveGame();
            }

            await CloudSaveSync.WipeCloudAsync();
            await CloudSaveSync.UploadAsync(forcePlayerChoice: true);

            bool ok = CloudSaveSync.LastUploadBytes > 0
                && !string.IsNullOrEmpty(CloudSaveSync.LastLocalFingerprint)
                && CloudSaveSync.LastLocalFingerprint == CloudSaveSync.LastCloudFingerprint;

            Assert(
                1,
                ok,
                $"Force upload bytes={CloudSaveSync.LastUploadBytes} " +
                $"fp={Short(CloudSaveSync.LastLocalFingerprint)}");
        }

        private static async Task Run02_FingerprintMatchAsync()
        {
            string before = CloudSaveSync.LastCloudFingerprint;
            await CloudSaveSync.CompareAndResolveAwaitable();
            bool ok = !string.IsNullOrEmpty(before)
                && before == CloudSaveSync.LastCloudFingerprint
                && !CloudSaveSync.HasPendingConflict
                && CloudSaveSync.State != CloudSyncState.Error;

            Assert(2, ok, $"compare idle — fp inchangé={before == CloudSaveSync.LastCloudFingerprint} state={CloudSaveSync.State}");
        }

        private static async Task Run03_ApplyRestoresCloudAsync(PersistentManager pm)
        {
            await CloudSaveSync.UploadAsync(forcePlayerChoice: true);
            int talsBefore = pm.Tals;
            pm.AddTals(77);
            int talsDirty = pm.Tals;

            await CloudSaveSync.ApplyCloudAsync();
            int talsAfter = pm.Tals;

            bool ok = talsDirty == talsBefore + 77 && talsAfter == talsBefore;
            Assert(
                3,
                ok,
                $"ApplyCloud restaure — Tals {talsBefore}→dirty {talsDirty}→après {talsAfter}");
        }

        private static async Task Run04_VirginPullAsync(PersistentManager pm)
        {
            // Cloud = save courante riche
            await CloudSaveSync.UploadAsync(forcePlayerChoice: true);
            SaveSummary cloudSnap = CloudSaveSync.LastCloudSummary;

            // Local vierge
            var virgin = new ChezArthur.Core.SaveData
            {
                playerName = "G2SuiteVirgin",
                tals = 0,
                bestStage = 0
            };
            SaveSystem.Save(virgin);
            pm.LoadGame();

            if (!pm.GetSaveSummary().IsVirgin)
            {
                _manual++;
                Log("#04 MANUAL soft — local non vierge après wipe (StarterCharacters peuplent) — " +
                    "fais le pt 3 checklist à la main (DevMenu reset → pull auto).");
                // Restaure cloud riche sur cette install pour la suite
                await CloudSaveSync.UploadAsync(forcePlayerChoice: true);
                return;
            }

            await CloudSaveSync.CompareAndResolveAwaitable();
            await Task.Delay(500);

            SaveSummary after = pm.GetSaveSummary();
            bool ok = after.IsRich
                && after.tals == cloudSnap.tals
                && after.bestStage == cloudSnap.bestStage;

            Assert(
                4,
                ok,
                $"pull auto vierge→riche Tals={after.tals} ét.={after.bestStage} " +
                $"(cloud avait Tals={cloudSnap.tals})");
        }

        private static async Task Run05_SimulateConflictAsync(PersistentManager pm)
        {
            await CloudSaveSync.UploadAsync(forcePlayerChoice: true);
            pm.AddTals(13);
            pm.SaveGame();

            CloudSaveSync.DebugSimulateConflict();
            await Task.Delay(300);

            bool conflict = CloudSaveSync.HasPendingConflict
                || CloudSaveSync.State == CloudSyncState.Conflict;

            // Résolution auto « garder téléphone » sans UI
            await CloudSaveSync.UploadAsync(forcePlayerChoice: true);
            CloudSaveSync.DebugClearConflictFlag();

            Assert(
                5,
                conflict,
                $"Simuler conflit → pending/state Conflict={conflict} puis upload force (keep local)");
        }

        private static void Run06_Manual()
        {
            _manual++;
            Log("#06 MANUAL — hors auto");
            Log("  a) Débounce 30 s + flush pause (jouer 1 min, attendre, alt-tab).");
            Log("  b) Dialogue UI : Simuler conflit → 2 taps Garder / Récupérer (cartes chiffrées).");
            Log("  c) Offline total : zéro spam, jeu OK.");
            Log("  d) Smoke saisons / missions / gacha.");
            Log("  → Si #01–#05 PASS, plomberie P1 OK.");
        }

        private static void BackupSave(PersistentManager pm)
        {
            pm.SaveGame();
            string source = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(source))
                return;
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            _backupPath = Path.Combine(Application.persistentDataPath, "save_g2_backup_" + stamp + ".json");
            File.Copy(source, _backupPath, overwrite: true);
            Log("Backup OK → " + _backupPath);
        }

        private static void RestoreFromPath(PersistentManager pm, string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var parsed = JsonUtility.FromJson<ChezArthur.Core.SaveData>(json);
                if (parsed == null)
                    return;
                SaveMigrator.MigrateToCurrent(parsed);
                SaveSystem.Save(parsed);
                pm.LoadGame();
                Log("Restore OK → " + path);
            }
            catch (Exception e)
            {
                Log("Restore FAIL — " + e.Message);
            }
        }

        private static string Short(string fp)
        {
            if (string.IsNullOrEmpty(fp))
                return "—";
            return fp.Length > 8 ? fp.Substring(0, 8) : fp;
        }

        private static void Assert(int num, bool ok, string detail)
        {
            if (ok)
            {
                _pass++;
                Log($"#{num:00} PASS — {detail}");
            }
            else
            {
                _fail++;
                Log($"#{num:00} FAIL — {detail}");
            }
        }

        private static void Log(string msg) => _lines.Add(TAG + " " + msg);

        private static void Flush()
        {
            var sb = new StringBuilder(2048);
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_lines[i]);
            }
            Debug.Log(sb.ToString());
        }
    }
}
#endif
