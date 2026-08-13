using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.RemoteConfig;
using ChezArthur.Meta;

namespace ChezArthur.Backend
{
    /// <summary>
    /// Overlay Remote Config : les SO restent la source de vérité ;
    /// absent / malformé / offline = jeu identique aux défauts.
    /// </summary>
    public static class RemoteTuning
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string KEY_SEASON_CALENDAR = "season_calendar";
        private const string KEY_SEASON_REWARDS = "season_rewards";
        private const string KEY_DIFFICULTY_TIERS = "difficulty_tiers";
        private const string KEY_LIVE_FLAGS = "live_flags";
        private const float FETCH_TIMEOUT_SECONDS = 5f;

        // ═══════════════════════════════════════════
        // ÉTAT
        // ═══════════════════════════════════════════
        private static bool _fetching;
        private static bool _warnedOnce;
        private static DateTime _lastFetchUtc;
        private static readonly List<string> _appliedKeys = new List<string>(4);
        private static bool _seasonEnabled = true;
        private static string _infoMessage = "";

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS / EVENTS
        // ═══════════════════════════════════════════
        public static bool SeasonEnabled => _seasonEnabled;
        public static string InfoMessage => _infoMessage ?? "";
        public static DateTime LastFetchUtc => _lastFetchUtc;
        public static IReadOnlyList<string> AppliedKeys => _appliedKeys;

        public static event Action OnTuningApplied;

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Fetch + parse défensif par clé + overlay. Jamais bloquant / jamais throw.
        /// </summary>
        public static async Task FetchAndApplyAsync()
        {
            if (_fetching)
                return;

            if (!BackendService.IsSignedIn)
                return;

            _fetching = true;
            try
            {
                Task<RuntimeConfig> fetchTask = RemoteConfigService.Instance.FetchConfigsAsync(
                    new UserAttributes(),
                    new AppAttributes());

                Task winner = await Task.WhenAny(
                    fetchTask,
                    Task.Delay(TimeSpan.FromSeconds(FETCH_TIMEOUT_SECONDS)));

                if (winner != fetchTask)
                {
                    WarnOnce("Fetch timeout " + FETCH_TIMEOUT_SECONDS + "s — défauts SO.");
                    return;
                }

                RuntimeConfig cfg = await fetchTask;
                if (cfg == null)
                {
                    WarnOnce("Fetch réponse nulle — défauts SO.");
                    return;
                }

                _appliedKeys.Clear();
                TryApplySeasonCalendar(cfg);
                TryApplySeasonRewards(cfg);
                TryApplyDifficulty(cfg);
                TryApplyLiveFlags(cfg);

                _lastFetchUtc = DateTime.UtcNow;
                Debug.Log(
                    "[Tuning] Fetch OK — appliqué=[" + string.Join(",", _appliedKeys) + "]");
                RaiseApplied();
            }
            catch (Exception e)
            {
                WarnOnce("Fetch échoué : " + e.Message);
            }
            finally
            {
                _fetching = false;
            }
        }

        /// <summary>
        /// Retour aux assets / défauts calendaire pour la session (debug).
        /// </summary>
        public static void ResetOverrides()
        {
            SeasonRewardsConfig.ClearRuntimeOverride();
            DifficultyConfig.ClearRuntimeOverride();
            SeasonRotationManager.ResetRemoteCalendar();
            _seasonEnabled = true;
            _infoMessage = "";
            _appliedKeys.Clear();
            Debug.Log("[Tuning] Overrides reset (session) — défauts SO.");
            RaiseApplied();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary> Force live flags (suite G3). </summary>
        public static void DebugSetLiveFlags(bool seasonEnabled, string infoMessage)
        {
            _seasonEnabled = seasonEnabled;
            _infoMessage = infoMessage ?? "";
            RaiseApplied();
        }

        /// <summary> Parse testable hors réseau. </summary>
        public static bool DebugTryParseCalendar(string json, out SeasonCalendarDto dto)
        {
            dto = null;
            try
            {
                if (string.IsNullOrEmpty(json))
                    return false;
                dto = JsonUtility.FromJson<SeasonCalendarDto>(json);
                return dto != null && !string.IsNullOrEmpty(dto.epochMondayIso);
            }
            catch
            {
                dto = null;
                return false;
            }
        }
#endif

        // ═══════════════════════════════════════════
        // INTERNE
        // ═══════════════════════════════════════════

        private static void TryApplySeasonCalendar(RuntimeConfig cfg)
        {
            string json = ReadJson(cfg, KEY_SEASON_CALENDAR);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                SeasonCalendarDto dto = JsonUtility.FromJson<SeasonCalendarDto>(json);
                if (dto == null || string.IsNullOrEmpty(dto.epochMondayIso))
                {
                    Debug.Log("[Tuning] season_calendar malformé — clé ignorée.");
                    return;
                }

                if (!DateTime.TryParseExact(
                        dto.epochMondayIso,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime epoch))
                {
                    Debug.Log("[Tuning] season_calendar epoch invalide — clé ignorée.");
                    return;
                }

                int length = dto.seasonLengthWeeks > 0 ? dto.seasonLengthWeeks : 6;
                SeasonRotationManager.ApplyRemoteCalendar(epoch, length);

                // LR list → clone / override rewards config (sans toucher l'asset).
                if (dto.lrBySeason != null && dto.lrBySeason.Length > 0)
                {
                    SeasonRewardsConfig baseCfg = SeasonRewardsConfig.LoadDefault();
                    SeasonRewardsConfig clone = UnityEngine.Object.Instantiate(baseCfg);
                    if (!clone.ApplyLrEntriesOverride(dto.lrBySeason))
                    {
                        UnityEngine.Object.Destroy(clone);
                    }
                    else
                    {
                        SeasonRewardsConfig.SetRuntimeInstance(clone);
                    }
                }

                _appliedKeys.Add(KEY_SEASON_CALENDAR);
            }
            catch (Exception e)
            {
                Debug.Log("[Tuning] season_calendar refusée : " + e.Message);
            }
        }

