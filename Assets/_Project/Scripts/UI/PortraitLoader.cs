using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Charge et libère les textures de portraits via Resources (cache multi-chemins + refcount).
    /// </summary>
    public static class PortraitLoader
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string SrPortraitsFolder = "CharacterPortraits/portrait";

        // ═══════════════════════════════════════════
        // TYPES
        // ═══════════════════════════════════════════
        private class CacheEntry
        {
            public Texture2D texture;
            public int refCount;
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static readonly Dictionary<string, CacheEntry> _cacheByPath =
            new Dictionary<string, CacheEntry>();
        private static readonly Dictionary<Texture2D, string> _pathByTexture =
            new Dictionary<Texture2D, string>();

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Charge un portrait SR depuis Resources (chemin historique).
        /// </summary>
        public static Texture2D Load(string characterId)
        {
            return LoadAtPath(SrPortraitsFolder + characterId);
        }

        /// <summary>
        /// Charge une texture Resources par chemin (ex. CharacterPortraitsSSR/...).
        /// </summary>
        public static Texture2D LoadAtPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (_cacheByPath.TryGetValue(path, out CacheEntry entry) && entry != null && entry.texture != null)
            {
                entry.refCount++;
                return entry.texture;
            }

            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                Debug.LogWarning($"[PortraitLoader] Portrait introuvable, chemin tenté : {path}");
                return null;
            }

            CacheEntry created = new CacheEntry
            {
                texture = texture,
                refCount = 1
            };
            _cacheByPath[path] = created;
            _pathByTexture[texture] = path;
            return texture;
        }

        /// <summary>
        /// Libère une texture (refcount). Unload quand plus aucune référence.
        /// </summary>
        public static void Release(Texture2D texture)
        {
            if (texture == null)
                return;

            if (_pathByTexture.TryGetValue(texture, out string path))
            {
                if (_cacheByPath.TryGetValue(path, out CacheEntry entry) && entry != null)
                {
                    entry.refCount--;
                    if (entry.refCount <= 0)
                    {
                        Resources.UnloadAsset(texture);
                        _cacheByPath.Remove(path);
                        _pathByTexture.Remove(texture);
                    }
                }
                else
                {
                    // Désync défensive : retire le miroir et unload.
                    _pathByTexture.Remove(texture);
                    Resources.UnloadAsset(texture);
                }

                return;
            }

            Resources.UnloadAsset(texture);
        }

        /// <summary>
        /// Snapshot debug du cache (l'appelant fournit la liste — zéro alloc interne).
        /// </summary>
        public static void GetCacheSnapshot(List<(string path, int refCount)> buffer)
        {
            if (buffer == null)
                return;

            buffer.Clear();
            foreach (KeyValuePair<string, CacheEntry> kvp in _cacheByPath)
            {
                CacheEntry entry = kvp.Value;
                int count = entry != null ? entry.refCount : 0;
                buffer.Add((kvp.Key, count));
            }
        }
    }
}
