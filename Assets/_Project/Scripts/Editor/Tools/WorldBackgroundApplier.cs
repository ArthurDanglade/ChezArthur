using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Hub;
using ChezArthur.UI;

namespace ChezArthur.Editor.Tools
{
    /// <summary>
    /// Fenetre editeur : applique un WorldBackgroundDefinition sur un ParallaxManager.
    /// </summary>
    public class WorldBackgroundApplier : EditorWindow
    {
        // ===========================================
        // STATE
        // ===========================================

        private ParallaxManager _manager;
        private WorldBackgroundDefinition _definition;
        private string _report = string.Empty;
        private Vector2 _reportScroll;

        // ===========================================
        // MENU
        // ===========================================

        [MenuItem("ChezArthur/Hub/World Background Applier")]
        public static void Open()
        {
            WorldBackgroundApplier window = GetWindow<WorldBackgroundApplier>();
            window.titleContent = new GUIContent("World Background Applier");
            window.minSize = new Vector2(360f, 280f);
            window.Show();
        }

        // ===========================================
        // GUI
        // ===========================================

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Cibles", EditorStyles.boldLabel);

            _manager = (ParallaxManager)EditorGUILayout.ObjectField(
                "Parallax Manager",
                _manager,
                typeof(ParallaxManager),
                true);

            if (_manager == null)
            {
                if (GUILayout.Button("Chercher dans la scene"))
                    FindManagerInScene();
            }
            else if (GUILayout.Button("Reselectionner (actif Accueil)"))
            {
                FindManagerInScene();
            }

            _definition = (WorldBackgroundDefinition)EditorGUILayout.ObjectField(
                "Definition",
                _definition,
                typeof(WorldBackgroundDefinition),
                false);

            EditorGUILayout.Space(8f);

