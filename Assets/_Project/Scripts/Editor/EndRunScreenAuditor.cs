#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Audit et corrections sûres du montage de l'écran de fin de run (DefeatUI).
    /// Lit les références sérialisées — jamais de Find par nom.
    /// </summary>
    public static class EndRunScreenAuditor
    {
        // ═══════════════════════════════════════════
        // CONSTANTES — source de vérité layout (polish)
        // ═══════════════════════════════════════════
        private const int SPACING = 24;
        private const int PADDING_LATERAL = 56;
        private const float ENTRY_MIN_HEIGHT = 300f;
        private const float BOTTOM_ANCHOR_TOLERANCE = 0.01f;

        private static readonly string[] EntryUiFields =
        {
            "iconImage",
            "rarityBorder",
            "rankText",
            "quoteText",
            "damageDealtText",
            "damageTakenText",
            "healingText"
        };

        private sealed class AuditReport
        {
            public int ErrorCount;
            public int WarningCount;
            public int RepairableErrorCount;
            public readonly List<string> AppliedFixes = new List<string>();
            private readonly StringBuilder _lines = new StringBuilder(2048);

            public void Ok(string message) => _lines.AppendLine($"✓ {message}");

            public void Error(string message, string expected, string found, bool repairable = false)
            {
                ErrorCount++;
                if (repairable)
                    RepairableErrorCount++;
                _lines.AppendLine($"✗ [ERREUR] {message} → attendu {expected}, trouvé {found}");
            }

            public void ErrorSimple(string message, bool repairable = false)
            {
                ErrorCount++;
                if (repairable)
                    RepairableErrorCount++;
                _lines.AppendLine($"✗ [ERREUR] {message}");
            }

            public void ErrorManual(string message, string manualAction)
            {
                ErrorCount++;
                _lines.AppendLine($"✗ [ERREUR] {message} → action manuelle : {manualAction}");
            }

            public void Warn(string message, string expected, string found, string manualAction = null)
            {
                WarningCount++;
                string suffix = string.IsNullOrEmpty(manualAction)
                    ? string.Empty
                    : $" → action manuelle : {manualAction}";
                _lines.AppendLine($"✗ [AVERT.] {message} → attendu {expected}, trouvé {found}{suffix}");
            }

            public void Info(string message) => _lines.AppendLine($"ℹ {message}");

            public void LogVerdict()
            {
                if (ErrorCount == 0)
                {
                    Debug.Log(_lines + "[EndRunAudit] PRÊT POUR VALIDATION");
                    return;
                }

                Debug.Log(_lines +
                    $"[EndRunAudit] {ErrorCount} erreurs ({RepairableErrorCount} réparables via menu Corriger), {WarningCount} avert.");
            }
        }

        private struct DefeatUiRefs
        {
            public DefeatUI Component;
            public GameObject PanelRoot;
            public TextMeshProUGUI TitleText;
            public TextMeshProUGUI StageReachedText;
            public TextMeshProUGUI TalsEarnedText;
            public TextMeshProUGUI BossesDefeatedText;
            public TextMeshProUGUI BonusCountText;
            public TMP_Text SuperHitsText;
            public Button RetryButton;
            public Button ButtonReturnHub;
            public Transform RankingContainer;
            public EndRunCharacterEntryUI EntryPrefab;
            public CharacterDatabase CharacterDatabase;
        }

        [MenuItem("Chez Arthur/UI/Écran fin de run — Audit montage")]
        public static void AuditMontage()
        {
            RunAudit(applySafeFixes: false);
        }

        [MenuItem("Chez Arthur/UI/Écran fin de run — Corriger écarts sûrs")]
        public static void CorrectSafeGaps()
        {
            AuditReport fixReport = RunAudit(applySafeFixes: true);
            if (fixReport.AppliedFixes.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("[EndRunAudit] Corrections appliquées :");
                for (int i = 0; i < fixReport.AppliedFixes.Count; i++)
                    sb.AppendLine($"  • {fixReport.AppliedFixes[i]}");
                Debug.Log(sb.ToString());
            }
            else
            {
                Debug.Log("[EndRunAudit] Aucune correction sûre à appliquer.");
            }

            RunAudit(applySafeFixes: false);
        }

        private static AuditReport RunAudit(bool applySafeFixes)
        {
            var report = new AuditReport();

            DefeatUI defeatUi = Object.FindObjectOfType<DefeatUI>(true);
            if (defeatUi == null)
            {
                Debug.LogError("[EndRunAudit] Ouvrir la scène de combat.");
                return report;
            }

            DefeatUiRefs refs = ReadDefeatUiRefs(defeatUi);
            AuditWiring(report, refs);
            AuditScrollChain(report, refs, applySafeFixes);
            AuditPrefabEntry(report, refs, applySafeFixes);

            report.LogVerdict();
            return report;
        }

        private static DefeatUiRefs ReadDefeatUiRefs(DefeatUI defeatUi)
        {
            SerializedObject so = new SerializedObject(defeatUi);
            return new DefeatUiRefs
            {
                Component = defeatUi,
                PanelRoot = so.FindProperty("panelRoot")?.objectReferenceValue as GameObject,
                TitleText = so.FindProperty("titleText")?.objectReferenceValue as TextMeshProUGUI,
                StageReachedText = so.FindProperty("stageReachedText")?.objectReferenceValue as TextMeshProUGUI,
                TalsEarnedText = so.FindProperty("talsEarnedText")?.objectReferenceValue as TextMeshProUGUI,
                BossesDefeatedText = so.FindProperty("bossesDefeatedText")?.objectReferenceValue as TextMeshProUGUI,
                BonusCountText = so.FindProperty("bonusCountText")?.objectReferenceValue as TextMeshProUGUI,
                SuperHitsText = so.FindProperty("_superHitsText")?.objectReferenceValue as TMP_Text,
                RetryButton = so.FindProperty("retryButton")?.objectReferenceValue as Button,
                ButtonReturnHub = so.FindProperty("buttonReturnHub")?.objectReferenceValue as Button,
                RankingContainer = so.FindProperty("rankingContainer")?.objectReferenceValue as Transform,
                EntryPrefab = so.FindProperty("entryPrefab")?.objectReferenceValue as EndRunCharacterEntryUI,
                CharacterDatabase = so.FindProperty("characterDatabase")?.objectReferenceValue as CharacterDatabase
            };
        }

        // ═══════════════════════════════════════════
        // A — Wiring DefeatUI
        // ═══════════════════════════════════════════
        private static void AuditWiring(AuditReport report, DefeatUiRefs refs)
        {
            CheckRef(report, "panelRoot", refs.PanelRoot);
            CheckRef(report, "titleText", refs.TitleText);
            CheckRef(report, "stageReachedText", refs.StageReachedText);
            CheckRef(report, "talsEarnedText", refs.TalsEarnedText);
            CheckRef(report, "bossesDefeatedText", refs.BossesDefeatedText);
            if (refs.BonusCountText == null)
                report.Info("bonusCountText null (optionnel, masqué)");
            else if (!refs.BonusCountText.gameObject.activeSelf)
                report.Info("bonusCountText désactivé (hors écran)");
            else
                report.Info("bonusCountText actif — désactiver ou retirer du Content");
            if (refs.SuperHitsText == null)
                report.Info("_superHitsText null (optionnel, ignoré)");
            else
                report.Info("_superHitsText wiré");
            CheckRef(report, "retryButton", refs.RetryButton);
            CheckRef(report, "buttonReturnHub", refs.ButtonReturnHub);
            CheckRef(report, "rankingContainer", refs.RankingContainer);
            CheckRef(report, "characterDatabase", refs.CharacterDatabase);

            if (refs.EntryPrefab == null)
            {
                report.ErrorSimple("entryPrefab non assigné sur DefeatUI");
            }
            else if (!PrefabUtility.IsPartOfPrefabAsset(refs.EntryPrefab.gameObject))
            {
                report.ErrorSimple(
                    "entryPrefab doit être un asset prefab, pas une instance de scène");
            }
            else
            {
                report.Ok("entryPrefab est un asset prefab");
            }
        }

        private static void CheckRef(AuditReport report, string name, Object value)
        {
            if (value == null)
                report.ErrorSimple($"{name} non assigné sur DefeatUI");
            else
                report.Ok($"{name} assigné");
        }

        // ═══════════════════════════════════════════
        // B–G — Chaîne scroll + content + boutons + rankingContainer
        // ═══════════════════════════════════════════
        private static void AuditScrollChain(AuditReport report, DefeatUiRefs refs, bool applySafeFixes)
        {
            if (refs.RankingContainer == null)
                return;

            ScrollRect scrollRect = FindAncestorScrollRect(refs.RankingContainer);
            if (scrollRect == null)
            {
                report.ErrorSimple(
                    "ScrollRect introuvable en remontant depuis rankingContainer");
                return;
            }

            report.Ok($"ScrollRect trouvé sur '{scrollRect.gameObject.name}'");

            if (scrollRect.viewport == null)
            {
                report.ErrorSimple("ScrollRect.viewport non assigné");
            }
            else
            {
                report.Ok("ScrollRect.viewport assigné");

                if (scrollRect.viewport.GetComponent<RectMask2D>() == null)
                {
                    if (applySafeFixes)
                    {
                        Undo.RecordObject(scrollRect.viewport.gameObject, "Ajouter RectMask2D viewport");
                        scrollRect.viewport.gameObject.AddComponent<RectMask2D>();
                        MarkSceneDirty();
                        report.AppliedFixes.Add($"RectMask2D ajouté sur viewport '{scrollRect.viewport.name}'");
                        report.Ok("viewport RectMask2D ajouté");
                    }
                    else
                    {
                        report.Error("viewport sans RectMask2D", "RectMask2D présent", "absent", repairable: true);
                    }
                }
                else
                {
                    report.Ok("viewport porte un RectMask2D");
                }
            }

            Transform content = scrollRect.content;
            if (content == null)
            {
                report.ErrorSimple("ScrollRect.content non assigné");
                return;
            }

            report.Ok("ScrollRect.content assigné");

            if (!IsDescendantOf(refs.RankingContainer, content))
            {
                report.ErrorSimple("rankingContainer n'est pas descendant de ScrollRect.content");
            }
            else
            {
                report.Ok("rankingContainer descendant de content");
            }

            AuditScrollRectSettings(report, scrollRect, applySafeFixes);
            AuditContentLayout(report, content, applySafeFixes);
            AuditContentSiblingOrder(report, content, refs);
            AuditButtonsOutsideScroll(report, refs, content);
            AuditRankingContainerComponents(report, refs.RankingContainer, applySafeFixes);
        }

        private static void AuditScrollRectSettings(AuditReport report, ScrollRect scrollRect, bool applySafeFixes)
        {
            bool changed = false;
            Undo.RecordObject(scrollRect, "Corriger ScrollRect fin de run");

            if (scrollRect.horizontal)
            {
                if (applySafeFixes)
                {
                    scrollRect.horizontal = false;
                    changed = true;
                    report.AppliedFixes.Add("ScrollRect.horizontal → false");
                }
                else
                {
                    report.Error("ScrollRect.horizontal", "false", "true", repairable: true);
                }
            }
            else
            {
                report.Ok("ScrollRect.horizontal == false");
            }

            if (!scrollRect.vertical)
            {
                if (applySafeFixes)
                {
                    scrollRect.vertical = true;
                    changed = true;
                    report.AppliedFixes.Add("ScrollRect.vertical → true");
                }
                else
                {
                    report.Error("ScrollRect.vertical", "true", "false", repairable: true);
                }
            }
            else
            {
                report.Ok("ScrollRect.vertical == true");
            }

            if (scrollRect.movementType != ScrollRect.MovementType.Clamped)
            {
                if (applySafeFixes)
                {
                    scrollRect.movementType = ScrollRect.MovementType.Clamped;
                    changed = true;
                    report.AppliedFixes.Add("ScrollRect.movementType → Clamped");
                }
                else
                {
                    report.Error(
                        "ScrollRect.movementType",
                        "Clamped",
                        scrollRect.movementType.ToString(),
                        repairable: true);
                }
            }
            else
            {
                report.Ok("ScrollRect.movementType == Clamped");
            }

            if (changed && applySafeFixes)
                MarkSceneDirty();
        }

        private static void AuditContentLayout(AuditReport report, Transform content, bool applySafeFixes)
        {
            RectTransform contentRt = content as RectTransform;
            if (contentRt != null)
            {
                Vector2 anchorMin = contentRt.anchorMin;
                Vector2 anchorMax = contentRt.anchorMax;
                if (anchorMin != new Vector2(0f, 1f) || anchorMax != new Vector2(1f, 1f) || Mathf.Abs(contentRt.pivot.y - 1f) > 0.001f)
                {
                    report.Warn(
                        "Content ancres/pivot",
                        "anchorMin=(0,1), anchorMax=(1,1), pivot.y=1",
                        $"anchorMin={anchorMin}, anchorMax={anchorMax}, pivot={contentRt.pivot}",
                        "Ajuster les ancres du Content en haut-stretch (polish manuel)");
                }
                else
                {
                    report.Ok("Content ancres haut-stretch OK");
                }
            }

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                if (applySafeFixes)
                {
                    Undo.RecordObject(content.gameObject, "Ajouter VerticalLayoutGroup content");
                    vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                    MarkSceneDirty();
                    report.AppliedFixes.Add("VerticalLayoutGroup ajouté sur content");
                }
                else
                {
                    report.ErrorSimple("Content sans VerticalLayoutGroup", repairable: true);
                    return;
                }
            }

            bool vlgChanged = false;
            Undo.RecordObject(vlg, "Corriger VerticalLayoutGroup content");

            vlgChanged |= SetIfDifferent(vlg.childControlWidth, true, () => vlg.childControlWidth = true);
            vlgChanged |= SetIfDifferent(vlg.childControlHeight, true, () => vlg.childControlHeight = true);
            vlgChanged |= SetIfDifferent(vlg.childForceExpandWidth, true, () => vlg.childForceExpandWidth = true);
            vlgChanged |= SetIfDifferent(vlg.childForceExpandHeight, false, () => vlg.childForceExpandHeight = false);
            vlgChanged |= SetIfDifferent(vlg.spacing, SPACING, () => vlg.spacing = SPACING);
            vlgChanged |= SetIfDifferent(vlg.padding.left, PADDING_LATERAL, () => vlg.padding.left = PADDING_LATERAL);
            vlgChanged |= SetIfDifferent(vlg.padding.right, PADDING_LATERAL, () => vlg.padding.right = PADDING_LATERAL);

            if (!ValuesMatchVlg(vlg))
            {
                if (applySafeFixes && vlgChanged)
                {
                    MarkSceneDirty();
                    report.AppliedFixes.Add("VerticalLayoutGroup content aligné sur les constantes");
                    report.Ok("VerticalLayoutGroup content corrigé");
                }
                else if (!applySafeFixes)
                {
                    report.Error(
                        "VerticalLayoutGroup content",
                        DescribeExpectedVlg(),
                        DescribeFoundVlg(vlg),
                        repairable: true);
                }
            }
            else
            {
                report.Ok("VerticalLayoutGroup content conforme");
            }

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                if (applySafeFixes)
                {
                    Undo.RecordObject(content.gameObject, "Ajouter ContentSizeFitter content");
                    csf = content.gameObject.AddComponent<ContentSizeFitter>();
                    MarkSceneDirty();
                    report.AppliedFixes.Add("ContentSizeFitter ajouté sur content");
                }
                else
                {
                    report.ErrorSimple("Content sans ContentSizeFitter", repairable: true);
                    return;
                }
            }

            bool csfChanged = false;
            Undo.RecordObject(csf, "Corriger ContentSizeFitter content");

            if (csf.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
            {
                if (applySafeFixes)
                {
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    csfChanged = true;
                    report.AppliedFixes.Add("ContentSizeFitter.verticalFit → PreferredSize");
                }
                else
                {
                    report.Error(
                        "ContentSizeFitter.verticalFit",
                        "PreferredSize",
                        csf.verticalFit.ToString(),
                        repairable: true);
                }
            }
            else
            {
                report.Ok("ContentSizeFitter.verticalFit == PreferredSize");
            }

            if (csf.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
            {
                if (applySafeFixes)
                {
                    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    csfChanged = true;
                    report.AppliedFixes.Add("ContentSizeFitter.horizontalFit → Unconstrained");
                }
                else
                {
                    report.Error(
                        "ContentSizeFitter.horizontalFit",
                        "Unconstrained",
                        csf.horizontalFit.ToString(),
                        repairable: true);
                }
            }
            else
            {
                report.Ok("ContentSizeFitter.horizontalFit == Unconstrained");
            }

            if (csfChanged && applySafeFixes)
                MarkSceneDirty();
        }

        private static void AuditContentSiblingOrder(AuditReport report, Transform content, DefeatUiRefs refs)
        {
            var ordered = new List<(string Name, Transform Transform)>
            {
                ("titleText", refs.TitleText != null ? refs.TitleText.transform : null),
                ("stageReachedText", refs.StageReachedText != null ? refs.StageReachedText.transform : null),
                ("talsEarnedText", refs.TalsEarnedText != null ? refs.TalsEarnedText.transform : null),
                ("bossesDefeatedText", refs.BossesDefeatedText != null ? refs.BossesDefeatedText.transform : null),
                ("rankingContainer", refs.RankingContainer)
            };

            var wiredInContent = new HashSet<Transform>();
            int previousIndex = -1;
            string previousName = null;
            bool orderOk = true;

            for (int i = 0; i < ordered.Count; i++)
            {
                Transform t = ordered[i].Transform;
                if (t == null)
                    continue;

                if (!IsDescendantOf(t, content))
                {
                    report.ErrorManual(
                        $"{ordered[i].Name} doit être enfant de ScrollRect.content",
                        $"Reparentez '{t.name}' sous le Content du scroll");
                    orderOk = false;
                    continue;
                }

                wiredInContent.Add(t);
                int index = t.GetSiblingIndex();
                if (index <= previousIndex)
                {
                    report.ErrorManual(
                        $"Ordre des enfants du Content : {ordered[i].Name} (index {index}) doit être après {previousName} (index {previousIndex})",
                        $"Dans le Content, glissez '{t.name}' sous '{previousName}' (ordre : title → stage → tals → bosses → bonus → rankingContainer)");
                    orderOk = false;
                }

                previousIndex = index;
                previousName = ordered[i].Name;
            }

            if (orderOk)
                report.Ok("Ordre des refs wirées dans le Content conforme");

            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);
                if (!wiredInContent.Contains(child))
                    report.Info($"Sibling non wiré dans Content : [{i}] '{child.name}'");
            }
        }

        private static void AuditButtonsOutsideScroll(AuditReport report, DefeatUiRefs refs, Transform content)
        {
            AuditButtonPlacement(report, "retryButton", refs.RetryButton, refs.PanelRoot, content);
            AuditButtonPlacement(report, "buttonReturnHub", refs.ButtonReturnHub, refs.PanelRoot, content);
        }

        private static void AuditButtonPlacement(
            AuditReport report,
            string fieldName,
            Button button,
            GameObject panelRoot,
            Transform content)
        {
            if (button == null || panelRoot == null)
                return;

            Transform buttonTransform = button.transform;
            if (!IsDescendantOf(buttonTransform, panelRoot.transform))
            {
                report.ErrorManual(
                    $"{fieldName} doit être sous panelRoot",
                    $"Reparentez '{buttonTransform.name}' sous '{panelRoot.name}'");
                return;
            }

            if (content != null && IsDescendantOf(buttonTransform, content))
            {
                report.ErrorManual(
                    $"{fieldName} ne doit pas être dans le Content scrollable",
                    $"Sortez '{buttonTransform.name}' du Content — placez-le sous panelRoot hors scroll");
                return;
            }

            report.Ok($"{fieldName} hors scroll OK");

            RectTransform rt = buttonTransform as RectTransform;
            if (rt == null)
                return;

            if (rt.anchorMin.y > BOTTOM_ANCHOR_TOLERANCE || rt.anchorMax.y > BOTTOM_ANCHOR_TOLERANCE)
            {
                report.Warn(
                    $"{fieldName} ancrage bas",
                    "anchorMin.y ≈ 0 et anchorMax.y ≈ 0",
                    $"anchorMin.y={rt.anchorMin.y:F3}, anchorMax.y={rt.anchorMax.y:F3}",
                    "Ancrer le bouton en bas du panel (polish manuel)");
            }
            else
            {
                report.Ok($"{fieldName} ancré en bas");
            }
        }

        private static void AuditRankingContainerComponents(
            AuditReport report,
            Transform rankingContainer,
            bool applySafeFixes)
        {
            if (rankingContainer == null)
                return;

            AuditRankingContainerForbiddenComponents(report, rankingContainer, applySafeFixes);
            AuditRankingContainerVerticalLayout(report, rankingContainer, applySafeFixes);
        }

        private static void AuditRankingContainerForbiddenComponents(
            AuditReport report,
            Transform rankingContainer,
            bool applySafeFixes)
        {
            ContentSizeFitter csf = rankingContainer.GetComponent<ContentSizeFitter>();
            if (csf != null)
            {
                if (applySafeFixes)
                {
                    Undo.DestroyObjectImmediate(csf);
                    MarkSceneDirty();
                    report.AppliedFixes.Add("ContentSizeFitter supprimé sur rankingContainer");
                }
                else
                {
                    report.Error(
                        "rankingContainer ContentSizeFitter",
                        "absent",
                        "présent",
                        repairable: true);
                }
            }

            LayoutElement layoutElement = rankingContainer.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                if (applySafeFixes)
                {
                    Undo.DestroyObjectImmediate(layoutElement);
                    MarkSceneDirty();
                    report.AppliedFixes.Add("LayoutElement supprimé sur rankingContainer");
                }
                else
                {
                    report.Error(
                        "rankingContainer LayoutElement",
                        "absent",
                        "présent",
                        repairable: true);
                }
            }

            if (csf == null && layoutElement == null)
                report.Ok("rankingContainer sans ContentSizeFitter ni LayoutElement");
        }

        private static void AuditRankingContainerVerticalLayout(
            AuditReport report,
            Transform rankingContainer,
            bool applySafeFixes)
        {
            VerticalLayoutGroup vlg = rankingContainer.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                if (applySafeFixes)
                {
                    vlg = Undo.AddComponent<VerticalLayoutGroup>(rankingContainer.gameObject);
                    MarkSceneDirty();
                    report.AppliedFixes.Add("VerticalLayoutGroup ajouté sur rankingContainer");
                }
                else
                {
                    report.ErrorSimple("rankingContainer sans VerticalLayoutGroup", repairable: true);
                    return;
                }
            }
            else
            {
                report.Ok("rankingContainer VerticalLayoutGroup présent");
            }

            bool changed = false;
            Undo.RecordObject(vlg, "Corriger VerticalLayoutGroup rankingContainer");

            changed |= AuditRankingContainerVlgBool(
                report, applySafeFixes, "childControlWidth", vlg.childControlWidth, true,
                () => vlg.childControlWidth = true);
            changed |= AuditRankingContainerVlgBool(
                report, applySafeFixes, "childControlHeight", vlg.childControlHeight, true,
                () => vlg.childControlHeight = true);
            changed |= AuditRankingContainerVlgBool(
                report, applySafeFixes, "childForceExpandWidth", vlg.childForceExpandWidth, true,
                () => vlg.childForceExpandWidth = true);
            changed |= AuditRankingContainerVlgBool(
                report, applySafeFixes, "childForceExpandHeight", vlg.childForceExpandHeight, false,
                () => vlg.childForceExpandHeight = false);

            if (!Mathf.Approximately(vlg.spacing, SPACING))
            {
                if (applySafeFixes)
                {
                    vlg.spacing = SPACING;
                    changed = true;
                    report.AppliedFixes.Add($"rankingContainer VLG.spacing → {SPACING}");
                }
                else
                {
                    report.Error(
                        "rankingContainer VLG.spacing",
                        SPACING.ToString(),
                        vlg.spacing.ToString("F0"),
                        repairable: true);
                }
            }
            else
            {
                report.Ok($"rankingContainer VLG.spacing == {SPACING}");
            }

            changed |= AuditRankingContainerVlgPadding(report, applySafeFixes, vlg, "left", vlg.padding.left);
            changed |= AuditRankingContainerVlgPadding(report, applySafeFixes, vlg, "right", vlg.padding.right);
            changed |= AuditRankingContainerVlgPadding(report, applySafeFixes, vlg, "top", vlg.padding.top);
            changed |= AuditRankingContainerVlgPadding(report, applySafeFixes, vlg, "bottom", vlg.padding.bottom);

            if (changed && applySafeFixes)
                MarkSceneDirty();
        }

        private static bool AuditRankingContainerVlgBool(
            AuditReport report,
            bool applySafeFixes,
            string propertyName,
            bool current,
            bool expected,
            System.Action apply)
        {
            if (current == expected)
            {
                report.Ok($"rankingContainer VLG.{propertyName} == {expected}");
                return false;
            }

            if (applySafeFixes)
            {
                apply();
                report.AppliedFixes.Add($"rankingContainer VLG.{propertyName} → {expected}");
                return true;
            }

            report.Error(
                $"rankingContainer VLG.{propertyName}",
                expected.ToString(),
                current.ToString(),
                repairable: true);
            return false;
        }

        private static bool AuditRankingContainerVlgPadding(
            AuditReport report,
            bool applySafeFixes,
            VerticalLayoutGroup vlg,
            string side,
            int current)
        {
            if (current == 0)
            {
                report.Ok($"rankingContainer VLG.padding.{side} == 0");
                return false;
            }

            if (applySafeFixes)
            {
                RectOffset padding = vlg.padding;
                switch (side)
                {
                    case "left": padding.left = 0; break;
                    case "right": padding.right = 0; break;
                    case "top": padding.top = 0; break;
                    case "bottom": padding.bottom = 0; break;
                }

                vlg.padding = padding;
                report.AppliedFixes.Add($"rankingContainer VLG.padding.{side} → 0");
                return true;
            }

            report.Error(
                $"rankingContainer VLG.padding.{side}",
                "0",
                current.ToString(),
                repairable: true);
            return false;
        }

        // ═══════════════════════════════════════════
        // H — Prefab d'entrée
        // ═══════════════════════════════════════════
        private static void AuditPrefabEntry(AuditReport report, DefeatUiRefs refs, bool applySafeFixes)
        {
            if (refs.EntryPrefab == null || !PrefabUtility.IsPartOfPrefabAsset(refs.EntryPrefab.gameObject))
                return;

            string prefabPath = AssetDatabase.GetAssetPath(refs.EntryPrefab.gameObject);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            bool prefabDirty = false;

            try
            {
                EndRunCharacterEntryUI entry = root.GetComponent<EndRunCharacterEntryUI>();
                if (entry == null)
                {
                    report.ErrorSimple($"Prefab '{prefabPath}' sans EndRunCharacterEntryUI");
                    return;
                }

                SerializedObject entrySo = new SerializedObject(entry);
                for (int i = 0; i < EntryUiFields.Length; i++)
                {
                    string field = EntryUiFields[i];
                    Object value = entrySo.FindProperty(field)?.objectReferenceValue;
                    if (value == null)
                        report.ErrorSimple($"Prefab entry — {field} non wiré");
                    else
                        report.Ok($"Prefab entry — {field} wiré");
                }

                LayoutElement layoutElement = root.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    if (applySafeFixes)
                    {
                        layoutElement = root.AddComponent<LayoutElement>();
                        prefabDirty = true;
                        report.AppliedFixes.Add("LayoutElement ajouté sur la racine du prefab entry");
                    }
                    else
                    {
                        report.ErrorSimple("Prefab entry — LayoutElement racine absent", repairable: true);
                    }
                }

                if (layoutElement != null)
                {
                    if (layoutElement.preferredHeight < ENTRY_MIN_HEIGHT)
                    {
                        if (applySafeFixes)
                        {
                            layoutElement.preferredHeight = ENTRY_MIN_HEIGHT;
                            prefabDirty = true;
                            report.AppliedFixes.Add($"LayoutElement.preferredHeight → {ENTRY_MIN_HEIGHT}");
                        }
                        else
                        {
                            report.Error(
                                "Prefab entry LayoutElement.preferredHeight",
                                $">= {ENTRY_MIN_HEIGHT}",
                                layoutElement.preferredHeight.ToString("F0"),
                                repairable: true);
                        }
                    }
                    else
                    {
                        report.Ok($"Prefab entry preferredHeight >= {ENTRY_MIN_HEIGHT}");
                    }

                    if (layoutElement.flexibleWidth < 1f)
                    {
                        if (applySafeFixes)
                        {
                            layoutElement.flexibleWidth = 1f;
                            prefabDirty = true;
                            report.AppliedFixes.Add("LayoutElement.flexibleWidth → 1");
                        }
                        else
                        {
                            report.Error(
                                "Prefab entry LayoutElement.flexibleWidth",
                                ">= 1",
                                layoutElement.flexibleWidth.ToString("F1"),
                                repairable: true);
                        }
                    }
                    else
                    {
                        report.Ok("Prefab entry flexibleWidth >= 1");
                    }
                }

                prefabDirty |= FixRaycastTargets(report, root, applySafeFixes);

                if (prefabDirty && applySafeFixes)
                {
                    entrySo.Update();
                    entrySo.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    report.AppliedFixes.Add($"Prefab sauvegardé : {prefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool FixRaycastTargets(AuditReport report, GameObject root, bool applySafeFixes)
        {
            bool changed = false;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (!images[i].raycastTarget)
                    continue;

                if (applySafeFixes)
                {
                    images[i].raycastTarget = false;
                    changed = true;
                }
                else
                {
                    report.Error(
                        $"Prefab entry Image '{images[i].name}' raycastTarget",
                        "false",
                        "true",
                        repairable: true);
                }
            }

            TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (!texts[i].raycastTarget)
                    continue;

                if (applySafeFixes)
                {
                    texts[i].raycastTarget = false;
                    changed = true;
                }
                else
                {
                    report.Error(
                        $"Prefab entry TMP '{texts[i].name}' raycastTarget",
                        "false",
                        "true",
                        repairable: true);
                }
            }

            if (changed && applySafeFixes)
                report.AppliedFixes.Add("raycastTarget désactivé sur toutes les Images/TMP du prefab entry");

            if (!applySafeFixes && images.Length + texts.Length > 0)
            {
                bool allOff = true;
                for (int i = 0; i < images.Length; i++)
                    allOff &= !images[i].raycastTarget;
                for (int i = 0; i < texts.Length; i++)
                    allOff &= !texts[i].raycastTarget;
                if (allOff)
                    report.Ok("Prefab entry — tous les raycastTarget désactivés");
            }

            return changed;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
        private static ScrollRect FindAncestorScrollRect(Transform start)
        {
            Transform current = start;
            while (current != null)
            {
                ScrollRect scrollRect = current.GetComponent<ScrollRect>();
                if (scrollRect != null)
                    return scrollRect;
                current = current.parent;
            }

            return null;
        }

        private static bool IsDescendantOf(Transform child, Transform ancestor)
        {
            if (child == null || ancestor == null)
                return false;

            Transform current = child;
            while (current != null)
            {
                if (current == ancestor)
                    return true;
                current = current.parent;
            }

            return false;
        }

        private static bool SetIfDifferent<T>(T current, T expected, System.Action apply)
        {
            if (EqualityComparer<T>.Default.Equals(current, expected))
                return false;
            apply();
            return true;
        }

        private static bool ValuesMatchVlg(VerticalLayoutGroup vlg)
        {
            return vlg.childControlWidth
                && vlg.childControlHeight
                && vlg.childForceExpandWidth
                && !vlg.childForceExpandHeight
                && Mathf.Approximately(vlg.spacing, SPACING)
                && vlg.padding.left == PADDING_LATERAL
                && vlg.padding.right == PADDING_LATERAL;
        }

        private static string DescribeExpectedVlg()
        {
            return $"ctrlW/H=true, expandW=true, expandH=false, spacing={SPACING}, pad L/R={PADDING_LATERAL}";
        }

        private static string DescribeFoundVlg(VerticalLayoutGroup vlg)
        {
            return $"ctrlW={vlg.childControlWidth}, ctrlH={vlg.childControlHeight}, expandW={vlg.childForceExpandWidth}, expandH={vlg.childForceExpandHeight}, spacing={vlg.spacing}, pad L={vlg.padding.left}, R={vlg.padding.right}";
        }

        private static void MarkSceneDirty()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
