using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Source de frames d'un portrait. AW1 ne connaît PAS AnimatedPortraitData :
    /// AW2/AW3 brancheront le vrai système via un adapter mince implémentant cette
    /// interface (spécifié en AW2 — ne pas l'écrire maintenant).
    /// </summary>
    public interface IPortraitFrameSource
    {
        /// <summary>Texture (ou sheet) courante.</summary>
        Texture Texture { get; }

        /// <summary>Taille d'UNE frame en texels (grille de l'art).</summary>
        Vector2Int FrameSizeTexels { get; }

        /// <summary>UV-rect normalisé de la frame à l'instant t (temps de séquence).</summary>
        Rect GetUvRect(float time);
    }
}
