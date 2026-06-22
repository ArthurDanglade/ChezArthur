using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Décor d'arène multi-calques. Gère une couleur de fond placeholder par univers
    /// (fallback tant que l'art n'est pas peint) + une pile ordonnée de calques d'art
    /// (background peint, puis calques animés à venir).
    ///
    /// Principe responsive (cf. tâche 2.2) : chaque calque est calé en FIT-WIDTH sur la
    /// largeur du cadre jouable, ratio préservé — JAMAIS d'étirement. La hauteur propre
    /// du sprite fournit le « bleed » vertical révélé sur les téléphones les plus hauts.
    /// L'ordre de rendu est piloté par le sortingOrder de chaque SpriteRenderer (réglé à
    /// la main dans l'éditeur), pas par ce script.
    /// </summary>
    public class ArenaDecor : MonoBehaviour
    {
        public enum FitMode
        {
            /// <summary>Largeur du sprite = largeur du cadre, ratio préservé. Mode de PRODUCTION.</summary>
            FitWidth,
            /// <summary>Remplit toute la vue caméra (recadre le débordement). APERÇU pour juger l'art avant le bleed.</summary>
            Cover
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [Tooltip("Arène source des bounds (cadre jouable).")]
        [SerializeField] private Arena arena;

        [Tooltip("Fond couleur unie (placeholder par univers). Sorting le plus bas. Doit avoir un sprite blanc (carré) pour afficher la couleur.")]
        [SerializeField] private SpriteRenderer baseColorRenderer;

        [Tooltip("Calques d'art peints (background, puis calques animés). L'ordre de rendu suit leur sortingOrder.")]
        [SerializeField] private List<SpriteRenderer> artLayers = new List<SpriteRenderer>();

        [Header("Cadrage")]
        [SerializeField] private FitMode fitMode = FitMode.FitWidth;

        [Tooltip("Décalage vertical (unités monde) pour aligner l'art sur la vue. À régler à l'œil.")]
        [SerializeField] private float verticalOffset = 0f;

        [Tooltip("Caméra de vue. Requise pour le mode Cover et pour que le fond couleur remplisse les écrans hauts.")]
        [SerializeField] private Camera viewCamera;

        [Header("Couleurs placeholder par univers")]
        [SerializeField] private Color colorUnivers1 = new Color(0.102f, 0.082f, 0.180f); // Ardacula — Le Château Maudit (gothique bleu-violet)
        [SerializeField] private Color colorUnivers2 = new Color(0.196f, 0.165f, 0.125f); // L'Ancien n°1 — Le Monde des Forts (épique, bronze)
        [SerializeField] private Color colorUnivers3 = new Color(0.094f, 0.110f, 0.133f); // Don Costardo — La Cité des Ombres (urbain, gris-bleu)
        [SerializeField] private Color colorUnivers4 = new Color(0.157f, 0.137f, 0.094f); // Morre Voeux — Les Terres du Pillage (glauque, olive)
        [SerializeField] private Color colorUnivers5 = new Color(0.176f, 0.063f, 0.051f); // Troplin — Erebor Assiégé (climax, rouge sombre)

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _lastScreenW;
        private int _lastScreenH;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Update()
        {
            // Le mode Cover dépend de la taille de vue : on ne re-cale que si l'écran a changé.
            if (fitMode != FitMode.Cover || arena == null) return;
            if (Screen.width == _lastScreenW && Screen.height == _lastScreenH) return;
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
            FitToBounds(arena.Bounds);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Aperçu live du cadrage en édition (réglage de verticalOffset, fitMode, etc.).
            if (arena != null)
                FitToBounds(arena.Bounds);
        }

        [ContextMenu("Refit décor")]
        private void RefitFromInspector()
        {
            if (arena != null)
                FitToBounds(arena.Bounds);
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique la couleur placeholder de l'univers (1 à 5) au fond couleur unie.
        /// Sans effet si baseColorRenderer non assigné.
        /// </summary>
        public void SetUnivers(int universNumber)
        {
            if (baseColorRenderer == null) return;

            baseColorRenderer.color = universNumber switch
            {
                1 => colorUnivers1,
                2 => colorUnivers2,
                3 => colorUnivers3,
                4 => colorUnivers4,
                5 => colorUnivers5,
                _ => colorUnivers1
            };
        }

        /// <summary>
        /// Cale tous les calques (fond + art) sur le cadre de l'arène.
        /// Appelé par StageGenerator à chaque génération d'étage.
        /// </summary>
        public void FitToBounds(Bounds bounds)
        {
            // Le fond couleur unie remplit toujours toute la vue (couleur unie → sur-dimensionner est gratuit).
            FitRenderer(baseColorRenderer, bounds, forceCover: true);

            if (artLayers == null) return;
            for (int i = 0; i < artLayers.Count; i++)
                FitRenderer(artLayers[i], bounds, forceCover: false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Positionne et redimensionne un SpriteRenderer sur le cadre, ratio préservé (échelle uniforme).
        /// </summary>
        private void FitRenderer(SpriteRenderer sr, Bounds bounds, bool forceCover)
        {
            if (sr == null || sr.sprite == null) return; // pas de sprite → rien à caler

            // Position : centré X, Y avec décalage de réglage. On conserve le Z existant.
            Vector3 p = sr.transform.position;
            sr.transform.position = new Vector3(bounds.center.x, bounds.center.y + verticalOffset, p.z);

            Vector2 spriteSize = sr.sprite.bounds.size; // unités monde à l'échelle 1 (tient compte du PPU)
            float sw = spriteSize.x > 0f ? spriteSize.x : 1f;
            float sh = spriteSize.y > 0f ? spriteSize.y : 1f;

            float scale;
            if (fitMode == FitMode.Cover || forceCover)
            {
                // Cover : on remplit la vue, on recadre le débordement. Échelle uniforme.
                float viewW = bounds.size.x;
                float viewH = bounds.size.y;
                if (viewCamera != null && viewCamera.orthographic)
                {
                    viewH = viewCamera.orthographicSize * 2f;
                    viewW = viewH * viewCamera.aspect;
                }
                scale = Mathf.Max(viewW / sw, viewH / sh);
            }
            else
            {
                // FitWidth : largeur du sprite = largeur du cadre. Ratio préservé. JAMAIS d'étirement.
                scale = bounds.size.x / sw;
            }

            sr.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
