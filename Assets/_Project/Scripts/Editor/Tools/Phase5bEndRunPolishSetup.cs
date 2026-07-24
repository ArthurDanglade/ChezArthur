#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
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
    /// Phase 5b — purge chiffres flottants (câblage HUD) + rebuild bandeaux fin de run pro.
    /// </summary>
    public static class Phase5bEndRunPolishSetup
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string SuccessSfxPath = "Assets/_Project/Audio/SFX/successound.mp3";
        private const string UnlockSfxPath = "Assets/_Project/Audio/SFX/unlocksound.wav";

        [MenuItem("Chez Arthur/Missions/Phase 5b — Polish Fin de Run (chiffres + bandeaux)")]
        public static void Apply()
        {
            if (!File.Exists(GameScenePath))
            {
                EditorUtility.DisplayDialog("Phase 5b", "Scène Game introuvable.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            DefeatUI defeat = Object.FindObjectOfType<DefeatUI>(true);
            if (defeat == null)
            {
                EditorUtility.DisplayDialog("Phase 5b", "DefeatUI introuvable.", "OK");
                return;
            }

            // Supprime l'ancien bandeau placeholder.
            EndRunAnnouncementBanner[] oldBanners = Object.FindObjectsOfType<EndRunAnnouncementBanner>(true);
            for (int i = 0; i < oldBanners.Length; i++)
            {
                if (oldBanners[i] != null)
                    Object.DestroyImmediate(oldBanners[i].gameObject);
            }

            EndRunAnnouncementBanner banner = BuildPolishedBanner(defeat.transform.root);
            WireDefeat(defeat, banner);
            WireCombatHudRoots(defeat);
            AssignJingles(banner);

            EditorUtility.SetDirty(defeat);
            EditorUtility.SetDirty(banner);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog(
                "Phase 5b Polish",
                "Terminé.\n\n" +
                "• ClearAllVisuals au HideCombatHud (chiffres Pixel Battle Text)\n" +
                "• HUD combat étendu (TeamPanel, BattleTextCanvas, etc.)\n" +
                "• Bandeau reconstruit (eyebrow + titre + barre accent + motion)\n\n" +
                "Test : fin de run avec mission / nouveau boss.",
                "OK");

            Debug.Log("[Phase5bEndRunPolishSetup] OK");
        }

        private static EndRunAnnouncementBanner BuildPolishedBanner(Transform root)
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            Transform parent = canvas != null ? canvas.transform : root;

            GameObject rootGo = new GameObject("EndRunAnnouncementBanner", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.5f, 0.78f);
            rootRt.anchorMax = new Vector2(0.5f, 0.78f);
            rootRt.sizeDelta = new Vector2(680f, 140f);
            rootRt.anchoredPosition = Vector2.zero;

            CanvasGroup cg = rootGo.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            // Glow soft derrière
            GameObject glowGo = CreateChild(rootGo.transform, "Glow");
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            StretchWithPadding(glowRt, -28f, -18f);
            Image glow = glowGo.AddComponent<Image>();
            glow.color = new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.2f);
            glow.raycastTarget = false;

            // Panel
            GameObject panelGo = CreateChild(rootGo.transform, "Panel");
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            StretchWithPadding(panelRt, 0f, 0f);
            Image bg = panelGo.AddComponent<Image>();
            Color bgColor = UiTheme.BgElevated;
            bgColor.a = 0.96f;
            bg.color = bgColor;

            Outline outline = panelGo.AddComponent<Outline>();
            outline.effectColor = UiTheme.BorderSubtle;
            outline.effectDistance = new Vector2(2f, -2f);

            // Accent bar top
            GameObject accentGo = CreateChild(panelGo.transform, "AccentBar");
            RectTransform accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 1f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.sizeDelta = new Vector2(0f, 6f);
            accentRt.anchoredPosition = Vector2.zero;
            Image accent = accentGo.AddComponent<Image>();
            accent.color = UiTheme.Gold;
            accent.raycastTarget = false;

            // Eyebrow
            GameObject eyeGo = CreateChild(panelGo.transform, "Eyebrow");
            RectTransform eyeRt = eyeGo.GetComponent<RectTransform>();
            eyeRt.anchorMin = new Vector2(0f, 0.55f);
            eyeRt.anchorMax = new Vector2(1f, 0.92f);
            eyeRt.offsetMin = new Vector2(28f, 0f);
            eyeRt.offsetMax = new Vector2(-28f, -8f);
            TextMeshProUGUI eyebrow = eyeGo.AddComponent<TextMeshProUGUI>();
            eyebrow.text = "MISSION ACCOMPLIE";
            eyebrow.fontSize = 18f;
            eyebrow.fontStyle = FontStyles.Bold;
            eyebrow.characterSpacing = 6f;
            eyebrow.alignment = TextAlignmentOptions.Center;
            eyebrow.color = UiTheme.Gold;
            eyebrow.raycastTarget = false;

            // Title
            GameObject titleGo = CreateChild(panelGo.transform, "Title");
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.08f);
            titleRt.anchorMax = new Vector2(1f, 0.58f);
            titleRt.offsetMin = new Vector2(24f, 8f);
            titleRt.offsetMax = new Vector2(-24f, 0f);
            TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "1 mission terminée !";
            title.fontSize = 34f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = UiTheme.TextPrimary;
            title.enableAutoSizing = true;
            title.fontSizeMin = 22f;
            title.fontSizeMax = 36f;
            title.raycastTarget = false;

            EndRunAnnouncementBanner banner = rootGo.AddComponent<EndRunAnnouncementBanner>();
            banner.EditorWire(cg, panelRt, bg, accent, glow, eyebrow, title);
            return banner;
        }

        private static void WireDefeat(DefeatUI defeat, EndRunAnnouncementBanner banner)
        {
            SerializedObject so = new SerializedObject(defeat);
            SerializedProperty prop = so.FindProperty("endRunAnnouncementBanner");
            if (prop != null)
            {
                prop.objectReferenceValue = banner;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void WireCombatHudRoots(DefeatUI defeat)
        {
            var roots = new List<GameObject>();
            TryAdd(roots, "TeamPanel");
            TryAdd(roots, "SynergyHud");
            TryAdd(roots, "TalsGroup");
            TryAdd(roots, "MenuContainer");
            TryAdd(roots, "BattleTextCanvas");
            TryAdd(roots, "PressurePerimeter_v1");
            TryAdd(roots, "PressureGaugeSystems");
            TryAdd(roots, "HPBarManager");
            TryAdd(roots, "PixelBattleTextController");

            // Sous-arbres fréquents sous GameUI
            GameObject gameUi = GameObject.Find("GameUI");
            if (gameUi != null)
            {
                Transform pressureUi = gameUi.transform.Find("PressureGaugeUI");
                if (pressureUi != null && !roots.Contains(pressureUi.gameObject))
                    roots.Add(pressureUi.gameObject);
            }

            SerializedObject so = new SerializedObject(defeat);
            SerializedProperty prop = so.FindProperty("combatHudRootsToHide");
            if (prop == null)
                return;

            prop.arraySize = roots.Count;
            for (int i = 0; i < roots.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = roots[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void TryAdd(List<GameObject> roots, string objectName)
        {
            GameObject[] all = Object.FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].name != objectName)
                    continue;
                if (!roots.Contains(all[i]))
                    roots.Add(all[i]);
                return;
            }
        }

        private static void AssignJingles(EndRunAnnouncementBanner banner)
        {
            AudioClip missions = AssetDatabase.LoadAssetAtPath<AudioClip>(SuccessSfxPath);
            AudioClip bosses = AssetDatabase.LoadAssetAtPath<AudioClip>(UnlockSfxPath);
            if (bosses == null)
                bosses = missions;
            banner.EditorSetJingles(missions, bosses);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchWithPadding(RectTransform rt, float padX, float padY)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }
    }
}
#endif
