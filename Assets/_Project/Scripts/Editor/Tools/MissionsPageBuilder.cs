#if UNITY_EDITOR
using System.IO;
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
    /// Gate 4.a — PageMusique → PageMissions (purge nominative + renommage + structure).
    /// Idempotent, Undo-safe. DRY RUN = log ; APPLIQUER = exécution.
    /// </summary>
    public static class MissionsPageBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Missions Page 4.a";
        private const string PageMusiqueName = "PageMusique";
        private const string PageMissionsName = "PageMissions";
        private const string NavName = "NavigationBar";
        private const string MusicPlayerScriptPath =
            "Assets/_Project/Scripts/UI/MusicPlayerUI.cs";
        private const string MusicFolder = "Assets/_Project/Sprites/Music";
        private const string BadgeIconPath = "Assets/_Project/Sprites/UI/UI 0 badge.png";
        private const string MusicNavIconPath = "Assets/_Project/Sprites/UI/UI - music.png";

        /// <summary>
        /// Sprites Music exclusifs (référencés par PageMusique) — purge nominative obligatoire.
        /// </summary>
        private static readonly string[] MusicExclusiveSprites =
        {
            "Music sky - plain.png",
            "Music sky - back cloud.png",
            "Music sky - front cloud.png",
            "TRAIN-Sheet 216 x 165.png",
            "particles.png"
        };

        /// <summary>
        /// Orphelins Sprites/Music/ (déjà sans usage scène hors PageMusique).
        /// </summary>
        private static readonly string[] MusicOrphanSprites =
        {
            "CLOUD 1.png",
            "CLOUD 2.png",
            "CLOUD 3.png",
            "SKY.png",
            "TRAIN.png",
            "Music sky.png",
            "fondhubarene.png"
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Page Missions 4.a (DRY RUN inventaire)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Page Missions 4.a (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Gate 4.a — Page Missions",
                    "Va purger PageMusique, renommer PageMissions, supprimer "
                    + "MusicPlayerUI.cs + sprites Music listés nommément, "
                    + "mettre à jour nav/strings, construire la structure page.\n\n"
                    + "Ctrl+S Hub ensuite. Continuer ?",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(apply: true);
        }

        // ═══════════════════════════════════════════
        // PIPELINE
        // ═══════════════════════════════════════════

        private static void Run(bool apply)
        {
            var log = new StringBuilder(12288);
            string mode = apply ? "APPLIQUER" : "DRY RUN inventaire";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" MissionsPageBuilder — {mode} Gate 4.a");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "Hub")
            {
                Debug.LogError("[MissionsPageBuilder] Ouvre Hub.unity.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            Transform page = FindDeep(scene, PageMissionsName);
            Transform pageMusique = FindDeep(scene, PageMusiqueName);
            if (page == null)
                page = pageMusique;

            // —— 1. Page rename + purge enfants ——
            log.AppendLine("## PageMusique / PageMissions");
            ProcessPageIdentity(page, pageMusique, apply, log, ref todo, ref conforme, ref failed);

            // Re-résout après rename
            page = FindDeep(scene, PageMissionsName);
            if (page == null)
                page = FindDeep(scene, PageMusiqueName);

            if (page != null)
                ProcessPageChildren(page, apply, log, ref todo, ref conforme, ref failed);
            else
            {
                failed++;
                log.AppendLine("- ✗ Page introuvable — abort structure");
            }

            log.AppendLine();

            // —— 2. MusicPlayerUI.cs ——
            log.AppendLine("## Script MusicPlayerUI.cs");
            ProcessMusicPlayerScript(apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 3. Sprites Music (liste nominative) ——
            log.AppendLine("## Sprites Music exclusifs + orphelins (liste nominative)");
            ProcessMusicSprites(apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 4. Strings code (déjà patchés hors scène — vérif) ——
            log.AppendLine("## Strings code (même livraison, pas de renommage silencieux)");
            VerifyCodeStrings(log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 5. Nav scène ——
            log.AppendLine("## Nav Hub (TabDefinition pageIndex 3)");
            ProcessNavTab(scene, apply, log, ref todo, ref conforme, ref failed);
            log.AppendLine();

            // —— 6. Structure page ——
            log.AppendLine("## Structure page cible (4.a)");
            if (page != null)
                ProcessPageStructure(page, scene, apply, log, ref todo, ref conforme, ref failed);
            else
            {
                failed++;
                log.AppendLine("- ✗ Structure skip — page absente");
            }

            log.AppendLine();

            // —— 7. Rappel 4.b ——
            log.AppendLine("## Rappel cartographie data (4.b, pas 4.a)");
            log.AppendLine("- Daily 6 / Weekly 5 / Seasonal 0 SO / Permanent 8 — catalog 19");
            log.AppendLine("- Adaptateur réel pur → MissionManager (zéro mock)");
            log.AppendLine("- Saison = empty state « La saison arrive bientôt » (Caption / TextMuted)");
            log.AppendLine("- Bonus couche conditionnel (Daily/Weekly oui, Permanent non)");
            log.AppendLine("- Mapping UI : InProgress/Completed/Claimed → EN COURS/RÉCLAMABLE/RÉCLAMÉE");
            log.AppendLine("- Locked = réserve UI seulement (enum système intact)");
            log.AppendLine();

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());
        }

        // ═══════════════════════════════════════════
        // ÉTAPES
        // ═══════════════════════════════════════════

        private static void ProcessPageIdentity(
            Transform page,
            Transform pageMusique,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (page != null && page.name == PageMissionsName)
            {
                conforme++;
                log.AppendLine($"- `{GetPath(page)}` déjà renommée ✓");
                return;
            }

            if (pageMusique == null)
            {
                failed++;
                log.AppendLine("- ✗ Ni PageMusique ni PageMissions");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine($"- [DRY] RENOOMMER `{GetPath(pageMusique)}` → PageMissions — À FAIRE");
                return;
            }

            Undo.RecordObject(pageMusique.gameObject, UndoLabel);
            pageMusique.name = PageMissionsName;
            EditorUtility.SetDirty(pageMusique.gameObject);
            conforme++;
            log.AppendLine($"- RENOOMMÉ `{GetPath(pageMusique)}` ✓");
        }

        private static void ProcessPageChildren(
            Transform page,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            string[] purgeNames = { "MusicBackground", "PhoneScreen" };
            for (int i = 0; i < purgeNames.Length; i++)
            {
                Transform child = page.Find(purgeNames[i]);
                if (child == null)
                {
                    conforme++;
                    log.AppendLine($"- `{purgeNames[i]}` déjà absent ✓");
                    continue;
                }

                if (!apply)
                {
                    todo++;
                    log.AppendLine($"- [DRY] PURGER enfant `{GetPath(child)}` — À FAIRE");
                    continue;
                }

                Undo.DestroyObjectImmediate(child.gameObject);
                conforme++;
                log.AppendLine($"- PURGÉ `{purgeNames[i]}` ✓");
            }

            // MusicPlayerUI component sur la page
            MonoBehaviour[] behaviours = page.GetComponents<MonoBehaviour>();
            bool foundPlayer = false;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                    continue;
                if (behaviours[i].GetType().Name != "MusicPlayerUI")
                    continue;

                foundPlayer = true;
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] RETIRER composant MusicPlayerUI sur page — À FAIRE");
                }
                else
                {
                    Undo.DestroyObjectImmediate(behaviours[i]);
                    conforme++;
                    log.AppendLine("- Composant MusicPlayerUI retiré ✓");
                }
            }

            if (!foundPlayer)
            {
                conforme++;
                log.AppendLine("- Composant MusicPlayerUI déjà absent ✓");
            }

            // Image built-in décorative page (fond musique) → transparent / raycast off
            Image pageImg = page.GetComponent<Image>();
            if (pageImg != null)
            {
                if (pageImg.color.a > 0.01f || pageImg.raycastTarget)
                {
                    if (!apply)
                    {
                        todo++;
                        log.AppendLine("- [DRY] Neutraliser Image racine Page (α=0, raycast off) — À FAIRE");
                    }
                    else
                    {
                        Undo.RecordObject(pageImg, UndoLabel);
                        Color c = pageImg.color;
                        c.a = 0f;
                        pageImg.color = c;
                        pageImg.raycastTarget = false;
                        EditorUtility.SetDirty(pageImg);
                        conforme++;
                        log.AppendLine("- Image racine Page neutralisée ✓");
                    }
                }
                else
                {
                    conforme++;
                    log.AppendLine("- Image racine Page déjà neutre ✓");
                }
            }
        }

        private static void ProcessMusicPlayerScript(
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            bool exists = File.Exists(MusicPlayerScriptPath)
                          || AssetDatabase.LoadAssetAtPath<MonoScript>(MusicPlayerScriptPath) != null;
            if (!exists)
            {
                conforme++;
                log.AppendLine($"- `{MusicPlayerScriptPath}` déjà supprimé ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine($"- [DRY] SUPPRIMER fichier `{MusicPlayerScriptPath}` — À FAIRE");
                return;
            }

            if (AssetDatabase.DeleteAsset(MusicPlayerScriptPath))
            {
                conforme++;
                log.AppendLine($"- SUPPRIMÉ `{MusicPlayerScriptPath}` ✓");
            }
            else
            {
                failed++;
                log.AppendLine($"- ✗ Échec suppression `{MusicPlayerScriptPath}`");
            }
        }

        private static void ProcessMusicSprites(
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            log.AppendLine("### Exclusifs (PageMusique)");
            ProcessSpriteList(MusicExclusiveSprites, "exclusif", apply, log, ref todo, ref conforme, ref failed);

            log.AppendLine("### Orphelins");
            ProcessSpriteList(MusicOrphanSprites, "orphelin", apply, log, ref todo, ref conforme, ref failed);

            // Dossier vide ?
            if (AssetDatabase.IsValidFolder(MusicFolder))
            {
                string[] remain = AssetDatabase.FindAssets(string.Empty, new[] { MusicFolder });
                if (remain == null || remain.Length == 0)
                {
                    if (!apply)
                    {
                        todo++;
                        log.AppendLine($"- [DRY] SUPPRIMER dossier vide `{MusicFolder}` — À FAIRE");
                    }
                    else if (AssetDatabase.DeleteAsset(MusicFolder))
                    {
                        conforme++;
                        log.AppendLine($"- Dossier `{MusicFolder}` supprimé ✓");
                    }
                }
                else if (apply)
                {
                    log.AppendLine($"- Dossier `{MusicFolder}` conserve {remain.Length} asset(s) (meta résiduels possibles)");
                }
            }
        }

        private static void ProcessSpriteList(
            string[] fileNames,
            string kind,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = fileNames[i];
                string path = MusicFolder + "/" + fileName;
                bool exists = File.Exists(path)
                              || AssetDatabase.LoadMainAssetAtPath(path) != null;

                if (!exists)
                {
                    conforme++;
                    log.AppendLine($"- `{fileName}` ({kind}) déjà absent ✓");
                    continue;
                }

                if (!apply)
                {
                    todo++;
                    log.AppendLine($"- [DRY] SUPPRIMER `{fileName}` ({kind}) — path `{path}` — À FAIRE");
                    continue;
                }

                if (AssetDatabase.DeleteAsset(path))
                {
                    conforme++;
                    log.AppendLine($"- SUPPRIMÉ `{fileName}` ({kind}) ✓");
                }
                else
                {
                    failed++;
                    log.AppendLine($"- ✗ Échec suppression `{fileName}` ({kind})");
                }
            }
        }

        private static void VerifyCodeStrings(
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            // Gacha fallback
            string gachaPath = "Assets/_Project/Scripts/Gacha/GachaAnimationController.cs";
            string gacha = File.ReadAllText(gachaPath);
            if (gacha.Contains("\"PageMusique\""))
            {
                failed++;
                log.AppendLine("- ✗ GachaAnimationController contient encore \"PageMusique\"");
            }
            else if (gacha.Contains("\"PageMissions\""))
            {
                conforme++;
                log.AppendLine("- GachaAnimationController names[] : PageMissions ✓");
            }
            else
            {
                failed++;
                log.AppendLine("- ✗ GachaAnimationController : string PageMissions introuvable");
            }

            string navPath = "Assets/_Project/Scripts/Editor/Tools/HubNavBuilder.cs";
            string nav = File.ReadAllText(navPath);
            bool navOk = nav.Contains("\"missions\"")
                         && nav.Contains("\"Missions\"")
                         && nav.Contains("\"PageMissions\"")
                         && !nav.Contains("\"PageMusique\"")
                         && !nav.Contains("TabIds = { \"accueil\", \"equipe\", \"invocation\", \"musique\" }");
            if (navOk)
            {
                conforme++;
                log.AppendLine("- HubNavBuilder TabIds/Labels/PageNames → missions ✓");
            }
            else
            {
                failed++;
                log.AppendLine("- ✗ HubNavBuilder encore partiellement musique/PageMusique");
            }

            string hubPath = "Assets/_Project/Scripts/Hub/HubManager.cs";
            string hub = File.ReadAllText(hubPath);
            if (hub.Contains("3 = Musique") || hub.Contains("Invocation, Musique"))
            {
                failed++;
                log.AppendLine("- ✗ HubManager tooltip encore Musique");
            }
            else if (hub.Contains("3 = Missions"))
            {
                conforme++;
                log.AppendLine("- HubManager tooltips index 3 → Missions ✓");
            }
            else
            {
                failed++;
                log.AppendLine("- ✗ HubManager : string Missions index 3 introuvable");
            }
        }

        private static void ProcessNavTab(
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            HubNavBarUI nav = FindNav(scene);
            if (nav == null)
            {
                failed++;
                log.AppendLine("- NavigationBar / HubNavBarUI introuvable ✗");
                return;
            }

            Sprite badge = AssetDatabase.LoadAssetAtPath<Sprite>(BadgeIconPath);
            if (badge == null)
            {
                failed++;
                log.AppendLine($"- ✗ Icône badge introuvable `{BadgeIconPath}`");
                return;
            }

            SerializedObject so = new SerializedObject(nav);
            SerializedProperty tabs = so.FindProperty("tabs");
            if (tabs == null || tabs.arraySize < 4)
            {
                failed++;
                log.AppendLine("- tabs[] invalide ✗");
                return;
            }

            SerializedProperty t3 = tabs.GetArrayElementAtIndex(3);
            string id = t3.FindPropertyRelative("id").stringValue;
            string label = t3.FindPropertyRelative("label").stringValue;
            int pageIndex = t3.FindPropertyRelative("pageIndex").intValue;
            Object icon = t3.FindPropertyRelative("icon").objectReferenceValue;

            bool ok = id == "missions"
                      && label == "Missions"
                      && pageIndex == 3
                      && icon == badge;

            log.AppendLine($"- tab[3] id={id} label={label} pageIndex={pageIndex}");

            if (ok)
            {
                conforme++;
                log.AppendLine("- Tab missions + icône badge ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    "- [DRY] id/label → missions/Missions + icon `UI 0 badge.png` (pageIndex 3) — À FAIRE");
                return;
            }

            Undo.RecordObject(nav, UndoLabel);
            t3.FindPropertyRelative("id").stringValue = "missions";
            t3.FindPropertyRelative("label").stringValue = "Missions";
            t3.FindPropertyRelative("pageIndex").intValue = 3;
            t3.FindPropertyRelative("icon").objectReferenceValue = badge;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(nav);
            nav.Rebuild();

            conforme++;
            log.AppendLine("- Tab[3] → missions / Missions / badge ✓ (Rebuild)");

            // Note : UI - music.png devient orphelin nav — non purgé (hors Sprites/Music/)
            if (AssetDatabase.LoadAssetAtPath<Sprite>(MusicNavIconPath) != null)
                log.AppendLine(
                    $"- Note : `{MusicNavIconPath}` orphelin nav possible (hors scope Music/)");
        }

        private static void ProcessPageStructure(
            Transform page,
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            MissionsPageUI pageUi = page.GetComponent<MissionsPageUI>();
            Transform root = page.Find("MissionsRoot");
            if (pageUi != null && root != null)
            {
                conforme++;
                log.AppendLine("- MissionsRoot + MissionsPageUI déjà présents ✓");
                WirePageRefs(page, scene, apply, log, ref todo, ref conforme, ref failed);
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] CRÉER MissionsRoot (VLG) + TabBarUI 4 onglets — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] CRÉER LayerBonusRow (bordure AccentAmber) — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] CRÉER Scroll + MissionEntryTemplate (4 états) — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] CRÉER SeasonEmpty (PanelSurface + Caption) — À FAIRE");
                todo++;
                log.AppendLine("- [DRY] CÂBLER MissionsPageUI + navBar SetBadge — À FAIRE");
                return;
            }

            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
            if (spriteS == null || spriteM == null || spriteL == null)
            {
                failed++;
                log.AppendLine("- ✗ RoundedRect_S/M/L manquants");
                return;
            }

            // Purge éventuels enfants hors structure (sécurité)
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                Transform c = page.GetChild(i);
                if (c.name == "MissionsRoot")
                    continue;
                // Ne touche plus Music* (déjà purgés)
                if (c.name == "MusicBackground" || c.name == "PhoneScreen")
                    Undo.DestroyObjectImmediate(c.gameObject);
            }

            GameObject rootGo = new GameObject("MissionsRoot", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(rootGo, UndoLabel);
            Undo.SetTransformParent(rootGo.transform, page, false, UndoLabel);
            RectTransform rootRt = (RectTransform)rootGo.transform;
            StretchFull(rootRt);

            VerticalLayoutGroup vlg = Undo.AddComponent<VerticalLayoutGroup>(rootGo);
            int pad = Mathf.RoundToInt(UiTheme.Space4);
            vlg.padding = new RectOffset(pad, pad, pad, pad);
            vlg.spacing = UiTheme.Space3;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            TabBarUI tabBar = CreateTabBar(rootGo.transform, spriteS);
            conforme++;
            log.AppendLine("- TabBarUI 4 sous-onglets créé ✓");

            // Bonus row (AccentAmber) — contient une MissionEntryUI
            RectTransform bonusRoot = CreatePanelChild(
                rootGo.transform, "LayerBonusRow", spriteS, spriteM, spriteL,
                PanelSurface.SurfaceBorder.AccentAmber, preferredH: 160f, flexible: 0f);
            MissionEntryUI bonusEntry = BuildMissionEntry(bonusRoot, "LayerBonusEntry", spriteS, spriteM, spriteL);
            conforme++;
            log.AppendLine("- LayerBonusRow (AccentAmber) ✓");

            // Scroll + content + template
            GameObject scrollGo = new GameObject(
                "MissionScroll",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect));
            Undo.RegisterCreatedObjectUndo(scrollGo, UndoLabel);
            Undo.SetTransformParent(scrollGo.transform, rootGo.transform, false, UndoLabel);
            LayoutElement scrollLe = Undo.AddComponent<LayoutElement>(scrollGo);
            scrollLe.flexibleHeight = 1f;
            scrollLe.minHeight = 200f;
            Image scrollImg = scrollGo.GetComponent<Image>();
            scrollImg.color = new Color(0f, 0f, 0f, 0f);
            scrollImg.raycastTarget = true;

            GameObject viewport = new GameObject(
                "Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            Undo.RegisterCreatedObjectUndo(viewport, UndoLabel);
            Undo.SetTransformParent(viewport.transform, scrollGo.transform, false, UndoLabel);
            StretchFull((RectTransform)viewport.transform);
            Image vpImg = viewport.GetComponent<Image>();
            vpImg.color = Color.white;
            vpImg.raycastTarget = true;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject(
                "Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(content, UndoLabel);
            Undo.SetTransformParent(content.transform, viewport.transform, false, UndoLabel);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup contentVlg = content.GetComponent<VerticalLayoutGroup>();
            contentVlg.spacing = UiTheme.Space2;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            MissionEntryUI template = BuildMissionEntry(
                contentRt, "MissionEntryTemplate", spriteS, spriteM, spriteL);
            template.gameObject.SetActive(false);
            conforme++;
            log.AppendLine("- Scroll + MissionEntryTemplate ✓");

            // Season empty
            RectTransform seasonRt = CreatePanelChild(
                rootGo.transform, "SeasonEmpty", spriteS, spriteM, spriteL,
                PanelSurface.SurfaceBorder.Subtle, preferredH: 180f, flexible: 1f);
            GameObject seasonLabelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(seasonLabelGo, UndoLabel);
            Undo.SetTransformParent(seasonLabelGo.transform, seasonRt, false, UndoLabel);
            StretchFull((RectTransform)seasonLabelGo.transform);
            TextMeshProUGUI seasonTmp = seasonLabelGo.GetComponent<TextMeshProUGUI>();
            seasonTmp.text = "La saison arrive bientôt";
            seasonTmp.fontSize = UiTypography.Caption;
            seasonTmp.color = UiTheme.TextMuted;
            seasonTmp.alignment = TextAlignmentOptions.Center;
            seasonTmp.raycastTarget = false;
            seasonRt.gameObject.SetActive(false);
            conforme++;
            log.AppendLine("- SeasonEmpty PanelSurface + Caption ✓");

            // MissionsPageUI
            if (pageUi == null)
                pageUi = Undo.AddComponent<MissionsPageUI>(page.gameObject);

            HubNavBarUI nav = FindNav(scene);
            SerializedObject pageSo = new SerializedObject(pageUi);
            pageSo.FindProperty("tabBar").objectReferenceValue = tabBar;
            pageSo.FindProperty("layerBonusRoot").objectReferenceValue = bonusRoot;
            pageSo.FindProperty("layerBonusEntry").objectReferenceValue = bonusEntry;
            pageSo.FindProperty("missionScroll").objectReferenceValue = scroll;
            pageSo.FindProperty("listContent").objectReferenceValue = contentRt;
            pageSo.FindProperty("entryTemplate").objectReferenceValue = template;
            pageSo.FindProperty("seasonEmptyRoot").objectReferenceValue = seasonRt.gameObject;
            pageSo.FindProperty("seasonEmptyLabel").objectReferenceValue = seasonTmp;
            pageSo.FindProperty("navBar").objectReferenceValue = nav;
            pageSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pageUi);

            conforme++;
            log.AppendLine("- MissionsPageUI câblé (provider = 4.b) + SetBadge tab missions ✓");
            log.AppendLine("- IMissionProvider non branché ici (adaptateur réel = gate 4.b) ✓");
        }

        private static void WirePageRefs(
            Transform page,
            Scene scene,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            MissionsPageUI pageUi = page.GetComponent<MissionsPageUI>();
            if (pageUi == null)
                return;

            HubNavBarUI nav = FindNav(scene);
            SerializedObject so = new SerializedObject(pageUi);
            if (so.FindProperty("navBar").objectReferenceValue == null && nav != null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Câbler navBar sur MissionsPageUI — À FAIRE");
                }
                else
                {
                    so.FindProperty("navBar").objectReferenceValue = nav;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(pageUi);
                    conforme++;
                    log.AppendLine("- navBar câblé ✓");
                }
            }
            else
            {
                conforme++;
                log.AppendLine("- MissionsPageUI refs OK ✓");
            }
        }

        // ═══════════════════════════════════════════
        // FACTORY UI
        // ═══════════════════════════════════════════

        private static TabBarUI CreateTabBar(Transform parent, Sprite spriteS)
        {
            GameObject barGo = new GameObject("TabBar", typeof(RectTransform), typeof(TabBarUI));
            Undo.RegisterCreatedObjectUndo(barGo, UndoLabel);
            Undo.SetTransformParent(barGo.transform, parent, false, UndoLabel);
            LayoutElement barLe = Undo.AddComponent<LayoutElement>(barGo);
            barLe.minHeight = UiTheme.TouchTargetMin;
            barLe.preferredHeight = UiTheme.TouchTargetMin;
            barLe.flexibleWidth = 1f;

            GameObject template = new GameObject(
                "TabItemTemplate",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(template, UndoLabel);
            Undo.SetTransformParent(template.transform, barGo.transform, false, UndoLabel);
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
            RectTransform fillRt = (RectTransform)fillGo.transform;
            StretchFull(fillRt);
            float inset = UiTheme.BorderThin;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
            Undo.SetTransformParent(labelGo.transform, template.transform, false, UndoLabel);
            StretchFull((RectTransform)labelGo.transform);
            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = UiTypography.Label;
            tmp.color = UiTheme.TextSecondary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            TabBarUI tabBar = barGo.GetComponent<TabBarUI>();
            SerializedObject so = new SerializedObject(tabBar);
            so.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            so.FindProperty("tabItemTemplate").objectReferenceValue = template;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tabBar;
        }

        private static RectTransform CreatePanelChild(
            Transform parent,
            string name,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            PanelSurface.SurfaceBorder border,
            float preferredH,
            float flexible)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = preferredH;
            le.preferredHeight = preferredH;
            le.flexibleHeight = flexible;
            le.flexibleWidth = 1f;

            PanelSurface surface = Undo.AddComponent<PanelSurface>(go);
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Panel;
            surfaceSo.FindProperty("borderStyle").enumValueIndex = (int)border;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();

            return (RectTransform)go.transform;
        }

        private static MissionEntryUI BuildMissionEntry(
            Transform parent,
            string name,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);

            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = 148f;
            le.preferredHeight = 148f;
            le.flexibleWidth = 1f;

            PanelSurface surface = Undo.AddComponent<PanelSurface>(go);
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Card;
            surfaceSo.FindProperty("borderStyle").enumValueIndex =
                (int)PanelSurface.SurfaceBorder.Subtle;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();

            // Contenu interne
            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(VerticalLayoutGroup));
            Undo.RegisterCreatedObjectUndo(body, UndoLabel);
            Undo.SetTransformParent(body.transform, go.transform, false, UndoLabel);
            StretchFull((RectTransform)body.transform);
            float inset = UiTheme.Space3;
            RectTransform bodyRt = (RectTransform)body.transform;
            bodyRt.offsetMin = new Vector2(inset, inset);
            bodyRt.offsetMax = new Vector2(-inset, -inset);
            VerticalLayoutGroup bodyVlg = body.GetComponent<VerticalLayoutGroup>();
            bodyVlg.spacing = UiTheme.Space1;
            bodyVlg.childControlWidth = true;
            bodyVlg.childControlHeight = true;
            bodyVlg.childForceExpandWidth = true;
            bodyVlg.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateTmp(body.transform, "Title", "Mission", UiTypography.Label, UiTheme.TextPrimary);
            TextMeshProUGUI state = CreateTmp(body.transform, "StateLabel", "EN COURS", UiTypography.Caption, UiTheme.TextSecondary);

            GameObject trackGo = new GameObject(
                "ProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(trackGo, UndoLabel);
            Undo.SetTransformParent(trackGo.transform, body.transform, false, UndoLabel);
            LayoutElement trackLe = Undo.AddComponent<LayoutElement>(trackGo);
            trackLe.minHeight = 12f;
            trackLe.preferredHeight = 12f;
            Image trackImg = trackGo.GetComponent<Image>();
            trackImg.color = UiTheme.BorderSubtle;
            trackImg.raycastTarget = false;

            GameObject fillGo = new GameObject(
                "ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(fillGo, UndoLabel);
            Undo.SetTransformParent(fillGo.transform, trackGo.transform, false, UndoLabel);
            RectTransform fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(0f, 0f);
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.color = UiTheme.AccentAmber;
            fillImg.raycastTarget = false;

            TextMeshProUGUI progress = CreateTmp(
                body.transform, "ProgressText", "0/1", UiTypography.Caption, UiTheme.TextMuted);
            TextMeshProUGUI reward = CreateTmp(
                body.transform, "RewardText", "+0 Tals", UiTypography.Caption, UiTheme.AccentGold);

            // Claim button
            GameObject claimGo = new GameObject(
                "ClaimButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            Undo.RegisterCreatedObjectUndo(claimGo, UndoLabel);
            Undo.SetTransformParent(claimGo.transform, body.transform, false, UndoLabel);
            LayoutElement claimLe = Undo.AddComponent<LayoutElement>(claimGo);
            claimLe.minHeight = UiTheme.TouchTargetMin * 0.75f;
            claimLe.preferredHeight = UiTheme.TouchTargetMin * 0.75f;
            Image claimImg = claimGo.GetComponent<Image>();
            claimImg.sprite = spriteS;
            claimImg.type = Image.Type.Sliced;
            claimImg.color = UiTheme.AccentGold;
            TextMeshProUGUI claimLabel = CreateTmp(
                claimGo.transform, "Label", "Réclamer", UiTypography.Label, UiTheme.TextPrimary);
            claimGo.SetActive(false);

            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(check, UndoLabel);
            Undo.SetTransformParent(check.transform, body.transform, false, UndoLabel);
            TextMeshProUGUI checkTmp = check.GetComponent<TextMeshProUGUI>();
            checkTmp.text = "✓";
            checkTmp.fontSize = UiTypography.Body;
            checkTmp.color = UiTheme.Success;
            checkTmp.alignment = TextAlignmentOptions.Center;
            checkTmp.raycastTarget = false;
            check.SetActive(false);

            GameObject lockGo = new GameObject("LockIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(lockGo, UndoLabel);
            Undo.SetTransformParent(lockGo.transform, body.transform, false, UndoLabel);
            TextMeshProUGUI lockTmp = lockGo.GetComponent<TextMeshProUGUI>();
            lockTmp.text = "VERROU";
            lockTmp.fontSize = UiTypography.Caption;
            lockTmp.color = UiTheme.TextMuted;
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.raycastTarget = false;
            lockGo.SetActive(false);

            MissionEntryUI entry = Undo.AddComponent<MissionEntryUI>(go);
            SerializedObject so = new SerializedObject(entry);
            so.FindProperty("surface").objectReferenceValue = surface;
            so.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("progressText").objectReferenceValue = progress;
            so.FindProperty("rewardText").objectReferenceValue = reward;
            so.FindProperty("stateLabel").objectReferenceValue = state;
            so.FindProperty("progressFill").objectReferenceValue = fillImg;
            so.FindProperty("progressTrack").objectReferenceValue = (RectTransform)trackGo.transform;
            so.FindProperty("claimButton").objectReferenceValue = claimGo.GetComponent<Button>();
            so.FindProperty("claimButtonLabel").objectReferenceValue = claimLabel;
            so.FindProperty("checkmark").objectReferenceValue = check;
            so.FindProperty("lockIcon").objectReferenceValue = lockGo;
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent,
            string name,
            string text,
            float size,
            Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(go, UndoLabel);
            Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            LayoutElement le = Undo.AddComponent<LayoutElement>(go);
            le.minHeight = size + 4f;
            le.preferredHeight = size + 8f;
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

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

        private static HubNavBarUI FindNav(Scene scene)
        {
            Transform t = FindDeep(scene, NavName);
            return t != null ? t.GetComponent<HubNavBarUI>() : null;
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
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
#endif
