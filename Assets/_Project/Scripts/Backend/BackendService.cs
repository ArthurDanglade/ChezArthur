using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.Core;
using ChezArthur.Meta;

#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace ChezArthur.Backend
{
    /// <summary> Résultat d'une tentative de liaison Google Play Games. </summary>
    public enum GoogleLinkResult
    {
        Success = 0,
        AlreadyLinkedNeedsConfirm = 1,
        Error = 2,
        NotAvailable = 3
    }

    /// <summary>
    /// Plomberie UGS (Auth anonyme + sync temps serveur + liaison Google Play Games).
    /// Offline-first : jamais bloquant, jamais d'exception propagée.
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
        private static bool _isGoogleLinked;
        private static string _lastLinkError = "";
        private static bool _pendingSwitchConfirm;
        private static string _googleDisplayName = "";

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

        /// <summary> Hôte DontDestroyOnLoad (coroutines Cloud Save). </summary>
        public static MonoBehaviour HostBehaviour => _host;

        /// <summary> Identité Google Play Games liée (rafraîchi après sign-in / link). </summary>
        public static bool IsGoogleLinked => _isGoogleLinked;

        /// <summary> Dernier message d'erreur de liaison (lisible UI). </summary>
        public static string LastLinkError => _lastLinkError ?? "";

        /// <summary> En attente de confirmation joueur pour bascule AccountAlreadyLinked. </summary>
        public static bool PendingSwitchConfirm => _pendingSwitchConfirm;

        /// <summary> Nom PGS si dispo (sinon vide). </summary>
        public static string GoogleDisplayName => _googleDisplayName ?? "";

        public static event Action OnServerTimeSynced;

        /// <summary> Émis après changement d'état compte (lien / bascule / unlink). </summary>
        public static event Action OnAccountStateChanged;

        /// <summary> Garantit l'hôte (appelé par CloudSaveSync). </summary>
        public static void EnsureHostPublic() => EnsureHost();

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

        /// <summary>
        /// Lie le joueur courant à Google Play Games (code one-shot, jamais loggé).
        /// AccountAlreadyLinked → PendingSwitchConfirm sans sign-out (UI confirme d'abord).
        /// </summary>
        public static async Task<GoogleLinkResult> LinkWithGoogleAsync()
        {
            _lastLinkError = "";

#if !(UNITY_ANDROID && !UNITY_EDITOR)
            _lastLinkError = "appareil uniquement";
            NotifyAccountState();
            return GoogleLinkResult.NotAvailable;
#else
            if (!IsSignedIn)
            {
                _lastLinkError = "hors ligne / non connecté";
                NotifyAccountState();
                return GoogleLinkResult.Error;
            }

            try
            {
                string code = await RequestGoogleAuthCodeAsync();
                if (string.IsNullOrEmpty(code))
                {
                    _lastLinkError = "auth Google refusée ou annulée";
                    NotifyAccountState();
                    return GoogleLinkResult.Error;
                }

                await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(code);
                RefreshGoogleLinkedState();
                _pendingSwitchConfirm = false;
                _lastLinkError = "";
                Debug.Log("[Backend] Liaison Google Play Games OK — PlayerId inchangé.");
                NotifyAccountState();
                return GoogleLinkResult.Success;
            }
            catch (AuthenticationException ex)
                when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                _pendingSwitchConfirm = true;
                _lastLinkError = "compte déjà lié ailleurs";
                Debug.Log("[Backend] AccountAlreadyLinked — attente confirmation bascule.");
                NotifyAccountState();
                return GoogleLinkResult.AlreadyLinkedNeedsConfirm;
            }
            catch (AuthenticationException ex)
            {
                _lastLinkError = "erreur auth (" + ex.ErrorCode + ")";
                Debug.LogWarning("[Backend] Link Google échoué : " + ex.Message);
                NotifyAccountState();
                return GoogleLinkResult.Error;
            }
            catch (RequestFailedException ex)
            {
                _lastLinkError = "requête échouée";
                Debug.LogWarning("[Backend] Link Google requête échouée : " + ex.Message);
                NotifyAccountState();
                return GoogleLinkResult.Error;
            }
            catch (Exception ex)
            {
                _lastLinkError = "erreur inattendue";
                Debug.LogWarning("[Backend] Link Google : " + ex.Message);
                NotifyAccountState();
                return GoogleLinkResult.Error;
            }
#endif
        }

        /// <summary>
        /// Après confirmation UI : SignOut → SignIn Google → Compare cloud (politique P1).
        /// </summary>
        public static async Task<bool> ConfirmSwitchToLinkedGoogleAsync()
        {
            _lastLinkError = "";

#if !(UNITY_ANDROID && !UNITY_EDITOR)
            _lastLinkError = "appareil uniquement";
            _pendingSwitchConfirm = false;
            NotifyAccountState();
            return false;
#else
            if (!_pendingSwitchConfirm && !IsSignedIn)
            {
                // Autorise le bouton Debug même sans flag (force bascule)
            }

            try
            {
                if (IsSignedIn)
                    AuthenticationService.Instance.SignOut();

                string code = await RequestGoogleAuthCodeAsync();
                if (string.IsNullOrEmpty(code))
                {
                    _lastLinkError = "auth Google refusée ou annulée";
                    // Tentative de récupération anonyme pour ne pas laisser hors session
                    await TrySignInAnonymousQuietAsync();
                    NotifyAccountState();
                    return false;
                }

                await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(code);
                _pendingSwitchConfirm = false;
                RefreshGoogleLinkedState();
                string id = PlayerId;
                string shortId = id.Length > 8 ? id.Substring(0, 8) : id;
                Debug.Log("[Backend] Bascule Google OK — PlayerId=" + shortId + "…");
                NotifyAccountState();

                SyncServerTime();
                CloudSaveSync.CompareAndResolveAsync();
                return true;
            }
            catch (Exception ex)
            {
                _lastLinkError = "bascule échouée";
                Debug.LogWarning("[Backend] Bascule Google : " + ex.Message);
                await TrySignInAnonymousQuietAsync();
                NotifyAccountState();
                return false;
            }
#endif
        }

        /// <summary>
        /// Unlink Google — debug / QA uniquement (pas d'UI joueur v1).
        /// </summary>
        public static async Task UnlinkGoogleAsync()
        {
            _lastLinkError = "";
            try
            {
                if (!IsSignedIn)
                {
                    _lastLinkError = "non connecté";
                    NotifyAccountState();
                    return;
                }

                await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync();
                RefreshGoogleLinkedState();
                _pendingSwitchConfirm = false;
                Debug.Log("[Backend] Unlink Google OK (QA).");
                NotifyAccountState();
            }
            catch (Exception ex)
            {
                _lastLinkError = "unlink échoué";
                Debug.LogWarning("[Backend] Unlink Google : " + ex.Message);
                NotifyAccountState();
            }
        }

        /// <summary> Debug : arme le flag bascule (simule AccountAlreadyLinked côté UI). </summary>
        public static void DebugArmSwitchConfirm()
        {
            _pendingSwitchConfirm = true;
            _lastLinkError = "compte déjà lié ailleurs";
            NotifyAccountState();
        }

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

                RefreshGoogleLinkedState();
                NotifyAccountState();

                SyncServerTime();
                // Cloud save : compare au boot (fire-and-forget) — MT4-G2.
                CloudSaveSync.CompareAndResolveAsync();
                // Remote Config overlay — MT4-G3.
                _ = RemoteTuning.FetchAndApplyAsync();
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
                new System.Collections.Generic.Dictionary<string, object>());

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

        private static void RefreshGoogleLinkedState()
        {
            _isGoogleLinked = false;
            _googleDisplayName = "";
            try
            {
                if (!IsSignedIn)
                    return;

                PlayerInfo info = AuthenticationService.Instance.PlayerInfo;
                if (info == null)
                    return;

                string gpgId = info.GetGooglePlayGamesId();
                _isGoogleLinked = !string.IsNullOrEmpty(gpgId);

#if UNITY_ANDROID && !UNITY_EDITOR
                if (_isGoogleLinked)
                {
                    try
                    {
                        string name = PlayGamesPlatform.Instance.GetUserDisplayName();
                        if (!string.IsNullOrEmpty(name))
                            _googleDisplayName = name;
                    }
                    catch
                    {
                        // ignore
                    }
                }
#endif
            }
            catch
            {
                _isGoogleLinked = false;
            }
        }

        private static void NotifyAccountState()
        {
            try
            {
                OnAccountStateChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Backend] OnAccountStateChanged : " + e.Message);
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary> Authenticate + RequestServerSideAccess — code never logged. </summary>
        private static Task<string> RequestGoogleAuthCodeAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            try
            {
                PlayGamesPlatform.Activate();
                PlayGamesPlatform.Instance.Authenticate(status =>
                {
                    if (status != SignInStatus.Success)
                    {
                        tcs.TrySetResult(null);
                        return;
                    }

                    PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                    {
                        tcs.TrySetResult(code);
                    });
                });
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }

            return tcs.Task;
        }
#endif

        private static async Task TrySignInAnonymousQuietAsync()
        {
            try
            {
                if (!IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                RefreshGoogleLinkedState();
            }
            catch
            {
                // offline OK
            }
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
            _ = RemoteTuning.FetchAndApplyAsync();
        }

        internal static void OnHostApplicationPause(bool paused)
        {
            if (paused)
                CloudSaveSync.FlushIfDirty();
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

        /// <summary> Hôte DontDestroyOnLoad — focus + pause + coroutines. </summary>
        private sealed class BackendServiceHost : MonoBehaviour
        {
            private void OnApplicationFocus(bool hasFocus)
            {
                BackendService.OnHostApplicationFocus(hasFocus);
            }

            private void OnApplicationPause(bool pauseStatus)
            {
                BackendService.OnHostApplicationPause(pauseStatus);
            }
        }
    }
}
