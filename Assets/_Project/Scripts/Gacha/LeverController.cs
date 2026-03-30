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

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (leverHandle != null)
                _leverImage = leverHandle.GetComponent<Image>();

            _currentPull = 0f;

            if (_leverImage != null && leverUp != null)
                _leverImage.sprite = leverUp;
        }

        private void OnEnable()
        {
            ResetLever();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Réinitialise le levier.
        /// </summary>
        public void ResetLever()
        {
            _currentPull = 0f;
            _isComplete = false;

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
                OnCrankComplete?.Invoke();
            }
        }
    }

    /// <summary>
    /// Alias de compatibilité pour conserver les références existantes vers CrankController.
    /// </summary>
    public class CrankController : LeverController
    {
    }
}

