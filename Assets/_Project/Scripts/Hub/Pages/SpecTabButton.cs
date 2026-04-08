using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages
{
    public class SpecTabButton : MonoBehaviour
    {
        private static readonly Color AttackerColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color DefenderColor = new Color(0.25f, 0.70f, 0.35f, 1f);
        private static readonly Color SupportColor = new Color(0.25f, 0.45f, 0.85f, 1f);
        private static readonly Color DefaultColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private Image backgroundImage;

        private int _specIndex;

        public void Setup(string label, CharacterRole role, int specIndex, Action<int> onClicked)
        {
            _specIndex = specIndex;

            if (labelText != null)
                labelText.text = label;

            if (backgroundImage != null)
                backgroundImage.color = GetColorForRole(role);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClicked?.Invoke(_specIndex));
            }
        }

        public void SetActive(bool isActive)
        {
            float bgAlpha = isActive ? 1f : 0.45f;
            float textAlpha = isActive ? 1f : 0.65f;

            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                c.a = bgAlpha;
                backgroundImage.color = c;
            }

            if (labelText != null)
            {
                Color c = labelText.color;
                c.a = textAlpha;
                labelText.color = c;
            }
        }

        public void Cleanup()
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        private static Color GetColorForRole(CharacterRole role)
        {
            return role switch
            {
                CharacterRole.Attacker => AttackerColor,
                CharacterRole.Defender => DefenderColor,
                CharacterRole.Support => SupportColor,
                _ => DefaultColor
            };
        }
    }
}
