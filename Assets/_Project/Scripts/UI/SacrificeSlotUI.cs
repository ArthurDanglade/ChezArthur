using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Représente une carte de slot sélectionnable dans l'écran de sacrifice.
    /// </summary>
    public class SacrificeSlotUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références UI")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button selectButton;

        [Header("Sélection")]
        [SerializeField] private Image selectionOutline;
        [SerializeField] private Color selectedColor = new Color(0.94f, 0.78f, 0.18f);
        [SerializeField] private Color defaultColor = new Color(0.20f, 0.45f, 0.95f);

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly Color DEFAULT_ITEM_COLOR = Color.gray;
        private static readonly Color LEVEL_LOW_COLOR = new Color(0.20f, 0.45f, 0.95f);
        private static readonly Color LEVEL_MID_COLOR = new Color(0.55f, 0.25f, 0.75f);
        private static readonly Color LEVEL_HIGH_COLOR = new Color(0.95f, 0.75f, 0.25f);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _slotIndex = -1;
        private bool _isValise;
        private bool _isSelected;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Premier tap sur la carte : surbrillance + prévisualisation de la comparaison. </summary>
        public event Action<int> OnSlotHighlighted;

        /// <summary> Second tap sur la même carte : confirmation du sacrifice. </summary>
        public event Action<int> OnSlotSelected;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectClicked);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(OnSelectClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour l'état visuel de sélection (surlignage).
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (selectionOutline != null)
            {
                selectionOutline.enabled = selected;
                selectionOutline.color = selected ? selectedColor : Color.clear;
            }
        }

        /// <summary>
        /// Réinitialise la sélection locale (tap suivant = premier tap).
        /// </summary>
        public void ResetSelection()
        {
            _isSelected = false;
            SetSelected(false);
        }

        /// <summary>
        /// Configure l'affichage pour un slot de valise.
        /// </summary>
        public void SetupValise(int slotIndex, ValiseInstance instance)
        {
            ResetSelection();
            _slotIndex = slotIndex;
            _isValise = true;

            if (instance == null || instance.Data == null)
            {
                ClearDisplay();
                return;
            }

            if (nameText != null)
                nameText.text = instance.Data.ValiseName;
            if (levelText != null)
            {
                levelText.gameObject.SetActive(true);
                levelText.text = $"Niv. {instance.CurrentLevel}";
            }
            if (descriptionText != null)
                descriptionText.text = instance.Data.GetFormattedDescription();
            if (iconImage != null)
            {
                iconImage.enabled = instance.Data.Icon != null;
                if (instance.Data.Icon != null)
                    iconImage.sprite = instance.Data.Icon;
            }
            if (backgroundImage != null)
                backgroundImage.color = GetValiseColor(instance.CurrentLevel);
        }

        /// <summary>
        /// Configure l'affichage pour un slot d'item.
        /// </summary>
        public void SetupItem(int slotIndex, ItemInstance instance)
        {
            ResetSelection();
            _slotIndex = slotIndex;
            _isValise = false;

            if (instance == null || instance.Data == null)
            {
                ClearDisplay();
                return;
            }

            if (nameText != null)
                nameText.text = instance.Data.ItemName;
            if (levelText != null)
            {
                levelText.text = "";
                levelText.gameObject.SetActive(false);
            }
            if (descriptionText != null)
                descriptionText.text = instance.Data.GetFormattedDescription();
            if (iconImage != null)
            {
                iconImage.enabled = instance.Data.Icon != null;
                if (instance.Data.Icon != null)
                    iconImage.sprite = instance.Data.Icon;
            }
            if (backgroundImage != null)
                backgroundImage.color = DEFAULT_ITEM_COLOR;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnSelectClicked()
        {
            if (_slotIndex < 0) return;

            // Premier tap : surligner et prévisualiser ; second tap sur la même carte : confirmer.
            if (_isSelected)
                OnSlotSelected?.Invoke(_slotIndex);
            else
                OnSlotHighlighted?.Invoke(_slotIndex);
        }

        private void ClearDisplay()
        {
            ResetSelection();
            if (nameText != null) nameText.text = "";
            if (descriptionText != null) descriptionText.text = "";
            if (levelText != null)
            {
                levelText.text = "";
                levelText.gameObject.SetActive(_isValise);
            }
            if (iconImage != null) iconImage.enabled = false;
            if (backgroundImage != null) backgroundImage.color = DEFAULT_ITEM_COLOR;
        }

        private static Color GetValiseColor(int level)
        {
            if (level >= 20) return LEVEL_HIGH_COLOR;
            if (level >= 10) return LEVEL_MID_COLOR;
            return LEVEL_LOW_COLOR;
        }
    }
}
