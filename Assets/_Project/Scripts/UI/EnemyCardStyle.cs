using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Typographie et espacements de la fiche ennemi (mobile portrait).
    /// </summary>
    public static class EnemyCardStyle
    {
        public const float TopOffsetY = -120f;
        public const float HorizontalMargin = 0.04f;

        public const int PanelPaddingLeft = 36;
        public const int PanelPaddingRight = 36;
        public const int PanelPaddingTop = 28;
        public const int PanelPaddingBottom = 36;
        public const float PanelSectionSpacing = 22f;

        public const float HeaderHeight = 220f;
        public const float SpriteFrameSize = 200f;
        public const float HeaderSpacing = 20f;
        public const float HeaderInfoSpacing = 14f;

        public const float NameFontSize = 44f;
        public const float TypeFontSize = 26f;
        public const float TypeBadgeHeight = 44f;
        public const float TypeBadgeWidth = 180f;

        public const float DescriptionFontSize = 30f;
        public const float DescriptionLineSpacing = 10f;
        public const float DescriptionSectionSpacing = 10f;

        public const float StatLabelFontSize = 24f;
        public const float StatValueFontSize = 36f;
        public const float StatsRowHeight = 100f;
        public const float StatsBlockSpacing = 8f;
        public const float StatsRowSpacing = 12f;

        public const float PassiveTitleFontSize = 24f;
        public const float PassiveBodyFontSize = 28f;
        public const float PassiveSectionSpacing = 12f;
        public const float PassiveLineSpacing = 8f;

        public const float BodyTextHorizontalMargin = 6f;

        public static void Apply(
            GameObject panelRoot,
            TextMeshProUGUI nameText,
            TextMeshProUGUI typeText,
            TextMeshProUGUI descriptionText,
            TextMeshProUGUI hpText,
            TextMeshProUGUI atkText,
            TextMeshProUGUI defText,
            TextMeshProUGUI spdText,
            TextMeshProUGUI passiveTitleText,
            TextMeshProUGUI passivesText,
            GameObject descriptionBlock,
            GameObject passiveBlock,
            Image spriteFrame)
        {
            if (panelRoot != null)
            {
                RectTransform panelRt = panelRoot.transform as RectTransform;
                if (panelRt != null)
                {
                    panelRt.anchorMin = new Vector2(HorizontalMargin, 1f);
                    panelRt.anchorMax = new Vector2(1f - HorizontalMargin, 1f);
                    panelRt.anchoredPosition = new Vector2(0f, TopOffsetY);
                }

                VerticalLayoutGroup panelVlg = panelRoot.GetComponent<VerticalLayoutGroup>();
                if (panelVlg != null)
                {
                    panelVlg.padding = new RectOffset(
                        PanelPaddingLeft,
                        PanelPaddingRight,
                        PanelPaddingTop,
                        PanelPaddingBottom);
                    panelVlg.spacing = PanelSectionSpacing;
                }
            }

            Transform header = panelRoot != null ? panelRoot.transform.Find("Header") : null;
            if (header != null)
            {
                LayoutElement headerLe = header.GetComponent<LayoutElement>();
                if (headerLe != null)
                {
                    headerLe.minHeight = HeaderHeight;
                    headerLe.preferredHeight = HeaderHeight;
                }

                HorizontalLayoutGroup headerHlg = header.GetComponent<HorizontalLayoutGroup>();
                if (headerHlg != null)
                    headerHlg.spacing = HeaderSpacing;

                Transform spriteFrameT = header.Find("SpriteFrame");
                if (spriteFrameT != null)
                {
                    LayoutElement frameLe = spriteFrameT.GetComponent<LayoutElement>();
                    if (frameLe != null)
                    {
                        frameLe.minWidth = SpriteFrameSize;
                        frameLe.preferredWidth = SpriteFrameSize;
                        frameLe.minHeight = SpriteFrameSize;
                        frameLe.preferredHeight = SpriteFrameSize;
                    }
                }

                Transform headerInfo = header.Find("HeaderInfo");
                if (headerInfo != null)
                {
                    VerticalLayoutGroup infoVlg = headerInfo.GetComponent<VerticalLayoutGroup>();
                    if (infoVlg != null)
                        infoVlg.spacing = HeaderInfoSpacing;

                    Transform typeBadge = headerInfo.Find("TypeBadge");
                    if (typeBadge != null)
                    {
                        LayoutElement badgeLe = typeBadge.GetComponent<LayoutElement>();
                        if (badgeLe != null)
                        {
                            badgeLe.preferredWidth = TypeBadgeWidth;
                            badgeLe.minHeight = TypeBadgeHeight;
                            badgeLe.preferredHeight = TypeBadgeHeight;
                        }
                    }
                }
            }

            ApplyNameText(nameText);
            ApplyTypeText(typeText);
            ApplyBodyText(descriptionText, DescriptionFontSize, DescriptionLineSpacing);
            ApplyPassiveTitle(passiveTitleText);
            ApplyBodyText(passivesText, PassiveBodyFontSize, PassiveLineSpacing);

            ApplyStatValue(hpText);
            ApplyStatValue(atkText);
            ApplyStatValue(defText);
            ApplyStatValue(spdText);
            ApplyStatLabels(panelRoot != null ? panelRoot.transform : null);

            EnsureSectionSpacing(descriptionBlock, DescriptionSectionSpacing);
            EnsureSectionSpacing(passiveBlock, PassiveSectionSpacing);

            Transform statsRow = panelRoot != null ? panelRoot.transform.Find("StatsRow") : null;
            if (statsRow != null)
            {
                LayoutElement rowLe = statsRow.GetComponent<LayoutElement>();
                if (rowLe != null)
                {
                    rowLe.minHeight = StatsRowHeight;
                    rowLe.preferredHeight = StatsRowHeight;
                }

                HorizontalLayoutGroup statsHlg = statsRow.GetComponent<HorizontalLayoutGroup>();
                if (statsHlg != null)
                    statsHlg.spacing = StatsRowSpacing;
            }

            if (spriteFrame != null)
            {
                Transform spriteImageT = spriteFrame.transform.Find("SpriteImage");
                if (spriteImageT is RectTransform spriteImageRt)
                {
                    spriteImageRt.offsetMin = new Vector2(12f, 12f);
                    spriteImageRt.offsetMax = new Vector2(-12f, -12f);
                }
            }
        }

        private static void ApplyNameText(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            tmp.fontSize = NameFontSize;
            tmp.lineSpacing = 0f;
            tmp.margin = Vector4.zero;
        }

        private static void ApplyTypeText(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            tmp.fontSize = TypeFontSize;
            tmp.lineSpacing = 0f;
        }

        private static void ApplyPassiveTitle(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            tmp.fontSize = PassiveTitleFontSize;
            tmp.lineSpacing = 0f;
            tmp.characterSpacing = 4f;
            if (string.Equals(tmp.text, "PASSIF"))
                tmp.text = "PASSIFS";
        }

        private static void ApplyBodyText(TextMeshProUGUI tmp, float fontSize, float lineSpacing)
        {
            if (tmp == null)
                return;

            tmp.fontSize = fontSize;
            tmp.lineSpacing = lineSpacing;
            tmp.margin = new Vector4(BodyTextHorizontalMargin, 0f, BodyTextHorizontalMargin, 0f);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
        }

        private static void ApplyStatValue(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            tmp.fontSize = StatValueFontSize;
            tmp.lineSpacing = 0f;
        }

        private static void ApplyStatLabels(Transform panelRoot)
        {
            if (panelRoot == null)
                return;

            Transform statsRow = panelRoot.Find("StatsRow");
            if (statsRow == null)
                return;

            for (int i = 0; i < statsRow.childCount; i++)
            {
                Transform block = statsRow.GetChild(i);
                Transform label = block.Find($"StatLabel_{block.name.Replace("Stat_", string.Empty)}");
                if (label == null)
                    continue;

                TextMeshProUGUI labelTmp = label.GetComponent<TextMeshProUGUI>();
                if (labelTmp == null)
                    continue;

                labelTmp.fontSize = StatLabelFontSize;

                VerticalLayoutGroup blockVlg = block.GetComponent<VerticalLayoutGroup>();
                if (blockVlg != null)
                    blockVlg.spacing = StatsBlockSpacing;
            }
        }

        private static void EnsureSectionSpacing(GameObject section, float spacing)
        {
            if (section == null)
                return;

            VerticalLayoutGroup vlg = section.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
                vlg.spacing = spacing;
        }
    }
}
