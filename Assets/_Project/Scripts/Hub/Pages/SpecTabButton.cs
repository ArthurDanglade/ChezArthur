using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Onglet de spécialisation (fond neutre + liseré rôle en bas).
    /// </summary>
    public class SpecTabButton : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image underlineImage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _specIndex;
        private CharacterRole _role = CharacterRole.Attacker;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void Setup(string label, CharacterRole role, int specIndex, Action<int> onClicked)
        {
            _specIndex = specIndex;
            _role = role;

            if (labelText != null)
                labelText.text = label;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(_specIndex));
            }

            SetActive(false);
        }

        public void SetActive(bool isActive)
        {
            // Toujours relire la palette (évite un accent figé rouge après changement de thème).
            Color accent = RolePalette.GetColor(_role);
            accent.a = 1f;

            if (backgroundImage != null)
                backgroundImage.color = isActive ? UiTheme.CardPanelEntry : Color.clear;

            if (labelText != null)
                labelText.color = isActive ? accent : UiTheme.TextMuted;

            if (underlineImage != null)
            {
                underlineImage.gameObject.SetActive(isActive);
                underlineImage.enabled = isActive;
                if (isActive)
                    underlineImage.color = accent;
            }
        }

        public void Cleanup()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }
    }
}
