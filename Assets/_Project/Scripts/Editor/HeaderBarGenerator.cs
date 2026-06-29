#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Construit un header élégant dans la scène (barre + Étage + Tals + bouton Menu + pill de tour).
    /// Couleurs/sprites/police via UiTheme + UiGen. Sélectionne d'abord le parent (ex. SafeArea).
    /// Menu : Take Five Games > UI > Construire header élégant.
    /// </summary>
    public static class HeaderBarGenerator
    {
        [MenuItem("Take Five Games/UI/Construire header élégant")]
        public static void Build()
        {
            var parent = Selection.activeTransform as RectTransform;
            if (parent == null)
            {
                EditorUtility.DisplayDialog("Sélection requise",
                    "Sélectionne le parent du header (ex. SafeArea).", "OK");
                return;
            }

            Sprite card   = UiGen.Card;
            Sprite coin   = UiGen.LoadSprite(UiTheme.SpriteCoin) ?? UiGen.Knob;
            Sprite burger = UiGen.LoadSprite(UiTheme.SpriteMenu) ?? UiGen.Knob;

            // Couleurs sourcées du thème (alpha conservé pour la légère transparence des barres)
            Color barCol  = UiTheme.SurfaceBar; barCol.a  = 0.96f;
            Color pillCol = UiTheme.SurfaceBar; pillCol.a = 0.94f;

            // ── Barre ──
            var bar = NewUI("HeaderBar", parent, out RectTransform barRt);
            barRt.anchorMin = new Vector2(0,1); barRt.anchorMax = new Vector2(1,1); barRt.pivot = new Vector2(0.5f,1);
            barRt.offsetMin = new Vector2(0,-110); barRt.offsetMax = new Vector2(0,0);
            var barImg = bar.AddComponent<Image>();
            barImg.sprite = card; barImg.type = Image.Type.Sliced; barImg.color = barCol; barImg.raycastTarget = false;
            var barH = bar.AddComponent<HorizontalLayoutGroup>();
            barH.padding = new RectOffset(24,24,0,0); barH.spacing = 14;
            barH.childAlignment = TextAnchor.MiddleLeft;
            barH.childControlWidth = true; barH.childControlHeight = true;
            barH.childForceExpandWidth = false; barH.childForceExpandHeight = false;

            NewText("StageValue", barRt, "Étage 1", 30, UiTheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            Spacer("Spacer1", barRt);

            var talsGroup = NewUI("TalsGroup", barRt, out RectTransform talsRt);
            var tg = talsGroup.AddComponent<HorizontalLayoutGroup>();
            tg.spacing = 8; tg.childAlignment = TextAnchor.MiddleCenter;
            tg.childControlWidth = true; tg.childControlHeight = true;
            tg.childForceExpandWidth = false; tg.childForceExpandHeight = false;
            var coinGo = NewUI("CoinIcon", talsRt, out _);
            var coinImg = coinGo.AddComponent<Image>(); coinImg.sprite = coin; coinImg.color = UiTheme.Gold;
            var coinLe = coinGo.AddComponent<LayoutElement>(); coinLe.minWidth = 30; coinLe.preferredWidth = 30; coinLe.minHeight = 30; coinLe.preferredHeight = 30;
            NewText("TalsValue", talsRt, "0", 30, UiTheme.Gold, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            Spacer("Spacer2", barRt);

            var menu = NewUI("MenuButton", barRt, out RectTransform menuRt);
            var menuImg = menu.AddComponent<Image>(); menuImg.sprite = card; menuImg.type = Image.Type.Sliced; menuImg.color = UiTheme.Frame;
            menu.AddComponent<Button>().targetGraphic = menuImg;
            var menuLe = menu.AddComponent<LayoutElement>(); menuLe.minWidth = 66; menuLe.preferredWidth = 66; menuLe.minHeight = 66; menuLe.preferredHeight = 66;
            var burgerGo = NewUI("Icon", menuRt, out RectTransform burgerRt);
            var burgerImg = burgerGo.AddComponent<Image>(); burgerImg.sprite = burger; burgerImg.color = UiTheme.TextSecondary; burgerImg.raycastTarget = false;
            burgerRt.anchorMin = Vector2.zero; burgerRt.anchorMax = Vector2.one;
            burgerRt.offsetMin = new Vector2(16,16); burgerRt.offsetMax = new Vector2(-16,-16);

            // ── Pill de tour ──
            var pill = NewUI("TurnPill", parent, out RectTransform pillRt);
            pillRt.anchorMin = new Vector2(0.5f,1); pillRt.anchorMax = new Vector2(0.5f,1); pillRt.pivot = new Vector2(0.5f,1);
            pillRt.anchoredPosition = new Vector2(0,-120);
            var pillImg = pill.AddComponent<Image>(); pillImg.sprite = card; pillImg.type = Image.Type.Sliced; pillImg.color = pillCol; pillImg.raycastTarget = false;
            var pillH = pill.AddComponent<HorizontalLayoutGroup>();
            pillH.padding = new RectOffset(20,20,8,8);
            pillH.childAlignment = TextAnchor.MiddleCenter;
            pillH.childControlWidth = true; pillH.childControlHeight = true;
            pillH.childForceExpandWidth = false; pillH.childForceExpandHeight = false;
            var pillCsf = pill.AddComponent<ContentSizeFitter>();
            pillCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            pillCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            NewText("TurnValue", pillRt, "Tour : —", 22, UiTheme.TextSecondary, FontStyles.Normal, TextAlignmentOptions.Center);

            Undo.RegisterCreatedObjectUndo(bar, "Build header");
            Undo.RegisterCreatedObjectUndo(pill, "Build header pill");
            Selection.activeGameObject = bar;
            Debug.Log("[Header] Header élégant construit. Rebranche GameUI (StageValue/TalsValue/TurnValue) et PauseMenuUI (MenuButton).");
        }

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
            var font = UiGen.LoadFont(); if (font != null) t.font = font;
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
            t.enableWordWrapping = false; t.overflowMode = TextOverflowModes.Overflow;
            go.AddComponent<LayoutElement>();
            return t;
        }
    }
}
#endif
