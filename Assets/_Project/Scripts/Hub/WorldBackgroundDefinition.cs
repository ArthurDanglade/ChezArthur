using UnityEngine;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Mode de pose des calques sur le ParallaxManager.
    /// </summary>
    public enum LayerLayoutMode
    {
        /// <summary>Ancien comportement montagne : stretch plein parent.</summary>
        StretchFullBleed = 0,
        /// <summary>Canvas natif + offset Y (strips type U1).</summary>
        NativeStacked = 1
    }

    /// <summary>
    /// Donnees d'un fond de monde hub (calques + vitesses UV + cadrage).
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
        [SerializeField] private Vector2Int nativeCanvasSize = new Vector2Int(228, 532);
        [SerializeField] private LayerLayoutMode layoutMode = LayerLayoutMode.NativeStacked;
        [Tooltip("Y natif (depuis le haut) aligne au centre vertical du trou de fenetre wagon.")]
        [SerializeField] private float nativeFocusY = 260f;
        [Tooltip("0 = cale largeur du trou, 1 = cale hauteur du trou, entre = lerp.")]
        [Range(0f, 1f)]
        [SerializeField] private float nativeFitBias = 0f;
        [Tooltip("-1 = centre horizontal, sinon px natif.")]
        [SerializeField] private float nativeFocusX = -1f;
        [SerializeField] private LayerEntry[] layers;

        // ===========================================
        // PROPRIETES PUBLIQUES
        // ===========================================

        public string WorldId => worldId;
        public string DisplayName => displayName;
        public Vector2Int NativeCanvasSize => nativeCanvasSize;
        public LayerLayoutMode LayoutMode => layoutMode;
        public float NativeFocusY => nativeFocusY;
        public float NativeFitBias => nativeFitBias;
        public float NativeFocusX => nativeFocusX;

        /// <summary>
        /// Index 0 = calque le plus en arriere.
        /// </summary>
        public LayerEntry[] Layers => layers;

        // ===========================================
        // TYPES IMBRIQUES
        // ===========================================

        /// <summary>
        /// Entree d'un calque (texture + vitesse UV + offset Y natif).
        /// </summary>
        [System.Serializable]
        public class LayerEntry
        {
            [SerializeField] private string layerName;
            [SerializeField] private Texture2D texture;
            [SerializeField] private int nativeOffsetY;
            [SerializeField] private float scrollSpeed;

            public string LayerName => layerName;
            public Texture2D Texture => texture;
            public int NativeOffsetY => nativeOffsetY;
            public float ScrollSpeed => scrollSpeed;
        }
    }
}
