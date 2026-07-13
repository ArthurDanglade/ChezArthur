using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ChezArthur.Audio;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Transition inter-étages « ascenseur » : défilement vertical caméra fluide,
    /// écran noir, swap étage, retour à la vue normale (même décor recréé).
    /// </summary>
    public class StageTransitionUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON (scène)
        // ═══════════════════════════════════════════
        public static StageTransitionUI Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Overlay")]
        [SerializeField] private CanvasGroup overlayGroup;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField, Range(0.5f, 1f)] private float overlayMaxAlpha = 0.95f;

        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI stageNumberText;

        [Header("Ascenseur caméra")]
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] private Arena arena;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private float scrollOffsetY = 16f;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.45f;
        [SerializeField] private float tickInterval = 0.28f;
        [SerializeField] private float preScrollSettleDuration = 0.3f;
        [SerializeField] private float scrollDuration = 1.3f;
        [SerializeField] private float blackHoldDuration = 0.45f;
        [SerializeField] private float fadeOutDuration = 0.55f;
        [SerializeField] private float tickPunchScale = 1.18f;
        [SerializeField] private float tickPunchDuration = 0.14f;

        [Header("Audio")]
        [SerializeField] private AudioClip tickClip;
        [SerializeField, Range(0f, 1f)] private float tickVolume = 0.55f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Vector3 _numberBaseScale = Vector3.one;
        private float _lastTickPunchTime = -999f;
        private int _ticksPlayed;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (stageNumberText != null)
                _numberBaseScale = stageNumberText.transform.localScale;

            if (arena == null)
                arena = FindObjectOfType<Arena>();

            if (turnManager == null)
                turnManager = FindObjectOfType<TurnManager>();

            if (cameraShake == null && Camera.main != null)
                cameraShake = Camera.main.GetComponent<CameraShake>();

            RefreshScrollOffset();
            ResetCameraScroll();
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Joue la transition ascenseur. onBlackHold est invoqué pendant l'écran noir (swap étage).
        /// </summary>
        public IEnumerator PlayAscenseur(int fromStage, int toStage, Action onBlackHold = null)
        {
            yield return PlayAscenseurRoutine(fromStage, toStage, onBlackHold);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator PlayAscenseurRoutine(int fromStage, int toStage, Action onBlackHold)
        {
            if (overlayGroup == null)
                yield break;

            EnsureActiveHierarchy();
            RefreshScrollOffset();
            ResetCameraScroll();
            RefitArenaDecor();

            if (cameraShake == null && Camera.main != null)
                cameraShake = Camera.main.GetComponent<CameraShake>();

            if (cameraShake != null)
                cameraShake.ClearTrauma();

            int start = Mathf.Max(1, fromStage);
            int end = Mathf.Max(start, toStage);
            int tickSteps = Mathf.Max(1, end - start);

            if (labelText != null)
                labelText.text = "ÉTAGE";

            if (stageNumberText != null)
            {
                stageNumberText.text = start.ToString();
                stageNumberText.transform.localScale = _numberBaseScale;
            }

            overlayGroup.blocksRaycasts = true;
            overlayGroup.interactable = false;

            if (turnManager != null)
                turnManager.SetTurnChangeEnabled(false);

            PreserveMovingAllies();

            float ticksDuration = tickSteps * tickInterval;
            float scrollStart = fadeInDuration + ticksDuration + preScrollSettleDuration;
            float scrollEnd = scrollStart + scrollDuration;
            float blackHoldStart = scrollEnd;
            float fadeOutStart = blackHoldStart + blackHoldDuration;
            float totalDuration = fadeOutStart + fadeOutDuration;
            float ticksStart = fadeInDuration;

            _ticksPlayed = 0;
            bool blackHoldFired = false;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                if (!blackHoldFired)
                {
                    float scrollT = EvaluateScroll(elapsed, scrollStart, scrollEnd);
                    ApplyCameraScroll(scrollT);
                }

                overlayGroup.alpha = EvaluateOverlayAlpha(
                    elapsed,
                    scrollStart,
                    scrollEnd,
                    fadeOutStart) * overlayMaxAlpha;

                UpdateStageTicks(elapsed, ticksStart, tickInterval, tickSteps, start, end);
                ApplyNumberPunch(elapsed);

                if (!blackHoldFired && elapsed >= blackHoldStart)
                {
                    blackHoldFired = true;
                    ResetCameraScroll();
                    onBlackHold?.Invoke();
                }

                yield return null;
            }

            if (stageNumberText != null)
            {
                stageNumberText.text = end.ToString();
                stageNumberText.transform.localScale = _numberBaseScale;
            }

            ResetCameraScroll();
            HideOverlayOnly();
        }

        private void RefitArenaDecor()
        {
            if (arena == null)
                return;

            ArenaDecor decor = FindObjectOfType<ArenaDecor>();
            if (decor != null)
                decor.FitToBounds(arena.Bounds);

            ArenaBackground background = FindObjectOfType<ArenaBackground>();
            if (background != null)
                background.FitToBounds(arena.Bounds);
        }

        private void RefreshScrollOffset()
        {
            if (arena != null)
                scrollOffsetY = arena.Height;
        }

        private static float EvaluateScroll(float elapsed, float scrollStart, float scrollEnd)
        {
            if (elapsed < scrollStart)
                return 0f;

            if (elapsed >= scrollEnd)
                return 1f;

            float t = (elapsed - scrollStart) / (scrollEnd - scrollStart);
            return SmoothStep(t);
        }

        private float EvaluateOverlayAlpha(float elapsed, float scrollStart, float scrollEnd, float fadeOutStart)
        {
            if (elapsed < fadeInDuration)
                return SmoothStep(elapsed / fadeInDuration) * 0.7f;

            if (elapsed < scrollStart)
                return 0.7f;

            if (elapsed < scrollEnd)
            {
                float scrollT = (elapsed - scrollStart) / (scrollEnd - scrollStart);
                return Mathf.Lerp(0.7f, 1f, SmoothStep(scrollT));
            }

            if (elapsed < fadeOutStart)
                return 1f;

            float fadeT = (elapsed - fadeOutStart) / fadeOutDuration;
            return 1f - SmoothStep(fadeT);
        }

        private void ApplyCameraScroll(float progress01)
        {
            if (cameraShake == null)
                return;

            cameraShake.SetCinematic(new Vector2(0f, scrollOffsetY * progress01), 0f);
        }

        private void ResetCameraScroll()
        {
            if (cameraShake == null && Camera.main != null)
                cameraShake = Camera.main.GetComponent<CameraShake>();

            if (cameraShake != null)
            {
                cameraShake.ClearTrauma();
                cameraShake.SetCinematic(Vector2.zero, 0f);
            }
        }

        /// <summary>
        /// Garde les alliés encore en mouvement en Dynamic pour qu'ils rebondissent pendant le scroll caméra.
        /// Le repositionnement spawn se fait dans onBlackHold (ContinueToNextStage).
        /// </summary>
        private void PreserveMovingAllies()
        {
            if (turnManager == null)
                return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || !ally.IsMoving)
                    continue;

                ally.SetMovable(true);
            }
        }

        private void UpdateStageTicks(
            float elapsed,
            float ticksStart,
            float tickInterval,
            int tickSteps,
            int start,
            int end)
        {
            if (elapsed < ticksStart)
                return;

            int shouldHavePlayed = Mathf.Min(
                tickSteps,
                Mathf.FloorToInt((elapsed - ticksStart) / tickInterval) + 1);

            while (_ticksPlayed < shouldHavePlayed)
            {
                int stageValue = start + _ticksPlayed + 1;
                if (stageValue > end)
                    stageValue = end;

                if (stageNumberText != null)
                    stageNumberText.text = stageValue.ToString();

                PlayTickSound(stageValue);
                _lastTickPunchTime = elapsed;
                _ticksPlayed++;
            }
        }

        private void ApplyNumberPunch(float elapsed)
        {
            if (stageNumberText == null)
                return;

            float sincePunch = elapsed - _lastTickPunchTime;
            if (sincePunch < 0f || sincePunch > tickPunchDuration)
            {
                stageNumberText.transform.localScale = _numberBaseScale;
                return;
            }

            float half = tickPunchDuration * 0.5f;
            Vector3 peak = _numberBaseScale * tickPunchScale;
            Transform t = stageNumberText.transform;

            if (sincePunch < half)
            {
                float k = sincePunch / half;
                t.localScale = Vector3.Lerp(_numberBaseScale, peak, k);
            }
            else
            {
                float k = (sincePunch - half) / half;
                t.localScale = Vector3.Lerp(peak, _numberBaseScale, k);
            }
        }

        private void PlayTickSound(int stage)
        {
            if (tickClip == null || SfxPlayer.Instance == null)
                return;

            SfxPlayer.Instance.Play(tickClip, tickVolume, 1f + (stage % 3) * 0.04f);
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void EnsureActiveHierarchy()
        {
            Transform t = transform;
            while (t != null)
            {
                if (!t.gameObject.activeSelf)
                    t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        private void HideOverlayOnly()
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = 0f;
                overlayGroup.blocksRaycasts = false;
                overlayGroup.interactable = false;
            }

            if (stageNumberText != null)
                stageNumberText.transform.localScale = _numberBaseScale;

            if (panelRoot != null)
                panelRoot.gameObject.SetActive(true);
        }

        private void HideImmediate()
        {
            ResetCameraScroll();
            HideOverlayOnly();
        }
    }
}
