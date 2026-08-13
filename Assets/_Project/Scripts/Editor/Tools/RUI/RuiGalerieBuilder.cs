#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Characters;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// RUI1 — Galerie du socle (harnais visuel, miroir maquette HTML).
    /// Scène Dev dédiée. Menu Chez Arthur/RUI/* (RUI-D8).
    /// </summary>
    public static class RuiGalerieBuilder
    {
        private const string SceneFolder = "Assets/_Project/Scenes/Dev";
        private const string ScenePath = SceneFolder + "/RUIGalerie.unity";
        private const string ReportPath = "Audits/RUI1_Galerie_Report.md";
        private const string LibraryPath =
            "Assets/_Project/ScriptableObjects/Config/RarityVisualLibrary.asset";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI/RUI";
        private const string ListRowPrefabPath = PrefabFolder + "/ListRow.prefab";
        private const string UndoLabel = "RUI1 Galerie";
        private const string RootName = "RUIGalerieRoot";

        private static readonly List<string> ComponentOrigins = new List<string>(16);

        [MenuItem("Chez Arthur/RUI/Galerie (RUI1) — DRY RUN")]
        public static void DryRun() => Run(false);

        [MenuItem("Chez Arthur/RUI/Galerie (RUI1) — APPLIQUER")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "RUI1 Galerie",
                    "Construit la scène Dev/RUIGalerie (10 sections = maquette).\n\n"
                    + "Aucun écran de prod modifié.",
                    "Appliquer",
                    "Annuler"))
                return;
            Run(true);
        }

        private static void Run(bool apply)
        {
            ComponentOrigins.Clear();
            var log = new StringBuilder(8192);
            log.AppendLine("# RUI1 — Galerie socle — rapport");
            log.AppendLine();
            log.AppendLine("Date : " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            log.AppendLine("Mode : " + (apply ? "APPLIQUER" : "DRY RUN"));
            log.AppendLine();
            log.AppendLine("## Origine des composants (G1)");
            log.AppendLine();

            Note("UiTextStyle / ApplyTextStyle", "naissance RUI1 (map tokens existants)");
            Note("CreatePanel(1..3)", "wrap PanelSurface + panelLevel (Hub panelLevel=0 inchangé)");
            Note("CreateSectionHeader", "naissance RUI1");
            Note("CreateButton Primary/Secondary/Locked", "wrap HubButtonUI existant");
            Note("CreateButton Danger", "extension HubButtonUI.ButtonVariant");
            Note("CreateTabBar", "WRAPPER TabBarUI existant (G1)");
            Note("CreateListRow / StatCell / Chip / RewardChip", "naissance RUI1");
            Note("CreatePageScaffold / PopupScaffold", "naissance RUI1");
            Note("Rarity badges perso", "RarityBadgeView + RarityVisualLibrary (BR I1/I2)");
            Note("Rareté valise/bonus", "tokens UiTheme.Valise* / Bonus* (liseré+label)");

            for (int i = 0; i < ComponentOrigins.Count; i++)
                log.AppendLine("- " + ComponentOrigins[i]);

            log.AppendLine();
            log.AppendLine("## Sandbox historique (G3)");
            log.AppendLine();
            log.AppendLine(
                "- UIKitSandbox couvre PanelSurface samples / boutons / pills / TabBar.");
            log.AppendLine(
                "- Galerie RUI1 couvre typo + surfaces 1..3 + 4 boutons + SectionHeader + "
                + "ListRow + StatCell + chips + raretés réelles + RewardChip + TabBar + scaffolds.");
            log.AppendLine(
                "- **Verdict G3** : couverture ≥ sandbox → retraite sandbox possible "
                + "(scène + builder) après checklist device ; sinon consigner le delta.");
            log.AppendLine();

            if (!apply)
            {
                log.AppendLine("**DRY RUN — 0 écriture.**");
                WriteReport(log);
                Debug.Log("[RUI1] DRY RUN Galerie — voir " + ReportPath);
                return;
            }

            RoundedRectSpriteGenerator.GenerateAll();
            EnsureFolder(SceneFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder("Audits");
            EnsureFolder("Docs");

            Scene scene = OpenOrCreateScene();
            EnsureCamera(scene);
            EnsureEventSystem(scene);
            Canvas canvas = FindOrCreateCanvas(scene);
            Rebuild(canvas, log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            WriteDocs();
            WriteReport(log);
            AssetDatabase.SaveAssets();
            Debug.Log("[RUI1] Galerie APPLIQUER — " + ScenePath + " · " + ReportPath);
        }

        private static void Note(string component, string origin)
        {
            ComponentOrigins.Add("`" + component + "` → " + origin);
        }

        private static void Rebuild(Canvas canvas, StringBuilder log)
        {
            Transform canvasTx = canvas.transform;
            for (int i = canvasTx.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(canvasTx.GetChild(i).gameObject);

            // Fond
            GameObject bg = new GameObject(
                "Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(bg, UndoLabel);
            bg.transform.SetParent(canvasTx, false);
            Stretch((RectTransform)bg.transform);
            bg.GetComponent<Image>().color = UiTheme.BgDeep;
            bg.GetComponent<Image>().raycastTarget = false;

            GameObject root = new GameObject(RootName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, UndoLabel);
            root.transform.SetParent(canvasTx, false);
            Stretch((RectTransform)root.transform);

            GameObject scrollGo = new GameObject(
                "Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(root.transform, false);
            Stretch((RectTransform)scrollGo.transform);

            GameObject viewport = new GameObject(
                "Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            Stretch((RectTransform)viewport.transform);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject(
                "Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = (RectTransform)content.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 48);
            vlg.spacing = UiTheme.Space4;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = contentRt;
            scroll.horizontal = false;

            Transform col = content.transform;
            BuildSection1(col);
            BuildSection2(col);
            BuildSection3(col);
            BuildSection4(col);
            BuildSection5(col);
            BuildSection6(col);
            BuildSection7(col);
            BuildSection8(col);
            BuildSection9(col);
            BuildSection10(col);

            // Prefab ListRow (runtime)
            ListRowUI sampleRow = UiKitFactory.CreateListRow(col, "ListRowPrefabSource");
            if (sampleRow != null)
            {
                PrefabUtility.SaveAsPrefabAsset(sampleRow.gameObject, ListRowPrefabPath);
                Object.DestroyImmediate(sampleRow.gameObject);
                log.AppendLine();
                log.AppendLine("## Prefabs");
                log.AppendLine();
                log.AppendLine("- `" + ListRowPrefabPath + "` ✓");
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            log.AppendLine();
            log.AppendLine("**Résultat : scène Galerie construite (10 sections).**");
        }

        private static void BuildSection1(Transform col)
        {
            AddSecLabel(col, "1 · Typographie (styles TMP nommés)");
            AddStyled(col, "Display — Chez Arthur", UiTextStyle.Display);
            AddStyled(col, "H1 — Titre de page", UiTextStyle.H1);
            AddStyled(col, "H2 — Sous-titre / carte", UiTextStyle.H2);
            AddStyled(col, "Body — texte courant lisible.", UiTextStyle.Body);
            AddStyled(col, "Caption — méta / timer", UiTextStyle.Caption);
            AddStyled(col, "CHIP / LABEL", UiTextStyle.Chip);
        }

        private static void BuildSection2(Transform col)
        {
            AddSecLabel(col, "2 · Surfaces — 3 niveaux, jamais plus");
            PanelSurface p1 = UiKitFactory.CreatePanel(col, 1, "Surface_Deep");
            PadPanel(p1.gameObject, 96f);
            PanelSurface p2 = UiKitFactory.CreatePanel(p1.transform, 2, "Surface_Panel");
            PadPanel(p2.gameObject, 72f);
            PanelSurface p3 = UiKitFactory.CreatePanel(p2.transform, 3, "Surface_Elevated");
            PadPanel(p3.gameObject, 48f);
            TextMeshProUGUI inner = CreateLabel(p3.transform, "Niveau 3 (Elevated)");
            UiKitFactory.ApplyTextStyle(inner, UiTextStyle.Caption);
        }

        private static void BuildSection3(Transform col)
        {
            AddSecLabel(col, "3 · Boutons — 4 variants + verrouillé");
            UiKitFactory.CreateButton(
                col, HubButtonUI.ButtonVariant.Primary, "START A RUN", null, UiTheme.ButtonPrimaryH);
            UiKitFactory.CreateButton(
                col, HubButtonUI.ButtonVariant.Secondary, "Secondaire", null, UiTheme.ButtonSecondaryH);
            UiKitFactory.CreateButton(
                col, HubButtonUI.ButtonVariant.Danger, "Abandonner la run", null, UiTheme.ButtonSecondaryH);
            UiKitFactory.CreateButton(
                col, HubButtonUI.ButtonVariant.Secondary, "BOSS RUSH",
                "Bats au moins un boss pour débloquer", UiTheme.ButtonSecondaryH,
                locked: true, objectName: "Btn_BossRush_Locked");
        }

        private static void BuildSection4(Transform col)
        {
            AddSecLabel(col, "4 · SectionHeader + ListRow");
            UiKitFactory.CreateSectionHeader(col, "ÉQUIPE", "4");
            ListRowUI row = UiKitFactory.CreateListRow(col);
            row.SetName("Kramhoisi");
            row.SetMeta("Nv.12");
            row.SetFrameColor(UiTheme.RaritySSR);
            row.SetHp(0.78f, "780/1000");
        }

        private static void BuildSection5(Transform col)
        {
            AddSecLabel(col, "5 · StatCell ×4");
            GameObject row = new GameObject(
                "StatsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(col, false);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = UiTheme.Space2;
            hlg.childForceExpandWidth = true;
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 80f;
            le.preferredHeight = 80f;
            UiKitFactory.CreateStatCell(row.transform, "PV", "1200", UiTheme.StatHp);
            UiKitFactory.CreateStatCell(row.transform, "ATK", "340", UiTheme.StatAtk);
            UiKitFactory.CreateStatCell(row.transform, "DEF", "210", UiTheme.StatDef);
            UiKitFactory.CreateStatCell(row.transform, "VIT", "95", UiTheme.StatSpeed);
        }

        private static void BuildSection6(Transform col)
        {
            AddSecLabel(col, "6 · Chips & badges");
            GameObject row = new GameObject(
                "ChipsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(col, false);
            HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = UiTheme.Space2;
            hlg.childForceExpandWidth = false;
            LayoutElement le = row.AddComponent<LayoutElement>();
            le.minHeight = 48f;
            UiKitFactory.CreateChip(row.transform, "NOUVEAU", UiTheme.BadgeNew, UiTheme.BgDeep);
            UiKitFactory.CreateChip(row.transform, "SYNERGIE", UiTheme.BgElevated, UiTheme.TextMuted);
            UiKitFactory.CreateChip(row.transform, "ITEM", UiTheme.BadgeItem, UiTheme.TextPrimary);
        }

        private static void BuildSection7(Transform col)
        {
            AddSecLabel(col, "7 · Rareté — deux langages, jamais croisés");
            // Persos — VRAIS badges BR (G2)
            GameObject badges = new GameObject(
                "PersoBadges", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            badges.transform.SetParent(col, false);
            HorizontalLayoutGroup hlg = badges.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = UiTheme.Space3;
            LayoutElement ble = badges.AddComponent<LayoutElement>();
            ble.minHeight = 96f;
            ble.preferredHeight = 96f;

            RarityVisualLibrary library =
                AssetDatabase.LoadAssetAtPath<RarityVisualLibrary>(LibraryPath);
            CreateRealBadge(badges.transform, library, CharacterRarity.SR, false);
            CreateRealBadge(badges.transform, library, CharacterRarity.SSR, false);
            CreateRealBadge(badges.transform, library, CharacterRarity.LR, true);

            // Valises — liseré + label (tokens)
            AddSecLabel(col, "Valises / bonus (liseré + label)");
            GameObject valises = new GameObject(
                "ValiseRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            valises.transform.SetParent(col, false);
            valises.GetComponent<HorizontalLayoutGroup>().spacing = UiTheme.Space2;
            LayoutElement vle = valises.AddComponent<LayoutElement>();
            vle.minHeight = 56f;
            CreateValiseChip(valises.transform, "COMMUNE", UiTheme.ValiseCommune);
            CreateValiseChip(valises.transform, "RARE", UiTheme.ValiseRare);
            CreateValiseChip(valises.transform, "ÉPIQUE", UiTheme.ValiseEpique);
            CreateValiseChip(valises.transform, "LÉGENDAIRE", UiTheme.ValiseLegendaire);
        }

        private static void CreateRealBadge(
            Transform parent, RarityVisualLibrary library, CharacterRarity rarity, bool animate)
        {
            GameObject go = new GameObject(
                "Badge_" + rarity, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.minWidth = 72f;
            le.preferredWidth = 72f;
            le.minHeight = 72f;
            le.preferredHeight = 72f;

            RarityBadgeView view = go.AddComponent<RarityBadgeView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("library").objectReferenceValue = library;
            so.FindProperty("playAnimation").boolValue = animate;
            so.ApplyModifiedPropertiesWithoutUndo();
            view.Bind(rarity);
            if (animate)
                view.SetPlaying(true);
        }

        private static void CreateValiseChip(Transform parent, string label, Color accent)
        {
            PanelSurface panel = UiKitFactory.CreatePanel(parent, 3, "Valise_" + label);
            LayoutElement le = panel.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 48f;
            le.preferredHeight = 48f;
            le.minWidth = 120f;
            le.preferredWidth = 140f;
            // Liseré = border accent via Image racine
            Image border = panel.GetComponent<Image>();
            if (border != null)
                border.color = accent;
            TextMeshProUGUI tmp = CreateLabel(panel.transform, label);
            UiKitFactory.ApplyTextStyle(tmp, UiTextStyle.Chip);
            tmp.color = accent;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void BuildSection8(Transform col)
        {
            AddSecLabel(col, "8 · Récompense (Tals — devise unique)");
            UiKitFactory.CreateRewardChip(col, 150);
        }

        private static void BuildSection9(Transform col)
        {
            AddSecLabel(col, "9 · TabBar");
            string[] labels = { "Équipe", "Valises", "Réglages" };
            TabBarUI tabs = UiKitFactory.CreateTabBar(col, labels);
            if (tabs != null)
                tabs.Init(labels, null, 0);
        }

        private static void BuildSection10(Transform col)
        {
            AddSecLabel(col, "10 · PageScaffold — zones réservées");
            LayoutElement hostLe = new GameObject(
                "ScaffoldHost", typeof(RectTransform), typeof(LayoutElement))
                .GetComponent<LayoutElement>();
            hostLe.gameObject.transform.SetParent(col, false);
            hostLe.minHeight = 520f;
            hostLe.preferredHeight = 520f;
            PageScaffold page = UiKitFactory.CreatePageScaffold(hostLe.transform);
            if (page != null && page.TitleZone != null)
            {
                TextMeshProUGUI t = CreateLabel(page.TitleZone, "Titre dans la zone titre");
                UiKitFactory.ApplyTextStyle(t, UiTextStyle.H1);
            }

            AddSecLabel(col, "PopupScaffold (micro-décision)");
            LayoutElement popupHost = new GameObject(
                "PopupHost", typeof(RectTransform), typeof(LayoutElement))
                .GetComponent<LayoutElement>();
            popupHost.gameObject.transform.SetParent(col, false);
            popupHost.minHeight = 480f;
            popupHost.preferredHeight = 480f;
            UiKitFactory.CreatePopupScaffold(popupHost.transform);
        }

        private static void AddSecLabel(Transform parent, string text)
        {
            TextMeshProUGUI tmp = CreateLabel(parent, text);
            UiKitFactory.ApplyTextStyle(tmp, UiTextStyle.Chip);
            tmp.color = UiTheme.AccentTeal;
            LayoutElement le = tmp.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 36f;
            le.preferredHeight = 36f;
        }

        private static void AddStyled(Transform parent, string text, UiTextStyle style)
        {
            TextMeshProUGUI tmp = CreateLabel(parent, text);
            UiKitFactory.ApplyTextStyle(tmp, style);
            LayoutElement le = tmp.gameObject.AddComponent<LayoutElement>();
            le.minHeight = style == UiTextStyle.Display ? 72f : 40f;
            le.preferredHeight = le.minHeight;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string text)
        {
            GameObject go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void PadPanel(GameObject go, float height)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Scene OpenOrCreateScene()
        {
            if (File.Exists(ScenePath))
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void EnsureCamera(Scene scene)
        {
            Camera cam = Object.FindObjectOfType<Camera>();
            if (cam != null)
                return;
            GameObject go = new GameObject("Main Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(go, scene);
            cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiTheme.BgDeep;
            go.tag = "MainCamera";
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
                return;
            GameObject go = new GameObject(
                "EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        private static Canvas FindOrCreateCanvas(Scene scene)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.scene == scene)
                    return canvases[i];
            }

            GameObject go = new GameObject(
                "Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path) || Directory.Exists(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, name);
            else
                Directory.CreateDirectory(path);
        }

        private static void WriteDocs()
        {
            File.WriteAllText(
                "Docs/RUI_Regles_Usage.md",
                @"# RUI — Règles d'usage (v1)

Source : maquette Galerie validée (RUI-D6) + audit §3.

1. **Raretés jamais croisées** : personnages = `RarityBadgeView` / `RarityVisualLibrary` (BR I1/I2). Valises & bonus = liseré + label via `UiTheme.Valise*` / `Bonus*` — jamais les couleurs perso sur une valise.
2. **3 niveaux de surface max** : Deep / Panel / Elevated (`CreatePanel(1..3)`). Pas de 4ᵉ fond improvisé.
3. **PageScaffold obligatoire** pour toute page (Header 112 / Titre / Scroll / Footer 152). Titres dans la zone titre — jamais sous le header.
4. **Popups = micro-décisions seulement** (RUI-D2). Contenus riches → pages.
5. **Chip synergie fermé par défaut** (extensible au tap).
6. **Touch min 96** (`UiTheme.TouchTargetMin`).
7. **Boutons** : Primary / Secondary / Danger / Locked+condition (`SetSubLabel`) — une seule famille `HubButtonUI`.
8. **Typo** : `UiTextStyle` uniquement (Display/H1/H2/Body/Caption/Chip) via `UiKitFactory.ApplyTextStyle`.
",
                Encoding.UTF8);

            File.WriteAllText(
                "Docs/RUI_Contrat_Artistes.md",
                @"# RUI — Contrat artistes v1

Règle : **habillage remplaçable sans toucher la structure**. Les builders posent des zones nommées + 9-slice ; les artistes swapent les sprites.

## Slots skinnables (noms stables)

| Composant | Slot / enfant | État |
|---|---|---|
| `PanelSurface` | Image racine (bordure) + enfant `Fill` | Deep/Panel/Elevated via `panelLevel` ; bordures Subtle/Amber/Gold |
| `HubButtonUI` | racine + `Fill` + `Label` + `SubLabel` | Primary / Secondary / Danger / Locked |
| `TabBarUI` | `TabItemTemplate` → `Fill` + `Label` (+ `Icon`) | Active / Inactive |
| `SectionHeaderUI` | `AccentBar` + `Title` + `Count` | — |
| `ListRowUI` | `Avatar` + `Name` + `Meta` + `HpBar` | Frame couleur = rareté perso (badge séparé) |
| `StatCellUI` | fond + `Label` + `Value` | Accent PV/ATK/DEF/VIT |
| `UiChipUI` / `RewardChipUI` | fond + `Label` / `Icon`+`Amount` | — |
| `RarityBadgeView` | Image (frames lib) | SR/SSR/LR — **ne pas** redessiner hors lib |
| `PageScaffold` | `HeaderZone` / `TitleZone` / `ScrollZone` / `FooterZone` | Hauteurs tokens |
| `PopupScaffold` | `Scrim` + `Card` | Micro-décision |

## Sprites 9-slice

Générés : `RoundedRect_S/M/L` (RadiusS/M/L). Remplacer = même noms / mêmes border slices.

## Interdit artistes

- Modifier la hiérarchie des zones scaffold
- Mélanger palette perso et valise
- Poser du texte hors zone titre sur une page
",
                Encoding.UTF8);
        }

        private static void WriteReport(StringBuilder log)
        {
            EnsureFolder("Audits");
            File.WriteAllText(ReportPath, log.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(ReportPath);
        }
    }
}
#endif
