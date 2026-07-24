using System;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Horloge jeu : fuseau Europe/Paris, ids de reset quotidien (00h00) et hebdo (lundi 00h00).
    /// Injectable en debug via <see cref="SetDebugOverride"/>.
    /// </summary>
    public static class GameClock
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string TZ_IANA = "Europe/Paris";
        private const string TZ_WINDOWS = "Romance Standard Time";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static TimeZoneInfo _parisZone;
        private static bool _parisZoneResolved;
        private static DateTime? _debugOverrideUtc;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Instant UTC courant (ou override debug). </summary>
        public static DateTime UtcNow => _debugOverrideUtc ?? DateTime.UtcNow;

        /// <summary> Instant courant en heure de Paris. </summary>
        public static DateTime ParisNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, GetParisTimeZone());

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
