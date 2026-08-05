using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Profil de fit sprite pour un prefab d'état (pack StateEffect ou legacy).
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusFxFitProfile : MonoBehaviour
    {
        [Tooltip("Taille monde de référence du pack à scale 1 (calibre le fit).")]
        public float designSize = 1.2f;

        [Tooltip("Marge autour du sprite (1 = pile bounds, >1 = auréole).")]
        public float padding = 1.2f;

        [Tooltip("Pack Y-up (enfants −90°) → remap plan XY caméra 2D.")]
        public bool remapYUpToXy = true;

        [Tooltip("Offset sorting vs le SpriteRenderer cible.")]
        public int sortingOrderOffset = 2;

        [Tooltip("Boucle de présence (sinon one-shot catalogue).")]
        public bool isLoop = true;

        [Tooltip("Durée one-shot (secondes) — conducteur root.")]
        public float oneShotDuration = 1.35f;
    }
}
