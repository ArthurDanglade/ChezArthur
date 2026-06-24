#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère par code le prefab d'entrée de valise (ValiseEntryUI), version habillée :
    /// carte arrondie, icône encadrée, aéré. Menu : Take Five Games > UI > Générer entrée valise.
    /// </summary>
    public static class ValiseEntryCardGenerator
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/ValiseEntryCard.prefab";

        private static readonly Color CardBg    = new Color(0.118f, 0.129f, 0.157f, 1f);
        private static readonly Color NameColor = Color.white;
        private static readonly Color LevelCol  = new Color(0.62f, 0.64f, 0.70f, 1f);
        private static readonly Color DescColor = new Color(0.78f, 0.80f, 0.86f, 1f);
        private static readonly Color FrameCol  = new Color(0.30f, 0.33f, 0.40f, 1f);

        [MenuItem("Take Five Games/UI/Générer entrée valise (ValiseEntry)")]
        public static void Generate()
        {
            Sprite card = LoadByName("card_rounded")
                ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // ── Racine ──
            GameObject root = NewUI("ValiseEntryCard", null, out RectTransform rootRt);
            Image rootImg = root.AddComponent<Image>();
            rootImg.sprite = card; rootImg.type = Image.Type.Sliced; rootImg.color = CardBg;

            var rootH = root.AddComponent<HorizontalLayoutGroup>();
            rootH.padding = new RectOffset(14, 14, 14, 14);
            rootH.spacing = 14;
            rootH.childAlignment = TextAnchor.MiddleLeft;
            rootH.childControlWidth = true;  rootH.childControlHeight = true;
            rootH.childForceExpandWidth = false; rootH.childForceExpandHeight = false;
            var rootLe = root.AddComponent<LayoutElement>();
            rootLe.minHeight = 104; rootLe.preferredHeight = 104;

            // ── Cadre icône ──
            GameObject frame = NewUI("IconFrame", rootRt, out RectTransform frameRt);
            Image frameImg = frame.AddComponent<Image>();
            frameImg.sprite = card; frameImg.type = Image.Type.Sliced; frameImg.color = FrameCol;
            var frameLe = frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 76;  frameLe.preferredWidth = 76;
            frameLe.minHeight = 76; frameLe.preferredHeight = 76;

            GameObject icon = NewUI("Icon", frameRt, out RectTransform iconRt);
            Image iconImg = icon.AddComponent<Image>();
            iconImg.sprite = knob; iconImg.color = Color.white;
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(5, 5); iconRt.offsetMax = new Vector2(-5, -5);

            // ── Colonne infos ──
            GameObject info = NewUI("InfoColumn", rootRt, out RectTransform infoRt);
            var infoV = info.AddComponent<VerticalLayoutGroup>();
            infoV.spacing = 4;
            infoV.childAlignment = TextAnchor.UpperLeft;
            infoV.childControlWidth = true;  infoV.childControlHeight = true;
            infoV.childForceExpandWidth = true; infoV.childForceExpandHeight = false;
            info.AddComponent<LayoutElement>().flexibleWidth = 1;

            GameObject topRow = NewUI("TopRow", infoRt, out RectTransform topRt);
            var topH = topRow.AddComponent<HorizontalLayoutGroup>();
            topH.spacing = 8;
            topH.childAlignment = TextAnchor.MiddleLeft;
            topH.childControlWidth = true;  topH.childControlHeight = true;
            topH.childForceExpandWidth = false; topH.childForceExpandHeight = false;
            topRow.AddComponent<LayoutElement>().minHeight = 32;

            TextMeshProUGUI nameText = NewText("NameText", topRt, "Valise", 26, NameColor,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
            nameText.GetComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI levelText = NewText("LevelText", topRt, "Niv. 1", 20, LevelCol,
                FontStyles.Normal, TextAlignmentOptions.MidlineRight, false);

            TextMeshProUGUI descText = NewText("DescText", infoRt, "Description…", 20, DescColor,
                FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
            descText.GetComponent<LayoutElement>().flexibleWidth = 1;

            // ── Composant + branchement ──
            var entry = root.AddComponent<ValiseEntryUI>();
            var so = new SerializedObject(entry);
            Wire(so, "iconImage", iconImg);
            Wire(so, "nameText",  nameText);
            Wire(so, "levelText", levelText);
            Wire(so, "descText",  descText);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                System.IO.Directory.CreateDirectory(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[Generator] Entrée valise (habillée) générée : {PrefabPath}");
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
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = align;
            t.enableWordWrapping = wrap;
            t.overflowMode = TextOverflowModes.Overflow;
            go.AddComponent<LayoutElement>();
            return t;
        }

        private static Sprite LoadByName(string spriteName)
        {
            var guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null && s.name == spriteName) return s;
            }
            return null;
        }

        private static void Wire(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[Generator] Champ '{field}' introuvable sur ValiseEntryUI.");
                return;
            }
            prop.objectReferenceValue = value;
        }
    }
}
#endif
