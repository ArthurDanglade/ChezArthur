using ChezArthur.Characters;
using ChezArthur.Meta;
using UnityEngine;

namespace ChezArthur.Missions
{
    /// <summary>
    /// Définition data-driven d'une mission.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Mission_",
        menuName = "Chez Arthur/Missions/Mission Data",
        order = 20)]
    public class MissionData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string missionId;
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 4)] private string description;
        [SerializeField] private MissionLayer layer;
        [SerializeField] private int sortOrder;

        [Header("Trigger")]
        [SerializeField] private MissionTriggerType triggerType;
        [Tooltip("Seuil : étage, compteur, ou 1 pour binaire.")]
        [SerializeField] private int targetValue = 1;
        [SerializeField] private bool firstTimeOnly;

        [Header("Filtres optionnels")]
        [SerializeField] private bool filterByRarity;
        [SerializeField] private CharacterRarity requiredRarity = CharacterRarity.SSR;

        [Header("Univers")]
        [Tooltip("UniverseCompleted : univers du slot 1 de la semaine courante.")]
        [SerializeField] private bool useSeasonSlot1Universe;

        [Header("Composition")]
        [SerializeField] private MissionCompositionRequirement compositionRequirement =
            MissionCompositionRequirement.None;
        [Tooltip("Ne progresse / ne valide la composition qu'à partir de cet étage (0 = aucun).")]
        [SerializeField] private int minStageForComposition;

        [Header("Récompense")]
        [SerializeField] private int rewardTals;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string MissionId => missionId;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionLayer Layer => layer;
        public int SortOrder => sortOrder;
        public MissionTriggerType TriggerType => triggerType;
        public int TargetValue => targetValue;
        public bool FirstTimeOnly => firstTimeOnly;
        public bool FilterByRarity => filterByRarity;
        public CharacterRarity RequiredRarity => requiredRarity;
        public bool UseSeasonSlot1Universe => useSeasonSlot1Universe;
        public MissionCompositionRequirement CompositionRequirement => compositionRequirement;
        public int MinStageForComposition => minStageForComposition;
        public int RewardTals => rewardTals;

        public bool IsLayerBonus => triggerType == MissionTriggerType.LayerCompletionBonus;
        public bool HasCompositionRequirement =>
            compositionRequirement != MissionCompositionRequirement.None;

        /// <summary>
        /// Titre UI résolu (univers slot 1 / rôle semaine).
        /// </summary>
        public string GetResolvedDisplayName()
        {
            if (triggerType == MissionTriggerType.UniverseCompleted && useSeasonSlot1Universe)
            {
                int u = SeasonRotationManager.GetCurrentUniverseAtSlot(0);
                return $"Terminer l'univers de {UniverseIds.GetDisplayName(u)}";
            }

            if (compositionRequirement == MissionCompositionRequirement.FullSeasonRole
                || compositionRequirement == MissionCompositionRequirement.FullSeasonRoleNoSwitch)
            {
                string role = WeeklyMissionSchedule.GetRoleDisplayName(
                    WeeklyMissionSchedule.GetCompositionRoleForCurrentWeek());
                return $"Atteindre l'étage {targetValue} — équipe full {role}";
            }

            return displayName;
        }

        public string GetResolvedDescription()
        {
            if (triggerType == MissionTriggerType.UniverseCompleted && useSeasonSlot1Universe)
            {
                int u = SeasonRotationManager.GetCurrentUniverseAtSlot(0);
                return $"Vaincs le boss final (étage 20) de {UniverseIds.GetDisplayName(u)}.";
            }

            if (compositionRequirement == MissionCompositionRequirement.FullSeasonRole
                || compositionRequirement == MissionCompositionRequirement.FullSeasonRoleNoSwitch)
            {
                string role = WeeklyMissionSchedule.GetRoleDisplayName(
                    WeeklyMissionSchedule.GetCompositionRoleForCurrentWeek());
                return $"Spé principale Hub de toute l'équipe = {role}. Seuil min. étage {minStageForComposition}.";
            }

            return description;
        }

#if UNITY_EDITOR
        public void EditorApply(
            string id,
            string name,
            string desc,
            MissionLayer missionLayer,
            MissionTriggerType trigger,
            int target,
            int tals,
            bool firstTime,
            int order,
            bool rarityFilter = false,
            CharacterRarity rarity = CharacterRarity.SSR,
            bool seasonSlot1Universe = false,
            MissionCompositionRequirement composition = MissionCompositionRequirement.None,
            int minCompositionStage = 0)
        {
            missionId = id;
            displayName = name;
            description = desc;
            layer = missionLayer;
            triggerType = trigger;
            targetValue = target;
            rewardTals = tals;
            firstTimeOnly = firstTime;
            sortOrder = order;
            filterByRarity = rarityFilter;
            requiredRarity = rarity;
            useSeasonSlot1Universe = seasonSlot1Universe;
            compositionRequirement = composition;
            minStageForComposition = minCompositionStage;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
