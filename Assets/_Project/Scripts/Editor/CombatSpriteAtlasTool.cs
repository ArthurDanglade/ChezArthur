#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Crée ou met à jour l'atlas SA_Combat_Characters pour les sprites de combat.
    /// Idempotent : réapplique les réglages et n'ajoute pas deux fois le dossier Characters.
    /// Menu : Chez Arthur > Art > Créer ou MàJ SA_Combat_Characters.
    /// </summary>
    public static class CombatSpriteAtlasTool
    {
        private const string CombatFolder = "Assets/_Project/Art/Combat";
        private const string CharactersFolder = "Assets/_Project/Art/Combat/Characters";
        private const string AtlasPath = "Assets/_Project/Art/Combat/SA_Combat_Characters.spriteatlas";
        private const int AtlasMaxTextureSize = 2048;
        private const int PackPadding = 4;

        [MenuItem("Chez Arthur/Art/Créer ou MàJ SA_Combat_Characters")]
        public static void CreateOrUpdateAtlas()
        {
            EnsureFolderExists("Assets/_Project/Art", "Combat");
            EnsureFolderExists(CombatFolder, "Characters");

            bool created = !AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);

            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
                created = true;
            }

            ApplyAtlasSettings(atlas);
            EnsureCharactersFolderPackable(atlas);

            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Object[] packables = SpriteAtlasExtensions.GetPackables(atlas);
            int packableCount = packables != null ? packables.Length : 0;
            string status = created ? "créé" : "déjà existant (réglages MàJ)";
            Debug.Log($"[CombatSpriteAtlas] Atlas {status} — {AtlasPath} — packables : {packableCount}.");
        }

        private static void EnsureFolderExists(string parentFolder, string childName)
        {
            string childPath = parentFolder + "/" + childName;
            if (AssetDatabase.IsValidFolder(childPath))
                return;

            AssetDatabase.CreateFolder(parentFolder, childName);
        }

        private static void ApplyAtlasSettings(SpriteAtlas atlas)
        {
            var packing = new SpriteAtlasPackingSettings
            {
                enableRotation = false,
                enableTightPacking = false,
                padding = PackPadding
            };
            SpriteAtlasExtensions.SetPackingSettings(atlas, packing);

            var textureSettings = new SpriteAtlasTextureSettings
            {
                filterMode = FilterMode.Point,
                sRGB = true,
                generateMipMaps = false,
                readable = false
            };
            SpriteAtlasExtensions.SetTextureSettings(atlas, textureSettings);

            ApplyPlatformSettings(atlas, "Android");
            ApplyPlatformSettings(atlas, "iPhone");

            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
        }

        private static void ApplyPlatformSettings(SpriteAtlas atlas, string buildTarget)
        {
            TextureImporterPlatformSettings platformSettings = SpriteAtlasExtensions.GetPlatformSettings(atlas, buildTarget);
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = AtlasMaxTextureSize;
            platformSettings.format = TextureImporterFormat.RGBA32;
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            SpriteAtlasExtensions.SetPlatformSettings(atlas, platformSettings);
        }

        private static void EnsureCharactersFolderPackable(SpriteAtlas atlas)
        {
            DefaultAsset charactersFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(CharactersFolder);
            if (charactersFolder == null)
            {
                Debug.LogWarning($"[CombatSpriteAtlas] Dossier introuvable : {CharactersFolder}");
                return;
            }

            Object[] packables = SpriteAtlasExtensions.GetPackables(atlas);
            if (packables != null)
            {
                for (int i = 0; i < packables.Length; i++)
                {
                    if (packables[i] == charactersFolder)
                        return;
                }
            }

            SpriteAtlasExtensions.Add(atlas, new Object[] { charactersFolder });
        }
    }
}
#endif
