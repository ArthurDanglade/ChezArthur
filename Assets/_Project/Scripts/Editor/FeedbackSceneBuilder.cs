#if UNITY_EDITOR
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Feedback;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câble CombatFeedbackService dans la scène combat (idempotent).
    /// Exécution + commit de scène séparés du code F2-P2a (protocole G6).
    /// </summary>
    public static class FeedbackSceneBuilder
    {
        private const string ServiceObjectName = "CombatFeedbackService";
        private const string CatalogPath = "Assets/_Project/Data/Feedback/FeedbackCatalog.asset";

        [MenuItem("Chez Arthur/Feedback/Câbler Feedback Scène Combat")]
        public static void WireCombatFeedbackService()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[FeedbackSceneBuilder] Aucune scène active chargée.");
                return;
            }

            FeedbackCatalog catalog = AssetDatabase.LoadAssetAtPath<FeedbackCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[FeedbackSceneBuilder] Catalogue introuvable : {CatalogPath}");
                return;
            }

            GameObject go = GameObject.Find(ServiceObjectName);
            if (go == null)
            {
                Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null || all[i].name != ServiceObjectName)
                        continue;
                    if (!all[i].gameObject.scene.IsValid() || all[i].gameObject.scene != scene)
                        continue;
                    go = all[i].gameObject;
                    break;
                }
            }

            if (go == null)
            {
                go = new GameObject(ServiceObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Créer CombatFeedbackService");
                EditorSceneManager.MarkSceneDirty(scene);
            }

            CombatFeedbackService service = go.GetComponent<CombatFeedbackService>();
            if (service == null)
            {
                service = Undo.AddComponent<CombatFeedbackService>(go);
                EditorSceneManager.MarkSceneDirty(scene);
            }

            CameraShake shake = Object.FindObjectOfType<CameraShake>(true);
            if (shake == null)
                Debug.LogWarning("[FeedbackSceneBuilder] CameraShake introuvable dans la scène.");

            SerializedObject so = new SerializedObject(service);
            SerializedProperty catalogProp = so.FindProperty("_catalog");
            SerializedProperty shakeProp = so.FindProperty("_cameraShake");

            bool alreadyWired =
                catalogProp != null && catalogProp.objectReferenceValue == catalog
                && shakeProp != null && shakeProp.objectReferenceValue == shake;

            if (alreadyWired)
            {
                Debug.Log("[FeedbackSceneBuilder] Déjà câblé — aucun changement.");
                return;
            }

            Undo.RecordObject(service, "Câbler CombatFeedbackService");
            if (catalogProp != null)
                catalogProp.objectReferenceValue = catalog;
            if (shakeProp != null)
                shakeProp.objectReferenceValue = shake;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(service);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[FeedbackSceneBuilder] CombatFeedbackService câblé. Sauve la scène (commit séparé).");
        }
    }
}
#endif
