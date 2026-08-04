using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Core
{
    /// <summary>
    /// Chaîne de migration du schéma de sauvegarde. Appelé par SaveSystem.Load
    /// AVANT tout usage des données. Chaque étape est idempotente.
    /// </summary>
    public static class SaveMigrator
    {
        /// <summary>
        /// Migre les données jusqu'à la version courante.
        /// </summary>
        /// <returns>True si une migration de version a eu lieu.</returns>
        public static bool MigrateToCurrent(SaveData data)
        {
            if (data == null)
                return false;

            int from = data.saveVersion;
            if (from >= SaveSystem.CURRENT_SAVE_VERSION)
            {
                NormalizeNulls(data);
                return false;
            }

            if (from < 1)
                MigrateV0ToV1(data);
            if (from < 2)
                MigrateV1ToV2(data);
            if (from < 3)
                MigrateV2ToV3(data);

            NormalizeNulls(data);
            data.saveVersion = SaveSystem.CURRENT_SAVE_VERSION;
            Debug.Log($"[SaveMigrator] Migration v{from} → v{SaveSystem.CURRENT_SAVE_VERSION}");
            return true;
        }

        /// <summary>
        /// v0 → v1 : migration de l'équipe legacy mono-preset vers teamPreset0.
        /// Ne vide pas selectedTeamIds (filet CharacterManager conservé).
        /// </summary>
        private static void MigrateV0ToV1(SaveData data)
        {
            bool presetsEmpty =
                IsListEmpty(data.teamPreset0)
                && IsListEmpty(data.teamPreset1)
                && IsListEmpty(data.teamPreset2)
                && IsListEmpty(data.teamPreset3)
                && IsListEmpty(data.teamPreset4);

            if (presetsEmpty && data.selectedTeamIds != null && data.selectedTeamIds.Count > 0)
            {
                data.teamPreset0 = new List<string>(data.selectedTeamIds);
                data.activePresetIndex = 0;
            }
        }

        /// <summary>
        /// Champs additifs uniquement (records, missions, Boss Rush, hint) —
        /// défauts de type corrects par construction. Historique exact non tracé :
        /// étape conservée comme point d'ancrage.
        /// </summary>
        private static void MigrateV1ToV2(SaveData data)
        {
        }

        /// <summary>
        /// Champs additifs uniquement (records, missions, Boss Rush, hint) —
        /// défauts de type corrects par construction. Historique exact non tracé :
        /// étape conservée comme point d'ancrage.
        /// </summary>
        private static void MigrateV2ToV3(SaveData data)
        {
        }

        /// <summary>
        /// Protège des saves éditées à la main : listes/strings null → valeurs sûres.
        /// </summary>
        private static void NormalizeNulls(SaveData data)
        {
            if (data.playerName == null)
                data.playerName = "";
            if (data.ownedCharacters == null)
                data.ownedCharacters = new List<Characters.OwnedCharacter>();
            if (data.teamPreset0 == null)
                data.teamPreset0 = new List<string>();
            if (data.teamPreset1 == null)
                data.teamPreset1 = new List<string>();
            if (data.teamPreset2 == null)
                data.teamPreset2 = new List<string>();
            if (data.teamPreset3 == null)
                data.teamPreset3 = new List<string>();
            if (data.teamPreset4 == null)
                data.teamPreset4 = new List<string>();
            if (data.selectedTeamIds == null)
                data.selectedTeamIds = new List<string>();
            if (data.pityBannerIds == null)
                data.pityBannerIds = new List<string>();
            if (data.pityCounts == null)
                data.pityCounts = new List<int>();
            if (data.lastDailyResetId == null)
                data.lastDailyResetId = "";
            if (data.lastWeeklyResetId == null)
                data.lastWeeklyResetId = "";
            if (data.lastSeasonId == null)
                data.lastSeasonId = "";
            if (data.missionProgress == null)
                data.missionProgress = new List<MissionProgressSaveEntry>();
            if (data.bossRushEnemyIds == null)
                data.bossRushEnemyIds = new List<string>();
            if (data.bossRushMajorBossIds == null)
                data.bossRushMajorBossIds = new List<string>();
            if (data.bossRushWeeklyCountedIds == null)
                data.bossRushWeeklyCountedIds = new List<string>();
        }

        private static bool IsListEmpty(List<string> list)
        {
            return list == null || list.Count == 0;
        }

        // ───────────────────────────────────────────
        // Gabarit v4 (à activer au prochain changement de schéma) :
        // 1. Incrémenter SaveSystem.CURRENT_SAVE_VERSION.
        // 2. Ajouter MigrateV3ToV4(SaveData) et l'appeler dans MigrateToCurrent (if from < 4).
        // 3. Documenter le changement de schéma dans la docstring de MigrateV3ToV4.
        // ───────────────────────────────────────────
    }
}
