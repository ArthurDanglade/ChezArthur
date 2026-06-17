using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Carte UI d'un slot de la Gare (instanciée depuis prefab).
    /// </summary>
    public class GareSlotCard : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références UI")]
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _badgeText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private GameObject _soldOverlay;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private GareSlotData _slot;
        private int _slotIndex;
        private Action<int> _onBuy;
        private bool _canAfford = true;

        /// <summary> Index du slot dans la liste GareManager. </summary>
        public int SlotIndex => _slotIndex;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Configure la carte avec les données d'un slot Gare.
        /// </summary>
        public void Setup(GareSlotData slot, int index, Action<int> onBuy)
        {
            _slot = slot;
            _slotIndex = index;
            _onBuy = onBuy;

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(OnBuyButtonClicked);
            }

            if (slot == null)
            {
                ClearDisplay();
                return;
            }

            switch (slot.SlotType)
            {
                case GareSlotType.NewValise:
                    SetupNewValise(slot);
                    break;
                case GareSlotType.ValiseUpgrade:
                    SetupValiseUpgrade(slot);
                    break;
                case GareSlotType.Item:
                    SetupItem(slot);
                    break;
                case GareSlotType.HealSmall:
                    SetupHealSmall();
                    break;
                case GareSlotType.HealMedium:
                case GareSlotType.HealLarge:
                    SetupGenericHeal(slot.SlotType);
                    break;
                default:
                    ClearDisplay();
                    break;
            }

            if (_costText != null)
                _costText.text = slot.Cost.ToString();

            if (_soldOverlay != null)
                _soldOverlay.SetActive(slot.IsPurchased);

            if (_buyButton != null)
                _buyButton.interactable = !slot.IsPurchased && _canAfford;
        }

        /// <summary>
        /// Active ou désactive le bouton d'achat selon les Tals disponibles.
        /// </summary>
        public void SetAffordable(bool can)
        {
            _canAfford = can;
            if (_buyButton == null || _slot == null)
                return;

            _buyButton.interactable = can && !_slot.IsPurchased;
        }

        /// <summary>
        /// Marque visuellement le slot comme vendu.
        /// </summary>
        public void MarkSold()
        {
            if (_soldOverlay != null)
                _soldOverlay.SetActive(true);

            if (_buyButton != null)
                _buyButton.interactable = false;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnBuyButtonClicked()
        {
            _onBuy?.Invoke(_slotIndex);
        }

        private void SetupNewValise(GareSlotData slot)
        {
            ValiseData data = slot.ValiseData;
            if (data == null)
            {
                ClearDisplay();
                return;
            }

            if (_nameText != null)
                _nameText.text = data.ValiseName;

            if (_badgeText != null)
                _badgeText.text = "NOUVELLE VALISE";

            SetIcon(data.Icon);
        }

        private void SetupValiseUpgrade(GareSlotData slot)
        {
            ValiseData data = slot.ValiseData;
            if (data == null)
            {
                ClearDisplay();
                return;
            }

            if (_nameText != null)
                _nameText.text = data.ValiseName;

            if (_badgeText != null)
                _badgeText.text = GetRarityLabel(slot.UpgradeRarity);

            SetIcon(data.Icon);
        }

        private void SetupItem(GareSlotData slot)
        {
            ItemData data = slot.ItemData;
            if (data == null)
            {
                ClearDisplay();
                return;
            }

            if (_nameText != null)
                _nameText.text = data.ItemName;

            if (_badgeText != null)
                _badgeText.text = "ITEM";

            SetIcon(data.Icon);
        }

        private void SetupHealSmall()
        {
            if (_nameText != null)
                _nameText.text = "Soin d'équipe";

            if (_badgeText != null)
                _badgeText.text = "SOIN 10%";

            SetIcon(null);
        }

        private void SetupGenericHeal(GareSlotType healType)
        {
            if (_nameText != null)
                _nameText.text = "Soin d'équipe";

            if (_badgeText != null)
                _badgeText.text = healType.ToString().ToUpperInvariant();

            SetIcon(null);
        }

        private void SetIcon(Sprite icon)
        {
            if (_icon == null)
                return;

            _icon.enabled = icon != null;
            if (icon != null)
                _icon.sprite = icon;
        }

        private void ClearDisplay()
        {
            if (_nameText != null)
                _nameText.text = string.Empty;

            if (_badgeText != null)
                _badgeText.text = string.Empty;

            if (_costText != null)
                _costText.text = string.Empty;

            SetIcon(null);

            if (_soldOverlay != null)
                _soldOverlay.SetActive(false);
        }

        private static string GetRarityLabel(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Commune => "COMMUNE",
            ValiseImprovementRarity.Rare => "RARE",
            ValiseImprovementRarity.Epique => "ÉPIQUE",
            ValiseImprovementRarity.Legendaire => "LÉGENDAIRE",
            _ => string.Empty
        };
    }
}
