using ChezArthur.Characters;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Adapter mince AnimatedPortraitData → IPortraitFrameSource (AW2).
    /// Charge le sheet via PortraitLoader et résout l'UV-rect au temps t
    /// avec index mémorisé (amorti O(1)).
    /// </summary>
    public sealed class AnimatedPortraitFrameSource : IPortraitFrameSource
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly Vector2Int FallbackFrameSize = new Vector2Int(96, 128);
        private static readonly Rect FullRect = new Rect(0f, 0f, 1f, 1f);

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly AnimatedPortraitData _data;
        private readonly Texture2D _sheet;
        private readonly float[] _cumulative;
        private readonly float _totalDuration;
        private readonly Vector2Int _frameSize;
        private readonly bool _invalid;

        private int _memorizedIndex;
        private bool _warned;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public Texture Texture => _sheet;
        public Vector2Int FrameSizeTexels => _frameSize;

        // ═══════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════

        /// <summary>
        /// Construit depuis un AnimatedPortraitData. Ne jette jamais.
        /// </summary>
        public AnimatedPortraitFrameSource(AnimatedPortraitData data)
        {
            _data = data;
            _memorizedIndex = 0;
            _warned = false;

            Texture2D sheet = null;
            float[] cumulative = null;
            float totalDuration = 0f;
            Vector2Int frameSize = FallbackFrameSize;
            bool invalid = true;

            if (data == null)
            {
                WarnOnce("AnimatedPortraitData null.");
            }
            else
            {
                string path = data.ResourcesPath;
                if (!string.IsNullOrEmpty(path))
                    sheet = PortraitLoader.LoadAtPath(path);

                if (sheet == null)
                {
                    WarnOnce(
                        $"Sheet introuvable (path='{path ?? string.Empty}') sur '{data.name}'.");
                    frameSize = ResolveFrameSize(data, null);
                }
                else
                {
                    var timeline = data.Timeline;
                    if (timeline == null || timeline.Count == 0)
                    {
                        WarnOnce($"Timeline vide sur '{data.name}'.");
                        frameSize = ResolveFrameSize(data, sheet);
                    }
                    else
                    {
                        int count = timeline.Count;
                        cumulative = new float[count + 1];
                        cumulative[0] = 0f;
                        for (int i = 0; i < count; i++)
                            cumulative[i + 1] = cumulative[i]
                                + Mathf.Max(0f, timeline[i].duration);

                        totalDuration = cumulative[count];
                        frameSize = ResolveFrameSize(data, sheet);
                        invalid = false;
                    }
                }
            }

            _sheet = sheet;
            _cumulative = cumulative;
            _totalDuration = totalDuration;
            _frameSize = frameSize;
            _invalid = invalid;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// UV-rect de la frame à l'instant <paramref name="time"/> (boucle).
        /// </summary>
        public Rect GetUvRect(float time)
        {
            if (_invalid || _data == null || _sheet == null
                || _cumulative == null || _cumulative.Length < 2
                || _totalDuration <= 0f)
            {
                return FullRect;
            }

            float t = time % _totalDuration;
            if (t < 0f)
                t += _totalDuration;

            // Remise à zéro si le temps a wrap / seek en arrière.
            if (_memorizedIndex < 0
                || _memorizedIndex >= _cumulative.Length - 1
                || t < _cumulative[_memorizedIndex])
            {
                _memorizedIndex = 0;
            }

            // Avance amortie O(1) tant que t est dans le segment courant.
            while (_memorizedIndex + 1 < _cumulative.Length
                   && t >= _cumulative[_memorizedIndex + 1])
            {
                _memorizedIndex++;
            }

            // Sécurité : ne pas dépasser le dernier frame.
            int lastFrame = _cumulative.Length - 2;
            if (_memorizedIndex > lastFrame)
                _memorizedIndex = lastFrame;

            var timeline = _data.Timeline;
            if (timeline == null || _memorizedIndex < 0 || _memorizedIndex >= timeline.Count)
                return FullRect;

            return _data.GetCellUvRect(timeline[_memorizedIndex].cellIndex);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private Vector2Int ResolveFrameSize(AnimatedPortraitData data, Texture2D sheet)
        {
            if (data != null && data.CellWidth > 0 && data.CellHeight > 0)
                return new Vector2Int(data.CellWidth, data.CellHeight);

            if (data != null && sheet != null && sheet.width > 0 && sheet.height > 0)
            {
                int cellIndex = 0;
                var timeline = data.Timeline;
                if (timeline != null && timeline.Count > 0)
                    cellIndex = timeline[0].cellIndex;

                Rect uv = data.GetCellUvRect(cellIndex);
                int w = Mathf.Max(1, Mathf.RoundToInt(uv.width * sheet.width));
                int h = Mathf.Max(1, Mathf.RoundToInt(uv.height * sheet.height));
                return new Vector2Int(w, h);
            }

            return FallbackFrameSize;
        }

        private void WarnOnce(string message)
        {
            if (_warned)
                return;

            _warned = true;
            Debug.LogWarning($"[AnimatedPortraitFrameSource] {message}");
        }
    }
}
