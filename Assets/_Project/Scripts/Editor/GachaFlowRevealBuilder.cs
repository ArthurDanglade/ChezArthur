#if UNITY_EDITOR
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.Gacha;
using ChezArthur.UI;
using ChezArthur.UI.RevealStage;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Câble le flow gacha « Entrée en scène » (INVR2) — idempotent.
    /// SmokeTransition conservé (partagé avec le train).
    /// </summary>
    public static class GachaFlowRevealBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string HubScenePath = "Assets/_Project/Scenes/Hub.unity";
        private const string ConfigPath = "Assets/_Project/Data/UI/RevealStageConfig.asset";
        private const string MatPath = "Assets/_Project/Art/FX/RevealLight.mat";
        private const string ReportRelPath = "Audits/gacha_flow_invr3_build.txt";

        private const string DirectorGoName = "RevealDirector";
        private const string SkipAllGoName = "BtnSkipAll";

        private const string EntryRiserPath =
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_entry_riser.wav";
        private const string SnapSrPath =
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_sr.wav";
        private const string SnapSsrPath =
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_ssr.wav";
        private const string SnapLrPath =
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_snap_lr.wav";
        private const string StampPath =
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_stamp.wav";
        private const string ExitDimPath =
            "Assets/_Project/Audio/SFX/Reveal/sfx_inv_exit_dim.wav";
        private const string StatsUpPath =
            "Assets/_Project/Audio/SFX/statsupsound.wav";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Reveal/Câbler le flow gacha (INVR2)")]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>Point d'entrée idempotent (MenuItem + script).</summary>
        public static void Build()
        {
            var report = new StringBuilder(8192);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" BUILD Gacha Flow Reveal INVR3");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();
            report.AppendLine("NOTE : SmokeTransition CONSERVÉ (partagé train).");
            report.AppendLine("NOTE : AW intouché. Banque sonore INVR3 masterisée.");
            report.AppendLine();

            bool anyChange = false;

            GachaAnimationController controller = FindControllerPreferOpenScenes(report);
            if (controller == null)
            {
                report.AppendLine("Aucun contrôleur en scènes ouvertes — ouverture Hub.unity.");
                Scene hub = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
                report.AppendLine($"Scène ouverte : {hub.path}");
                controller = FindControllerInLoadedScenes();
            }

            if (controller == null)
            {
                report.AppendLine("ÉCHEC : GachaAnimationController introuvable.");
                WriteReport(report);
                Debug.LogError("[GachaFlowRevealBuilder] GachaAnimationController introuvable.");
                return;
            }

            report.AppendLine(
                $"Contrôleur : '{controller.name}' (scène '{controller.gameObject.scene.path}').");

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty revealProp = so.FindProperty("revealScene");
            GameObject revealScene = revealProp != null
                ? revealProp.objectReferenceValue as GameObject
                : null;

            if (revealScene == null)
            {
                report.AppendLine("ÉCHEC : revealScene null sur le contrôleur.");
                WriteReport(report);
                Debug.LogError("[GachaFlowRevealBuilder] revealScene manquant.");
                return;
            }

            report.AppendLine($"revealScene : '{revealScene.name}'.");

            RevealStageConfig config =
                AssetDatabase.LoadAssetAtPath<RevealStageConfig>(ConfigPath);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (config == null)
            {
                report.AppendLine($"ÉCHEC : config manquante → {ConfigPath}");
                WriteReport(report);
                return;
            }

            if (mat == null)
            {
                report.AppendLine($"ÉCHEC : matériau manquant → {MatPath}");
                WriteReport(report);
                return;
            }

            // ── Director GO ──
            Transform dirT = revealScene.transform.Find(DirectorGoName);
            GameObject dirGo;
            if (dirT == null)
            {
                dirGo = new GameObject(DirectorGoName, typeof(RectTransform));
                dirGo.transform.SetParent(revealScene.transform, false);
                StretchFull(dirGo.GetComponent<RectTransform>());
                anyChange = true;
                report.AppendLine($"Director : GO créé '{DirectorGoName}'.");
            }
            else
            {
                dirGo = dirT.gameObject;
                report.AppendLine("Director : GO déjà présent (idempotent).");
            }

            RevealStageDirector director = dirGo.GetComponent<RevealStageDirector>();
            if (director == null)
            {
                director = dirGo.AddComponent<RevealStageDirector>();
                anyChange = true;
                report.AppendLine("Director : composant RevealStageDirector ajouté.");
            }

            director.Wire(config, mat);
            EditorUtility.SetDirty(director);
            report.AppendLine("Director : Wire(config, mat) OK.");

            // ── Skip-all button ──
            Button skipBtn = EnsureSkipAllButton(revealScene.transform, report, ref anyChange);

            // ── Wire controller ──
            if (WireRef(so, "revealDirector", director, report)) anyChange = true;
            if (WireRef(so, "revealConfig", config, report)) anyChange = true;
            if (WireRef(so, "skipAllButton", skipBtn, report)) anyChange = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // ── Purge GOs legacy labels ──
            anyChange |= DestroyChildIfPresent(revealScene.transform, "CharacterNameText", report);
            anyChange |= DestroyChildIfPresent(revealScene.transform, "CharacterRarityText", report);
            anyChange |= DestroyChildIfPresent(revealScene.transform, "StatusText", report);

            // SmokeTransition : CONSERVÉ (train)
            Transform smoke = FindDeep(revealScene.transform.root, "SmokeTransition");
            if (smoke == null)
            {
                // Cherche aussi sous le controller / canvas
                smoke = FindDeep(controller.transform.root, "SmokeTransition");
            }

            if (smoke != null)
                report.AppendLine("SmokeTransition : présent (conservé pour le train).");
            else
                report.AppendLine("WARN : SmokeTransition introuvable dans la hiérarchie (train ?).");

            // DoorScene : supprimer seulement si orphelin
            HandleDoorScene(controller, report, ref anyChange);

            // ── Clips définitifs INVR3 ──
            if (AssignDefinitiveClips(config, report))
            {
                anyChange = true;
                EditorUtility.SetDirty(config);
            }

            if (EnforceRevealAudioImports(report))
                anyChange = true;

            if (anyChange)
            {
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                AssetDatabase.SaveAssets();
            }

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(
                anyChange
                    ? " Build terminé — des changements ont été appliqués."
                    : " Build terminé (idempotent). Relance = zéro changement attendu.");
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log(report.ToString());
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static bool AssignDefinitiveClips(RevealStageConfig config, StringBuilder report)
        {
            AudioClip riser = AssetDatabase.LoadAssetAtPath<AudioClip>(EntryRiserPath);
            AudioClip snapSr = AssetDatabase.LoadAssetAtPath<AudioClip>(SnapSrPath);
            AudioClip snapSsr = AssetDatabase.LoadAssetAtPath<AudioClip>(SnapSsrPath);
            AudioClip snapLr = AssetDatabase.LoadAssetAtPath<AudioClip>(SnapLrPath);
            AudioClip stamp = AssetDatabase.LoadAssetAtPath<AudioClip>(StampPath);
            AudioClip exitDim = AssetDatabase.LoadAssetAtPath<AudioClip>(ExitDimPath);
            AudioClip stats = AssetDatabase.LoadAssetAtPath<AudioClip>(StatsUpPath);

            SerializedObject cfgSo = new SerializedObject(config);
            bool changed = false;
            changed |= SetClip(cfgSo, "entryRiserClip", riser, report, "entryRiserClip ← sfx_inv_entry_riser");
            changed |= SetClip(cfgSo, "snapSrClip", snapSr, report, "snapSrClip ← sfx_inv_snap_sr");
            changed |= SetClip(cfgSo, "snapSsrClip", snapSsr, report, "snapSsrClip ← sfx_inv_snap_ssr");
            changed |= SetClip(cfgSo, "snapLrClip", snapLr, report, "snapLrClip ← sfx_inv_snap_lr");
            changed |= SetClip(cfgSo, "stampClip", stamp, report, "stampClip ← sfx_inv_stamp");
            changed |= SetClip(cfgSo, "statTickClip", stats, report, "statTickClip ← statsupsound");
            changed |= SetClip(cfgSo, "exitDimClip", exitDim, report, "exitDimClip ← sfx_inv_exit_dim");

            if (changed)
                cfgSo.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        /// <summary>
        /// Import settings INVR3 : mono, DecompressOnLoad, Vorbis q70, preload.
        /// </summary>
        private static bool EnforceRevealAudioImports(StringBuilder report)
        {
            report.AppendLine("── Import settings audio ──");
            string[] paths =
            {
                EntryRiserPath, SnapSrPath, SnapSsrPath, SnapLrPath,
                StampPath, ExitDimPath, StatsUpPath
            };

            bool any = false;
            for (int i = 0; i < paths.Length; i++)
            {
                AudioImporter importer =
                    AssetImporter.GetAtPath(paths[i]) as AudioImporter;
                if (importer == null)
                {
                    report.AppendLine($"WARN : importer absent → {paths[i]}");
                    continue;
                }

                bool dirty = false;
                if (!importer.forceToMono)
                {
                    importer.forceToMono = true;
                    dirty = true;
                }

                AudioImporterSampleSettings sample = importer.defaultSampleSettings;
                if (sample.loadType != AudioClipLoadType.DecompressOnLoad
                    || sample.compressionFormat != AudioCompressionFormat.Vorbis
                    || Mathf.Abs(sample.quality - 0.7f) > 0.001f)
                {
                    sample.loadType = AudioClipLoadType.DecompressOnLoad;
                    sample.compressionFormat = AudioCompressionFormat.Vorbis;
                    sample.quality = 0.7f;
                    importer.defaultSampleSettings = sample;
                    dirty = true;
                }

                if (!importer.preloadAudioData)
                {
                    importer.preloadAudioData = true;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    any = true;
                    report.AppendLine($"Import : corrigé → {paths[i]}");
                }
                else
                {
                    report.AppendLine($"Import : conforme → {paths[i]}");
                }
            }

            return any;
        }

        private static bool SetClip(
            SerializedObject so,
            string propName,
            AudioClip clip,
            StringBuilder report,
            string okMsg)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p == null)
            {
                report.AppendLine($"WARN : propriété SO absente : {propName}");
                return false;
            }

            if (clip == null)
            {
                report.AppendLine($"WARN : clip introuvable pour {propName}");
                return false;
            }

            if (p.objectReferenceValue == clip)
            {
                report.AppendLine($"Clip : {propName} déjà OK.");
                return false;
            }

            p.objectReferenceValue = clip;
            report.AppendLine($"Clip : {okMsg}");
            return true;
        }

        private static Button EnsureSkipAllButton(
            Transform revealRoot,
            StringBuilder report,
            ref bool anyChange)
        {
            Transform existing = revealRoot.Find(SkipAllGoName);
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(
                    SkipAllGoName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button));
                go.transform.SetParent(revealRoot, false);
                anyChange = true;
                report.AppendLine($"SkipAll : GO créé '{SkipAllGoName}'.");
            }
            else
            {
                go = existing.gameObject;
                report.AppendLine("SkipAll : GO déjà présent.");
            }

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(220f, 64f);
            // Coin haut-droit sous SafeArea (marge ~48 px)
            rt.anchoredPosition = new Vector2(-24f, -72f);

            Image img = go.GetComponent<Image>();
            img.color = new Color(
                UiTheme.GachaStageCharcoal.r,
                UiTheme.GachaStageCharcoal.g,
                UiTheme.GachaStageCharcoal.b,
                0.72f);
            img.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            if (btn == null)
                btn = go.AddComponent<Button>();

            // Label TMP
            Transform labelT = go.transform.Find("Label");
            TextMeshProUGUI tmp;
            if (labelT == null)
            {
                GameObject labelGo = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(go.transform, false);
                StretchFull(labelGo.GetComponent<RectTransform>());
                tmp = labelGo.GetComponent<TextMeshProUGUI>();
                anyChange = true;
            }
            else
            {
                tmp = labelT.GetComponent<TextMeshProUGUI>();
            }

            if (tmp != null)
            {
                tmp.text = "Tout passer";
                tmp.color = UiTheme.TextPrimary;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28f;
                tmp.raycastTarget = false;
                if (TMP_Settings.defaultFontAsset != null)
                    tmp.font = TMP_Settings.defaultFontAsset;
            }

            if (go.activeSelf)
            {
                go.SetActive(false);
                anyChange = true;
                report.AppendLine("SkipAll : désactivé par défaut.");
            }
            else
            {
                report.AppendLine("SkipAll : déjà inactif.");
            }

            return btn;
        }

        private static void HandleDoorScene(
            GachaAnimationController controller,
            StringBuilder report,
            ref bool anyChange)
        {
            Transform door = FindDeep(controller.transform.root, "DoorScene");
            if (door == null)
            {
                report.AppendLine("DoorScene : absent (OK).");
                return;
            }

            // Références : autres MonoBehaviours (hors GAC) ?
            bool externalRef = false;
            MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i] is GachaAnimationController)
                    continue;
                SerializedObject so = new SerializedObject(all[i]);
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    if (it.objectReferenceValue == null)
                        continue;
                    Transform t = null;
                    if (it.objectReferenceValue is GameObject go)
                        t = go.transform;
                    else if (it.objectReferenceValue is Component c)
                        t = c.transform;
                    if (t != null && (t == door || t.IsChildOf(door)))
                    {
                        externalRef = true;
                        report.AppendLine(
                            $"DoorScene : référencé par '{all[i].GetType().Name}' → conservation.");
                        break;
                    }
                }

                if (externalRef)
                    break;
            }

            if (externalRef)
            {
                if (door.gameObject.activeSelf)
                {
                    door.gameObject.SetActive(false);
                    anyChange = true;
                    report.AppendLine("WARN : DoorScene désactivé (références externes).");
                }
                else
                {
                    report.AppendLine("WARN : DoorScene déjà inactif (références externes).");
                }

                return;
            }

            Object.DestroyImmediate(door.gameObject);
            anyChange = true;
            report.AppendLine("DoorScene : supprimé (aucune référence externe).");
        }

        private static bool DestroyChildIfPresent(
            Transform parent,
            string name,
            StringBuilder report)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                // Cherche profond
                t = FindDeep(parent, name);
            }

            if (t == null)
            {
                report.AppendLine($"Purge GO : '{name}' déjà absent.");
                return false;
            }

            Object.DestroyImmediate(t.gameObject);
            report.AppendLine($"Purge GO : '{name}' supprimé.");
            return true;
        }

        private static bool WireRef(
            SerializedObject so,
            string propName,
            Object value,
            StringBuilder report)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p == null)
            {
                report.AppendLine($"Wire : propriété absente '{propName}' (code INVR2 pas compilé ?).");
                return false;
            }

            if (p.objectReferenceValue == value)
            {
                report.AppendLine($"Wire : {propName} déjà OK.");
                return false;
            }

            p.objectReferenceValue = value;
            report.AppendLine($"Wire : {propName} ← {(value != null ? value.name : "null")}.");
            return true;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static GachaAnimationController FindControllerPreferOpenScenes(
            StringBuilder report)
        {
            GachaAnimationController c = FindControllerInLoadedScenes();
            if (c != null)
                report.AppendLine("Contrôleur trouvé dans les scènes ouvertes.");
            return c;
        }

        private static GachaAnimationController FindControllerInLoadedScenes()
        {
            GachaAnimationController[] all =
                Object.FindObjectsOfType<GachaAnimationController>(true);
            return all != null && all.Length > 0 ? all[0] : null;
        }

        private static void WriteReport(StringBuilder report)
        {
            string full = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", ReportRelPath));
            string dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(full, report.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
#endif
