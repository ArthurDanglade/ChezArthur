using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Contrôle la manivelle interactive pour lancer une invocation.
    /// Le joueur fait un geste circulaire pour tourner la manivelle.
    /// </summary>
    public class CrankController : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private RectTransform crankHandle;
        [SerializeField] private Image progressFill;

        [Header("Configuration")]
        [SerializeField] private float rotationsRequired = 3f; // Nombre de tours pour compléter
        [SerializeField] private float rotationSpeed = 1f; // Multiplicateur de vitesse

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action OnCrankComplete;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _totalRotation = 0f;
        private float _targetRotation;
        private float _previousAngle;
        private bool _isComplete = false;
        private Vector2 _centerPoint;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            _targetRotation = rotationsRequired * 360f;
            ResetCrank();
        }

        private void OnEnable()
        {
            ResetCrank();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Réinitialise la manivelle.
        /// </summary>
        public void ResetCrank()
        {
            _totalRotation = 0f;
            _isComplete = false;

            if (crankHandle != null)
                crankHandle.localRotation = Quaternion.identity;

            if (progressFill != null)
                progressFill.fillAmount = 0f;
        }

        // ═══════════════════════════════════════════
        // DRAG HANDLERS
        // ═══════════════════════════════════════════

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isComplete) return;

            // Calculer le centre de la manivelle en coordonnées écran
            _centerPoint = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, crankHandle.position);

            // Calculer l'angle initial
            Vector2 direction = eventData.position - _centerPoint;
            _previousAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isComplete) return;

            // Calculer l'angle actuel
            Vector2 direction = eventData.position - _centerPoint;
            float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Calculer la différence d'angle
            float angleDelta = Mathf.DeltaAngle(_previousAngle, currentAngle);

            // Appliquer la rotation (seulement dans le sens horaire = négatif)
            float rotationToAdd = -angleDelta * rotationSpeed;

            if (rotationToAdd > 0) // Sens horaire uniquement
            {
                _totalRotation += rotationToAdd;

                // Tourner visuellement la manivelle
                if (crankHandle != null)
                {
                    crankHandle.Rotate(0f, 0f, -rotationToAdd);
                }

                // Mettre à jour la jauge
                UpdateProgress();
            }

            _previousAngle = currentAngle;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void UpdateProgress()
        {
            float progress = Mathf.Clamp01(_totalRotation / _targetRotation);

            if (progressFill != null)
            {
                progressFill.fillAmount = progress;
            }

            // Vérifier si complet
            if (progress >= 1f && !_isComplete)
            {
                _isComplete = true;
                OnCrankComplete?.Invoke();
            }
        }
    }
}
