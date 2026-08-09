using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Hub;

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

            _definition = (WorldBackgroundDefinition)EditorGUILayout.ObjectField(
                "Definition",
                _definition,
                typeof(WorldBackgroundDefinition),
                false);

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Appliquer", GUILayout.Height(28f)))
                Apply();

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
                _report = "Aucun ParallaxManager trouve dans la scene.";
                return;
            }

            _manager = all[0];

            StringBuilder sb = new StringBuilder(256);
            sb.Append("ParallaxManager selectionne (premier) : ")
                .Append(GetTransformPath(_manager.transform))
                .Append('\n');
            sb.Append("Total trouves : ").Append(all.Length).Append('\n');

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;
                sb.Append("  [").Append(i).Append("] ")
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

            LayerSnapshot[] before = CaptureManagerLayers(_manager);
            _manager.ApplyDefinition(_definition);
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
            sb.Append("Calques definition : ").Append(defCount).Append('\n');
            sb.Append("Calques ParallaxManager : ").Append(managerCount).Append('\n');

            if (defCount > managerCount)
            {
                sb.Append("AVERTISSEMENT : la definition a plus de calques (")
                    .Append(defCount)
                    .Append(") que le ParallaxManager (")
                    .Append(managerCount)
                    .Append("). Les calques en trop sont ignores.\n");
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
