using UnityEngine;
using UnityEngine.EventSystems;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Relais pointeur sur la zone artwork du CharacterDetailPopup (hold-to-equip).
    /// </summary>
    public class ArtworkHoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private CharacterDetailPopup _popup;
        private bool _enabled = true;

        public void Bind(CharacterDetailPopup popup)
        {
            _popup = popup;
        }

        public void SetRelayEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_enabled)
                return;
            _popup?.NotifyArtworkPointerDown(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_enabled)
                return;
            _popup?.NotifyArtworkPointerUp(eventData);
        }
    }
}
