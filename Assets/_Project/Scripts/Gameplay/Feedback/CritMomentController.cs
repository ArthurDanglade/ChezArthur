using System.Collections;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// 3e pic timeScale sanctionné (charte §5.1, avenant F4) — timers unscaled,
    /// leçon StageAnnouncerUI. Crit-kill ou top-dégâts (≥ fraction PV max).
    /// </summary>
    public class CritMomentController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static CritMomentController Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED
        // ═══════════════════════════════════════════
        [SerializeField] private float _topDamageFraction = 0.25f;
        [SerializeField] private float _dipScale = 0.35f;
        [SerializeField] private float _dipDuration = 0.12f;
        [SerializeField] private float _cooldownSeconds = 2f;

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════
        private float _nextAllowedUnscaledTime;
        private Coroutine _dipRoutine;
        private bool _dipActive;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary> Crée l'instance DontDestroyOnLoad au premier usage. </summary>
        public static CritMomentController EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            GameObject go = new GameObject("[CritMoment]");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<CritMomentController>();
            return Instance;
        }

        /// <summary>
        /// Tente le dip + punch caméra. Skip si timeScale ≠ 1, cooldown, ou sous le seuil.
        /// </summary>
        public void TryPlay(bool killPredicted, int damage, int targetMaxHp)
        {
            if (Time.timeScale != 1f)
                return;
            if (Time.unscaledTime < _nextAllowedUnscaledTime)
                return;

            bool topDamage = targetMaxHp > 0
                && damage >= targetMaxHp * _topDamageFraction;
            if (!killPredicted && !topDamage)
                return;

            _nextAllowedUnscaledTime = Time.unscaledTime + _cooldownSeconds;

            if (_dipRoutine != null)
                StopCoroutine(_dipRoutine);
            _dipRoutine = StartCoroutine(DipRoutine());

            ArenaCamera cam = Object.FindObjectOfType<ArenaCamera>();
            cam?.PlayCritPunch();
        }

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDisable()
        {
            RestoreTimeScaleIfOurs();
            _dipRoutine = null;
            _dipActive = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            RestoreTimeScaleIfOurs();
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════
        private IEnumerator DipRoutine()
        {
            _dipActive = true;
            Time.timeScale = _dipScale;

            yield return new WaitForSecondsRealtime(_dipDuration);

            RestoreTimeScaleIfOurs();
            _dipActive = false;
            _dipRoutine = null;
        }

        private void RestoreTimeScaleIfOurs()
        {
            // Ne pas écraser finisher / défaite qui auraient pris la main.
            if (_dipActive && Mathf.Approximately(Time.timeScale, _dipScale))
                Time.timeScale = 1f;
        }
    }
}
