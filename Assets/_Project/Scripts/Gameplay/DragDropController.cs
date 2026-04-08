using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère le drag & drop pour tirer et lancer le personnage actif du TurnManager (style slingshot).
    /// Souris (éditeur) et touch (mobile). Aucun visuel de drag pour l'instant.
    /// </summary>
    public class DragDropController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [Tooltip("Contrôle le personnage actif (CurrentParticipant) au lieu d'un CharacterBall fixe.")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Camera cam;

        [Header("Force de lancement")]
        [SerializeField] private float forceMultiplier = 50f;
        [SerializeField] private float maxLaunchForce = 150f;
        [SerializeField] private float minPullDistance = 0.01f;
        [SerializeField] private float maxDragDistance = 3f;

        [Header("UI")]
        [SerializeField] private LaunchForceUI launchForceUI;

        [Header("Visualisation")]
        [SerializeField] private DragVisualizer dragVisualizer;

        [Header("Tap / Spec Switch")]
        [Tooltip("Distance max en unités monde en dessous de "
            + "laquelle un press+release est considéré comme un tap.")]
        [SerializeField] private float tapMaxDistance = 0.3f;
        [Tooltip("Durée max en secondes pour qu'un press soit "
            + "considéré comme un tap (pas un drag).")]
        [SerializeField] private float tapMaxDuration = 0.25f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Camera _camera;
        private bool _isDragging;
        private float _pressTime;
        private Vector2 _pressWorldPos;
        private bool _pressCancelled;
        private Enemy _pressedEnemy;
        private Vector2 _dragStartWorld;   // Point d'appui du doigt (pour calculs direction/force = mouvement du doigt)
        private Vector2 _fingerStartWorld; // Même chose (pour CancelZone et passage au DragVisualizer)
        private int _pointerId = -1;

        // Cache pour IsPointerOverInteractableUI (évite les allocations dans Update)
        private PointerEventData _cachedPointerData;
        private List<RaycastResult> _cachedRaycastResults;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _camera = cam != null ? cam : Camera.main;

            if (EventSystem.current != null)
                _cachedPointerData = new PointerEventData(EventSystem.current);
            _cachedRaycastResults = new List<RaycastResult>();
        }

        private void Update()
        {
            if (turnManager == null || !turnManager.HasCurrentParticipant || !turnManager.IsPlayerTurn || _camera == null) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            // Met à jour la jauge de force pendant le drag
            if (_isDragging)
                UpdateLaunchForceUI();

            if (Input.touchCount > 0)
                ProcessTouch();
            else
                ProcessMouse();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// True si le pointeur est au-dessus d'un élément UI réellement interactif (Button, Slider, Toggle, etc.).
        /// Retourne false pour les Image/Text seuls (ex. fond de Canvas), pour ne pas bloquer le drag.
        /// </summary>
        private bool IsPointerOverInteractableUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null || _cachedPointerData == null || _cachedRaycastResults == null) return false;

            _cachedPointerData.position = screenPosition;
            _cachedRaycastResults.Clear();
            EventSystem.current.RaycastAll(_cachedPointerData, _cachedRaycastResults);

            for (int i = 0; i < _cachedRaycastResults.Count; i++)
            {
                GameObject go = _cachedRaycastResults[i].gameObject;
                if (go == null) continue;

                if (go.GetComponent<Button>() != null) return true;
                if (go.GetComponent<Slider>() != null) return true;
                if (go.GetComponent<Toggle>() != null) return true;
                if (go.GetComponent<TMP_InputField>() != null) return true;
                if (go.GetComponent<ScrollRect>() != null) return true;
            }

            return false;
        }

        private void ProcessTouch()
        {
            Touch touch = Input.GetTouch(0);
            Vector2 worldPos = GetWorldPosition2D(
                TouchToScreen(touch));

            if (touch.phase == TouchPhase.Began)
            {
                if (IsPointerOverInteractableUI(touch.position))
                    return;

                _pressTime = Time.time;
                _pressWorldPos = worldPos;
                _pressCancelled = false;
                _pressedEnemy = null;

                Enemy tappedEnemy = GetEnemyAtWorldPos(worldPos);
                if (tappedEnemy != null && turnManager.IsPlayerTurn)
                {
                    _pressedEnemy = tappedEnemy;
                    _pointerId = touch.fingerId;
                    EnemyCardUI.Instance?.Show(tappedEnemy);
                    return;
                }

                if (!turnManager.CurrentParticipant.IsMoving)
                {
                    _pointerId = touch.fingerId;
                    _fingerStartWorld = worldPos;
                    _dragStartWorld = worldPos;
                    // _isDragging démarré dans TouchPhase.Moved
                }
            }
            else if (_pointerId == touch.fingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    float dist = Vector2.Distance(_pressWorldPos, worldPos);
                    if (dist > tapMaxDistance)
                    {
                        _pressCancelled = true;
                        if (!_isDragging
                            && _pressedEnemy == null
                            && turnManager.HasCurrentParticipant
                            && turnManager.IsPlayerTurn
                            && !turnManager.CurrentParticipant.IsMoving)
                        {
                            _isDragging = true;
                            launchForceUI?.Show();
                            if (launchForceUI != null
                                && turnManager.CurrentParticipant != null)
                                launchForceUI.SetTarget(
                                    turnManager.CurrentParticipant.Transform);
                            if (dragVisualizer != null
                                && turnManager.CurrentParticipant != null)
                                dragVisualizer.StartDrag(
                                    turnManager.CurrentParticipant.Transform,
                                    _fingerStartWorld);
                        }
                    }
                }
                else if (touch.phase == TouchPhase.Ended
                    || touch.phase == TouchPhase.Canceled)
                {
                    // Relâchement ennemi → ferme card
                    if (_pressedEnemy != null)
                    {
                        EnemyCardUI.Instance?.Hide();
                        _pressedEnemy = null;
                        _pointerId = -1;
                        return;
                    }

                    // Tap sur CharacterBall → switch spé
                    if (!_pressCancelled && !_isDragging)
                    {
                        float duration = Time.time - _pressTime;
                        float dist = Vector2.Distance(
                            _pressWorldPos, worldPos);
                        if (duration <= tapMaxDuration
                            && dist <= tapMaxDistance)
                        {
                            TrySpecSwitchAtWorldPos(worldPos);
                        }
                    }

                    if (_isDragging)
                        LaunchFromDrag(worldPos);

                    dragVisualizer?.EndDrag();
                    if (launchForceUI != null)
                        launchForceUI.SetTarget(null);
                    launchForceUI?.Hide();
                    _pointerId = -1;
                    _isDragging = false;
                    _pressCancelled = false;
                }
            }
        }

        private void ProcessMouse()
        {
            Vector3 screenPos = Input.mousePosition;
            Vector2 worldPos = GetWorldPosition2D(screenPos);

            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverInteractableUI(Input.mousePosition))
                    return;

                _pressTime = Time.time;
                _pressWorldPos = worldPos;
                _pressCancelled = false;
                _pressedEnemy = null;

                // Détecte si on appuie sur un ennemi
                Enemy tappedEnemy = GetEnemyAtWorldPos(worldPos);
                if (tappedEnemy != null && turnManager.IsPlayerTurn)
                {
                    _pressedEnemy = tappedEnemy;
                    EnemyCardUI.Instance?.Show(tappedEnemy);
                    return; // ne pas démarrer le drag
                }

                if (!turnManager.CurrentParticipant.IsMoving)
                {
                    // Ne pas démarrer _isDragging immédiatement.
                    // Le drag démarre seulement si le joueur bouge
                    // au-delà de tapMaxDistance (voir bloc else if).
                    _fingerStartWorld = worldPos;
                    _dragStartWorld = worldPos;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                // Relâchement après appui sur un ennemi
                if (_pressedEnemy != null)
                {
                    EnemyCardUI.Instance?.Hide();
                    _pressedEnemy = null;
                    return;
                }

                // Détecte tap sur CharacterBall pour switch spé
                if (!_pressCancelled && _isDragging == false)
                {
                    float duration = Time.time - _pressTime;
                    float dist = Vector2.Distance(_pressWorldPos, worldPos);
                    if (duration <= tapMaxDuration && dist <= tapMaxDistance)
                    {
                        TrySpecSwitchAtWorldPos(worldPos);
                    }
                }

                if (_isDragging)
                    LaunchFromDrag(worldPos);

                dragVisualizer?.EndDrag();
                if (launchForceUI != null)
                    launchForceUI.SetTarget(null);
                launchForceUI?.Hide();
                _isDragging = false;
                _pressCancelled = false;
            }
            else if (Input.GetMouseButton(0))
            {
                float dist = Vector2.Distance(_pressWorldPos, worldPos);
                if (dist > tapMaxDistance)
                {
                    _pressCancelled = true;
                    if (!_isDragging
                        && _pressedEnemy == null
                        && turnManager.HasCurrentParticipant
                        && turnManager.IsPlayerTurn
                        && !turnManager.CurrentParticipant.IsMoving)
                    {
                        // Démarre le drag maintenant qu'on a bougé
                        _isDragging = true;
                        launchForceUI?.Show();
                        if (launchForceUI != null
                            && turnManager.CurrentParticipant != null)
                            launchForceUI.SetTarget(
                                turnManager.CurrentParticipant.Transform);
                        if (dragVisualizer != null
                            && turnManager.CurrentParticipant != null)
                            dragVisualizer.StartDrag(
                                turnManager.CurrentParticipant.Transform,
                                _fingerStartWorld);
                    }
                }
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
        /// True si le pointeur (écran) est au-dessus du collider du personnage actif.
        /// </summary>
        private bool IsPointerOverBall(Vector3 screenPos)
        {
            if (turnManager == null || !turnManager.HasCurrentParticipant || !turnManager.IsPlayerTurn) return false;
            Vector2 worldPos = GetWorldPosition2D(screenPos);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            return hit != null && hit.GetComponent<CharacterBall>() == turnManager.CurrentParticipant as CharacterBall;
        }

        /// <summary>
        /// Met à jour la jauge de force pendant le drag (position actuelle du pointeur).
        /// </summary>
        private void UpdateLaunchForceUI()
        {
            if (launchForceUI == null || turnManager == null || !turnManager.HasCurrentParticipant) return;

            Vector2 currentWorld = GetCurrentDragWorldPosition();
            float distance = Vector2.Distance(_dragStartWorld, currentWorld);
            float normalizedForce = maxDragDistance > 0f ? distance / maxDragDistance : 0f;

            float maxMultiplier = 1f;
            if (turnManager.CurrentParticipant is CharacterBall ball)
                maxMultiplier = ball.EffectiveLaunchForceMultiplier;

            // Cap la force selon le multiplicateur de bonus (100% sans bonus, plus avec)
            normalizedForce = Mathf.Clamp(normalizedForce, 0f, maxMultiplier);

            // En zone d'annulation, afficher 0%
            float displayForce = (dragVisualizer != null && dragVisualizer.IsInCancelZone(currentWorld)) ? 0f : normalizedForce;
            launchForceUI.UpdateForce(displayForce, maxMultiplier);

            // Met à jour la ligne de visée
            if (dragVisualizer != null)
                dragVisualizer.UpdateDrag(currentWorld, normalizedForce);
        }

        /// <summary>
        /// Retourne la position monde 2D actuelle du pointeur (souris ou doigt en cours de drag).
        /// </summary>
        private Vector2 GetCurrentDragWorldPosition()
        {
            if (Input.touchCount > 0 && _pointerId >= 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.fingerId == _pointerId)
                        return GetWorldPosition2D(TouchToScreen(t));
                }
            }
            return GetWorldPosition2D(Input.mousePosition);
        }

        /// <summary>
        /// Lance le personnage actif à partir de la position de release du drag.
        /// </summary>
        private void LaunchFromDrag(Vector2 dragEndWorld)
        {
            if (turnManager == null || !turnManager.HasCurrentParticipant || !turnManager.IsPlayerTurn) return;

            // Si dans la zone d'annulation, ne pas lancer
            if (dragVisualizer != null && dragVisualizer.IsInCancelZone(dragEndWorld))
            {
                Debug.Log("[DragDrop] Lancer annulé - dans la zone d'annulation");
                return;
            }

            Vector2 direction = (_dragStartWorld - dragEndWorld).normalized;
            float distance = Vector2.Distance(_dragStartWorld, dragEndWorld);
            float normalizedPercent = maxDragDistance > 0f ? (distance / maxDragDistance) * 100f : 0f;

            Debug.Log($"[DragDrop] Distance: {distance:F3}, MinPull: {minPullDistance:F3}, Percent: {normalizedPercent:F1}%");

            if (distance < minPullDistance)
            {
                Debug.Log($"[DragDrop] ANNULÉ - distance ({distance:F3}) < minPullDistance ({minPullDistance:F3})");
                return;
            }

            float maxMultiplier = 1f;
            if (turnManager.CurrentParticipant is CharacterBall ball)
                maxMultiplier = ball.EffectiveLaunchForceMultiplier;

            // La distance effective est cappée selon maxDragDistance et le bonus
            float effectiveDistance = Mathf.Min(distance, maxDragDistance * maxMultiplier);
            float force = effectiveDistance * forceMultiplier;

            turnManager.CurrentParticipant.Launch(direction, force);
        }

        /// <summary>
        /// Retourne l'ennemi sous le pointeur monde, ou null.
        /// </summary>
        private Enemy GetEnemyAtWorldPos(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return null;
            return hit.GetComponent<Enemy>();
        }

        /// <summary>
        /// Si un CharacterBall SSR ou LR est sous le pointeur
        /// et que c'est son tour, passe à la spé suivante.
        /// </summary>
        private void TrySpecSwitchAtWorldPos(Vector2 worldPos)
        {
            if (turnManager == null
                || !turnManager.HasCurrentParticipant
                || !turnManager.IsPlayerTurn)
                return;

            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return;

            CharacterBall ball = hit.GetComponent<CharacterBall>();
            if (ball == null) return;

            // Vérifie que c'est bien le participant actif
            if (!ReferenceEquals(ball, turnManager.CurrentParticipant))
                return;

            // Vérifie que le perso a au moins une spé alternative
            if (ball.Data == null) return;
            int specCount = ball.Data.GetSpecializationCount();
            if (specCount <= 0) return;

            // Calcule l'index actuel et le suivant (cyclique)
            int currentIndex = -1;
            if (ball.ActiveSpec != null && ball.Data != null)
            {
                // Base = -1, alternatives = 0, 1, 2...
                for (int i = 0; i < specCount; i++)
                {
                    SpecializationData alt =
                        ball.Data.GetSpecialization(i);
                    if (ReferenceEquals(alt, ball.ActiveSpec))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            // Cycle : -1 → 0 → 1 → ... → specCount-1 → -1
            int nextIndex;
            if (currentIndex == specCount - 1)
                nextIndex = -1; // retour à la spé de base
            else
                nextIndex = currentIndex + 1;

            ball.SwitchSpecInCombat(nextIndex);

            // Affiche le bandeau de switch
            if (SpecSwitchBannerUI.Instance != null
                && ball.ActiveSpec != null)
            {
                SpecSwitchBannerUI.Instance.Show(
                    ball.ActiveSpec.SpecName,
                    ball.ActiveSpec.Role);
            }

            Debug.Log($"[DragDropController] Switch spé : "
                + $"{ball.Data.CharacterName} → spé {nextIndex}");
        }

        private static Vector3 TouchToScreen(Touch touch)
        {
            return new Vector3(touch.position.x, touch.position.y, 0f);
        }
    }
}
