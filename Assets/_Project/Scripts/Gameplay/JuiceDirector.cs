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

        [Header("Escalade de combo")]
        [SerializeField] private float _comboPitchPerHit = 0.06f;
        [SerializeField] private int _comboPitchMaxSteps = 10;
        [SerializeField] private float _comboShakePerHit = 0.08f;
        [SerializeField] private int _comboShakeMaxSteps = 8;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _comboCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Nombre de coups ennemis dans la chaîne de ricochet du lancer courant. </summary>
        public int ComboCount => _comboCount;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Instance = this;
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

            _comboCount++;
            _comboUI?.OnCombo(_comboCount);

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
                trauma *= 1f + _comboShakePerHit * Mathf.Min(_comboCount - 1, _comboShakeMaxSteps);
                _cameraShake.AddTrauma(trauma);
            }

            SpawnImpactBurst(contactPoint, contactNormal, damage, isCrit);

            PlayHitSfx(damage, isCrit);

            Debug.Log($"[Juice] Hitstop {duration * 1000f:0}ms (dmg {damage}{(isCrit ? " CRIT" : "")}, combo {_comboCount})");
        }

        /// <summary>
        /// Swoosh de lancer, kick caméra et souffle de propulsion (opposé au tir).
        /// </summary>
        public void PlayLaunch(Vector2 position, Vector2 direction, float speed)
        {
            _comboCount = 0;

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

        /// <summary> Stub — mort ennemi (étape suivante). </summary>
        public void PlayKill(CharacterBall killer, Enemy enemy, Vector2 position) { }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

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

        private void PlayHitSfx(int damage, bool isCrit)
        {
            if (SfxPlayer.Instance == null) return;

            float t = Mathf.Clamp01(damage / _damageForMaxHitStop);
            float volume = Mathf.Lerp(_hitVolumeMin, _hitVolumeMax, t);
            float pitch = Mathf.Lerp(_hitPitchHigh, _hitPitchLow, t) + Random.Range(-_pitchVariation, _pitchVariation);
            pitch += _comboPitchPerHit * Mathf.Min(_comboCount - 1, _comboPitchMaxSteps);

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