        private static void TryApplySeasonRewards(RuntimeConfig cfg)
        {
            string json = ReadJson(cfg, KEY_SEASON_REWARDS);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                SeasonRewardsDto dto = JsonUtility.FromJson<SeasonRewardsDto>(json);
                if (dto == null || dto.tiers == null)
                {
                    Debug.Log("[Tuning] season_rewards malformé — clé ignorée.");
                    return;
                }

                SeasonRewardsConfig baseCfg = SeasonRewardsConfig.LoadDefault();
                SeasonRewardsConfig clone = UnityEngine.Object.Instantiate(baseCfg);
                if (!clone.ApplyOverride(dto))
                {
                    UnityEngine.Object.Destroy(clone);
                    return;
                }

                SeasonRewardsConfig.SetRuntimeInstance(clone);
                _appliedKeys.Add(KEY_SEASON_REWARDS);
            }
            catch (Exception e)
            {
                Debug.Log("[Tuning] season_rewards refusée : " + e.Message);
            }
        }

        private static void TryApplyDifficulty(RuntimeConfig cfg)
        {
            string json = ReadJson(cfg, KEY_DIFFICULTY_TIERS);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                DifficultyTiersDto dto = JsonUtility.FromJson<DifficultyTiersDto>(json);
                if (dto == null || dto.tiers == null)
                {
                    Debug.Log("[Tuning] difficulty_tiers malformé — clé ignorée.");
                    return;
                }

                DifficultyConfig baseCfg = DifficultyConfig.LoadDefault();
                DifficultyConfig clone = UnityEngine.Object.Instantiate(baseCfg);
                if (!clone.ApplyOverride(dto))
                {
                    UnityEngine.Object.Destroy(clone);
                    return;
                }

                DifficultyConfig.SetRuntimeInstance(clone);
                _appliedKeys.Add(KEY_DIFFICULTY_TIERS);
            }
            catch (Exception e)
            {
                Debug.Log("[Tuning] difficulty_tiers refusée : " + e.Message);
            }
        }

        private static void TryApplyLiveFlags(RuntimeConfig cfg)
        {
            string json = ReadJson(cfg, KEY_LIVE_FLAGS);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                LiveFlagsDto dto = JsonUtility.FromJson<LiveFlagsDto>(json);
                if (dto == null)
                {
                    Debug.Log("[Tuning] live_flags malformé — clé ignorée.");
                    return;
                }

                _seasonEnabled = dto.seasonEnabled;
                _infoMessage = dto.infoMessage ?? "";
                _appliedKeys.Add(KEY_LIVE_FLAGS);
            }
            catch (Exception e)
            {
                Debug.Log("[Tuning] live_flags refusée : " + e.Message);
            }
        }

        private static string ReadJson(RuntimeConfig cfg, string key)
        {
            try
            {
                if (cfg == null || !cfg.HasKey(key))
                    return null;

                string json = cfg.GetJson(key);
                if (!string.IsNullOrEmpty(json))
                    return json;
                return cfg.GetString(key);
            }
            catch
            {
                return null;
            }
        }

        private static void RaiseApplied()
        {
            try
            {
                OnTuningApplied?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Tuning] OnTuningApplied : " + e.Message);
            }
        }

        private static void WarnOnce(string message)
        {
            if (_warnedOnce)
                return;
            _warnedOnce = true;
            Debug.LogWarning("[Tuning] " + message);
        }

        private struct UserAttributes { }
        private struct AppAttributes { }

        // ═══════════════════════════════════════════
        // DTOs (JsonUtility)
        // ═══════════════════════════════════════════

        [Serializable]
        public class SeasonCalendarDto
        {
            public string epochMondayIso;
            public int seasonLengthWeeks = 6;
            public SeasonLrEntryDto[] lrBySeason;
        }

        [Serializable]
        public class SeasonLrEntryDto
        {
            public int seasonIndex = 1;
            public string lrCharacterId;
        }

        [Serializable]
        public class SeasonRewardsDto
        {
            public SeasonTierDto[] tiers;
            public int prestigeStep = 150;
            public int prestigeTalsReward = 50;
        }

        [Serializable]
        public class SeasonTierDto
        {
            public int scoreRequired;
            public int talsReward;
            public bool grantsLrLevel;
        }

        [Serializable]
        public class DifficultyTiersDto
        {
            public DifficultyTierDto[] tiers;
            public int unlockStage = 50;
        }

        [Serializable]
        public class DifficultyTierDto
        {
            public string label;
            public float multiplier = 1f;
        }

        [Serializable]
        public class LiveFlagsDto
        {
            public bool seasonEnabled = true;
            public string infoMessage;
        }
    }
}
