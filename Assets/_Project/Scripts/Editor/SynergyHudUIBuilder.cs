#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Construit l'indicateur SynergyHud sous SafeArea du Canvas HUD (Game.unity).
    /// Idempotent : abandonne si SynergyHud existe déjà.
    /// </summary>
    public static class SynergyHudUIBuilder
    {
        private const string RootName = "SynergyHud";
        private const string ChipsRowName = "ChipsRow";
        private const string ChipTemplateName = "ChipTemplate";
        private const string DetailsPanelName = "DetailsPanel";
        private const string DetailsContainerName = "DetailsContainer";
        private const string DetailsRowTemplateName = "DetailsRowTemplate";

        private const float HudAnchorY = -180f;
        private const float DetailsPanelWidth = 880f;
        private const float DetailsPanelTopOffset = 8f;

        [MenuItem("Chez Arthur/UI/Build Synergy HUD")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[SynergyHudUIBuilder] Aucune scène active.");
                return;
            }

            Transform turnPill = FindTransformByName(scene, "TurnPill");
            if (turnPill == null)
            {
                Debug.LogError("[SynergyHudUIBuilder] TurnPill introuvable — ouvre Game.unity.");
                return;
            }

            Transform safeArea = turnPill.parent;
            if (safeArea == null)
            {
                Debug.LogError("[SynergyHudUIBuilder] Parent de TurnPill introuvable.");
                return;
            }

            if (FindChildByName(safeArea, RootName) != null)
            {
                Debug.LogWarning(
                    $"[SynergyHudUIBuilder] '{RootName}' existe déjà sous '{safeArea.name}' — abandon (idempotent).");
                return;
            }

            Undo.SetCurrentGroupName("Build Synergy HUD");
            int undoGroup = Undo.GetCurrentGroup();

            Sprite cardSprite = UiGen.Card;
            TMP_FontAsset font = LoadHudFont();
            Color surfaceBar = UiTheme.SurfaceBar;
            surfaceBar.a = 0.96f;

            // ── Racine ──
            GameObject rootGo = CreateUiChild(safeArea, RootName);
            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 1f);
            rootRt.anchorMax = new Vector2(0.5f, 1f);
            rootRt.pivot = new Vector2(0.5f, 1f);
            rootRt.anchoredPosition = new Vector2(0f, HudAnchorY);
            rootRt.sizeDelta = Vector2.zero;

            SynergyHudUI hud = rootGo.AddComponent<SynergyHudUI>();

            int turnPillIndex = turnPill.GetSiblingIndex();
            rootGo.transform.SetSiblingIndex(turnPillIndex + 1);

            // ── ChipsRow ──
            GameObject chipsRowGo = CreateUiChild(rootRt, ChipsRowName);
            RectTransform chipsRowRt = chipsRowGo.GetComponent<RectTransform>();
            chipsRowRt.anchorMin = new Vector2(0.5f, 1f);
            chipsRowRt.anchorMax = new Vector2(0.5f, 1f);
            chipsRowRt.pivot = new Vector2(0.5f, 1f);
            chipsRowRt.anchoredPosition = Vector2.zero;
            chipsRowRt.sizeDelta = Vector2.zero;

            Image chipsRowBg = chipsRowGo.AddComponent<Image>();
            chipsRowBg.sprite = cardSprite;
            chipsRowBg.type = Image.Type.Sliced;
            chipsRowBg.color = surfaceBar;
            chipsRowBg.raycastTarget = true;

            HorizontalLayoutGroup chipsHlg = chipsRowGo.AddComponent<HorizontalLayoutGroup>();
            chipsHlg.padding = new RectOffset(8, 8, 8, 8);
            chipsHlg.spacing = 12;
            chipsHlg.childAlignment = TextAnchor.MiddleCenter;
            chipsHlg.childControlWidth = true;
            chipsHlg.childControlHeight = true;
            chipsHlg.childForceExpandWidth = false;
            chipsHlg.childForceExpandHeight = false;

            ContentSizeFitter chipsCsf = chipsRowGo.AddComponent<ContentSizeFitter>();
            chipsCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            chipsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SynergyChipsPressHandler pressHandler = chipsRowGo.AddComponent<SynergyChipsPressHandler>();

            // ── ChipTemplate ──
            GameObject chipTemplateGo = CreateUiChild(chipsRowRt, ChipTemplateName);
            chipTemplateGo.SetActive(false);

            Image chipBg = chipTemplateGo.AddComponent<Image>();
            chipBg.sprite = cardSprite;
            chipBg.type = Image.Type.Sliced;
            chipBg.color = UiTheme.Frame;
            chipBg.raycastTarget = false;

            HorizontalLayoutGroup chipHlg = chipTemplateGo.AddComponent<HorizontalLayoutGroup>();
            chipHlg.padding = new RectOffset(12, 12, 6, 6);
            chipHlg.childAlignment = TextAnchor.MiddleCenter;
            chipHlg.childControlWidth = true;
            chipHlg.childControlHeight = true;
            chipHlg.childForceExpandWidth = false;
            chipHlg.childForceExpandHeight = false;

            ContentSizeFitter chipCsf = chipTemplateGo.AddComponent<ContentSizeFitter>();
            chipCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            chipCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI chipText = CreateTmp(chipTemplateGo.transform, "ChipText", font, UiTheme.FontLabel);
            chipText.text = "Synergie";
            chipText.color = UiTheme.Gold;
            chipText.fontStyle = FontStyles.Bold;
            chipText.alignment = TextAlignmentOptions.Center;

            // ── DetailsPanel ──
            GameObject detailsGo = CreateUiChild(rootRt, DetailsPanelName);
            RectTransform detailsRt = detailsGo.GetComponent<RectTransform>();
            detailsRt.anchorMin = new Vector2(0.5f, 1f);
            detailsRt.anchorMax = new Vector2(0.5f, 1f);
            detailsRt.pivot = new Vector2(0.5f, 1f);
            detailsRt.anchoredPosition = new Vector2(0f, -DetailsPanelTopOffset);
            detailsRt.sizeDelta = new Vector2(DetailsPanelWidth, 0f);

            CanvasGroup detailsCg = detailsGo.AddComponent<CanvasGroup>();
            detailsCg.alpha = 0f;
            detailsCg.blocksRaycasts = false;
            detailsCg.interactable = false;

            Image detailsBg = detailsGo.AddComponent<Image>();
            detailsBg.sprite = cardSprite;
            detailsBg.type = Image.Type.Sliced;
            detailsBg.color = surfaceBar;
            detailsBg.raycastTarget = false;

            VerticalLayoutGroup detailsVlg = detailsGo.AddComponent<VerticalLayoutGroup>();
            detailsVlg.padding = new RectOffset(16, 16, 12, 12);
            detailsVlg.spacing = UiTheme.SpacingRow;
            detailsVlg.childAlignment = TextAnchor.UpperCenter;
            detailsVlg.childControlWidth = true;
            detailsVlg.childControlHeight = true;
            detailsVlg.childForceExpandWidth = true;
            detailsVlg.childForceExpandHeight = false;

            ContentSizeFitter detailsCsf = detailsGo.AddComponent<ContentSizeFitter>();
            detailsCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            detailsCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── DetailsContainer ──
            GameObject detailsContainerGo = CreateUiChild(detailsRt, DetailsContainerName);
            RectTransform detailsContainerRt = detailsContainerGo.GetComponent<RectTransform>();
            StretchFull(detailsContainerRt);

            VerticalLayoutGroup containerVlg = detailsContainerGo.AddComponent<VerticalLayoutGroup>();
            containerVlg.spacing = UiTheme.SpacingRow;
            containerVlg.childAlignment = TextAnchor.UpperLeft;
            containerVlg.childControlWidth = true;
            containerVlg.childControlHeight = true;
            containerVlg.childForceExpandWidth = true;
            containerVlg.childForceExpandHeight = false;

            ContentSizeFitter containerCsf = detailsContainerGo.AddComponent<ContentSizeFitter>();
            containerCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── DetailsRowTemplate ──
            GameObject rowTemplateGo = CreateUiChild(detailsContainerRt, DetailsRowTemplateName);
            rowTemplateGo.SetActive(false);

            VerticalLayoutGroup rowVlg = rowTemplateGo.AddComponent<VerticalLayoutGroup>();
            rowVlg.spacing = 4;
            rowVlg.childAlignment = TextAnchor.UpperLeft;
            rowVlg.childControlWidth = true;
            rowVlg.childControlHeight = true;
            rowVlg.childForceExpandWidth = true;
            rowVlg.childForceExpandHeight = false;

            TextMeshProUGUI rowName = CreateTmp(rowTemplateGo.transform, "NameText", font, UiTheme.FontBody);
            rowName.text = "Nom synergie";
            rowName.color = UiTheme.Gold;
            rowName.fontStyle = FontStyles.Bold;
            rowName.alignment = TextAlignmentOptions.TopLeft;
            rowName.enableWordWrapping = false;

            TextMeshProUGUI rowDesc = CreateTmp(rowTemplateGo.transform, "DescriptionText", font, 20f);
            rowDesc.text = "Description de la synergie.";
            rowDesc.color = UiTheme.TextSecondary;
            rowDesc.fontStyle = FontStyles.Normal;
            rowDesc.alignment = TextAlignmentOptions.TopLeft;
            rowDesc.enableWordWrapping = true;

            // ── Wiring ──
            pressHandler.SetOwner(hud);

            SerializedObject so = new SerializedObject(hud);
            UiGen.Wire(so, "chipsContainer", chipsRowRt);
            UiGen.Wire(so, "chipTemplate", chipTemplateGo);
            UiGen.Wire(so, "detailsPanel", detailsCg);
            UiGen.Wire(so, "detailsContainer", detailsContainerRt);
            UiGen.Wire(so, "detailsRowTemplate", rowTemplateGo);
            UiGen.Wire(so, "pressHandler", pressHandler);
            so.ApplyModifiedPropertiesWithoutUndo();

            LayoutRebuilder.ForceRebuildLayoutImmediate(chipsRowRt);
            float chipsHeight = chipsRowRt.rect.height;
            detailsRt.anchoredPosition = new Vector2(0f, -(chipsHeight + DetailsPanelTopOffset));

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = rootGo;
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"[SynergyHudUIBuilder] '{RootName}' créé sous '{safeArea.name}' (sibling après TurnPill). " +
                "Sauvegarde la scène après test Play.");
        }

        private static GameObject CreateUiChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, TMP_FontAsset font, float fontSize)
        {
            GameObject go = CreateUiChild(parent, name);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_FontAsset LoadHudFont()
        {
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null)
                return font;

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static Transform FindTransformByName(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindInHierarchy(roots[i].transform, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindInHierarchy(Transform root, string name)
        {
            if (root.name == name)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindInHierarchy(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }
    }
}
#endif
