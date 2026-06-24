#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Construit un header élégant dans la scène (barre + zones + icônes + bouton Menu + pill de tour).
    /// Crée des emplacements nommés (StageValue / TalsValue / TurnValue / MenuButton) à rebrancher
    /// sur GameUI et PauseMenuUI. Sélectionne d'abord le parent (ex. TopPanel ou SafeArea).
    /// Menu : Take Five Games > UI > Construire header élégant.
    /// </summary>
    public static class HeaderBarGenerator
    {
        private static readonly Color BarColor   = new Color(0.078f, 0.086f, 0.118f, 0.96f);
        private static readonly Color PillColor  = new Color(0.102f, 0.114f, 0.149f, 0.94f);
        private static readonly Color StageColor = new Color(0.93f, 0.94f, 0.97f, 1f);
        private static readonly Color TalsColor  = new Color(0.90f, 0.77f, 0.35f, 1f);
        private static readonly Color TurnColor  = new Color(0.72f, 0.745f, 0.82f, 1f);
        private static readonly Color MenuBg     = new Color(0.137f, 0.153f, 0.20f, 1f);
        private static readonly Color IconCol    = new Color(0.86f, 0.88f, 0.94f, 1f);

        [MenuItem("Take Five Games/UI/Construire header élégant")]
        public static void Build()
        {
            var parent = Selection.activeTransform as RectTransform;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne le parent du header (ex. TopPanel ou SafeArea).", "OK");
                return;
            }

            Sprite card  = LoadByName("card_rounded") ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            Sprite coin  = LoadByName("tals_coin")    ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite burger= LoadByName("menu_burger")  ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            // ── Barre ──
            var bar = NewUI("HeaderBar", parent, out RectTransform barRt);
            barRt.anchorMin = new Vector2(0,1); barRt.anchorMax = new Vector2(1,1); barRt.pivot = new Vector2(0.5f,1);
            barRt.offsetMin = new Vector2(0,-110); barRt.offsetMax = new Vector2(0,0);
            var barImg = bar.AddComponent<Image>();
            barImg.sprite = card; barImg.type = Image.Type.Sliced; barImg.color = BarColor; barImg.raycastTarget = false;
            var barH = bar.AddComponent<HorizontalLayoutGroup>();
            barH.padding = new RectOffset(24,24,0,0); barH.spacing = 14;
            barH.childAlignment = TextAnchor.MiddleLeft;
            barH.childControlWidth = true; barH.childControlHeight = true;
            barH.childForceExpandWidth = false; barH.childForceExpandHeight = false;

            // Étage (gauche)
            var stage = NewText("StageValue", barRt, "Étage 1", 30, StageColor, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            Spacer("Spacer1", barRt);

            // Tals (centre) : pièce + valeur
            var talsGroup = NewUI("TalsGroup", barRt, out RectTransform talsRt);
            var tg = talsGroup.AddComponent<HorizontalLayoutGroup>();
            tg.spacing = 8; tg.childAlignment = TextAnchor.MiddleCenter;
            tg.childControlWidth = true; tg.childControlHeight = true;
            tg.childForceExpandWidth = false; tg.childForceExpandHeight = false;
            var coinGo = NewUI("CoinIcon", talsRt, out _);
            var coinImg = coinGo.AddComponent<Image>(); coinImg.sprite = coin; coinImg.color = TalsColor;
            var coinLe = coinGo.AddComponent<LayoutElement>(); coinLe.minWidth = 30; coinLe.preferredWidth = 30; coinLe.minHeight = 30; coinLe.preferredHeight = 30;
            NewText("TalsValue", talsRt, "0", 30, TalsColor, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            Spacer("Spacer2", barRt);

            // Bouton Menu (droite) : carré arrondi + burger
            var menu = NewUI("MenuButton", barRt, out RectTransform menuRt);
            var menuImg = menu.AddComponent<Image>(); menuImg.sprite = card; menuImg.type = Image.Type.Sliced; menuImg.color = MenuBg;
            var menuBtn = menu.AddComponent<Button>(); menuBtn.targetGraphic = menuImg;
            var menuLe = menu.AddComponent<LayoutElement>(); menuLe.minWidth = 66; menuLe.preferredWidth = 66; menuLe.minHeight = 66; menuLe.preferredHeight = 66;
            var burgerGo = NewUI("Icon", menuRt, out RectTransform burgerRt);
            var burgerImg = burgerGo.AddComponent<Image>(); burgerImg.sprite = burger; burgerImg.color = IconCol; burgerImg.raycastTarget = false;
            burgerRt.anchorMin = Vector2.zero; burgerRt.anchorMax = Vector2.one;
            burgerRt.offsetMin = new Vector2(16,16); burgerRt.offsetMax = new Vector2(-16,-16);

            // ── Pill de tour (sous la barre, centré) ──
            var pill = NewUI("TurnPill", parent, out RectTransform pillRt);
            pillRt.anchorMin = new Vector2(0.5f,1); pillRt.anchorMax = new Vector2(0.5f,1); pillRt.pivot = new Vector2(0.5f,1);
            pillRt.anchoredPosition = new Vector2(0,-120);
            var pillImg = pill.AddComponent<Image>(); pillImg.sprite = card; pillImg.type = Image.Type.Sliced; pillImg.color = PillColor; pillImg.raycastTarget = false;
            var pillH = pill.AddComponent<HorizontalLayoutGroup>();
            pillH.padding = new RectOffset(20,20,8,8);
            pillH.childAlignment = TextAnchor.MiddleCenter;
            pillH.childControlWidth = true; pillH.childControlHeight = true;
            pillH.childForceExpandWidth = false; pillH.childForceExpandHeight = false;
            var pillCsf = pill.AddComponent<ContentSizeFitter>();
            pillCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pillCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            NewText("TurnValue", pillRt, "Tour : —", 22, TurnColor, FontStyles.Normal, TextAlignmentOptions.Center);

            Undo.RegisterCreatedObjectUndo(bar, "Build header");
            Undo.RegisterCreatedObjectUndo(pill, "Build header pill");
            Selection.activeGameObject = bar;
            Debug.Log("[Header] Header élégant construit. Rebranche GameUI (StageValue/TalsValue/TurnValue) " +
                      "et PauseMenuUI (MenuButton), puis désactive les anciens.");
        }

        // ── helpers ──
        private static GameObject NewUI(string name, RectTransform parent, out RectTransform rt)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            return go;
        }

        private static void Spacer(string name, RectTransform parent)
        {
            var go = NewUI(name, parent, out _);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
        }

        private static TextMeshProUGUI NewText(string name, RectTransform parent, string text,
            float size, Color color, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
            t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Overflow;
            go.AddComponent<LayoutElement>();
            return t;
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
