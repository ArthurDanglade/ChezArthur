using System;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Horloge jeu : fuseau Europe/Paris, ids de reset quotidien / hebdo.
    /// Ordre UtcNowGuarded : override debug &gt; ancre serveur (realtime) &gt; garde locale PlayerPrefs.
    /// </summary>
    public static class GameClock
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string TZ_IANA = "Europe/Paris";
        private const string TZ_WINDOWS = "Romance Standard Time";
        private const string PREFS_LAST_SEEN_UTC_TICKS = "GameClock_LastSeenUtcTicks";
        private const long ROLLBACK_TOLERANCE_TICKS = TimeSpan.TicksPerMinute * 5;
        private const long FLOOR_WRITE_INTERVAL_TICKS = TimeSpan.TicksPerMinute;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static TimeZoneInfo _parisZone;
        private static bool _parisZoneResolved;
        private static DateTime? _debugOverrideUtc;
        private static bool _rollbackWarnedThisSession;
        private static long _lastFloorWriteUtcTicks;

        private static DateTime _serverAnchorUtc;
        private static double _serverAnchorRealtime;
        private static bool _hasServerAnchor;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Instant UTC courant (ou override debug). Brut — pas d'ancre serveur. </summary>
        public static DateTime UtcNow => _debugOverrideUtc ?? DateTime.UtcNow;

        /// <summary> True si une ancre temps serveur est posée (MT4-G1). </summary>
        public static bool HasServerTime => _hasServerAnchor;

        /// <summary>
        /// UTC résolu : override debug &gt; ancre serveur (realtime) &gt; garde locale.
        /// L'ancre serveur est immunisée contre un changement d'horloge device en session.
        /// </summary>
        public static DateTime UtcNowGuarded
        {
            get
            {
                // 1) Voyage dans le temps debug — toujours prioritaire (tests MT2).
                if (_debugOverrideUtc.HasValue)
                    return _debugOverrideUtc.Value;

                // 2) Ancre serveur : serverUtc + delta realtime (pas DateTime.UtcNow).
                // 3) Sinon horloge device, puis plancher PlayerPrefs.
                DateTime now = ResolveBaseUtc();

                long floorTicks = 0;
                string raw = PlayerPrefs.GetString(PREFS_LAST_SEEN_UTC_TICKS, "");
                if (!string.IsNullOrEmpty(raw) && long.TryParse(raw, out long parsed))
                    floorTicks = parsed;

                if (floorTicks > 0 && now.Ticks < floorTicks - ROLLBACK_TOLERANCE_TICKS)
                {
                    if (!_rollbackWarnedThisSession)
                    {
                        _rollbackWarnedThisSession = true;
                        Debug.LogWarning(
                            "[GameClock] Horloge reculée — temps gelé au plancher (garde locale).");
                    }
                    return new DateTime(floorTicks, DateTimeKind.Utc);
                }

                // Nourrir le plancher avec le temps résolu (serveur vu → sessions offline suivantes).
                if (now.Ticks > floorTicks
                    && now.Ticks - _lastFloorWriteUtcTicks >= FLOOR_WRITE_INTERVAL_TICKS)
                {
                    PlayerPrefs.SetString(PREFS_LAST_SEEN_UTC_TICKS, now.Ticks.ToString());
                    PlayerPrefs.Save();
                    _lastFloorWriteUtcTicks = now.Ticks;
                }

                return now;
            }
        }

        /// <summary> Instant courant en heure de Paris (via UtcNowGuarded). </summary>
        public static DateTime ParisNow =>
            TimeZoneInfo.ConvertTimeFromUtc(UtcNowGuarded, GetParisTimeZone());

        /// <summary> True si un override debug est actif. </summary>
        public static bool HasDebugOverride => _debugOverrideUtc.HasValue;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Pose l'ancre temps serveur (appelée par BackendService après Cloud Code).
        /// </summary>
        public static void SetServerAnchor(DateTime serverUtc, double realtimeAtFetch)
        {
            if (serverUtc.Kind == DateTimeKind.Unspecified)
                serverUtc = DateTime.SpecifyKind(serverUtc, DateTimeKind.Utc);
            else if (serverUtc.Kind == DateTimeKind.Local)
                serverUtc = serverUtc.ToUniversalTime();

            _serverAnchorUtc = serverUtc;
            _serverAnchorRealtime = realtimeAtFetch;
            _hasServerAnchor = true;
            Debug.Log("[GameClock] Ancre serveur posée — " + serverUtc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        }

        /// <summary>
        /// Id journalier Paris : "yyyy-MM-dd" du jour civil courant (change à 00h00 Paris).
        /// </summary>
        public static string GetDailyResetId()
        {
            DateTime paris = ParisNow;
            return FormatDateId(paris.Year, paris.Month, paris.Day);
        }

        /// <summary>
        /// Id hebdomadaire : lundi 00h00 Paris de la semaine courante, format "yyyy-MM-dd".
        /// </summary>
        public static string GetWeeklyResetId()
        {
            DateTime monday = GetMondayOfCurrentWeekParis();
            return FormatDateId(monday.Year, monday.Month, monday.Day);
        }

        /// <summary>
        /// Lundi 00h00 (heure de Paris) de la semaine civile courante.
        /// </summary>
        public static DateTime GetMondayOfCurrentWeekParis()
        {
            DateTime paris = ParisNow.Date;
            int offset = ((int)paris.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return paris.AddDays(-offset);
        }

        /// <summary>
        /// Nombre de semaines complètes écoulées depuis un lundi d'époque (Paris), borné ≥ 0.
        /// </summary>
        public static int GetWeeksSinceEpochMonday(DateTime epochMondayParisDate)
        {
            DateTime epoch = epochMondayParisDate.Date;
            DateTime monday = GetMondayOfCurrentWeekParis();
            int days = (int)(monday - epoch).TotalDays;
            if (days < 0)
                return 0;
            return days / 7;
        }

        /// <summary>
        /// Force l'horloge (UTC). Passer null pour revenir au temps réel / serveur.
        /// </summary>
        public static void SetDebugOverride(DateTime? utcOverride)
        {
            if (utcOverride.HasValue && utcOverride.Value.Kind == DateTimeKind.Unspecified)
                _debugOverrideUtc = DateTime.SpecifyKind(utcOverride.Value, DateTimeKind.Utc);
            else
                _debugOverrideUtc = utcOverride;
        }

        /// <summary>
        /// Avance l'override debug d'un nombre de jours (crée un override sur UtcNow si besoin).
        /// </summary>
        public static void DebugAdvanceDays(int days)
        {
            DateTime baseUtc = UtcNow;
            SetDebugOverride(baseUtc.AddDays(days));
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Évalue la garde anti-recul avec un "now" fictif (n'écrit pas le plancher).
        /// </summary>
        public static DateTime DebugEvaluateAntiRollback(DateTime fakeUtcNow, out bool frozeToFloor)
        {
            frozeToFloor = false;
            long floorTicks = 0;
            string raw = PlayerPrefs.GetString(PREFS_LAST_SEEN_UTC_TICKS, "");
            if (!string.IsNullOrEmpty(raw) && long.TryParse(raw, out long parsed))
                floorTicks = parsed;

            if (floorTicks > 0 && fakeUtcNow.Ticks < floorTicks - ROLLBACK_TOLERANCE_TICKS)
            {
                frozeToFloor = true;
                return new DateTime(floorTicks, DateTimeKind.Utc);
            }

            return fakeUtcNow.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(fakeUtcNow, DateTimeKind.Utc)
                : fakeUtcNow;
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static DateTime ResolveBaseUtc()
        {
            if (_hasServerAnchor)
            {
                double elapsed = Time.realtimeSinceStartupAsDouble - _serverAnchorRealtime;
                if (elapsed < 0d)
                    elapsed = 0d;
                return _serverAnchorUtc.AddSeconds(elapsed);
            }

            return DateTime.UtcNow;
        }

        private static TimeZoneInfo GetParisTimeZone()
        {
            if (_parisZoneResolved)
                return _parisZone;

            _parisZoneResolved = true;
            try
            {
                _parisZone = TimeZoneInfo.FindSystemTimeZoneById(TZ_IANA);
                return _parisZone;
            }
            catch (Exception)
            {
                // Windows / certaines runtimes Unity exposent l'id Windows.
            }

            try
            {
                _parisZone = TimeZoneInfo.FindSystemTimeZoneById(TZ_WINDOWS);
                return _parisZone;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[GameClock] Fuseau Paris introuvable ({TZ_IANA} / {TZ_WINDOWS}). " +
                    $"Fallback UTC+1 fixe. ({e.Message})");
                _parisZone = TimeZoneInfo.CreateCustomTimeZone(
                    "ChezArthur_Paris_Fallback",
                    TimeSpan.FromHours(1),
                    "Paris Fallback",
                    "Paris Fallback");
                return _parisZone;
            }
        }

        private static string FormatDateId(int year, int month, int day)
        {
            return $"{year:D4}-{month:D2}-{day:D2}";
        }
    }
}
