#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère le prefab EndRunCharacterEntryUI (carte arrondie + wiring des 7 champs sérialisés).
    /// </summary>
    public static class EndRunEntryPrefabBuilder
    {
        private const string RateUpPrefabName = "RateUpCharacterEntry";
        private const string OutputPrefabName = "EndRunCharacterEntryUI.prefab";
        private const float EntryPreferredHeight = 300f;
        private const float PortraitSize = 176f;

        [MenuItem("Chez Arthur/UI/Générer prefab EndRunCharacterEntry")]
        public static void Generate()
        {
            TryBuildPrefab(overwrite: false);
        }

        [MenuItem("Chez Arthur/UI/Regénérer prefab EndRunCharacterEntry (écrase)")]
        public static void Regenerate()
        {
            TryBuildPrefab(overwrite: true);
        }

        private static void TryBuildPrefab(bool overwrite)
        {
            string rateUpPath = FindRateUpPrefabPath();
            if (string.IsNullOrEmpty(rateUpPath))
            {
                Debug.LogError(
                    $"[EndRunEntryPrefabBuilder] Prefab {RateUpPrefabName} introuvable — impossible de déduire le dossier cible.");
                return;
            }

            string outputFolder = Path.GetDirectoryName(rateUpPath)?.Replace('\\', '/');
            string outputPath = $"{outputFolder}/{OutputPrefabName}";
            Debug.Log($"[EndRunEntryPrefabBuilder] Dossier cible : {outputFolder}");

            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
            {
                Debug.LogWarning(
                    $"[EndRunEntryPrefabBuilder] ABANDON — le prefab existe déjà : {outputPath}. " +
                    "Utilisez « Regénérer prefab EndRunCharacterEntry (écrase) ».");
                return;
            }

            if (overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null)
                AssetDatabase.DeleteAsset(outputPath);

            GameObject root = BuildPrefabRoot();
            EndRunCharacterEntryUI entry = root.GetComponent<EndRunCharacterEntryUI>();
            SerializedObject so = new SerializedObject(entry);
            int wiredCount = CountWiredReferences(so);
            if (wiredCount != 7)
            {
                Object.DestroyImmediate(root);
                Debug.LogError(
                    $"[EndRunEntryPrefabBuilder] Échec wiring : {wiredCount}/7 références — prefab non créé.");
                return;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[EndRunEntryPrefabBuilder] Prefab {(overwrite ? "regénéré" : "créé")} : {outputPath} — 7/7 références wirées.");
            if (prefab != null)
                EditorGUIUtility.PingObject(prefab);
        }

        private static GameObject BuildPrefabRoot()
        {
            Sprite cardSprite = UiGen.Card;
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            TMP_FontAsset font = UiGen.LoadFont();

            GameObject root = new GameObject("EndRunCharacterEntryUI", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.sizeDelta = new Vector2(0f, EntryPreferredHeight);

            LayoutElement rootLe = root.AddComponent<LayoutElement>();
            rootLe.minHeight = EntryPreferredHeight;
            rootLe.preferredHeight = EntryPreferredHeight;
            rootLe.flexibleWidth = 1f;

            Image cardBg = CreateImage(
                "CardBackground",
                rootRt,
                cardSprite,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            cardBg.type = Image.Type.Sliced;
            cardBg.color = UiTheme.Surface;
            StretchFull(cardBg.rectTransform);

            RectTransform contentRt = CreateRect(
                "Content",
                rootRt,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            StretchFull(contentRt);
            contentRt.offsetMin = new Vector2(16f, 14f);
            contentRt.offsetMax = new Vector2(-16f, -14f);

            HorizontalLayoutGroup row = contentRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 20f;
            row.padding = new RectOffset(0, 0, 0, 0);
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            RectTransform portraitColumn = CreateLayoutChild(
                "PortraitColumn",
                contentRt,
                preferredWidth: PortraitSize,
                preferredHeight: PortraitSize,
                flexibleWidth: 0f);

            Image rarityBorderImg = CreateImage(
                "RarityBorder",
                portraitColumn,
                uiSprite,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            StretchFull(rarityBorderImg.rectTransform);
            rarityBorderImg.color = UiTheme.Frame;

            Image iconImg = CreateImage(
                "Icon",
                rarityBorderImg.rectTransform,
                uiSprite,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(PortraitSize - 16f, PortraitSize - 16f));
            iconImg.preserveAspect = true;

            RectTransform infoColumn = CreateLayoutChild(
                "InfoColumn",
                contentRt,
                preferredWidth: 0f,
                preferredHeight: EntryPreferredHeight - 28f,
                flexibleWidth: 1f);

            VerticalLayoutGroup infoVlg = infoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            infoVlg.spacing = 8f;
            infoVlg.childAlignment = TextAnchor.UpperLeft;
            infoVlg.childControlWidth = true;
            infoVlg.childControlHeight = true;
            infoVlg.childForceExpandWidth = true;
            infoVlg.childForceExpandHeight = false;

            TextMeshProUGUI rankTmp = CreateTextChild(
                infoColumn,
                font,
                "RankText",
                "1er",
                40f,
                FontStyles.Bold,
                TextAlignmentOptions.TopLeft,
                false,
                44f,
                UiTheme.TextPrimary);

            TextMeshProUGUI quoteTmp = CreateTextChild(
                infoColumn,
                font,
                "QuoteText",
                "« Réplique de fin de run »",
                28f,
                FontStyles.Italic,
                TextAlignmentOptions.TopLeft,
                true,
                88f,
                UiTheme.TextSecondary);
            quoteTmp.overflowMode = TextOverflowModes.Ellipsis;
            quoteTmp.maxVisibleLines = 3;

            RectTransform statsBlockRt = CreateLayoutChild(
                "StatsBlock",
                infoColumn,
                preferredWidth: 0f,
                preferredHeight: 120f,
                flexibleWidth: 1f);

            VerticalLayoutGroup statsVlg = statsBlockRt.gameObject.AddComponent<VerticalLayoutGroup>();
            statsVlg.spacing = 6f;
            statsVlg.childAlignment = TextAnchor.UpperLeft;
            statsVlg.childControlWidth = true;
            statsVlg.childControlHeight = true;
            statsVlg.childForceExpandWidth = true;
            statsVlg.childForceExpandHeight = false;

            TextMeshProUGUI damageDealtTmp = CreateStatLine(statsBlockRt, font, "DamageDealtText", "Dégâts infligés : 0");
            TextMeshProUGUI damageTakenTmp = CreateStatLine(statsBlockRt, font, "DamageTakenText", "Dégâts encaissés : 0");
            TextMeshProUGUI healingTmp = CreateStatLine(statsBlockRt, font, "HealingText", "Soins appliqués : 0");

            EndRunCharacterEntryUI entry = root.AddComponent<EndRunCharacterEntryUI>();
            SerializedObject so = new SerializedObject(entry);
            Wire(so, "iconImage", iconImg);
            Wire(so, "rarityBorder", rarityBorderImg);
            Wire(so, "rankText", rankTmp);
            Wire(so, "quoteText", quoteTmp);
            Wire(so, "damageDealtText", damageDealtTmp);
            Wire(so, "damageTakenText", damageTakenTmp);
            Wire(so, "healingText", healingTmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static string FindRateUpPrefabPath()
        {
            string[] guids = AssetDatabase.FindAssets($"{RateUpPrefabName} t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (path.EndsWith($"{RateUpPrefabName}.prefab"))
                    return path;
            }

            return null;
        }

        private static void Wire(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogError($"[EndRunEntryPrefabBuilder] Propriété introuvable : {propertyName}");
                return;
            }

            prop.objectReferenceValue = value;
        }

        private static int CountWiredReferences(SerializedObject so)
        {
            string[] fields =
            {
                "iconImage",
                "rarityBorder",
                "rankText",
                "quoteText",
                "damageDealtText",
                "damageTakenText",
                "healingText"
            };

            int count = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                SerializedProperty prop = so.FindProperty(fields[i]);
                if (prop != null && prop.objectReferenceValue != null)
                    count++;
            }

            return count;
        }

        private static RectTransform CreateLayoutChild(
            string name,
            RectTransform parent,
            float preferredWidth,
            float preferredHeight,
            float flexibleWidth)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = preferredHeight;
            le.preferredHeight = preferredHeight;
            le.preferredWidth = preferredWidth;
            le.flexibleWidth = flexibleWidth;
            return rt;
        }

        private static TextMeshProUGUI CreateTextChild(
            RectTransform parent,
            TMP_FontAsset font,
            string name,
            string text,
            float fontSize,
            FontStyles fontStyle,
            TextAlignmentOptions alignment,
            bool wrap,
            float preferredHeight,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = preferredHeight;
            le.preferredHeight = preferredHeight;
            le.flexibleWidth = 1f;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.enableWordWrapping = wrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Image CreateImage(
            string name,
            RectTransform parent,
            Sprite sprite,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform CreateRect(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        private static TextMeshProUGUI CreateStatLine(RectTransform parent, TMP_FontAsset font, string name, string text)
        {
            return CreateTextChild(
                parent,
                font,
                name,
                text,
                26f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft,
                false,
                32f,
                UiTheme.TextMuted);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
