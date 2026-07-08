using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Charge et libère les textures de portraits via Resources.
    /// </summary>
    public static class PortraitLoader
    {
        private static Texture2D _cached;
        private static string _cachedId;

        /// <summary>
        /// Charge un portrait depuis Resources avec un cache mono-entrée.
        /// </summary>
        public static Texture2D Load(string characterId)
        {
            if (characterId == _cachedId && _cached != null)
                return _cached;

            if (_cached != null)
            {
                Resources.UnloadAsset(_cached);
                _cached = null;
                _cachedId = null;
            }

            string path = "CharacterPortraits/portrait" + characterId;
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"[PortraitLoader] Portrait introuvable, chemin tenté : {path}");
                return null;
            }

            _cached = texture;
            _cachedId = characterId;
            return texture;
        }

        /// <summary>
        /// Libère explicitement une texture de portrait.
        /// </summary>
        public static void Release(Texture2D texture)
        {
            if (texture == null)
                return;

            if (texture == _cached)
            {
                Resources.UnloadAsset(texture);
                _cached = null;
                _cachedId = null;
                return;
            }

            Resources.UnloadAsset(texture);
        }
    }
}
