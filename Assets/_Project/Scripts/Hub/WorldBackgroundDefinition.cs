using UnityEngine;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Donnees d'un fond de monde hub (calques + vitesses UV + premier plan).
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

        [Tooltip("Position du wagon dans le canvas scene (coin haut-gauche, y depuis le haut). Lisible dans l'Aseprite.")]
        [SerializeField] private Vector2Int wagonCanvasPosition = new Vector2Int(28, 0);

        [Header("Premier plan (relivre par univers)")]
        [SerializeField] private Sprite wagonSprite;
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private Sprite windowGlareSprite;
        [Tooltip("Position du perso dans l'espace art wagon, coin haut-gauche, y depuis le haut.")]
        [SerializeField] private Vector2Int characterArtPosition = new Vector2Int(30, 249);
        [Tooltip("Position de la vitre dans l'espace art wagon, coin haut-gauche, y depuis le haut.")]
        [SerializeField] private Vector2Int glareArtPosition = new Vector2Int(0, 169);

        [Header("Overlay lumiere (optionnel, par univers)")]
        [SerializeField] private Sprite lightOverlaySprite;

        [SerializeField] private LayerEntry[] layers;

        // ===========================================
        // PROPRIETES PUBLIQUES
        // ===========================================

        public string WorldId => worldId;
        public string DisplayName => displayName;
        public Vector2Int NativeCanvasSize => nativeCanvasSize;
        public Vector2Int WagonCanvasPosition => wagonCanvasPosition;

        public Sprite WagonSprite => wagonSprite;
        public Sprite CharacterSprite => characterSprite;
        public Sprite WindowGlareSprite => windowGlareSprite;
        public Vector2Int CharacterArtPosition => characterArtPosition;
        public Vector2Int GlareArtPosition => glareArtPosition;
        public Sprite LightOverlaySprite => lightOverlaySprite;

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
