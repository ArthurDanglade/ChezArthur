#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée ou met à jour les prefabs CharacterBall avec AuraController pour tous les SR.
    /// Convention spritesheet : Art/Combat/Auras/aura_&lt;id&gt;.png (Multiple + Slice).
    /// Menu : Chez Arthur > UI > Build / Refresh SR Aura Balls.
    /// </summary>
    public static class CombatAuraBallBuilder
    {
        private const string SrDataFolder = "Assets/_Project/Data/Characters/SR";
        private const string BasePrefabPath = "Assets/_Project/Prefabs/Characters/CharacterBall_Base.prefab";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Characters";
        private const string AurasFolder = "Assets/_Project/Art/Combat/Auras/";
        private const string AuraLayerName = "AuraLayer";
        private const string ShadowName = "Shadow";
        private const float DefaultAuraScale = 1.35f;
        private const int CombatVisualSortingOrder = 10;

        private static readonly Dictionary<string, string> AuraSheetAliases = new Dictionary<string, string>
        {
            { "kramhoisi", "aura_fire_kramhoisi.png" }
        };

        [MenuItem("Chez Arthur/UI/Build all SR Aura Balls")]
        public static void BuildAllSrAuraBalls()
        {
            RunBatch(fullRebuild: true);
        }

        [MenuItem("Chez Arthur/UI/Refresh all SR aura frames")]
        public static void RefreshAllSrAuraFrames()
        {
            RunBatch(fullRebuild: false);
        }

        [MenuItem("Chez Arthur/UI/Build SR Aura Ball (sélection)", true)]
        private static bool ValidateBuildSelected()
        {
            return TryGetSelectedSrCharacter(out _);
        }

        [MenuItem("Chez Arthur/UI/Build SR Aura Ball (sélection)")]
        public static void BuildSelectedSrAuraBall()
        {
            if (!TryGetSelectedSrCharacter(out CharacterData data))
                return;

            BuildOrRefresh(data, fullRebuild: true);
        }

        [MenuItem("Chez Arthur/UI/Refresh SR aura frames (sélection)", true)]
        private static bool ValidateRefreshSelected()
        {
            return TryGetSelectedSrCharacter(out _);
        }

        [MenuItem("Chez Arthur/UI/Refresh SR aura frames (sélection)")]
        public static void RefreshSelectedSrAuraFrames()
        {
            if (!TryGetSelectedSrCharacter(out CharacterData data))
                return;

            BuildOrRefresh(data, fullRebuild: false);
        }

        public static void BuildOrRefreshById(string characterId, bool fullRebuild)
        {
            CharacterData data = FindSrCharacterById(characterId);
            if (data == null)
            {
                Debug.LogError("[CombatAuraBallBuilder] CharacterData SR introuvable pour id : " + characterId);
                return;
            }

            BuildOrRefresh(data, fullRebuild);
        }

        private static void RunBatch(bool fullRebuild)
        {
            List<CharacterData> characters = CollectSrCharacters();
            if (characters.Count == 0)
            {
                Debug.LogWarning("[CombatAuraBallBuilder] Aucun CharacterData SR trouvé sous " + SrDataFolder);
                return;
            }

            int built = 0;
            int refreshed = 0;
            int skipped = 0;
            int missingSheet = 0;
            var details = new StringBuilder();

            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData data = characters[i];
                BuildResult result = BuildOrRefresh(data, fullRebuild);
                switch (result.Status)
                {
                    case BuildStatus.Built:
                        built++;
                        break;
                    case BuildStatus.Refreshed:
                        refreshed++;
                        break;
                    case BuildStatus.Skipped:
                        skipped++;
                        break;
                }

                if (!result.HasAuraSheet)
                    missingSheet++;

                details.AppendLine("  - ").Append(data.CharacterName)
                    .Append(" (").Append(data.Id).Append(") : ").Append(result.Message);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[CombatAuraBallBuilder] Batch terminé — construits : " + built +
                ", frames rafraîchies : " + refreshed +
                ", ignorés : " + skipped +
                ", sans spritesheet : " + missingSheet + "\n" + details);
        }

        private static BuildResult BuildOrRefresh(CharacterData data, bool fullRebuild)
        {
            if (data == null)
                return BuildResult.Skipped("CharacterData null");

            string prefabPath = GetPrefabPath(data);
            string auraPath = ResolveAuraSheetPath(data);
            bool hasAuraSheet = HasSlicedAuraSheet(auraPath);

            if (!fullRebuild && AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                if (RefreshExistingPrefabFrames(data, prefabPath, auraPath))
                {
                    string message = hasAuraSheet
                        ? "frames rechargées"
                        : "frames rechargées (spritesheet absent ou non découpé)";
                    return new BuildResult(BuildStatus.Refreshed, hasAuraSheet, message);
                }

                Debug.LogWarning(
                    "[CombatAuraBallBuilder] Refresh impossible pour " + data.CharacterName +
                    " — build complet.");
            }

            return BuildFromBasePrefab(data, prefabPath, auraPath, hasAuraSheet);
        }

        private static BuildResult BuildFromBasePrefab(
            CharacterData data,
            string prefabPath,
            string auraPath,
            bool hasAuraSheet)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (source == null)
            {
                Debug.LogError("[CombatAuraBallBuilder] Prefab base introuvable : " + BasePrefabPath);
                return BuildResult.Skipped("prefab base introuvable");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[CombatAuraBallBuilder] Impossible d'instancier le prefab base.");
                return BuildResult.Skipped("instanciation impossible");
            }

            try
            {
                Transform root = instance.transform;
                Transform visual = FindChildByTrimmedName(root, "Visual");
                if (visual == null)
                {
                    Debug.LogError("[CombatAuraBallBuilder] Enfant Visual introuvable sur " + data.CharacterName);
                    return BuildResult.Skipped("Visual introuvable");
                }

                SpriteRenderer characterRenderer = visual.GetComponent<SpriteRenderer>();
                if (characterRenderer == null)
                {
                    Debug.LogError("[CombatAuraBallBuilder] SpriteRenderer Visual introuvable sur " + data.CharacterName);
                    return BuildResult.Skipped("SpriteRenderer Visual introuvable");
                }

                Transform shadow = FindChildByTrimmedName(root, ShadowName);
                SpriteRenderer groundAuraRenderer = shadow != null
                    ? shadow.GetComponent<SpriteRenderer>()
                    : null;
                if (groundAuraRenderer == null)
                {
                    Debug.LogError("[CombatAuraBallBuilder] SpriteRenderer Shadow introuvable sur " + data.CharacterName);
                    return BuildResult.Skipped("Shadow introuvable");
                }

                Transform auraLayer = FindChildByTrimmedName(root, AuraLayerName);
                GameObject auraGo = auraLayer != null
                    ? auraLayer.gameObject
                    : new GameObject(AuraLayerName, typeof(Transform));

                if (auraLayer == null)
                    auraGo.transform.SetParent(root, false);

                auraGo.transform.localPosition = Vector3.zero;
                auraGo.transform.localRotation = Quaternion.identity;
                auraGo.transform.localScale = Vector3.one;

                SpriteRenderer haloAuraRenderer = auraGo.GetComponent<SpriteRenderer>();
                if (haloAuraRenderer == null)
                    haloAuraRenderer = auraGo.AddComponent<SpriteRenderer>();

                characterRenderer.sortingOrder = CombatVisualSortingOrder;
                groundAuraRenderer.sortingLayerID = characterRenderer.sortingLayerID;

                AuraController auraController = instance.GetComponent<AuraController>();
                if (auraController == null)
                    auraController = instance.AddComponent<AuraController>();

                Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(auraPath);
                Sprite[] auraFrames = LoadSpritesFromSheet(auraPath);

                WireAuraController(
                    auraController,
                    sheet,
                    auraFrames,
                    preserveScale: false,
                    haloAuraRenderer,
                    groundAuraRenderer,
                    characterRenderer,
                    AuraPlacementMode.GroundRing);

                CharacterBall ball = instance.GetComponent<CharacterBall>();
                if (ball != null)
                {
                    SerializedObject ballSo = new SerializedObject(ball);
                    UiGen.Wire(ballSo, "_auraController", auraController);
                    ballSo.ApplyModifiedPropertiesWithoutUndo();
                }

                EnsureFolder(PrefabFolder);
                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                if (savedPrefab == null)
                {
                    Debug.LogError("[CombatAuraBallBuilder] Échec sauvegarde prefab : " + prefabPath);
                    return BuildResult.Skipped("échec sauvegarde prefab");
                }

                WireCharacterDataPrefab(data, savedPrefab.GetComponent<CharacterBall>());

                string message = hasAuraSheet
                    ? "prefab créé/mis à jour (" + auraFrames.Length + " frame(s))"
                    : "prefab créé/mis à jour — ajoute " + Path.GetFileName(auraPath) + " puis Refresh";

                if (!hasAuraSheet)
                {
                    Debug.LogWarning(
                        "[CombatAuraBallBuilder] " + data.CharacterName + " : spritesheet absent ou non découpé : " + auraPath);
                }

                return new BuildResult(BuildStatus.Built, hasAuraSheet, message);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static bool RefreshExistingPrefabFrames(CharacterData data, string prefabPath, string auraPath)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
                return false;

            try
            {
                AuraController auraController = prefabRoot.GetComponent<AuraController>();
                if (auraController == null)
                {
                    Debug.LogError("[CombatAuraBallBuilder] AuraController introuvable sur " + prefabPath);
                    return false;
                }

                Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(auraPath);
                Sprite[] auraFrames = LoadSpritesFromSheet(auraPath);
                if (auraFrames.Length == 0)
                {
                    Debug.LogWarning(
                        "[CombatAuraBallBuilder] " + data.CharacterName + " : aucune frame dans " + auraPath +
                        " — vérifie Sprite Mode Multiple + Slice + Apply.");
                    return false;
                }

                WireAuraController(
                    auraController,
                    sheet,
                    auraFrames,
                    preserveScale: true,
                    haloAuraRenderer: FindHaloRenderer(prefabRoot),
                    groundAuraRenderer: FindShadowRenderer(prefabRoot),
                    characterRenderer: FindCharacterRenderer(prefabRoot),
                    placement: AuraPlacementMode.GroundRing);

                CharacterBall ball = prefabRoot.GetComponent<CharacterBall>();
                if (ball != null)
                {
                    SerializedObject ballSo = new SerializedObject(ball);
                    UiGen.Wire(ballSo, "_auraController", auraController);
                    ballSo.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                WireCharacterDataPrefab(data, AssetDatabase.LoadAssetAtPath<CharacterBall>(prefabPath));
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void WireCharacterDataPrefab(CharacterData data, CharacterBall prefab)
        {
            if (data == null || prefab == null)
                return;

            SerializedObject dataSo = new SerializedObject(data);
            UiGen.Wire(dataSo, "combatBallPrefab", prefab);
            dataSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        private static void WireAuraController(
            AuraController auraController,
            Texture2D sheet,
            Sprite[] auraFrames,
            bool preserveScale,
            SpriteRenderer haloAuraRenderer,
            SpriteRenderer groundAuraRenderer,
            SpriteRenderer characterRenderer,
            AuraPlacementMode placement)
        {
            SerializedObject auraSo = new SerializedObject(auraController);
            if (haloAuraRenderer != null)
                UiGen.Wire(auraSo, "haloAuraRenderer", haloAuraRenderer);
            if (groundAuraRenderer != null)
                UiGen.Wire(auraSo, "groundAuraRenderer", groundAuraRenderer);
            if (characterRenderer != null)
                UiGen.Wire(auraSo, "characterRenderer", characterRenderer);
            UiGen.Wire(auraSo, "auraSpriteSheet", sheet);

            SerializedProperty placementProp = auraSo.FindProperty("placement");
            if (placementProp != null)
                placementProp.enumValueIndex = (int)placement;

            SerializedProperty framesProp = auraSo.FindProperty("auraFrames");
            if (framesProp != null)
            {
                framesProp.arraySize = auraFrames.Length;
                for (int i = 0; i < auraFrames.Length; i++)
                    framesProp.GetArrayElementAtIndex(i).objectReferenceValue = auraFrames[i];
            }

            if (!preserveScale)
            {
                SerializedProperty baseScaleProp = auraSo.FindProperty("auraBaseScale");
                if (baseScaleProp != null)
                    baseScaleProp.vector3Value = new Vector3(DefaultAuraScale, DefaultAuraScale, 1f);
            }

            auraSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<CharacterData> CollectSrCharacters()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { SrDataFolder });
            var characters = new List<CharacterData>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (data == null || data.Rarity != CharacterRarity.SR)
                    continue;

                if (string.IsNullOrEmpty(data.Id) || data.Id.Contains("_"))
                    continue;

                characters.Add(data);
            }

            characters.Sort((a, b) => string.Compare(a.CharacterName, b.CharacterName, System.StringComparison.Ordinal));
            return characters;
        }

        private static CharacterData FindSrCharacterById(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return null;

            List<CharacterData> characters = CollectSrCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i].Id == characterId)
                    return characters[i];
            }

            return null;
        }

        private static bool TryGetSelectedSrCharacter(out CharacterData data)
        {
            data = Selection.activeObject as CharacterData;
            return data != null && data.Rarity == CharacterRarity.SR && !string.IsNullOrEmpty(data.Id) && !data.Id.Contains("_");
        }

        private static string GetPrefabPath(CharacterData data)
        {
            return PrefabFolder + "/CharacterBall_" + GetPrefabSafeName(data) + ".prefab";
        }

        private static string GetPrefabSafeName(CharacterData data)
        {
            if (!string.IsNullOrEmpty(data.CharacterName))
                return data.CharacterName.Replace(" ", "");

            if (string.IsNullOrEmpty(data.Id))
                return "Unknown";

            return char.ToUpperInvariant(data.Id[0]) + data.Id.Substring(1);
        }

        private static string ResolveAuraSheetPath(CharacterData data)
        {
            if (AuraSheetAliases.TryGetValue(data.Id, out string aliasFileName))
                return AurasFolder + aliasFileName;

            return AurasFolder + "aura_" + data.Id + ".png";
        }

        private static bool HasSlicedAuraSheet(string auraPath)
        {
            return LoadSpritesFromSheet(auraPath).Length > 0;
        }

        private static SpriteRenderer FindCharacterRenderer(GameObject root)
        {
            Transform visual = FindChildByTrimmedName(root.transform, "Visual");
            return visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        }

        private static SpriteRenderer FindShadowRenderer(GameObject root)
        {
            Transform shadow = FindChildByTrimmedName(root.transform, ShadowName);
            return shadow != null ? shadow.GetComponent<SpriteRenderer>() : null;
        }

        private static SpriteRenderer FindHaloRenderer(GameObject root)
        {
            Transform auraLayer = FindChildByTrimmedName(root.transform, AuraLayerName);
            return auraLayer != null ? auraLayer.GetComponent<SpriteRenderer>() : null;
        }

        private static Sprite[] LoadSpritesFromSheet(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
                return new Sprite[0];

            var sprites = new List<Sprite>(assets.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    sprites.Add(sprite);
            }

            sprites.Sort((a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b)));
            return sprites.ToArray();
        }

        private static int TrailingNumber(Sprite sprite)
        {
            if (sprite == null)
                return 0;

            string name = sprite.name;
            int i = name.Length - 1;
            while (i >= 0 && char.IsDigit(name[i]))
                i--;

            return int.TryParse(name.Substring(i + 1), out int number) ? number : 0;
        }

        private static Transform FindChildByTrimmedName(Transform parent, string name)
        {
            string target = name.Trim();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Trim() == target)
                    return child;
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string child = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(child))
                AssetDatabase.CreateFolder(parent, child);
        }

        private enum BuildStatus
        {
            Built,
            Refreshed,
            Skipped
        }

        private readonly struct BuildResult
        {
            public BuildStatus Status { get; }
            public bool HasAuraSheet { get; }
            public string Message { get; }

            public BuildResult(BuildStatus status, bool hasAuraSheet, string message)
            {
                Status = status;
                HasAuraSheet = hasAuraSheet;
                Message = message;
            }

            public static BuildResult Skipped(string reason)
            {
                return new BuildResult(BuildStatus.Skipped, false, reason);
            }
        }
    }
}
#endif
