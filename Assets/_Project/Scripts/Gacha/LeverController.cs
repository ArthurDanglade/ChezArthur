using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Contrôle le levier interactif pour lancer une invocation.
    /// Le joueur glisse le levier vers le bas.
    /// </summary>
    public class LeverController : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RectTransform leverHandle;
        [SerializeField] private Image progressFill;

        [Header("Levier — Position haute")]
        [SerializeField] private Sprite leverUp;
        [SerializeField] private Vector2 leverUpPosition;
        [SerializeField] private Vector2 leverUpSize = new Vector2(100f, 200f);

        [Header("Levier — Mi-course")]
        [SerializeField] private Sprite leverMid;
        [SerializeField] private Vector2 leverMidPosition;
        [SerializeField] private Vector2 leverMidSize = new Vector2(100f, 200f);

        [Header("Levier — Tiré")]
        [SerializeField] private Sprite leverDown;
        [SerializeField] private Vector2 leverDownPosition;
        [SerializeField] private Vector2 leverDownSize = new Vector2(100f, 200f);

        [Header("Configuration")]
        [SerializeField] private float pullDistance = 300f;

        [Header("Audio — 3 paliers (début / milieu / fin)")]
        [SerializeField] private AudioClip leverStartClip;
        [SerializeField] private AudioClip leverMidClip;
        [SerializeField] private AudioClip leverEndClip;
        [SerializeField] [Range(0f, 1f)] private float leverSfxVolume = 1f;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action OnCrankComplete;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _isComplete;
        private Image _leverImage;
        private float _dragStartY;
        private float _currentPull;
        /// <summary> Dernier palier SFX joué : -1 = aucun, 0 = début, 1 = milieu, 2 = fin. </summary>
        private int _sfxStagePlayed = -1;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (leverHandle != null)
                _leverImage = leverHandle.GetComponent<Image>();

            // Image sur le même GO si leverHandle non assigné / self.
            if (_leverImage == null)
                _leverImage = GetComponent<Image>();

            _currentPull = 0f;

            if (_leverImage != null && leverUp != null)
                _leverImage.sprite = leverUp;

            EnsureRaycastReceivable();
        }

        private void OnEnable()
        {
            EnsureRaycastReceivable();
            ResetLever();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Force le Graphic du levier à recevoir les events (après purge scène, etc.).
        /// </summary>
        public void EnsureRaycastReceivable()
        {
            if (_leverImage == null && leverHandle != null)
                _leverImage = leverHandle.GetComponent<Image>();
            if (_leverImage == null)
                _leverImage = GetComponent<Image>();

            if (_leverImage != null)
                _leverImage.raycastTarget = true;
        }

        /// <summary>
        /// Réinitialise le levier.
        /// </summary>
        public void ResetLever()
        {
            _currentPull = 0f;
            _isComplete = false;
            _sfxStagePlayed = -1;

            if (_leverImage != null && leverUp != null)
                _leverImage.sprite = leverUp;

            if (leverHandle != null)
            {
                leverHandle.anchoredPosition = leverUpPosition;
                leverHandle.sizeDelta = leverUpSize;
            }

            if (progressFill != null)
                progressFill.fillAmount = 0f;
        }

        /// <summary>
        /// Alias de rétrocompatibilité pour les scripts qui appellent encore ResetCrank().
        /// </summary>
        public void ResetCrank()
        {
            ResetLever();
        }

        // ═══════════════════════════════════════════
        // DRAG HANDLERS
        // ═══════════════════════════════════════════

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isComplete) return;
            _dragStartY = eventData.position.y;
            PlayStageSfx(0);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isComplete) return;

            // Distance tirée vers le bas (positif = vers le bas).
            float dragDelta = _dragStartY - eventData.position.y;
            float distance = pullDistance > 0f ? pullDistance : 1f;
            _currentPull = Mathf.Clamp01(dragDelta / distance);

            UpdateLeverSprite();
            UpdateProgress();
            UpdateStageSfx();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void UpdateLeverSprite()
        {
            if (_leverImage == null || leverHandle == null) return;

            if (_currentPull < 0.33f)
            {
                _leverImage.sprite = leverUp;
                leverHandle.anchoredPosition = leverUpPosition;
                leverHandle.sizeDelta = leverUpSize;
            }
            else if (_currentPull < 0.66f)
            {
                _leverImage.sprite = leverMid;
                leverHandle.anchoredPosition = leverMidPosition;
                leverHandle.sizeDelta = leverMidSize;
            }
            else
            {
                _leverImage.sprite = leverDown;
                leverHandle.anchoredPosition = leverDownPosition;
                leverHandle.sizeDelta = leverDownSize;
            }
        }

        private void UpdateProgress()
        {
            if (progressFill != null)
                progressFill.fillAmount = _currentPull;

            if (_currentPull >= 1f && !_isComplete)
            {
                _isComplete = true;
                PlayStageSfx(2);
                OnCrankComplete?.Invoke();
            }
        }

        /// <summary>
        /// Paliers : 0 début (&lt;0.33), 1 milieu, 2 fin (≥0.66). One-shot à la montée.
        /// </summary>
        private void UpdateStageSfx()
        {
            int stage = 0;
            if (_currentPull >= 0.66f)
                stage = 2;
            else if (_currentPull >= 0.33f)
                stage = 1;

            if (stage > _sfxStagePlayed)
                PlayStageSfx(stage);
        }

        private void PlayStageSfx(int stage)
        {
            if (stage <= _sfxStagePlayed)
                return;

            _sfxStagePlayed = stage;

            AudioClip clip = null;
            if (stage == 0)
                clip = leverStartClip;
            else if (stage == 1)
                clip = leverMidClip;
            else
                clip = leverEndClip;

            if (clip != null)
                GachaAnimationController.PlayGachaSfx(clip, leverSfxVolume);
        }
    }

    /// <summary>
    /// Alias de compatibilité pour conserver les références existantes vers CrankController.
    /// </summary>
    public class CrankController : LeverController
    {
    }
}

