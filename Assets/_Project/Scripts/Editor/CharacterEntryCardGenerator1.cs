#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère par code le prefab de carte personnage (CharacterEntryUI), version habillée :
    /// carte arrondie, portrait encadré (anneau de rareté), aéré, typographie hiérarchisée.
    /// Menu : Take Five Games > UI > Générer carte perso.
    /// </summary>
    public static class CharacterEntryCardGenerator
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string PrefabPath = PrefabFolder + "/CharacterEntryCard.prefab";

        private static readonly Color CardBg    = new Color(0.118f, 0.129f, 0.157f, 1f); // #1E2128
        private static readonly Color NameColor = Color.white;
        private static readonly Color LevelCol  = new Color(0.62f, 0.64f, 0.70f, 1f);
        private static readonly Color StatColor = new Color(0.90f, 0.91f, 0.95f, 1f);
        private static readonly Color FrameDef  = new Color(0.30f, 0.33f, 0.40f, 1f);    // anneau par défaut

        [MenuItem("Take Five Games/UI/Générer carte perso (CharacterEntry)")]
        public static void Generate()
        {
            // Carte arrondie si importée, sinon UISprite intégré ; Knob pour le portrait placeholder.
            Sprite card = LoadByName("card_rounded")
                ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // ── Racine ──
            GameObject root = NewUI("CharacterEntryCard", null, out RectTransform rootRt);
            Image rootImg = root.AddComponent<Image>();
            rootImg.sprite = card; rootImg.type = Image.Type.Sliced; rootImg.color = CardBg;

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
            frameImg.sprite = card; frameImg.type = Image.Type.Sliced; frameImg.color = FrameDef;
            var frameLe = frame.AddComponent<LayoutElement>();
            frameLe.minWidth = 112; frameLe.preferredWidth = 112;
            frameLe.minHeight = 112; frameLe.preferredHeight = 112;

            // Portrait dans le cadre (inset de 6 → l'anneau dépasse tout autour)
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

            // Rangée haut : nom + niveau
            GameObject topRow = NewUI("TopRow", infoRt, out RectTransform topRt);
            var topH = topRow.AddComponent<HorizontalLayoutGroup>();
            topH.spacing = 8;
            topH.childAlignment = TextAnchor.MiddleLeft;
            topH.childControlWidth = true;  topH.childControlHeight = true;
            topH.childForceExpandWidth = false; topH.childForceExpandHeight = false;
            topRow.AddComponent<LayoutElement>().minHeight = 38;

            TextMeshProUGUI nameText = NewText("NameText", topRt, "Nom", 32, NameColor,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, false);
            nameText.GetComponent<LayoutElement>().flexibleWidth = 1;

            TextMeshProUGUI levelText = NewText("LevelText", topRt, "Niv. 1", 22, LevelCol,
                FontStyles.Normal, TextAlignmentOptions.MidlineRight, false);

            // Stats : 2 rangées de 2
            RectTransform row1 = NewStatRow("StatsRow1", infoRt);
            RectTransform row2 = NewStatRow("StatsRow2", infoRt);
            TextMeshProUGUI hpText    = NewStat("HpText",    row1, "PV —");
            TextMeshProUGUI atkText   = NewStat("AtkText",   row1, "ATK —");
            TextMeshProUGUI defText   = NewStat("DefText",   row2, "DEF —");
            TextMeshProUGUI speedText = NewStat("SpeedText", row2, "VIT —");

            // ── Composant + branchement ──
            var entry = root.AddComponent<CharacterEntryUI>();
            var so = new SerializedObject(entry);
            Wire(so, "portraitImage", portraitImg);
            Wire(so, "rarityAccent",  frameImg);
            Wire(so, "nameText",      nameText);
            Wire(so, "levelText",     levelText);
            Wire(so, "hpText",        hpText);
            Wire(so, "atkText",       atkText);
            Wire(so, "defText",       defText);
            Wire(so, "speedText",     speedText);
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Sauvegarde ──
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                System.IO.Directory.CreateDirectory(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            Debug.Log($"[Generator] Carte perso (habillée) générée : {PrefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
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
            var t = NewText(name, parent, text, 22, StatColor, FontStyles.Normal,
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
                Debug.LogWarning($"[Generator] Champ '{field}' introuvable sur CharacterEntryUI — " +
                                 "vérifie que le script est à jour (anneau de rareté ajouté).");
                return;
            }
            prop.objectReferenceValue = value;
        }
    }
}
#endif
