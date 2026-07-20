using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Conteneur de références UI pour l'overlay de cérémonie d'éveil.
    /// </summary>
    public class AwakeningCeremonyView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private CharacterArtworkView primeView;
        [SerializeField] private CharacterArtworkView dechuView;
        [SerializeField] private RawImage dechuRawImage;
        [SerializeField] private Button tapButton;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public CanvasGroup CanvasGroup => canvasGroup;
        public TextMeshProUGUI TitleText => titleText;
        public TextMeshProUGUI NameText => nameText;
        public CharacterArtworkView PrimeView => primeView;
        public CharacterArtworkView DechuView => dechuView;
        public RawImage DechuRawImage => dechuRawImage;
        public Button TapButton => tapButton;
    }
}
