#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.Roguelike;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Construit la section Synergies du menu pause (onglet ÉQUIPE) dans Game.unity.
    /// Idempotent : crée ou met à jour header/content, câblage et ordre des siblings.
    /// Menu : Chez Arthur > UI > Build Pause Synergies Section.
    /// </summary>
    public static class SynergyPauseSectionBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/SynergyEntryCard.prefab";

        private const string TeamPanelName = "TeamPanel";
        private const string ScrollViewName = "Scroll View";
        private const string ViewportName = "Viewport";
        private const string ContentName = "Content";

        private const string ValisesContentName = "ValisesContent";
        private const string SynergiesHeaderName = "SynergiesHeader";
        private const string HeaderLineName = "HeaderLine";
        private const string SynergiesContentName = "SynergiesContent";
        private const string EmptyLabelName = "EmptyLabel";

        private const string PersosHeaderName = "PersosHeader";
        private const string ValisesHeaderName = "ValisesHeader";
        private const string ItemsHeaderName = "ItemsHeader";

        [MenuItem("Chez Arthur/UI/Build Pause Synergies Section")]
        public static void Build()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogError("[SynergyPauseSectionBuilder] Aucune scène active.");
                return;
            }

            Transform teamPanel = FindTransformByName(scene, TeamPanelName);
            if (teamPanel == null)
            {
                Debug.LogError("[SynergyPauseSectionBuilder] TeamPanel introuvable — ouvre Game.unity.");
                return;
            }

            Transform content = FindScrollContent(teamPanel);
            if (content == null)
            {
                Debug.LogError("[SynergyPauseSectionBuilder] Content du scroll TeamPanel introuvable.");
                return;
            }

            Transform valisesContent = FindChildByTrimmedName(content, ValisesContentName);
            if (valisesContent == null)
            {
                Debug.LogError("[SynergyPauseSectionBuilder] ValisesContent introuvable sous Content.");
                return;
            }

            Undo.SetCurrentGroupName("Build Pause Synergies Section");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject entryPrefab = EnsureSynergyEntryPrefab();
            if (entryPrefab == null)
            {
                Debug.LogError("[SynergyPauseSectionBuilder] Impossible de créer ou charger SynergyEntryCard.prefab.");
                return;
            }

            RoguelikeSelectionPool valisePool = FindSceneComponent<RoguelikeSelectionPool>(scene);
            if (valisePool == null)
                Debug.LogWarning("[SynergyPauseSectionBuilder] RoguelikeSelectionPool introuvable — valisePool non câblé.");

            TMP_FontAsset font = LoadUiFont();
            int insertIndex = valisesContent.GetSiblingIndex() + 1;

            Transform synergiesHeader = EnsureSectionHeader(content, SynergiesHeaderName, "SYNERGIES", font);
            Transform synergiesContent = EnsureSynergiesContent(content, font);
            Transform synergiesLine = EnsureHeaderLineAfter(content, synergiesHeader);

            synergiesHeader.SetSiblingIndex(insertIndex);
            synergiesLine.SetSiblingIndex(insertIndex + 1);
            synergiesContent.SetSiblingIndex(insertIndex + 2);

            FixLegacyHeaderTexts(content, font);

            SynergySectionUI section = synergiesContent.GetComponent<SynergySectionUI>();
            if (section == null)
                section = Undo.AddComponent<SynergySectionUI>(synergiesContent.gameObject);

            Transform emptyLabel = FindChildByTrimmedName(synergiesContent, EmptyLabelName);
            if (emptyLabel == null)
            {
                Debug.LogError("[SynergyPauseSectionBuilder] EmptyLabel introuvable sous SynergiesContent.");
                return;
            }

            SerializedObject sectionSo = new SerializedObject(section);
            UiGen.Wire(sectionSo, "contentParent", synergiesContent);
            UiGen.Wire(sectionSo, "synergyEntryPrefab", entryPrefab);
            UiGen.Wire(sectionSo, "emptyLabel", emptyLabel.gameObject);
            UiGen.Wire(sectionSo, "valisePool", valisePool);
            sectionSo.ApplyModifiedPropertiesWithoutUndo();

            TeamPanelUI teamPanelUi = teamPanel.GetComponent<TeamPanelUI>();
            if (teamPanelUi == null)
            {
                Debug.LogError("[SynergyPauseSectionBuilder] TeamPanelUI introuvable sur TeamPanel.");
                return;
            }

            SerializedObject teamSo = new SerializedObject(teamPanelUi);
            UiGen.Wire(teamSo, "synergySection", section);
            teamSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = synergiesContent.gameObject;
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                "[SynergyPauseSectionBuilder] Section Synergies prête — siblings 6-7-8 sous ValisesContent, " +
                "TeamPanel.synergySection câblé. Sauvegarde la scène (Ctrl+S).");
        }

        private static GameObject EnsureSynergyEntryPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null && IsPrefabWired(prefab))
                return prefab;

            SynergyEntryCardGenerator.Generate();
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            return prefab != null && IsPrefabWired(prefab) ? prefab : null;
        }

        private static bool IsPrefabWired(GameObject prefab)
        {
            SynergyEntryUI entry = prefab.GetComponent<SynergyEntryUI>();
            if (entry == null)
                return false;

            SerializedObject so = new SerializedObject(entry);
            SerializedProperty nameText = so.FindProperty("nameText");
            return nameText != null && nameText.objectReferenceValue != null;
        }

        private static Transform EnsureSynergiesContent(Transform contentParent, TMP_FontAsset font)
        {
            Transform existing = FindChildByTrimmedName(contentParent, SynergiesContentName);
            GameObject go = existing != null
                ? existing.gameObject
                : CreateUiChild(contentParent, SynergiesContentName);
            RectTransform rt = go.GetComponent<RectTransform>();

            VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = Undo.AddComponent<VerticalLayoutGroup>(go);
                vlg.spacing = 8;
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
            {
                le = Undo.AddComponent<LayoutElement>(go);
                le.minHeight = 10;
            }

            if (go.GetComponent<SynergySectionUI>() == null)
                Undo.AddComponent<SynergySectionUI>(go);

            EnsureEmptyLabel(rt, font);

            return rt;
        }

        private static void EnsureEmptyLabel(Transform synergiesContent, TMP_FontAsset font)
        {
            Transform existing = FindChildByTrimmedName(synergiesContent, EmptyLabelName);
            GameObject emptyGo = existing != null
                ? existing.gameObject
                : CreateUiChild(synergiesContent, EmptyLabelName);

            TextMeshProUGUI emptyText = emptyGo.GetComponent<TextMeshProUGUI>();
            if (emptyText == null)
                emptyText = Undo.AddComponent<TextMeshProUGUI>(emptyGo);

            if (font != null)
                emptyText.font = font;
            emptyText.text = "Aucune synergie active";
            emptyText.fontSize = 18f;
            emptyText.color = UiTheme.TextSecondary;
            emptyText.fontStyle = FontStyles.Normal;
            emptyText.alignment = TextAlignmentOptions.MidlineLeft;
            emptyText.enableWordWrapping = false;
            emptyText.raycastTarget = false;

            if (emptyGo.GetComponent<LayoutElement>() == null)
                Undo.AddComponent<LayoutElement>(emptyGo);

            emptyGo.SetActive(false);
        }

        private static Transform EnsureSectionHeader(
            Transform contentParent,
            string objectName,
            string label,
            TMP_FontAsset font)
        {
            Transform existing = FindChildByTrimmedName(contentParent, objectName);
            GameObject go = existing != null
                ? existing.gameObject
                : CreateUiChild(contentParent, objectName);

            if (go.GetComponent<LayoutElement>() == null)
            {
                LayoutElement le = Undo.AddComponent<LayoutElement>(go);
                le.minHeight = 40;
                le.preferredHeight = 40;
            }

            TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
            if (text == null)
                text = Undo.AddComponent<TextMeshProUGUI>(go);

            if (font != null)
                text.font = font;
            text.text = label;
            text.fontSize = 24f;
            text.fontStyle = FontStyles.Bold;
            text.color = UiTheme.AccentSection;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableWordWrapping = false;
            text.raycastTarget = false;

            return go.transform;
        }

        private static Transform EnsureHeaderLineAfter(Transform contentParent, Transform header)
        {
            int lineIndex = header.GetSiblingIndex() + 1;
            if (lineIndex < contentParent.childCount)
            {
                Transform candidate = contentParent.GetChild(lineIndex);
                if (TrimName(candidate.name) == HeaderLineName)
                    return candidate;
            }

            GameObject go = CreateUiChild(contentParent, HeaderLineName);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minHeight = 2;
            le.preferredHeight = 2;
            le.flexibleWidth = 1f;

            Image img = go.AddComponent<Image>();
            img.color = UiTheme.Filet;
            img.raycastTarget = false;

            return go.transform;
        }

        private static void FixLegacyHeaderTexts(Transform content, TMP_FontAsset font)
        {
            SetHeaderLabel(content, PersosHeaderName, "PERSONNAGES", font);
            SetHeaderLabel(content, ValisesHeaderName, "VALISES", font);
            SetHeaderLabel(content, ItemsHeaderName, "ITEMS", font);
        }

        private static void SetHeaderLabel(Transform content, string headerName, string label, TMP_FontAsset font)
        {
            Transform header = FindChildByTrimmedName(content, headerName);
            if (header == null)
                return;

            TextMeshProUGUI text = header.GetComponent<TextMeshProUGUI>();
            if (text == null)
                return;

            if (font != null)
                text.font = font;
            text.text = label;
            text.color = UiTheme.AccentSection;
        }

        private static Transform FindScrollContent(Transform teamPanel)
        {
            Transform scroll = FindChildByTrimmedName(teamPanel, ScrollViewName);
            if (scroll == null)
                return null;

            Transform viewport = FindChildByTrimmedName(scroll, ViewportName);
            if (viewport == null)
                return null;

            return FindChildByTrimmedName(viewport, ContentName);
        }

        private static GameObject CreateUiChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }

        private static TMP_FontAsset LoadUiFont()
        {
            TMP_FontAsset font = UiGen.LoadFont();
            if (font != null)
                return font;

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }

            return null;
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
            if (TrimName(root.name) == TrimName(name))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindInHierarchy(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static Transform FindChildByTrimmedName(Transform parent, string name)
        {
            string target = TrimName(name);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (TrimName(child.name) == target)
                    return child;
            }

            return null;
        }

        private static string TrimName(string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Trim();
        }
    }
}
#endif
