using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Scaffold micro-décision (RUI-D2) : scrim + carte centrale.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class PopupScaffold : MonoBehaviour
    {
        [SerializeField] private Image scrim;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private Button closeButton;

        public Image Scrim => scrim;
        public RectTransform CardRoot => cardRoot;
        public Button CloseButton => closeButton;

        public void Bind(Image scrimImage, RectTransform card, Button close)
        {
            scrim = scrimImage;
            cardRoot = card;
            closeButton = close;
        }
    }
}
