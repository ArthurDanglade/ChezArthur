using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Gère l'écran de sélection de slot à sacrifier (valise ou item).
    /// </summary>
    public class SacrificeUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private List<SacrificeSlotUI> sacrificeSlots = new List<SacrificeSlotUI>();

        [Header("Bonus entrant")]
        [SerializeField] private GameObject incomingContainer;
        [SerializeField] private TextMeshProUGUI incomingNameText;
        [SerializeField] private TextMeshProUGUI incomingValueText;
        [SerializeField] private TextMeshProUGUI incomingRarityText;
        [SerializeField] private Image incomingBadgeBackground;

        [Header("Comparaison — sections")]
        [SerializeField] private GameObject comparisonContainer;
        [SerializeField] private TextMeshProUGUI sacrificeHeader;
        [SerializeField] private TextMeshProUGUI gainHeader;
        [SerializeField] private StatLineUI[] loseRows;
        [SerializeField] private StatLineUI[] gainRows;
        [SerializeField] private TextMeshProUGUI rarityQualifier;
        [SerializeField] private TextMeshProUGUI confirmHintText;

        [Header("Couleurs comparaison")]
        [SerializeField] private Color loseColor = new Color(0.85f, 0.24f, 0.24f);
        [SerializeField] private Color gainColor = new Color(0.16f, 0.84f, 0.47f);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private ValiseData _incomingValise;
        private ValiseImprovementRarity _incomingValiseRarity;
        private ItemData _incomingItem;
        private bool _isValiseSacrifice;
        private int _highlightedSlotIndex = -1;
        private readonly List<ComparisonLine> _loseBuffer = new List<ComparisonLine>(4);
        private readonly List<ComparisonLine> _gainBuffer = new List<ComparisonLine>(4);

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            for (int i = 0; i < sacrificeSlots.Count; i++)
            {
                SacrificeSlotUI slot = sacrificeSlots[i];
                if (slot == null) continue;
                slot.OnSlotHighlighted += OnSlotHighlighted;
                slot.OnSlotSelected += OnSlotSelected;
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < sacrificeSlots.Count; i++)
            {
                SacrificeSlotUI slot = sacrificeSlots[i];
                if (slot == null) continue;
                slot.OnSlotHighlighted -= OnSlotHighlighted;
                slot.OnSlotSelected -= OnSlotSelected;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche l'écran de sacrifice pour une valise entrante.
        /// </summary>
        public void ShowForValise(ValiseData incoming, ValiseImprovementRarity rarity)
        {
            _incomingValise = incoming;
            _incomingValiseRarity = rarity;
            _incomingItem = null;
            _isValiseSacrifice = true;
            _highlightedSlotIndex = -1;

            if (titleText != null)
                titleText.text = "Quelle valise remplacer ?";

            if (incomingContainer != null)
                incomingContainer.SetActive(true);

            if (incoming != null)
            {
                if (incomingNameText != null) incomingNameText.text = incoming.ValiseName;
                if (incomingValueText != null)
                {
                    ValiseInstance memorized = ValiseManager.Instance != null
                        ? ValiseManager.Instance.GetMemorizedValise(incoming.Id) : null;
                    int startLevel = memorized != null ? memorized.CurrentLevel : 0;
                    incomingValueText.text = startLevel > 0 ? $"Niv. {startLevel} → {startLevel + 1}" : "Niv. 1";
                }
            }
            else
            {
                if (incomingNameText != null) incomingNameText.text = "";
                if (incomingValueText != null) incomingValueText.text = "";
            }

            if (comparisonContainer != null)
                comparisonContainer.SetActive(false);

            ResetAllSlotSelections();

            IReadOnlyList<ValiseInstance> activeValises = ValiseManager.Instance != null
                ? ValiseManager.Instance.GetActiveSlots()
                : null;

            for (int i = 0; i < sacrificeSlots.Count; i++)
            {
                SacrificeSlotUI slot = sacrificeSlots[i];
                if (slot == null) continue;

                if (activeValises != null && i < activeValises.Count)
                {
                    slot.SetupValise(i, activeValises[i]);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            if (panelRoot != null)
                panelRoot.SetActive(true);

            GameManager.Instance?.ChangeState(GameState.Paused);
        }

        /// <summary>
        /// Affiche l'écran de sacrifice pour un item entrant.
        /// </summary>
        public void ShowForItem(ItemData incoming)
        {
            _incomingItem = incoming;
            _incomingValise = null;
            _isValiseSacrifice = false;
            _highlightedSlotIndex = -1;

            if (titleText != null)
                titleText.text = "Quel item remplacer ?";

            if (incomingContainer != null)
                incomingContainer.SetActive(true);

            if (incoming != null)
            {
                if (incomingNameText != null)
                    incomingNameText.text = incoming.ItemName;
                if (incomingValueText != null)
                    incomingValueText.text = incoming.GetFormattedDescription();
                if (incomingRarityText != null)
                    incomingRarityText.text = incoming.IsDownsideItem ? "RISQUÉ" : "ITEM";
                if (incomingBadgeBackground != null)
                    incomingBadgeBackground.color = incoming.IsDownsideItem
                        ? loseColor
                        : new Color(0.56f, 0.33f, 0.94f);
            }
            else
            {
                if (incomingNameText != null) incomingNameText.text = "";
                if (incomingValueText != null) incomingValueText.text = "";
                if (incomingRarityText != null) incomingRarityText.text = "";
            }

            if (comparisonContainer != null)
                comparisonContainer.SetActive(false);

            ResetAllSlotSelections();

            IReadOnlyList<ItemInstance> activeItems = ItemManager.Instance != null
                ? ItemManager.Instance.GetActiveSlots()
                : null;

            for (int i = 0; i < sacrificeSlots.Count; i++)
            {
                SacrificeSlotUI slot = sacrificeSlots[i];
                if (slot == null) continue;

                if (activeItems != null && i < activeItems.Count)
                {
                    slot.SetupItem(i, activeItems[i]);
                    slot.gameObject.SetActive(true);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            if (panelRoot != null)
                panelRoot.SetActive(true);

            GameManager.Instance?.ChangeState(GameState.Paused);
        }

        /// <summary>
        /// Cache l'écran de sacrifice.
        /// </summary>
        public void Hide()
        {
            GameManager.Instance?.ChangeState(GameState.Playing);

            _highlightedSlotIndex = -1;

            if (comparisonContainer != null)
                comparisonContainer.SetActive(false);

            ResetAllSlotSelections();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Premier tap sur un slot : surligner et afficher la comparaison perdu / gagné.
        /// </summary>
        private void OnSlotHighlighted(int slotIndex)
        {
            if (slotIndex < 0) return;

            ResetAllSlotSelections();

            if (slotIndex < sacrificeSlots.Count && sacrificeSlots[slotIndex] != null)
                sacrificeSlots[slotIndex].SetSelected(true);

            _highlightedSlotIndex = slotIndex;
            ShowComparison(slotIndex);
        }

        private void OnSlotSelected(int slotIndex)
        {
            if (slotIndex < 0) return;

            if (_isValiseSacrifice)
            {
                if (ValiseManager.Instance != null && _incomingValise != null)
                    ValiseManager.Instance.ConfirmSacrifice(slotIndex, _incomingValise, _incomingValiseRarity);
            }
            else
            {
                if (ItemManager.Instance != null && _incomingItem != null)
                    ItemManager.Instance.ConfirmSacrifice(slotIndex, _incomingItem);
            }

            _highlightedSlotIndex = -1;
            if (comparisonContainer != null)
                comparisonContainer.SetActive(false);
            ResetAllSlotSelections();

            Hide();
        }

        /// <summary>
        /// Met à jour la zone de comparaison pour le slot mis en avant.
        /// </summary>
        private void ShowComparison(int slotIndex)
        {
            if (comparisonContainer != null) comparisonContainer.SetActive(true);
            if (confirmHintText != null) confirmHintText.text = "Appuie à nouveau pour confirmer";

            if (_isValiseSacrifice)
            {
                if (_incomingValise == null || ValiseManager.Instance == null) return;
                var slots = ValiseManager.Instance.GetActiveSlots();
                if (slots == null || slotIndex < 0 || slotIndex >= slots.Count) return;
                ValiseInstance sacrificed = slots[slotIndex];
                if (sacrificed == null || sacrificed.Data == null) return;

                SacrificeComparisonBuilder.BuildSacrificedLines(sacrificed, _loseBuffer);
                if (sacrificeHeader != null) sacrificeHeader.text = $"Tu sacrifies — {sacrificed.Data.ValiseName}";
                ApplyLines(loseRows, _loseBuffer, true, gainColor);

                SacrificeComparisonBuilder.BuildIncomingLines(_incomingValise, _incomingValiseRarity, _gainBuffer);
                if (gainHeader != null) gainHeader.text = $"Tu reçois — {_incomingValise.ValiseName}";
                Color gainGood = GetGainColor(_incomingValiseRarity);
                ApplyLines(gainRows, _gainBuffer, false, gainGood);

                if (rarityQualifier != null)
                {
                    bool show = _incomingValiseRarity != ValiseImprovementRarity.Commune;
                    rarityQualifier.gameObject.SetActive(show);
                    if (show)
                    {
                        rarityQualifier.text = $"amélioration {GetRarityLabel(_incomingValiseRarity)}";
                        rarityQualifier.color = gainGood;
                    }
                }
            }
            else
            {
                if (_incomingItem == null || ItemManager.Instance == null) return;
                var slots = ItemManager.Instance.GetActiveSlots();
                if (slots == null || slotIndex < 0 || slotIndex >= slots.Count) return;
                ItemInstance sacrificed = slots[slotIndex];
                if (sacrificed == null || sacrificed.Data == null) return;

                if (sacrificeHeader != null) sacrificeHeader.text = $"Tu sacrifies — {sacrificed.Data.ItemName}";
                HideAllRows(loseRows);
                if (loseRows != null && loseRows.Length > 0 && loseRows[0] != null)
                    loseRows[0].ShowEffect(sacrificed.Data.GetFormattedDescription(), loseColor);

                if (gainHeader != null) gainHeader.text = $"Tu reçois — {_incomingItem.ItemName}";
                HideAllRows(gainRows);
                if (gainRows != null && gainRows.Length > 0 && gainRows[0] != null)
                    gainRows[0].ShowEffect(_incomingItem.GetFormattedDescription(), gainColor);

                if (rarityQualifier != null) rarityQualifier.gameObject.SetActive(false);
            }
        }

        /// <summary> Applique les lignes aux rows. Couleur/signe via polarité : bon = signe +, mauvais = signe −. </summary>
        private void ApplyLines(StatLineUI[] rows, List<ComparisonLine> lines, bool isSacrifice, Color goodColor)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == null) continue;
                if (i >= lines.Count) { rows[i].Hide(); continue; }

                ComparisonLine line = lines[i];
                bool isGood = (isSacrifice == line.IsCost);
                Color color = isGood ? goodColor : loseColor;

                if (line.IsEffectLine)
                {
                    rows[i].ShowEffect(line.Text, color);
                }
                else
                {
                    string sign = isGood ? "+" : "−";
                    rows[i].ShowStat(line.Text, FormatValue(line.Magnitude, line.IsPercentage, sign), color);
                }
            }
        }

        private static void HideAllRows(StatLineUI[] rows)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null) rows[i].Hide();
        }

        private static string FormatValue(float magnitude, bool isPercentage, string sign)
        {
            float display = isPercentage ? magnitude * 100f : magnitude;
            return $"{sign}{display.ToString("0.#")}{(isPercentage ? " %" : "")}";
        }

        /// <summary> Couleur du gain : vert pour Commune, couleur de rareté au-delà. </summary>
        private Color GetGainColor(ValiseImprovementRarity rarity)
            => rarity == ValiseImprovementRarity.Commune ? gainColor : GetValiseRarityBadgeColor(rarity);

        private static string GetRarityLabel(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Commune => "commune",
            ValiseImprovementRarity.Rare => "rare",
            ValiseImprovementRarity.Epique => "épique",
            ValiseImprovementRarity.Legendaire => "légendaire",
            _ => ""
        };

        private void ResetAllSlotSelections()
        {
            for (int i = 0; i < sacrificeSlots.Count; i++)
            {
                if (sacrificeSlots[i] != null)
                    sacrificeSlots[i].ResetSelection();
            }
        }

        /// <summary>
        /// Couleur du badge selon la rareté d'amélioration de la valise entrante.
        /// </summary>
        private static Color GetValiseRarityBadgeColor(ValiseImprovementRarity rarity)
        {
            switch (rarity)
            {
                case ValiseImprovementRarity.Commune:
                    return Color.gray;
                case ValiseImprovementRarity.Rare:
                    return Color.blue;
                case ValiseImprovementRarity.Epique:
                    return new Color(0.55f, 0.25f, 0.75f);
                case ValiseImprovementRarity.Legendaire:
                    return new Color(1f, 0.84f, 0f);
                default:
                    return Color.gray;
            }
        }
    }
}
