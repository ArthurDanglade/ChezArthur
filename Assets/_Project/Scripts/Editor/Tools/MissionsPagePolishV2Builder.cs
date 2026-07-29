#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub;
using ChezArthur.Hub.Pages.Missions;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Polish Missions v2 — TabBar compacte + icônes, Tals2 cohérent, FX claim + SFX.
    /// </summary>
    public static class MissionsPagePolishV2Builder
    {
        private const string UndoLabel = "Missions Polish v2";
        private const string PageMissionsName = "PageMissions";
        private const float TabBarHeight = 108f;
        private const float IconSize = 32f;

        private const string Tals1Path = "Assets/_Project/Sprites/UI/Tals1.png";
        private const string Tals2Path = "Assets/_Project/Sprites/UI/Tals2.png";
        private const string Tals3Path = "Assets/_Project/Sprites/UI/Tals3.png";
        private const string SuccessSfxPath = "Assets/_Project/Audio/SFX/successound.mp3";
        private const string TalsSfx1Path = "Assets/_Project/Audio/SFX/Talsound1.mp3";
        private const string TalsSfx2Path = "Assets/_Project/Audio/SFX/talsound2.mp3";

        [MenuItem("Chez Arthur/Refonte Hub/Page Missions — Polish v2 TabBar+FX (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Page Missions — Polish v2 TabBar+FX (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Polish Missions v2",
                    "TabBar compacte fixe + glyphes, icône Tals2 (header+cartes), "
                    + "TalsClaimFX (pluie max 20) + SFX success/tals.\n\nCtrl+S Hub ensuite.",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(8192);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" MissionsPagePolishV2Builder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                Debug.LogError("[MissionsPagePolishV2] Ouvre Hub.unity.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            Transform page = FindDeep(scene, PageMissionsName);
            Transform root = page != null ? page.Find("MissionsRoot") : null;
            if (page == null || root == null)
            {
                failed++;
                log.AppendLine("- ✗ PageMissions / MissionsRoot introuvable");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            Sprite tals1 = AssetDatabase.LoadAssetAtPath<Sprite>(Tals1Path);
            Sprite tals2 = AssetDatabase.LoadAssetAtPath<Sprite>(Tals2Path);
            Sprite tals3 = AssetDatabase.LoadAssetAtPath<Sprite>(Tals3Path);
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();

            if (tals2 == null)
            {
                failed++;
                log.AppendLine($"- ✗ `{Tals2Path}` introuvable");
            }

            AudioClip success = AssetDatabase.LoadAssetAtPath<AudioClip>(SuccessSfxPath);
            AudioClip sfx1 = AssetDatabase.LoadAssetAtPath<AudioClip>(TalsSfx1Path);
            AudioClip sfx2 = AssetDatabase.LoadAssetAtPath<AudioClip>(TalsSfx2Path);
            if (success == null)
                log.AppendLine($"- ⚠ `{SuccessSfxPath}` manquant (claim SFX skip)");
            if (sfx1 == null && sfx2 == null)
                log.AppendLine("- ⚠ Talsound clips manquants");

            // —— 1. TabBar compacte ——
            log.AppendLine("## TabBar compacte fixe + glyphes");
            ProcessTabBar(root, spriteS, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 2. Icônes Tals2 ——
            log.AppendLine("## Icône Tals2 (header + cartes)");
            ProcessTalsIcons(root, tals2, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 3. FX claim ——
            log.AppendLine("## TalsClaimFX + SFX");
            ProcessClaimFx(
                page, tals1, tals2, tals3, success, sfx1, sfx2,
                apply, log, ref todo, ref conforme, ref failed);

            log.AppendLine();
            AppendCounter(log, todo, conforme, failed);

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log(log.ToString());
        }

        private static void ProcessTabBar(
            Transform root,
            Sprite spriteS,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform tabBarTx = root.Find("TabBar");
            if (tabBarTx == null)
            {
                failed++;
                log.AppendLine("- ✗ TabBar introuvable");
                return;
            }

            LayoutElement le = tabBarTx.GetComponent<LayoutElement>();
            bool heightOk = le != null
                            && Mathf.Approximately(le.preferredHeight, TabBarHeight)
                            && Mathf.Approximately(le.flexibleHeight, 0f);

            Transform template = tabBarTx.Find("TabItemTemplate");
            bool hasGlyph = template != null && template.Find("Content/IconGlyph") != null;

            if (heightOk && hasGlyph)
            {
                conforme++;
                log.AppendLine($"- TabBar hauteur {TabBarHeight} + IconGlyph ✓");
                return;
            }

            if (!apply)
            {
                if (!heightOk)
                {
                    todo++;
                    log.AppendLine(
                        $"- [DRY] Fixer TabBar h={TabBarHeight} flexibleHeight=0 — À FAIRE");
                }

                if (!hasGlyph)
                {
                    todo++;
                    log.AppendLine("- [DRY] Rebuild TabItemTemplate (Icon + glyphe + Label) — À FAIRE");
                }

                return;
            }

            if (le == null)
                le = Undo.AddComponent<LayoutElement>(tabBarTx.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.minHeight = TabBarHeight;
            le.preferredHeight = TabBarHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            // MissionScroll flex 1
            Transform scroll = root.Find("MissionScroll");
            if (scroll != null)
            {
                LayoutElement sle = scroll.GetComponent<LayoutElement>();
                if (sle == null)
                    sle = Undo.AddComponent<LayoutElement>(scroll.gameObject);
                Undo.RecordObject(sle, UndoLabel);
                sle.flexibleHeight = 1f;
                sle.minHeight = 200f;
            }

            // SeasonEmpty : flexible mais pas d'expansion parasite quand inactif
            Transform season = root.Find("SeasonEmpty");
            if (season != null)
            {
                LayoutElement sele = season.GetComponent<LayoutElement>();
                if (sele != null)
                {
                    Undo.RecordObject(sele, UndoLabel);
                    sele.flexibleHeight = 1f;
                    sele.preferredHeight = 160f;
                }
            }

            RebuildTabTemplate(tabBarTx, spriteS);
            conforme++;
            log.AppendLine($"- TabBar h={TabBarHeight} + template Icon/Glyph rebuild ✓");
        }

        private static void RebuildTabTemplate(Transform tabBarTx, Sprite spriteS)
        {
            Transform old = tabBarTx.Find("TabItemTemplate");
            if (old != null)
                Undo.DestroyObjectImmediate(old.gameObject);

            // Purge clones runtime
            for (int i = tabBarTx.childCount - 1; i >= 0; i--)
            {
                Transform c = tabBarTx.GetChild(i);
                if (c.name.StartsWith("Tab_"))
                    Undo.DestroyObjectImmediate(c.gameObject);
            }

            GameObject template = new GameObject(
                "TabItemTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(template, UndoLabel);
            Undo.SetTransformParent(template.transform, tabBarTx, false, UndoLabel);
            template.SetActive(false);

            Image border = template.GetComponent<Image>();
            border.sprite = spriteS;
            border.type = Image.Type.Sliced;
            border.color = UiTheme.BorderSubtle;

            GameObject fillGo = new GameObject(
                "Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fillGo, UndoLabel);
            Undo.SetTransformParent(fillGo.transform, template.transform, false, UndoLabel);
            Image fill = fillGo.GetComponent<Image>();
            fill.sprite = spriteS;
            fill.type = Image.Type.Sliced;
            StretchFull((RectTransform)fillGo.transform);
            float inset = UiTheme.BorderThin;
            RectTransform fillRt = (RectTransform)fillGo.transform;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);

            GameObject content = new GameObject(
                "Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(content, UndoLabel);
            Undo.SetTransformParent(content.transform, template.transform, false, UndoLabel);
            StretchFull((RectTransform)content.transform);
            float pad = UiTheme.Space2;
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.offsetMin = new Vector2(pad, pad);
            contentRt.offsetMax = new Vector2(-pad, -pad);
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 2f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Icon (Image placeholder — désactivé si glyphe)
            GameObject iconGo = new GameObject(
                "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
            Undo.SetTransformParent(iconGo.transform, content.transform, false, UndoLabel);
            LayoutElement iconLe = Undo.AddComponent<LayoutElement>(iconGo);
            iconLe.minWidth = IconSize;
            iconLe.preferredWidth = IconSize;
            iconLe.minHeight = IconSize;
            iconLe.preferredHeight = IconSize;
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconGo.SetActive(false);

            // IconGlyph (TMP placeholder)
            GameObject glyphGo = new GameObject(
                "IconGlyph", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(glyphGo, UndoLabel);
            Undo.SetTransformParent(glyphGo.transform, content.transform, false, UndoLabel);
            LayoutElement glyphLe = Undo.AddComponent<LayoutElement>(glyphGo);
            glyphLe.minHeight = IconSize;
            glyphLe.preferredHeight = IconSize;
            TextMeshProUGUI glyph = glyphGo.GetComponent<TextMeshProUGUI>();
            glyph.text = "◆";
            glyph.fontSize = 28f;
            glyph.color = UiTheme.TextMuted;
            glyph.alignment = TextAlignmentOptions.Center;
            glyph.raycastTarget = false;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            Undo.SetTransformParent(labelGo.transform, content.transform, false, UndoLabel);
            LayoutElement labelLe = Undo.AddComponent<LayoutElement>(labelGo);
            labelLe.minHeight = UiTypography.Caption + 4f;
            labelLe.preferredHeight = UiTypography.Caption + 8f;
            TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "Tab";
            label.fontSize = UiTypography.Caption;
            label.color = UiTheme.TextSecondary;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            TabBarUI tabBar = tabBarTx.GetComponent<TabBarUI>();
            if (tabBar == null)
                tabBar = Undo.AddComponent<TabBarUI>(tabBarTx.gameObject);

            SerializedObject so = new SerializedObject(tabBar);
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("tabItemTemplate").objectReferenceValue = template;
            so.FindProperty("fixedItemHeight").floatValue = TabBarHeight;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tabBar);
        }

        private static void ProcessTalsIcons(
            Transform root,
            Sprite tals2,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (tals2 == null)
            {
                failed++;
                log.AppendLine("- ✗ Tals2 null — skip");
                return;
            }

            // Header PillTals/TalsIcon
            Transform pill = FindDeep(root.root, "PillTals");
            if (pill == null)
            {
                GameObject found = GameObject.Find("PillTals");
                if (found != null)
                    pill = found.transform;
            }

            if (pill != null)
            {
                Transform iconTx = FindDeep(pill, "TalsIcon");
                Image icon = iconTx != null ? iconTx.GetComponent<Image>() : null;
                if (icon != null)
                {
                    if (icon.sprite == tals2)
                    {
                        conforme++;
                        log.AppendLine("- Header PillTals → Tals2 ✓");
                    }
                    else if (!apply)
                    {
                        todo++;
                        log.AppendLine(
                            $"- [DRY] Header icon `{icon.sprite?.name}` → Tals2 — À FAIRE");
                    }
                    else
                    {
                        Undo.RecordObject(icon, UndoLabel);
                        icon.sprite = tals2;
                        icon.preserveAspect = true;
                        EditorUtility.SetDirty(icon);
                        conforme++;
                        log.AppendLine("- Header PillTals → Tals2 ✓");
                    }
                }
                else
                {
                    failed++;
                    log.AppendLine("- ✗ PillTals/TalsIcon Image introuvable");
                }
            }
            else
            {
                failed++;
                log.AppendLine("- ✗ PillTals introuvable");
            }

            // Mission entry templates
            int patched = 0;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] == null || images[i].gameObject.name != "TalsIcon")
                    continue;

                if (images[i].sprite == tals2)
                {
                    patched++;
                    continue;
                }

                if (!apply)
                {
                    todo++;
                    log.AppendLine(
                        $"- [DRY] `{GetPath(images[i].transform)}` → Tals2 — À FAIRE");
                    continue;
                }

                Undo.RecordObject(images[i], UndoLabel);
                images[i].sprite = tals2;
                images[i].preserveAspect = true;
                EditorUtility.SetDirty(images[i]);
                patched++;
            }

            if (apply || patched > 0)
            {
                conforme++;
                log.AppendLine($"- Cartes TalsIcon → Tals2 ({patched}) ✓");
            }
        }

        private static void ProcessClaimFx(
            Transform page,
            Sprite tals1,
            Sprite tals2,
            Sprite tals3,
            AudioClip success,
            AudioClip sfx1,
            AudioClip sfx2,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            MissionsPageUI pageUi = page.GetComponent<MissionsPageUI>();
            TalsClaimFX fx = page.GetComponent<TalsClaimFX>();
            if (fx == null)
                fx = Object.FindObjectOfType<TalsClaimFX>();

            if (fx != null && pageUi != null)
            {
                SerializedObject pageSo = new SerializedObject(pageUi);
                if (pageSo.FindProperty("claimFx").objectReferenceValue == fx)
                {
                    conforme++;
                    log.AppendLine("- TalsClaimFX déjà câblé ✓");
                    return;
                }
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Créer/câbler TalsClaimFX (Tals1/2/3 + SFX) — À FAIRE");
                return;
            }

            if (fx == null)
                fx = Undo.AddComponent<TalsClaimFX>(page.gameObject);

            Transform pill = FindDeep(page.root, "PillTals");
            if (pill == null)
            {
                GameObject found = GameObject.Find("PillTals");
                if (found != null)
                    pill = found.transform;
            }

            var sprites = new System.Collections.Generic.List<Sprite>(3);
            if (tals1 != null) sprites.Add(tals1);
            if (tals2 != null) sprites.Add(tals2);
            if (tals3 != null) sprites.Add(tals3);

            var pickups = new System.Collections.Generic.List<AudioClip>(2);
            if (sfx1 != null) pickups.Add(sfx1);
            if (sfx2 != null) pickups.Add(sfx2);

            SerializedObject so = new SerializedObject(fx);
            SerializedProperty spritesProp = so.FindProperty("coinSprites");
            spritesProp.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

            so.FindProperty("counterTarget").objectReferenceValue = pill as RectTransform;
            so.FindProperty("claimSuccessClip").objectReferenceValue = success;

            SerializedProperty pickProp = so.FindProperty("pickupClips");
            pickProp.arraySize = pickups.Count;
            for (int i = 0; i < pickups.Count; i++)
                pickProp.GetArrayElementAtIndex(i).objectReferenceValue = pickups[i];

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fx);

            if (pageUi != null)
            {
                SerializedObject pageSo = new SerializedObject(pageUi);
                pageSo.FindProperty("claimFx").objectReferenceValue = fx;
                pageSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pageUi);
            }

            conforme++;
            log.AppendLine(
                $"- TalsClaimFX câblé (sprites={sprites.Count}, pickups={pickups.Count}, "
                + $"target={(pill != null ? pill.name : "null")}) ✓");
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine("## COMPTEUR D'ACTIONS (harnais v2)");
            log.AppendLine($"- À FAIRE : {todo}");
            log.AppendLine($"- CONFORMES : {conforme}");
            log.AppendLine($"- ÉCHECS : {failed}");
            log.AppendLine(todo == 0 && failed == 0
                ? "- Convergence : OUI"
                : "- Convergence : NON");
        }

        private static Transform FindDeep(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform f = FindDeep(root.transform, name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform f = FindDeep(root.GetChild(i), name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static string GetPath(Transform t)
        {
            if (t == null)
                return "—";
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
#endif
