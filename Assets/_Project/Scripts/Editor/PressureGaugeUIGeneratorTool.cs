#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère le manomètre vertical de pression (PressureGaugeBar_v1) sous le canvas GameUI.
    /// </summary>
    public static class PressureGaugeUIGeneratorTool
    {
        private const string BarObjectName = "PressureGaugeBar_v1";

        private const bool ANCHOR_RIGHT = true;
        private const float WIDTH = 16f;
        private const float TOP_MARGIN = 240f;
        private const float BOTTOM_MARGIN = 340f;
        private const float SIDE_MARGIN = 10f;
        private const int OverlaySortOrder = 120;

        [MenuItem("Chez Arthur/UI/Générer Manomètre Pression")]
        public static void Generate()
        {
            Undo.SetCurrentGroupName("Générer Manomètre Pression");
            int undoGroup = Undo.GetCurrentGroup();

            Transform hudParent = FindHudParent();
            if (hudParent == null)
            {
                Debug.LogError("[PressureGaugeUIGenerator] GameUI / SafeArea introuvable dans la scène.");
                return;
            }

            DestroyExistingBar();

            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject rootGo = new GameObject(
                BarObjectName,
                typeof(RectTransform),
                typeof(PressureGaugeUI));
            Undo.RegisterCreatedObjectUndo(rootGo, "Créer PressureGaugeBar_v1");
            rootGo.layer = hudParent.gameObject.layer;
            rootGo.transform.SetParent(hudParent, false);

            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            ConfigureRootRect(rootRt);
            EnsureOverlayCanvas(rootGo);

            Color track = new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.82f);
            Image background = CreateStretchImage("Background", rootRt, uiSprite, track);
            Image ghostFill = CreateVerticalFilledImage(
                "GhostFill",
                rootRt,
                uiSprite,
                new Color(0.92f, 0.94f, 0.98f, 0.35f));
            Image fill = CreateVerticalFilledImage(
                "Fill",
                rootRt,
                uiSprite,
                Color.white);

            PressureGaugeUI ui = rootGo.GetComponent<PressureGaugeUI>();
            WirePressureGaugeUI(ui, rootRt, background, ghostFill, fill);

            rootRt.SetAsLastSibling();
            rootGo.SetActive(true);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = rootGo;

            string side = ANCHOR_RIGHT ? "droite" : "gauche";
            Debug.Log(
                $"[PressureGaugeUIGenerator] Manomètre généré ({BarObjectName}) — bord {side}, " +
                $"largeur {WIDTH}px, marges T{TOP_MARGIN}/B{BOTTOM_MARGIN}/S{SIDE_MARGIN}.");
        }

        private static Transform FindHudParent()
        {
            GameUI gameUI = Object.FindObjectOfType<GameUI>(true);
            if (gameUI == null)
                return null;

            if (gameUI.transform.parent != null)
                return gameUI.transform.parent;

            Canvas canvas = gameUI.GetComponentInParent<Canvas>(true);
            return canvas != null ? canvas.transform : gameUI.transform;
        }

        private static void DestroyExistingBar()
        {
            PressureGaugeUI[] existing = Object.FindObjectsOfType<PressureGaugeUI>(true);
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null)
                    Undo.DestroyObjectImmediate(existing[i].gameObject);
            }
        }

        private static void ConfigureRootRect(RectTransform rootRt)
        {
            float anchorX = ANCHOR_RIGHT ? 1f : 0f;
            float verticalInset = TOP_MARGIN + BOTTOM_MARGIN;

            rootRt.anchorMin = new Vector2(anchorX, 0f);
            rootRt.anchorMax = new Vector2(anchorX, 1f);
            rootRt.pivot = new Vector2(anchorX, 0.5f);
            rootRt.anchoredPosition = new Vector2(ANCHOR_RIGHT ? -SIDE_MARGIN : SIDE_MARGIN, 0f);
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            rootRt.sizeDelta = new Vector2(WIDTH, -verticalInset);
        }

        private static void EnsureOverlayCanvas(GameObject rootGo)
        {
            Canvas overlay = rootGo.GetComponent<Canvas>();
            if (overlay == null)
                overlay = rootGo.AddComponent<Canvas>();

            overlay.overrideSorting = true;
            overlay.sortingOrder = OverlaySortOrder;
        }

        private static Image CreateStretchImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Créer {name}");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;

            return img;
        }

        private static Image CreateVerticalFilledImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Créer {name}");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Vertical;
            img.fillOrigin = (int)Image.OriginVertical.Bottom;
            img.fillAmount = 0f;
            img.color = color;
            img.raycastTarget = false;

            return img;
        }

        private static void WirePressureGaugeUI(
            PressureGaugeUI ui,
            RectTransform barRoot,
            Image background,
            Image ghostFill,
            Image fill)
        {
            if (ui == null)
                return;

            SerializedObject so = new SerializedObject(ui);
            UiGen.Wire(so, "barRoot", barRoot);
            UiGen.Wire(so, "backgroundImage", background);
            UiGen.Wire(so, "ghostFillImage", ghostFill);
            UiGen.Wire(so, "fillImage", fill);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
