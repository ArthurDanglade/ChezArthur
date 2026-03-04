using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche une ligne de visée pendant le drag et une zone d'origine pour annuler.
    /// </summary>
    public class DragVisualizer : MonoBehaviour
    {
        [Header("Ligne de visée")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float maxLineLength = 5f;
        [SerializeField] private Color lowPowerColor = Color.white;
        [SerializeField] private Color highPowerColor = Color.red;

        [Header("Zone d'annulation")]
        [SerializeField] private GameObject cancelZone;
        [SerializeField] private float cancelZoneRadius = 0.3f;

        [Header("Feedback Cancel Zone")]
        [SerializeField] private Color cancelZoneNormalColor = new Color(0.5f, 1f, 0.5f, 0.3f);
        [SerializeField] private Color cancelZoneActiveColor = new Color(1f, 0.5f, 0.5f, 0.5f);

        private Transform _characterTransform;
        private Vector2 _dragStartWorld;
        private bool _isActive;
        private SpriteRenderer _cancelZoneRenderer;

        private void Awake()
        {
            // Cache tout au démarrage
            if (lineRenderer != null)
                lineRenderer.enabled = false;
            if (cancelZone != null)
            {
                cancelZone.SetActive(false);
                _cancelZoneRenderer = cancelZone.GetComponent<SpriteRenderer>();
            }
        }

        /// <summary>
        /// Commence la visualisation du drag.
        /// </summary>
        /// <param name="character">Transform du personnage (ligne part de lui).</param>
        /// <param name="fingerStartWorld">Position du doigt au début (pour la CancelZone).</param>
        public void StartDrag(Transform character, Vector2 fingerStartWorld)
        {
            _characterTransform = character;
            _dragStartWorld = fingerStartWorld; // Position du doigt pour la CancelZone
            _isActive = true;

            // Affiche la zone d'annulation là où le DOIGT a touché
            if (cancelZone != null)
            {
                cancelZone.SetActive(true);
                cancelZone.transform.position = new Vector3(fingerStartWorld.x, fingerStartWorld.y, 0f);
                cancelZone.transform.localScale = Vector3.one * cancelZoneRadius * 2f;
            }

            if (lineRenderer != null)
                lineRenderer.enabled = true;
        }

        /// <summary>
        /// Met à jour la visualisation pendant le drag.
        /// </summary>
        public void UpdateDrag(Vector2 currentDragWorld, float normalizedForce)
        {
            if (!_isActive || _characterTransform == null) return;

            // Direction du LANCER (opposée au drag : point d'appui → doigt actuel, puis inversée)
            Vector2 dragVector = currentDragWorld - _dragStartWorld; // Vecteur du drag
            Vector2 launchDirection = -dragVector.normalized;       // Direction opposée = direction du lancer

            // Si dans la zone d'annulation, cache la ligne
            if (IsInCancelZone(currentDragWorld))
            {
                if (lineRenderer != null)
                    lineRenderer.enabled = false;
                if (_cancelZoneRenderer != null)
                    _cancelZoneRenderer.color = cancelZoneActiveColor;
                return;
            }

            if (_cancelZoneRenderer != null)
                _cancelZoneRenderer.color = cancelZoneNormalColor;

            if (lineRenderer != null && !lineRenderer.enabled)
                lineRenderer.enabled = true;

            float lineLength = Mathf.Clamp01(normalizedForce) * maxLineLength;

            // Ligne : du PERSONNAGE vers la direction du LANCER
            Vector3 startPos = _characterTransform.position;
            Vector3 endPos = startPos + new Vector3(launchDirection.x, launchDirection.y, 0f) * lineLength;

            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, startPos);
                lineRenderer.SetPosition(1, endPos);

                Color lineColor = Color.Lerp(lowPowerColor, highPowerColor, normalizedForce);
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
            }
        }

        /// <summary>
        /// Termine la visualisation.
        /// </summary>
        public void EndDrag()
        {
            _isActive = false;
            _characterTransform = null;

            if (lineRenderer != null)
                lineRenderer.enabled = false;
            if (cancelZone != null)
                cancelZone.SetActive(false);
        }

        /// <summary>
        /// Vérifie si la position actuelle est dans la zone d'annulation (point de départ du doigt).
        /// </summary>
        public bool IsInCancelZone(Vector2 currentDragWorld)
        {
            // Distance entre le doigt actuel et le point de DÉPART DU DOIGT
            float distance = Vector2.Distance(_dragStartWorld, currentDragWorld);
            return distance <= cancelZoneRadius;
        }
    }
}
