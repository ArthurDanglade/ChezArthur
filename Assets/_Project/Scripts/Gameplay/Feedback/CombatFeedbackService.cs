using System.Collections;
using ChezArthur.Audio;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Service runtime de feedback combat (catalogue + garde-fous + pool).
    /// Dormant tant que rien n'appelle Play (F2-P1).
    /// </summary>
    public class CombatFeedbackService : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int EventCount = FeedbackCatalog.EventCount;
        private const int MaxActiveFx = 12;
        private const int CapImpacts = 4;
        private const int CapStatuts = 2;
        private const int CapMoments = 2;
        private const int CapUI = 1;
        private const int StealEmphasis = 5;

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static CombatFeedbackService Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private FeedbackCatalog _catalog;
        [SerializeField] private CameraShake _cameraShake;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private FxPool _fxPool;
        private readonly float[] _lastPlayTime = new float[EventCount];
        private readonly bool[] _warnedMissing = new bool[EventCount];

        private readonly float[] _impactEnds = new float[CapImpacts];
        private readonly float[] _statutEnds = new float[CapStatuts];
        private readonly float[] _momentEnds = new float[CapMoments];
        private readonly float[] _uiEnds = new float[CapUI];

        // ═══════════════════════════════════════════
        // DIAGNOSTICS
        // ═══════════════════════════════════════════
        public int ActiveFxCount => _fxPool != null ? _fxPool.ActiveCount : 0;
        public int SkippedFx { get; private set; }
        public int SkippedVoices { get; private set; }
        public int SkippedCooldown { get; private set; }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Instance = this;

            Transform poolRoot = new GameObject("FxPoolRoot").transform;
            poolRoot.SetParent(transform, false);
            _fxPool = new FxPool(poolRoot);

            if (_catalog != null)
                _catalog.BuildRuntimeIndex();

            for (int i = 0; i < EventCount; i++)
                _lastPlayTime[i] = -999f;
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
        /// Joue un événement de feedback avec les garde-fous charte §3.
        /// </summary>
        public void Play(FeedbackEventId id, in FeedbackContext ctx)
        {
            int idx = (int)id;
            if (idx < 0 || idx >= EventCount)
                return;

            FeedbackBundle bundle = _catalog != null
                ? _catalog.Resolve(id, ctx.CharacterId)
                : null;

            if (bundle == null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!_warnedMissing[idx])
                {
                    _warnedMissing[idx] = true;
                    Debug.LogWarning($"[CombatFeedbackService] Pas de bundle pour {id}");
                }
#endif
                return;
            }

            float now = Time.unscaledTime;

            // 1) Cooldown
            float cd = bundle.cooldownMs * 0.001f;
            if (now - _lastPlayTime[idx] < cd)
            {
                SkippedCooldown++;
                return;
            }

            _lastPlayTime[idx] = now;

            // 2) VFX budget
            bool playVfx = bundle.HasVfx;
            if (playVfx && _fxPool != null && _fxPool.ActiveCount >= MaxActiveFx && bundle.emphasis < StealEmphasis)
            {
                playVfx = false;
                SkippedFx++;
            }

            if (playVfx)
                SpawnVfx(bundle, in ctx);

            // 3) SFX familles de voix
            if (bundle.HasSfx)
                TryPlaySfx(bundle, now);

            // 4) Shake / hitstop
            if (bundle.shakeTrauma > 0f && _cameraShake != null)
                _cameraShake.AddTrauma(bundle.shakeTrauma);

            if (bundle.hitstopMs > 0f && ctx.TargetBall != null)
                ctx.TargetBall.ApplyHitStop(bundle.hitstopMs * 0.001f);

            // 5) Haptic — réservé F4
        }

        // ═══════════════════════════════════════════
        // DEV HARNESS
        // ═══════════════════════════════════════════

        [ContextMenu("DEV — Log et reset compteurs")]
        private void DevLogResetCounters()
        {
            Debug.Log(
                $"[CombatFeedbackService] ActiveFx={ActiveFxCount} SkippedFx={SkippedFx} " +
                $"SkippedVoices={SkippedVoices} SkippedCooldown={SkippedCooldown}");
            SkippedFx = 0;
            SkippedVoices = 0;
            SkippedCooldown = 0;
        }

        [ContextMenu("DEV — Jouer tous les events (0,5 s)")]
        private void DevPlayAllEvents()
        {
            StartCoroutine(DevPlayAllRoutine());
        }

        [ContextMenu("DEV — Spam HitEnemy x20 (200 ms)")]
        private void DevSpamHitEnemy()
        {
            StartCoroutine(DevSpamHitRoutine());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SpawnVfx(FeedbackBundle bundle, in FeedbackContext ctx)
        {
            ParticleSystem ps = _fxPool.Get(bundle.vfxPrefab);
            if (ps == null)
                return;

            Transform t = ps.transform;
            t.position = new Vector3(ctx.Position.x, ctx.Position.y, 0f);

            if (ctx.Direction.sqrMagnitude > 0.0001f)
            {
                Vector3 dir = new Vector3(ctx.Direction.x, ctx.Direction.y, 0f).normalized;
                t.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            }
            else
            {
                t.rotation = Quaternion.identity;
            }

            t.localScale = Vector3.one * bundle.vfxScale;

            if (bundle.attachMode == FeedbackBundle.AttachMode.FollowTarget && ctx.Target != null)
                t.SetParent(ctx.Target, true);

            ApplyTint(ps, bundle);
            ps.Play(true);
        }

        private static void ApplyTint(ParticleSystem ps, FeedbackBundle bundle)
        {
            if (bundle.tintMode == FeedbackBundle.TintMode.None)
                return;

            Color c = bundle.tintMode == FeedbackBundle.TintMode.Custom
                ? bundle.customTint
                : CombatFeedbackPalette.GetColor(bundle.tintCause);

            ParticleSystem.MainModule main = ps.main;
            main.startColor = c;
        }

        private void TryPlaySfx(FeedbackBundle bundle, float now)
        {
            float[] ends = GetFamilyEnds(bundle.voiceFamily, out int cap);
            if (ends == null)
                return;

            int free = FindFreeSlot(ends, now, cap);
            if (free < 0)
            {
                if (bundle.emphasis < StealEmphasis)
                {
                    SkippedVoices++;
                    return;
                }

                free = FindOldestSlot(ends, cap);
            }

            AudioClip clip = PickClip(bundle.clips);
            if (clip == null)
                return;

            float pitch = Random.Range(bundle.pitchMin, bundle.pitchMax);
            if (pitch < 0.01f)
                pitch = 1f;

            if (SfxPlayer.Instance != null)
                SfxPlayer.Instance.Play(clip, bundle.volumeScale, pitch);

            float duration = clip.length / pitch;
            ends[free] = now + duration;
        }

        private float[] GetFamilyEnds(FeedbackBundle.VoiceFamily family, out int cap)
        {
            switch (family)
            {
                case FeedbackBundle.VoiceFamily.Impacts:
                    cap = CapImpacts;
                    return _impactEnds;
                case FeedbackBundle.VoiceFamily.Statuts:
                    cap = CapStatuts;
                    return _statutEnds;
                case FeedbackBundle.VoiceFamily.Moments:
                    cap = CapMoments;
                    return _momentEnds;
                case FeedbackBundle.VoiceFamily.UI:
                    cap = CapUI;
                    return _uiEnds;
                default:
                    cap = 0;
                    return null;
            }
        }

        private static int FindFreeSlot(float[] ends, float now, int cap)
        {
            for (int i = 0; i < cap; i++)
            {
                if (ends[i] <= now)
                    return i;
            }

            return -1;
        }

        private static int FindOldestSlot(float[] ends, int cap)
        {
            int oldest = 0;
            float minEnd = ends[0];
            for (int i = 1; i < cap; i++)
            {
                if (ends[i] < minEnd)
                {
                    minEnd = ends[i];
                    oldest = i;
                }
            }

            return oldest;
        }

        private static AudioClip PickClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            // Filtre nulls sans alloc : premier non-null aléatoire parmi non-null
            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    count++;
            }

            if (count == 0)
                return null;

            int pick = Random.Range(0, count);
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;
                if (pick == 0)
                    return clips[i];
                pick--;
            }

            return null;
        }

        private IEnumerator DevPlayAllRoutine()
        {
            FeedbackContext ctx = FeedbackContext.At(Vector2.zero);
            for (int i = 0; i < EventCount; i++)
            {
                var id = (FeedbackEventId)i;
                Play(id, in ctx);
                Debug.Log($"[CombatFeedbackService] DEV play {id}");
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        private IEnumerator DevSpamHitRoutine()
        {
            FeedbackContext ctx = FeedbackContext.At(Vector2.zero);
            for (int i = 0; i < 20; i++)
            {
                Play(FeedbackEventId.HitEnemy, in ctx);
                yield return new WaitForSecondsRealtime(0.01f);
            }

            Debug.Log(
                $"[CombatFeedbackService] Spam done — SkippedCooldown={SkippedCooldown} SkippedVoices={SkippedVoices}");
        }
    }
}
