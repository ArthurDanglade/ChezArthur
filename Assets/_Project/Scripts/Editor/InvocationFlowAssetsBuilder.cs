#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.UI.InvocationFlow;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Génère material, config (si absente) et prefabs Invocation Flow INV1 (idempotent).
    /// </summary>
    public static class InvocationFlowAssetsBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string ArtFxFolder = "Assets/_Project/Art/FX";
        private const string DataUiFolder = "Assets/_Project/Data/UI";
        private const string PrefabUiFolder = "Assets/_Project/Prefabs/UI";

        private const string NoisePath = ArtFxFolder + "/ArtworkNoise.png";
        private const string GlowPath = ArtFxFolder + "/AwGlowSoft.png";
        private const string AdditiveMatPath = ArtFxFolder + "/AwAdditive.mat";
        private const string PixelVeilMatPath = ArtFxFolder + "/PixelVeil.mat";
        private const string ConfigPath = DataUiFolder + "/InvocationFlowConfig.asset";
        private const string VeilPrefabPath = PrefabUiFolder + "/PixelVeilOverlay.prefab";
        private const string RarityPrefabPath = PrefabUiFolder + "/RevealRarityLayer.prefab";
        private const string BannerPrefabPath = PrefabUiFolder + "/RevealBanner.prefab";
        private const string ReportRelPath = "Audits/invocation_flow_build.txt";

        private const string PixelVeilShaderName = "ChezArthur/UI/PixelVeil";
        private const string AdditiveShaderName = "ChezArthur/UI/AdditiveTint";

        private static readonly Color FlashWarm = new Color(1f, 0.973f, 0.918f, 0f);
        private static readonly Color BannerBg = new Color(0.06f, 0.07f, 0.10f, 0.92f);

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Construire assets Invocation Flow (INV1)")]
        public static void BuildMenu()
        {
            Build();
        }

        /// <summary>Point d'entrée idempotent (MenuItem + batchmode).</summary>
        public static void Build()
        {
            var report = new StringBuilder(4096);
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" BUILD Invocation Flow INV1");
            report.AppendLine($" Date : {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine();
            report.AppendLine("NOTE : ArtworkNoise / AwGlowSoft / AwAdditive réutilisés (pas régénérés).");
            report.AppendLine("NOTE : socle AW intact — aucune modification ArtworkTransition.");
            report.AppendLine();

            EnsureFolder(ArtFxFolder);
            EnsureFolder(DataUiFolder);
            EnsureFolder(PrefabUiFolder);

            InvocationFlowConfig config = EnsureConfig(report);

            Texture2D noiseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GlowPath);
            Material additiveMat = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMatPath);

            if (noiseTex == null)
                report.AppendLine($"WARN : noise manquant → {NoisePath}");
            else
                report.AppendLine($"REUSE : {NoisePath} (seed 1337 AW, non régénéré)");

            if (glowTex == null)
                report.AppendLine($"WARN : glow manquant → {GlowPath}");
            else
                report.AppendLine($"REUSE : {GlowPath}");

            if (additiveMat == null)
            {
                report.AppendLine($"WARN : AwAdditive.mat manquant → création locale AdditiveTint");
                additiveMat = EnsureMaterial(AdditiveMatPath, AdditiveShaderName, report, "AwAdditive.mat");
            }
            else
            {
                report.AppendLine($"REUSE : {AdditiveMatPath}");
            }

            Material veilMat = EnsureMaterial(PixelVeilMatPath, PixelVeilShaderName, report, "PixelVeil.mat");
            if (veilMat != null && noiseTex != null)
            {
                veilMat.SetTexture("_NoiseTex", noiseTex);
                veilMat.SetFloat("_Progress", 0f);
                veilMat.SetFloat("_GlobalAlpha", 1f);
                veilMat.SetVector("_Cells", new Vector4(26f, 46f, 0f, 0f));
                EditorUtility.SetDirty(veilMat);
            }

            BuildVeilPrefab(veilMat, config, report);
            BuildRarityPrefab(additiveMat, glowTex, config, report);
            BuildBannerPrefab(config, report);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report.AppendLine();
            report.AppendLine("═══════════════════════════════════════════");
            report.AppendLine(" Build terminé (idempotent). Relance = zéro changement attendu.");
            report.AppendLine("═══════════════════════════════════════════");

            WriteReport(report);
            Debug.Log($"[InvocationFlowAssetsBuilder] OK — rapport : {ReportRelPath}");
            if (veilMat != null)
                EditorGUIUtility.PingObject(veilMat);
        }

        // ═══════════════════════════════════════════
        // CONFIG
        // ═══════════════════════════════════════════

        private static InvocationFlowConfig EnsureConfig(StringBuilder report)
        {
            InvocationFlowConfig existing =
                AssetDatabase.LoadAssetAtPath<InvocationFlowConfig>(ConfigPath);
            if (existing != null)
            {
                report.AppendLine($"CONFIG : conservée (non écrasée) → {ConfigPath}");
                return existing;
            }

            InvocationFlowConfig created = ScriptableObject.CreateInstance<InvocationFlowConfig>();
            AssetDatabase.CreateAsset(created, ConfigPath);
            report.AppendLine($"CONFIG : créée avec défauts CreateInstance → {ConfigPath}");
            return created;
        }

        // ═══════════════════════════════════════════
        // MATÉRIAUX
        // ═══════════════════════════════════════════

        private static Material EnsureMaterial(
            string path, string shaderName, StringBuilder report, string label)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                report.AppendLine($"MATÉRIAU : ÉCHEC shader introuvable « {shaderName} » ({label})");
                Debug.LogError($"[InvocationFlowAssetsBuilder] Shader introuvable : {shaderName}");
                return null;
            }

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
                report.AppendLine($"MATÉRIAU : créé → {path}");
            }
            else
            {
                mat.shader = shader;
                report.AppendLine($"MATÉRIAU : mis à jour → {path}");
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ═══════════════════════════════════════════
        // PREFAB — VOILE
        // ═══════════════════════════════════════════

        private static void BuildVeilPrefab(
            Material veilMat, InvocationFlowConfig config, StringBuilder report)
        {
            GameObject root = new GameObject("PixelVeilOverlay", typeof(RectTransform));
            RectTransform rt = root.GetComponent<RectTransform>();
            StretchFull(rt);

            Image img = root.AddComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            if (veilMat != null)
                img.material = veilMat;

            PixelVeilController ctrl = root.AddComponent<PixelVeilController>();
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("sharedMaterial").objectReferenceValue = veilMat;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, VeilPrefabPath);
            Object.DestroyImmediate(root);
            report.AppendLine($"PREFAB : écrit → {VeilPrefabPath} (inactif, raycast off)");
        }

        // ═══════════════════════════════════════════
        // PREFAB — RARETÉ
        // ═══════════════════════════════════════════

        private static void BuildRarityPrefab(
            Material additiveMat,
            Texture2D glowTex,
            InvocationFlowConfig config,
            StringBuilder report)
        {
            GameObject root = new GameObject("RevealRarityLayer", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            StretchFull(rootRt);

            // Conteneur shake
            RectTransform shaker = CreateChild(rootRt, "ShakeContainer");
            StretchFull(shaker);

            // Underglow — centré à 45 % de hauteur
            RectTransform glowRt = CreateChild(shaker, "Underglow");
            glowRt.anchorMin = new Vector2(0.5f, 0.45f);
            glowRt.anchorMax = new Vector2(0.5f, 0.45f);
            glowRt.pivot = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(420f, 280f);
            glowRt.anchoredPosition = Vector2.zero;
            RawImage underglow = glowRt.gameObject.AddComponent<RawImage>();
            underglow.texture = glowTex;
            underglow.color = new Color(1f, 1f, 1f, 0f);
            underglow.raycastTarget = false;
            if (additiveMat != null)
                underglow.material = additiveMat;

            // Rim 5 px (4 bords)
            RectTransform rimRt = CreateChild(shaker, "RimFrame");
            StretchFull(rimRt);
            Image rimParent = rimRt.gameObject.AddComponent<Image>();
            rimParent.color = new Color(1f, 1f, 1f, 0f);
            rimParent.raycastTarget = false;
            if (additiveMat != null)
                rimParent.material = additiveMat;
            CreateRimEdge(rimRt, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 5f), additiveMat);
            CreateRimEdge(rimRt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 5f), additiveMat);
            CreateRimEdge(rimRt, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(5f, 0f), additiveMat);
            CreateRimEdge(rimRt, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(5f, 0f), additiveMat);

            // Particules (composant AW réutilisé)
            RectTransform partRt = CreateChild(shaker, "Particles");
            StretchFull(partRt);
            PixelParticleGraphic particles = partRt.gameObject.AddComponent<PixelParticleGraphic>();
            particles.raycastTarget = false;
            if (additiveMat != null)
                particles.material = additiveMat;

            // Flash (hors shake)
            Image flash = CreateImageChild(rootRt, "FlashOverlay", null, FlashWarm);
            StretchFull(flash.rectTransform);

            RevealRarityLayer layer = root.AddComponent<RevealRarityLayer>();
            SerializedObject so = new SerializedObject(layer);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("underglowImage").objectReferenceValue = underglow;
            so.FindProperty("rimFrame").objectReferenceValue = rimParent;
            so.FindProperty("particles").objectReferenceValue = particles;
            so.FindProperty("shakeContainer").objectReferenceValue = shaker;
            so.FindProperty("flashOverlay").objectReferenceValue = flash;
            so.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject partSo = new SerializedObject(particles);
            partSo.FindProperty("glowTexture").objectReferenceValue = glowTex;
            partSo.FindProperty("m_RaycastTarget").boolValue = false;
            if (additiveMat != null)
                partSo.FindProperty("m_Material").objectReferenceValue = additiveMat;
            partSo.ApplyModifiedPropertiesWithoutUndo();

            DisableRaycastsRecursive(root);

            PrefabUtility.SaveAsPrefabAsset(root, RarityPrefabPath);
            Object.DestroyImmediate(root);
            report.AppendLine($"PREFAB : écrit → {RarityPrefabPath} (PixelParticleGraphic AW, raycast off)");
        }

        // ═══════════════════════════════════════════
        // PREFAB — BANDEAU
        // ═══════════════════════════════════════════

        private static void BuildBannerPrefab(InvocationFlowConfig config, StringBuilder report)
        {
            GameObject root = new GameObject("RevealBanner", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 0f);
            rootRt.pivot = new Vector2(0.5f, 0f);
            rootRt.sizeDelta = new Vector2(0f, 118f);
            rootRt.anchoredPosition = Vector2.zero;

            CanvasGroup cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;

            // Fond gradient approximé (Image sombre)
            Image bg = root.AddComponent<Image>();
            bg.color = BannerBg;
            bg.raycastTarget = false;

            // Nom (gauche)
            TextMeshProUGUI nameTmp = CreateTmp(rootRt, "Name", 28f, TextAlignmentOptions.Left);
            RectTransform nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = new Vector2(0f, 0.55f);
            nameRt.anchorMax = new Vector2(0.55f, 0.95f);
            nameRt.offsetMin = new Vector2(16f, 0f);
            nameRt.offsetMax = new Vector2(-8f, -4f);

            // Barre rareté 3 px sous le nom
            RectTransform barRt = CreateChild(rootRt, "RarityBar");
            barRt.anchorMin = new Vector2(0f, 0.48f);
            barRt.anchorMax = new Vector2(0f, 0.48f);
            barRt.pivot = new Vector2(0f, 0.5f);
            barRt.anchoredPosition = new Vector2(16f, 0f);
            barRt.sizeDelta = new Vector2(0f, 3f);
            Image rarityBar = barRt.gameObject.AddComponent<Image>();
            rarityBar.color = Color.white;
            rarityBar.raycastTarget = false;

            // Chip niveau (droite)
            TextMeshProUGUI levelTmp = CreateTmp(rootRt, "LevelChip", 20f, TextAlignmentOptions.Right);
            RectTransform levelRt = levelTmp.rectTransform;
            levelRt.anchorMin = new Vector2(0.55f, 0.55f);
            levelRt.anchorMax = new Vector2(1f, 0.95f);
            levelRt.offsetMin = new Vector2(8f, 0f);
            levelRt.offsetMax = new Vector2(-16f, -4f);

            // Statut
            TextMeshProUGUI statusTmp = CreateTmp(rootRt, "Status", 16f, TextAlignmentOptions.Left);
            RectTransform statusRt = statusTmp.rectTransform;
            statusRt.anchorMin = new Vector2(0f, 0.30f);
            statusRt.anchorMax = new Vector2(0.4f, 0.50f);
            statusRt.offsetMin = new Vector2(16f, 0f);
            statusRt.offsetMax = new Vector2(0f, 0f);

            // Chips stats
            var chipGroups = new CanvasGroup[4];
            var chipLabels = new TextMeshProUGUI[4];
            var chipRects = new RectTransform[4];
            string[] labels = { "HP 0", "ATK 0", "DEF 0", "SPD 0" };
            for (int i = 0; i < 4; i++)
            {
                RectTransform chipRt = CreateChild(rootRt, "StatChip_" + i);
                chipRt.anchorMin = new Vector2(0f, 0.12f);
                chipRt.anchorMax = new Vector2(0f, 0.12f);
                chipRt.pivot = new Vector2(0f, 0.5f);
                chipRt.anchoredPosition = new Vector2(16f + i * 88f, 0f);
                chipRt.sizeDelta = new Vector2(80f, 22f);

                CanvasGroup chipCg = chipRt.gameObject.AddComponent<CanvasGroup>();
                chipCg.alpha = 0f;
                Image chipBg = chipRt.gameObject.AddComponent<Image>();
                chipBg.color = new Color(1f, 1f, 1f, 0.08f);
                chipBg.raycastTarget = false;

                TextMeshProUGUI chipLabel = CreateTmp(chipRt, "Label", 14f, TextAlignmentOptions.Center);
                StretchFull(chipLabel.rectTransform);
                chipLabel.text = labels[i];

                chipGroups[i] = chipCg;
                chipLabels[i] = chipLabel;
                chipRects[i] = chipRt;
            }

            // Ligne XP au ras du bord bas
            RectTransform xpTrack = CreateChild(rootRt, "XpTrack");
            xpTrack.anchorMin = new Vector2(0f, 0f);
            xpTrack.anchorMax = new Vector2(1f, 0f);
            xpTrack.pivot = new Vector2(0f, 0f);
            xpTrack.anchoredPosition = Vector2.zero;
            xpTrack.sizeDelta = new Vector2(0f, 3f);
            Image xpTrackImg = xpTrack.gameObject.AddComponent<Image>();
            xpTrackImg.color = new Color(1f, 1f, 1f, 0.12f);
            xpTrackImg.raycastTarget = false;

            RectTransform xpFill = CreateChild(xpTrack, "XpFill");
            xpFill.anchorMin = new Vector2(0f, 0f);
            xpFill.anchorMax = new Vector2(0f, 1f);
            xpFill.pivot = new Vector2(0f, 0.5f);
            xpFill.anchoredPosition = Vector2.zero;
            xpFill.sizeDelta = new Vector2(0f, 0f);
            Image xpFillImg = xpFill.gameObject.AddComponent<Image>();
            xpFillImg.color = new Color(0.95f, 0.82f, 0.40f, 0.95f);
            xpFillImg.raycastTarget = false;

            // Chip XP éphémère
            TextMeshProUGUI xpChip = CreateTmp(rootRt, "XpChip", 14f, TextAlignmentOptions.Center);
            RectTransform xpChipRt = xpChip.rectTransform;
            xpChipRt.anchorMin = new Vector2(1f, 0f);
            xpChipRt.anchorMax = new Vector2(1f, 0f);
            xpChipRt.pivot = new Vector2(1f, 0f);
            xpChipRt.anchoredPosition = new Vector2(-12f, 8f);
            xpChipRt.sizeDelta = new Vector2(72f, 20f);
            xpChip.text = "+XP";
            SetTmpAlpha(xpChip, 0f);

            AudioSource audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            RevealBannerUI banner = root.AddComponent<RevealBannerUI>();
            SerializedObject so = new SerializedObject(banner);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("canvasGroup").objectReferenceValue = cg;
            so.FindProperty("rootRect").objectReferenceValue = rootRt;
            so.FindProperty("nameText").objectReferenceValue = nameTmp;
            so.FindProperty("rarityBar").objectReferenceValue = rarityBar;
            so.FindProperty("levelChip").objectReferenceValue = levelTmp;
            so.FindProperty("statusText").objectReferenceValue = statusTmp;

            SerializedProperty groupsProp = so.FindProperty("statChipGroups");
            groupsProp.arraySize = 4;
            SerializedProperty labelsProp = so.FindProperty("statChipLabels");
            labelsProp.arraySize = 4;
            SerializedProperty rectsProp = so.FindProperty("statChipRects");
            rectsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                groupsProp.GetArrayElementAtIndex(i).objectReferenceValue = chipGroups[i];
                labelsProp.GetArrayElementAtIndex(i).objectReferenceValue = chipLabels[i];
                rectsProp.GetArrayElementAtIndex(i).objectReferenceValue = chipRects[i];
            }

            so.FindProperty("xpLineFill").objectReferenceValue = xpFill;
            so.FindProperty("xpChip").objectReferenceValue = xpChip;
            so.FindProperty("oneshotSource").objectReferenceValue = audio;
            so.ApplyModifiedPropertiesWithoutUndo();

            DisableRaycastsRecursive(root);
            root.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, BannerPrefabPath);
            Object.DestroyImmediate(root);
            report.AppendLine($"PREFAB : écrit → {BannerPrefabPath} (TMP, raycast off, inactif)");
        }

        // ═══════════════════════════════════════════
        // HELPERS UI
        // ═══════════════════════════════════════════

        private static void CreateRimEdge(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 sizeDelta,
            Material additiveMat)
        {
            RectTransform rt = CreateChild(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;
            rt.offsetMin = new Vector2(
                anchorMin.x == 0f && anchorMax.x == 1f ? 0f : rt.offsetMin.x,
                anchorMin.y == 0f && anchorMax.y == 1f ? 0f : rt.offsetMin.y);
            // Stretch horizontal/vertical selon l'axe
            if (Mathf.Approximately(anchorMin.x, 0f) && Mathf.Approximately(anchorMax.x, 1f))
            {
                rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
                rt.sizeDelta = new Vector2(0f, sizeDelta.y);
            }
            else if (Mathf.Approximately(anchorMin.y, 0f) && Mathf.Approximately(anchorMax.y, 1f))
            {
                rt.offsetMin = new Vector2(rt.offsetMin.x, 0f);
                rt.offsetMax = new Vector2(rt.offsetMax.x, 0f);
                rt.sizeDelta = new Vector2(sizeDelta.x, 0f);
            }

            Image img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
            img.raycastTarget = false;
            if (additiveMat != null)
                img.material = additiveMat;
        }

        private static RectTransform CreateChild(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image CreateImageChild(
            RectTransform parent, string name, Sprite sprite, Color color)
        {
            RectTransform rt = CreateChild(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI CreateTmp(
            RectTransform parent, string name, float fontSize, TextAlignmentOptions align)
        {
            RectTransform rt = CreateChild(parent, name);
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.text = name;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void DisableRaycastsRecursive(GameObject root)
        {
            var graphics = root.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
        }

        private static void SetTmpAlpha(TextMeshProUGUI tmp, float a)
        {
            Color c = tmp.color;
            c.a = a;
            tmp.color = c;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void WriteReport(StringBuilder report)
        {
            string auditsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Audits"));
            Directory.CreateDirectory(auditsRoot);
            string fullPath = Path.Combine(auditsRoot, "invocation_flow_build.txt");
            File.WriteAllText(fullPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[InvocationFlowAssetsBuilder] Rapport écrit : {fullPath}");
        }
    }
}
#endif
