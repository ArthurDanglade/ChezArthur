using System.Collections;
using ChezArthur.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Pile visuelle de la transition : carte, particules, rayons, halo, flash, vignette, shake local.
    /// </summary>
    public class ArtworkTransitionView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float RaysASpeed = 0.08f;
        private const float RaysBSpeed = -0.128f;
        private const float TraumaDecay = 1.6f;
        private const float FlashDecay = 3.4f;
        private const float LoopFadeDuration = 0.25f;
        private const float RefPreviewHeight = 640f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Hiérarchie")]
        [SerializeField] private RectTransform shaker;
        [SerializeField] private RawImage raysA;
        [SerializeField] private RawImage raysB;
        [SerializeField] private RawImage halo;
        [SerializeField] private ArtworkTransitionGraphic card;
        [SerializeField] private PixelParticleGraphic particlesAsh;
        [SerializeField] private PixelParticleGraphic particlesEnergy;
        [SerializeField] private Image vignette;
        [SerializeField] private Image flash;

        [Header("Stage")]
        [SerializeField] private RectTransform stageRoot;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private IPortraitFrameSource _frontSource;
        private IPortraitFrameSource _backSource;
        private Texture _lastFrontTex;
        private Texture _lastBackTex;
        private Rect _lastFrontUv;
        private Rect _lastBackUv;
        private float _trauma;
        private float _flash;
        private float _shakeIntensity = 0.55f;
        private AudioSource _oneshotSource;
        private AudioSource _loopSource;
        private Coroutine _loopFadeCo;
        private bool _warnedMixer;
        private Vector2 _shakerRest;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public ArtworkTransitionGraphic Card => card;
        public PixelParticleGraphic ParticlesAsh => particlesAsh;
        public PixelParticleGraphic ParticlesEnergy => particlesEnergy;
        public RectTransform StageRoot => stageRoot != null ? stageRoot : (RectTransform)transform;
        public RectTransform CardRect => card != null ? card.rectTransform : null;

        /// <summary>k = hauteurStage / 640 (échelle preview).</summary>
        public float StageScaleK
        {
            get
            {
                RectTransform rt = StageRoot;
                float h = rt != null ? rt.rect.height : RefPreviewHeight;
                return Mathf.Max(0.01f, h / RefPreviewHeight);
            }
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            if (shaker != null)
                _shakerRest = shaker.anchoredPosition;

            EnsureAudioSources();
            RouteToSfxBus();
        }

        // ═══════════════════════════════════════════
        // ÉTAT VISUEL
        // ═══════════════════════════════════════════

        /// <summary>Applique l'état de transition à toute la pile visuelle.</summary>
        public void ApplyState(in TransitionState s, ArtworkTransitionConfig c, float timeSeq)
        {
            if (c != null)
                _shakeIntensity = c.shakeIntensity;

            if (card != null)
            {
                card.ApplyState(in s, c, timeSeq);
                card.rectTransform.localScale = Vector3.one * s.scale;
            }

            // Rayons — rotation continue (temps non-scalé) + alpha
            float rayA = s.raysAlpha;
            float rayB = s.raysAlpha * 0.6f;
            if (raysA != null)
            {
                SetRawAlpha(raysA, rayA);
                raysA.rectTransform.localRotation = Quaternion.Euler(0f, 0f, timeSeq * RaysASpeed * Mathf.Rad2Deg);
            }

            if (raysB != null)
            {
                SetRawAlpha(raysB, rayB);
                raysB.rectTransform.localRotation = Quaternion.Euler(0f, 0f, timeSeq * RaysBSpeed * Mathf.Rad2Deg);
            }

            // Halo — scale 0.8→1.2 selon glowAmp
            if (halo != null)
            {
                float hs = Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01(s.glowAmp));
                halo.rectTransform.localScale = Vector3.one * hs;
                SetRawAlpha(halo, Mathf.Clamp01(s.glowAmp));
            }

            if (vignette != null)
            {
                Color vc = vignette.color;
                vc.a = Mathf.Clamp01(s.vignette);
                vignette.color = vc;
            }

            PushPortraitFrames(timeSeq);
            SyncParticleParams(c);
        }

        /// <summary>Decay trauma / flash + offset shake. Appelé par le Driver.</summary>
        public void TickFx(float dt)
        {
            if (dt <= 0f)
                return;

            if (_trauma > 0f)
            {
                _trauma = Mathf.Max(0f, _trauma - dt * TraumaDecay);
                ApplyShakeOffset();
            }
            else if (shaker != null)
            {
                shaker.anchoredPosition = _shakerRest;
            }

            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - dt * FlashDecay);
                ApplyFlashVisual();
            }
        }

        /// <summary>Ajoute du trauma de shake local (clampé × shakeIntensity).</summary>
        public void AddTrauma(float x)
        {
            _trauma = Mathf.Clamp01(_trauma + x * _shakeIntensity);
            ApplyShakeOffset();
        }

        /// <summary>Pose un flash (max avec la valeur courante).</summary>
        public void SetFlash(float v)
        {
            _flash = Mathf.Max(_flash, Mathf.Clamp01(v));
            ApplyFlashVisual();
        }

        /// <summary>Assigne les sources de portraits avant / arrière.</summary>
        public void SetPortraits(IPortraitFrameSource front, IPortraitFrameSource back)
        {
            _frontSource = front;
            _backSource = back;
            _lastFrontTex = null;
            _lastBackTex = null;
            PushPortraitFrames(0f);
        }

        /// <summary>Reset complet des FX visuels (particules, trauma, flash, alphas).</summary>
        public void ResetVisuals()
        {
            if (particlesAsh != null) particlesAsh.Clear();
            if (particlesEnergy != null) particlesEnergy.Clear();
            _trauma = 0f;
            _flash = 0f;
            if (shaker != null) shaker.anchoredPosition = _shakerRest;
            ApplyFlashVisual();

            if (raysA != null) SetRawAlpha(raysA, 0f);
            if (raysB != null) SetRawAlpha(raysB, 0f);
            if (halo != null) SetRawAlpha(halo, 0f);
            if (vignette != null)
            {
                Color vc = vignette.color;
                vc.a = 0f;
                vignette.color = vc;
            }
        }

        /// <summary>Taille texels de la frame avant (pour le champ CPU).</summary>
        public Vector2Int FrontFrameSizeTexels
        {
            get
            {
                if (_frontSource != null)
                    return _frontSource.FrameSizeTexels;
                return new Vector2Int(96, 128);
            }
        }

        // ═══════════════════════════════════════════
        // AUDIO
        // ═══════════════════════════════════════════

        /// <summary>One-shot null-safe (clip null → no-op).</summary>
        public void PlayOneShot(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null || _oneshotSource == null)
                return;

            _oneshotSource.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            _oneshotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        /// <summary>Démarre une boucle (remplace la précédente).</summary>
        public void StartLoop(AudioClip clip, float volume)
        {
            if (_loopSource == null)
                return;

            if (_loopFadeCo != null)
            {
                StopCoroutine(_loopFadeCo);
                _loopFadeCo = null;
            }

            if (clip == null)
            {
                _loopSource.Stop();
                _loopSource.clip = null;
                return;
            }

            _loopSource.clip = clip;
            _loopSource.volume = Mathf.Clamp01(volume);
            _loopSource.loop = true;
            _loopSource.Play();
        }

        /// <summary>Ajuste le volume de la boucle courante.</summary>
        public void SetLoopVolume(float v)
        {
            if (_loopSource != null && _loopSource.isPlaying)
                _loopSource.volume = Mathf.Clamp01(v);
        }

        /// <summary>Arrête la boucle avec fade (0.25 s par défaut).</summary>
        public void StopLoop(float fade = LoopFadeDuration)
        {
            if (_loopSource == null || !_loopSource.isPlaying)
                return;

            if (_loopFadeCo != null)
                StopCoroutine(_loopFadeCo);

            if (fade <= 0f)
            {
                _loopSource.Stop();
                _loopSource.clip = null;
                return;
            }

            _loopFadeCo = StartCoroutine(FadeOutLoop(fade));
        }

        /// <summary>Coupe immédiatement tout audio.</summary>
        public void StopAllAudio()
        {
            if (_loopFadeCo != null)
            {
                StopCoroutine(_loopFadeCo);
                _loopFadeCo = null;
            }

            if (_loopSource != null)
            {
                _loopSource.Stop();
                _loopSource.clip = null;
            }

            if (_oneshotSource != null)
                _oneshotSource.Stop();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void PushPortraitFrames(float timeSeq)
        {
            if (card == null)
                return;

            // GetUvRect d'abord : SimpleFlipbookSource met à jour Texture au passage.
            if (_frontSource != null)
            {
                Rect uv = _frontSource.GetUvRect(timeSeq);
                Texture tex = _frontSource.Texture;
                if (tex != _lastFrontTex || !RectsEqual(uv, _lastFrontUv))
                {
                    card.SetFrontFrame(tex, uv);
                    _lastFrontTex = tex;
                    _lastFrontUv = uv;
                }
            }

            if (_backSource != null)
            {
                Rect uv = _backSource.GetUvRect(timeSeq);
                Texture tex = _backSource.Texture;
                if (tex != _lastBackTex || !RectsEqual(uv, _lastBackUv))
                {
                    card.SetBackFrame(tex, uv);
                    _lastBackTex = tex;
                    _lastBackUv = uv;
                }
            }
        }

        private void SyncParticleParams(ArtworkTransitionConfig c)
        {
            float k = StageScaleK;
            Vector2Int art = FrontFrameSizeTexels;
            float cell = 1f;
            if (card != null && art.x > 0)
            {
                Rect cr = card.rectTransform.rect;
                cell = cr.width / art.x;
            }

            Vector2 center = Vector2.zero;
            float glow = c != null ? c.glowIntensity : 0.7f;

            if (particlesEnergy != null)
            {
                particlesEnergy.SetStageScaleK(k);
                particlesEnergy.SetCellSize(cell);
                particlesEnergy.SetGlowIntensity(glow);
                particlesEnergy.SetCenter(center);
            }

            if (particlesAsh != null)
            {
                particlesAsh.SetStageScaleK(k);
                particlesAsh.SetCellSize(cell);
                particlesAsh.SetGlowIntensity(0f);
                particlesAsh.SetCenter(center);
            }
        }

        private void ApplyShakeOffset()
        {
            if (shaker == null)
                return;

            float t2 = _trauma * _trauma;
            if (t2 < 0.0004f)
            {
                shaker.anchoredPosition = _shakerRest;
                return;
            }

            float T = Time.unscaledTime;
            float k = StageScaleK;
            float ox = Mathf.Sin(T * 51.7f) * 13f * t2 * k;
            float oy = Mathf.Cos(T * 42.3f) * 11f * t2 * k;
            shaker.anchoredPosition = _shakerRest + new Vector2(ox, oy);
        }

        private void ApplyFlashVisual()
        {
            if (flash == null)
                return;

            Color fc = flash.color;
            // Couleur ancrée FlashWarm, alpha = flash²
            fc.r = AwPalette.FlashWarm.r;
            fc.g = AwPalette.FlashWarm.g;
            fc.b = AwPalette.FlashWarm.b;
            fc.a = _flash * _flash;
            flash.color = fc;
        }

        private void EnsureAudioSources()
        {
            if (_oneshotSource == null)
            {
                _oneshotSource = gameObject.AddComponent<AudioSource>();
                _oneshotSource.playOnAwake = false;
                _oneshotSource.loop = false;
                _oneshotSource.spatialBlend = 0f;
            }

            if (_loopSource == null)
            {
                _loopSource = gameObject.AddComponent<AudioSource>();
                _loopSource.playOnAwake = false;
                _loopSource.loop = true;
                _loopSource.spatialBlend = 0f;
            }
        }

        private void RouteToSfxBus()
        {
            var group = AudioBuses.SfxGroup;
            if (group != null)
            {
                if (_oneshotSource != null) _oneshotSource.outputAudioMixerGroup = group;
                if (_loopSource != null) _loopSource.outputAudioMixerGroup = group;
            }
            else if (!_warnedMixer)
            {
                _warnedMixer = true;
                Debug.LogWarning("[ArtworkTransitionView] AudioBuses.SfxGroup absent — audio en mode legacy");
            }
        }

        private IEnumerator FadeOutLoop(float fade)
        {
            float start = _loopSource.volume;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                _loopSource.volume = Mathf.Lerp(start, 0f, Mathf.Clamp01(t / fade));
                yield return null;
            }

            _loopSource.Stop();
            _loopSource.clip = null;
            _loopSource.volume = start;
            _loopFadeCo = null;
        }

        private static void SetRawAlpha(RawImage img, float a)
        {
            Color c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }

        private static bool RectsEqual(Rect a, Rect b) =>
            Mathf.Abs(a.x - b.x) < 1e-6f
            && Mathf.Abs(a.y - b.y) < 1e-6f
            && Mathf.Abs(a.width - b.width) < 1e-6f
            && Mathf.Abs(a.height - b.height) < 1e-6f;
    }
}
