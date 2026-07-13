using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Charge les sous-sprites d'une texture Multiple (spritesheet d'aura).
    /// </summary>
    public static class AuraSpriteSheetLoader
    {
        public static Sprite[] LoadSprites(Texture2D sheet, bool sortByTrailingNumber)
        {
            if (sheet == null)
                return new Sprite[0];

#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(sheet);
            if (string.IsNullOrEmpty(path))
                return new Sprite[0];

            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
                return new Sprite[0];

            var sprites = new List<Sprite>(assets.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                    sprites.Add(sprite);
            }

            if (sortByTrailingNumber && sprites.Count > 1)
                sprites.Sort((a, b) => TrailingNumber(a).CompareTo(TrailingNumber(b)));

            return sprites.ToArray();
#else
            return new Sprite[0];
#endif
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
    }
}
