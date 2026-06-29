using System;
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
        [SerializeField] private Button confirmButton;

        [Header("Bonus entrant")]
        [SerializeField] private GameObject incomingContainer;
        [SerializeField] private TextMeshProUGUI incomingNameText;
        [SerializeField] private TextMeshProUGUI incomingValueText;
        [SerializeField] private TextMeshProUGUI incomingRarityText;
        [SerializeField] private Image incomingBadgeBackground;
        [SerializeField] private Image incomingCardBackground;

        [Header("Comparaison — sections")]
        [SerializeField] private GameObject comparisonContainer;
        [SerializeField] private TextMeshProUGUI sacrificeHeader;
        [SerializeField] private TextMeshProUGUI gainHeader;
        [SerializeField] private TextMeshProUGUI loseEffectText;
        [SerializeField] private TextMeshProUGUI loseValueText;
        [SerializeField] private TextMeshProUGUI gainEffectText;
        [SerializeField] private TextMeshProUGUI gainValueText;
        [SerializeField] private Color neutralEffectColor = new Color(0.88f, 0.90f, 0.92f);
        [SerializeField] private StatLineUI[] loseRows;
        [SerializeField] private StatLineUI[] gainRows;
        [SerializeField] private TextMeshProUGUI rarityQualifier;
        [SerializeField] private TextMeshProUGUI confirmHintText;

        [Header("Comparaison — colonnes")]
        [SerializeField] private Image loseIcon;
        [SerializeField] private Image loseRarityFrame;
        [SerializeField] private TextMeshProUGUI loseNameText;
        [SerializeField] private TextMeshProUGUI loseLevelText;
        [SerializeField] private Image gainIcon;
        [SerializeField] private Image gainRarityFrame;
        [SerializeField] private TextMeshProUGUI gainNameText;
        [SerializeField] private TextMeshProUGUI gainLevelText;

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
        private GameState _previousState;
        private readonly List<ComparisonLine> _loseBuffer = new List<ComparisonLine>(4);
        private readonly List<ComparisonLine> _gainBuffer = new List<ComparisonLine>(4);
        private Transform _comparisonOriginalParent;
        private int _comparisonOriginalSiblingIndex;

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
            }

            if (comparisonContainer != null)
            {
                _comparisonOriginalParent = comparisonContainer.transform.parent;
                _comparisonOriginalSiblingIndex = comparisonContainer.transform.GetSiblingIndex();
            }
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < sacrificeSlots.Count; i++)
            {
                SacrificeSlotUI slot = sacrificeSlots[i];
                if (slot == null) continue;
                slot.OnSlotHighlighted -= OnSlotHighlighted;
            }

            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
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
                if (incomingBadgeBackground != null)
                    incomingBadgeBackground.color = gainColor;
                if (incomingCardBackground != null)
                    incomingCardBackground.color = new Color(0.086f, 0.188f, 0.102f); // vert sombre carte entrante
                if (incomingRarityText != null)
                {
                    incomingRarityText.gameObject.SetActive(true);
                    incomingRarityText.text = "VALISE";
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
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            if (GameManager.Instance != null)
                _previousState = GameManager.Instance.CurrentState;

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
                if (incomingCardBackground != null)
                    incomingCardBackground.color = new Color(0.14f, 0.15f, 0.20f); // neutre sombre pour les items
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
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            if (GameManager.Instance != null)
                _previousState = GameManager.Instance.CurrentState;

            GameManager.Instance?.ChangeState(GameState.Paused);
        }

        /// <summary> Déclenché après confirmation d'un sacrifice (avant fermeture du panneau). </summary>
        public event Action OnSacrificeConfirmed;

        /// <summary>
        /// Cache l'écran de sacrifice.
        /// </summary>
        public void Hide()
        {
            if (GameManager.Instance != null)
            {
                // Le flux bonus ferme et repasse en Playing avant la fin du sacrifice :
                // ne pas réappliquer un Paused obsolète capturé à l'ouverture.
                if (_previousState == GameState.Paused && GameManager.Instance.CurrentState == GameState.Playing)
                {
                    // Garde Playing.
                }
                else
                {
                    GameManager.Instance.ChangeState(_previousState);
                }
            }

            _highlightedSlotIndex = -1;

            if (comparisonContainer != null)
                comparisonContainer.SetActive(false);

            if (comparisonContainer != null && _comparisonOriginalParent != null)
            {
                comparisonContainer.transform.SetParent(_comparisonOriginalParent, false);
                comparisonContainer.transform.SetSiblingIndex(_comparisonOriginalSiblingIndex);
            }

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

        private void ConfirmSacrificeForSlot(int slotIndex)
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

            OnSacrificeConfirmed?.Invoke();
            Hide();
        }

        private void OnConfirmClicked()
        {
            if (_highlightedSlotIndex >= 0)
                ConfirmSacrificeForSlot(_highlightedSlotIndex);
        }

        /// <summary>
        /// Met à jour la zone de comparaison pour le slot mis en avant.
        /// </summary>
        private void ShowComparison(int slotIndex)
        {
            if (comparisonContainer != null && slotIndex >= 0 && slotIndex < sacrificeSlots.Count
                && sacrificeSlots[slotIndex] != null && sacrificeSlots[slotIndex].DetailSlot != null)
            {
                comparisonContainer.transform.SetParent(sacrificeSlots[slotIndex].DetailSlot, false);
                comparisonContainer.transform.SetAsLastSibling();
            }

            ConfigureComparisonLayout();

            if (comparisonContainer != null) comparisonContainer.SetActive(true);
            if (confirmHintText != null)
                confirmHintText.gameObject.SetActive(false);
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(true);

            if (sacrificeHeader != null)
            {
                sacrificeHeader.text = "Tu perds :";
                sacrificeHeader.color = loseColor;
            }
            if (gainHeader != null)
            {
                gainHeader.text = "Tu gagnes :";
                gainHeader.color = gainColor;
            }

            // Les textes effet/valeur vivent dans loseRows[0] / gainRows[0] : ne masquer que les lignes secondaires.
            HideSecondaryRows(loseRows);
            HideSecondaryRows(gainRows);
            ShowPrimaryComparisonRows();

            if (_isValiseSacrifice)
            {
                if (_incomingValise == null || ValiseManager.Instance == null) return;
                var slots = ValiseManager.Instance.GetActiveSlots();
                if (slots == null || slotIndex < 0 || slotIndex >= slots.Count) return;
                ValiseInstance sacrificed = slots[slotIndex];
                if (sacrificed == null || sacrificed.Data == null) return;

                if (sacrificeHeader != null) sacrificeHeader.text = "Tu perds";
                if (gainHeader != null) gainHeader.text = "Tu gagnes";
                FillColumn(loseIcon, loseRarityFrame, loseNameText, loseLevelText,
                    sacrificed.Data.Icon, sacrificed.LastImprovementRarity,
                    sacrificed.Data.ValiseName, sacrificed.CurrentLevel, true);
                // Valise entrante = nouvelle (le sacrifice ne survient que pour une nouvelle valise) → niveau 1
                FillColumn(gainIcon, gainRarityFrame, gainNameText, gainLevelText,
                    _incomingValise.Icon, _incomingValiseRarity,
                    _incomingValise.ValiseName, 1, true);

                SacrificeComparisonBuilder.BuildSacrificedLines(sacrificed, _loseBuffer);
                SetEffectAndValue(loseEffectText, loseValueText, sacrificed.Data, _loseBuffer, "Actuellement : ", loseColor);

                SacrificeComparisonBuilder.BuildIncomingLines(_incomingValise, _incomingValiseRarity, _gainBuffer);
                Color gainGood = GetGainColor(_incomingValiseRarity);
                SetEffectAndValue(gainEffectText, gainValueText, _incomingValise, _gainBuffer, "À ce niveau : ", gainGood);

                if (rarityQualifier != null)
                {
                    bool show = _incomingValiseRarity != ValiseImprovementRarity.Commune;
                    rarityQualifier.gameObject.SetActive(show);
                    if (show) { rarityQualifier.text = $"amélioration {GetRarityLabel(_incomingValiseRarity)}"; rarityQualifier.color = gainGood; }
                }
            }
            else
            {
                if (_incomingItem == null || ItemManager.Instance == null) return;
                var islots = ItemManager.Instance.GetActiveSlots();
                if (islots == null || slotIndex < 0 || slotIndex >= islots.Count) return;
                ItemInstance sacrificed = islots[slotIndex];
                if (sacrificed == null || sacrificed.Data == null) return;

                if (sacrificeHeader != null) sacrificeHeader.text = "Tu perds";
                if (gainHeader != null) gainHeader.text = "Tu gagnes";
                FillColumn(loseIcon, loseRarityFrame, loseNameText, loseLevelText,
                    sacrificed.Data.Icon, default, sacrificed.Data.ItemName, 0, false);
                FillColumn(gainIcon, gainRarityFrame, gainNameText, gainLevelText,
                    _incomingItem.Icon, default, _incomingItem.ItemName, 0, false);

                if (loseEffectText != null) { loseEffectText.text = $"{sacrificed.Data.ItemName} — {sacrificed.Data.GetFormattedDescription()}"; loseEffectText.color = neutralEffectColor; }
                if (loseValueText != null) loseValueText.gameObject.SetActive(false);
                if (gainEffectText != null) { gainEffectText.text = $"{_incomingItem.ItemName} — {_incomingItem.GetFormattedDescription()}"; gainEffectText.color = neutralEffectColor; }
                if (gainValueText != null) gainValueText.gameObject.SetActive(false);
                if (rarityQualifier != null) rarityQualifier.gameObject.SetActive(false);
            }

            RebuildComparisonLayout(slotIndex);
        }

        /// <summary> Remplit une colonne icône / cadre rareté / nom / niveau de la comparaison. </summary>
        private void FillColumn(Image icon, Image rarityFrame, TextMeshProUGUI nameText,
            TextMeshProUGUI levelText, Sprite sprite, ValiseImprovementRarity rarity,
            string displayName, int level, bool isValise)
        {
            if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
            if (rarityFrame != null)
            {
                rarityFrame.enabled = isValise;
                if (isValise) rarityFrame.color = ValiseRarityPalette.Color(rarity);
            }
            if (nameText != null) nameText.text = displayName;
            if (levelText != null)
            {
                levelText.gameObject.SetActive(isValise);
                if (isValise) levelText.text = "Niv. " + level;
            }
        }

        /// <summary>
        /// Normalise ancres / Layout Groups du ComparisonContainer (évite le chevauchement des TMP).
        /// </summary>
        private void ConfigureComparisonLayout()
        {
            if (comparisonContainer == null) return;

            if (comparisonContainer.transform is RectTransform comparisonRect)
                ApplyVerticalLayoutChild(comparisonRect);

            ConfigureVerticalLayoutGroup(comparisonContainer, spacing: 36f, paddingHorizontal: 16, paddingVertical: 20);
            EnsureVerticalContentSizeFitter(comparisonContainer);

            if (sacrificeHeader != null)
            {
                ConfigureSectionHeader(sacrificeHeader);
                ConfigureSection(sacrificeHeader.transform.parent);
            }

            if (gainHeader != null)
            {
                ConfigureSectionHeader(gainHeader);
                ConfigureSection(gainHeader.transform.parent);
            }

            ConfigureComparisonRow(loseRows);
            ConfigureComparisonRow(gainRows);
            ConfigureComparisonText(loseEffectText, isValueLine: false);
            ConfigureComparisonText(loseValueText, isValueLine: true);
            ConfigureComparisonText(gainEffectText, isValueLine: false);
            ConfigureComparisonText(gainValueText, isValueLine: true);
            ConfigureComparisonText(rarityQualifier, isValueLine: true);

            if (confirmButton != null && confirmButton.transform is RectTransform confirmRect)
            {
                ApplyVerticalLayoutChild(confirmRect);
                EnsureLayoutElement(confirmRect, minHeight: 60f, bottomMargin: 24f);
            }
        }

        private static void ConfigureSection(Transform section)
        {
            if (section == null) return;

            if (section is RectTransform sectionRect)
                ApplyVerticalLayoutChild(sectionRect);

            ConfigureVerticalLayoutGroup(section.gameObject, spacing: 20f, paddingHorizontal: 4, paddingVertical: 8);
            EnsureVerticalContentSizeFitter(section.gameObject);

            for (int i = 0; i < section.childCount; i++)
            {
                if (section.GetChild(i) is RectTransform childRect)
                    ApplyVerticalLayoutChild(childRect);
            }
        }

        private static void ConfigureSectionHeader(TextMeshProUGUI header)
        {
            if (header == null) return;
            ApplyVerticalLayoutChild(header.rectTransform);
            ApplyVerticalLayoutChild(header.rectTransform);
            EnsureLayoutElement(header.rectTransform, minHeight: 48f, bottomMargin: 16f);
            header.enableWordWrapping = false;
            header.alignment = TextAlignmentOptions.MidlineLeft;
            header.lineSpacing = 4f;
            header.paragraphSpacing = 8f;
        }

        private static void ConfigureComparisonRow(StatLineUI[] rows)
        {
            if (rows == null || rows.Length == 0 || rows[0] == null) return;

            StatLineUI row = rows[0];
            if (row.transform is RectTransform rowRect)
                ApplyVerticalLayoutChild(rowRect);

            ConfigureVerticalLayoutGroup(row.gameObject, spacing: 18f, paddingHorizontal: 0, paddingVertical: 6);
            EnsureVerticalContentSizeFitter(row.gameObject);

            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
                ConfigureComparisonText(texts[i], isValueLine: i > 0);
        }

        private static void ConfigureComparisonText(TextMeshProUGUI text, bool isValueLine)
        {
            if (text == null) return;

            ApplyVerticalLayoutChild(text.rectTransform);
            EnsureLayoutElement(text.rectTransform, minHeight: isValueLine ? 40f : 48f, bottomMargin: isValueLine ? 18f : 22f);
            EnsureTextContentSizeFitter(text);

            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.lineSpacing = 10f;
            text.paragraphSpacing = 14f;
            text.margin = new Vector4(0f, 8f, 0f, isValueLine ? 16f : 20f);
        }

        private static void EnsureTextContentSizeFitter(TextMeshProUGUI text)
        {
            ContentSizeFitter csf = text.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = text.gameObject.AddComponent<ContentSizeFitter>();

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ApplyVerticalLayoutChild(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void ConfigureVerticalLayoutGroup(GameObject target, float spacing, int paddingHorizontal, int paddingVertical)
        {
            if (target == null) return;

            VerticalLayoutGroup vlg = target.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) return;

            vlg.spacing = spacing;
            vlg.padding = new RectOffset(paddingHorizontal, paddingHorizontal, paddingVertical, paddingVertical);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
        }

        private static void EnsureVerticalContentSizeFitter(GameObject target)
        {
            if (target == null) return;

            ContentSizeFitter csf = target.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = target.AddComponent<ContentSizeFitter>();

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void EnsureLayoutElement(RectTransform rect, float minHeight, float bottomMargin = 0f)
        {
            if (rect == null) return;

            LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = rect.gameObject.AddComponent<LayoutElement>();

            layoutElement.minHeight = minHeight + bottomMargin;
        }

        /// <summary> Force le recalcul des Layout Groups après reparentage du ComparisonContainer. </summary>
        private void RebuildComparisonLayout(int slotIndex)
        {
            Canvas.ForceUpdateCanvases();

            if (comparisonContainer != null && comparisonContainer.transform is RectTransform comparisonRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(comparisonRect);

            if (slotIndex < 0 || slotIndex >= sacrificeSlots.Count || sacrificeSlots[slotIndex] == null)
                return;

            if (sacrificeSlots[slotIndex].DetailSlot is RectTransform detailRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(detailRect);

            if (sacrificeSlots[slotIndex].transform is RectTransform slotRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slotRect);

                if (slotRect.parent is RectTransform gridRect)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);
            }
        }

        private void SetEffectAndValue(TextMeshProUGUI effectText, TextMeshProUGUI valueText, ValiseData data, List<ComparisonLine> buffer, string valuePrefix, Color valueColor)
        {
            if (effectText != null)
            {
                string desc = data.GetFormattedDescription();
                effectText.text = string.IsNullOrEmpty(desc) ? data.ValiseName : $"{data.ValiseName} — {desc}";
                effectText.color = neutralEffectColor;
            }
            if (valueText != null)
            {
                valueText.gameObject.SetActive(true);
                bool isEffect = buffer.Count == 1 && buffer[0].IsEffectLine;
                string val = BuildValueString(buffer);
                valueText.text = isEffect ? val : valuePrefix + val;
                valueText.color = valueColor;
            }
        }

        private static string BuildValueString(List<ComparisonLine> buffer)
        {
            if (buffer == null || buffer.Count == 0) return "";
            if (buffer.Count == 1 && buffer[0].IsEffectLine) return buffer[0].Text;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i > 0) sb.Append("   ·   ");
                sb.Append(SacrificeComparisonBuilder.FormatLine(buffer[i]));
            }
            return sb.ToString();
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

        /// <summary> Masque les lignes secondaires (index 1+) ; la ligne 0 porte loseEffectText / gainEffectText. </summary>
        private static void HideSecondaryRows(StatLineUI[] rows)
        {
            if (rows == null) return;
            for (int i = 1; i < rows.Length; i++)
            {
                if (rows[i] != null) rows[i].Hide();
            }
        }

        /// <summary> Réactive la première ligne de comparaison (effet + valeur). </summary>
        private void ShowPrimaryComparisonRows()
        {
            if (loseRows != null && loseRows.Length > 0 && loseRows[0] != null)
                loseRows[0].gameObject.SetActive(true);
            if (gainRows != null && gainRows.Length > 0 && gainRows[0] != null)
                gainRows[0].gameObject.SetActive(true);
            if (loseValueText != null) loseValueText.gameObject.SetActive(true);
            if (gainValueText != null) gainValueText.gameObject.SetActive(true);
        }

        private static string FormatValue(float magnitude, bool isPercentage, string sign)
            => SacrificeComparisonBuilder.FormatMagnitude(magnitude, isPercentage, sign);

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
