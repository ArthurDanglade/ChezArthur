using System;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Roulement saisonnier 5 semaines × 5 slots d'univers.
    /// Slot 0 = étages 1–20, slot 1 = 21–40, etc.
    /// </summary>
    public static class SeasonRotationManager
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Lundi d'époque de la saison 1 (date civile Paris).
        /// Semaine 1 du tableau = première semaine à partir de cette date.
        /// Ajustable via <see cref="SetEpochMondayParis"/> si le calendrier marketing change.
        /// </summary>
        private static DateTime _epochMondayParis = new DateTime(2026, 7, 20);

        private const int SEASON_WEEK_COUNT = 5;
        private const int SLOT_COUNT = 5;
        private const int UNIVERSE_SIZE = 20;

        /// <summary>
        /// [weekIndex 0–4][slotIndex 0–4] → universeId 1–5.
        /// W1: Ardacula, Troplin, Don Costardo, L'Ancien, Faille.
        /// </summary>
        private static readonly int[,] RotationTable =
        {
            { UniverseIds.Ardacula, UniverseIds.Troplin, UniverseIds.DonCostardo, UniverseIds.Ancien, UniverseIds.Faille },
            { UniverseIds.Troplin, UniverseIds.DonCostardo, UniverseIds.Ancien, UniverseIds.Faille, UniverseIds.Ardacula },
            { UniverseIds.DonCostardo, UniverseIds.Ancien, UniverseIds.Faille, UniverseIds.Ardacula, UniverseIds.Troplin },
            { UniverseIds.Ancien, UniverseIds.Faille, UniverseIds.Ardacula, UniverseIds.Troplin, UniverseIds.DonCostardo },
            { UniverseIds.Faille, UniverseIds.Ardacula, UniverseIds.Troplin, UniverseIds.DonCostardo, UniverseIds.Ancien }
        };

        private static int? _debugForcedWeekIndex;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Index de semaine de rotation 0–4 (semaine 1 du doc = 0). </summary>
        public static int CurrentWeekIndex => ResolveWeekIndex();

        /// <summary> Semaine affichable 1–5. </summary>
        public static int CurrentWeekNumber => CurrentWeekIndex + 1;

        /// <summary> Id de saison dérivé de l'époque + cycle (pour resets saisonniers futurs). </summary>
        public static string CurrentSeasonId
        {
            get
            {
                int weeks = GameClock.GetWeeksSinceEpochMonday(_epochMondayParis);
                int seasonIndex = weeks / SEASON_WEEK_COUNT;
                return $"S{seasonIndex + 1}";
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Univers logique (1–5) pour un slot 0–4 selon la semaine courante.
        /// </summary>
        public static int GetCurrentUniverseAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SLOT_COUNT)
            {
                Debug.LogWarning($"[SeasonRotation] slotIndex hors plage : {slotIndex}");
                return UniverseIds.Ardacula;
            }

            return RotationTable[CurrentWeekIndex, slotIndex];
        }

        /// <summary>
        /// Slot saisonnier (0–4) pour un numéro d'étage (1+). Post-100 continue le cycle.
        /// </summary>
        public static int GetSlotIndexForStage(int stageNumber)
        {
            if (stageNumber < 1)
                stageNumber = 1;

            int block = (stageNumber - 1) / UNIVERSE_SIZE;
            return block % SLOT_COUNT;
        }

        /// <summary>
        /// Univers logique pour un étage donné (avant content gate).
        /// </summary>
        public static int GetLogicalUniverseForStage(int stageNumber)
        {
            return GetCurrentUniverseAtSlot(GetSlotIndexForStage(stageNumber));
        }

        /// <summary>
        /// Redéfinit le lundi d'époque (date civile Paris, sans heure).
        /// </summary>
        public static void SetEpochMondayParis(DateTime mondayParisDate)
        {
            _epochMondayParis = mondayParisDate.Date;
        }

        /// <summary>
        /// Force la semaine de rotation 0–4. Null = calcul via GameClock.
        /// </summary>
        public static void SetDebugForcedWeekIndex(int? weekIndex0To4)
        {
            if (weekIndex0To4.HasValue)
                _debugForcedWeekIndex = Mathf.Clamp(weekIndex0To4.Value, 0, SEASON_WEEK_COUNT - 1);
            else
                _debugForcedWeekIndex = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary> Lecture debug de la table pour un weekIndex donné. </summary>
        public static int DebugGetUniverseAt(int weekIndex0To4, int slotIndex)
        {
            weekIndex0To4 = Mathf.Clamp(weekIndex0To4, 0, SEASON_WEEK_COUNT - 1);
            slotIndex = Mathf.Clamp(slotIndex, 0, SLOT_COUNT - 1);
            return RotationTable[weekIndex0To4, slotIndex];
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static int ResolveWeekIndex()
        {
            if (_debugForcedWeekIndex.HasValue)
                return _debugForcedWeekIndex.Value;

            int weeks = GameClock.GetWeeksSinceEpochMonday(_epochMondayParis);
            return weeks % SEASON_WEEK_COUNT;
        }
    }
}
