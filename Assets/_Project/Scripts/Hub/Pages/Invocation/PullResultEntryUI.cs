using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gacha;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Carte personnage du récap d'invocation (grille ou grande carte x1).
    /// </summary>
    public class PullResultEntryUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float StatusChipFrameAlpha = 0.28f;
        private const float SsrGlowAlpha = 0.35f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Affichage")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI iconFallbackText;
        [SerializeField] private Image rarityTopBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI statusChipText;
        [SerializeField] private Image statusChipFrame;
        [SerializeField] private GameObject rateUpBadge;
        [SerializeField] private Button cardButton;
        [SerializeField] private Image ssrGlow;
        [SerializeField] private CanvasGroup canvasGroup;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Action<PulledCharacter> _onClicked;
        private PulledCharacter _pulled;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Configure la carte (surcharge legacy — sans clic).
        /// </summary>
        public void Setup(CharacterData data, PulledCharacter pulled)
        {
            Setup(data, pulled, null);
        }

        /// <summary>
        /// Configure la carte avec callback de clic (fiche personnage).
        /// </summary>
        public void Setup(
            CharacterData data,
            PulledCharacter pulled,
            Action<PulledCharacter> onClicked)
        {
            _pulled = pulled;
            _onClicked = onClicked;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            ApplyIcon(data, pulled);
            ApplyRarityChrome(data);
            ApplyName(data, pulled);
            ApplyStatusChip(pulled);
            ApplyRateUp(pulled);
            WireButton();
        }

        /// <summary>
        /// Retire les listeners (pooling / fin de récap).
        /// </summary>
        public void Cleanup()
        {
            if (cardButton != null)
                cardButton.onClick.RemoveAllListeners();

            _onClicked = null;
            _pulled = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyIcon(CharacterData data, PulledCharacter pulled)
        {
            bool hasIcon = data != null && data.Icon != null;

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    iconImage.sprite = data.Icon;
                    iconImage.color = Color.white;
                }
            }

            if (iconFallbackText != null)
            {
                iconFallbackText.gameObject.SetActive(!hasIcon);
                if (!hasIcon)
                {
                    string source = data != null && !string.IsNullOrEmpty(data.CharacterName)
                        ? data.CharacterName
                        : (pulled != null ? pulled.characterId : "?");
                    iconFallbackText.text = GetInitial(source);
                    iconFallbackText.color = UiTheme.TextSecondary;
                }
            }
        }

        private void ApplyRarityChrome(CharacterData data)
        {
            CharacterRarity rarity = data != null ? data.Rarity : CharacterRarity.SR;
            Color rarityColor = CharacterRarityPalette.GetColor(rarity);

            if (rarityTopBorder != null)
                rarityTopBorder.color = rarityColor;

            bool showGlow = data != null
                && (data.Rarity == CharacterRarity.SSR || data.Rarity == CharacterRarity.LR);

            if (ssrGlow != null)
            {
                ssrGlow.gameObject.SetActive(showGlow);
                if (showGlow)
                {
                    Color glow = rarityColor;
                    glow.a = SsrGlowAlpha;
                    ssrGlow.color = glow;
                }
            }
        }

        private void ApplyName(CharacterData data, PulledCharacter pulled)
        {
            if (nameText == null)
                return;

            if (data != null && !string.IsNullOrEmpty(data.CharacterName))
                nameText.text = data.CharacterName;
            else if (pulled != null)
                nameText.text = pulled.characterId;
            else
                nameText.text = string.Empty;
        }

        private void ApplyStatusChip(PulledCharacter pulled)
        {
            if (pulled == null)
                return;

            Color statusColor = pulled.FormatStatusColor();

            if (statusChipText != null)
            {
                statusChipText.text = pulled.FormatStatusText();
                statusChipText.color = statusColor;
            }

            if (statusChipFrame != null)
            {
                Color frame = statusColor;
                frame.a = StatusChipFrameAlpha;
                statusChipFrame.color = frame;
            }
        }

        private void ApplyRateUp(PulledCharacter pulled)
        {
            if (rateUpBadge != null)
                rateUpBadge.SetActive(pulled != null && pulled.isRateUp);
        }

        private void WireButton()
        {
            if (cardButton == null)
                return;

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(OnCardClicked);
        }

        private void OnCardClicked()
        {
            if (_pulled != null)
                _onClicked?.Invoke(_pulled);
        }

        private static string GetInitial(string source)
        {
            if (string.IsNullOrEmpty(source))
                return "?";

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (!char.IsWhiteSpace(c))
                    return char.ToUpperInvariant(c).ToString();
            }

            return "?";
        }
    }
}
