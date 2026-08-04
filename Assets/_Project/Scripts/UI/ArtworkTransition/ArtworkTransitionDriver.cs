using System;
using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Séquenceur des transitions Déchéance / Ascension : états, événements, émetteurs.
    /// Temps non-scalé, scrubbable via SetTime.
    /// </summary>
    public class ArtworkTransitionDriver : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // ENUMS
        // ═══════════════════════════════════════════
        private enum DriverMode { Idle, Decheance, Ascension, Done }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private ArtworkTransitionView view;
        [SerializeField] private ArtworkTransitionConfig config;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private DriverMode _mode = DriverMode.Idle;
        private bool _sequenceIsAscension;
        private float _t;
        private float _duration;
        private DecheanceTimeline _tlDec;
        private AscensionTimeline _tlAsc;
        private Action _onFinished;
        private bool _finishedInvoked;

        private IPortraitFrameSource _primeSource;
        private IPortraitFrameSource _dechuSource;
        private IPortraitFrameSource _pixelSource; // source pour cache pixels (prime)

        private readonly ArtworkNoiseField _field = new ArtworkNoiseField();
        private readonly int[] _frontBuffer = new int[8192];
        private Color32[] _pixelCache;
        private int _pixelCacheW, _pixelCacheH;
        private bool _pixelCacheValid;
        private bool _warnedPixelRead;

        // Événements (temps + id)
        private float[] _eventTimes;
        private int[] _eventIds;
        private int _eventCount;
        private int _eventIndex;
        private bool _muteEvents; // SetTime : aucun son / aucun one-shot

        // Émetteurs continus (accumulateurs)
        private float _accMote;
        private float _accEmber;
        private float _accAsh;
        private float _accConverge;
        private float _accReforge;

        private uint _rng = 2026u;
        private bool _reforgeLoopStarted;

        // IDs événements Déchéance
        private const int EvDecSting = 0;
        private const int EvDecShimmer = 1;
        private const int EvDecIgnite = 2;
        private const int EvDecTraumaA = 3;
        private const int EvDecTraumaB = 4;
        private const int EvDecWhoosh = 5;

        // IDs Ascension (pulses = 100+i)
        private const int EvAscRiser = 50;
        private const int EvAscRiserCut = 51;
        private const int EvAscPulseBase = 100;
        private const int EvAscWhiteout = 60;
        private const int EvAscClimax = 61;
        private const int EvAscReforgeStart = 62;
        private const int EvAscReforgeEnd = 63;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public float CurrentTime => _t;
        public bool IsPlaying => _mode == DriverMode.Decheance || _mode == DriverMode.Ascension;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Update()
        {
            if (_mode != DriverMode.Decheance && _mode != DriverMode.Ascension)
                return;

            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            Advance(dt, playEvents: true, emitContinuous: true);
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>Lance la Déchéance : avant = prime, arrière = déchu.</summary>
        public void PlayDecheance(IPortraitFrameSource prime, IPortraitFrameSource dechu, Action onFinished)
        {
            if (config == null || view == null)
            {
                Debug.LogError("[ArtworkTransitionDriver] Config ou View manquant.");
                onFinished?.Invoke();
                return;
            }

            BeginPlay(DriverMode.Decheance, prime, dechu, onFinished);
            _tlDec = ArtworkTransitionMath.BuildDecheance(config);
            _duration = _tlDec.duration;
            view.SetPortraits(prime, dechu);
            _pixelSource = prime;
            BuildNoiseField();
            CachePrimePixels();
            BuildDecheanceEvents();
            FireInitialEvaluate();
        }

        /// <summary>Lance l'Ascension : avant = déchu, arrière = prime.</summary>
        public void PlayAscension(IPortraitFrameSource prime, IPortraitFrameSource dechu, Action onFinished)
        {
            if (config == null || view == null)
            {
                Debug.LogError("[ArtworkTransitionDriver] Config ou View manquant.");
                onFinished?.Invoke();
                return;
            }

            BeginPlay(DriverMode.Ascension, prime, dechu, onFinished);
            _tlAsc = ArtworkTransitionMath.BuildAscension(config);
            _duration = _tlAsc.duration;
            view.SetPortraits(dechu, prime);
            _pixelSource = prime;
            BuildNoiseField();
            CachePrimePixels();
            BuildAscensionEvents();
            FireInitialEvaluate();
        }

        /// <summary>Snap à l'état final : sons coupés, particules clear, onFinished.</summary>
        public void SkipToEnd()
        {
            if (_mode != DriverMode.Decheance && _mode != DriverMode.Ascension && _mode != DriverMode.Done)
                return;

            _t = _duration;
            _eventIndex = _eventCount;
            _muteEvents = true;
            view.StopAllAudio();
            view.ResetVisuals();
            ApplyEvaluateOnly();
            _mode = DriverMode.Done;
            InvokeFinishedOnce();
            _muteEvents = false;
        }

        /// <summary>
        /// Debug/scrub — état pur à t, événements passés marqués joués, pas de son, particules clear.
        /// </summary>
        public void SetTime(float t)
        {
            if (_mode != DriverMode.Decheance && _mode != DriverMode.Ascension && _mode != DriverMode.Done)
                return;

            _t = Mathf.Clamp(t, 0f, _duration);
            _eventIndex = 0;
            while (_eventIndex < _eventCount && _eventTimes[_eventIndex] <= _t)
                _eventIndex++;

            view.StopAllAudio();
            if (view.ParticlesAsh != null) view.ParticlesAsh.Clear();
            if (view.ParticlesEnergy != null) view.ParticlesEnergy.Clear();
            ResetEmitters();

            _muteEvents = true;
            ApplyEvaluateOnly();
            _muteEvents = false;

            if (_t >= _duration - 1e-4f)
            {
                _mode = DriverMode.Done;
                InvokeFinishedOnce();
            }
            else
            {
                // Reprendre la lecture si on scrub en arrière depuis Done
                _mode = _sequenceIsAscension ? DriverMode.Ascension : DriverMode.Decheance;
                _finishedInvoked = false;
            }
        }

        // ═══════════════════════════════════════════
        // BOUCLE INTERNE
        // ═══════════════════════════════════════════

        private void Advance(float dt, bool playEvents, bool emitContinuous)
        {
            _t += dt;

            if (playEvents && !_muteEvents)
                DrainEvents();

            TransitionState state = EvaluateCurrent();

            if (emitContinuous)
                EmitContinuous(dt, in state);

            if (view != null)
            {
                view.ApplyState(in state, config, _t);
                view.TickFx(dt);

                if (_mode == DriverMode.Decheance)
                    view.SetLoopVolume(state.crackle * 0.05f);

                if (view.ParticlesEnergy != null) view.ParticlesEnergy.Tick(dt);
                if (view.ParticlesAsh != null) view.ParticlesAsh.Tick(dt);
            }

            if (_t >= _duration)
            {
                _t = _duration;
                _mode = DriverMode.Done;
                // Stop boucle reforge si encore active
                if (_reforgeLoopStarted)
                {
                    view?.StopLoop();
                    _reforgeLoopStarted = false;
                }

                InvokeFinishedOnce();
            }
        }

        private void FireInitialEvaluate()
        {
            TransitionState state = EvaluateCurrent();
            view.ApplyState(in state, config, _t);
            DrainEvents(); // t=0 events
        }

        private void ApplyEvaluateOnly()
        {
            TransitionState state = EvaluateCurrent();
            if (view != null)
                view.ApplyState(in state, config, _t);
        }

        private TransitionState EvaluateCurrent()
        {
            if (_sequenceIsAscension)
                return ArtworkTransitionMath.EvaluateAscension(_t, in _tlAsc, config);

            return ArtworkTransitionMath.EvaluateDecheance(_t, in _tlDec, config);
        }

        // ═══════════════════════════════════════════
        // ÉVÉNEMENTS
        // ═══════════════════════════════════════════

        private void BuildDecheanceEvents()
        {
            float tIg = _tlDec.ignitionTime;
            float tEnd = _tlDec.burnEndTime;
            float burn = config.burnDuration;

            EnsureEventCapacity(8);
            _eventCount = 0;
            AddEvent(0.00f, EvDecSting);
            AddEvent(0.05f, EvDecShimmer);
            AddEvent(tIg, EvDecIgnite);
            AddEvent(tIg + 0.45f * burn, EvDecTraumaA);
            AddEvent(tIg + 0.85f * burn, EvDecTraumaB);
            AddEvent(tEnd, EvDecWhoosh);
            SortEvents();
            _eventIndex = 0;
        }

        private void BuildAscensionEvents()
        {
            EnsureEventCapacity(16);
            _eventCount = 0;
            AddEvent(0.02f, EvAscRiser);

            int n = _tlAsc.pulseCount;
            for (int i = 0; i < n; i++)
                AddEvent(_tlAsc.Pulse(i), EvAscPulseBase + i);

            AddEvent(_tlAsc.whiteoutTime, EvAscWhiteout);
            // Coupure riser 70 ms avant climax (fade 30 ms) — silence avant l'impact.
            AddEvent(_tlAsc.climaxTime - 0.07f, EvAscRiserCut);
            AddEvent(_tlAsc.climaxTime, EvAscClimax);
            AddEvent(_tlAsc.reforgeStartTime, EvAscReforgeStart);
            AddEvent(_tlAsc.reforgeEndTime, EvAscReforgeEnd);
            SortEvents();
            _eventIndex = 0;
        }

        private void DrainEvents()
        {
            while (_eventIndex < _eventCount && _eventTimes[_eventIndex] <= _t)
            {
                FireEvent(_eventIds[_eventIndex]);
                _eventIndex++;
            }
        }

        private void FireEvent(int id)
        {
            if (_muteEvents || view == null || config == null)
                return;

            Vector2 center = Vector2.zero;

            switch (id)
            {
                case EvDecSting:
                    view.PlayOneShot(config.stingClip, 0.9f);
                    view.SetFlash(0.55f);
                    view.AddTrauma(0.40f);
                    SpawnBursts(12, center);
                    break;

                case EvDecShimmer:
                    view.StartLoop(config.shimmerLoopClip, 0.35f);
                    break;

                case EvDecIgnite:
                    view.StopLoop(0f);
                    view.PlayOneShot(config.igniteClip, 0.7f);
                    view.StartLoop(config.crackleLoopClip, 0.05f);
                    view.AddTrauma(0.18f);
                    break;

                case EvDecTraumaA:
                case EvDecTraumaB:
                    view.AddTrauma(0.13f);
                    break;

                case EvDecWhoosh:
                    view.PlayOneShot(config.whooshDownClip, 0.7f);
                    view.StopLoop();
                    break;

                case EvAscRiser:
                    view.StartLoop(config.riserClip, 0.6f, loop: false);
                    break;

                case EvAscRiserCut:
                    view.StopLoop(0.03f);
                    break;

                case EvAscWhiteout:
                    SpawnConverges(30);
                    break;

                case EvAscClimax:
                    view.PlayOneShot(config.climaxClip, 1.0f);
                    view.SetFlash(1.0f);
                    view.AddTrauma(0.60f);
                    SpawnBursts(config.climaxBurstCount, center);
                    break;

                case EvAscReforgeStart:
                    view.StartLoop(config.reforgeLoopClip, 0.5f);
                    _reforgeLoopStarted = true;
                    break;

                case EvAscReforgeEnd:
                    if (_reforgeLoopStarted)
                    {
                        view.StopLoop();
                        _reforgeLoopStarted = false;
                    }
                    break;

                default:
                    if (id >= EvAscPulseBase && id < EvAscPulseBase + 8)
                    {
                        int i = id - EvAscPulseBase;
                        float pitch = Mathf.Pow(1.26f, i);
                        view.PlayOneShot(config.pulseClip, 0.6f, pitch);
                        view.AddTrauma(0.10f + 0.06f * i);
                        SpawnConverges(16 + 10 * i);
                    }
                    break;
            }
        }

        // ═══════════════════════════════════════════
        // ÉMETTEURS CONTINUS
        // ═══════════════════════════════════════════

        private void EmitContinuous(float dt, in TransitionState state)
        {
            if (view == null || config == null)
                return;

            float k = view.StageScaleK;
            var energy = view.ParticlesEnergy;
            var ash = view.ParticlesAsh;

            if (!_sequenceIsAscension)
            {
                float tIg = _tlDec.ignitionTime;
                float tEnd = _tlDec.burnEndTime;

                // Contemplation motes (0.4 → tIg)
                if (_t >= 0.4f && _t < tIg && energy != null)
                {
                    _accMote += 7f * dt;
                    while (_accMote >= 1f)
                    {
                        _accMote -= 1f;
                        float x = SignedRange(170f) * k;
                        float y = Range(-300f, -180f) * k; // sous le centre
                        energy.SpawnMote(new Vector2(x, y));
                    }
                }

                // Combustion braises + cendres (tIg → tEnd + 0.3)
                if (_t >= tIg && _t < tEnd + 0.3f)
                {
                    int nc = _field.FrontCells(
                        state.progress, true, config.dirWeight, config.frontBand * 1.15f, _frontBuffer);
                    float rateScale = Mathf.Min(1f, nc / 60f);

                    if (energy != null && nc > 0)
                    {
                        _accEmber += config.emberRatePerSec * dt * rateScale;
                        Color hot = Color.Lerp(AwPalette.Gold, AwPalette.AshViolet, state.hybrid);
                        while (_accEmber >= 1f)
                        {
                            _accEmber -= 1f;
                            CellToLocal(PickFrontCell(nc), out Vector2 pos);
                            energy.SpawnEmber(pos, hot);
                        }
                    }

                    if (ash != null && nc > 0)
                    {
                        _accAsh += config.ashRatePerSec * dt * rateScale;
                        while (_accAsh >= 1f)
                        {
                            _accAsh -= 1f;
                            int cellIdx = PickFrontCell(nc);
                            CellToLocal(cellIdx, out Vector2 pos);
                            int cx = _frontBuffer[cellIdx * 2];
                            int cy = _frontBuffer[cellIdx * 2 + 1];
                            Color pix = SamplePrimePixel(cx, cy);
                            Color col = Color.Lerp(pix, AwPalette.AshDark, 0.45f + 0.4f * state.hybrid);
                            ash.SpawnAsh(pos, col);
                        }
                    }
                }
            }
            else
            {
                float tW = _tlAsc.whiteoutTime;
                float tC = _tlAsc.climaxTime;
                float tR0 = _tlAsc.reforgeStartTime;
                float tRE = _tlAsc.reforgeEndTime;

                // White-out converge
                if (_t >= tW && _t < tC && energy != null)
                {
                    _accConverge += 90f * dt;
                    while (_accConverge >= 1f)
                    {
                        _accConverge -= 1f;
                        energy.SpawnConverge();
                    }
                }

                // Reforge sparks
                if (_t >= tR0 && _t < tRE + 0.2f && energy != null)
                {
                    int nc = _field.FrontCells(
                        state.progress, false, config.dirWeight, config.frontBand * 1.15f, _frontBuffer);
                    float rateScale = Mathf.Min(1f, nc / 60f);
                    _accReforge += 130f * dt * rateScale;
                    while (_accReforge >= 1f)
                    {
                        _accReforge -= 1f;
                        if (nc <= 0) break;
                        CellToLocal(PickFrontCell(nc), out Vector2 pos);
                        energy.SpawnReforge(pos);
                    }
                }

                // Apothéose motes
                if (_t >= tRE && energy != null)
                {
                    float av = AwEase.Seg(_t, tRE, _duration);
                    _accMote += 13f * dt * (1f - av);
                    while (_accMote >= 1f)
                    {
                        _accMote -= 1f;
                        float x = SignedRange(170f) * k;
                        float y = Range(-300f, -180f) * k;
                        energy.SpawnMote(new Vector2(x, y));
                    }
                }
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS SPAWN / CACHE
        // ═══════════════════════════════════════════

        private void SpawnBursts(int n, Vector2 center)
        {
            var energy = view != null ? view.ParticlesEnergy : null;
            if (energy == null) return;
            for (int i = 0; i < n; i++)
                energy.SpawnBurst(center);
        }

        private void SpawnConverges(int n)
        {
            var energy = view != null ? view.ParticlesEnergy : null;
            if (energy == null) return;
            for (int i = 0; i < n; i++)
                energy.SpawnConverge();
        }

        private void CellToLocal(int cellIdx, out Vector2 pos)
        {
            int cx = _frontBuffer[cellIdx * 2];
            int cy = _frontBuffer[cellIdx * 2 + 1];
            CellXYToLocal(cx, cy, out pos);
        }

        private void CellXYToLocal(int cx, int cy, out Vector2 pos)
        {
            int artW = Mathf.Max(1, _field.Width);
            int artH = Mathf.Max(1, _field.Height);
            float cardW = 96f;
            float cardH = 128f;
            if (view != null && view.CardRect != null)
            {
                Rect r = view.CardRect.rect;
                cardW = r.width;
                cardH = r.height;
            }

            // y = 0 en bas, cohérent §E
            float x = (cx + 0.5f) / artW * cardW - cardW * 0.5f;
            float y = (cy + 0.5f) / artH * cardH - cardH * 0.5f;
            pos = new Vector2(x, y);
        }

        private int PickFrontCell(int nc)
        {
            if (nc <= 1) return 0;
            return Mathf.FloorToInt(Next() * nc) % nc;
        }

        private void BuildNoiseField()
        {
            Vector2Int sz = view != null ? view.FrontFrameSizeTexels : new Vector2Int(96, 128);
            _field.Build(sz.x, sz.y, config.noiseUvScale, config.noiseSeed);
        }

        private void CachePrimePixels()
        {
            _pixelCacheValid = false;
            _pixelCache = null;
            _pixelCacheW = 0;
            _pixelCacheH = 0;

            if (_pixelSource == null)
                return;

            Texture tex = _pixelSource.Texture;
            Rect uv = _pixelSource.GetUvRect(0f);
            Vector2Int frame = _pixelSource.FrameSizeTexels;
            _pixelCacheW = Mathf.Max(1, frame.x);
            _pixelCacheH = Mathf.Max(1, frame.y);

            try
            {
                var tex2d = tex as Texture2D;
                if (tex2d == null)
                    throw new InvalidOperationException("Texture non-Texture2D");

                int tw = tex2d.width;
                int th = tex2d.height;
                int x0 = Mathf.Clamp(Mathf.FloorToInt(uv.x * tw), 0, tw - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(uv.y * th), 0, th - 1);
                int rw = Mathf.Clamp(Mathf.RoundToInt(uv.width * tw), 1, tw - x0);
                int rh = Mathf.Clamp(Mathf.RoundToInt(uv.height * th), 1, th - y0);

                // GetPixels une seule fois — région UV(0)
                Color[] cols = tex2d.GetPixels(x0, y0, rw, rh);
                _pixelCacheW = rw;
                _pixelCacheH = rh;
                _pixelCache = new Color32[rw * rh];
                for (int i = 0; i < cols.Length; i++)
                    _pixelCache[i] = cols[i];
                _pixelCacheValid = true;
            }
            catch (Exception)
            {
                if (!_warnedPixelRead)
                {
                    _warnedPixelRead = true;
                    Debug.LogWarning(
                        "[ArtworkTransitionDriver] Impossible de lire les pixels CPU du prime — fallback cendre.");
                }

                _pixelCacheValid = false;
            }
        }

        private Color SamplePrimePixel(int cx, int cy)
        {
            if (!_pixelCacheValid || _pixelCache == null)
                return Color.Lerp(AwPalette.AshDark, AwPalette.AshViolet, 0.3f);

            int x = Mathf.Clamp(cx, 0, _pixelCacheW - 1);
            int y = Mathf.Clamp(cy, 0, _pixelCacheH - 1);
            // Si le champ art ≠ cache, scale
            if (_field.Width != _pixelCacheW || _field.Height != _pixelCacheH)
            {
                x = Mathf.Clamp(Mathf.FloorToInt(cx / (float)_field.Width * _pixelCacheW), 0, _pixelCacheW - 1);
                y = Mathf.Clamp(Mathf.FloorToInt(cy / (float)_field.Height * _pixelCacheH), 0, _pixelCacheH - 1);
            }

            return _pixelCache[y * _pixelCacheW + x];
        }

        // ═══════════════════════════════════════════
        // SETUP / UTIL
        // ═══════════════════════════════════════════

        private void BeginPlay(
            DriverMode mode, IPortraitFrameSource prime, IPortraitFrameSource dechu, Action onFinished)
        {
            view.StopAllAudio();
            view.ResetVisuals();
            _mode = mode;
            _sequenceIsAscension = mode == DriverMode.Ascension;
            _t = 0f;
            _onFinished = onFinished;
            _finishedInvoked = false;
            _primeSource = prime;
            _dechuSource = dechu;
            _muteEvents = false;
            _reforgeLoopStarted = false;
            _warnedPixelRead = false;
            ResetEmitters();
            _rng = 2026u;
        }

        private void ResetEmitters()
        {
            _accMote = 0f;
            _accEmber = 0f;
            _accAsh = 0f;
            _accConverge = 0f;
            _accReforge = 0f;
        }

        private void InvokeFinishedOnce()
        {
            if (_finishedInvoked)
                return;
            _finishedInvoked = true;
            var cb = _onFinished;
            _onFinished = null;
            cb?.Invoke();
        }

        private void EnsureEventCapacity(int n)
        {
            if (_eventTimes == null || _eventTimes.Length < n)
            {
                _eventTimes = new float[n];
                _eventIds = new int[n];
            }
        }

        private void AddEvent(float time, int id)
        {
            if (_eventCount >= _eventTimes.Length)
            {
                Array.Resize(ref _eventTimes, _eventCount + 8);
                Array.Resize(ref _eventIds, _eventCount + 8);
            }

            _eventTimes[_eventCount] = time;
            _eventIds[_eventCount] = id;
            _eventCount++;
        }

        private void SortEvents()
        {
            // Insertion sort — N petit
            for (int i = 1; i < _eventCount; i++)
            {
                float t = _eventTimes[i];
                int id = _eventIds[i];
                int j = i - 1;
                while (j >= 0 && _eventTimes[j] > t)
                {
                    _eventTimes[j + 1] = _eventTimes[j];
                    _eventIds[j + 1] = _eventIds[j];
                    j--;
                }

                _eventTimes[j + 1] = t;
                _eventIds[j + 1] = id;
            }
        }

        private float Next()
        {
            _rng += 0x6D2B79F5u;
            uint t = _rng;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return (t ^ (t >> 14)) / 4294967296f;
        }

        private float Range(float a, float b) => a + (b - a) * Next();
        private float SignedRange(float a) => Range(-a, a);
    }
}
