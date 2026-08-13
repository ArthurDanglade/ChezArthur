using System;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Horloge jeu : fuseau Europe/Paris, ids de reset quotidien (00h00) et hebdo (lundi 00h00).
    /// Injectable en debug via <see cref="SetDebugOverride"/>.
    /// Garde locale anti-recul en attendant le temps serveur (MT4) —
    /// limitation assumée : PlayerPrefs effaçables.
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

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Instant UTC courant (ou override debug). </summary>
        public static DateTime UtcNow => _debugOverrideUtc ?? DateTime.UtcNow;

        /// <summary>
        /// UTC avec garde anti-recul (PlayerPrefs). L'override debug court-circuite le plancher.
        /// </summary>
        public static DateTime UtcNowGuarded
        {
            get
            {
                // Voyage dans le temps debug : jamais bloqué par la garde.
                if (_debugOverrideUtc.HasValue)
                    return _debugOverrideUtc.Value;

                DateTime now = DateTime.UtcNow;
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
                            "[GameClock] Horloge reculée — temps gelé au plancher (garde locale MT2).");
                    }
                    return new DateTime(floorTicks, DateTimeKind.Utc);
                }

                // Progression : écrire le plancher au plus 1×/minute.
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
        /// Force l'horloge (UTC). Passer null pour revenir au temps réel.
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
        /// Évalue la garde anti-recul avec un "now" fictif (ne mute pas l'override, n'écrit pas le plancher).
        /// Pour la suite d'intégrité G5 — simule une horloge système reculée.
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
