using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bootstrap temporaire pour tester CharacterArtworkView en scène.
    /// </summary>
    public class ArtworkViewTestBootstrap : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Test Gate 3")]
        [SerializeField] private CharacterData data;
        [SerializeField] private CharacterArtworkView view;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (view == null || data == null)
                return;

            view.Show(data);
        }

        private void OnDestroy()
        {
            if (view == null)
                return;

            view.Release();
        }
    }
}
