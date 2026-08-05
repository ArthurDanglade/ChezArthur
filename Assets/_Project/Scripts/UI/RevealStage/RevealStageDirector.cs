using System;
using System.Collections;
using ChezArthur.Audio;
using ChezArthur.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.RevealStage
{
    /// <summary>
    /// Cœur « Entrée en scène » — courbes pures du temps, scrub-friendly, dormant jusqu'à INVR2.
    /// </summary>
    public class RevealStageDirector : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float REF_WIDTH_PX = 360f;
        private const float SETTLE_VIGNETTE_DUR = 0.22f;
        private const float VOL_RISER = 0.6f;
        private const float VOL_SNAP = 1.0f;
        private const float VOL_DIM = 0.7f;
        private const float MOTE_RATE = 14f;
        private const float FOCAL_CLAMP_MIN = 0.05f;
        private const float FOCAL_CLAMP_MAX = 0.95f;

        private static readonly int RectMinId = Shader.PropertyToID("_RectMin");
        private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
        private static readonly int DitherCellId = Shader.PropertyToID("_DitherCellPx");
        private static readonly int FocalRectId = Shader.PropertyToID("_FocalRect");
        private static readonly int AspectYId = Shader.PropertyToID("_AspectY");
        private static readonly int LightRId = Shader.PropertyToID("_LightR");
        private static readonly int LightBId = Shader.PropertyToID("_LightB");
        private static readonly int TintId = Shader.PropertyToID("_Tint");
        private static readonly int SnapId = Shader.PropertyToID("_Snap");
        private static readonly int FrontSoftId = Shader.PropertyToID("_FrontSoft");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int ShadowLevelId = Shader.PropertyToID("_ShadowLevel");
        private static readonly int DimId = Shader.PropertyToID("_Dim");

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Config")]
        [SerializeField] private RevealStageConfig config;

        [Header("Matériau partagé (RevealLight.mat)")]
        [SerializeField] private Material sharedMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RawImage _target;
        private CharacterArtworkView _view;
        private RevealPixelFxGraphic _fx;
        private Material _mat;
        private Coroutine _routine;
        private bool _playing;
        private bool _skipToSnap;
        private int _stageGen;
        private Vector3 _baseLocalPos;
        private Vector3 _baseLocalScale;
        private float _lastScreenW;
        private float _lastScreenH;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public bool IsPlaying => _playing;
        public RevealPixelFxGraphic Fx => _fx;
        public RevealStageConfig Config => config;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void OnDestroy()
        {
            StopRoutine();
            DestroyMat();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>Branche config / matériau partagé (builder, harness).</summary>
        public void Wire(RevealStageConfig cfg, Material shared)
        {
            config = cfg;
            sharedMaterial = shared;
        }

        /// <summary>
        /// Clone runtime du mat + câble le FX graphic au-dessus du RawImage.
        /// </summary>
        public void Bind(RawImage target, CharacterArtworkView view)
        {
            _target = target;
            _view = view;
            EnsureMat();
            EnsureFx();

            if (_target != null)
            {
                _baseLocalPos = _target.rectTransform.localPosition;
                _baseLocalScale = _target.rectTransform.localScale;
                if (_mat != null)
                    _target.material = _mat;
            }

            RecalcRectUniforms();
            ArmDark();
        }

        /// <summary>État noir total (pré-armement sous le voile train).</summary>
        public void ArmDark()
        {
            // Invalide toute sortie Dim en cours (M-INVR1-3 — enchaînement).
            _stageGen++;

            if (_mat == null)
                return;

            ApplyLight(0f, 0f);
            _mat.SetFloat(SnapId, 0f);
            _mat.SetFloat(FlashId, 0f);
            _mat.SetFloat(VignetteId, 0f);
            _mat.SetFloat(DimId, 1f);
            if (config != null)
            {
                _mat.SetFloat(FrontSoftId, config.frontSoft);
                _mat.SetFloat(ShadowLevelId, config.shadowLevel);
            }
        }

        /// <summary>
        /// Joue entrée → suspension → noir → snap → settle.
        /// fakeout : profil/teinte SR à l'entrée ; micro-flashs teinte réelle dans le noir.
        /// </summary>
        public IEnumerator CoPlayArrival(
            CharacterRarity rarity,
            bool fakeout,
            Vector2 focalArt01,
            Action onSnap = null,
            bool suppressSnapSfx = false)
        {
            if (_target == null || _mat == null || config == null)
                yield break;

            StopRoutineKeepPlaying();
            _playing = true;
            _skipToSnap = false;
            _routine = null;

            CharacterRarity entryRarity = fakeout ? CharacterRarity.SR : rarity;
            Color entryTint = CharacterRarityPalette.GetColor(entryRarity);
            Color realTint = CharacterRarityPalette.GetColor(rarity);

            RecalcRectUniforms();
            ApplyFocal(focalArt01);
            ArmDark();
            _mat.SetColor(TintId, entryTint);

            PlayManaged(config.entryRiserClip, VOL_RISER);

            // ── Entrée ──
            float entryDur = config.GetEntry(entryRarity);
            int pulses = config.GetPulses(entryRarity);
            float lightMax = config.GetLightMax(entryRarity);
            float t = 0f;
            while (t < entryDur && !_skipToSnap)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, entryDur));
                float flick = entryRarity == CharacterRarity.LR
                    ? 0.92f + 0.08f * Mathf.Sin(37f * t) * Mathf.Sin(23f * t)
                    : 1f;
                float ease = EaseInOut(p);
                float pulse = Mathf.Sin(Mathf.PI * Frac(p * pulses));
                pulse *= pulse;
                float R = (0.16f + 0.42f * ease + 0.07f * pulse) * flick;
                float B = lightMax * (0.35f + 0.65f * ease) * flick;
                ApplyLight(R, B);
                MaybeSpawnMote(dt, R, entryTint);
                if (_fx != null) _fx.Tick(dt);
                yield return null;
            }

            // ── Suspension ──
            if (!_skipToSnap)
            {
                float holdDur = fakeout ? config.fakeHold : config.GetHold(entryRarity);
                t = 0f;
                while (t < holdDur && !_skipToSnap)
                {
                    float dt = Time.unscaledDeltaTime;
                    t += dt;
                    float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, holdDur));
                    ApplyLight(0.58f * (1f - 0.25f * p), lightMax * (1f - 0.35f * p));
                    if (_fx != null) _fx.Tick(dt);
                    yield return null;
                }
            }

            // ── Noir (silence = beat) ──
            StopManaged();
            float cutDur = config.cutDuration + (fakeout ? config.fakeCutBonus : 0f);
            ApplyLight(0f, 0f);
            t = 0f;
            while (t < cutDur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, cutDur));
                if (fakeout && ((p >= 0.35f && p <= 0.45f) || (p >= 0.70f && p <= 0.78f)))
                {
                    _mat.SetColor(TintId, realTint);
                    ApplyLight(0.2f, 0.25f);
                }
                else
                {
                    ApplyLight(0f, 0f);
                }

                if (_fx != null) _fx.Tick(dt);
                yield return null;
            }

            ApplyLight(0f, 0f);
            _mat.SetColor(TintId, realTint);

            // ── Snap (jamais coupé) ──
            float snapDur = config.GetSnap(rarity);
            float punch = config.GetPunch(rarity);
            int partsBudget = config.GetParts(rarity);
            int partsEmitted = 0;
            // suppressSnapSfx : le sting AW porte l'impact (nouveau porteur de couple).
            if (!suppressSnapSfx)
                PlayManaged(config.GetSnapClip(rarity), VOL_SNAP);
            onSnap?.Invoke();

            Rect rect = _target.rectTransform.rect;
            float rectW = Mathf.Max(1f, rect.width);
            Vector2 focalPx = FocalToLocalPx();

            t = 0f;
            while (t < snapDur)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, snapDur));
                float snap = EaseOutCubic(p);
                float flash = (1f - p) * (1f - p) * 0.9f;
                _mat.SetFloat(SnapId, snap);
                _mat.SetFloat(FlashId, flash);

                float punchK = punch * (1f - p) * (1f - p);
                _target.rectTransform.localScale = _baseLocalScale * (1f + punchK);
                float jitter = punch * 0.08f * rectW * (1f - p) * (1f - p);
                _target.rectTransform.localPosition = _baseLocalPos
                    + new Vector3(SignedNoise(t) * jitter, SignedNoise(t + 17f) * jitter, 0f);

                // Burst le long du front
                float frontR = snap * 1.65f * 0.5f * rectW;
                int targetParts = Mathf.RoundToInt(partsBudget * snap);
                while (partsEmitted < targetParts)
                {
                    float ang = (partsEmitted * 2.399963f) % (Mathf.PI * 2f);
                    Vector2 pos = focalPx + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * frontR;
                    if (_fx != null)
                        _fx.SpawnBurst(pos, realTint, 1);
                    partsEmitted++;
                }

                if (_fx != null) _fx.Tick(dt);
                yield return null;
            }

            _mat.SetFloat(SnapId, 1f);
            _mat.SetFloat(FlashId, 0f);
            _target.rectTransform.localScale = _baseLocalScale;
            _target.rectTransform.localPosition = _baseLocalPos;

            // ── Settle vignette ──
            t = 0f;
            float vigTarget = config.vignette;
            while (t < SETTLE_VIGNETTE_DUR)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / SETTLE_VIGNETTE_DUR);
                _mat.SetFloat(VignetteId, vigTarget * EaseInOut(p));
                if (_fx != null) _fx.Tick(dt);
                yield return null;
            }

            _mat.SetFloat(VignetteId, vigTarget);
            _playing = false;
            _skipToSnap = false;
        }

        /// <summary>Extinction Dim 1→0 ; l'appelant peut chevaucher une entrée (entryOverlap).</summary>
        public IEnumerator CoPlayExit()
        {
            if (_mat == null || config == null)
                yield break;

            // Jeton : une nouvelle ArmDark / arrivée stoppe cette sortie sans poser Dim=0.
            int gen = _stageGen;
            _playing = true;
            PlayManaged(config.exitDimClip, VOL_DIM);
            float dur = config.exitDim;
            float t = 0f;
            while (t < dur)
            {
                if (_stageGen != gen)
                {
                    _playing = false;
                    yield break;
                }

                float dt = Time.unscaledDeltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / Mathf.Max(0.0001f, dur));
                _mat.SetFloat(DimId, 1f - p);
                if (_fx != null) _fx.Tick(dt);
                yield return null;
            }

            if (_stageGen != gen)
            {
                _playing = false;
                yield break;
            }

            _mat.SetFloat(DimId, 0f);
            _playing = false;
        }

        /// <summary>
        /// Tap : saute entrée/suspension → noir. LE SNAP SE JOUE TOUJOURS.
        /// </summary>
        public void SkipToSnap()
        {
            if (_playing)
                _skipToSnap = true;
        }

        /// <summary>
        /// Teardown : material null sur RawImage, FX Clear, localPosition rendue, Dim=1.
        /// </summary>
        public void ResetVisuals()
        {
            StopRoutine();
            StopManaged();
            _playing = false;
            _skipToSnap = false;

            if (_fx != null)
                _fx.Clear();

            if (_target != null)
            {
                _target.material = null;
                _target.rectTransform.localPosition = _baseLocalPos;
                _target.rectTransform.localScale = _baseLocalScale;
            }

            if (_mat != null)
                _mat.SetFloat(DimId, 1f);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureMat()
        {
            if (_mat != null)
                return;

            if (sharedMaterial != null)
                _mat = new Material(sharedMaterial);
            else
            {
                Shader shader = Shader.Find("ChezArthur/UI/RevealLight");
                if (shader != null)
                    _mat = new Material(shader);
            }
        }

        private void EnsureFx()
        {
            if (_fx != null || _target == null)
                return;

            Transform parent = _target.transform.parent;
            if (parent == null)
                parent = _target.transform;

            Transform existing = parent.Find("RevealPixelFx");
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
                _fx = go.GetComponent<RevealPixelFxGraphic>();
                if (_fx == null)
                    _fx = go.AddComponent<RevealPixelFxGraphic>();
            }
            else
            {
                go = new GameObject("RevealPixelFx", typeof(RectTransform), typeof(CanvasRenderer));
                go.transform.SetParent(parent, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                StretchFull(rt);
                _fx = go.AddComponent<RevealPixelFxGraphic>();
            }

            go.transform.SetAsLastSibling();
            _fx.raycastTarget = false;
            if (config != null)
                _fx.SetCellSize(config.ditherCell);
        }

        private void RecalcRectUniforms()
        {
            if (_mat == null || _target == null)
                return;

            Rect r = _target.rectTransform.rect;
            float w = Mathf.Max(1f, r.width);
            float h = Mathf.Max(1f, r.height);
            _mat.SetVector(RectMinId, new Vector4(r.xMin, r.yMin, 0f, 0f));
            _mat.SetVector(RectSizeId, new Vector4(w, h, 0f, 0f));
            _mat.SetFloat(AspectYId, h / w);

            float cell = config != null ? config.ditherCell : 3f;
            float cellPx = cell * w / REF_WIDTH_PX;
            _mat.SetFloat(DitherCellId, Mathf.Max(0.5f, cellPx));
            if (_fx != null)
                _fx.SetCellSize(Mathf.Max(1f, cellPx));

            if (config != null)
            {
                _mat.SetFloat(FrontSoftId, config.frontSoft);
                _mat.SetFloat(ShadowLevelId, config.shadowLevel);
            }

            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
        }

        private void ApplyFocal(Vector2 focalArt01)
        {
            if (_mat == null || _target == null)
                return;

            Rect uv = _target.uvRect;
            float sx = Mathf.Max(0.0001f, uv.width);
            float sy = Mathf.Max(0.0001f, uv.height);
            float fx = (focalArt01.x - uv.x) / sx;
            float fy = (focalArt01.y - uv.y) / sy;
            fx = Mathf.Clamp(fx, FOCAL_CLAMP_MIN, FOCAL_CLAMP_MAX);
            fy = Mathf.Clamp(fy, FOCAL_CLAMP_MIN, FOCAL_CLAMP_MAX);
            _mat.SetVector(FocalRectId, new Vector4(fx, fy, 0f, 0f));
        }

        private Vector2 FocalToLocalPx()
        {
            if (_mat == null || _target == null)
                return Vector2.zero;

            Vector4 f = _mat.GetVector(FocalRectId);
            Rect r = _target.rectTransform.rect;
            return new Vector2(r.xMin + f.x * r.width, r.yMin + f.y * r.height);
        }

        private void ApplyLight(float R, float B)
        {
            if (_mat == null) return;
            _mat.SetFloat(LightRId, R);
            _mat.SetFloat(LightBId, B);
        }

        private void MaybeSpawnMote(float dt, float R, Color tint)
        {
            if (_fx == null || _target == null)
                return;

            // Probabilité dt·14
            if (UnityEngine.Random.value > dt * MOTE_RATE)
                return;

            Rect r = _target.rectTransform.rect;
            float radius = R * 0.30f * r.width;
            Vector2 focal = FocalToLocalPx();
            float ang = UnityEngine.Random.value * Mathf.PI * 2f;
            float rad = UnityEngine.Random.value * radius;
            _fx.SpawnMote(focal + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad, tint);
        }

        private void PlayManaged(AudioClip clip, float vol)
        {
            if (clip == null || SfxManager.Instance == null)
                return;
            SfxManager.Instance.PlayManagedSfx(clip, vol);
        }

        private static void StopManaged()
        {
            if (SfxManager.Instance != null)
                SfxManager.Instance.StopManagedSfx();
        }

        private void StopRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private void StopRoutineKeepPlaying()
        {
            // Arrivée pilotée par l'appelant via StartCoroutine — pas de handle interne ici.
            _skipToSnap = false;
        }

        private void DestroyMat()
        {
            if (_mat == null) return;
            if (Application.isPlaying)
                Destroy(_mat);
            else
                DestroyImmediate(_mat);
            _mat = null;
        }

        private static float EaseInOut(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        private static float EaseOutCubic(float x)
        {
            x = Mathf.Clamp01(x);
            float inv = 1f - x;
            return 1f - inv * inv * inv;
        }

        private static float Frac(float x) => x - Mathf.Floor(x);

        private static float SignedNoise(float t) =>
            Mathf.Sin(t * 47.13f) * Mathf.Cos(t * 31.7f);

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void LateUpdate()
        {
            if (_mat == null || _target == null)
                return;
            if (!Mathf.Approximately(_lastScreenW, Screen.width)
                || !Mathf.Approximately(_lastScreenH, Screen.height))
                RecalcRectUniforms();

            // Drain des particules résiduelles hors phase (le Director possède le Tick).
            if (!_playing && _fx != null && _fx.AliveCount > 0)
                _fx.Tick(Time.unscaledDeltaTime);
        }
    }
}
