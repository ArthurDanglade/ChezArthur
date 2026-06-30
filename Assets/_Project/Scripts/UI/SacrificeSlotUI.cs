using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
        // Anciennement « descriptionText » → devient la valeur à droite (FormerlySerializedAs garde le lien Inspector).
        [FormerlySerializedAs("descriptionText")]
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private TextMeshProUGUI descLineText;   // ligne de description sous le header
        [SerializeField] private Image iconImage;
        [SerializeField] private Image rarityAccent; // accent coloré par la dernière rareté de la valise
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button selectButton;
        [SerializeField] private Transform detailSlot;

        [Header("Sélection")]
        [SerializeField] private Image selectionOutline;
        [SerializeField] private Color neutralColor = new Color(0.15f, 0.16f, 0.21f);   // fond neutre par défaut
        [SerializeField] private Color selectedColor = new Color(0.50f, 0.16f, 0.16f);  // rouge « tu vas sacrifier »

        [Header("Couleurs valeur")]
        [SerializeField] private Color valueColor = new Color(0.85f, 0.88f, 0.90f);     // valeur chiffrée
        [SerializeField] private Color variableColor = new Color(0.94f, 0.89f, 0.66f);  // badge « variable »

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
        /// <summary> Tap sur la carte : surbrillance + prévisualisation de la comparaison. </summary>
        public event Action<int> OnSlotHighlighted;

        /// <summary> Conteneur où le code injecte la comparaison + le bouton quand la carte est choisie. </summary>
        public Transform DetailSlot => detailSlot;

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
        /// Met à jour l'état visuel de sélection. La ligne de description se masque quand la carte
        /// est choisie : la comparaison prend sa place dans le DetailSlot.
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (backgroundImage != null)
                backgroundImage.color = selected ? selectedColor : neutralColor;
            // Pastille Gate 5a : le liseré OR s'allume avec la sélection.
            if (selectionOutline != null)
                selectionOutline.enabled = selected;
        }

        /// <summary> Réinitialise la sélection locale. </summary>
        public void ResetSelection()
        {
            _isSelected = false;
            SetSelected(false);
        }

        /// <summary> Configure l'affichage pour un slot de valise. </summary>
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

            // Valeur à droite + description dessous, selon le type de valise.
            SacrificeComparisonBuilder.BuildSacrificedLines(instance, _summaryBuffer);
            ApplyValueAndDescription(instance.Data.ComparisonMode);

            if (iconImage != null)
            {
                iconImage.enabled = instance.Data.Icon != null;
                if (instance.Data.Icon != null)
                    iconImage.sprite = instance.Data.Icon;
            }
            if (backgroundImage != null)
                backgroundImage.color = neutralColor;

            if (rarityAccent != null)
            {
                rarityAccent.enabled = true;
                rarityAccent.color = ValiseRarityPalette.Color(instance.LastImprovementRarity);
            }

            // Pastille Gate 5a : valeur et description ne sont jamais montrées dans le slot.
            if (valueText != null) valueText.gameObject.SetActive(false);
            if (descLineText != null) descLineText.gameObject.SetActive(false);
        }

        /// <summary> Configure l'affichage pour un slot d'item. </summary>
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

            // Item : pas de valeur chiffrée à droite, la description occupe la ligne dessous.
            if (valueText != null)
                valueText.text = "";
            SetDescLine(instance.Data.GetFormattedDescription());

            if (iconImage != null)
            {
                iconImage.enabled = instance.Data.Icon != null;
                if (instance.Data.Icon != null)
                    iconImage.sprite = instance.Data.Icon;
            }
            if (backgroundImage != null)
                backgroundImage.color = neutralColor;

            if (rarityAccent != null)
                rarityAccent.enabled = false; // pas de rareté de valise pour un item

            // Pastille Gate 5a : valeur et description ne sont jamais montrées dans le slot.
            if (valueText != null) valueText.gameObject.SetActive(false);
            if (descLineText != null) descLineText.gameObject.SetActive(false);
        }
        // ═══════════════════════════════════════════

        private void OnSelectClicked()
        {
            if (_slotIndex < 0) return;
            // La confirmation passe par le bouton explicite ; un tap ne fait que surligner.
            OnSlotHighlighted?.Invoke(_slotIndex);
        }

        /// <summary>
        /// Répartit la valeur (à droite) et la description (dessous) selon le type de valise.
        /// Conditionnelle (EffectLine) → badge « variable » + phrase ; à stat → valeur chiffrée + secondaires.
        /// </summary>
        private void ApplyValueAndDescription(ValiseComparisonMode mode)
        {
            if (mode == ValiseComparisonMode.EffectLine && _summaryBuffer.Count > 0)
            {
                if (valueText != null)
                {
                    valueText.text = "variable";
                    valueText.color = variableColor;
                }
                SetDescLine(_summaryBuffer[0].Text);
                return;
            }

            if (_summaryBuffer.Count > 0)
            {
                if (valueText != null)
                {
                    valueText.text = SacrificeComparisonBuilder.FormatLine(_summaryBuffer[0]);
                    valueText.color = valueColor;
                }

                if (_summaryBuffer.Count > 1)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 1; i < _summaryBuffer.Count; i++)
                    {
                        if (i > 1) sb.Append("   ");
                        sb.Append(SacrificeComparisonBuilder.FormatLine(_summaryBuffer[i]));
                    }
                    SetDescLine(sb.ToString());
                }
                else
                {
                    SetDescLine("");
                }
                return;
            }

            if (valueText != null) valueText.text = "";
            SetDescLine("");
        }

        /// <summary> Affiche la ligne de description (ou la masque si vide / carte sélectionnée). </summary>
        private void SetDescLine(string text)
        {
            if (descLineText == null) return;
            descLineText.text = text;
            descLineText.gameObject.SetActive(!string.IsNullOrEmpty(text) && !_isSelected);
        }

        private void ClearDisplay()
        {
            ResetSelection();
            if (nameText != null) nameText.text = "";
            if (valueText != null) valueText.text = "";
            SetDescLine("");
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
