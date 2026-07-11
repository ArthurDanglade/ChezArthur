#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère le prefab d'entrée de synergie (SynergyEntryUI), habillé.
    /// Menu : Take Five Games > UI > Générer entrée synergie (SynergyEntry).
    /// </summary>
    public static class SynergyEntryCardGenerator
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/SynergyEntryCard.prefab";

        [MenuItem("Take Five Games/UI/Générer entrée synergie (SynergyEntry)")]
        public static void Generate()
        {
            Sprite card = UiGen.Card;

            GameObject root = NewUI("SynergyEntryCard", null, out RectTransform rootRt);
            Image rootImg = root.AddComponent<Image>();
            rootImg.sprite = card;
            rootImg.type = Image.Type.Sliced;
            rootImg.color = UiTheme.Surface;
            rootImg.raycastTarget = false;

            root.AddComponent<CanvasGroup>();

            VerticalLayoutGroup rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.padding = new RectOffset(14, 14, 14, 14);
            rootV.spacing = 4;
            rootV.childAlignment = TextAnchor.UpperLeft;
            rootV.childControlWidth = true;
            rootV.childControlHeight = true;
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            LayoutElement rootLe = root.AddComponent<LayoutElement>();
            rootLe.minHeight = 104;
            rootLe.preferredHeight = 104;

            TextMeshProUGUI nameText = NewText("NameText", rootRt, "MégaCrit", 22,
                UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.TopLeft, false);
            TextMeshProUGUI comboText = NewText("ComboText", rootRt, "Critique + Frénésie", 18,
                UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.TopLeft, false);
            TextMeshProUGUI descText = NewText("DescText", rootRt, "Description…", 18,
                UiTheme.TextPrimary, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);

            SynergyEntryUI entry = root.AddComponent<SynergyEntryUI>();
            SerializedObject so = new SerializedObject(entry);
            UiGen.Wire(so, "nameText", nameText);
            UiGen.Wire(so, "comboText", comboText);
            UiGen.Wire(so, "descriptionText", descText);
            UiGen.Wire(so, "backgroundImage", rootImg);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                System.IO.Directory.CreateDirectory(PrefabFolder);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[Generator] Entrée synergie générée : {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)go.transform;
            if (parent != null) rt.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            float size, Color color, FontStyles style, TextAlignmentOptions align, bool wrap)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            t.enableWordWrapping = wrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            go.AddComponent<LayoutElement>();
            return t;
        }
    }
}
#endif
