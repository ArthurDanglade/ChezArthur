using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Carte UI représentant un bonus sélectionnable.
    /// </summary>
    public class BonusCard : MonoBehaviour
    {
        [Header("Références UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button selectButton;

        [Header("Couleurs par rareté")]
        [SerializeField] private Color commonColor = Color.gray;
        [SerializeField] private Color uncommonColor = Color.green;
        [SerializeField] private Color rareColor = Color.blue;
        [SerializeField] private Color epicColor = new Color(0.5f, 0f, 0.5f);
        [SerializeField] private Color specialColor = Color.yellow;

        private BonusData _bonusData;

        /// <summary> Déclenché quand le joueur clique sur la carte. </summary>
        public event Action<BonusData> OnCardSelected;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);
        }

        /// <summary>
        /// Configure la carte avec les données du bonus.
        /// </summary>
        public void Setup(BonusData bonus)
        {
            _bonusData = bonus;

            if (bonus == null)
            {
                if (nameText != null) nameText.text = "";
                if (rarityText != null) rarityText.text = "";
                if (descriptionText != null) descriptionText.text = "";
                if (iconImage != null) iconImage.enabled = false;
                if (backgroundImage != null) backgroundImage.color = commonColor;
                return;
            }

            if (nameText != null)
                nameText.text = bonus.BonusName;

            if (rarityText != null)
                rarityText.text = bonus.IsSpecialBonus ? bonus.SpecialRarity.ToString() : bonus.StatRarity.ToString();

            if (descriptionText != null)
                descriptionText.text = bonus.GetFormattedDescription();

            if (iconImage != null)
            {
                iconImage.enabled = bonus.Icon != null;
                if (bonus.Icon != null)
                    iconImage.sprite = bonus.Icon;
            }

            if (backgroundImage != null)
                backgroundImage.color = GetColorForBonus(bonus);
        }

        private void OnSelectClicked()
        {
            OnCardSelected?.Invoke(_bonusData);
        }

        private Color GetColorForBonus(BonusData bonus)
        {
            if (bonus.IsSpecialBonus)
                return specialColor;
            switch (bonus.StatRarity)
            {
                case BonusRarity.Common: return commonColor;
                case BonusRarity.Uncommon: return uncommonColor;
                case BonusRarity.Rare: return rareColor;
                case BonusRarity.Epic: return epicColor;
                default: return commonColor;
            }
        }
    }
}
