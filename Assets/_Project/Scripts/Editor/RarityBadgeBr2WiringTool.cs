#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Characters;
using ChezArthur.Hub.Pages.Invocation;
using ChezArthur.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// BR2 — badges RateUp + PullResult (idle) + purge chrome Option B / rarityText.
    /// Idempotent ; rapport Audits/BR2_RarityBadges_Report.md.
    /// </summary>
    public static class RarityBadgeBr2WiringTool
    {
        private const string UndoLabel = "BR2 Rarity Badges";
        private const string LibraryPath =
            "Assets/_Project/ScriptableObjects/Config/RarityVisualLibrary.asset";
        private const string RateUpPrefabPath =
            "Assets/_Project/Prefabs/UI/RateUpCharacterEntry.prefab";
        private const string PullGridPrefabPath =
            "Assets/_Project/Prefabs/UI/PullResultEntry.prefab";
        private const string PullSinglePrefabPath =
            "Assets/_Project/Prefabs/UI/PullResultSingleCard.prefab";
        private const string ReportPath = "Audits/BR2_RarityBadges_Report.md";

        private const float RateUpBadgeSize = 56f;
        private const float PullGridBadgeSize = 64f;
        private const float PullSingleBadgeSize = 96f;
        private const float BadgeOverhang = 6f;

        [MenuItem("Chez Arthur/UI/BR2 — Badges RateUp + PullResult")]
        public static void Run()
        {
            if (!Application.isBatchMode)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                {
                    Debug.LogWarning("[BR2] Scène dirty non sauvée — abort (MT0).");
                    return;
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isDirty)
                {
                    Debug.LogWarning("[BR2] Scène dirty — abort (MT0).");
                    return;
                }
            }

            var report = new StringBuilder(4096);
            int changes = 0;
            var converged = new List<string>();

            report.AppendLine("# BR2 — Badges cohérence invocation — rapport wiring");
            report.AppendLine();
            report.AppendLine("Date : " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            report.AppendLine();
            report.AppendLine("## KB1 / KB2");
            report.AppendLine();
            report.AppendLine(
                "- **KB1** : `ssrGlow` / `rarityTopBorder` = rôle rareté uniquement "
                + "(audit code) → purge champ + GO (pas masquage).");
            report.AppendLine(
                "- **KB2** : `CharacterEntryUI` couleurs locales ≈ `UiTheme.Rarity*` "
                + "(#99CCFF / #FFD700 / #CC80FF) — iso rendu, bascule palette OK.");
            report.AppendLine(
                "- **Option B** : badge remplace chrome PullResult.");
            report.AppendLine();

            RarityVisualLibrary library =
                AssetDatabase.LoadAssetAtPath<RarityVisualLibrary>(LibraryPath);
            if (library == null)
            {
                report.AppendLine("- ✗ RarityVisualLibrary absente — abort");
                WriteReport(report, -1);
                return;
            }

            WireRateUp(library, report, ref changes, converged);
            WirePullPrefab(
                PullGridPrefabPath, "PullResultEntry", PullGridBadgeSize,
                library, report, ref changes, converged);
            WirePullPrefab(
                PullSinglePrefabPath, "PullResultSingleCard", PullSingleBadgeSize,
                library, report, ref changes, converged);

            report.AppendLine();
            report.AppendLine("## Convergence");
            report.AppendLine();
            if (converged.Count == 0)
                report.AppendLine("- (rien de nouveau ce run)");
            else
            {
                for (int i = 0; i < converged.Count; i++)
                    report.AppendLine("- " + converged[i]);
            }

            report.AppendLine();
            report.AppendLine($"**Résultat : {changes} changement(s)**");
            WriteReport(report, changes);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BR2] Terminé — {changes} changement(s). Voir {ReportPath}");
        }

        private static void WireRateUp(
            RarityVisualLibrary library,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            report.AppendLine("## RateUpCharacterEntry.prefab");
            report.AppendLine();

            if (!File.Exists(RateUpPrefabPath))
            {
                report.AppendLine("- ✗ Prefab introuvable");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(RateUpPrefabPath);
            try
            {
                Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                RateUpCharacterEntryUI ui = root.GetComponent<RateUpCharacterEntryUI>();
                if (ui == null)
                {
                    report.AppendLine("- ✗ RateUpCharacterEntryUI manquant");
                    return;
                }

                // A1 — purge RarityText
                Transform rarityTextTx = FindChildTrim(root.transform, "RarityText");
                if (rarityTextTx != null)
                {
                    Object.DestroyImmediate(rarityTextTx.gameObject);
                    changes++;
                    converged.Add("RateUp : purgé GO RarityText");
                    report.AppendLine("- RarityText purgé (A1)");
                }
                else
                {
                    report.AppendLine("- RarityText déjà absent ✓");
                }

                RarityBadgeView view = EnsureBadge(
                    root.transform, null, RateUpBadgeSize, library,
                    playAnimation: false, report, ref changes, converged, "RateUp");

                SerializedObject so = new SerializedObject(ui);
                SerializedProperty badgeProp = so.FindProperty("rarityBadge");
                SerializedProperty textProp = so.FindProperty("rarityText");
                bool dirty = false;
                if (textProp != null && textProp.objectReferenceValue != null)
                {
                    textProp.objectReferenceValue = null;
                    dirty = true;
                }

                if (badgeProp != null && badgeProp.objectReferenceValue != view)
                {
                    badgeProp.objectReferenceValue = view;
                    dirty = true;
                }

                if (dirty)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add("RateUp : rarityBadge câblé, rarityText null");
                }
                else
                {
                    report.AppendLine("- rarityBadge déjà câblé ✓");
                }

                PrefabUtility.SaveAsPrefabAsset(root, RateUpPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WirePullPrefab(
            string path,
            string label,
            float badgeSize,
            RarityVisualLibrary library,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            report.AppendLine();
            report.AppendLine($"## {label}.prefab");
            report.AppendLine();

            if (!File.Exists(path))
            {
                report.AppendLine("- ✗ Prefab introuvable");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                PullResultEntryUI ui = root.GetComponent<PullResultEntryUI>();
                if (ui == null)
                {
                    report.AppendLine("- ✗ PullResultEntryUI manquant");
                    return;
                }

                // KB1 — purge chrome rareté
                changes += PurgeChild(root.transform, "RarityTopBorder", converged, report);
                changes += PurgeChild(root.transform, "SsrGlow", converged, report);

                RarityBadgeView view = EnsureBadge(
                    root.transform, null, badgeSize, library,
                    playAnimation: false, report, ref changes, converged, label);

                SerializedObject so = new SerializedObject(ui);
                bool dirty = false;
                dirty |= ClearRef(so, "rarityTopBorder");
                dirty |= ClearRef(so, "ssrGlow");
                SerializedProperty badgeProp = so.FindProperty("rarityBadge");
                if (badgeProp != null && badgeProp.objectReferenceValue != view)
                {
                    badgeProp.objectReferenceValue = view;
                    dirty = true;
                }

                if (dirty)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add($"{label} : rarityBadge câblé, chrome null");
                    report.AppendLine("- SerializeField chrome purgés + badge câblé");
                }
                else
                {
                    report.AppendLine("- Wiring déjà à jour ✓");
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int PurgeChild(
            Transform root, string childName, List<string> converged, StringBuilder report)
        {
            Transform tx = FindChildTrim(root, childName);
            if (tx == null)
            {
                report.AppendLine($"- {childName} déjà absent ✓");
                return 0;
            }

            Object.DestroyImmediate(tx.gameObject);
            converged.Add($"Purge GO {childName}");
            report.AppendLine($"- {childName} purgé (KB1)");
            return 1;
        }

        private static bool ClearRef(SerializedObject so, string propName)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p == null || p.objectReferenceValue == null)
                return false;
            p.objectReferenceValue = null;
            return true;
        }

        private static RarityBadgeView EnsureBadge(
            Transform root,
            Transform preferredParent,
            float size,
            RarityVisualLibrary library,
            bool playAnimation,
            StringBuilder report,
            ref int changes,
            List<string> converged,
            string tag)
        {
            Transform parent = preferredParent != null ? preferredParent : root;
            Transform badgeTx = FindChildTrim(parent, "RarityBadge");
            if (badgeTx == null)
                badgeTx = FindDeepTrim(root, "RarityBadge");

            GameObject badgeGo;
            if (badgeTx == null)
            {
                badgeGo = new GameObject(
                    "RarityBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeGo.transform.SetParent(parent, false);
                changes++;
                converged.Add($"{tag} : créé RarityBadge");
                report.AppendLine("- RarityBadge créé");
            }
            else
            {
                badgeGo = badgeTx.gameObject;
                report.AppendLine("- RarityBadge déjà présent ✓");
            }

            Image img = badgeGo.GetComponent<Image>();
            if (img == null)
                img = badgeGo.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            img.color = Color.white;

            RectTransform rt = badgeGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = new Vector2(-BadgeOverhang, BadgeOverhang);
            rt.localEulerAngles = Vector3.zero;
            // Au-dessus du status chip (bas) — sibling index haut
            badgeGo.transform.SetAsLastSibling();

            RarityBadgeView view = badgeGo.GetComponent<RarityBadgeView>();
            if (view == null)
            {
                view = badgeGo.AddComponent<RarityBadgeView>();
                changes++;
                converged.Add($"{tag} : RarityBadgeView ajouté");
            }

            SerializedObject viewSo = new SerializedObject(view);
            SerializedProperty libProp = viewSo.FindProperty("library");
            SerializedProperty playProp = viewSo.FindProperty("playAnimation");
            bool viewDirty = false;
            if (libProp != null && libProp.objectReferenceValue != library)
            {
                libProp.objectReferenceValue = library;
                viewDirty = true;
            }

            if (playProp != null && playProp.boolValue != playAnimation)
            {
                playProp.boolValue = playAnimation;
                viewDirty = true;
            }

            if (viewDirty)
            {
                viewSo.ApplyModifiedPropertiesWithoutUndo();
                changes++;
                converged.Add($"{tag} : library + playAnimation={playAnimation}");
            }

            return view;
        }

        private static Transform FindChildTrim(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c != null && c.name.Trim() == name)
                    return c;
            }

            return null;
        }

        private static Transform FindDeepTrim(Transform parent, string name)
        {
            Transform direct = FindChildTrim(parent, name);
            if (direct != null)
                return direct;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepTrim(parent.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void WriteReport(StringBuilder report, int changes)
        {
            string dir = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(ReportPath);
        }
    }
}
#endif
