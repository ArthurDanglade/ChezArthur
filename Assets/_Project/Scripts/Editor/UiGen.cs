#if UNITY_EDITOR
using UnityEngine;
using TMPro;
using UnityEditor;
using ChezArthur.UI; // UiTheme

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Helpers partagés par les générateurs UI : chargement par nom du sprite de carte,
    /// de la police TMP (via UiTheme.FontDefault) et wiring de champ sérialisé.
    /// Centralise ce qui était dupliqué dans chaque générateur.
    /// </summary>
    public static class UiGen
    {
        /// <summary> Charge un sprite par son nom exact, ou null. </summary>
        public static Sprite LoadSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            foreach (var g in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null && s.name == spriteName) return s;
            }
            return null;
        }

        /// <summary> Sprite de carte du thème, avec repli sur l'UISprite intégré. </summary>
        public static Sprite Card => LoadSprite(UiTheme.SpriteCard)
            ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        /// <summary> Knob intégré (placeholder d'icône). </summary>
        public static Sprite Knob => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        /// <summary>
        /// Pixel blanc opaque 1×1 — pour traits UI nets (sans soft edge de UISprite).
        /// Régénère le fichier s'il n'est pas réellement blanc (sinon les tints de rôle
        /// deviennent « grisé rouge »).
        /// </summary>
        public static Sprite SolidWhite
        {
            get
            {
                const string path = "Assets/_Project/Art/UI/ui_white_pixel.png";
                if (!IsSolidWhitePixel(path))
                    EnsureSolidWhitePixel(path);

                return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }

        private static bool IsSolidWhitePixel(string path)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
                return false;

            // isReadable peut être false — on force une lecture via fichier PNG.
            try
            {
                byte[] raw = System.IO.File.ReadAllBytes(path);
                var tmp = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                if (!tmp.LoadImage(raw))
                {
                    Object.DestroyImmediate(tmp);
                    return false;
                }

                Color32 px = tmp.GetPixel(0, 0);
                Object.DestroyImmediate(tmp);
                return px.r > 250 && px.g > 250 && px.b > 250 && px.a > 250;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureSolidWhitePixel(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Art");
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/UI"))
                    AssetDatabase.CreateFolder("Assets/_Project/Art", "UI");
            }

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            System.IO.File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 32;
            importer.SaveAndReimport();

            Debug.Log("[UiGen] ui_white_pixel.png régénéré en blanc opaque 1×1.");
        }

        /// <summary> Police TMP du thème (UiTheme.FontDefault), ou null = police TMP par défaut. </summary>
        public static TMP_FontAsset LoadFont()
        {
            if (string.IsNullOrEmpty(UiTheme.FontDefault)) return null;
            foreach (var g in AssetDatabase.FindAssets($"{UiTheme.FontDefault} t:TMP_FontAsset"))
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            return null;
        }

        /// <summary> Branche un champ sérialisé sur une valeur (avec avertissement si introuvable). </summary>
        public static void Wire(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[UiGen] Champ '{field}' introuvable sur {so.targetObject.GetType().Name}.");
        }
    }
}
#endif
