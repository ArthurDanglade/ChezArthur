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

        [Header("Éléments roguelike")]
        [SerializeField] private TextMeshProUGUI badgeText;
        [SerializeField] private GameObject beforeAfterContainer;
        [SerializeField] private TextMeshProUGUI beforeText;
        [SerializeField] private TextMeshProUGUI afterText;
        [SerializeField] private GameObject downsideContainer;
        [SerializeField] private TextMeshProUGUI downsideText;

        [Header("Couleurs badge")]
        [SerializeField] private Color badgeNewColor = new Color(0.16f, 0.84f, 0.47f);
        [SerializeField] private Color badgeUpgradeColor = new Color(0.94f, 0.78f, 0.18f);
        [SerializeField] private Color badgeItemColor = new Color(0.56f, 0.33f, 0.94f);
        [SerializeField] private Color badgeDownsideColor = new Color(0.85f, 0.24f, 0.24f);
        [SerializeField] private Image badgeBackground;

        private BonusData _bonusData;
        private RoguelikeOption _roguelikeOption;

        /// <summary> Déclenché quand le joueur clique sur la carte. </summary>
        public event Action<BonusData> OnCardSelected;
        /// <summary> Déclenché quand le joueur clique sur une option roguelike. </summary>
        public event Action<RoguelikeOption> OnRoguelikeOptionSelected;

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
            _roguelikeOption = null;
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

        /// <summary>
        /// Configure la carte avec une option roguelike (valise/item).
        /// </summary>
        public void SetupRoguelike(RoguelikeOption option)
        {
            _roguelikeOption = null;
            _bonusData = null;
            _roguelikeOption = option;

            // Reset de tous les éléments optionnels roguelike.
            if (beforeAfterContainer != null) beforeAfterContainer.SetActive(false);
            if (downsideContainer != null) downsideContainer.SetActive(false);
            if (badgeText != null) badgeText.text = "";

            if (option == null)
            {
                ClearRoguelikeDisplay();
                return;
            }

            switch (option.Type)
            {
                case RoguelikeOptionType.ValiseNew:
                    SetupNewValise(option);
                    break;
                case RoguelikeOptionType.ValiseUpgrade:
                    SetupUpgradeValise(option);
                    break;
                case RoguelikeOptionType.Item:
                    SetupItem(option);
                    break;
                default:
                    Debug.LogWarning($"[BonusCard] Type d'option roguelike non géré: {option.Type}", this);
                    ClearRoguelikeDisplay();
                    break;
            }

            if (backgroundImage != null)
                backgroundImage.color = GetColorForRoguelikeOption(option);
        }

        private void SetupNewValise(RoguelikeOption option)
        {
            ValiseData data = option.ValiseData;
            if (data == null) return;

            if (nameText != null) nameText.text = data.ValiseName;

            SetBadge("NOUVELLE VALISE", badgeNewColor);

            // Affiche la formule de base de la valise.
            if (descriptionText != null)
                descriptionText.text = data.GetFormattedDescription();

            if (rarityText != null) rarityText.text = "";

            SetIcon(data.Icon);
        }

        private void SetupUpgradeValise(RoguelikeOption option)
        {
            ValiseData data = option.ValiseData;
            if (data == null) return;

            ValiseInstance currentInstance = ValiseManager.Instance != null
                ? ValiseManager.Instance.GetActiveValise(data.Id)
                : null;

            if (nameText != null)
                nameText.text = data.ValiseName;

            string rarityLabel = GetRarityLabel(option.ValiseRarity);
            SetBadge(rarityLabel, badgeUpgradeColor);

            if (currentInstance != null && beforeAfterContainer != null)
            {
                beforeAfterContainer.SetActive(true);

                float currentValue = currentInstance.GetTotalStatValue();
                if (beforeText != null)
                    beforeText.text = FormatStatValue(data.BaseStatType, currentValue);

                // Simule la valeur future sans muter l'instance active.
                int rarityMultiplier = ValiseTypeUtility.GetLevelBonus(option.ValiseRarity);
                float addedValue = data.BaseValuePerLevel * rarityMultiplier;
                float futureValue = currentValue + addedValue;
                if (afterText != null)
                    afterText.text = FormatStatValue(data.BaseStatType, futureValue);
            }

            if (descriptionText != null)
                descriptionText.text = beforeAfterContainer != null && currentInstance != null
                    ? GetStatLabel(data.BaseStatType)
                    : data.GetFormattedDescription();

            SetIcon(data.Icon);
            if (rarityText != null) rarityText.text = "";
        }

        private void SetupItem(RoguelikeOption option)
        {
            ItemData data = option.ItemData;
            if (data == null) return;

            if (nameText != null) nameText.text = data.ItemName;

            if (data.IsDownsideItem)
                SetBadge("RISQUÉ", badgeDownsideColor);
            else
                SetBadge("ITEM", badgeItemColor);

            if (descriptionText != null)
                descriptionText.text = data.GetFormattedDescription();

            if (data.IsDownsideItem && data.HasDownside && downsideContainer != null)
            {
                downsideContainer.SetActive(true);
                if (downsideText != null)
                {
                    string downsideStat = data.DirectDownsideStatType.ToString();
                    float downsideValue = data.DirectDownsideStatValue * 100f;
                    downsideText.text = $"▼ {downsideStat} -{downsideValue:0}%";
                }
            }

            SetIcon(data.Icon);
            if (rarityText != null) rarityText.text = "";
        }

        private void SetBadge(string label, Color color)
        {
            if (badgeText != null) badgeText.text = label;
            if (badgeBackground != null) badgeBackground.color = color;
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null) return;
            iconImage.enabled = icon != null;
            if (icon != null) iconImage.sprite = icon;
        }

        private void ClearRoguelikeDisplay()
        {
            if (nameText != null) nameText.text = "";
            if (rarityText != null) rarityText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (iconImage != null) iconImage.enabled = false;
            if (backgroundImage != null) backgroundImage.color = commonColor;
            if (badgeText != null) badgeText.text = "";
        }

        private string FormatStatValue(ValiseStatType statType, float value)
        {
            // Convertit la valeur brute en pourcentage lisible.
            float percent = value * 100f;
            return $"+{percent:0.#}%";
        }

        private string GetRarityLabel(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Commune => "COMMUNE",
            ValiseImprovementRarity.Rare => "RARE",
            ValiseImprovementRarity.Epique => "ÉPIQUE",
            ValiseImprovementRarity.Legendaire => "LÉGENDAIRE",
            _ => ""
        };

        private string GetStatLabel(ValiseStatType statType) => statType switch
        {
            ValiseStatType.ATK => "Attaque",
            ValiseStatType.DEF => "Défense",
            ValiseStatType.HP => "Points de vie",
            ValiseStatType.Speed => "Vitesse",
            ValiseStatType.LaunchForce => "Force de lancer",
            ValiseStatType.ReboundDecay => "Rebond",
            ValiseStatType.CritChance => "Chance critique",
            ValiseStatType.CritMultiplier => "Multiplicateur critique",
            ValiseStatType.DamageReduction => "Réduction dégâts",
            ValiseStatType.HealingBonus => "Vol de vie",
            ValiseStatType.RegenBetweenStages => "Régénération",
            _ => statType.ToString()
        };

        private void OnSelectClicked()
        {
            if (_roguelikeOption != null)
            {
                OnRoguelikeOptionSelected?.Invoke(_roguelikeOption);
                return;
            }
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

        private Color GetColorForRoguelikeOption(RoguelikeOption option)
        {
            if (option == null) return commonColor;
            if (option.Type == RoguelikeOptionType.Item)
                return specialColor;

            switch (option.ValiseRarity)
            {
                case ValiseImprovementRarity.Commune: return commonColor;
                case ValiseImprovementRarity.Rare: return uncommonColor;
                case ValiseImprovementRarity.Epique: return rareColor;
                case ValiseImprovementRarity.Legendaire: return epicColor;
                default: return commonColor;
            }
        }
    }
}
