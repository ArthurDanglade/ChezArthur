using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using ChezArthur.Meta;

namespace ChezArthur.Backend
{
    /// <summary>
    /// Plomberie UGS (Auth anonyme + sync temps serveur). Offline-first :
    /// jamais bloquant, jamais d'exception propagée. Aucun accès save/saison.
    /// </summary>
    public static class BackendService
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string CLOUD_CODE_GET_SERVER_TIME = "GetServerTimeUtc";
        private const float SYNC_TIMEOUT_SECONDS = 5f;
        private const float RESYNC_FOCUS_MINUTES = 5f;

        // ═══════════════════════════════════════════
        // ÉTAT
        // ═══════════════════════════════════════════
        private static bool _initStarted;
        private static bool _isInitialized;
        private static bool _offlineWarnedThisSession;
        private static DateTime _lastSyncUtc;
        private static float _lastSyncRealtime;
        private static bool _hasServerTime;
        private static BackendServiceHost _host;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS / EVENTS
        // ═══════════════════════════════════════════
        public static bool IsInitialized => _isInitialized;

        public static bool IsSignedIn
        {
            get
            {
                try
                {
                    return AuthenticationService.Instance != null
                        && AuthenticationService.Instance.IsSignedIn;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static string PlayerId
        {
            get
            {
                try
                {
                    if (!IsSignedIn)
                        return "";
                    return AuthenticationService.Instance.PlayerId ?? "";
                }
                catch
                {
                    return "";
                }
            }
        }

        public static bool HasServerTime => _hasServerTime;
        public static DateTime LastSyncUtc => _lastSyncUtc;

        public static event Action OnServerTimeSynced;

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Démarre l'init UGS en fire-and-forget. Idempotent. Jamais bloquant.
        /// </summary>
        public static void Initialize()
        {
            if (_initStarted)
                return;

            _initStarted = true;
            EnsureHost();
            _ = InitializeAsync();
        }

        /// <summary>
        /// Debug : relance l'init (réinitialise les gardes de session).
        /// </summary>
        public static void ForceReinitialize()
        {
            _initStarted = false;
            _isInitialized = false;
            _offlineWarnedThisSession = false;
            Initialize();
        }

        /// <summary>
        /// Appelle Cloud Code GetServerTimeUtc (timeout 5 s) et pose l'ancre GameClock.
        /// </summary>
        public static async void SyncServerTime()
        {
            try
            {
                await SyncServerTimeAsync();
            }
            catch (Exception e)
            {
                WarnOnce("Sync temps serveur échouée : " + e.Message);
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Sync awaitable pour la suite d'intégrité G1. Retourne true si ancre posée.
        /// </summary>
        public static async Task<bool> TrySyncServerTimeAsync()
        {
            try
            {
                await SyncServerTimeAsync();
                return _hasServerTime && GameClock.HasServerTime;
            }
            catch (Exception e)
            {
                WarnOnce("Sync temps serveur échouée : " + e.Message);
                return false;
            }
        }
#endif

        // ═══════════════════════════════════════════
        // INTERNE
        // ═══════════════════════════════════════════

        private static async Task InitializeAsync()
        {
            try
            {
                await UnityServices.InitializeAsync();
                _isInitialized = true;
                Debug.Log("[Backend] Unity Services initialisé.");

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                string id = AuthenticationService.Instance.PlayerId ?? "";
                string shortId = id.Length > 8 ? id.Substring(0, 8) : id;
                Debug.Log("[Backend] Sign-in anonyme OK — PlayerId=" + shortId + "…");

                SyncServerTime();
            }
            catch (Exception e)
            {
                WarnOnce("Init échouée (offline OK) : " + e.Message);
            }
        }

        private static async Task SyncServerTimeAsync()
        {
            if (!_isInitialized && UnityServices.State != ServicesInitializationState.Initialized)
            {
                WarnOnce("Sync ignorée — Services non initialisés.");
                return;
            }

            if (!IsSignedIn)
            {
                WarnOnce("Sync ignorée — pas de session Auth.");
                return;
            }

            Task<ServerTimeResponse> callTask = CloudCodeService.Instance.CallEndpointAsync<ServerTimeResponse>(
                CLOUD_CODE_GET_SERVER_TIME,
                new Dictionary<string, object>());

            Task winner = await Task.WhenAny(callTask, Task.Delay(TimeSpan.FromSeconds(SYNC_TIMEOUT_SECONDS)));
            if (winner != callTask)
            {
                WarnOnce("Sync temps serveur — timeout " + SYNC_TIMEOUT_SECONDS + "s.");
                return;
            }

            ServerTimeResponse response = await callTask;
            if (response == null || response.utcNowMs <= 0)
            {
                WarnOnce("Sync temps serveur — réponse invalide.");
                return;
            }

            DateTime serverUtc = DateTimeOffset.FromUnixTimeMilliseconds(response.utcNowMs).UtcDateTime;
            double realtime = Time.realtimeSinceStartupAsDouble;
            GameClock.SetServerAnchor(serverUtc, realtime);

            _hasServerTime = true;
            _lastSyncUtc = serverUtc;
            _lastSyncRealtime = Time.realtimeSinceStartup;
            Debug.Log("[Backend] Temps serveur synchronisé — " + serverUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
            OnServerTimeSynced?.Invoke();
        }

        private static void EnsureHost()
        {
            if (_host != null)
                return;

            GameObject go = new GameObject("[BackendServiceHost]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _host = go.AddComponent<BackendServiceHost>();
        }

        internal static void OnHostApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !_isInitialized)
                return;

            if (_hasServerTime
                && Time.realtimeSinceStartup - _lastSyncRealtime < RESYNC_FOCUS_MINUTES * 60f)
                return;

            SyncServerTime();
        }

        private static void WarnOnce(string message)
        {
            if (_offlineWarnedThisSession)
                return;

            _offlineWarnedThisSession = true;
            Debug.LogWarning("[Backend] " + message);
        }

        /// <summary> DTO Cloud Code GetServerTimeUtc. </summary>
        [Serializable]
        private class ServerTimeResponse
        {
            public long utcNowMs;
        }

        /// <summary> Hôte DontDestroyOnLoad — refresh au focus. </summary>
        private sealed class BackendServiceHost : MonoBehaviour
        {
            private void OnApplicationFocus(bool hasFocus)
            {
                BackendService.OnHostApplicationFocus(hasFocus);
            }
        }
    }
}
