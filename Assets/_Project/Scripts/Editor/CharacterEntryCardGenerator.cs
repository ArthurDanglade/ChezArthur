#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère le prefab de carte personnage (CharacterEntryUI), habillé.
    /// Couleurs/sprite/police via UiTheme + UiGen. Menu : Take Five Games > UI > Générer carte perso.
    /// </summary>
    public static class CharacterEntryCardGenerator
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/CharacterEntryCard.prefab";

        [MenuItem("Take Five Games/UI/Générer carte perso (CharacterEntry)")]
        public static void Generate()
        {
            Sprite card = UiGen.Card;
            Sprite knob = UiGen.Knob;

            // ── Racine ──
            GameObject root = NewUI("CharacterEntryCard", null, out RectTransform rootRt);
            Image rootImg = root.AddComponent<Image>();
            rootImg.sprite = card; rootImg.type = Image.Type.Sliced; rootImg.color = UiTheme.Surface;

            Button button = root.AddComponent<Button>();
            button.targetGraphic = rootImg;

            var rootH = root.AddComponent<HorizontalLayoutGroup>();
            rootH.padding = new RectOffset(16, 16, 16, 16);
            rootH.spacing = 16;
            rootH.childAlignment = TextAnchor.MiddleLeft;
            rootH.childControlWidth = true;  rootH.childControlHeight = true;
            rootH.childForceExpandWidth = false; rootH.childForceExpandHeight = false;

            var rootLe = root.AddComponent<LayoutElement>();
            rootLe.minHeight = 150; rootLe.preferredHeight = 150;

            // ── Cadre portrait (= anneau de rareté, coloré au runtime) ──
            GameObject frame = NewUI("PortraitFrame", rootRt, out RectTransform frameRt);
            Image frameImg = frame.AddComponent<Image>();
            frameImg.sprite = card; frameImg.type = Image.Type.Sliced; frameImg.color = UiTheme.Frame;
            var frameLe = frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 112; frameLe.preferredWidth = 112;
            frameLe.minHeight = 112; frameLe.preferredHeight = 112;

            GameObject portrait = NewUI("Portrait", frameRt, out RectTransform portraitRt);
            Image portraitImg = portrait.AddComponent<Image>();
            portraitImg.sprite = knob; portraitImg.color = Color.white;
            portraitRt.anchorMin = Vector2.zero; portraitRt.anchorMax = Vector2.one;
            portraitRt.offsetMin = new Vector2(6, 6); portraitRt.offsetMax = new Vector2(-6, -6);

            // ── Colonne infos ──
            GameObject info = NewUI("InfoColumn", rootRt, out RectTransform infoRt);
            var infoV = info.AddComponent<VerticalLayoutGroup>();
            infoV.spacing = 8;
            infoV.childAlignment = TextAnchor.MiddleLeft;
            infoV.childControlWidth = true;  infoV.childControlHeight = true;
            infoV.childForceExpandWidth = true; infoV.childForceExpandHeight = false;
            info.AddComponent<LayoutElement>().flexibleWidth = 1;

            GameObject topRow = NewUI("TopRow", infoRt, out RectTransform topRt);
            var topH = topRow.AddComponent<HorizontalLayoutGroup>();
            topH.spacing = 8;
            topH.childAlignment = TextAnchor.MiddleLeft;
            topH.childControlWidth = true;  topH.childControlHeight = true;
            topH.childForceExpandWidth = false; topH.childForceExpandHeight = false;
            topRow.AddComponent<LayoutElement>().minHeight = 38;

            TextMeshProUGUI nameText = NewText("NameText", topRt, "Nom", 32, UiTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
            nameText.GetComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI levelText = NewText("LevelText", topRt, "Niv. 1", 22, UiTheme.TextMuted,
                FontStyles.Normal, TextAlignmentOptions.MidlineRight, false);

            RectTransform row1 = NewStatRow("StatsRow1", infoRt);
            RectTransform row2 = NewStatRow("StatsRow2", infoRt);
            TextMeshProUGUI hpText    = NewStat("HpText",    row1, "PV —");
            TextMeshProUGUI atkText   = NewStat("AtkText",   row1, "ATK —");
            TextMeshProUGUI defText   = NewStat("DefText",   row2, "DEF —");
            TextMeshProUGUI speedText = NewStat("SpeedText", row2, "VIT —");

            // ── Composant + branchement ──
            var entry = root.AddComponent<CharacterEntryUI>();
            var so = new SerializedObject(entry);
            UiGen.Wire(so, "portraitImage", portraitImg);
            UiGen.Wire(so, "rarityAccent",  frameImg);
            UiGen.Wire(so, "nameText",      nameText);
            UiGen.Wire(so, "levelText",     levelText);
            UiGen.Wire(so, "hpText",        hpText);
            UiGen.Wire(so, "atkText",       atkText);
            UiGen.Wire(so, "defText",       defText);
            UiGen.Wire(so, "speedText",     speedText);
            UiGen.Wire(so, "cardButton",    button);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                System.IO.Directory.CreateDirectory(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[Generator] Carte perso générée : {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        // ── helpers ──
        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return go;
        }

        private static RectTransform NewStatRow(string name, RectTransform parent)
        {
            var go = NewUI(name, parent, out RectTransform rt);
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;  h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().minHeight = 26;
            return rt;
        }

        private static TextMeshProUGUI NewStat(string name, RectTransform parent, string text)
        {
            var t = NewText(name, parent, text, 22, UiTheme.TextSecondary, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, false);
            t.GetComponent<LayoutElement>().flexibleWidth = 1;
            return t;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            float size, Color color, FontStyles style, TextAlignmentOptions align, bool wrap)
        {
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            var font = UiGen.LoadFont(); if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = align;
            t.enableWordWrapping = wrap;
            t.overflowMode = TextOverflowModes.Overflow;
            go.AddComponent<LayoutElement>();
            return t;
        }
    }
}
#endif
