#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Gate 5b — Active le SCROLL HORIZONTAL du sélecteur de slots, conformément à la maquette :
    /// SlotsGrid est enveloppé dans un ScrollRect (horizontal only) avec un Viewport masqué et une
    /// barre fine dessous (auto-cachée tant qu'il n'y a pas de débordement). Un seul format gère
    /// 3 valises comme 7 items : le scroll absorbe la longueur.
    ///
    /// Garde-fou prefab : si SlotsGrid faisait partie d'une instance de prefab, le reparentage est
    /// bloqué par Unity → on s'arrête avec un message clair. (Ici SlotsGrid est un objet de scène.)
    /// Idempotent : relançable sans dupliquer. Menu : Take Five Games > UI > Activer le scroll des slots.
    /// </summary>
    public static class SacrificeSlotsScrollBuilder
    {
        // ── Contrat maquette ──
        private const float ViewportHeight = 110f;   // = hauteur d'une pastille
        private const float BarHeight      = 10f;    // barre fine
        private const float BarGap         = 6f;     // espace pastilles ↔ barre
        private const float ScrollHeight   = ViewportHeight + BarGap + BarHeight;
        private const int   BarPPUM        = 6;       // arrondi de la barre (plus haut = plus fin)

        [MenuItem("Take Five Games/UI/Activer le scroll des slots")]
        public static void Build()
        {
            var anySlot = Object.FindObjectOfType<SacrificeSlotUI>(true);
            if (anySlot == null || anySlot.transform.parent == null)
            { Dialog("Aucun SacrificeSlotUI (ou pas de parent SlotsGrid). Ouvre la scène Game."); return; }

            var slotsGrid = anySlot.transform.parent.gameObject;

            // Garde-fou prefab.
            if (PrefabUtility.IsPartOfPrefabInstance(slotsGrid))
            {
                Dialog("SlotsGrid fait partie d'une instance de prefab : Unity bloque son reparentage.\n" +
                       "Sors-le du prefab (ou édite le prefab) avant d'activer le scroll.");
                return;
            }

            Sprite rounded = UiGen.Card;
            Transform container = slotsGrid.transform.parent;
            int gridIndex = slotsGrid.transform.GetSiblingIndex();

            // Déjà enveloppé ? (SlotsGrid sous Viewport sous un ScrollRect)
            GameObject scrollGo, viewportGo;
            ScrollRect scroll;
            if (slotsGrid.transform.parent.name == "Viewport"
                && slotsGrid.transform.parent.parent != null
                && slotsGrid.transform.parent.parent.GetComponent<ScrollRect>() != null)
            {
                viewportGo = slotsGrid.transform.parent.gameObject;
                scrollGo   = viewportGo.transform.parent.gameObject;
                scroll     = scrollGo.GetComponent<ScrollRect>();
            }
            else
            {
                scrollGo = new GameObject("SlotsScroll", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(scrollGo, "create scroll");
                var srt = (RectTransform)scrollGo.transform;
                srt.SetParent(container, false);
                srt.SetSiblingIndex(gridIndex);
                scroll = Undo.AddComponent<ScrollRect>(scrollGo);

                viewportGo = new GameObject("Viewport", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(viewportGo, "create viewport");
                ((RectTransform)viewportGo.transform).SetParent(scrollGo.transform, false);

                Undo.SetTransformParent(slotsGrid.transform, viewportGo.transform, "reparent grid");
            }

            // ── SlotsScroll : pleine largeur (Container la contrôle), hauteur fixe ──
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.sizeDelta = new Vector2(scrollRt.sizeDelta.x, ScrollHeight);
            var scrollLe = scrollGo.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(scrollGo);
            scrollLe.minHeight = ScrollHeight; scrollLe.preferredHeight = ScrollHeight; // prêt pour Gate 6 (childControlHeight)

            // ── Viewport : en haut, hauteur pastille, masqué ──
            var vpRt = (RectTransform)viewportGo.transform;
            vpRt.anchorMin = new Vector2(0f, 1f); vpRt.anchorMax = new Vector2(1f, 1f); vpRt.pivot = new Vector2(0.5f, 1f);
            vpRt.anchoredPosition = Vector2.zero; vpRt.sizeDelta = new Vector2(0f, ViewportHeight);
            if (viewportGo.GetComponent<RectMask2D>() == null) Undo.AddComponent<RectMask2D>(viewportGo);
            var vpImg = viewportGo.GetComponent<Image>() ?? Undo.AddComponent<Image>(viewportGo);
            vpImg.color = new Color(1f, 1f, 1f, 0f); vpImg.raycastTarget = true; // capte le drag dans les espaces vides

            // ── Content = SlotsGrid : ancré haut-gauche, largeur = contenu (CSF horizontal) ──
            var gridRt = (RectTransform)slotsGrid.transform;
            gridRt.anchorMin = new Vector2(0f, 1f); gridRt.anchorMax = new Vector2(0f, 1f); gridRt.pivot = new Vector2(0f, 1f);
            gridRt.anchoredPosition = Vector2.zero;
            var hlg = slotsGrid.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.childAlignment = TextAnchor.MiddleLeft; // liste qui démarre à gauche et scrolle
            var csf = slotsGrid.GetComponent<ContentSizeFitter>() ?? Undo.AddComponent<ContentSizeFitter>(slotsGrid);
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; // largeur suit le contenu → scroll
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize; // hauteur = pastille
            EditorUtility.SetDirty(csf);

            // ── Barre fine en bas ──
            var bar = BuildScrollbar(scrollGo.transform, rounded);

            // ── Câblage ScrollRect ──
            Undo.RecordObject(scroll, "scroll cfg");
            scroll.content = gridRt;
            scroll.viewport = vpRt;
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.horizontalScrollbar = bar;
            scroll.verticalScrollbar = null;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.scrollSensitivity = 30f;
            EditorUtility.SetDirty(scroll);
            EditorUtility.SetDirty(scrollGo);

            Debug.Log("[Sacrifice] Scroll horizontal activé : SlotsScroll + Viewport + barre fine (auto-cachée).");
        }

        private static Scrollbar BuildScrollbar(Transform parent, Sprite rounded)
        {
            var barGo = FindOrCreate(parent, "Scrollbar");
            var barRt = (RectTransform)barGo.transform;
            barRt.anchorMin = new Vector2(0f, 0f); barRt.anchorMax = new Vector2(1f, 0f); barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = Vector2.zero; barRt.sizeDelta = new Vector2(0f, BarHeight);
            var barImg = barGo.GetComponent<Image>() ?? Undo.AddComponent<Image>(barGo);
            barImg.sprite = rounded; barImg.type = Image.Type.Sliced; barImg.pixelsPerUnitMultiplier = BarPPUM;
            barImg.color = UiTheme.Frame; barImg.raycastTarget = true;
            var bar = barGo.GetComponent<Scrollbar>() ?? Undo.AddComponent<Scrollbar>(barGo);

            var area = FindOrCreate(barGo.transform, "Sliding Area");
            var areaRt = (RectTransform)area.transform;
            areaRt.anchorMin = Vector2.zero; areaRt.anchorMax = Vector2.one;
            areaRt.offsetMin = new Vector2(2f, 2f); areaRt.offsetMax = new Vector2(-2f, -2f);

            var handle = FindOrCreate(area.transform, "Handle");
            var handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = Vector2.zero; handleRt.anchorMax = Vector2.one; handleRt.offsetMin = Vector2.zero; handleRt.offsetMax = Vector2.zero;
            var handleImg = handle.GetComponent<Image>() ?? Undo.AddComponent<Image>(handle);
            handleImg.sprite = rounded; handleImg.type = Image.Type.Sliced; handleImg.pixelsPerUnitMultiplier = BarPPUM;
            handleImg.color = UiTheme.Filet; handleImg.raycastTarget = true;

            Undo.RecordObject(bar, "bar cfg");
            bar.direction = Scrollbar.Direction.LeftToRight;
            bar.handleRect = handleRt;
            bar.targetGraphic = handleImg;
            EditorUtility.SetDirty(bar);
            return bar;
        }

        private static GameObject FindOrCreate(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "create");
            return go;
        }

        private static void Dialog(string msg) { EditorUtility.DisplayDialog("Scroll des slots", msg, "OK"); }
    }
}
#endif
