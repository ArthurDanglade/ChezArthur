using UnityEngine;
using ChezArthur.Core;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère le drag & drop pour tirer et lancer le personnage (style slingshot).
    /// Souris (éditeur) et touch (mobile). Aucun visuel de drag pour l'instant.
    /// </summary>
    public class DragDropController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CharacterBall characterBall;
        [SerializeField] private Camera cam;

        [Header("Force de lancement")]
        [SerializeField] private float forceMultiplier = 1f;
        [SerializeField] private float maxLaunchForce = 150f;
        [SerializeField] private float minPullDistance = 0.5f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Camera _camera;
        private bool _isDragging;
        private Vector2 _dragStartWorld;
        private int _pointerId = -1;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _camera = cam != null ? cam : Camera.main;
        }

        private void Update()
        {
            if (characterBall == null || _camera == null) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            if (Input.touchCount > 0)
                ProcessTouch();
            else
                ProcessMouse();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ProcessTouch()
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (!characterBall.IsMoving && IsPointerOverBall(TouchToScreen(touch)))
                {
                    _isDragging = true;
                    _pointerId = touch.fingerId;
                    _dragStartWorld = GetWorldPosition2D(TouchToScreen(touch));
                }
            }
            else if (_pointerId == touch.fingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                    _isDragging = true;
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (_isDragging)
                        LaunchFromDrag(GetWorldPosition2D(TouchToScreen(touch)));
                    _pointerId = -1;
                    _isDragging = false;
                }
            }
        }

        private void ProcessMouse()
        {
            Vector3 screenPos = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                if (!characterBall.IsMoving && IsPointerOverBall(screenPos))
                {
                    _isDragging = true;
                    _dragStartWorld = GetWorldPosition2D(screenPos);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_isDragging)
                    LaunchFromDrag(GetWorldPosition2D(screenPos));
                _isDragging = false;
            }
        }

        /// <summary>
        /// Convertit une position écran en position monde 2D (plan XY).
        /// </summary>
        private Vector2 GetWorldPosition2D(Vector3 screenPos)
        {
            float dist = Mathf.Abs(_camera.transform.position.z);
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
            return new Vector2(world.x, world.y);
        }

        /// <summary>
        /// True si le pointeur (écran) est au-dessus du collider du personnage.
        /// </summary>
        private bool IsPointerOverBall(Vector3 screenPos)
        {
            Vector2 worldPos = GetWorldPosition2D(screenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            return hit != null && hit.GetComponent<CharacterBall>() == characterBall;
        }

        /// <summary>
        /// Lance le personnage à partir de la position de release du drag.
        /// </summary>
        private void LaunchFromDrag(Vector2 dragEndWorld)
        {
            Vector2 direction = (_dragStartWorld - dragEndWorld).normalized;
            float distance = Vector2.Distance(_dragStartWorld, dragEndWorld);

            if (distance < minPullDistance) return;

            float force = Mathf.Min(distance * forceMultiplier, maxLaunchForce);
            characterBall.Launch(direction, force);
        }

        private static Vector3 TouchToScreen(Touch touch)
        {
            return new Vector3(touch.position.x, touch.position.y, 0f);
        }
    }
}
