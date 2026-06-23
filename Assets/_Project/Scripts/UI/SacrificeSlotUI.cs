using System;
using System.Collections.Generic;
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
        [SerializeField] private Color neutralColor = new Color(0.15f, 0.16f, 0.21f);   // fond neutre par défaut
        [SerializeField] private Color selectedColor = new Color(0.50f, 0.16f, 0.16f);  // rouge « tu vas sacrifier »

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _slotIndex = -1;
        private bool _isValise;
        private bool _isSelected;
        private readonly List<ComparisonLine> _summaryBuffer = new List<ComparisonLine>(4);

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
            if (backgroundImage != null)
                backgroundImage.color = selected ? selectedColor : neutralColor;
            if (selectionOutline != null)
                selectionOutline.enabled = false; // l'emphase passe par la couleur de fond, pas par un overlay couvrant
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
                descriptionText.text = SacrificeComparisonBuilder.BuildSacrificedSummary(instance, _summaryBuffer);
            if (iconImage != null)
            {
                iconImage.enabled = instance.Data.Icon != null;
                if (instance.Data.Icon != null)
                    iconImage.sprite = instance.Data.Icon;
            }
            if (backgroundImage != null)
                backgroundImage.color = neutralColor;
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
                backgroundImage.color = neutralColor;
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
            if (backgroundImage != null) backgroundImage.color = neutralColor;
        }
    }
}
