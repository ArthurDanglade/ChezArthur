using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Source statique : une Texture2D ou un Sprite, rect UV fixe.
    /// </summary>
    public sealed class StaticPortraitSource : IPortraitFrameSource
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Texture _texture;
        private readonly Rect _uvRect;
        private readonly Vector2Int _frameSize;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public Texture Texture => _texture;
        public Vector2Int FrameSizeTexels => _frameSize;

        // ═══════════════════════════════════════════
        // CONSTRUCTEURS
        // ═══════════════════════════════════════════

        /// <summary>Construit depuis une Texture2D pleine (UV 0..1).</summary>
        public StaticPortraitSource(Texture2D texture)
        {
            _texture = texture;
            _uvRect = new Rect(0f, 0f, 1f, 1f);
            _frameSize = texture != null
                ? new Vector2Int(texture.width, texture.height)
                : Vector2Int.one;
        }

        /// <summary>Construit depuis un Sprite (UV = textureRect normalisé).</summary>
        public StaticPortraitSource(Sprite sprite)
        {
            if (sprite == null)
            {
                _texture = null;
                _uvRect = new Rect(0f, 0f, 1f, 1f);
                _frameSize = Vector2Int.one;
                return;
            }

            _texture = sprite.texture;
            Rect tr = sprite.textureRect;
            float tw = sprite.texture != null ? sprite.texture.width : 1f;
            float th = sprite.texture != null ? sprite.texture.height : 1f;
            _uvRect = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
            _frameSize = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(tr.width)),
                Mathf.Max(1, Mathf.RoundToInt(tr.height)));
        }

        /// <summary>Construit depuis texture + rect UV + taille frame explicites.</summary>
        public StaticPortraitSource(Texture texture, Rect uvRect, Vector2Int frameSize)
        {
            _texture = texture;
            _uvRect = uvRect;
            _frameSize = new Vector2Int(Mathf.Max(1, frameSize.x), Mathf.Max(1, frameSize.y));
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public Rect GetUvRect(float time) => _uvRect;
    }

    /// <summary>
    /// Flipbook simple (liste de sprites + fps) — DEV / harness uniquement.
    /// Ce n'est PAS le contrat d'intégration AW2.
    /// </summary>
    public sealed class SimpleFlipbookSource : IPortraitFrameSource
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Sprite[] _frames;
        private readonly float _fps;
        private readonly Vector2Int _frameSize;
        private Texture _lastTexture;
        private Rect _lastUv;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public Texture Texture => _lastTexture;
        public Vector2Int FrameSizeTexels => _frameSize;

        // ═══════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════

        public SimpleFlipbookSource(Sprite[] frames, float fps)
        {
            _frames = frames;
            _fps = Mathf.Max(0.01f, fps);
            _frameSize = Vector2Int.one;
            _lastUv = new Rect(0f, 0f, 1f, 1f);

            if (frames != null && frames.Length > 0 && frames[0] != null)
            {
                Rect tr = frames[0].textureRect;
                _frameSize = new Vector2Int(
                    Mathf.Max(1, Mathf.RoundToInt(tr.width)),
                    Mathf.Max(1, Mathf.RoundToInt(tr.height)));
                CacheFrame(0);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public Rect GetUvRect(float time)
        {
            if (_frames == null || _frames.Length == 0)
                return _lastUv;

            int idx = Mathf.FloorToInt(Mathf.Max(0f, time) * _fps) % _frames.Length;
            if (idx < 0) idx += _frames.Length;
            CacheFrame(idx);
            return _lastUv;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void CacheFrame(int idx)
        {
            Sprite sp = _frames[idx];
            if (sp == null)
                return;

            _lastTexture = sp.texture;
            Rect tr = sp.textureRect;
            float tw = sp.texture != null ? sp.texture.width : 1f;
            float th = sp.texture != null ? sp.texture.height : 1f;
            _lastUv = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
        }
    }
}
