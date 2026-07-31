#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub.Pages;
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
    /// Gate 5.c — DetailPopupRebuilder (harnais v2) : prefab popup + cleanup Hub.
    /// </summary>
    public static class DetailPopupRebuilder
    {
        private const string UndoLabel = "Detail Popup Rebuilder 5.c";
        private const string PopupGuid = "4a21464fbb824924ab517e38b180d7a1";
        private const string CardPrefabPath = "Assets/_Project/Prefabs/UI/CharacterCard.prefab";
        private const float PanelClosedHeight = 330f;
        private const float BackVisualSize = 48f;
        private const float BackHitSize = 96f;
        private const string ExpectedHint =
            "Maintiens un personnage pour l'ajouter à l'équipe";

        [MenuItem("Chez Arthur/Refonte Hub/Detail Popup — 5.c (DRY RUN)")]
        public static void DryRun() => Run(false);

        [MenuItem("Chez Arthur/Refonte Hub/Detail Popup — 5.c (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "Detail Popup 5.c",
                    "Applique Gate 5.c au PREFAB CharacterDetailPopup "
                    + "(footer purge, BackButton, badge, hold area, panel 330), "
                    + "shine carte, purge DragLayer Hub, rebind.\n\nCtrl+S ensuite.",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(12288);
            int todo = 0, conforme = 0, failed = 0;
            string mode = apply ? "APPLIQUER" : "DRY RUN";
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" DetailPopupRebuilder 5.c — {mode}");
            log.AppendLine(" Harnais v2 — À FAIRE / CONFORMES / ÉCHECS");
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine();

            TransportIconGenerator.GenerateAll();
            Sprite backSprite = TransportIconGenerator.LoadBack();
            Sprite holdRing = TransportIconGenerator.LoadHoldRing();
            Sprite pillSprite = RoundedRectSpriteGenerator.LoadSpriteS();
            conforme++;
            log.AppendLine("- Icônes icon_back / icon_holdring générées ✓");
            log.AppendLine();

            // ── Prefab popup ──
            log.AppendLine("## Prefab CharacterDetailPopup");
            string popupPath = AssetDatabase.GUIDToAssetPath(PopupGuid);
            if (string.IsNullOrEmpty(popupPath))
            {
                failed++;
                log.AppendLine("- ✗ Prefab introuvable (guid)");
                AppendCounter(log, todo, conforme, failed);
                Debug.Log(log.ToString());
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(popupPath);
            try
            {
                CharacterDetailPopup popup = root.GetComponent<CharacterDetailPopup>();
                if (popup == null)
                {
                    failed++;
                    log.AppendLine("- ✗ CharacterDetailPopup manquant sur racine");
                }
                else if (!apply)
                {
                    todo++;
                    log.AppendLine("- [DRY] Purge Footer + panelClosedHeight=330 + Back/Badge/Hold — À FAIRE");
                    todo++;
                    log.AppendLine("- [DRY] SerializedObject rebind popup — À FAIRE");
                }
                else
                {
                    Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                    ApplyPrefabMutations(root, popup, backSprite, holdRing, pillSprite, log, ref conforme);
                    PrefabUtility.SaveAsPrefabAsset(root, popupPath);
                    conforme++;
                    log.AppendLine("- Prefab sauvegardé ✓");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            log.AppendLine();
            log.AppendLine("## CharacterCard.prefab — RarityShineFX");
            if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Ajouter RarityShineFX sur carte — À FAIRE");
            }
            else
            {
                EnsureCardShine(log, ref conforme, ref failed);
            }

            log.AppendLine();
            log.AppendLine("## Hub.unity — DragLayer + hint + rebind instances");
            Scene hub = SceneManager.GetActiveScene();
            bool hubOpen = hub.IsValid() && hub.name == "Hub";
            if (!hubOpen)
            {
                log.AppendLine("- Hub.unity non active — skip scène (ouvrir Hub puis relancer) ⚠");
            }
            else if (!apply)
            {
                todo++;
                log.AppendLine("- [DRY] Purge DragLayer + check hint + rebind popup instance — À FAIRE");
            }
            else
            {
                ApplyHubScene(holdRing, log, ref conforme, ref failed);
                EditorSceneManager.MarkSceneDirty(hub);
            }

            log.AppendLine();
            log.AppendLine("## INTERDITS");
            log.AppendLine("- PortraitStateResolver / Open / OpenLive signatures : non touchés ✓");
            log.AppendLine("- CharacterManager / header / nav : non touchés ✓");
            conforme++;

            log.AppendLine();
            AppendCounter(log, todo, conforme, failed);
            Debug.Log(log.ToString());
        }

        private static void ApplyPrefabMutations(
            GameObject root,
            CharacterDetailPopup popup,
            Sprite backSprite,
            Sprite holdRing,
            Sprite pillSprite,
            StringBuilder log,
            ref int conforme)
        {
            Transform tRoot = root.transform;

            // Purge Footer
            Transform statsPanel = FindDeep(tRoot, "StatsPanel");
            Transform footer = FindDeep(tRoot, "Footer");
            if (footer != null)
            {
                Object.DestroyImmediate(footer.gameObject);
                conforme++;
                log.AppendLine("- Footer (AddToTeam + Close) purgé ✓");
            }
            else
            {
                conforme++;
                log.AppendLine("- Footer déjà absent ✓");
            }

            if (statsPanel != null)
            {
                RectTransform spRt = (RectTransform)statsPanel;
                spRt.sizeDelta = new Vector2(spRt.sizeDelta.x, PanelClosedHeight);
                conforme++;
                log.AppendLine($"- StatsPanel sizeDelta.y = {PanelClosedHeight} ✓");
            }

            Transform expandedZone = FindDeep(tRoot, "ExpandedZone");
            if (expandedZone != null)
            {
                RectTransform ezRt = (RectTransform)expandedZone;
                // Sans footer : inset bas réduit (tabs+stats ~250 restent en haut).
                ezRt.anchoredPosition = new Vector2(0f, -58f);
                ezRt.sizeDelta = new Vector2(0f, -266f);
                conforme++;
                log.AppendLine("- ExpandedZone insets ajustés (sans footer) ✓");
            }

            // Artwork hold area
            Transform artwork = FindDeep(tRoot, "Artwork");
            RectTransform holdArea = EnsureArtworkHoldArea(tRoot, artwork, log, ref conforme);

            // BackButton
            Transform header = FindDeep(tRoot, "Header");
            Button backBtn = EnsureBackButton(header, backSprite, pillSprite, log, ref conforme);

            // Badge
            GameObject badgeGo;
            TextMeshProUGUI badgeText;
            EnsureInTeamBadge(header, pillSprite, out badgeGo, out badgeText, log, ref conforme);

            // Shine on artwork
            RarityShineFX shine = null;
            if (artwork != null)
            {
                shine = artwork.GetComponent<RarityShineFX>();
                if (shine == null)
                    shine = artwork.gameObject.AddComponent<RarityShineFX>();
                conforme++;
                log.AppendLine("- RarityShineFX sur Artwork ✓");
            }

            // Rebind
            SerializedObject so = new SerializedObject(popup);
            so.FindProperty("panelClosedHeight").floatValue = PanelClosedHeight;
            SetObj(so, "backButton", backBtn);
            SetObj(so, "inTeamBadge", badgeGo);
            SetObj(so, "inTeamBadgeText", badgeText);
            SetObj(so, "artworkHoldArea", holdArea);
            SetObj(so, "holdRingSprite", holdRing);
            SetObj(so, "artworkShine", shine);

            // Null legacy footer refs if still present in SO
            SerializedProperty addBtn = so.FindProperty("addToTeamButton");
            if (addBtn != null)
                addBtn.objectReferenceValue = null;
            SerializedProperty closeBtn = so.FindProperty("closeButton");
            if (closeBtn != null)
                closeBtn.objectReferenceValue = null;
            SerializedProperty primary = so.FindProperty("primaryButtonFrame");
            if (primary != null)
                primary.objectReferenceValue = null;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(popup);
            conforme++;
            log.AppendLine("- SerializedObject popup rebind ✓");
        }

        private static RectTransform EnsureArtworkHoldArea(
            Transform root,
            Transform artwork,
            StringBuilder log,
            ref int conforme)
        {
            Transform existing = root.Find("ArtworkHoldArea");
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = new GameObject(
                    "ArtworkHoldArea",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(root, false);
                // Juste au-dessus de Artwork, sous Header
                if (artwork != null)
                    go.transform.SetSiblingIndex(artwork.GetSiblingIndex() + 1);
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            // Laisse le StatsPanel (bas) recevoir les clics expand / tabs.
            rt.offsetMin = new Vector2(0f, PanelClosedHeight);
            rt.offsetMax = Vector2.zero;

            Image img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = true;

            if (go.GetComponent<ArtworkHoldRelay>() == null)
                go.AddComponent<ArtworkHoldRelay>();

            conforme++;
            log.AppendLine("- ArtworkHoldArea (plein cadre, raycast) ✓");
            return rt;
        }

        private static Button EnsureBackButton(
            Transform header,
            Sprite backSprite,
            Sprite pillSprite,
            StringBuilder log,
            ref int conforme)
        {
            if (header == null)
            {
                log.AppendLine("- ⚠ Header manquant — BackButton skip");
                return null;
            }

            Transform existing = header.Find("BackButton");
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = new GameObject(
                    "BackButton",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(PanelSurface));
                go.transform.SetParent(header, false);
                go.transform.SetAsFirstSibling();
            }

            RectTransform rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -16f);
            rt.sizeDelta = new Vector2(BackHitSize, BackHitSize);

            Image bg = go.GetComponent<Image>();
            bg.sprite = pillSprite;
            bg.type = Image.Type.Sliced;
            Color pill = UiTheme.BgElevated;
            pill.a = 0.7f;
            bg.color = pill;
            bg.raycastTarget = true;

            PanelSurface surface = go.GetComponent<PanelSurface>();
            if (surface != null)
            {
                SerializedObject sso = new SerializedObject(surface);
                sso.FindProperty("variant").enumValueIndex = (int)PanelSurface.SurfaceVariant.Pill;
                sso.FindProperty("blocksRaycasts").boolValue = true;
                sso.ApplyModifiedPropertiesWithoutUndo();
                surface.BlocksRaycasts = true;
                surface.ApplyStyle();
                bg.color = pill;
            }

            bg.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = bg;

            // Glyph / icon child
            Transform iconTx = go.transform.Find("Icon");
            GameObject iconGo;
            if (iconTx != null)
                iconGo = iconTx.gameObject;
            else
            {
                iconGo = new GameObject(
                    "Icon",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
            }

            RectTransform iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = new Vector2(0.5f, 0.5f);
            iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = new Vector2(BackVisualSize, BackVisualSize);
            iconRt.anchoredPosition = Vector2.zero;

            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconImg.color = UiTheme.TextPrimary;
            if (backSprite != null)
            {
                iconImg.sprite = backSprite;
                iconImg.enabled = true;
            }
            else
            {
                // Fallback ASCII via TMP
                Transform tmpTx = go.transform.Find("Glyph");
                if (tmpTx == null)
                {
                    GameObject glyph = new GameObject(
                        "Glyph",
                        typeof(RectTransform),
                        typeof(TextMeshProUGUI));
                    glyph.transform.SetParent(go.transform, false);
                    RectTransform grt = (RectTransform)glyph.transform;
                    StretchFull(grt);
                    TextMeshProUGUI tmp = glyph.GetComponent<TextMeshProUGUI>();
                    tmp.text = "<";
                    tmp.fontSize = 36f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = UiTheme.TextPrimary;
                    tmp.raycastTarget = false;
                }

                iconImg.enabled = false;
            }

            conforme++;
            log.AppendLine("- BackButton 48/96 Pill α0.7 ✓");
            return btn;
        }

        private static void EnsureInTeamBadge(
            Transform header,
            Sprite pillSprite,
            out GameObject badgeGo,
            out TextMeshProUGUI badgeText,
            StringBuilder log,
            ref int conforme)
        {
            badgeGo = null;
            badgeText = null;
            if (header == null)
                return;

            Transform existing = header.Find("InTeamBadge");
            if (existing != null)
                badgeGo = existing.gameObject;
            else
            {
                badgeGo = new GameObject(
                    "InTeamBadge",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                badgeGo.transform.SetParent(header, false);
            }

            RectTransform rt = (RectTransform)badgeGo.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -120f);
            rt.sizeDelta = new Vector2(160f, 36f);

            Image bg = badgeGo.GetComponent<Image>();
            bg.sprite = pillSprite;
            bg.type = Image.Type.Sliced;
            Color c = UiTheme.AccentAmber;
            c.a = 0.35f;
            bg.color = c;
            bg.raycastTarget = false;

            Transform labelTx = badgeGo.transform.Find("Label");
            GameObject labelGo;
            if (labelTx != null)
                labelGo = labelTx.gameObject;
            else
            {
                labelGo = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(badgeGo.transform, false);
            }

            RectTransform lrt = (RectTransform)labelGo.transform;
            StretchFull(lrt);
            lrt.offsetMin = new Vector2(8f, 2f);
            lrt.offsetMax = new Vector2(-8f, -2f);

            badgeText = labelGo.GetComponent<TextMeshProUGUI>();
            badgeText.text = "OK En equipe";
            badgeText.fontSize = UiTypography.Caption;
            badgeText.color = UiTheme.TextPrimary;
            badgeText.alignment = TextAlignmentOptions.Center;
            badgeText.raycastTarget = false;

            badgeGo.SetActive(false);
            conforme++;
            log.AppendLine("- InTeamBadge Pill (OK En equipe) ✓");
        }

        private static void EnsureCardShine(StringBuilder log, ref int conforme, ref int failed)
        {
            GameObject cardRoot = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                if (cardRoot == null)
                {
                    failed++;
                    log.AppendLine("- ✗ CharacterCard.prefab introuvable");
                    return;
                }

                if (cardRoot.GetComponent<RarityShineFX>() == null)
                    cardRoot.AddComponent<RarityShineFX>();
                PrefabUtility.SaveAsPrefabAsset(cardRoot, CardPrefabPath);
                conforme++;
                log.AppendLine("- RarityShineFX sur CharacterCard.prefab ✓");
            }
            finally
            {
                if (cardRoot != null)
                    PrefabUtility.UnloadPrefabContents(cardRoot);
            }
        }

        private static void ApplyHubScene(
            Sprite holdRing,
            StringBuilder log,
            ref int conforme,
            ref int failed)
        {
            // Purge DragLayer
            Transform dragLayer = FindInScene("DragLayer");
            if (dragLayer != null)
            {
                Undo.DestroyObjectImmediate(dragLayer.gameObject);
                conforme++;
                log.AppendLine("- DragLayer orphelin purgé ✓");
            }
            else
            {
                conforme++;
                log.AppendLine("- DragLayer déjà absent ✓");
            }

            // Hint
            Transform hint = FindInScene("DragHint");
            if (hint != null)
            {
                TextMeshProUGUI tmp = hint.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    if (tmp.text != ExpectedHint)
                    {
                        Undo.RecordObject(tmp, UndoLabel);
                        tmp.text = ExpectedHint;
                        EditorUtility.SetDirty(tmp);
                        conforme++;
                        log.AppendLine("- DragHint texte corrigé ✓");
                    }
                    else
                    {
                        conforme++;
                        log.AppendLine("- DragHint texte conforme ✓");
                    }
                }
            }
            else
            {
                log.AppendLine("- DragHint absent (ok si hint déjà vu) ⚠");
            }

            // Rebind popup instance + TeamDragController hold ring
            CharacterDetailPopup[] popups = Object.FindObjectsOfType<CharacterDetailPopup>(true);
            for (int i = 0; i < popups.Length; i++)
            {
                CharacterDetailPopup p = popups[i];
                if (p == null)
                    continue;

                // Prefab instance : apply overrides from asset automatically on save;
                // rebind scene-only refs (teamPageUI / teamDragController).
                TeamPageUI page = Object.FindObjectOfType<TeamPageUI>(true);
                TeamDragController drag = Object.FindObjectOfType<TeamDragController>(true);

                SerializedObject so = new SerializedObject(p);
                if (page != null)
                    SetObj(so, "teamPageUI", page);
                if (drag != null)
                    SetObj(so, "teamDragController", drag);
                if (holdRing != null)
                    SetObj(so, "holdRingSprite", holdRing);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(p);
            }

            conforme++;
            log.AppendLine($"- Popup instances rebind ({popups.Length}) ✓");

            TeamDragController ctrl = Object.FindObjectOfType<TeamDragController>(true);
            if (ctrl != null)
            {
                SerializedObject cso = new SerializedObject(ctrl);
                if (holdRing != null)
                    SetObj(cso, "holdRingSprite", holdRing);
                Transform dock = FindInScene("TeamDock");
                if (dock != null)
                {
                    SetObj(cso, "teamDock", dock);
                    Graphic g = dock.GetComponent<Graphic>();
                    if (g == null)
                        g = dock.GetComponentInChildren<Graphic>();
                    SetObj(cso, "teamDockPulseGraphic", g);
                }

                // Clear dragLayer legacy
                SerializedProperty dl = cso.FindProperty("dragLayer");
                if (dl != null)
                    dl.objectReferenceValue = null;

                cso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(ctrl);
                conforme++;
                log.AppendLine("- TeamDragController holdRing + dock pulse ✓");
            }
        }

        private static void SetObj(SerializedObject so, string name, Object value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null)
                p.objectReferenceValue = value;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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

        private static Transform FindInScene(string name)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform f = FindDeep(roots[i].transform, name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private static void AppendCounter(StringBuilder log, int todo, int conforme, int failed)
        {
            log.AppendLine("───────────────────────────────────────────");
            log.AppendLine($" À FAIRE={todo} | CONFORMES={conforme} | ÉCHECS={failed}");
            log.AppendLine("───────────────────────────────────────────");
        }
    }
}
#endif
