using System.Collections;
using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Enemies;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Orchestrateur du game feel en combat (hitstop, shake, particules, SFX).
    /// Singleton de scène — à placer sur un GameObject de la scène combat.
    /// </summary>
    public class JuiceDirector : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static JuiceDirector Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CameraShake _cameraShake;
        [SerializeField] private ComboUI _comboUI;
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private ArenaCamera _arenaCamera;

        [Header("Combo persistant")]
        [SerializeField] private float _comboWindow = 3.5f;

        [Header("Hitstop — impact ennemi")]
        [SerializeField] private float _hitStopBase = 0.05f;
        [SerializeField] private float _hitStopMax = 0.10f;
        [SerializeField] private float _damageForMaxHitStop = 150f;
        [SerializeField] private float _critHitStopMultiplier = 1.3f;

        [Header("Screen shake — impact ennemi")]
        [SerializeField] private float _shakeTraumaMin = 0.25f;
        [SerializeField] private float _shakeTraumaMax = 0.55f;
        [SerializeField] private float _critShakeMultiplier = 1.4f;

        [Header("Particules — impact ennemi")]
        [SerializeField] private ParticleSystem _impactBurstPrefab;
        [SerializeField] private int _particleCountMin = 6;
        [SerializeField] private int _particleCountMax = 18;
        [SerializeField] private Color _hitParticleColor = Color.white;
        [SerializeField] private Color _critParticleColor = new Color(1f, 0.85f, 0.2f);

        [Header("SFX — impact ennemi")]
        [SerializeField] private AudioClip[] _hitClips;
        [SerializeField] private AudioClip _critClip;
        [SerializeField] private float _hitVolumeMin = 0.5f;
        [SerializeField] private float _hitVolumeMax = 1f;
        [SerializeField] private float _hitPitchHigh = 1.1f;
        [SerializeField] private float _hitPitchLow = 0.85f;
        [SerializeField] private float _pitchVariation = 0.06f;

        [Header("SFX — lancer")]
        [SerializeField] private AudioClip _launchClip;
        [SerializeField] private float _launchVolume = 0.7f;
        [SerializeField] private float _launchPitchLow = 0.9f;
        [SerializeField] private float _launchPitchHigh = 1.15f;
        [SerializeField] private float _speedForMaxLaunchPitch = 20f;
        [SerializeField] private float _launchPitchVariation = 0.05f;
        [SerializeField] private float _launchShakeTrauma = 0.18f;

        [Header("Burst de lâcher")]
        [SerializeField] private ParticleSystem _launchBurstPrefab;
        [SerializeField] private float _launchBurstSpeedRef = 120f;

        [Header("SFX — rebond mur")]
        [SerializeField] private AudioClip[] _wallBounceClips;
        [SerializeField] private float _bounceVolumeMin = 0.3f;
        [SerializeField] private float _bounceVolumeMax = 0.7f;
        [SerializeField] private float _bounceBasePitch = 1f;
        [SerializeField] private float _bouncePitchPerHit = 0.05f;
        [SerializeField] private int _bouncePitchMaxSteps = 8;
        [SerializeField] private float _bouncePitchVariation = 0.04f;
        [SerializeField] private float _speedForMaxBounceVolume = 15f;

        [Header("Kill")]
        [SerializeField] private ParticleSystem _deathBurstPrefab;
        [SerializeField] private AudioClip _killClip;
        [SerializeField] private float _killShakeTrauma = 0.5f;
        [SerializeField] private float _killHitStop = 0.14f;

        [Header("Escalade de combo")]
        [SerializeField] private float _comboPitchPerHit = 0.06f;
        [SerializeField] private int _comboPitchMaxSteps = 10;
        [SerializeField] private float _comboShakePerHit = 0.08f;
        [SerializeField] private int _comboShakeMaxSteps = 8;

        [Header("Slow-mo de fin d'étage")]
        [SerializeField] private float _stageClearSlowScale = 0.08f;
        [SerializeField] private float _stageClearSlowHold = 0.5f;
        [SerializeField] private float _stageClearSlowRamp = 0.55f;
        [SerializeField] private float _finisherAnticipationSeconds = 0.15f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _comboCount;
        private float _comboDecayTimer;
        private float _baseFixedDelta = 0.02f;
        private Coroutine _slowMoRoutine;
        private Coroutine _finisherWaitRoutine;
        private bool _stageFinisherActive;
        private bool _finisherZoomDone;
        private bool _finisherSlowMoDone;
        private System.Action _pendingStageComplete;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Nombre de coups ennemis dans la chaîne de ricochet cross-tour. </summary>
        public int ComboCount => _comboCount;

        /// <summary> Fenêtre (s) avant impact pour anticiper le finisher sur le dernier ennemi. </summary>
        public float FinisherAnticipationSeconds => _finisherAnticipationSeconds;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Instance = this;
            _baseFixedDelta = Time.fixedDeltaTime;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_comboCount <= 0) return;

            bool canAct = _turnManager != null && _turnManager.IsPlayerTurn
                       && _turnManager.HasCurrentParticipant && !_turnManager.CurrentParticipant.IsMoving;

            if (canAct)
            {
                _comboDecayTimer += Time.deltaTime;
                _comboUI?.SetTimer(_comboDecayTimer / _comboWindow);
                if (_comboDecayTimer >= _comboWindow) ResetCombo();
            }
            else
            {
                _comboUI?.SetTimer(0f);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Gel court du lanceur proportionnel aux dégâts infligés (temps réel, pas de timeScale).
        /// </summary>
        public void PlayHitEnemy(CharacterBall attacker, Enemy enemy, int damage, bool isCrit, Vector2 contactPoint, Vector2 contactNormal, float impactSpeed)
        {
            if (attacker == null) return;

            bool comboEligible = _turnManager != null && _turnManager.IsPlayerTurn;
            float comboPitchBonus = 0f;
            float comboShakeMult = 1f;

            if (comboEligible)
            {
                _comboCount++;
                _comboDecayTimer = 0f;
                _comboUI?.OnCombo(_comboCount);
                comboPitchBonus = _comboPitchPerHit * Mathf.Min(_comboCount - 1, _comboPitchMaxSteps);
                comboShakeMult = 1f + _comboShakePerHit * Mathf.Min(_comboCount - 1, _comboShakeMaxSteps);
            }

            float t = Mathf.Clamp01(damage / _damageForMaxHitStop);
            float duration = Mathf.Lerp(_hitStopBase, _hitStopMax, t);
            if (isCrit)
                duration *= _critHitStopMultiplier;

            attacker.ApplyHitStop(duration);

            if (_cameraShake != null)
            {
                float trauma = Mathf.Lerp(_shakeTraumaMin, _shakeTraumaMax, t);
                if (isCrit)
                    trauma *= _critShakeMultiplier;
                trauma *= comboShakeMult;
                _cameraShake.AddTrauma(trauma);
            }

            SpawnImpactBurst(contactPoint, contactNormal, damage, isCrit);

            PlayHitSfx(damage, isCrit, comboPitchBonus);

            Debug.Log($"[Juice] Hitstop {duration * 1000f:0}ms (dmg {damage}{(isCrit ? " CRIT" : "")}, combo {_comboCount})");
        }

        /// <summary>
        /// Swoosh de lancer, kick caméra et souffle de propulsion (opposé au tir).
        /// </summary>
        public void PlayLaunch(Vector2 position, Vector2 direction, float speed)
        {
            _comboDecayTimer = 0f;

            if (SfxPlayer.Instance != null && _launchClip != null)
            {
                float t = Mathf.Clamp01(speed / _speedForMaxLaunchPitch);
                float pitch = Mathf.Lerp(_launchPitchLow, _launchPitchHigh, t)
                    + Random.Range(-_launchPitchVariation, _launchPitchVariation);

                SfxPlayer.Instance.Play(_launchClip, _launchVolume, pitch);
            }

            _cameraShake?.AddTrauma(_launchShakeTrauma);

            if (_launchBurstPrefab != null && direction.sqrMagnitude > 0.0001f)
            {
                Vector2 exhaust = -direction.normalized;
                Quaternion rot = exhaust.sqrMagnitude > 0.001f
                    ? Quaternion.FromToRotation(Vector3.up, (Vector3)exhaust)
                    : Quaternion.identity;

                ParticleSystem burst = Instantiate(_launchBurstPrefab, (Vector3)position, rot);
                float intensity = Mathf.Clamp01(speed / _launchBurstSpeedRef);
                burst.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.3f, intensity);
                burst.Play();
            }
        }

        /// <summary>
        /// Rebond mur avec volume selon l'impact et pitch montant par rebond consécutif.
        /// </summary>
        public void PlayBounceWall(Vector2 contactPoint, float impactSpeed, int bounceCount)
        {
            if (SfxPlayer.Instance == null || _wallBounceClips == null || _wallBounceClips.Length == 0) return;

            float t = Mathf.Clamp01(impactSpeed / _speedForMaxBounceVolume);
            float volume = Mathf.Lerp(_bounceVolumeMin, _bounceVolumeMax, t);
            int steps = Mathf.Min(Mathf.Max(0, bounceCount - 1), _bouncePitchMaxSteps);
            float pitch = _bounceBasePitch + steps * _bouncePitchPerHit
                + Random.Range(-_bouncePitchVariation, _bouncePitchVariation);

            SfxPlayer.Instance.Play(
                _wallBounceClips[Random.Range(0, _wallBounceClips.Length)],
                volume,
                pitch);
        }

        /// <summary>
        /// Burst, SFX, shake et hitstop prolongé à la mort d'un ennemi.
        /// </summary>
        public void PlayKill(Vector2 position)
        {
            if (_deathBurstPrefab != null)
            {
                ParticleSystem burst = Instantiate(_deathBurstPrefab, position, Quaternion.identity);
                burst.Play();
            }

            if (_killClip != null && SfxPlayer.Instance != null)
                SfxPlayer.Instance.Play(_killClip, 1f, 1f);

            _cameraShake?.AddTrauma(_killShakeTrauma);

            if (_turnManager != null && _turnManager.HasCurrentParticipant
                && _turnManager.CurrentParticipant is CharacterBall ball && ball.IsMoving)
                ball.ApplyHitStop(_killHitStop);
        }

        /// <summary>
        /// Anticipe le finisher (slow-mo + zoom) avant le coup fatal sur le dernier ennemi.
        /// </summary>
        public void TryStartStageFinisher(Vector3 killPos)
        {
            if (_stageFinisherActive) return;
            _stageFinisherActive = true;
            _finisherZoomDone = false;
            _finisherSlowMoDone = false;

            if (_arenaCamera != null)
                _arenaCamera.PlayFinisherZoom(killPos, () => _finisherZoomDone = true);
            else
                _finisherZoomDone = true;

            if (_slowMoRoutine != null) StopCoroutine(_slowMoRoutine);
            _slowMoRoutine = StartCoroutine(SlowMoRoutine(() =>
            {
                _finisherSlowMoDone = true;
                _slowMoRoutine = null;
            }));
        }

        /// <summary>
        /// Fin d'étage : attend la fin du finisher avant de poursuivre (bonus / étage suivant).
        /// </summary>
        public void NotifyStageCleared(Vector3 killPos, System.Action onComplete)
        {
            _pendingStageComplete = onComplete;
            if (!_stageFinisherActive)
                TryStartStageFinisher(killPos);

            if (_finisherWaitRoutine != null) StopCoroutine(_finisherWaitRoutine);
            _finisherWaitRoutine = StartCoroutine(WaitForFinisherThenComplete());
        }

        /// <summary>
        /// Remet à zéro le finisher entre deux étages (timeScale + flags).
        /// </summary>
        public void ResetForNewStage()
        {
            if (_slowMoRoutine != null) StopCoroutine(_slowMoRoutine);
            if (_finisherWaitRoutine != null) StopCoroutine(_finisherWaitRoutine);
            _slowMoRoutine = null;
            _finisherWaitRoutine = null;
            _stageFinisherActive = false;
            _finisherZoomDone = false;
            _finisherSlowMoDone = false;
            _pendingStageComplete = null;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDelta;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator WaitForFinisherThenComplete()
        {
            while (!_finisherZoomDone || !_finisherSlowMoDone)
                yield return null;

            _stageFinisherActive = false;
            _finisherWaitRoutine = null;
            System.Action cb = _pendingStageComplete;
            _pendingStageComplete = null;
            cb?.Invoke();
        }

        private IEnumerator SlowMoRoutine(System.Action onComplete)
        {
            Time.timeScale = _stageClearSlowScale;
            Time.fixedDeltaTime = _baseFixedDelta * _stageClearSlowScale;
            yield return new WaitForSecondsRealtime(_stageClearSlowHold);

            float t = 0f;
            while (t < _stageClearSlowRamp)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(_stageClearSlowScale, 1f, t / _stageClearSlowRamp);
                Time.timeScale = s;
                Time.fixedDeltaTime = _baseFixedDelta * s;
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDelta;
            onComplete?.Invoke();
        }

        private void SpawnImpactBurst(Vector2 pos, Vector2 normal, int damage, bool isCrit)
        {
            if (_impactBurstPrefab == null) return;

            Quaternion rot = normal.sqrMagnitude > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, (Vector3)normal.normalized)
                : Quaternion.identity;

            ParticleSystem ps = Instantiate(_impactBurstPrefab, (Vector3)pos, rot);

            ParticleSystem.MainModule main = ps.main;
            main.startColor = isCrit ? _critParticleColor : _hitParticleColor;

            int count = Mathf.RoundToInt(Mathf.Lerp(
                _particleCountMin,
                _particleCountMax,
                Mathf.Clamp01(damage / _damageForMaxHitStop)));

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)count));

            ps.Play();
        }

        private void ResetCombo()
        {
            _comboCount = 0;
            _comboDecayTimer = 0f;
            _comboUI?.EndCombo();
        }

        private void PlayHitSfx(int damage, bool isCrit, float comboPitchBonus)
        {
            if (SfxPlayer.Instance == null) return;

            float t = Mathf.Clamp01(damage / _damageForMaxHitStop);
            float volume = Mathf.Lerp(_hitVolumeMin, _hitVolumeMax, t);
            float pitch = Mathf.Lerp(_hitPitchHigh, _hitPitchLow, t) + Random.Range(-_pitchVariation, _pitchVariation);
            pitch += comboPitchBonus;

            if (isCrit && _critClip != null)
            {
                SfxPlayer.Instance.Play(_critClip, volume, pitch);
                return;
            }

            if (_hitClips != null && _hitClips.Length > 0)
                SfxPlayer.Instance.Play(_hitClips[Random.Range(0, _hitClips.Length)], volume, pitch);
        }
    }
}
