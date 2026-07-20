#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.Core;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte les singletons logiques de la jauge de pression en scène combat.
    /// </summary>
    public static class PressureGaugeSceneBuilder
    {
        private const string ManagerName = "PressureGaugeSystems";

        [MenuItem("Chez Arthur/UI/Monter systèmes Pression (logique)")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Monter systèmes Pression");
            int undoGroup = Undo.GetCurrentGroup();

            GameObject managerGo = GameObject.Find(ManagerName);
            if (managerGo == null)
            {
                managerGo = new GameObject(ManagerName);
                Undo.RegisterCreatedObjectUndo(managerGo, "Créer PressureGaugeSystems");
            }

            EnsureComponent<PressureGaugeSystem>(managerGo);
            EnsureComponent<RuptureEffectsSystem>(managerGo);
            PressureRupturePresentation presentation = EnsureComponent<PressureRupturePresentation>(managerGo);
            WirePresentation(presentation);
            RemoveDuplicatePressureComponentsFromRunManager();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = managerGo;

            Debug.Log(
                "[PressureGaugeSceneBuilder] PressureGaugeSystem + RuptureEffectsSystem montés. " +
                "Relance « Générer Manomètre Pression » si le HUD n'est pas visible.");
        }

        [MenuItem("Chez Arthur/UI/Configurer Jauge Pression (complet)")]
        public static void BuildComplete()
        {
            Build();
            PressureGaugeUIGeneratorTool.Generate();
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            if (existing != null)
                return existing;

            return Undo.AddComponent<T>(go);
        }

        private static void WirePresentation(PressureRupturePresentation presentation)
        {
            if (presentation == null)
                return;

            SerializedObject so = new SerializedObject(presentation);

            RuptureBannerUI ruptureBanner = Object.FindObjectOfType<RuptureBannerUI>(true);
            SerializedProperty bannerProp = so.FindProperty("ruptureBanner");
            if (bannerProp != null && ruptureBanner != null)
                bannerProp.objectReferenceValue = ruptureBanner;

            StageAnnouncerUI announcer = Object.FindObjectOfType<StageAnnouncerUI>(true);
            SerializedProperty announcerProp = so.FindProperty("stageAnnouncer");
            if (announcerProp != null && announcer != null)
                announcerProp.objectReferenceValue = announcer;

            CameraShake shake = Object.FindObjectOfType<CameraShake>(true);
            SerializedProperty shakeProp = so.FindProperty("cameraShake");
            if (shakeProp != null && shake != null)
                shakeProp.objectReferenceValue = shake;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveDuplicatePressureComponentsFromRunManager()
        {
            RunManager runManager = Object.FindObjectOfType<RunManager>(true);
            if (runManager == null)
                return;

            PressureGaugeSystem dupGauge = runManager.GetComponent<PressureGaugeSystem>();
            if (dupGauge != null)
                Undo.DestroyObjectImmediate(dupGauge);

            RuptureEffectsSystem dupFx = runManager.GetComponent<RuptureEffectsSystem>();
            if (dupFx != null)
                Undo.DestroyObjectImmediate(dupFx);
        }
    }
}
#endif
