#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère le périmètre de pression (PressurePerimeter_v1).
    /// Cadre rectangulaire sous le HeaderBar (layout runtime auto), sous le HUD.
    /// </summary>
    public static class PressurePerimeterGeneratorTool
    {
        private const string PerimeterObjectName = "PressurePerimeter_v1";
        private const string LegacyBarObjectName = "PressureGaugeBar_v1";
        private const string HeaderBarObjectName = "HeaderBar";

        // Bords L/R/B — le haut est calé runtime sur le bas du HeaderBar.
        private const float INSET = 3f;
        private const float TOP_GAP = 2f;

        private const float THICKNESS = 3f;
        // Chevauchement = épaisseur → coins fermés sans trou.
        private const float CORNER_OVERLAP = 3f;
        private const float TRACK_ALPHA = 0.42f;

        [MenuItem("Chez Arthur/UI/Générer Périmètre Pression")]
        public static void Generate()
        {
            Undo.SetCurrentGroupName("Générer Périmètre Pression");
            int undoGroup = Undo.GetCurrentGroup();

            Transform hudParent = FindHudParent();
            if (hudParent == null)
            {
                Debug.LogError("[PressurePerimeterGenerator] GameUI / SafeArea introuvable dans la scène.");
                return;
            }

            DestroyExistingPressureUi();

            // Pixel opaque : pas de soft edge (contrairement à UISprite).
            Sprite lineSprite = UiGen.SolidWhite;
            if (lineSprite == null)
            {
                Debug.LogError("[PressurePerimeterGenerator] Impossible de créer/charger ui_white_pixel.");
                return;
            }

            GameObject rootGo = new GameObject(
                PerimeterObjectName,
                typeof(RectTransform),
                typeof(PressurePerimeterUI));
            Undo.RegisterCreatedObjectUndo(rootGo, "Créer PressurePerimeter_v1");
            rootGo.layer = hudParent.gameObject.layer;
            rootGo.transform.SetParent(hudParent, false);

            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            ConfigureRootRect(rootRt);

            // Sous le HUD : pas d'overlay Canvas, premier sibling.
            rootRt.SetAsFirstSibling();

            RectTransform backgroundRoot = CreateContainer("Background", rootRt);
            CreateBackgroundTracks(backgroundRoot, lineSprite);

            List<Image> ghostLeft = CreateChain(
                "GhostLeft", rootRt, lineSprite,
                WithAlpha(UiTheme.Filet, 0.22f), isLeft: true);

            List<Image> ghostRight = CreateChain(
                "GhostRight", rootRt, lineSprite,
                WithAlpha(UiTheme.Filet, 0.22f), isLeft: false);

            List<Image> fillLeft = CreateChain(
                "FillLeft", rootRt, lineSprite, Color.white, isLeft: true);

            List<Image> fillRight = CreateChain(
                "FillRight", rootRt, lineSprite, Color.white, isLeft: false);

            PressurePerimeterUI ui = rootGo.GetComponent<PressurePerimeterUI>();
            RectTransform headerRt = FindChildRect(hudParent, HeaderBarObjectName);
            WirePerimeterUI(ui, fillLeft, fillRight, ghostLeft, ghostRight, headerRt);

            rootGo.SetActive(true);

            float leftLen = SumSegmentLengths(fillLeft);
            float rightLen = SumSegmentLengths(fillRight);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = rootGo;

            string headerInfo = headerRt != null
                ? $"HeaderBar lié (top auto +{TOP_GAP}px)"
                : "HeaderBar introuvable — fallback inset plein écran";

            Debug.Log(
                $"[PressurePerimeterGenerator] Périmètre — {PerimeterObjectName}, " +
                $"inset {INSET}px, épaisseur {THICKNESS}px. {headerInfo}. " +
                $"Longueurs chaînes G/D : {leftLen:F0}px / {rightLen:F0}px.");
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
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

        private static void DestroyExistingPressureUi()
        {
            PressurePerimeterUI[] perimeters = Object.FindObjectsOfType<PressurePerimeterUI>(true);
            for (int i = 0; i < perimeters.Length; i++)
            {
                if (perimeters[i] != null)
                    Undo.DestroyObjectImmediate(perimeters[i].gameObject);
            }

            PressureGaugeUI[] bars = Object.FindObjectsOfType<PressureGaugeUI>(true);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] != null)
                    Undo.DestroyObjectImmediate(bars[i].gameObject);
            }

            Transform legacy = FindLegacyByName(LegacyBarObjectName);
            if (legacy != null)
                Undo.DestroyObjectImmediate(legacy.gameObject);

            Transform legacyPerimeter = FindLegacyByName(PerimeterObjectName);
            if (legacyPerimeter != null)
                Undo.DestroyObjectImmediate(legacyPerimeter.gameObject);
        }

        private static Transform FindLegacyByName(string objectName)
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                Transform[] all = roots[r].GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == objectName)
                        return all[i];
                }
            }

            return null;
        }

        private static RectTransform FindChildRect(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == childName)
                    return child as RectTransform;
            }

            return null;
        }

        private static void ConfigureRootRect(RectTransform rootRt)
        {
            // Placeholder — PressurePerimeterUI.TryFitBelowHeader recalcule le top au Play.
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.pivot = new Vector2(0.5f, 0.5f);
            rootRt.anchoredPosition = Vector2.zero;
            rootRt.offsetMin = new Vector2(INSET, INSET);
            rootRt.offsetMax = new Vector2(-INSET, -INSET);
            rootRt.sizeDelta = Vector2.zero;
        }

        private static RectTransform CreateContainer(string name, RectTransform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Créer {name}");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        /// <summary>Track de fond : rectangle fermé, toujours visible (jauge 0 %).</summary>
        private static void CreateBackgroundTracks(RectTransform parent, Sprite sprite)
        {
            Color bg = WithAlpha(UiTheme.Filet, TRACK_ALPHA);

            CreateSimpleBar("BottomTrack", parent, sprite, bg,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, THICKNESS));

            CreateSimpleBar("TopTrack", parent, sprite, bg,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, THICKNESS));

            CreateSimpleBar("LeftTrack", parent, sprite, bg,
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(THICKNESS, 0f));

            CreateSimpleBar("RightTrack", parent, sprite, bg,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f),
                new Vector2(THICKNESS, 0f));
        }

        private static void CreateSimpleBar(
            string name,
            RectTransform parent,
            Sprite sprite,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Créer {name}");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
        }

        private static List<Image> CreateChain(
            string containerName,
            RectTransform root,
            Sprite sprite,
            Color color,
            bool isLeft)
        {
            RectTransform container = CreateContainer(containerName, root);
            List<Image> chain = new List<Image>(3);
            float corner = CORNER_OVERLAP;

            if (isLeft)
            {
                // Bas-centre → bas-gauche (remplit vers la gauche)
                chain.Add(CreateFilledBar(
                    "BasGaucheDemi", container, sprite, color,
                    new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                    new Vector2(corner, THICKNESS),
                    Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Right));

                // Bord gauche complet
                chain.Add(CreateFilledBar(
                    "BordGauche", container, sprite, color,
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f),
                    new Vector2(THICKNESS, 0f),
                    Image.FillMethod.Vertical, (int)Image.OriginVertical.Bottom));

                // Haut-gauche → haut-centre (remplit vers la droite)
                chain.Add(CreateFilledBar(
                    "HautGaucheDemi", container, sprite, color,
                    new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
                    new Vector2(corner, THICKNESS),
                    Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Left));
            }
            else
            {
                chain.Add(CreateFilledBar(
                    "BasDroitDemi", container, sprite, color,
                    new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                    new Vector2(corner, THICKNESS),
                    Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Left));

                chain.Add(CreateFilledBar(
                    "BordDroit", container, sprite, color,
                    new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f),
                    new Vector2(THICKNESS, 0f),
                    Image.FillMethod.Vertical, (int)Image.OriginVertical.Bottom));

                chain.Add(CreateFilledBar(
                    "HautDroitDemi", container, sprite, color,
                    new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                    new Vector2(corner, THICKNESS),
                    Image.FillMethod.Horizontal, (int)Image.OriginHorizontal.Right));
            }

            return chain;
        }

        private static Image CreateFilledBar(
            string name,
            RectTransform parent,
            Sprite sprite,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Image.FillMethod fillMethod,
            int fillOrigin)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, $"Créer {name}");
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = fillMethod;
            img.fillOrigin = fillOrigin;
            img.fillAmount = 0f;
            img.color = color;
            img.raycastTarget = false;

            return img;
        }

        private static float SumSegmentLengths(List<Image> chain)
        {
            float total = 0f;
            for (int i = 0; i < chain.Count; i++)
            {
                if (chain[i] == null)
                    continue;

                RectTransform rt = chain[i].rectTransform;
                total += chain[i].fillMethod == Image.FillMethod.Horizontal
                    ? rt.rect.width
                    : rt.rect.height;
            }

            return total;
        }

        private static void WirePerimeterUI(
            PressurePerimeterUI ui,
            List<Image> leftChain,
            List<Image> rightChain,
            List<Image> ghostLeftChain,
            List<Image> ghostRightChain,
            RectTransform headerRt)
        {
            if (ui == null)
                return;

            SerializedObject so = new SerializedObject(ui);
            WireImageList(so, "leftChain", leftChain);
            WireImageList(so, "rightChain", rightChain);
            WireImageList(so, "ghostLeftChain", ghostLeftChain);
            WireImageList(so, "ghostRightChain", ghostRightChain);

            UiGen.Wire(so, "leftTip", null);
            UiGen.Wire(so, "rightTip", null);
            UiGen.Wire(so, "leftTipImage", null);
            UiGen.Wire(so, "rightTipImage", null);
            UiGen.Wire(so, "topBoundary", headerRt);

            SetFloat(so, "edgeInset", INSET);
            SetFloat(so, "topGap", TOP_GAP);

            SerializedProperty gradProp = so.FindProperty("fillGradient");
            if (gradProp != null)
                ApplyPerimeterGradient(gradProp);

            SetFloat(so, "thicknessBase", THICKNESS);
            SetFloat(so, "thicknessAlert", 5f);
            SetFloat(so, "alertZoneStart", 0.75f);
            SetFloat(so, "pulseSpeed", 1.6f);
            SetFloat(so, "pulseAmplitude", 0.05f);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyPerimeterGradient(SerializedProperty gradProp)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(UiTheme.Filet, 0f),
                    new GradientColorKey(UiTheme.AccentSection, 0.5f),
                    new GradientColorKey(UiTheme.SuperLancerZone, 0.75f),
                    new GradientColorKey(UiTheme.Negative, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.88f, 0f),
                    new GradientAlphaKey(0.92f, 0.5f),
                    new GradientAlphaKey(1f, 0.75f),
                    new GradientAlphaKey(1f, 1f)
                });

            WriteGradientKeys(gradProp, g);
        }

        private static void WriteGradientKeys(SerializedProperty gradProp, Gradient g)
        {
            GradientColorKey[] ck = g.colorKeys;
            GradientAlphaKey[] ak = g.alphaKeys;

            for (int i = 0; i < 8; i++)
            {
                SerializedProperty key = gradProp.FindPropertyRelative("key" + i);
                if (key == null)
                    continue;

                if (i < ck.Length)
                {
                    Color c = ck[i].color;
                    float a = i < ak.Length ? ak[i].alpha : c.a;
                    key.colorValue = new Color(c.r, c.g, c.b, a);
                }
                else
                {
                    key.colorValue = Color.clear;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                SerializedProperty ctime = gradProp.FindPropertyRelative("ctime" + i);
                if (ctime == null)
                    continue;
                ctime.intValue = i < ck.Length
                    ? Mathf.RoundToInt(ck[i].time * 65535f)
                    : 0;
            }

            for (int i = 0; i < 8; i++)
            {
                SerializedProperty atime = gradProp.FindPropertyRelative("atime" + i);
                if (atime == null)
                    continue;
                atime.intValue = i < ak.Length
                    ? Mathf.RoundToInt(ak[i].time * 65535f)
                    : 0;
            }

            SerializedProperty numColor = gradProp.FindPropertyRelative("m_NumColorKeys");
            SerializedProperty numAlpha = gradProp.FindPropertyRelative("m_NumAlphaKeys");
            if (numColor != null)
                numColor.intValue = ck.Length;
            if (numAlpha != null)
                numAlpha.intValue = ak.Length;
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null)
                p.floatValue = value;
        }

        private static void WireImageList(SerializedObject so, string propertyName, List<Image> images)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[PressurePerimeterGenerator] Champ '{propertyName}' introuvable.");
                return;
            }

            prop.ClearArray();
            if (images == null)
                return;

            for (int i = 0; i < images.Count; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
        }
    }
}
#endif
