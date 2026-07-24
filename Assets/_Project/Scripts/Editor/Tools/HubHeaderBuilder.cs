#if UNITY_EDITOR
using System.Text;
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
    /// Gate 2.1 — Header Hub Option A : Header sous SafeRoot + HubHeaderSafeBleed.
    /// Idempotent, Undo-safe. Harnais v2 : À FAIRE / CONFORMES / ÉCHECS.
    /// </summary>
    public static class HubHeaderBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string UndoLabel = "Hub Header 2.1";
        private const string SafeRootName = "SafeRoot";
        private const string PageContainerName = "PageContainer";
        private const string HeaderName = "Header";
        private const string InfoBarName = "InfoBar";
        private const string CircuitGuid = "af4186dd3a542bd4fa83503ba0ae9f83";
        private const float PillHeight = 64f;
        private const float TalsIconSize = 48f;
        private const float CircuitHeight = 1080f * 51f / 228f;
        private static readonly Color CircuitTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        private static readonly string[] RequiredHeaderChildren =
        {
            "HeaderBackdrop", "CircuitBackdrop", "BottomHairline",
            "PillIdentity", "PillStage", "PillTals"
        };

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/Refonte Hub/Construire le Header (DRY RUN)")]
        public static void DryRun()
        {
            Run(apply: false);
        }

        [MenuItem("Chez Arthur/Refonte Hub/Construire le Header (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Construire le Header Hub",
                    "Va rebuild Header Option A SafeBleed under SafeRoot.\nContinuer ?",
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
            var log = new StringBuilder(8192);
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" HubHeaderBuilder — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine(" Convergence = À FAIRE : 0");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            int todo = 0;
            int conforme = 0;
            int failed = 0;

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError("[HubHeaderBuilder] Aucune scène active chargée.");
                return;
            }

            log.AppendLine($"Scène : `{scene.name}`");
            log.AppendLine();

            RectTransform safeRoot = FindSafeRoot(scene);
            if (safeRoot == null)
            {
                Debug.LogError("[HubHeaderBuilder] SafeRoot introuvable sous HubCanvas.");
                log.AppendLine("- ✗ SafeRoot introuvable — abort");
                failed++;
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            // 1. Audit SafeRoot
            AuditSafeRoot(safeRoot, apply, log, ref todo, ref conforme, ref failed);

            RectTransform pageContainer = FindDirectChild(safeRoot, PageContainerName);
            if (pageContainer == null)
            {
                failed++;
                log.AppendLine("- ✗ PageContainer introuvable sous SafeRoot");
                Debug.LogError("[HubHeaderBuilder] PageContainer introuvable.");
            }

            Sprite circuitSprite = LoadCircuitSprite();
            Sprite coinSprite = UiGen.LoadSprite(UiTheme.SpriteCoin);
            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            Sprite spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
            Sprite spriteL = RoundedRectSpriteGenerator.LoadSpriteL();

            if (circuitSprite == null)
            {
                failed++;
                log.AppendLine("- ✗ Sprite circuit (UI - New header.png) introuvable");
                Debug.LogError("[HubHeaderBuilder] Circuit sprite GUID introuvable.");
            }
            else
            {
                log.AppendLine($"- Circuit sprite : `{AssetDatabase.GetAssetPath(circuitSprite)}` ({circuitSprite.rect.width}×{circuitSprite.rect.height})");
            }

            if (coinSprite == null)
            {
                failed++;
                log.AppendLine($"- ✗ Sprite `{UiTheme.SpriteCoin}` introuvable");
                Debug.LogError("[HubHeaderBuilder] tals_coin introuvable.");
            }

            if (spriteS == null || spriteM == null || spriteL == null)
            {
                log.AppendLine("- ⚠ RoundedRect S/M/L manquants — génération…");
                if (apply)
                {
                    RoundedRectSpriteGenerator.GenerateAll();
                    spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
                    spriteM = RoundedRectSpriteGenerator.LoadSpriteM();
                    spriteL = RoundedRectSpriteGenerator.LoadSpriteL();
                }

                if (spriteS == null || spriteM == null || spriteL == null)
                {
                    failed++;
                    log.AppendLine("- ✗ RoundedRect toujours manquants");
                    Debug.LogError("[HubHeaderBuilder] RoundedRect sprites manquants.");
                }
            }

            log.AppendLine($"- HeaderHeight : {UiTheme.HeaderHeight}");
            log.AppendLine($"- CircuitHeight : {CircuitHeight:F2}");
            log.AppendLine("- Architecture : Option A — Header + HubHeaderSafeBleed sous SafeRoot");
            log.AppendLine("- TODO : icône étage/progression absente — PillStage texte seul (asset à créer).");
            log.AppendLine();

            bool canBuild = circuitSprite != null && coinSprite != null
                            && spriteS != null && pageContainer != null;

            // 2. EnsureHeaderHierarchy
            log.AppendLine("## Header sous SafeRoot (Option A)");
            log.AppendLine();

            HubHeaderUI headerUi = null;
            if (!canBuild)
            {
                failed++;
                log.AppendLine("- ✗ Préconditions KO — construction Header annulée");
            }
            else
            {
                headerUi = EnsureHeaderHierarchy(
                    safeRoot,
                    pageContainer,
                    circuitSprite,
                    coinSprite,
                    spriteS,
                    spriteM,
                    spriteL,
                    apply,
                    log,
                    ref todo,
                    ref conforme,
                    ref failed);
            }

            // 3. SafeRoot.conformTop = false + HubHeaderSafeBleed
            log.AppendLine();
            log.AppendLine("## SafeRoot.conformTop = false (haut physique)");
            EnsureSafeRootReachesPhysicalTop(safeRoot, apply, log, ref todo, ref conforme, ref failed);

            log.AppendLine();
            log.AppendLine("## HubHeaderSafeBleed (visuels en haut, pills safe)");
            EnsureHubHeaderSafeBleed(
                safeRoot,
                headerUi,
                apply,
                log,
                ref todo,
                ref conforme,
                ref failed);

            // 4. PageContainer full-bleed
            log.AppendLine();
            log.AppendLine("## PageContainer full-bleed (sous Header)");
            ApplyPageContainerFullBleed(pageContainer, apply, log, ref todo, ref conforme, ref failed);

            // 5. TryRemoveInfoBar
            log.AppendLine();
            log.AppendLine("## Suppression InfoBar");
            TryRemoveInfoBar(safeRoot, headerUi, apply, log, ref todo, ref conforme, ref failed);

            // 6. FixNavigationBar
            log.AppendLine();
            log.AppendLine("## NavigationBar (pivot bas + NavHeight)");
            FixNavigationBar(safeRoot, apply, log, ref todo, ref conforme, ref failed);

            // 7. ForceSiblingOrder
            log.AppendLine();
            log.AppendLine("## Ordre SafeRoot (PageContainer → Header → NavigationBar)");
            ForceSiblingOrder(safeRoot, pageContainer, headerUi, apply, log, ref todo, ref conforme, ref failed);

            if (apply)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                log.AppendLine();
                log.AppendLine("Scène marquée dirty — pense à sauvegarder (Ctrl+S).");
            }

            log.AppendLine();
            log.AppendLine("## OUTILS ÉDITEUR");
            log.AppendLine(
                "- `SafeAreaWrapper.cs` : supprimé (menu legacy Take Five Games, zéro référence code).");

            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());

            if (apply && failed > 0)
            {
                Debug.LogError(
                    $"[HubHeaderBuilder] APPLIQUER INCOMPLET — {failed} échec(s), " +
                    $"À FAIRE={todo}, CONFORMES={conforme}. Voir log.");
            }
            else if (apply && todo > 0)
            {
                Debug.LogError(
                    $"[HubHeaderBuilder] APPLIQUER ÉCART — À FAIRE restant = {todo} (attendu 0).");
            }
            else if (apply)
            {
                Debug.Log(
                    $"[HubHeaderBuilder] APPLIQUER OK — À FAIRE=0, CONFORMES={conforme}, ÉCHECS=0.");
            }
            else if (todo == 0 && failed == 0)
            {
                Debug.Log(
                    $"[HubHeaderBuilder] DRY RUN — convergence OK (À FAIRE=0, CONFORMES={conforme}).");
            }
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine();
            log.AppendLine("## COMPTEUR D'ACTIONS (harnais v2)");
            log.AppendLine($"- À FAIRE : {todo}");
            log.AppendLine($"- CONFORMES : {conforme}");
            log.AppendLine($"- ÉCHECS : {failed}");
            if (todo == 0 && failed == 0)
                log.AppendLine("- Convergence : OUI (À FAIRE = 0)");
            else
                log.AppendLine("- Convergence : NON");
        }

        // ═══════════════════════════════════════════
        // AUDIT SAFEROOT
        // ═══════════════════════════════════════════

        private static void AuditSafeRoot(
            RectTransform safeRoot,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            log.AppendLine("## AUDIT SafeRoot — composants");
            Component[] comps = safeRoot.GetComponents<Component>();
            SafeAreaFitter fitter = null;
            Component legacyWrapper = null;

            for (int i = 0; i < comps.Length; i++)
            {
                Component c = comps[i];
                if (c == null) continue;
                string typeName = c.GetType().Name;
                log.AppendLine($"- `{typeName}`");

                if (c is SafeAreaFitter saf)
                    fitter = saf;

                if (typeName == "SafeAreaWrapper" && !(c is Transform) && !(c is RectTransform))
                    legacyWrapper = c;
            }

            if (fitter != null)
                log.AppendLine("- SafeAreaFitter : présent ✓");
            else
                log.AppendLine("- SafeAreaFitter : absent ⚠ (hors scope builder Header)");

            if (legacyWrapper == null)
            {
                conforme++;
                log.AppendLine("- Aucun composant SafeAreaWrapper legacy — conforme ✓");
                log.AppendLine();
                return;
            }

            if (fitter == null)
            {
                if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] SafeAreaWrapper legacy présent (sans SafeAreaFitter) — À FAIRE");
                }
                else
                {
                    failed++;
                    log.AppendLine("- SafeAreaWrapper legacy sans SafeAreaFitter — ÉCHEC (cas non géré) ✗");
                }

                log.AppendLine();
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] SUPPRIMER composant legacy `SafeAreaWrapper` — À FAIRE");
                log.AppendLine();
                return;
            }

            Undo.DestroyObjectImmediate(legacyWrapper);
            if (safeRoot.GetComponent(legacyWrapper.GetType()) == null)
            {
                conforme++;
                log.AppendLine("- SUPPRIMER `SafeAreaWrapper` legacy ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- SUPPRIMER `SafeAreaWrapper` — ÉCHEC ✗");
                Debug.LogError("[HubHeaderBuilder] Échec suppression SafeAreaWrapper legacy.");
            }

            log.AppendLine();
        }

        // ═══════════════════════════════════════════
        // HEADER HIERARCHY (Option A)
        // ═══════════════════════════════════════════

        private static HubHeaderUI EnsureHeaderHierarchy(
            RectTransform safeRoot,
            RectTransform pageContainer,
            Sprite circuitSprite,
            Sprite coinSprite,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform existing = FindDirectChild(safeRoot, HeaderName);
            HubHeaderUI existingUi = existing != null ? existing.GetComponent<HubHeaderUI>() : null;
            bool structureOk = IsHeaderStructureConforme(existing);

            if (structureOk)
            {
                conforme++;
                log.AppendLine($"- Header déjà conforme (`{GetPath(existing)}`) ✓");
                return existingUi;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(existing != null
                    ? $"- [DRY] Mettre à jour Header (`{GetPath(existing)}`) — À FAIRE"
                    : $"- [DRY] CRÉER `{HeaderName}` sous SafeRoot (H={UiTheme.HeaderHeight}) — À FAIRE");
                log.AppendLine(
                    "- [DRY] Enfants : HeaderBackdrop, CircuitBackdrop, BottomHairline, " +
                    "PillIdentity, PillStage, PillTals");
                log.AppendLine("- [DRY] RectMask2D + HubHeaderUI + hairline BorderStrong");
                return existingUi;
            }

            GameObject headerGo;
            RectTransform headerRt;
            bool created = false;

            if (existing != null)
            {
                headerGo = existing.gameObject;
                headerRt = existing as RectTransform;
                log.AppendLine($"- Header existant : `{GetPath(existing)}` — mise à jour");
            }
            else
            {
                headerGo = new GameObject(HeaderName, typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(headerGo, UndoLabel);
                Undo.SetTransformParent(headerGo.transform, safeRoot, false, UndoLabel);
                headerRt = (RectTransform)headerGo.transform;
                created = true;
                log.AppendLine($"- CRÉER `{HeaderName}` sous SafeRoot ✓");
            }

            if (headerRt == null)
            {
                failed++;
                log.AppendLine("- ✗ Header sans RectTransform");
                Debug.LogError("[HubHeaderBuilder] Header sans RectTransform.");
                return null;
            }

            if (headerGo.GetComponent<RectMask2D>() == null)
                Undo.AddComponent<RectMask2D>(headerGo);

            Undo.RecordObject(headerRt, UndoLabel);
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0f, UiTheme.HeaderHeight);

            // Backdrop plein cadre
            Image backdrop = EnsureChildImage(headerRt, "HeaderBackdrop");
            ConfigureStretchImage(backdrop, UiTheme.BgPanel, raycast: false);

            // Circuit stretch plein header (visuel jusqu'à l'encoche)
            Image circuit = EnsureChildImage(headerRt, "CircuitBackdrop");
            Undo.RecordObject(circuit, UndoLabel);
            circuit.sprite = circuitSprite;
            circuit.color = CircuitTint;
            circuit.raycastTarget = false;
            circuit.type = Image.Type.Simple;
            circuit.preserveAspect = false;
            ConfigureStretchImage(circuit, CircuitTint, raycast: false);
            circuit.sprite = circuitSprite;
            circuit.color = CircuitTint;

            // Hairline bas
            Image hairline = EnsureChildImage(headerRt, "BottomHairline");
            Undo.RecordObject(hairline, UndoLabel);
            hairline.sprite = null;
            hairline.color = UiTheme.BorderStrong;
            hairline.raycastTarget = false;
            RectTransform hairRt = hairline.rectTransform;
            Undo.RecordObject(hairRt, UndoLabel);
            hairRt.anchorMin = new Vector2(0f, 0f);
            hairRt.anchorMax = new Vector2(1f, 0f);
            hairRt.pivot = new Vector2(0.5f, 0f);
            hairRt.anchoredPosition = Vector2.zero;
            hairRt.sizeDelta = new Vector2(0f, UiTheme.BorderThin);

            TextMeshProUGUI nameTmp = EnsurePill(
                headerRt, "PillIdentity", PillSide.Left,
                spriteS, spriteM, spriteL, out _, includeCoin: false, coinSprite: null);

            TextMeshProUGUI stageTmp = EnsurePill(
                headerRt, "PillStage", PillSide.Center,
                spriteS, spriteM, spriteL, out _, includeCoin: false, coinSprite: null);

            TextMeshProUGUI talsTmp = EnsurePill(
                headerRt, "PillTals", PillSide.Right,
                spriteS, spriteM, spriteL, out _, includeCoin: true, coinSprite: coinSprite);

            SetChildOrder(headerRt, RequiredHeaderChildren);

            HubHeaderUI ui = headerGo.GetComponent<HubHeaderUI>();
            if (ui == null)
                ui = Undo.AddComponent<HubHeaderUI>(headerGo);

            SerializedObject so = new SerializedObject(ui);
            UiGen.Wire(so, "playerNameText", nameTmp);
            UiGen.Wire(so, "bestStageText", stageTmp);
            UiGen.Wire(so, "talsText", talsTmp);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(ui);

            if (nameTmp != null && stageTmp != null && talsTmp != null
                && IsHeaderStructureConforme(headerRt))
            {
                conforme++;
                log.AppendLine(created
                    ? "- Header + HubHeaderUI câblé ✓ → conforme"
                    : "- Header mis à jour + HubHeaderUI câblé ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Header câblage — ÉCHEC ✗");
                Debug.LogError("[HubHeaderBuilder] Échec câblage HubHeaderUI.");
            }

            if (pageContainer != null && headerRt.parent == safeRoot)
            {
                int targetIndex = pageContainer.GetSiblingIndex() + 1;
                Undo.RecordObject(headerRt, UndoLabel);
                headerRt.SetSiblingIndex(targetIndex);
                log.AppendLine($"- Sibling Header → index {headerRt.GetSiblingIndex()} (après PageContainer)");
            }

            return ui;
        }

        // ═══════════════════════════════════════════
        // HUB HEADER SAFE BLEED
        // ═══════════════════════════════════════════

        private static void EnsureSafeRootReachesPhysicalTop(
            RectTransform safeRoot,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (safeRoot == null)
            {
                failed++;
                log.AppendLine("- SafeRoot introuvable — ÉCHEC ✗");
                return;
            }

            SafeAreaFitter fitter = safeRoot.GetComponent<SafeAreaFitter>();
            if (fitter == null)
            {
                failed++;
                log.AppendLine("- SafeAreaFitter absent — ÉCHEC ✗");
                return;
            }

            if (!fitter.ConformTop)
            {
                conforme++;
                log.AppendLine("- SafeAreaFitter.conformTop = false ✓ (haut physique)");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] SET conformTop = false — À FAIRE");
                return;
            }

            Undo.RecordObject(fitter, UndoLabel);
            fitter.ConformTop = false;
            EditorUtility.SetDirty(fitter);

            if (!fitter.ConformTop)
            {
                conforme++;
                log.AppendLine("- SafeAreaFitter.conformTop = false ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- conformTop toujours true — ÉCHEC ✗");
            }
        }

        private static void EnsureHubHeaderSafeBleed(
            RectTransform safeRoot,
            HubHeaderUI headerUi,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform headerTx = headerUi != null
                ? headerUi.transform
                : FindDirectChild(safeRoot, HeaderName);

            if (headerTx == null)
            {
                failed++;
                log.AppendLine("- Header absent — HubHeaderSafeBleed impossible ✗");
                return;
            }

            GameObject headerGo = headerTx.gameObject;
            HubHeaderSafeBleed bleed = headerGo.GetComponent<HubHeaderSafeBleed>();
            RectTransform pillIdentity = headerTx.Find("PillIdentity") as RectTransform;
            RectTransform pillStage = headerTx.Find("PillStage") as RectTransform;
            RectTransform pillTals = headerTx.Find("PillTals") as RectTransform;

            bool contentReady = pillIdentity != null && pillStage != null && pillTals != null;
            bool wiredOk = bleed != null && IsSafeBleedWired(bleed, pillIdentity, pillStage, pillTals);

            if (wiredOk)
            {
                conforme++;
                log.AppendLine("- HubHeaderSafeBleed déjà câblé (3 pills) ✓");
                if (apply)
                    bleed.Refresh();
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(bleed == null
                    ? "- [DRY] AJOUTER HubHeaderSafeBleed + câbler 3 pills — À FAIRE"
                    : "- [DRY] Recâbler HubHeaderSafeBleed.safeBandContent (3 pills) — À FAIRE");
                return;
            }

            if (!contentReady)
            {
                failed++;
                log.AppendLine("- HubHeaderSafeBleed — pills manquantes ✗");
                Debug.LogError("[HubHeaderBuilder] Impossible de câbler SafeBleed sans pills.");
                return;
            }

            if (bleed == null)
                bleed = Undo.AddComponent<HubHeaderSafeBleed>(headerGo);

            SerializedObject so = new SerializedObject(bleed);
            SerializedProperty prop = so.FindProperty("safeBandContent");
            if (prop == null)
            {
                failed++;
                log.AppendLine("- HubHeaderSafeBleed.safeBandContent introuvable ✗");
                Debug.LogError("[HubHeaderBuilder] Champ safeBandContent manquant.");
                return;
            }

            prop.arraySize = 3;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = pillIdentity;
            prop.GetArrayElementAtIndex(1).objectReferenceValue = pillStage;
            prop.GetArrayElementAtIndex(2).objectReferenceValue = pillTals;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bleed);

            bleed.Refresh();

            if (IsSafeBleedWired(bleed, pillIdentity, pillStage, pillTals))
            {
                conforme++;
                log.AppendLine("- HubHeaderSafeBleed câblé (3 pills) ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- HubHeaderSafeBleed câblage — ÉCHEC ✗");
                Debug.LogError("[HubHeaderBuilder] Échec câblage HubHeaderSafeBleed.");
            }
        }

        private static bool IsSafeBleedWired(
            HubHeaderSafeBleed bleed,
            RectTransform pillIdentity,
            RectTransform pillStage,
            RectTransform pillTals)
        {
            if (bleed == null)
                return false;

            SerializedObject so = new SerializedObject(bleed);
            SerializedProperty prop = so.FindProperty("safeBandContent");
            if (prop == null || !prop.isArray || prop.arraySize < 3)
                return false;

            return prop.GetArrayElementAtIndex(0).objectReferenceValue == pillIdentity
                   && prop.GetArrayElementAtIndex(1).objectReferenceValue == pillStage
                   && prop.GetArrayElementAtIndex(2).objectReferenceValue == pillTals;
        }

        private static bool IsHeaderStructureConforme(Transform headerTx)
        {
            if (headerTx == null)
                return false;
            if (headerTx.GetComponent<HubHeaderUI>() == null)
                return false;
            if (headerTx.GetComponent<RectMask2D>() == null)
                return false;

            RectTransform headerRt = headerTx as RectTransform;
            if (headerRt == null || headerRt.sizeDelta.y < UiTheme.HeaderHeight - 0.5f)
                return false;

            for (int i = 0; i < RequiredHeaderChildren.Length; i++)
            {
                if (headerTx.Find(RequiredHeaderChildren[i]) == null)
                    return false;
            }

            Image hair = headerTx.Find("BottomHairline")?.GetComponent<Image>();
            if (hair == null)
                return false;
            if (!ColorsApproximately(hair.color, UiTheme.BorderStrong))
                return false;

            return true;
        }

        private static bool ColorsApproximately(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.02f
                   && Mathf.Abs(a.g - b.g) < 0.02f
                   && Mathf.Abs(a.b - b.b) < 0.02f
                   && Mathf.Abs(a.a - b.a) < 0.02f;
        }

        private enum PillSide { Left, Center, Right }

        private static TextMeshProUGUI EnsurePill(
            RectTransform headerRt,
            string pillName,
            PillSide side,
            Sprite spriteS,
            Sprite spriteM,
            Sprite spriteL,
            out RectTransform pillRt,
            bool includeCoin,
            Sprite coinSprite)
        {
            Transform existing = headerRt.Find(pillName);
            GameObject pillGo;
            if (existing != null)
            {
                pillGo = existing.gameObject;
                pillRt = existing as RectTransform;
            }
            else
            {
                pillGo = new GameObject(
                    pillName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                Undo.RegisterCreatedObjectUndo(pillGo, UndoLabel);
                Undo.SetTransformParent(pillGo.transform, headerRt, false, UndoLabel);
                pillRt = (RectTransform)pillGo.transform;
            }

            Undo.RecordObject(pillRt, UndoLabel);
            float margin = UiTheme.Space4;
            switch (side)
            {
                case PillSide.Left:
                    pillRt.anchorMin = new Vector2(0f, 0.5f);
                    pillRt.anchorMax = new Vector2(0f, 0.5f);
                    pillRt.pivot = new Vector2(0f, 0.5f);
                    pillRt.anchoredPosition = new Vector2(margin, 0f);
                    break;
                case PillSide.Right:
                    pillRt.anchorMin = new Vector2(1f, 0.5f);
                    pillRt.anchorMax = new Vector2(1f, 0.5f);
                    pillRt.pivot = new Vector2(1f, 0.5f);
                    pillRt.anchoredPosition = new Vector2(-margin, 0f);
                    break;
                default:
                    pillRt.anchorMin = new Vector2(0.5f, 0.5f);
                    pillRt.anchorMax = new Vector2(0.5f, 0.5f);
                    pillRt.pivot = new Vector2(0.5f, 0.5f);
                    pillRt.anchoredPosition = Vector2.zero;
                    break;
            }

            pillRt.sizeDelta = new Vector2(pillRt.sizeDelta.x, PillHeight);

            LayoutElement rootLe = pillGo.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(pillGo);
            Undo.RecordObject(rootLe, UndoLabel);
            rootLe.minHeight = PillHeight;
            rootLe.preferredHeight = PillHeight;
            rootLe.flexibleWidth = 0f;

            ContentSizeFitter csf = pillGo.GetComponent<ContentSizeFitter>() ?? Undo.AddComponent<ContentSizeFitter>(pillGo);
            Undo.RecordObject(csf, UndoLabel);
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            HorizontalLayoutGroup hlg = pillGo.GetComponent<HorizontalLayoutGroup>()
                                       ?? Undo.AddComponent<HorizontalLayoutGroup>(pillGo);
            Undo.RecordObject(hlg, UndoLabel);
            int pad = Mathf.RoundToInt(UiTheme.Space3);
            hlg.padding = new RectOffset(pad, pad, pad, pad);
            hlg.spacing = UiTheme.Space2;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            PanelSurface surface = pillGo.GetComponent<PanelSurface>() ?? Undo.AddComponent<PanelSurface>(pillGo);
            SerializedObject surfaceSo = new SerializedObject(surface);
            surfaceSo.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Pill;
            surfaceSo.FindProperty("borderStyle").enumValueIndex = (int)PanelSurface.SurfaceBorder.Subtle;
            surfaceSo.FindProperty("roundedSpriteS").objectReferenceValue = spriteS;
            surfaceSo.FindProperty("roundedSpriteM").objectReferenceValue = spriteM;
            surfaceSo.FindProperty("roundedSpriteL").objectReferenceValue = spriteL;
            surfaceSo.FindProperty("blocksRaycasts").boolValue = false;
            surfaceSo.ApplyModifiedPropertiesWithoutUndo();
            surface.ApplyStyle();
            IgnoreLayoutOnFill(pillGo.transform);

            if (includeCoin && coinSprite != null)
                EnsureCoinIcon(pillGo.transform, coinSprite);

            string labelName = includeCoin ? "TxtTals" : (pillName == "PillStage" ? "TxtBestStage" : "TxtPlayerName");
            TextMeshProUGUI tmp = EnsureLabelTmp(pillGo.transform, labelName);

            if (includeCoin)
                tmp.text = "0";
            else if (pillName == "PillStage")
                tmp.text = "Étage 0";
            else
                tmp.text = "Voyageur";

            LayoutRebuilder.ForceRebuildLayoutImmediate(pillRt);
            return tmp;
        }

        private static void IgnoreLayoutOnFill(Transform pillTx)
        {
            Transform fill = pillTx.Find("Fill");
            if (fill == null) return;

            LayoutElement le = fill.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(fill.gameObject);
            Undo.RecordObject(le, UndoLabel);
            le.ignoreLayout = true;

            RectTransform fillRt = fill as RectTransform;
            if (fillRt == null) return;
            Undo.RecordObject(fillRt, UndoLabel);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            float inset = UiTheme.BorderThin;
            fillRt.offsetMin = new Vector2(inset, inset);
            fillRt.offsetMax = new Vector2(-inset, -inset);
            fillRt.SetAsFirstSibling();
        }

        private static void EnsureCoinIcon(Transform pillTx, Sprite coinSprite)
        {
            const string iconName = "TalsIcon";
            Transform existing = pillTx.Find(iconName);
            GameObject iconGo;
            if (existing != null)
            {
                iconGo = existing.gameObject;
            }
            else
            {
                iconGo = new GameObject(iconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(iconGo, UndoLabel);
                Undo.SetTransformParent(iconGo.transform, pillTx, false, UndoLabel);
            }

            Image img = iconGo.GetComponent<Image>();
            Undo.RecordObject(img, UndoLabel);
            img.sprite = coinSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;

            LayoutElement le = iconGo.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(iconGo);
            Undo.RecordObject(le, UndoLabel);
            le.minWidth = TalsIconSize;
            le.minHeight = TalsIconSize;
            le.preferredWidth = TalsIconSize;
            le.preferredHeight = TalsIconSize;
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            int insert = 1;
            if (pillTx.Find("Fill") != null)
                insert = 1;
            iconGo.transform.SetSiblingIndex(insert);
        }

        private static TextMeshProUGUI EnsureLabelTmp(Transform pillTx, string labelName)
        {
            Transform existing = pillTx.Find(labelName);
            GameObject labelGo;
            if (existing != null)
            {
                labelGo = existing.gameObject;
            }
            else
            {
                labelGo = new GameObject(labelName, typeof(RectTransform), typeof(TextMeshProUGUI));
                Undo.RegisterCreatedObjectUndo(labelGo, UndoLabel);
                Undo.SetTransformParent(labelGo.transform, pillTx, false, UndoLabel);
            }

            TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(tmp, UndoLabel);
            tmp.fontSize = UiTypography.Label;
            tmp.color = UiTheme.TextPrimary;
            if (labelName == "TxtBestStage")
                tmp.alignment = TextAlignmentOptions.Midline;
            else
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;

            LayoutElement le = labelGo.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(labelGo);
            Undo.RecordObject(le, UndoLabel);
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            labelGo.transform.SetAsLastSibling();
            return tmp;
        }

        // ═══════════════════════════════════════════
        // PAGE CONTAINER / INFOBAR / ORDER
        // ═══════════════════════════════════════════

        /// <summary>
        /// Pages full-bleed sous SafeRoot : le visu Accueil passe derrière le Header
        /// (plus de dédoublement toit de train). Header reste sibling au-dessus.
        /// </summary>
        private static void ApplyPageContainerFullBleed(
            RectTransform pageContainer,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            if (pageContainer == null)
            {
                log.AppendLine("- PageContainer absent — skip full-bleed");
                return;
            }

            const float targetOffsetMaxY = 0f;
            bool already = Mathf.Approximately(pageContainer.offsetMax.y, targetOffsetMaxY)
                           && Mathf.Approximately(pageContainer.offsetMin.y, 0f);

            if (already)
            {
                conforme++;
                log.AppendLine("- PageContainer déjà full-bleed (offsetMax.y=0) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] offsetMax.y : {pageContainer.offsetMax.y} → {targetOffsetMaxY} — À FAIRE");
                return;
            }

            Undo.RecordObject(pageContainer, UndoLabel);
            Vector2 max = pageContainer.offsetMax;
            max.y = targetOffsetMaxY;
            pageContainer.offsetMax = max;
            EditorUtility.SetDirty(pageContainer);

            if (Mathf.Approximately(pageContainer.offsetMax.y, targetOffsetMaxY))
            {
                conforme++;
                log.AppendLine($"- PageContainer.offsetMax.y → {targetOffsetMaxY} ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- PageContainer full-bleed — ÉCHEC ✗");
                Debug.LogError("[HubHeaderBuilder] Échec full-bleed PageContainer.");
            }
        }

        private static void FixNavigationBar(
            RectTransform safeRoot,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            RectTransform nav = FindDirectChild(safeRoot, "NavigationBar");
            if (nav == null)
            {
                log.AppendLine("- NavigationBar introuvable — skip (gate 2.2)");
                return;
            }

            // Gate 2.2 : HubNavBarUI / HubNavSafeBleed possèdent la hauteur (bleed bas).
            if (nav.GetComponent<HubNavBarUI>() != null
                || nav.GetComponent<HubNavSafeBleed>() != null)
            {
                conforme++;
                log.AppendLine(
                    "- NavigationBar gérée par Gate 2.2 (HubNavBarUI) — skip micro-fix ✓");
                return;
            }

            bool okLayout =
                Mathf.Approximately(nav.pivot.x, 0.5f)
                && Mathf.Approximately(nav.pivot.y, 0f)
                && Mathf.Approximately(nav.anchorMin.y, 0f)
                && Mathf.Approximately(nav.anchorMax.y, 0f)
                && Mathf.Approximately(nav.sizeDelta.y, UiTheme.NavHeight)
                && Mathf.Approximately(nav.anchoredPosition.y, 0f);

            if (okLayout)
            {
                conforme++;
                log.AppendLine($"- NavigationBar déjà conforme (H={UiTheme.NavHeight}, pivot bas) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine(
                    $"- [DRY] NavigationBar : pivot={nav.pivot}, H={nav.sizeDelta.y} " +
                    $"→ pivot (0.5,0), H={UiTheme.NavHeight} — À FAIRE");
                return;
            }

            Undo.RecordObject(nav, UndoLabel);
            nav.anchorMin = new Vector2(0f, 0f);
            nav.anchorMax = new Vector2(1f, 0f);
            nav.pivot = new Vector2(0.5f, 0f);
            nav.anchoredPosition = Vector2.zero;
            nav.sizeDelta = new Vector2(0f, UiTheme.NavHeight);
            EditorUtility.SetDirty(nav);

            if (Mathf.Approximately(nav.sizeDelta.y, UiTheme.NavHeight)
                && Mathf.Approximately(nav.pivot.y, 0f))
            {
                conforme++;
                log.AppendLine($"- NavigationBar → H={UiTheme.NavHeight}, pivot (0.5,0) ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- NavigationBar micro-fix — ÉCHEC ✗");
                Debug.LogError("[HubHeaderBuilder] Échec fix NavigationBar.");
            }
        }

        private static void TryRemoveInfoBar(
            RectTransform safeRoot,
            HubHeaderUI headerUi,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform infoBarTx = FindDirectChild(safeRoot, InfoBarName);
            if (infoBarTx == null)
            {
                Transform[] all = safeRoot.root.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].name == InfoBarName)
                    {
                        infoBarTx = all[i];
                        break;
                    }
                }
            }

            if (infoBarTx == null)
            {
                conforme++;
                log.AppendLine("- InfoBar : déjà absent — conforme ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine($"- [DRY] SUPPRIMER `{GetPath(infoBarTx)}` — À FAIRE");
                return;
            }

            if (headerUi == null)
            {
                failed++;
                log.AppendLine("- InfoBar NON supprimé — Header absent (précondition) ✗");
                Debug.LogError("[HubHeaderBuilder] Refuse de supprimer InfoBar sans Header valide.");
                return;
            }

            string path = GetPath(infoBarTx);
            Undo.DestroyObjectImmediate(infoBarTx.gameObject);

            bool gone = FindDirectChild(safeRoot, InfoBarName) == null;
            if (gone)
            {
                conforme++;
                log.AppendLine($"- SUPPRIMER `{path}` ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- SUPPRIMER InfoBar — ÉCHEC ✗");
                Debug.LogError("[HubHeaderBuilder] InfoBar toujours présent après destroy.");
            }
        }

        private static void ForceSiblingOrder(
            RectTransform safeRoot,
            RectTransform pageContainer,
            HubHeaderUI headerUi,
            bool apply,
            StringBuilder log,
            ref int todo,
            ref int conforme,
            ref int failed)
        {
            Transform nav = FindDirectChild(safeRoot, "NavigationBar");
            Transform headerTx = headerUi != null
                ? headerUi.transform
                : FindDirectChild(safeRoot, HeaderName);

            bool orderOk = IsSafeRootOrderConforme(safeRoot, pageContainer, headerTx, nav);
            if (orderOk)
            {
                conforme++;
                log.AppendLine("- Ordre siblings déjà conforme (PageContainer → Header → NavigationBar) ✓");
                return;
            }

            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] sibling[0] = `PageContainer` — À FAIRE");
                log.AppendLine("- [DRY] sibling[1] = `Header`");
                log.AppendLine("- [DRY] sibling[2] = `NavigationBar`");
                return;
            }

            int index = 0;
            bool ok = true;
            if (pageContainer != null && pageContainer.parent == safeRoot)
            {
                Undo.RecordObject(pageContainer, UndoLabel);
                pageContainer.SetSiblingIndex(index);
                log.AppendLine($"- sibling[{index}] = `{pageContainer.name}`");
                index++;
            }

            if (headerTx != null && headerTx.parent == safeRoot)
            {
                Undo.RecordObject(headerTx, UndoLabel);
                headerTx.SetSiblingIndex(index);
                log.AppendLine($"- sibling[{index}] = `{headerTx.name}`");
                index++;
            }
            else
            {
                ok = false;
            }

            if (nav != null && nav.parent == safeRoot)
            {
                Undo.RecordObject(nav, UndoLabel);
                nav.SetSiblingIndex(index);
                log.AppendLine($"- sibling[{index}] = `{nav.name}`");
            }

            if (ok && IsSafeRootOrderConforme(safeRoot, pageContainer, headerTx, nav))
            {
                conforme++;
                log.AppendLine("- Ordre siblings appliqué ✓ → conforme");
            }
            else
            {
                failed++;
                log.AppendLine("- Ordre siblings — ÉCHEC ✗");
            }
        }

        private static bool IsSafeRootOrderConforme(
            RectTransform safeRoot,
            RectTransform pageContainer,
            Transform headerTx,
            Transform nav)
        {
            if (safeRoot == null || pageContainer == null || headerTx == null)
                return false;
            if (pageContainer.parent != safeRoot || headerTx.parent != safeRoot)
                return false;
            if (pageContainer.GetSiblingIndex() != 0)
                return false;
            if (headerTx.GetSiblingIndex() != 1)
                return false;
            if (nav != null && nav.parent == safeRoot && nav.GetSiblingIndex() != 2)
                return false;
            return true;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static RectTransform FindSafeRoot(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < all.Length; j++)
                {
                    if (all[j].name == SafeRootName)
                        return all[j] as RectTransform;
                }
            }

            return null;
        }

        private static RectTransform FindDirectChild(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name == name)
                    return c as RectTransform;
            }

            return null;
        }

        private static Sprite LoadCircuitSprite()
        {
            string path = AssetDatabase.GUIDToAssetPath(CircuitGuid);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Image EnsureChildImage(RectTransform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                if (go.GetComponent<Image>() == null)
                    Undo.AddComponent<Image>(go);
            }
            else
            {
                go = new GameObject(childName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                Undo.SetTransformParent(go.transform, parent, false, UndoLabel);
            }

            return go.GetComponent<Image>();
        }

        private static void ConfigureStretchImage(Image img, Color color, bool raycast)
        {
            Undo.RecordObject(img, UndoLabel);
            img.sprite = null;
            img.color = color;
            img.raycastTarget = raycast;
            RectTransform rt = img.rectTransform;
            Undo.RecordObject(rt, UndoLabel);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetChildOrder(RectTransform parent, string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Transform child = parent.Find(names[i]);
                if (child == null) continue;
                Undo.RecordObject(child, UndoLabel);
                child.SetSiblingIndex(i);
            }
        }

        private static string GetPath(Transform t)
        {
            if (t == null) return "(null)";
            var sb = new StringBuilder(t.name);
            Transform p = t.parent;
            while (p != null)
            {
                sb.Insert(0, p.name + "/");
                p = p.parent;
            }

            return sb.ToString();
        }
    }
}
#endif
