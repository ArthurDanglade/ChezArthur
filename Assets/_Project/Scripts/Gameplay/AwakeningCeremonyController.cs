using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Orchestre la cérémonie d'éveil SSR (dissolve déchu → prime) en fin de run.
    /// Singleton de scène (pattern CombatStatsTracker). Temps entièrement unscaled.
    /// </summary>
    public class AwakeningCeremonyController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string DissolveAmountProperty = "_DissolveAmount";

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private AwakeningCeremonyView overlayPrefab;
        [SerializeField] private Material dissolveMaterial;
        [SerializeField] private AudioClip ceremonyClip;
        [SerializeField] private float introDuration = 1.2f;
        [SerializeField] private float dissolveDuration = 1.8f;
        [SerializeField] private float outroDuration = 2f;
        [SerializeField] private float fadeDuration = 0.3f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static AwakeningCeremonyController _instance;

        private AwakeningCeremonyView _overlayInstance;
        private Material _runtimeDissolveMat;
        private Coroutine _playRoutine;
        private bool _skipRequested;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static AwakeningCeremonyController Instance => _instance;

        public bool IsPlaying { get; private set; }

        /// <summary>
        /// True s'il reste des éveils à cérémonialiser (prime + déchu disponibles).
        /// </summary>
        public bool HasPendingCeremonies
        {
            get
            {
                if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                    return false;

                IReadOnlyList<OwnedCharacter> ownedList =
                    PersistentManager.Instance.Characters.GetOwnedCharacters();
                if (ownedList == null)
                    return false;

                for (int i = 0; i < ownedList.Count; i++)
                {
                    if (TryGetPendingCeremony(ownedList[i], out _, out _))
                        return true;
                }

                return false;
            }
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (_runtimeDissolveMat != null)
            {
                Destroy(_runtimeDissolveMat);
                _runtimeDissolveMat = null;
            }

            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Joue toutes les cérémonies en attente, puis invoque onComplete.
        /// </summary>
        public void PlayCeremonies(Action onComplete)
        {
            if (IsPlaying)
                return;

            if (!HasPendingCeremonies)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureOverlay();
            if (_overlayInstance == null)
            {
                Debug.LogError("[AwakeningCeremonyController] Overlay introuvable.");
                onComplete?.Invoke();
                return;
            }

            IsPlaying = true;
            _playRoutine = StartCoroutine(PlayCeremoniesRoutine(onComplete));
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureOverlay()
        {
            if (_overlayInstance != null)
                return;

            if (overlayPrefab == null)
            {
                Debug.LogError("[AwakeningCeremonyController] overlayPrefab non assigné.");
                return;
            }

            _overlayInstance = Instantiate(overlayPrefab);
            _overlayInstance.gameObject.SetActive(false);

            if (_overlayInstance.TapButton != null)
            {
                _overlayInstance.TapButton.onClick.RemoveAllListeners();
                _overlayInstance.TapButton.onClick.AddListener(OnTapRequested);
            }
        }

        private void OnTapRequested()
        {
            if (IsPlaying)
                _skipRequested = true;
        }

        private IEnumerator PlayCeremoniesRoutine(Action onComplete)
        {
            _overlayInstance.gameObject.SetActive(true);
            yield return FadeCanvas(0f, 1f);

            List<(OwnedCharacter owned, CharacterData data)> pending = CollectPending();
            for (int i = 0; i < pending.Count; i++)
            {
                OwnedCharacter owned = pending[i].owned;
                CharacterData data = pending[i].data;
                yield return PlayOneCeremony(owned, data);
            }

            yield return FadeCanvas(1f, 0f);
            _overlayInstance.gameObject.SetActive(false);

            IsPlaying = false;
            _playRoutine = null;
            onComplete?.Invoke();
        }

        private List<(OwnedCharacter owned, CharacterData data)> CollectPending()
        {
            var list = new List<(OwnedCharacter, CharacterData)>();
            IReadOnlyList<OwnedCharacter> ownedList =
                PersistentManager.Instance.Characters.GetOwnedCharacters();

            for (int i = 0; i < ownedList.Count; i++)
            {
                if (TryGetPendingCeremony(ownedList[i], out OwnedCharacter owned, out CharacterData data))
                    list.Add((owned, data));
            }

            return list;
        }

        private static bool TryGetPendingCeremony(
            OwnedCharacter owned,
            out OwnedCharacter persisted,
            out CharacterData data)
        {
            persisted = null;
            data = null;

            if (owned == null || !owned.isAwakened || owned.awakeningCeremonySeen)
                return false;

            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return false;

            // Instance persistée (Gate 2) — réécriture ceremonySeen dessus.
            persisted = PersistentManager.Instance.Characters.GetOwnedCharacter(owned.characterId);
            if (persisted == null)
                return false;

            var pair = PersistentManager.Instance.Characters.GetCharacterWithData(owned.characterId);
            data = pair.data;
            if (data == null
                || data.AnimatedPortraitPrime == null
                || data.AnimatedPortraitDechu == null)
            {
                return false;
            }

            return true;
        }

        private IEnumerator PlayOneCeremony(OwnedCharacter owned, CharacterData data)
        {
            AwakeningCeremonyView view = _overlayInstance;

            if (view.NameText != null)
                view.NameText.text = data.CharacterName;

            view.PrimeView?.ShowState(data, data.AnimatedPortraitPrime);
            view.DechuView?.ShowState(data, data.AnimatedPortraitDechu);

            EnsureRuntimeMaterial();
            if (view.DechuRawImage != null && _runtimeDissolveMat != null)
            {
                _runtimeDissolveMat.SetFloat(DissolveAmountProperty, 0f);
                view.DechuRawImage.material = _runtimeDissolveMat;
            }

            // Phase A — intro (les deux animent)
            _skipRequested = false;
            yield return WaitOrSkip(introDuration);

            // Phase B — dissolve
            view.PrimeView?.SetAnimationPaused(true);
            view.DechuView?.SetAnimationPaused(true);

            if (ceremonyClip != null)
                SfxManager.Instance?.PlaySfx(ceremonyClip);

            _skipRequested = false;
            yield return AnimateDissolve(0f, 1f, dissolveDuration);

            if (_runtimeDissolveMat != null)
                _runtimeDissolveMat.SetFloat(DissolveAmountProperty, 1f);

            // Phase C — outro (prime reprend)
            view.PrimeView?.SetAnimationPaused(false);
            _skipRequested = false;
            yield return WaitOrSkip(outroDuration);

            OwnedCharacter persisted =
                PersistentManager.Instance.Characters.GetOwnedCharacter(data.Id);
            if (persisted != null)
            {
                persisted.awakeningCeremonySeen = true;
                PersistentManager.Instance.SaveGame();
            }

            view.PrimeView?.Release();
            view.DechuView?.Release();

            if (view.DechuRawImage != null)
                view.DechuRawImage.material = null;
        }

        private void EnsureRuntimeMaterial()
        {
            if (_runtimeDissolveMat != null || dissolveMaterial == null)
                return;

            _runtimeDissolveMat = new Material(dissolveMaterial);
            _runtimeDissolveMat.SetFloat(DissolveAmountProperty, 0f);
        }

        private IEnumerator AnimateDissolve(float from, float to, float duration)
        {
            if (_runtimeDissolveMat == null || duration <= 0f)
            {
                if (_runtimeDissolveMat != null)
                    _runtimeDissolveMat.SetFloat(DissolveAmountProperty, to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_skipRequested)
                {
                    _skipRequested = false;
                    _runtimeDissolveMat.SetFloat(DissolveAmountProperty, to);
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOut(t);
                _runtimeDissolveMat.SetFloat(DissolveAmountProperty, Mathf.Lerp(from, to, eased));
                yield return null;
            }

            _runtimeDissolveMat.SetFloat(DissolveAmountProperty, to);
        }

        private IEnumerator WaitOrSkip(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_skipRequested)
                {
                    _skipRequested = false;
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator FadeCanvas(float from, float to)
        {
            CanvasGroup group = _overlayInstance != null ? _overlayInstance.CanvasGroup : null;
            if (group == null || fadeDuration <= 0f)
            {
                if (group != null)
                    group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                group.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            group.alpha = to;
        }

        private static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
