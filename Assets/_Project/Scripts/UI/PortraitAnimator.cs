using System.Collections.Generic;
using ChezArthur.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Anime le uvRect d'un RawImage portrait selon AnimatedPortraitData.
    /// Seul écrivain de rawImage.uvRect dans le pipeline portrait.
    /// </summary>
    [DisallowMultipleComponent]
    public class PortraitAnimator : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float MIN_SEGMENT_DURATION = 0.01f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RawImage _rawImage;
        private AnimatedPortraitData _data;
        private IReadOnlyList<PortraitFrame> _timeline;
        private int _segmentIndex;
        private float _accumulator;
        private Rect _currentCellUv = new Rect(0f, 0f, 1f, 1f);
        private Rect _cropRect = new Rect(0f, 0f, 1f, 1f);
        private bool _loop = true;
        private bool _oneShotFinished;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// True quand le dernier segment est écoulé en mode one-shot.
        /// L'Update fige alors la dernière frame et passe enabled=false.
        /// </summary>
        public bool HasFinishedOneShot => _oneShotFinished;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            if (_data == null || _data.IsStatic)
                return;

            _segmentIndex = 0;
            _accumulator = 0f;
            _oneShotFinished = false;
            if (_timeline != null && _timeline.Count > 0)
                _currentCellUv = _data.GetCellUvRect(_timeline[0].cellIndex);
            else
                _currentCellUv = new Rect(0f, 0f, 1f, 1f);

            ApplyComposedUvRect();
        }

        private void Update()
        {
            if (_data == null || _timeline == null || _timeline.Count == 0 || _data.TotalDuration <= 0f)
            {
                enabled = false;
                return;
            }

            if (_oneShotFinished)
            {
                enabled = false;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            if (_data.TotalDuration > 0f)
                dt = Mathf.Min(dt, _data.TotalDuration);

            _accumulator += dt;
            bool changed = false;
            float segDur = Mathf.Max(MIN_SEGMENT_DURATION, _timeline[_segmentIndex].duration);

            while (_accumulator >= segDur)
            {
                _accumulator -= segDur;

                // Fin du dernier segment en one-shot : figer, désactiver Update.
                if (_segmentIndex >= _timeline.Count - 1)
                {
                    if (!_loop)
                    {
                        _accumulator = 0f;
                        _oneShotFinished = true;
                        enabled = false;
                        return;
                    }

                    _segmentIndex = 0;
                }
                else
                {
                    _segmentIndex++;
                }

                segDur = Mathf.Max(MIN_SEGMENT_DURATION, _timeline[_segmentIndex].duration);
                changed = true;
            }

            if (changed)
            {
                _currentCellUv = _data.GetCellUvRect(_timeline[_segmentIndex].cellIndex);
                ApplyComposedUvRect();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Lie le RawImage cible (écrit uniquement via cet animator). </summary>
        public void Initialize(RawImage target)
        {
            _rawImage = target;
        }

        /// <summary> Lance la lecture animée d'un sheet SSR (boucle). </summary>
        public void PlayAnimated(AnimatedPortraitData data)
        {
            _loop = true;
            _oneShotFinished = false;
            ApplyPlayback(data);
        }

        /// <summary>
        /// Lecture one-shot : comme PlayAnimated mais sans boucle.
        /// HasFinishedOneShot passe à true à la fin du dernier segment.
        /// </summary>
        public void PlayAnimatedOnce(AnimatedPortraitData data)
        {
            _loop = false;
            _oneShotFinished = false;
            ApplyPlayback(data);
        }

        /// <summary> Mode statique : cellule identité, Update désactivé. </summary>
        public void PlayStatic()
        {
            _data = null;
            _timeline = null;
            _loop = true;
            _oneShotFinished = false;
            _currentCellUv = new Rect(0f, 0f, 1f, 1f);
            enabled = false;
            ApplyComposedUvRect();
        }

        /// <summary>
        /// Crop en espace cellule (0–1 dans une frame). Composé avec la cellule courante.
        /// </summary>
        public void SetCropRect(Rect cropInCellSpace)
        {
            _cropRect = cropInCellSpace;
            ApplyComposedUvRect();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyPlayback(AnimatedPortraitData data)
        {
            _data = data;
            _timeline = data != null ? data.Timeline : null;
            _segmentIndex = 0;
            _accumulator = 0f;

            if (_timeline != null && _timeline.Count > 0 && data != null)
                _currentCellUv = data.GetCellUvRect(_timeline[0].cellIndex);
            else
                _currentCellUv = new Rect(0f, 0f, 1f, 1f);

            enabled = data != null && !data.IsStatic;
            ApplyComposedUvRect();
        }

        private void ApplyComposedUvRect()
        {
            if (_rawImage == null || _rawImage.texture == null)
                return;

            _rawImage.uvRect = new Rect(
                _currentCellUv.x + _cropRect.x * _currentCellUv.width,
                _currentCellUv.y + _cropRect.y * _currentCellUv.height,
                _cropRect.width * _currentCellUv.width,
                _cropRect.height * _currentCellUv.height);
        }
    }
}
