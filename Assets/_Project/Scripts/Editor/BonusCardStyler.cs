#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Habille une BonusCard sélectionnée EN PLACE : lit ses références sérialisées et les stylise
    /// (carte arrondie, badge en pill, typo, couleurs de rareté sobres, icône preserveAspect).
    /// Ne touche ni au layout ni à la logique. À lancer sur chacune des cartes (ou le prefab).
    /// Menu : Take Five Games > UI > Habiller BonusCard.
    /// </summary>
    public static class BonusCardStyler
    {
        // Raretés retravaillées : tons sombres teintés (la carte reste lisible, pas criarde)
        private static readonly Color Common   = new Color(0.227f, 0.243f, 0.282f);
        private static readonly Color Uncommon = new Color(0.180f, 0.369f, 0.227f);
        private static readonly Color Rare     = new Color(0.165f, 0.290f, 0.478f);
        private static readonly Color Epic      = new Color(0.290f, 0.165f, 0.431f);
        private static readonly Color Special  = new Color(0.478f, 0.369f, 0.118f);

        private static readonly Color NameCol   = new Color(0.941f, 0.949f, 0.969f);
        private static readonly Color RarityCol = new Color(0.690f, 0.714f, 0.784f);
        private static readonly Color DescCol   = new Color(0.784f, 0.800f, 0.847f);
        private static readonly Color BeforeCol = new Color(0.565f, 0.596f, 0.659f);
        private static readonly Color AfterCol  = new Color(0.486f, 0.780f, 0.486f);
        private static readonly Color DownCol   = new Color(0.878f, 0.478f, 0.478f);

        [MenuItem("Take Five Games/UI/Habiller BonusCard")]
        public static void Style()
        {
            var go = Selection.activeGameObject;
            var card = go != null ? go.GetComponent<BonusCard>() : null;
            if (card == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne une carte avec le composant BonusCard.", "OK");
                return;
            }

            Sprite rounded = LoadByName("card_rounded")
                ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var so = new SerializedObject(card);

            // Fond → carte arrondie (garde sa teinte de rareté, posée par le code)
            var bg = Img(so, "backgroundImage");
            if (bg != null) { Undo.RecordObject(bg, "style"); bg.sprite = rounded; bg.type = Image.Type.Sliced; EditorUtility.SetDirty(bg); }

            // Badge → pill arrondi (couleur posée par le code selon le type)
            var badgeBg = Img(so, "badgeBackground");
            if (badgeBg != null) { Undo.RecordObject(badgeBg, "style"); badgeBg.sprite = rounded; badgeBg.type = Image.Type.Sliced; EditorUtility.SetDirty(badgeBg); }

            // Icône : ne pas déformer
            var icon = Img(so, "iconImage");
            if (icon != null) { Undo.RecordObject(icon, "style"); icon.preserveAspect = true; EditorUtility.SetDirty(icon); }

            // Typographie
            StyleText(so, "nameText",        30, FontStyles.Bold,   NameCol);
            StyleText(so, "rarityText",      18, FontStyles.Normal, RarityCol);
            StyleText(so, "descriptionText", 20, FontStyles.Normal, DescCol);
            StyleText(so, "badgeText",       18, FontStyles.Bold,   Color.white);
            StyleText(so, "beforeText",      22, FontStyles.Normal, BeforeCol);
            StyleText(so, "afterText",       22, FontStyles.Bold,   AfterCol);
            StyleText(so, "downsideText",    20, FontStyles.Bold,   DownCol);

            // Couleurs de rareté (champs lus par le code)
            SetColor(so, "commonColor",   Common);
            SetColor(so, "uncommonColor", Uncommon);
            SetColor(so, "rareColor",     Rare);
            SetColor(so, "epicColor",     Epic);
            SetColor(so, "specialColor",  Special);
            so.ApplyModifiedProperties();

            Debug.Log($"[BonusCard] '{card.name}' habillée.");
        }

        private static Image Img(SerializedObject so, string field)
        {
            var p = so.FindProperty(field);
            return p != null ? p.objectReferenceValue as Image : null;
        }

        private static void StyleText(SerializedObject so, string field, float size, FontStyles style, Color col)
        {
            var p = so.FindProperty(field);
            var t = p != null ? p.objectReferenceValue as TextMeshProUGUI : null;
            if (t == null) return;
            Undo.RecordObject(t, "style text");
            t.fontSize = size; t.fontStyle = style; t.color = col;
            EditorUtility.SetDirty(t);
        }

        private static void SetColor(SerializedObject so, string field, Color col)
        {
            var p = so.FindProperty(field);
            if (p != null) p.colorValue = col;
        }

        private static Sprite LoadByName(string spriteName)
        {
            foreach (var g in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null && s.name == spriteName) return s;
            }
            return null;
        }
    }
}
#endif
