using UnityEngine;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Jeu de frames pour animer un calque de fond (flipbook).
    /// Conteneur pur : aucune logique.
    /// </summary>
    [CreateAssetMenu(menuName = "ChezArthur/Hub/Layer Frame Set")]
    public class LayerFrameSet : ScriptableObject
    {
        [SerializeField] private Texture2D[] frames;
        [SerializeField] private float framesPerSecond = 8f;

        public Texture2D[] Frames => frames;
        public float FramesPerSecond => framesPerSecond;

        public bool IsValid => frames != null && frames.Length > 1
            && framesPerSecond > 0f;
    }
}
