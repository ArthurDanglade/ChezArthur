using System.Collections;
using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Feedback;

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
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private ArenaCamera _arenaCamera;

        [Header("Catalogue Feedback (F2-P2b)")]
        [Tooltip("Source data du groupe A (clips / prefabs de burst / volumes de base). Câblé par builder.")]
        [SerializeField] private FeedbackCatalog _feedbackCatalog;

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

        [Header("SFX — Super Lancer")]
        [Tooltip("Son de tension au début du gel — null = silence")]
        [SerializeField] private AudioClip _superChargeClip;
        [SerializeField] private float _superChargeVolume = 0.5f;
        [Tooltip("SFX dédié du Super Lancer — null = swoosh normal en attendant l'asset")]
        [SerializeField] private AudioClip _superLaunchClip;
        [SerializeField] private float _superLaunchVolume = 0.85f;
        [SerializeField] private float _superLaunchPitchVariation = 0.05f;
        [Tooltip("Trauma caméra AJOUTÉ au trauma de lancer normal sur un Super Lancer")]
        [SerializeField] private float _superLaunchExtraTrauma = 0.15f;
        [Tooltip("Multiplicateur d'échelle du burst de propulsion sur un Super Lancer")]
        [SerializeField] private float _superBurstScale = 1.4f;
        [Tooltip("Durée (s) du gel de relâche : la bille reste figée à pleine charge avant de partir. 0 = désactivé.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _superLaunchFreezeDuration = 0.09f;
        [Tooltip("Seconde couche jouée AVEC le SFX de détonation — typiquement un impact grave/sub. Null = couche unique.")]
        [SerializeField] private AudioClip _superDetonationLayerClip;
        [SerializeField] private float _superDetonationLayerVolume = 0.7f;

        [Header("SFX — tension de visée (Super Lancer)")]
        [Tooltip("Boucle jouée pendant la visée — placeholder : Vinyle effet loop")]
        [SerializeField] private AudioClip _aimTensionLoopClip;
        [Tooltip("Distance angulaire (°) au bord de la zone à laquelle la tension commence à monter")]
        [SerializeField] private float _aimTensionRampDeg = 120f;
        [SerializeField] private float _tensionVolumeMin = 0.05f;
        [SerializeField] private float _tensionVolumeMax = 0.5f;
        [SerializeField] private float _tensionPitchMin = 0.8f;
        [SerializeField] private float _tensionPitchMax = 1.6f;
        [SerializeField] private AudioClip _zoneEnterTickClip;
        [SerializeField] private float _zoneEnterTickVolume = 0.3f;
        [SerializeField] private float _zoneEnterTickPitch = 1.8f;

        [Header("Duck musique pendant visée")]
        [Tooltip("Durée du fondu vers le snapshot AimFocus / Normal (s).")]
        [SerializeField] private float _aimMusicDuckFadeSeconds = 0.35f;

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

        [Header("Escalade par hit (lancer en cours)")]
        [SerializeField] private float _comboPitchPerHit = 0.06f;
        [SerializeField] private int _comboPitchMaxSteps = 10;
        [SerializeField] private float _comboShakePerHit = 0.08f;
        [SerializeField] private int _comboShakeMaxSteps = 8;

        [Header("Slow-mo de fin d'étage")]
        [SerializeField] private float _stageClearSlowScale = 0.08f;
        [SerializeField] private float _stageClearSlowHold = 0.5f;
        [SerializeField] private float _stageClearSlowRamp = 0.55f;
        [SerializeField] private float _finisherAnticipationSeconds = 0.15f;

        [Header("Game over — beat avant écran fin de run")]
        [SerializeField] private float _defeatSlowScale = 0.12f;
        [SerializeField] private float _defeatSlowHold = 0.35f;
        [SerializeField] private float _defeatShakeTrauma = 0.35f;
        [Tooltip("SFX unique au game over (pas à chaque mort d'allié) — null = silence")]
        [SerializeField] private AudioClip _defeatStampClip;
        [SerializeField] private float _defeatStampVolume = 0.85f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _baseFixedDelta = 0.02f;
        private Coroutine _slowMoRoutine;
        private Coroutine _defeatBeatRoutine;
        private Coroutine _finisherWaitRoutine;
        private bool _stageFinisherActive;
        private bool _finisherZoomDone;
        private bool _finisherSlowMoDone;
        private System.Action _pendingStageComplete;
        private AudioSource _tensionSource;
        private bool _wasInZone;

        // Bundles catalogue groupe A (résolus paresseusement — F2-P2b)
        private FeedbackBundle _launchBundle;
        private FeedbackBundle _bounceBundle;
        private FeedbackBundle _hitBundle;
        private FeedbackBundle _critBundle;
        private FeedbackBundle _killBundle;
        private FeedbackBundle _defeatBundle;
        private bool _catalogBundlesResolved;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Gel court du lanceur proportionnel aux dégâts infligés (temps réel, pas de timeScale).
        /// </summary>
        public void PlayHitEnemy(CharacterBall attacker, Enemy enemy, int damage, bool isCrit, Vector2 contactPoint, Vector2 contactNormal, float impactSpeed)
        {
            if (attacker == null) return;

            // Escalade sensorielle indexée sur les hits du lancer en cours
            // (index 1 = premier hit → aucun bonus, comportement identique à l'ancien premier hit de combo).
            int launchHitIndex = attacker.EnemyHitCountThisLaunch;
            float comboPitchBonus = _comboPitchPerHit * Mathf.Clamp(launchHitIndex - 1, 0, _comboPitchMaxSteps);
            float comboShakeMult = 1f + _comboShakePerHit * Mathf.Clamp(launchHitIndex - 1, 0, _comboShakeMaxSteps);

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

            Debug.Log($"[Juice] Hitstop {duration * 1000f:0}ms (dmg {damage}{(isCrit ? " CRIT" : "")}, launchHit {launchHitIndex})");
        }

        /// <summary>
        /// Swoosh de lancer, kick caméra et souffle de propulsion (opposé au tir).
        /// </summary>
        public void PlayLaunch(CharacterBall attacker, Vector2 direction, float speed, bool isSuper = false)
        {
            if (attacker == null) return;

            if (!isSuper)
            {
                PlayLaunchNormal(attacker, direction, speed);
                return;
            }

            StartCoroutine(SuperLaunchSequence(attacker, direction));
        }

        /// <summary> Swoosh, burst et secousse pour un lancer normal. </summary>
        private void PlayLaunchNormal(CharacterBall attacker, Vector2 direction, float speed)
        {
            Vector2 position = attacker.transform.position;

            AudioClip clip = GetLaunchClip();
            if (clip != null)
            {
                float t = Mathf.Clamp01(speed / _speedForMaxLaunchPitch);
                float pitch = Mathf.Lerp(_launchPitchLow, _launchPitchHigh, t)
                    + Random.Range(-_launchPitchVariation, _launchPitchVariation);

                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Moments, clip, GetLaunchVolume(), pitch, 3);
            }

            _cameraShake?.AddTrauma(_launchShakeTrauma);
            SpawnLaunchBurst(position, direction, speed, false);
        }

        /// <summary> Séquence Super Lancer en deux temps : charge (gel + tension) puis détonation. </summary>
        private IEnumerator SuperLaunchSequence(CharacterBall attacker, Vector2 dir)
        {
            // TEMPS 1 — LA CHARGE : la bille se fige comprimée, tension audible.
            attacker.ApplyHitStop(_superLaunchFreezeDuration);
            attacker.PlaySuperChargeVisual(_superLaunchFreezeDuration);
            if (_superChargeClip != null && _superLaunchFreezeDuration > 0.01f)
            {
                // Le riser est compressé/étiré pour tenir EXACTEMENT dans le gel :
                // sa crête tombe sur la détonation, quel que soit l'asset déposé
                // et quelle que soit la durée de gel réglée en tuning.
                float syncPitch = Mathf.Clamp(
                    _superChargeClip.length / _superLaunchFreezeDuration, 0.5f, 3f);
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Moments, _superChargeClip, _superChargeVolume, syncPitch, 5);
            }
            yield return new WaitForSecondsRealtime(_superLaunchFreezeDuration);

            // TEMPS 2 — LA DÉTONATION : tout part à l'instant exact du départ.
            if (attacker == null || !attacker.isActiveAndEnabled) yield break;

            float speed = attacker.CurrentVelocity;
            Vector2 position = attacker.transform.position;

            if (_superLaunchClip != null)
            {
                float pitch = 1f + Random.Range(-_superLaunchPitchVariation, _superLaunchPitchVariation);
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Moments, _superLaunchClip, _superLaunchVolume, pitch, 5);
            }
            else
            {
                AudioClip launchClip = GetLaunchClip();
                if (launchClip != null)
                {
                    float t = Mathf.Clamp01(speed / _speedForMaxLaunchPitch);
                    float pitch = Mathf.Lerp(_launchPitchLow, _launchPitchHigh, t)
                        + Random.Range(-_launchPitchVariation, _launchPitchVariation);

                    CombatFeedbackService.PlaySfxGuarded(
                        FeedbackBundle.VoiceFamily.Moments, launchClip, GetLaunchVolume(), pitch, 5);
                }
            }

            if (_superDetonationLayerClip != null)
            {
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Moments,
                    _superDetonationLayerClip,
                    _superDetonationLayerVolume,
                    1f,
                    5);
            }
            // Deux one-shots empilés : le crack (haut) + le poids (bas). C'est la
            // superposition qui fait le BAM, pas le volume d'un seul clip.

            _cameraShake?.AddTrauma(_launchShakeTrauma + _superLaunchExtraTrauma);
            SpawnLaunchBurst(position, dir, speed, true);
            attacker.PlayLaunchStretchVisual(dir);
        }

        /// <summary> Souffle de propulsion opposé au tir. </summary>
        private void SpawnLaunchBurst(Vector2 position, Vector2 direction, float speed, bool isSuper)
        {
            ParticleSystem prefab = GetLaunchBurstPrefab();
            if (prefab == null || direction.sqrMagnitude <= 0.0001f) return;

            Vector2 exhaust = -direction.normalized;
            Quaternion rot = exhaust.sqrMagnitude > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, (Vector3)exhaust)
                : Quaternion.identity;

            float intensity = Mathf.Clamp01(speed / _launchBurstSpeedRef);
            float scale = Mathf.Lerp(0.7f, 1.3f, intensity);
            if (isSuper)
                scale *= _superBurstScale;

            CombatFeedbackService.SpawnFxGuarded(
                prefab,
                position,
                rot,
                scale,
                isSuper ? 5 : 3);
        }

        /// <summary> Démarre la boucle de tension pendant la visée Super Lancer. </summary>
        public void BeginAimTension()
        {
            _wasInZone = false;
            BeginMusicDuck();

            if (_aimTensionLoopClip == null) return;

            AudioSource src = GetTensionSource();
            src.clip = _aimTensionLoopClip;
            src.volume = _tensionVolumeMin;
            src.pitch = _tensionPitchMin;
            src.Play();
        }

        /// <summary> Met à jour volume/pitch de la tension et joue le tick d'entrée en zone. </summary>
        public void UpdateAimTension(float degreesToZoneEdge, bool isInZone)
        {
            // degreesToZoneEdge : distance angulaire restante avant le bord de la zone (0 = dedans).
            // La géométrie vit dans SuperLancerSystem, le mapping sensoriel ici —
            // aucun des deux ne connaît le métier de l'autre.
            float tension01 = 1f - Mathf.Clamp01(degreesToZoneEdge / Mathf.Max(1f, _aimTensionRampDeg));

            if (_tensionSource != null && _tensionSource.isPlaying)
            {
                _tensionSource.volume = Mathf.Lerp(_tensionVolumeMin, _tensionVolumeMax, tension01);
                _tensionSource.pitch = Mathf.Lerp(_tensionPitchMin, _tensionPitchMax, tension01);
            }

            // Tick d'entrée : rejoue à chaque passage en zone — affordance auditive du « maintenant ».
            if (isInZone && !_wasInZone && _zoneEnterTickClip != null)
            {
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.UI,
                    _zoneEnterTickClip,
                    _zoneEnterTickVolume,
                    _zoneEnterTickPitch,
                    2);
            }

            _wasInZone = isInZone;
        }

        /// <summary> Coupe la tension de visée (cancel ou release). Aucun son d'échec. </summary>
        public void EndAimTension()
        {
            if (_tensionSource != null)
                _tensionSource.Stop();

            EndMusicDuck();
            _wasInZone = false;
        }

        /// <summary>
        /// Rebond mur avec volume selon l'impact et pitch montant par rebond consécutif.
        /// </summary>
        public void PlayBounceWall(Vector2 contactPoint, float impactSpeed, int bounceCount)
        {
            AudioClip[] clips = GetBounceClips();
            if (clips == null || clips.Length == 0) return;

            float t = Mathf.Clamp01(impactSpeed / _speedForMaxBounceVolume);
            float volume = Mathf.Lerp(_bounceVolumeMin, _bounceVolumeMax, t);
            int steps = Mathf.Min(Mathf.Max(0, bounceCount - 1), _bouncePitchMaxSteps);
            float pitch = _bounceBasePitch + steps * _bouncePitchPerHit
                + Random.Range(-_bouncePitchVariation, _bouncePitchVariation);

            CombatFeedbackService.PlaySfxGuarded(
                FeedbackBundle.VoiceFamily.Impacts,
                clips[Random.Range(0, clips.Length)],
                volume,
                pitch,
                2);
        }

        /// <summary>
        /// Burst, SFX, shake et hitstop prolongé à la mort d'un ennemi.
        /// </summary>
        public void PlayKill(Vector2 position)
        {
            ParticleSystem deathPrefab = GetDeathBurstPrefab();
            if (deathPrefab != null)
            {
                CombatFeedbackService.SpawnFxGuarded(
                    deathPrefab, position, Quaternion.identity, 1f, 5);
            }

            AudioClip killClip = GetKillClip();
            if (killClip != null)
            {
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Impacts, killClip, 1f, 1f, 5);
            }
            else
            {
                AudioClip[] hitClips = GetHitClips();
                if (hitClips != null && hitClips.Length > 0)
                {
                    // Placeholder : réutilise un hit court, plus grave, en attendant un vrai SFX de mort.
                    AudioClip fallback = hitClips[Random.Range(0, hitClips.Length)];
                    CombatFeedbackService.PlaySfxGuarded(
                        FeedbackBundle.VoiceFamily.Impacts, fallback, 0.9f, 0.75f, 5);
                }
            }

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
        /// Beat dramatique au game over : slow-mo court, shake, SFX stamp (placeholder optionnel).
        /// </summary>
        public void PlayDefeatBeat(System.Action onComplete)
        {
            if (_defeatBeatRoutine != null)
                StopCoroutine(_defeatBeatRoutine);
            _defeatBeatRoutine = StartCoroutine(DefeatBeatRoutine(onComplete));
        }

        /// <summary>
        /// Remet à zéro le finisher entre deux étages (timeScale + flags).
        /// </summary>
        public void ResetForNewStage()
        {
            if (_slowMoRoutine != null) StopCoroutine(_slowMoRoutine);
            if (_defeatBeatRoutine != null) StopCoroutine(_defeatBeatRoutine);
            if (_finisherWaitRoutine != null) StopCoroutine(_finisherWaitRoutine);
            _slowMoRoutine = null;
            _defeatBeatRoutine = null;
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

        private IEnumerator DefeatBeatRoutine(System.Action onComplete)
        {
            _cameraShake?.AddTrauma(_defeatShakeTrauma);

            AudioClip stamp = GetDefeatStampClip();
            if (stamp != null)
            {
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Moments, stamp, GetDefeatStampVolume(), 1f, 6);
            }

            Time.timeScale = _defeatSlowScale;
            Time.fixedDeltaTime = _baseFixedDelta * _defeatSlowScale;
            yield return new WaitForSecondsRealtime(_defeatSlowHold);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _baseFixedDelta;
            _defeatBeatRoutine = null;
            onComplete?.Invoke();
        }

        private void SpawnImpactBurst(Vector2 pos, Vector2 normal, int damage, bool isCrit)
        {
            ParticleSystem prefab = GetImpactBurstPrefab();
            if (prefab == null) return;

            Quaternion rot = normal.sqrMagnitude > 0.001f
                ? Quaternion.FromToRotation(Vector3.up, (Vector3)normal.normalized)
                : Quaternion.identity;

            ParticleSystem ps = CombatFeedbackService.SpawnFxGuarded(
                prefab, pos, rot, 1f, isCrit ? 5 : 4, play: false);
            if (ps == null)
                return;

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

        private void PlayHitSfx(int damage, bool isCrit, float comboPitchBonus)
        {
            float t = Mathf.Clamp01(damage / _damageForMaxHitStop);
            float volume = Mathf.Lerp(_hitVolumeMin, _hitVolumeMax, t);
            float pitch = Mathf.Lerp(_hitPitchHigh, _hitPitchLow, t) + Random.Range(-_pitchVariation, _pitchVariation);
            pitch += comboPitchBonus;

            AudioClip critClip = GetCritClip();
            if (isCrit && critClip != null)
            {
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Impacts, critClip, volume, pitch, 5);
                return;
            }

            AudioClip[] hitClips = GetHitClips();
            if (hitClips != null && hitClips.Length > 0)
            {
                CombatFeedbackService.PlaySfxGuarded(
                    FeedbackBundle.VoiceFamily.Impacts,
                    hitClips[Random.Range(0, hitClips.Length)],
                    volume,
                    pitch,
                    4);
            }
        }

        // ═══════════════════════════════════════════
        // CATALOGUE GROUPE A — accès à repli legacy (F2-P2b)
        // ═══════════════════════════════════════════

        private void ResolveCatalogBundlesIfNeeded()
        {
            if (_catalogBundlesResolved)
                return;

            _catalogBundlesResolved = true;
            if (_feedbackCatalog == null)
                return;

            _launchBundle = _feedbackCatalog.Resolve(FeedbackEventId.AllyLaunch, null);
            _bounceBundle = _feedbackCatalog.Resolve(FeedbackEventId.WallBounce, null);
            _hitBundle = _feedbackCatalog.Resolve(FeedbackEventId.HitEnemy, null);
            _critBundle = _feedbackCatalog.Resolve(FeedbackEventId.Crit, null);
            _killBundle = _feedbackCatalog.Resolve(FeedbackEventId.Kill, null);
            _defeatBundle = _feedbackCatalog.Resolve(FeedbackEventId.DefeatBeat, null);
        }

        private AudioClip PickClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            return clips[Random.Range(0, clips.Length)];
        }

        private AudioClip[] GetHitClips()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_hitBundle != null && _hitBundle.HasSfx)
                return _hitBundle.clips;
            return _hitClips;
        }

        private AudioClip GetCritClip()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_critBundle != null && _critBundle.HasSfx)
                return PickClip(_critBundle.clips);
            return _critClip;
        }

        private AudioClip[] GetBounceClips()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_bounceBundle != null && _bounceBundle.HasSfx)
                return _bounceBundle.clips;
            return _wallBounceClips;
        }

        private AudioClip GetKillClip()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_killBundle != null && _killBundle.HasSfx)
                return PickClip(_killBundle.clips);
            return _killClip;
        }

        private AudioClip GetLaunchClip()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_launchBundle != null && _launchBundle.HasSfx)
                return PickClip(_launchBundle.clips);
            return _launchClip;
        }

        private float GetLaunchVolume()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_launchBundle != null && _launchBundle.HasSfx)
                return _launchBundle.volumeScale;
            return _launchVolume;
        }

        private AudioClip GetDefeatStampClip()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_defeatBundle != null && _defeatBundle.HasSfx)
                return PickClip(_defeatBundle.clips);
            return _defeatStampClip;
        }

        private float GetDefeatStampVolume()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_defeatBundle != null && _defeatBundle.HasSfx)
                return _defeatBundle.volumeScale;
            return _defeatStampVolume;
        }

        private ParticleSystem GetImpactBurstPrefab()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_hitBundle != null && _hitBundle.HasVfx)
                return _hitBundle.vfxPrefab;
            return _impactBurstPrefab;
        }

        private ParticleSystem GetLaunchBurstPrefab()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_launchBundle != null && _launchBundle.HasVfx)
                return _launchBundle.vfxPrefab;
            return _launchBurstPrefab;
        }

        private ParticleSystem GetDeathBurstPrefab()
        {
            ResolveCatalogBundlesIfNeeded();
            if (_killBundle != null && _killBundle.HasVfx)
                return _killBundle.vfxPrefab;
            return _deathBurstPrefab;
        }

        /// <summary> Source dédiée à la boucle de tension (incompatible avec le pool one-shot SfxPlayer). </summary>
        private AudioSource GetTensionSource()
        {
            if (_tensionSource == null)
            {
                var go = new GameObject("AimTensionSource");
                go.transform.SetParent(transform, false);
                _tensionSource = go.AddComponent<AudioSource>();
                _tensionSource.loop = true;
                _tensionSource.playOnAwake = false;
                _tensionSource.spatialBlend = 0f;
                if (AudioBuses.SfxGroup != null)
                    _tensionSource.outputAudioMixerGroup = AudioBuses.SfxGroup;
            }

            return _tensionSource;
        }

        /// <summary> Baisse la musique pour laisser passer la tension de visée (snapshot AimFocus). </summary>
        private void BeginMusicDuck()
        {
            AudioBuses.TransitionToAim(_aimMusicDuckFadeSeconds);
        }

        /// <summary> Restaure le volume musique après visée ou annulation (snapshot Normal). </summary>
        private void EndMusicDuck()
        {
            AudioBuses.TransitionToNormal(_aimMusicDuckFadeSeconds);
        }
    }
}
