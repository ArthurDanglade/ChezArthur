#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.Hub;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Place le bouton preview éveil + refs + clips audio dans la scène Hub.
    /// </summary>
    public static class AwakeningCeremonyHubPreviewSetup
    {
        private const string HubScenePath = "Assets/_Project/Scenes/Hub.unity";
        private const string OverlayPrefabPath = "Assets/_Project/Prefabs/UI/AwakeningCeremonyOverlay.prefab";
        private const string DissolveMatPath = "Assets/_Project/Art/FX/AwakeningDissolve.mat";
        private const string ArdaculaPath = "Assets/_Project/Data/Characters/SSR/Ardacula/Ardacula.asset";

        // GUIDs des clips déjà branchés sur le controller Game
        private const string RiserGuid = "243b8faec833fb8449b19ad94762feab";
        private const string FlashGuid = "5b47dc9bc77cdb748af9322ab6a293b0";
        private const string FanfareGuid = "c749cb104090202408c977fe9d4129fd";

        [MenuItem("Chez Arthur/Debug/Setup Preview Éveil (Hub)")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(HubScenePath);
            if (!scene.IsValid())
            {
                Debug.LogError("[AwakeningCeremonyHubPreviewSetup] Impossible d'ouvrir Hub.unity.");
                return;
            }

            CharacterData ardacula = AssetDatabase.LoadAssetAtPath<CharacterData>(ArdaculaPath);
            GameObject overlayGo = AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            AwakeningCeremonyView overlay = overlayGo != null
                ? overlayGo.GetComponent<AwakeningCeremonyView>()
                : null;
            Material dissolve = AssetDatabase.LoadAssetAtPath<Material>(DissolveMatPath);
            AudioClip riser = LoadClip(RiserGuid);
            AudioClip flash = LoadClip(FlashGuid);
            AudioClip fanfare = LoadClip(FanfareGuid);

            if (ardacula == null || overlay == null || dissolve == null)
            {
                Debug.LogError(
                    "[AwakeningCeremonyHubPreviewSetup] Asset manquant — " +
                    $"ardacula={ardacula != null}, overlay={overlay != null}, dissolve={dissolve != null}");
                return;
            }

            AwakeningCeremonyDebugButton button =
                Object.FindObjectOfType<AwakeningCeremonyDebugButton>();
            if (button == null)
            {
                GameObject go = new GameObject("AwakeningCeremonyDebugPreview");
                button = go.AddComponent<AwakeningCeremonyDebugButton>();
            }

            SerializedObject so = new SerializedObject(button);
            so.FindProperty("previewCharacter").objectReferenceValue = ardacula;
            so.FindProperty("overlayPrefab").objectReferenceValue = overlay;
            so.FindProperty("dissolveMaterial").objectReferenceValue = dissolve;
            so.FindProperty("riserClip").objectReferenceValue = riser;
            so.FindProperty("flashClip").objectReferenceValue = flash;
            so.FindProperty("fanfareClip").objectReferenceValue = fanfare;
            so.FindProperty("buildButtonIfMissing").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = button.gameObject;

            Debug.Log(
                "[AwakeningCeremonyHubPreviewSetup] OK — clips audio + refs branchés. " +
                "Si les champs audio étaient vides, rejoue ce menu.");
        }

        private static AudioClip LoadClip(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
#endif
