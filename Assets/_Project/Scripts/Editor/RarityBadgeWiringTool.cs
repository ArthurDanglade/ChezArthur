#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChezArthur.Characters;
using ChezArthur.Hub.Pages;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// BR1 — pose / converge badges de rareté (librairie + View carte + View popup).
    /// Idempotent et convergent (A2) : re-run assigne frames manquantes ; « 0 changement »
    /// seulement à l'état cible.
    /// </summary>
    public static class RarityBadgeWiringTool
    {
        private const string UndoLabel = "BR1 Rarity Badges";
        private const string LibraryPath =
            "Assets/_Project/ScriptableObjects/Config/RarityVisualLibrary.asset";
        private const string RarityFolder = "Assets/_Project/Sprites/UI/Rarity";
        private const string CardPrefabPath = "Assets/_Project/Prefabs/UI/CharacterCard.prefab";
        private const string PopupPrefabPath = "Assets/_Project/Prefabs/UI/CharacterDetailPopup.prefab";
        private const string ReportPath = "Audits/BR1_RarityBadges_Report.md";

        private const string SheetSr = "badge_sr_sheet.png";
        private const string SheetSsr = "badge_ssr_sheet.png";
        private const string SheetLr = "badge_lr_sheet.png";

        private const float CardBadgeWidthRatio = 0.52f;
        private const float CardBadgeOverhang = 8f;
        private const float PopupBadgeSize = 200f;
        private const float PopupBadgeTiltZ = -12f;
        private const float SlotBadgeRatio = 0.42f;
        private const float SlotBadgeOverhang = 4f;
        private const float BackButtonSize = 72f;
        private const float BackIconSize = 44f;
        private const float BackMargin = 16f;
        private const float PanelTitleHeight = 52f;
        private const float PanelClosedHeightTarget = 322f;

        [MenuItem("Chez Arthur/UI/BR1 — Poser les badges de rareté")]
        public static void Run()
        {
            // Application.isBatchMode (pas EditorApplication — inexistant en 2022.3).
            if (!Application.isBatchMode)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
                {
                    Debug.LogWarning("[BR1] Scène dirty non sauvée — abort (MT0).");
                    return;
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isDirty)
                {
                    Debug.LogWarning("[BR1] Scène dirty — abort (MT0). Sauve puis relance.");
                    return;
                }
            }

            var report = new StringBuilder(4096);
            int changes = 0;
            var converged = new List<string>();

            report.AppendLine("# BR1 — Badges de rareté — rapport wiring");
            report.AppendLine();
            report.AppendLine("Date : " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            report.AppendLine();
            report.AppendLine("## Traçabilité");
            report.AppendLine();
            report.AppendLine(
                "- **Gate 5.c.1** = lane **Refonte Hub** (`DetailPopupPolishBuilder`, "
                + "menus `Chez Arthur/Refonte Hub/Detail Popup — Polish 5.c.1`). "
                + "Absente de la bibliothèque docs projet.");
            report.AppendLine(
                "- Dette popup hors BR1 (propriétaire Refonte Hub) : `typeText`, "
                + "`rarityChipText`, `rarityChipFrame` sérialisés morts.");
            report.AppendLine(
                "- **BR-D5** : 5.c.1 a coupé le shine popup — ne pas réintroduire un shine "
                + "SSR/LR sans retrouver ce verdict.");
            report.AppendLine(
                "- **A1** : refs `badgeRarityText` / `badgeSprites` aussi dans "
                + "`TeamPageRebuilder` et `CharacterCardPolishBuilder` → alignés pour "
                + "ne plus recréer les orphelins.");
            report.AppendLine();

            EnsureRarityFolder(report, ref changes, converged);
            EnsureSpriteImport(Path.Combine(RarityFolder, SheetSr).Replace('\\', '/'), report, ref changes, converged);
            EnsureSpriteImport(Path.Combine(RarityFolder, SheetSsr).Replace('\\', '/'), report, ref changes, converged);
            EnsureSpriteImport(Path.Combine(RarityFolder, SheetLr).Replace('\\', '/'), report, ref changes, converged);

            RarityVisualLibrary library = EnsureLibrary(report, ref changes, converged);
            if (library != null)
                ConvergeLibraryFrames(library, report, ref changes, converged);

            WireCardPrefab(library, report, ref changes, converged);
            WirePopupPrefab(library, report, ref changes, converged);
            WireTeamSlots(library, report, ref changes, converged);

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
            if (changes == 0)
                report.AppendLine("**Résultat : 0 changement** (état cible atteint).");
            else
                report.AppendLine($"**Résultat : {changes} changement(s)** — re-run jusqu'à 0.");

            Directory.CreateDirectory("Audits");
            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log(report.ToString());
        }

        // ═══════════════════════════════════════════
        // ASSETS
        // ═══════════════════════════════════════════

        private static void EnsureRarityFolder(
            StringBuilder report, ref int changes, List<string> converged)
        {
            if (AssetDatabase.IsValidFolder(RarityFolder))
            {
                report.AppendLine("- Dossier Rarity/ déjà présent ✓");
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Sprites/UI"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Sprites"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Sprites");
                AssetDatabase.CreateFolder("Assets/_Project/Sprites", "UI");
            }

            AssetDatabase.CreateFolder("Assets/_Project/Sprites/UI", "Rarity");
            changes++;
            converged.Add("Créé dossier Sprites/UI/Rarity/");
            report.AppendLine("- Dossier Rarity/ créé");
        }

        private static void EnsureSpriteImport(
            string path, StringBuilder report, ref int changes, List<string> converged)
        {
            if (!File.Exists(path))
            {
                report.AppendLine($"- ⚠ Sheet absente : `{Path.GetFileName(path)}` (lib frames vides jusqu'à pose)");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(path);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                report.AppendLine($"- ✗ Import impossible : {path}");
                return;
            }

            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            string fileName = Path.GetFileName(path);
            bool multiSheet = fileName == SheetSsr || fileName == SheetLr;
            SpriteImportMode wantMode = multiSheet
                ? SpriteImportMode.Multiple
                : SpriteImportMode.Single;

            if (importer.spriteImportMode != wantMode)
            {
                importer.spriteImportMode = wantMode;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (importer.isReadable)
            {
                importer.isReadable = false;
                dirty = true;
            }

            if (importer.sRGBTexture == false)
            {
                importer.sRGBTexture = true;
                dirty = true;
            }

            if (importer.maxTextureSize > 2048)
            {
                importer.maxTextureSize = 2048;
                dirty = true;
            }

            // Filtre : Point pour SR pixel-art ; Bilinear pour SSR/LR métallisés.
            FilterMode wantFilter = fileName == SheetSr ? FilterMode.Point : FilterMode.Bilinear;
            if (importer.filterMode != wantFilter)
            {
                importer.filterMode = wantFilter;
                dirty = true;
            }

            // FullRect obligatoire : Tight casse les badges (L/R = îlots déconnectés).
            var texSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(texSettings);
            if (texSettings.spriteMeshType != SpriteMeshType.FullRect)
            {
                texSettings.spriteMeshType = SpriteMeshType.FullRect;
                texSettings.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(texSettings);
                dirty = true;
            }

            // Mode Multiple doit être posé avant le data provider de slice.
            if (dirty)
                importer.SaveAndReimport();

            bool sliceDirty = false;
            if (multiSheet)
                sliceDirty = ApplyMultiSheetSlice(importer, path);

            if (dirty || sliceDirty)
            {
                if (sliceDirty)
                    importer.SaveAndReimport();

                changes++;
                converged.Add("Import Sprite réglé : " + fileName);
                report.AppendLine($"- Import ajusté `{fileName}`"
                    + (multiSheet ? " (Multiple FullRect, pad 2)" : " (Single FullRect)"));
            }
            else
            {
                report.AppendLine($"- Import OK `{fileName}` ✓");
            }
        }

        /// <summary>
        /// Découpe SSR/LR via ISpriteEditorDataProvider.
        /// Cell = (largeur sheet − pads) / 10 — lit la texture réelle (A2 convergent).
        /// </summary>
        private static bool ApplyMultiSheetSlice(TextureImporter importer, string path)
        {
            const int FrameCount = 10;
            const int Pad = 2;

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                return false;

            int cellW = (tex.width - (FrameCount - 1) * Pad) / FrameCount;
            int cellH = tex.height;
            if (cellW < 1 || cellH < 1)
                return false;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider dataProvider =
                factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
                return false;

            dataProvider.InitSpriteEditorDataProvider();
            SpriteRect[] existing = dataProvider.GetSpriteRects();
            bool needsUpdate = existing == null || existing.Length != FrameCount;
            if (!needsUpdate)
            {
                for (int i = 0; i < FrameCount; i++)
                {
                    float wantX = i * (cellW + Pad);
                    Rect r = existing[i].rect;
                    if (r.x != wantX || r.y != 0f || r.width != cellW || r.height != cellH)
                    {
                        needsUpdate = true;
                        break;
                    }
                }
            }

            if (!needsUpdate)
                return false;

            string baseName = Path.GetFileNameWithoutExtension(path);
            var rects = new SpriteRect[FrameCount];
            var namePairs = new List<SpriteNameFileIdPair>(FrameCount);
            for (int i = 0; i < FrameCount; i++)
            {
                GUID spriteId = GUID.Generate();
                if (existing != null && i < existing.Length)
                {
                    GUID prev = existing[i].spriteID;
                    if (!prev.Empty())
                        spriteId = prev;
                }

                string spriteName = baseName + "_" + i.ToString("D2");
                rects[i] = new SpriteRect
                {
                    name = spriteName,
                    spriteID = spriteId,
                    rect = new Rect(i * (cellW + Pad), 0f, cellW, cellH),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = SpriteAlignment.Center
                };
                namePairs.Add(new SpriteNameFileIdPair(spriteName, spriteId));
            }

            dataProvider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider nameIdProvider =
                dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameIdProvider != null)
                nameIdProvider.SetNameFileIdPairs(namePairs);

            dataProvider.Apply();
            return true;
        }

        private static RarityVisualLibrary EnsureLibrary(
            StringBuilder report, ref int changes, List<string> converged)
        {
            RarityVisualLibrary lib =
                AssetDatabase.LoadAssetAtPath<RarityVisualLibrary>(LibraryPath);
            if (lib != null)
            {
                report.AppendLine("- RarityVisualLibrary.asset déjà présent ✓");
                return lib;
            }

            string dir = Path.GetDirectoryName(LibraryPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                    AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "Config");
            }

            lib = ScriptableObject.CreateInstance<RarityVisualLibrary>();
            AssetDatabase.CreateAsset(lib, LibraryPath);
            changes++;
            converged.Add("Créé RarityVisualLibrary.asset");
            report.AppendLine("- RarityVisualLibrary.asset créé");
            return lib;
        }

        private static void ConvergeLibraryFrames(
            RarityVisualLibrary lib,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            SerializedObject so = new SerializedObject(lib);
            bool dirty = false;

            dirty |= AssignFramesIfMissing(
                so, "srVisuals", Path.Combine(RarityFolder, SheetSr).Replace('\\', '/'),
                report, converged);
            dirty |= AssignFramesIfMissing(
                so, "ssrVisuals", Path.Combine(RarityFolder, SheetSsr).Replace('\\', '/'),
                report, converged);
            dirty |= AssignFramesIfMissing(
                so, "lrVisuals", Path.Combine(RarityFolder, SheetLr).Replace('\\', '/'),
                report, converged);

            if (dirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(lib);
                AssetDatabase.SaveAssets();
                changes++;
            }
            else
            {
                report.AppendLine("- Librairie frames déjà à jour ✓");
            }
        }

        /// <summary>
        /// A2 : n'assigne que si frames null/vides OU sheet a plus de frames que l'asset.
        /// Remplacement sheet → reimport + re-run converge.
        /// </summary>
        private static bool AssignFramesIfMissing(
            SerializedObject so,
            string visualsProp,
            string sheetPath,
            StringBuilder report,
            List<string> converged)
        {
            Sprite[] sheetFrames = LoadSheetFrames(sheetPath);
            SerializedProperty visuals = so.FindProperty(visualsProp);
            if (visuals == null)
                return false;

            SerializedProperty framesProp = visuals.FindPropertyRelative("badgeFrames");
            int currentCount = framesProp != null ? framesProp.arraySize : 0;
            bool currentEmpty = currentCount == 0;
            for (int i = 0; i < currentCount; i++)
            {
                if (framesProp.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    currentEmpty = true;
                    break;
                }
            }

            if (sheetFrames == null || sheetFrames.Length == 0)
            {
                report.AppendLine($"- {visualsProp} : sheet absente/vide — frames inchangées");
                return false;
            }

            bool needsUpdate = currentEmpty || currentCount != sheetFrames.Length;
            if (!needsUpdate)
            {
                // Même count : vérifier identité des refs (remplacement sheet = nouveaux sprites).
                for (int i = 0; i < sheetFrames.Length; i++)
                {
                    Object cur = framesProp.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (cur != sheetFrames[i])
                    {
                        needsUpdate = true;
                        break;
                    }
                }
            }

            if (!needsUpdate)
                return false;

            framesProp.arraySize = sheetFrames.Length;
            for (int i = 0; i < sheetFrames.Length; i++)
                framesProp.GetArrayElementAtIndex(i).objectReferenceValue = sheetFrames[i];

            SerializedProperty idle = visuals.FindPropertyRelative("idleFrameIndex");
            if (idle != null && idle.intValue < 0)
                idle.intValue = 0;

            SerializedProperty fps = visuals.FindPropertyRelative("framesPerSecond");
            if (fps != null && fps.floatValue <= 0f)
                fps.floatValue = 10f;

            string msg = $"{visualsProp} ← {sheetFrames.Length} frame(s) depuis {Path.GetFileName(sheetPath)}";
            converged.Add(msg);
            report.AppendLine("- Convergé : " + msg);
            return true;
        }

        private static Sprite[] LoadSheetFrames(string path)
        {
            if (!File.Exists(path))
                return null;

            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            var list = new List<Sprite>(8);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is Sprite sp)
                    list.Add(sp);
            }

            if (list.Count == 0)
            {
                Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (single != null)
                    list.Add(single);
            }

            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.Count > 0 ? list.ToArray() : null;
        }

        // ═══════════════════════════════════════════
        // CARTE
        // ═══════════════════════════════════════════

        private static void WireCardPrefab(
            RarityVisualLibrary library,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            report.AppendLine();
            report.AppendLine("## CharacterCard.prefab");
            report.AppendLine();

            if (!File.Exists(CardPrefabPath))
            {
                report.AppendLine("- ✗ Prefab carte introuvable");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            try
            {
                Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                CharacterCardUI cardUi = root.GetComponent<CharacterCardUI>();
                if (cardUi == null)
                {
                    report.AppendLine("- ✗ CharacterCardUI manquant");
                    return;
                }

                Transform badgeTx = FindChildTrim(root.transform, "BadgeRarity");
                if (badgeTx == null)
                {
                    report.AppendLine("- ✗ GO BadgeRarity manquant (attendu sur prefab)");
                    return;
                }

                // A1 — purge GO BadgeText
                Transform textTx = FindChildTrim(badgeTx, "BadgeText");
                if (textTx != null)
                {
                    Object.DestroyImmediate(textTx.gameObject);
                    changes++;
                    converged.Add("Carte : purgé GO BadgeText");
                    report.AppendLine("- BadgeText purgé (A1)");
                }
                else
                {
                    report.AppendLine("- BadgeText déjà absent ✓");
                }

                Image badgeImg = badgeTx.GetComponent<Image>();
                if (badgeImg != null)
                {
                    if (badgeImg.raycastTarget)
                    {
                        badgeImg.raycastTarget = false;
                        changes++;
                        converged.Add("Carte : raycastTarget=false sur BadgeRarity");
                    }

                    badgeImg.preserveAspect = true;
                }

                RarityBadgeView view = badgeTx.GetComponent<RarityBadgeView>();
                if (view == null)
                {
                    view = badgeTx.gameObject.AddComponent<RarityBadgeView>();
                    changes++;
                    converged.Add("Carte : RarityBadgeView ajouté sur BadgeRarity");
                    report.AppendLine("- RarityBadgeView ajouté");
                }
                else
                {
                    report.AppendLine("- RarityBadgeView déjà présent ✓");
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

                if (playProp != null && !playProp.boolValue)
                {
                    playProp.boolValue = true;
                    viewDirty = true;
                }

                if (viewDirty)
                {
                    viewSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add("Carte : library + playAnimation=true");
                }

                // Placement Dokkan haut-gauche (defaults plan)
                RectTransform badgeRt = badgeTx as RectTransform;
                if (badgeRt != null)
                {
                    RectTransform cardRt = root.transform as RectTransform;
                    float cardW = cardRt != null && cardRt.sizeDelta.x > 1f
                        ? cardRt.sizeDelta.x
                        : 150f;
                    float size = cardW * CardBadgeWidthRatio;

                    bool placeDirty = false;
                    if (badgeRt.anchorMin != new Vector2(0f, 1f)
                        || badgeRt.anchorMax != new Vector2(0f, 1f)
                        || badgeRt.pivot != new Vector2(0f, 1f))
                    {
                        badgeRt.anchorMin = new Vector2(0f, 1f);
                        badgeRt.anchorMax = new Vector2(0f, 1f);
                        badgeRt.pivot = new Vector2(0f, 1f);
                        placeDirty = true;
                    }

                    Vector2 wantPos = new Vector2(-CardBadgeOverhang, CardBadgeOverhang);
                    Vector2 wantSize = new Vector2(size, size);
                    if (badgeRt.anchoredPosition != wantPos || badgeRt.sizeDelta != wantSize)
                    {
                        badgeRt.anchoredPosition = wantPos;
                        badgeRt.sizeDelta = wantSize;
                        placeDirty = true;
                    }

                    if (placeDirty)
                    {
                        changes++;
                        converged.Add("Carte : ancre haut-gauche Dokkan (~52 %, overhang 8)");
                        report.AppendLine("- Placement haut-gauche appliqué (gros)");
                    }
                    else
                    {
                        report.AppendLine("- Placement haut-gauche déjà OK ✓");
                    }
                }

                SerializedObject cardSo = new SerializedObject(cardUi);
                SerializedProperty rarityBadgeProp = cardSo.FindProperty("rarityBadge");
                if (rarityBadgeProp != null && rarityBadgeProp.objectReferenceValue != view)
                {
                    rarityBadgeProp.objectReferenceValue = view;
                    cardSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add("Carte : CharacterCardUI.rarityBadge câblé");
                    report.AppendLine("- rarityBadge câblé");
                }
                else
                {
                    report.AppendLine("- rarityBadge déjà câblé ✓");
                }

                PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ═══════════════════════════════════════════
        // POPUP
        // ═══════════════════════════════════════════

        private static void WirePopupPrefab(
            RarityVisualLibrary library,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            report.AppendLine();
            report.AppendLine("## CharacterDetailPopup.prefab");
            report.AppendLine();

            if (!File.Exists(PopupPrefabPath))
            {
                report.AppendLine("- ✗ Prefab popup introuvable");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PopupPrefabPath);
            try
            {
                Undo.RegisterCompleteObjectUndo(root, UndoLabel);
                CharacterDetailPopup popup = root.GetComponent<CharacterDetailPopup>();
                if (popup == null)
                {
                    report.AppendLine("- ✗ CharacterDetailPopup manquant");
                    return;
                }

                // Parent = Artwork (scopé — évite collision showcase Hub RarityBadge)
                Transform artworkTx = FindDeepTrim(root.transform, "Artwork");
                if (artworkTx == null)
                {
                    report.AppendLine("- ✗ Artwork introuvable sous popup");
                    return;
                }

                Transform badgeTx = FindChildTrim(artworkTx, "RarityBadge");
                if (badgeTx == null)
                {
                    GameObject go = new GameObject(
                        "RarityBadge",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    go.transform.SetParent(artworkTx, false);
                    badgeTx = go.transform;
                    changes++;
                    converged.Add("Popup : GO RarityBadge créé sous Artwork");
                    report.AppendLine("- RarityBadge créé sous Artwork");
                }
                else
                {
                    report.AppendLine("- RarityBadge déjà présent sous Artwork ✓");
                }

                Image img = badgeTx.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = false;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }

                RectTransform rt = badgeTx as RectTransform;
                if (rt != null)
                {
                    bool placeDirty = false;
                    if (rt.anchorMin != new Vector2(0f, 1f)
                        || rt.anchorMax != new Vector2(0f, 1f)
                        || rt.pivot != new Vector2(0.5f, 0.5f))
                    {
                        rt.anchorMin = new Vector2(0f, 1f);
                        rt.anchorMax = new Vector2(0f, 1f);
                        // Pivot centre pour que la rotation Z incliner proprement.
                        rt.pivot = new Vector2(0.5f, 0.5f);
                        placeDirty = true;
                    }

                    // Position = coin + demi-taille (pivot centre).
                    Vector2 wantPos = new Vector2(
                        8f + PopupBadgeSize * 0.5f,
                        -8f - PopupBadgeSize * 0.5f);
                    Vector2 wantSize = new Vector2(PopupBadgeSize, PopupBadgeSize);
                    Vector3 wantEuler = new Vector3(0f, 0f, PopupBadgeTiltZ);
                    if (rt.anchoredPosition != wantPos || rt.sizeDelta != wantSize)
                    {
                        rt.anchoredPosition = wantPos;
                        rt.sizeDelta = wantSize;
                        placeDirty = true;
                    }

                    float z = rt.localEulerAngles.z;
                    if (z > 180f)
                        z -= 360f;
                    if (!Mathf.Approximately(z, PopupBadgeTiltZ))
                    {
                        rt.localEulerAngles = wantEuler;
                        placeDirty = true;
                    }

                    // Badge au premier plan sous Artwork (au-dessus du portrait).
                    badgeTx.SetAsLastSibling();

                    if (placeDirty)
                    {
                        changes++;
                        converged.Add(
                            $"Popup : badge {PopupBadgeSize}px tilt {PopupBadgeTiltZ}°");
                        report.AppendLine(
                            $"- Placement badge {PopupBadgeSize}px, tilt {PopupBadgeTiltZ}°");
                    }
                    else
                    {
                        report.AppendLine("- Placement badge popup déjà OK ✓");
                    }
                }

                StylePopupBackButton(root.transform, report, ref changes, converged);
                RelayoutPopupHeaderChrome(root.transform, report, ref changes, converged);
                MoveNameToStatsPanelTitle(root, popup, report, ref changes, converged);

                RarityBadgeView view = badgeTx.GetComponent<RarityBadgeView>();
                if (view == null)
                {
                    view = badgeTx.gameObject.AddComponent<RarityBadgeView>();
                    changes++;
                    converged.Add("Popup : RarityBadgeView ajouté");
                    report.AppendLine("- RarityBadgeView ajouté");
                }
                else
                {
                    report.AppendLine("- RarityBadgeView déjà présent ✓");
                }

                SerializedObject viewSo = new SerializedObject(view);
                bool viewDirty = false;
                SerializedProperty libProp = viewSo.FindProperty("library");
                SerializedProperty playProp = viewSo.FindProperty("playAnimation");
                if (libProp != null && libProp.objectReferenceValue != library)
                {
                    libProp.objectReferenceValue = library;
                    viewDirty = true;
                }

                if (playProp != null && !playProp.boolValue)
                {
                    playProp.boolValue = true;
                    viewDirty = true;
                }

                if (viewDirty)
                {
                    viewSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add("Popup : library + playAnimation=true");
                }

                SerializedObject popupSo = new SerializedObject(popup);
                SerializedProperty rarityBadgeProp = popupSo.FindProperty("rarityBadge");
                if (rarityBadgeProp != null && rarityBadgeProp.objectReferenceValue != view)
                {
                    rarityBadgeProp.objectReferenceValue = view;
                    popupSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add("Popup : CharacterDetailPopup.rarityBadge câblé");
                    report.AppendLine("- rarityBadge câblé");
                }
                else
                {
                    report.AppendLine("- rarityBadge déjà câblé ✓");
                }

                PrefabUtility.SaveAsPrefabAsset(root, PopupPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ═══════════════════════════════════════════
        // POPUP — BACK (bas, flèche seule) + HEADER
        // ═══════════════════════════════════════════

        private static void StylePopupBackButton(
            Transform popupRoot,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            Transform backTx = FindDeepTrim(popupRoot, "BackButton");
            if (backTx == null)
            {
                report.AppendLine("- ✗ BackButton introuvable");
                return;
            }

            bool dirty = false;

            // Parent = racine popup (pas Artwork) : au-dessus du StatsPanel en sibling order.
            if (backTx.parent != popupRoot)
            {
                backTx.SetParent(popupRoot, false);
                dirty = true;
            }

            Transform statsPanel = FindDeepTrim(popupRoot, "StatsPanel");
            float panelH = PanelClosedHeightTarget;
            if (statsPanel is RectTransform statsRt && statsRt.sizeDelta.y > 1f)
                panelH = statsRt.sizeDelta.y;

            RectTransform brt = backTx as RectTransform;
            if (brt != null)
            {
                Vector2 aMin = Vector2.zero;
                Vector2 aMax = Vector2.zero;
                Vector2 pivot = Vector2.zero;
                Vector2 pos = new Vector2(BackMargin, panelH + 12f);
                Vector2 size = new Vector2(BackButtonSize, BackButtonSize);
                if (brt.anchorMin != aMin || brt.anchorMax != aMax || brt.pivot != pivot
                    || brt.anchoredPosition != pos || brt.sizeDelta != size)
                {
                    brt.anchorMin = aMin;
                    brt.anchorMax = aMax;
                    brt.pivot = pivot;
                    brt.anchoredPosition = pos;
                    brt.sizeDelta = size;
                    dirty = true;
                }

                brt.localEulerAngles = Vector3.zero;
            }

            Image rootImg = backTx.GetComponent<Image>();
            if (rootImg != null)
            {
                Color c = rootImg.color;
                if (c.a > 0.01f)
                {
                    c.a = 0f;
                    rootImg.color = c;
                    dirty = true;
                }

                rootImg.enabled = true;
                rootImg.raycastTarget = true;
            }

            Transform fill = FindChildTrim(backTx, "Fill");
            if (fill != null && fill.gameObject.activeSelf)
            {
                fill.gameObject.SetActive(false);
                dirty = true;
            }

            Transform iconTx = FindChildTrim(backTx, "Icon");
            if (iconTx != null)
            {
                RectTransform irt = iconTx as RectTransform;
                if (irt != null)
                {
                    Vector2 iconSize = new Vector2(BackIconSize, BackIconSize);
                    if (irt.sizeDelta != iconSize)
                    {
                        irt.anchorMin = new Vector2(0.5f, 0.5f);
                        irt.anchorMax = new Vector2(0.5f, 0.5f);
                        irt.pivot = new Vector2(0.5f, 0.5f);
                        irt.anchoredPosition = Vector2.zero;
                        irt.sizeDelta = iconSize;
                        dirty = true;
                    }
                }

                Image iconImg = iconTx.GetComponent<Image>();
                if (iconImg != null)
                {
                    iconImg.raycastTarget = false;
                    iconImg.preserveAspect = true;
                }
            }

            Button btn = backTx.GetComponent<Button>();
            if (btn != null && rootImg != null && btn.targetGraphic != rootImg)
            {
                btn.targetGraphic = rootImg;
                dirty = true;
            }

            int last = popupRoot.childCount - 1;
            if (backTx.GetSiblingIndex() != last)
            {
                backTx.SetAsLastSibling();
                dirty = true;
            }

            if (dirty)
            {
                changes++;
                converged.Add("Popup : Back au-dessus StatsPanel, flèche seule, cliquable");
                report.AppendLine("- Back au-dessus du panneau stats (raycast OK) ✓");
            }
            else
            {
                report.AppendLine("- Back déjà OK ✓");
            }
        }

        private static void RelayoutPopupHeaderChrome(
            Transform popupRoot,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            Transform inTeam = FindDeepTrim(popupRoot, "InTeamBadge");
            if (inTeam != null && inTeam.gameObject.activeSelf)
            {
                inTeam.gameObject.SetActive(false);
                changes++;
                converged.Add("Popup : InTeamBadge désactivé");
                report.AppendLine("- InTeamBadge retiré (OK En équipe) ✓");
            }
            else
            {
                report.AppendLine("- InTeamBadge déjà off ✓");
            }
        }

        private static void MoveNameToStatsPanelTitle(
            GameObject popupRoot,
            CharacterDetailPopup popup,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            Transform statsPanel = FindDeepTrim(popupRoot.transform, "StatsPanel");
            Transform nameTx = FindDeepTrim(popupRoot.transform, "NameText");
            if (statsPanel == null || nameTx == null)
            {
                report.AppendLine("- ✗ StatsPanel ou NameText introuvable");
                return;
            }

            bool dirty = false;
            if (nameTx.parent != statsPanel)
            {
                nameTx.SetParent(statsPanel, false);
                dirty = true;
            }

            // Juste sous PanelTopBorder.
            Transform border = FindChildTrim(statsPanel, "PanelTopBorder");
            int insertAt = border != null ? border.GetSiblingIndex() + 1 : 0;
            if (nameTx.GetSiblingIndex() != insertAt)
            {
                nameTx.SetSiblingIndex(insertAt);
                dirty = true;
            }

            RectTransform nrt = nameTx as RectTransform;
            if (nrt != null)
            {
                Vector2 aMin = new Vector2(0f, 1f);
                Vector2 aMax = new Vector2(1f, 1f);
                Vector2 pivot = new Vector2(0.5f, 1f);
                Vector2 pos = new Vector2(0f, -8f);
                Vector2 size = new Vector2(-(UiTheme.PadCard * 2f), PanelTitleHeight);
                if (nrt.anchorMin != aMin || nrt.anchorMax != aMax
                    || nrt.anchoredPosition != pos
                    || !Mathf.Approximately(nrt.sizeDelta.y, PanelTitleHeight))
                {
                    nrt.anchorMin = aMin;
                    nrt.anchorMax = aMax;
                    nrt.pivot = pivot;
                    nrt.anchoredPosition = pos;
                    nrt.sizeDelta = size;
                    dirty = true;
                }
            }

            TextMeshProUGUI tmp = nameTx.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                if (!Mathf.Approximately(tmp.fontSize, UiTypography.Title)
                    || tmp.fontStyle != FontStyles.Bold
                    || tmp.alignment != TextAlignmentOptions.MidlineLeft)
                {
                    tmp.fontSize = UiTypography.Title;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary;
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;
                    tmp.raycastTarget = false;
                    dirty = true;
                }
            }

            // Décale TabBar / StatsRow sous le titre (évite chevauchement).
            float tabY = -8f - PanelTitleHeight - 4f;
            Transform tabBar = FindChildTrim(statsPanel, "TabBar");
            if (tabBar is RectTransform tabRt)
            {
                if (!Mathf.Approximately(tabRt.anchoredPosition.y, tabY))
                {
                    tabRt.anchoredPosition = new Vector2(tabRt.anchoredPosition.x, tabY);
                    dirty = true;
                }

                float statsY = tabY - tabRt.sizeDelta.y - 4f;
                Transform statsRow = FindChildTrim(statsPanel, "StatsRow");
                if (statsRow is RectTransform rowRt
                    && !Mathf.Approximately(rowRt.anchoredPosition.y, statsY))
                {
                    rowRt.anchoredPosition = new Vector2(rowRt.anchoredPosition.x, statsY);
                    dirty = true;
                }
            }

            // Expand au-dessus du titre (chevron en tête d'encart).
            Transform expand = FindChildTrim(statsPanel, "ExpandButton");
            if (expand != null && expand.GetSiblingIndex() > insertAt)
            {
                expand.SetSiblingIndex(insertAt);
                dirty = true;
            }

            RectTransform statsRt = statsPanel as RectTransform;
            if (statsRt != null
                && !Mathf.Approximately(statsRt.sizeDelta.y, PanelClosedHeightTarget))
            {
                statsRt.sizeDelta = new Vector2(statsRt.sizeDelta.x, PanelClosedHeightTarget);
                dirty = true;
            }

            SerializedObject popupSo = new SerializedObject(popup);
            SerializedProperty closedH = popupSo.FindProperty("panelClosedHeight");
            if (closedH != null
                && !Mathf.Approximately(closedH.floatValue, PanelClosedHeightTarget))
            {
                closedH.floatValue = PanelClosedHeightTarget;
                popupSo.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            SerializedProperty nameProp = popupSo.FindProperty("nameText");
            if (nameProp != null && nameProp.objectReferenceValue != tmp)
            {
                nameProp.objectReferenceValue = tmp;
                popupSo.ApplyModifiedPropertiesWithoutUndo();
                dirty = true;
            }

            if (dirty)
            {
                changes++;
                converged.Add("Popup : nom = titre encart stats");
                report.AppendLine("- NameText → titre StatsPanel ✓");
            }
            else
            {
                report.AppendLine("- Titre StatsPanel déjà OK ✓");
            }
        }

        // ═══════════════════════════════════════════
        // SLOTS ÉQUIPE (slice BR2 anticipée)
        // ═══════════════════════════════════════════

        private static void WireTeamSlots(
            RarityVisualLibrary library,
            StringBuilder report,
            ref int changes,
            List<string> converged)
        {
            report.AppendLine();
            report.AppendLine("## TeamSlotUI (scène Hub)");
            report.AppendLine();

            TeamSlotUI[] slots = Object.FindObjectsOfType<TeamSlotUI>(true);
            if (slots == null || slots.Length == 0)
            {
                report.AppendLine(
                    "- ⚠ Aucun TeamSlotUI en scènes chargées — ouvrir Hub puis re-run");
                return;
            }

            int wired = 0;
            for (int s = 0; s < slots.Length; s++)
            {
                TeamSlotUI slot = slots[s];
                if (slot == null)
                    continue;

                Undo.RegisterCompleteObjectUndo(slot.gameObject, UndoLabel);
                Transform slotTx = slot.transform;

                // Cherche sous Inner (emplacement réel) — sinon re-création en boucle.
                Transform inner = FindChildTrim(slotTx, "Inner");
                Transform searchRoot = inner != null ? inner : slotTx;
                Transform badgeTx = FindChildTrim(searchRoot, "RarityBadge");
                if (badgeTx == null)
                    badgeTx = FindDeepTrim(slotTx, "RarityBadge");

                // Purge doublons d'anciens runs non idempotents.
                PurgeDuplicateNamed(slotTx, "RarityBadge", badgeTx, ref changes, converged, slot.name);

                if (badgeTx == null)
                {
                    Transform parent = inner != null ? inner : slotTx;
                    GameObject go = new GameObject(
                        "RarityBadge",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));
                    Undo.RegisterCreatedObjectUndo(go, UndoLabel);
                    go.transform.SetParent(parent, false);
                    badgeTx = go.transform;
                    changes++;
                    converged.Add($"Slot {slot.name} : RarityBadge créé");
                }

                Image badgeImg = badgeTx.GetComponent<Image>();
                if (badgeImg != null)
                {
                    badgeImg.raycastTarget = false;
                    badgeImg.preserveAspect = true;
                    badgeImg.color = Color.white;
                }

                RectTransform badgeRt = badgeTx as RectTransform;
                RectTransform parentRt = badgeTx.parent as RectTransform;
                float parentW = 100f;
                if (parentRt != null && parentRt.rect.width > 1f)
                    parentW = parentRt.rect.width;
                float size = parentW * SlotBadgeRatio;
                if (size < 28f)
                    size = 28f;

                if (badgeRt != null)
                {
                    bool placeDirty = false;
                    if (badgeRt.anchorMin != new Vector2(0f, 1f)
                        || badgeRt.anchorMax != new Vector2(0f, 1f)
                        || badgeRt.pivot != new Vector2(0f, 1f))
                    {
                        badgeRt.anchorMin = new Vector2(0f, 1f);
                        badgeRt.anchorMax = new Vector2(0f, 1f);
                        badgeRt.pivot = new Vector2(0f, 1f);
                        placeDirty = true;
                    }

                    Vector2 wantPos = new Vector2(-SlotBadgeOverhang, SlotBadgeOverhang);
                    Vector2 wantSize = new Vector2(size, size);
                    if (badgeRt.anchoredPosition != wantPos || badgeRt.sizeDelta != wantSize)
                    {
                        badgeRt.anchoredPosition = wantPos;
                        badgeRt.sizeDelta = wantSize;
                        placeDirty = true;
                    }

                    if (placeDirty)
                    {
                        changes++;
                        converged.Add($"Slot {slot.name} : placement badge");
                    }
                }

                badgeTx.SetAsLastSibling();

                RarityBadgeView view = badgeTx.GetComponent<RarityBadgeView>();
                if (view == null)
                {
                    view = Undo.AddComponent<RarityBadgeView>(badgeTx.gameObject);
                    changes++;
                    converged.Add($"Slot {slot.name} : RarityBadgeView");
                }

                SerializedObject viewSo = new SerializedObject(view);
                bool viewDirty = false;
                SerializedProperty libProp = viewSo.FindProperty("library");
                SerializedProperty playProp = viewSo.FindProperty("playAnimation");
                if (libProp != null && libProp.objectReferenceValue != library)
                {
                    libProp.objectReferenceValue = library;
                    viewDirty = true;
                }

                if (playProp != null && playProp.boolValue)
                {
                    // Slots : restent idle (perf dock + petite taille).
                    playProp.boolValue = false;
                    viewDirty = true;
                }

                if (viewDirty)
                {
                    viewSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                }

                SerializedObject slotSo = new SerializedObject(slot);
                SerializedProperty rarityProp = slotSo.FindProperty("rarityBadge");
                if (rarityProp != null && rarityProp.objectReferenceValue != view)
                {
                    rarityProp.objectReferenceValue = view;
                    slotSo.ApplyModifiedPropertiesWithoutUndo();
                    changes++;
                    converged.Add($"Slot {slot.name} : rarityBadge câblé");
                }

                // Masquer si slot vide au moment du wiring.
                if (slot.IsEmpty)
                    badgeTx.gameObject.SetActive(false);

                EditorUtility.SetDirty(slot);
                EditorSceneManager.MarkSceneDirty(slot.gameObject.scene);
                wired++;
            }

            report.AppendLine($"- TeamSlotUI traités : {wired}");
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void PurgeDuplicateNamed(
            Transform root,
            string name,
            Transform keep,
            ref int changes,
            List<string> converged,
            string slotName)
        {
            if (root == null)
                return;

            var doomed = new List<Transform>(4);
            CollectNamed(root, name, doomed);
            for (int i = 0; i < doomed.Count; i++)
            {
                if (doomed[i] == null || doomed[i] == keep)
                    continue;
                Object.DestroyImmediate(doomed[i].gameObject);
                changes++;
                converged.Add($"Slot {slotName} : doublon {name} purgé");
            }
        }

        private static void CollectNamed(Transform root, string name, List<Transform> into)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform c = root.GetChild(i);
                if (c.name != null && c.name.Trim() == name)
                    into.Add(c);
                CollectNamed(c, name, into);
            }
        }

        private static Transform FindChildTrim(Transform parent, string name)
        {
            if (parent == null)
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name != null && c.name.Trim() == name)
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
    }
}
#endif
