#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub.Pages;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 5.c.1 — polish fiche : header allégé, stats colorées, shine off, panneau plus bas.
    /// </summary>
    public static class DetailPopupPolishBuilder
    {
        private const string UndoLabel = "Detail Popup Polish 5.c.1";
        private const string PopupGuid = "4a21464fbb824924ab517e38b180d7a1";
        private const string CardPrefabPath = "Assets/_Project/Prefabs/UI/CharacterCard.prefab";
        private const float PanelClosedHeight = 270f;
        private const float HeaderHeight = 110f;
        private const float StatsRowHeight = 112f;
        private const float StatsRowFromTop = -118f;
        private const float NameLeftPad = 24f;

        [MenuItem("Chez Arthur/Refonte Hub/Detail Popup — Polish 5.c.1 (DRY RUN)")]
        public static void DryRun() => Run(false);

        [MenuItem("Chez Arthur/Refonte Hub/Detail Popup — Polish 5.c.1 (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Detail Popup Polish 5.c.1",
                    "Header (back/nom, purge chip+type), stats colorées + Nv dans la rangée, "
                    + "panneau 270, shine OFF.\n\nCtrl+S ensuite.",
                    "Appliquer",
                    "Annuler"))
                return;
            Run(true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(8192);
            int todo = 0, conforme = 0, failed = 0;
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" DetailPopupPolishBuilder 5.c.1 — {(apply ? "APPLIQUER" : "DRY RUN")}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            string popupPath = AssetDatabase.GUIDToAssetPath(PopupGuid);
            if (string.IsNullOrEmpty(popupPath))
            {
                failed++;
                log.AppendLine("- ✗ Prefab popup introuvable");
                Finish(log, todo, conforme, failed);
                return;
            }

            if (!apply)
            {
                todo += 4;
                log.AppendLine("- [DRY] Header + purge RarityChip/TypeText — À FAIRE");
                log.AppendLine("- [DRY] Rebuild StatsRow (Nv + PV/ATK/DEF/VIT colorés) — À FAIRE");
                log.AppendLine("- [DRY] panelClosedHeight=270 + hold inset — À FAIRE");
                log.AppendLine("- [DRY] Shine OFF popup + carte — À FAIRE");
                Finish(log, todo, conforme, failed);
                return;
            }

            Sprite pill = RoundedRectSpriteGenerator.LoadSpriteS();
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;

            GameObject root = PrefabUtility.LoadPrefabContents(popupPath);
            try
            {
                CharacterDetailPopup popup = root.GetComponent<CharacterDetailPopup>();
                if (popup == null)
                {
                    failed++;
                    log.AppendLine("- ✗ CharacterDetailPopup manquant");
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                    PolishPrefab(root, popup, pill, font, log, ref conforme);
                    PrefabUtility.SaveAsPrefabAsset(root, popupPath);
                    conforme++;
                    log.AppendLine("- Prefab sauvegardé ✓");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            // Shine off carte
            GameObject card = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                RarityShineFX[] shines = card.GetComponentsInChildren<RarityShineFX>(true);
                for (int i = 0; i < shines.Length; i++)
                    Object.DestroyImmediate(shines[i]);
                PrefabUtility.SaveAsPrefabAsset(card, CardPrefabPath);
                conforme++;
                log.AppendLine($"- RarityShineFX retiré carte ({shines.Length}) ✓");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(card);
            }

            Finish(log, todo, conforme, failed);
        }

        private static void PolishPrefab(
            GameObject root,
            CharacterDetailPopup popup,
            Sprite pill,
            TMP_FontAsset font,
            StringBuilder log,
            ref int conforme)
        {
            Transform tRoot = root.transform;
            Transform header = FindDeep(tRoot, "Header");
            Transform statsPanel = FindDeep(tRoot, "StatsPanel");

            // Header height + name offset
            if (header != null)
            {
                RectTransform hrt = (RectTransform)header;
                hrt.sizeDelta = new Vector2(hrt.sizeDelta.x, HeaderHeight);

                Transform name = header.Find("NameText");
                // BR1 : NameText vit dans StatsPanel (titre encart) — ne pas le reposer ici.
                if (name != null && name.parent == header)
                {
                    RectTransform nrt = (RectTransform)name;
                    nrt.anchoredPosition = new Vector2(NameLeftPad, -28f);
                    nrt.sizeDelta = new Vector2(520f, 56f);
                }

                DisableGo(header, "RarityChip", log, ref conforme, "RarityChip masqué");
                DisableGo(header, "TypeText", log, ref conforme, "TypeText masqué");
                DisableGo(header, "LevelText", log, ref conforme, "LevelText header masqué (déplacé stats)");

                Transform badge = header.Find("InTeamBadge");
                if (badge != null)
                {
                    badge.gameObject.SetActive(false);
                    conforme++;
                    log.AppendLine("- InTeamBadge off (BR1) ✓");
                }

                conforme++;
                log.AppendLine("- Header allégé (H=110, sans pastille équipe) ✓");
            }

            // Rebuild StatsRow
            TextMeshProUGUI levelValue = null;
            TextMeshProUGUI hpValue = null, atkValue = null, defValue = null, speedValue = null;
            if (statsPanel != null)
            {
                RectTransform spRt = (RectTransform)statsPanel;
                spRt.sizeDelta = new Vector2(spRt.sizeDelta.x, PanelClosedHeight);

                Transform oldRow = statsPanel.Find("StatsRow");
                if (oldRow != null)
                    Object.DestroyImmediate(oldRow.gameObject);

                GameObject row = new GameObject(
                    "StatsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(statsPanel, false);
                // Sous TabBar
                Transform tabBar = statsPanel.Find("TabBar");
                if (tabBar != null)
                    row.transform.SetSiblingIndex(tabBar.GetSiblingIndex() + 1);

                RectTransform rowRt = (RectTransform)row.transform;
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.anchoredPosition = new Vector2(0f, StatsRowFromTop);
                rowRt.sizeDelta = new Vector2(-(UiTheme.PadCard * 2f), StatsRowHeight);

                HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childForceExpandHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.padding = new RectOffset(4, 4, 4, 4);

                levelValue = CreateStatCell(
                    row.transform, "NV", "Nv.—", UiTheme.StatSpeed, pill, font, isLevel: true);
                hpValue = CreateStatCell(
                    row.transform, "PV", "—", UiTheme.StatHp, pill, font, isLevel: false);
                atkValue = CreateStatCell(
                    row.transform, "ATK", "—", UiTheme.StatAtk, pill, font, isLevel: false);
                defValue = CreateStatCell(
                    row.transform, "DEF", "—", UiTheme.StatDef, pill, font, isLevel: false);
                speedValue = CreateStatCell(
                    row.transform, "VIT", "—", UiTheme.StatSpeed, pill, font, isLevel: false);

                conforme++;
                log.AppendLine("- StatsRow rebuild (Nv + 4 cellules colorées) ✓");
            }

            // Hold area inset : bas = panneau, haut = header (Back cliquable)
            Transform hold = tRoot.Find("ArtworkHoldArea");
            if (hold != null)
            {
                RectTransform hrt = (RectTransform)hold;
                hrt.offsetMin = new Vector2(0f, PanelClosedHeight);
                hrt.offsetMax = new Vector2(0f, -120f);
                Transform headerTx = tRoot.Find("Header");
                if (headerTx != null)
                    hold.SetSiblingIndex(Mathf.Max(0, headerTx.GetSiblingIndex()));
            }

            Transform expandedZone = FindDeep(tRoot, "ExpandedZone");
            if (expandedZone != null)
            {
                RectTransform ezRt = (RectTransform)expandedZone;
                ezRt.anchorMin = Vector2.zero;
                ezRt.anchorMax = Vector2.one;
                ezRt.anchoredPosition = Vector2.zero;
                ezRt.sizeDelta = Vector2.zero;
                ezRt.offsetMin = new Vector2(12f, 4f);
                ezRt.offsetMax = new Vector2(-12f, -236f);
                conforme++;
                log.AppendLine("- ExpandedZone flush bas / sous stats ✓");
            }

            // Shine OFF
            RarityShineFX[] shines = root.GetComponentsInChildren<RarityShineFX>(true);
            for (int i = 0; i < shines.Length; i++)
                Object.DestroyImmediate(shines[i]);
            conforme++;
            log.AppendLine($"- RarityShineFX purgé popup ({shines.Length}) ✓");

            // Rebind
            SerializedObject so = new SerializedObject(popup);
            so.FindProperty("panelClosedHeight").floatValue = PanelClosedHeight;
            SetObj(so, "levelText", levelValue);
            SetObj(so, "hpText", hpValue);
            SetObj(so, "atkText", atkValue);
            SetObj(so, "defText", defValue);
            SetObj(so, "speedText", speedValue);
            SetObj(so, "typeText", null);
            SetObj(so, "rarityChipText", null);
            SetObj(so, "rarityChipFrame", null);
            SetObj(so, "artworkShine", null);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(popup);
            conforme++;
            log.AppendLine("- SerializedObject rebind ✓");
            log.AppendLine($"- panelClosedHeight = {PanelClosedHeight} ✓");
        }

        private static TextMeshProUGUI CreateStatCell(
            Transform parent,
            string label,
            string value,
            Color accent,
            Sprite pill,
            TMP_FontAsset font,
            bool isLevel)
        {
            GameObject col = new GameObject(
                label + "Col",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            col.transform.SetParent(parent, false);

            LayoutElement le = col.GetComponent<LayoutElement>();
            le.flexibleWidth = isLevel ? 0.85f : 1f;
            le.minWidth = isLevel ? 72f : 88f;

            Image bg = col.GetComponent<Image>();
            bg.sprite = pill;
            bg.type = Image.Type.Sliced;
            Color fill = accent;
            fill.a = 0.22f;
            bg.color = fill;
            bg.raycastTarget = false;

            // Liseré via Outline simple = 2e Image enfant Border
            GameObject borderGo = new GameObject(
                "Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderGo.transform.SetParent(col.transform, false);
            borderGo.transform.SetAsFirstSibling();
            RectTransform brt = (RectTransform)borderGo.transform;
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            Image border = borderGo.GetComponent<Image>();
            border.sprite = pill;
            border.type = Image.Type.Sliced;
            Color bc = accent;
            bc.a = 0.85f;
            border.color = bc;
            border.raycastTarget = false;
            // Fill inset to show border ring
            // Actually both full = border hidden. Use fill as main, add thin top bar instead.
            Object.DestroyImmediate(borderGo);

            VerticalLayoutGroup vlg = col.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(6, 6, 10, 8);
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            TextMeshProUGUI valueTmp = CreateTmp(
                col.transform, "Value", font, UiTheme.CardFontStatValue, UiTheme.TextPrimary);
            valueTmp.text = value;
            valueTmp.alignment = TextAlignmentOptions.Center;
            LayoutElement vLe = valueTmp.gameObject.AddComponent<LayoutElement>();
            vLe.preferredHeight = 40f;

            TextMeshProUGUI labelTmp = CreateTmp(
                col.transform, "Label", font, UiTheme.CardFontStatLabel, accent);
            labelTmp.text = label;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontStyle = FontStyles.Bold;
            LayoutElement lLe = labelTmp.gameObject.AddComponent<LayoutElement>();
            lLe.preferredHeight = 24f;

            // Accent bar top
            GameObject barGo = new GameObject(
                "AccentBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barGo.transform.SetParent(col.transform, false);
            barGo.transform.SetAsFirstSibling();
            LayoutElement barLe = barGo.AddComponent<LayoutElement>();
            barLe.preferredHeight = 4f;
            barLe.flexibleWidth = 1f;
            Image bar = barGo.GetComponent<Image>();
            bar.color = accent;
            bar.raycastTarget = false;

            return valueTmp;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent, string name, TMP_FontAsset font, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static void DisableGo(
            Transform parent, string childName, StringBuilder log, ref int conforme, string msg)
        {
            Transform t = parent.Find(childName);
            if (t == null)
            {
                // RarityChip may be nested path
                t = FindDeep(parent, childName);
            }

            if (t == null)
                return;

            t.gameObject.SetActive(false);
            conforme++;
            log.AppendLine($"- {msg} ✓");
        }

        private static void SetObj(SerializedObject so, string name, Object value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null)
                p.objectReferenceValue = value;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static void Finish(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine();
            log.AppendLine("───────────────────────────────────────────");
            log.AppendLine($" À FAIRE={todo} | CONFORMES={conforme} | ÉCHECS={failed}");
            log.AppendLine("───────────────────────────────────────────");
            Debug.Log(log.ToString());
        }
    }
}
#endif
