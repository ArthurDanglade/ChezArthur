#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte le panneau BossHPBarUI sous le canvas HUD et branche HPBarManager.bossBar.
    /// </summary>
    public static class BossHPBarUIBuilder
    {
        private const string PanelName = "BossHPBarPanel";
        private const string ManagerName = "HPBarManager";

        [MenuItem("Chez Arthur/UI/Monter BossHPBarUI (boss)")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Monter BossHPBarUI");
            int undoGroup = Undo.GetCurrentGroup();

            Canvas canvas = FindMainCanvas();
            if (canvas == null)
            {
                Debug.LogError("[BossHPBarUIBuilder] Aucun Canvas HUD trouvé dans la scène.");
                return;
            }

            BossHPBarUI bossBar = EnsureBossPanel(canvas);
            WireHPBarManager(bossBar);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            if (bossBar != null)
                Selection.activeGameObject = bossBar.gameObject;

            Debug.Log("[BossHPBarUIBuilder] Montage terminé (panneau boss + HPBarManager.bossBar).");
        }

        private static Canvas FindMainCanvas()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].renderMode != RenderMode.WorldSpace)
                    return canvases[i];
            }

            return canvases.Length > 0 ? canvases[0] : null;
        }

        private static BossHPBarUI EnsureBossPanel(Canvas canvas)
        {
            BossHPBarUI existing = Object.FindObjectOfType<BossHPBarUI>(true);
            if (existing != null)
            {
                UpgradeLayout(existing);
                WireBossBarUI(existing);
                SetPanelHidden(existing);
                return existing;
            }

            Sprite card = UiGen.Card;
            Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject panelGo = new GameObject(
                PanelName,
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(BossHPBarUI));
            Undo.RegisterCreatedObjectUndo(panelGo, "Créer BossHPBarPanel");
            panelGo.transform.SetParent(canvas.transform, false);

            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -168f);
            panelRt.sizeDelta = new Vector2(660f, 78f);
            panelRt.SetAsLastSibling();
            panelGo.SetActive(true);

            CanvasGroup group = panelGo.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            TextMeshProUGUI bossName = CreateText(
                "BossName",
                panelRt,
                "BOSS",
                26f,
                UiTheme.Gold,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            RectTransform nameRt = bossName.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -2f);
            nameRt.sizeDelta = new Vector2(0f, 32f);

            RectTransform trackRt = CreateBarTrack(panelRt, card, uiSprite, out Image ghostFill, out Image fill);

            BossHPBarUI ui = panelGo.GetComponent<BossHPBarUI>();
            WireBossBarUI(ui, group, panelRt, bossName, fill, ghostFill);
            SetPanelHidden(ui);

            return ui;
        }

        private static void UpgradeLayout(BossHPBarUI ui)
        {
            if (ui == null)
                return;

            RectTransform panelRt = ui.transform as RectTransform;
            if (panelRt == null)
                return;

            Undo.RecordObject(panelRt, "Recentrer panneau boss");
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.anchoredPosition = new Vector2(0f, -168f);
            panelRt.sizeDelta = new Vector2(660f, 78f);

            TextMeshProUGUI bossName = ui.transform.Find("BossName")?.GetComponent<TextMeshProUGUI>();
            Image fill = ui.transform.Find("BarTrack/Fill")?.GetComponent<Image>();
            Image ghost = ui.transform.Find("BarTrack/GhostFill")?.GetComponent<Image>();
            CanvasGroup group = ui.GetComponent<CanvasGroup>();

            if (bossName == null || fill == null)
            {
                Debug.LogWarning("[BossHPBarUIBuilder] Hiérarchie incomplète — recrée le panneau ou complète à la main.");
                return;
            }

            WireBossBarUI(ui, group, panelRt, bossName, fill, ghost);
        }

        private static RectTransform CreateBarTrack(
            RectTransform parent,
            Sprite card,
            Sprite uiSprite,
            out Image ghostFill,
            out Image fill)
        {
            GameObject trackGo = new GameObject("BarTrack", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(trackGo, "Créer piste barre boss");
            trackGo.transform.SetParent(parent, false);

            RectTransform trackRt = trackGo.GetComponent<RectTransform>();
            trackRt.anchorMin = new Vector2(0f, 0f);
            trackRt.anchorMax = new Vector2(1f, 0f);
            trackRt.pivot = new Vector2(0.5f, 0f);
            trackRt.anchoredPosition = new Vector2(0f, 6f);
            trackRt.sizeDelta = new Vector2(-24f, 22f);

            Image trackImg = trackGo.AddComponent<Image>();
            trackImg.sprite = card;
            trackImg.type = Image.Type.Sliced;
            trackImg.color = UiTheme.SurfaceBar;
            trackImg.raycastTarget = false;

            ghostFill = CreateFilledBar("GhostFill", trackRt, uiSprite, new Color(0.88f, 0.45f, 0.38f, 0.85f));
            fill = CreateFilledBar("Fill", trackRt, uiSprite, UiTheme.Negative);

            return trackRt;
        }

        private static Image CreateFilledBar(string name, RectTransform parent, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(-8f, 14f);

            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.color = color;
            img.raycastTarget = false;

            return img;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            RectTransform parent,
            string text,
            float size,
            Color color,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null)
                tmp.font = font;

            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;

            return tmp;
        }

        private static void WireBossBarUI(BossHPBarUI ui)
        {
            if (ui == null)
                return;

            CanvasGroup group = ui.GetComponent<CanvasGroup>();
            RectTransform panelRt = ui.transform as RectTransform;
            TextMeshProUGUI bossName = ui.transform.Find("BossName")?.GetComponent<TextMeshProUGUI>();
            Image fill = ui.transform.Find("BarTrack/Fill")?.GetComponent<Image>();
            Image ghost = ui.transform.Find("BarTrack/GhostFill")?.GetComponent<Image>();

            WireBossBarUI(ui, group, panelRt, bossName, fill, ghost);
        }

        private static void WireBossBarUI(
            BossHPBarUI ui,
            CanvasGroup group,
            RectTransform panelRt,
            TextMeshProUGUI bossName,
            Image fill,
            Image ghost)
        {
            if (ui == null)
                return;

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("canvasGroup").objectReferenceValue = group;
            so.FindProperty("panelRoot").objectReferenceValue = panelRt;
            so.FindProperty("bossNameText").objectReferenceValue = bossName;
            so.FindProperty("fillImage").objectReferenceValue = fill;
            so.FindProperty("ghostFillImage").objectReferenceValue = ghost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHPBarManager(BossHPBarUI bossBar)
        {
            if (bossBar == null)
                return;

            HPBarManager manager = Object.FindObjectOfType<HPBarManager>(true);
            if (manager == null)
            {
                Debug.LogWarning(
                    $"[BossHPBarUIBuilder] {ManagerName} introuvable — lance aussi « Monter HPBarManager (ennemis) » ou assigne bossBar manuellement.");
                return;
            }

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("bossBar").objectReferenceValue = bossBar;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPanelHidden(BossHPBarUI ui)
        {
            if (ui == null)
                return;

            // Host actif pour les coroutines ; masquage via CanvasGroup uniquement.
            ui.gameObject.SetActive(true);

            CanvasGroup group = ui.GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }
    }
}
#endif
