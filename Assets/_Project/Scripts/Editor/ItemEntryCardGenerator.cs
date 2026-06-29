#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère le prefab d'entrée d'item (ItemEntryUI), habillé.
    /// Couleurs/sprite/police via UiTheme + UiGen. Menu : Take Five Games > UI > Générer entrée item.
    /// </summary>
    public static class ItemEntryCardGenerator
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/ItemEntryCard.prefab";

        [MenuItem("Take Five Games/UI/Générer entrée item (ItemEntry)")]
        public static void Generate()
        {
            Sprite card = UiGen.Card;
            Sprite knob = UiGen.Knob;

            GameObject root = NewUI("ItemEntryCard", null, out RectTransform rootRt);
            Image rootImg = root.AddComponent<Image>();
            rootImg.sprite = card; rootImg.type = Image.Type.Sliced; rootImg.color = UiTheme.Surface;

            var rootH = root.AddComponent<HorizontalLayoutGroup>();
            rootH.padding = new RectOffset(14, 14, 14, 14);
            rootH.spacing = 14;
            rootH.childAlignment = TextAnchor.MiddleLeft;
            rootH.childControlWidth = true;  rootH.childControlHeight = true;
            rootH.childForceExpandWidth = false; rootH.childForceExpandHeight = false;
            var rootLe = root.AddComponent<LayoutElement>();
            rootLe.minHeight = 96; rootLe.preferredHeight = 96;

            GameObject frame = NewUI("IconFrame", rootRt, out RectTransform frameRt);
            Image frameImg = frame.AddComponent<Image>();
            frameImg.sprite = card; frameImg.type = Image.Type.Sliced; frameImg.color = UiTheme.Frame;
            var frameLe = frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 72;  frameLe.preferredWidth = 72;
            frameLe.minHeight = 72; frameLe.preferredHeight = 72;

            GameObject icon = NewUI("Icon", frameRt, out RectTransform iconRt);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.sprite = knob; iconImg.color = Color.white;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(5, 5); iconRt.offsetMax = new Vector2(-5, -5);

            GameObject info = NewUI("InfoColumn", rootRt, out RectTransform infoRt);
            var infoV = info.AddComponent<VerticalLayoutGroup>();
            infoV.spacing = 4;
            infoV.childAlignment = TextAnchor.UpperLeft;
            infoV.childControlWidth = true;  infoV.childControlHeight = true;
            infoV.childForceExpandWidth = true; infoV.childForceExpandHeight = false;
            info.AddComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI nameText = NewText("NameText", infoRt, "Item", 26, UiTheme.TextPrimary,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
            nameText.GetComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI descText = NewText("DescText", infoRt, "Description…", 20, UiTheme.TextSecondary,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            descText.GetComponent<LayoutElement>().flexibleWidth = 1;

            var entry = root.AddComponent<ItemEntryUI>();
            var so = new SerializedObject(entry);
            UiGen.Wire(so, "iconImage", iconImg);
            UiGen.Wire(so, "nameText",  nameText);
            UiGen.Wire(so, "descText",  descText);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                System.IO.Directory.CreateDirectory(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[Generator] Entrée item générée : {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return go;
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
