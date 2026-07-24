using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Meta
{
    /// <summary>
    /// Gate de contenu : mappe un univers logique (roulement) vers un univers spawnable.
    /// Tant que seuls les assets U1 existent, forcer Ardacula évite des pools vides.
    /// </summary>
    [CreateAssetMenu(
        fileName = "UniverseContentConfig",
        menuName = "Chez Arthur/Meta/Universe Content Config",
        order = 10)]
    public class UniverseContentConfig : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Contenu disponible")]
        [Tooltip("Si true, tout spawn utilise Ardacula (tests / contenu incomplet).")]
        [SerializeField] private bool forceArdaculaOnly = true;

        [Tooltip("Univers dont les pools ennemis / décors sont prêts (1–5). Ignoré si forceArdaculaOnly.")]
        [SerializeField] private List<int> availableUniverseIds = new List<int> { UniverseIds.Ardacula };

        [Tooltip("Univers de repli si le logique n'est pas disponible.")]
        [SerializeField] private int fallbackUniverseId = UniverseIds.Ardacula;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public bool ForceArdaculaOnly => forceArdaculaOnly;
        public IReadOnlyList<int> AvailableUniverseIds => availableUniverseIds;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Résout l'univers à spawner / décorer à partir de l'univers logique du roulement.
        /// </summary>
        public int ResolveSpawnUniverse(int logicalUniverseId)
        {
            if (forceArdaculaOnly)
                return UniverseIds.Ardacula;

            if (IsAvailable(logicalUniverseId))
                return logicalUniverseId;

            if (IsAvailable(fallbackUniverseId))
                return fallbackUniverseId;

            return UniverseIds.Ardacula;
        }

        /// <summary>
        /// True si cet univers a du contenu prêt.
        /// </summary>
        public bool IsAvailable(int universeId)
        {
            if (!UniverseIds.IsValid(universeId))
                return false;

            if (availableUniverseIds == null || availableUniverseIds.Count == 0)
                return universeId == UniverseIds.Ardacula;

            for (int i = 0; i < availableUniverseIds.Count; i++)
            {
                if (availableUniverseIds[i] == universeId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Résolution sûre quand aucune config n'est assignée (défaut = Ardacula only).
        /// </summary>
        public static int ResolveSpawnUniverseOrDefault(UniverseContentConfig config, int logicalUniverseId)
        {
            if (config != null)
                return config.ResolveSpawnUniverse(logicalUniverseId);

            return UniverseIds.Ardacula;
        }
    }
}
