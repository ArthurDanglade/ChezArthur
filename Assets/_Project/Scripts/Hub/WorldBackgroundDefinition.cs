using UnityEngine;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Donnees d'un fond de monde hub (calques + vitesses UV).
    /// Conteneur pur : aucune logique.
    /// </summary>
    [CreateAssetMenu(menuName = "ChezArthur/Hub/World Background Definition")]
    public class WorldBackgroundDefinition : ScriptableObject
    {
        // ===========================================
        // SERIALIZED FIELDS
        // ===========================================

        [SerializeField] private string worldId;
        [SerializeField] private string displayName;
        [SerializeField] private LayerEntry[] layers;

        // ===========================================
        // PROPRIETES PUBLIQUES
        // ===========================================

        public string WorldId => worldId;
        public string DisplayName => displayName;

        /// <summary>
        /// Index 0 = calque le plus en arriere.
        /// </summary>
        public LayerEntry[] Layers => layers;

        // ===========================================
        // TYPES IMBRIQUES
        // ===========================================

        /// <summary>
        /// Entree d'un calque (texture + vitesse UV / seconde).
        /// </summary>
        [System.Serializable]
        public class LayerEntry
        {
            [SerializeField] private string layerName;
            [SerializeField] private Texture2D texture;
            [SerializeField] private float scrollSpeed;

            public string LayerName => layerName;
            public Texture2D Texture => texture;
            public float ScrollSpeed => scrollSpeed;
        }
    }
}