            EditorGUI.BeginDisabledGroup(_manager == null || _definition == null);
            if (GUILayout.Button("Appliquer", GUILayout.Height(28f)))
                Apply();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Rapport", EditorStyles.boldLabel);
            _reportScroll = EditorGUILayout.BeginScrollView(
                _reportScroll,
                GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(
                _report,
                GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ===========================================
        // ACTIONS
        // ===========================================

        private void FindManagerInScene()
        {
            ParallaxManager[] all = Object.FindObjectsOfType<ParallaxManager>(true);
            if (all == null || all.Length == 0)
            {
                _manager = null;
                _report = "Aucun ParallaxManager trouve dans la scene.";
                return;
            }

            // Preferer le manager ACTIF sous HomeIllustrationRig / Framing.
            ParallaxManager preferred = null;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || !all[i].gameObject.activeInHierarchy)
                    continue;
                if (all[i].GetComponentInParent<HomeIllustrationFraming>() == null)
                    continue;
                preferred = all[i];
                break;
            }

            if (preferred == null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].gameObject.activeInHierarchy)
                    {
                        preferred = all[i];
                        break;
                    }
                }
            }

            if (preferred == null)
                preferred = all[0];

            _manager = preferred;

            StringBuilder sb = new StringBuilder(256);
            sb.Append("ParallaxManager selectionne : ")
                .Append(GetTransformPath(_manager.transform))
                .Append(" active=")
                .Append(_manager.gameObject.activeInHierarchy)
                .Append('\n');
            sb.Append("Total trouves : ").Append(all.Length).Append('\n');

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;
                bool isSel = all[i] == _manager;
                sb.Append(isSel ? "  >> [" : "  [").Append(i).Append("] ")
                    .Append(GetTransformPath(all[i].transform))
                    .Append(" active=")
                    .Append(all[i].gameObject.activeInHierarchy)
                    .Append('\n');
            }

            _report = sb.ToString();
        }

        private void Apply()
        {
            if (_manager == null || _definition == null)
            {
                _report =
                    "Appliquer annule : ParallaxManager et/ou Definition manquant.";
                return;
            }

            if (!_manager.gameObject.activeInHierarchy)
            {
                _report =
                    "Appliquer annule : le ParallaxManager cible est INACTIF.\n"
                    + "Path : " + GetTransformPath(_manager.transform) + "\n"
                    + "Clique 'Reselectionner (actif Accueil)' puis reessaie.";
                return;
            }

            // Framing d'abord : le layout des calques doit utiliser la taille finale.
            Canvas.ForceUpdateCanvases();
            HomeIllustrationFraming framing =
                _manager.GetComponentInParent<HomeIllustrationFraming>();
            if (framing != null)
                framing.Refresh();
            Canvas.ForceUpdateCanvases();

            LayerSnapshot[] before = CaptureManagerLayers(_manager);
            _manager.ApplyDefinition(_definition);
            _manager.RelayoutToCurrentRect();
            Canvas.ForceUpdateCanvases();

            EditorUtility.SetDirty(_manager);
            if (_manager.gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    _manager.gameObject.scene);

            _report = BuildReport(_manager, _definition, before);
        }

        // ===========================================
        // SNAPSHOT / RAPPORT
        // ===========================================

        private struct LayerSnapshot
        {
            public string textureName;
            public int width;
            public int height;
        }

        private static LayerSnapshot[] CaptureManagerLayers(ParallaxManager manager)
        {
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty layersProp = so.FindProperty("layers");
            if (layersProp == null)
                return new LayerSnapshot[0];

            int count = layersProp.arraySize;
            LayerSnapshot[] snaps = new LayerSnapshot[count];

            for (int i = 0; i < count; i++)
            {
                SerializedProperty entry = layersProp.GetArrayElementAtIndex(i);
                SerializedProperty imageProp = entry.FindPropertyRelative("image");
                RawImage image = imageProp != null
                    ? imageProp.objectReferenceValue as RawImage
                    : null;

                Texture tex = image != null ? image.texture : null;
                snaps[i].textureName = tex != null ? tex.name : "(null)";
                snaps[i].width = tex != null ? tex.width : 0;
                snaps[i].height = tex != null ? tex.height : 0;
            }

            return snaps;
        }

        private static string BuildReport(
            ParallaxManager manager,
            WorldBackgroundDefinition definition,
            LayerSnapshot[] before)
        {
            StringBuilder sb = new StringBuilder(1024);

            WorldBackgroundDefinition.LayerEntry[] defLayers = definition.Layers;
            int defCount = defLayers != null ? defLayers.Length : 0;

            LayerSnapshot[] after = CaptureManagerLayers(manager);
            int managerCount = after.Length;

            sb.Append("Definition : ").Append(definition.name).Append('\n');
            sb.Append("worldId : ").Append(definition.WorldId).Append('\n');
            sb.Append("Manager path : ")
                .Append(GetTransformPath(manager.transform))
                .Append('\n');
            sb.Append("Calques definition : ").Append(defCount).Append('\n');
            sb.Append("Calques ParallaxManager : ").Append(managerCount).Append('\n');

            RectTransform root = manager.RootRect;
            bool skyOk = false;
            if (root != null)
            {
                RectTransform rig = root.parent as RectTransform;
                sb.Append("LandscapeLayer.rect : ")
                    .Append(root.rect.width.ToString("0.#")).Append('x')
                    .Append(root.rect.height.ToString("0.#")).Append('\n');
                if (rig != null)
                {
                    sb.Append("HomeIllustrationRig : anchors=(")
                        .Append(rig.anchorMin.x.ToString("0.##")).Append('-')
                        .Append(rig.anchorMax.x.ToString("0.##"))
                        .Append(") sizeDelta=")
                        .Append(rig.sizeDelta.x.ToString("0.#")).Append('x')
                        .Append(rig.sizeDelta.y.ToString("0.#"))
                        .Append(" rect=")
                        .Append(rig.rect.width.ToString("0.#")).Append('x')
                        .Append(rig.rect.height.ToString("0.#"))
                        .Append('\n');
                }

                if (managerCount > 0)
                {
                    SerializedObject so = new SerializedObject(manager);
                    SerializedProperty layersProp = so.FindProperty("layers");
                    if (layersProp != null && layersProp.arraySize > 0)
                    {
                        SerializedProperty imgProp = layersProp
                            .GetArrayElementAtIndex(0)
                            .FindPropertyRelative("image");
                        RawImage sky = imgProp != null
                            ? imgProp.objectReferenceValue as RawImage
                            : null;
                        if (sky != null)
                        {
                            RectTransform srt = sky.rectTransform;
                            sb.Append("Sky layer : anchors=(")
                                .Append(srt.anchorMin.x.ToString("0.##")).Append('-')
                                .Append(srt.anchorMax.x.ToString("0.##"))
                                .Append(") sizeDelta=")
                                .Append(srt.sizeDelta.x.ToString("0.#")).Append('x')
                                .Append(srt.sizeDelta.y.ToString("0.#"))
                                .Append(" rect=")
                                .Append(srt.rect.width.ToString("0.#")).Append('x')
                                .Append(srt.rect.height.ToString("0.#"))
                                .Append('\n');

                            skyOk = Mathf.Approximately(srt.anchorMin.x, 0f)
                                && Mathf.Approximately(srt.anchorMax.x, 1f)
                                && Mathf.Approximately(srt.sizeDelta.x, 0f);
                        }
                    }
                }
            }

            if (!skyOk)
            {
                sb.Append(
                    "ALERTE LAYOUT : Sky n'est PAS en stretch pleine largeur "
                    + "(attendu anchors 0-1, sizeDelta.x=0).\n"
                    + "→ Unity n'a peut-etre pas recompile ParallaxManager. "
                    + "Attends la recompile puis re-Apply.\n");
            }
            else
            {
                sb.Append("LAYOUT OK : Sky stretch pleine largeur.\n");
            }

            if (defCount > managerCount)
            {
                sb.Append("AVERTISSEMENT : definition ")
                    .Append(defCount)
                    .Append(" calques > manager ")
                    .Append(managerCount)
                    .Append(".\n");
            }

            sb.Append('\n');

            int lineCount = managerCount;
            if (defCount > lineCount)
                lineCount = defCount;

            for (int i = 0; i < lineCount; i++)
            {
                string beforeName = "(n/a)";
                int beforeW = 0;
                int beforeH = 0;
                if (before != null && i < before.Length)
                {
                    beforeName = before[i].textureName;
                    beforeW = before[i].width;
                    beforeH = before[i].height;
                }

                string afterName = "(n/a)";
                int afterW = 0;
                int afterH = 0;
                if (i < after.Length)
                {
                    afterName = after[i].textureName;
                    afterW = after[i].width;
                    afterH = after[i].height;
                }
                else if (i < defCount && defLayers[i].Texture != null)
                {
                    afterName = defLayers[i].Texture.name;
                    afterW = defLayers[i].Texture.width;
                    afterH = defLayers[i].Texture.height;
                }

                sb.Append('[').Append(i).Append("] AVANT tex=")
                    .Append(beforeName)
                    .Append(" dims=")
                    .Append(beforeW).Append('x').Append(beforeH)
                    .Append("  ->  APRES tex=")
                    .Append(afterName)
                    .Append(" dims=")
                    .Append(afterW).Append('x').Append(afterH);

                if (beforeW > 0 && afterW > 0 && beforeW != afterW)
                {
                    float ratio = (float)beforeW / (float)afterW;
                    sb.Append(" (ratio ")
                        .Append(ratio.ToString("0.00"))
                        .Append("x)");
                }

                if (i < defCount)
                {
                    sb.Append(" | layerName=")
                        .Append(defLayers[i].LayerName)
                        .Append(" | scrollSpeed=")
                        .Append(defLayers[i].ScrollSpeed.ToString("0.##"));
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        private static string GetTransformPath(Transform t)
        {
            string path = t.name;
            Transform p = t.parent;
            while (p != null)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }

            return path;
        }
    }
}
