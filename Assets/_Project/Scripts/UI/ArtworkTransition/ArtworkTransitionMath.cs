using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>Phase courante d'une séquence (affichage debug / logique du driver).</summary>
    public enum AwPhase
    {
        Idle, Apparition, Contemplation, Ignition, Combustion, Retombee,
        Fremissement, Pulsations, Whiteout, Reforge, Apotheose
    }

    /// <summary>État complet du rendu à l'instant t — fonction pure du temps, scrubbable.</summary>
    public struct TransitionState
    {
        public float progress, hybrid, whiteFront, bright, rim, emberCool, edgeGain, jitter;
        public float scale, vignette, raysAlpha, glowAmp, crackle;
        public bool frontIsPrime, consumeFromTop;
        public Color rimColor;
        public AwPhase phase;

        public static TransitionState Default => new TransitionState
        {
            edgeGain = 1f, scale = 1f, frontIsPrime = true, consumeFromTop = true,
            rimColor = AwPalette.RimGold, phase = AwPhase.Idle,
        };
    }

    /// <summary>Palette du chantier AW (ancrée charte F0 + rareté SSR).</summary>
    public static class AwPalette
    {
        public static readonly Color GoldCore  = new Color(1.00f, 0.96f, 0.78f);
        public static readonly Color Gold      = new Color(1.00f, 0.72f, 0.25f);
        public static readonly Color EmberRed  = new Color(0.85f, 0.30f, 0.10f);
        public static readonly Color AshViolet = new Color(0.58f, 0.32f, 0.80f);
        public static readonly Color AshDark   = new Color(0.22f, 0.13f, 0.32f);
        public static readonly Color RimGold   = new Color(1.00f, 0.83f, 0.42f);
        public static readonly Color RimEmber  = new Color(0.75f, 0.35f, 0.55f);
        public static readonly Color FlashWarm = new Color(1.00f, 0.973f, 0.918f);
    }

    /// <summary>Easings — mêmes courbes que la preview AW0.</summary>
    public static class AwEase
    {
        public static float InQuad(float t)    => t * t;
        public static float OutQuad(float t)   => 1f - (1f - t) * (1f - t);
        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
        public static float OutCubic(float t)  => 1f - Mathf.Pow(1f - t, 3f);
        // Segment : progression 0..1 de t entre a et b (clampée)
        public static float Seg(float t, float a, float b) => Mathf.Clamp01((t - a) / (b - a));
    }

    public struct DecheanceTimeline { public float ignitionTime, burnEndTime, duration; }

    public struct AscensionTimeline
    {
        public float pulseStartTime, whiteoutTime, climaxTime, reforgeStartTime, reforgeEndTime, duration;
        public int pulseCount;
        public float p0, p1, p2, p3, p4; // instants des pulses (max 5)
        public float Pulse(int i) => i == 0 ? p0 : i == 1 ? p1 : i == 2 ? p2 : i == 3 ? p3 : p4;
    }

    public static class ArtworkTransitionMath
    {
        // Durées de structure (non exposées au tuning — voir §D)
        public const float APPEAR = 0.15f, SETTLE = 0.90f, TREMBLE = 0.85f;
        public const float WHITEOUT_RAMP = 0.30f, WHITEOUT_HOLD = 0.08f;
        public const float CLIMAX_TO_REFORGE = 0.02f, APOTHEOSIS = 0.95f;
        public const float PULSE_ACCEL_RATIO = 0.68f; // intervalles de pulses en accélération

        // ═══════════════════════ TIMELINES ═══════════════════════

        public static DecheanceTimeline BuildDecheance(ArtworkTransitionConfig c)
        {
            DecheanceTimeline tl;
            tl.ignitionTime = APPEAR + c.holdDuration;
            tl.burnEndTime = tl.ignitionTime + c.burnDuration;
            tl.duration = tl.burnEndTime + SETTLE;
            return tl;
        }

        public static AscensionTimeline BuildAscension(ArtworkTransitionConfig c)
        {
            AscensionTimeline tl = default;
            tl.pulseStartTime = TREMBLE;
            tl.pulseCount = Mathf.Clamp(c.pulseCount, 2, 5);
            float sum = 0f, v = 1f;
            for (int i = 0; i < tl.pulseCount; i++) { sum += v; v *= PULSE_ACCEL_RATIO; }
            float acc = 0f; v = 1f;
            for (int i = 0; i < tl.pulseCount; i++)
            {
                float tp = tl.pulseStartTime + (acc / sum) * c.pulsePhaseDuration;
                if (i == 0) tl.p0 = tp; else if (i == 1) tl.p1 = tp; else if (i == 2) tl.p2 = tp;
                else if (i == 3) tl.p3 = tp; else tl.p4 = tp;
                acc += v; v *= PULSE_ACCEL_RATIO;
            }
            tl.whiteoutTime = tl.pulseStartTime + c.pulsePhaseDuration;
            tl.climaxTime = tl.whiteoutTime + WHITEOUT_RAMP + WHITEOUT_HOLD;
            tl.reforgeStartTime = tl.climaxTime + CLIMAX_TO_REFORGE;
            tl.reforgeEndTime = tl.reforgeStartTime + c.reforgeDuration;
            tl.duration = tl.reforgeEndTime + APOTHEOSIS;
            return tl;
        }

        // ═══════════════════════ ÉVALUATIONS (fonctions pures) ═══════════════════════

        /// <summary>Déchéance : avant = prime, arrière = déchu, consumé depuis le HAUT (la chute tombe).</summary>
        public static TransitionState EvaluateDecheance(float t, in DecheanceTimeline tl, ArtworkTransitionConfig c)
        {
            var s = TransitionState.Default;
            s.frontIsPrime = true; s.consumeFromTop = true;

            if (t < tl.ignitionTime)
            {
                // Apparition + contemplation : le joueur VOIT ce qu'il pourrait avoir
                s.phase = t < 0.16f ? AwPhase.Apparition : AwPhase.Contemplation;
                s.whiteFront = 1f - AwEase.OutCubic(AwEase.Seg(t, 0f, 0.18f));
                s.scale = Mathf.Lerp(1.12f, 1f, AwEase.OutCubic(AwEase.Seg(t, 0f, 0.42f)));
                float breathe = AwEase.Seg(t, 0.42f, tl.ignitionTime);
                s.scale += 0.008f * Mathf.Sin(t * 5.6f) * AwEase.InOutSine(Mathf.Min(breathe * 3f, 1f));
                s.rim = (0.34f + 0.22f * Mathf.Sin(t * 10f))
                        * AwEase.InOutSine(AwEase.Seg(t, 0.25f, 0.8f))
                        * (1f - AwEase.Seg(t, tl.ignitionTime - 0.15f, tl.ignitionTime) * 0.5f);
                s.vignette = 0.16f * AwEase.Seg(t, 0.3f, tl.ignitionTime);
                s.glowAmp = 0.12f * c.glowIntensity * AwEase.InOutSine(AwEase.Seg(t, 0.3f, 1.2f));
            }
            else if (t < tl.burnEndTime)
            {
                // Combustion : le front dévore le prime, l'or vire à la cendre
                float cs = AwEase.Seg(t, tl.ignitionTime, tl.burnEndTime);
                s.phase = cs < 0.1f ? AwPhase.Ignition : AwPhase.Combustion;
                s.progress = AwEase.InOutSine(cs);
                s.hybrid = AwEase.InQuad(AwEase.Seg(cs, 0.22f, 0.95f));
                s.emberCool = 1f; s.jitter = 1f;
                s.scale = Mathf.Lerp(1f, 0.994f, cs);
                s.vignette = Mathf.Lerp(0.16f, 0.38f, AwEase.InOutSine(cs));
                s.crackle = Mathf.Sin(Mathf.PI * Mathf.Min(cs * 1.25f, 1f)) * 0.9f + 0.1f;
                s.glowAmp = 0.10f * c.glowIntensity * (1f - cs);
            }
            else
            {
                // Retombée : les braises s'éteignent, le déchu s'installe
                float ss = AwEase.Seg(t, tl.burnEndTime, tl.duration);
                s.phase = AwPhase.Retombee;
                s.progress = 1f; s.hybrid = 1f;
                s.emberCool = (1f - AwEase.OutCubic(ss)) * 0.9f;
                s.edgeGain = 1f - AwEase.OutQuad(ss);
                s.scale = 1f - 0.016f * Mathf.Sin(Mathf.PI * AwEase.InOutSine(Mathf.Min(ss * 1.6f, 1f)));
                s.vignette = Mathf.Lerp(0.38f, 0.22f, ss);
                s.crackle = Mathf.Max(0f, 0.25f * (1f - ss * 2.2f));
                s.rim = 0.10f * (1f - ss) * (0.5f + 0.5f * Mathf.Sin(t * 6f));
                s.rimColor = AwPalette.RimEmber;
            }
            return s;
        }

        /// <summary>Ascension : avant = déchu, arrière = prime, reconstruit depuis le BAS (l'élévation monte).</summary>
        public static TransitionState EvaluateAscension(float t, in AscensionTimeline tl, ArtworkTransitionConfig c)
        {
            var s = TransitionState.Default;
            s.frontIsPrime = false; s.consumeFromTop = false;

            if (t < tl.climaxTime)
            {
                // Frémissement → pulsations → white-out (anticipation façon évolution)
                s.phase = t < tl.pulseStartTime ? AwPhase.Fremissement
                        : (t < tl.whiteoutTime ? AwPhase.Pulsations : AwPhase.Whiteout);
                s.jitter = t < tl.pulseStartTime ? 0.6f * AwEase.Seg(t, 0.15f, tl.pulseStartTime) : 0.35f;
                s.rim = 0.15f * AwEase.InOutSine(AwEase.Seg(t, 0.2f, tl.pulseStartTime))
                      + 0.1f * Mathf.Sin(t * 7f) * AwEase.Seg(t, 0.4f, tl.pulseStartTime);

                float white = 0f, punch = 0f;
                int n = tl.pulseCount;
                for (int i = 0; i < n; i++)
                {
                    float e = t - tl.Pulse(i);
                    if (e >= 0f)
                    {
                        float env = Mathf.Exp(-e * 5.2f);
                        float peak = 0.30f + 0.45f * (i / Mathf.Max(1f, n - 1f));
                        white = Mathf.Max(white, peak * env);
                        punch = Mathf.Max(punch, 0.016f * (1f + i * 0.4f) * env);
                        white += 0.05f * (i + 1) / n; // plancher qui monte à chaque pulse
                    }
                }
                if (t >= tl.whiteoutTime)
                    white = Mathf.Max(white, AwEase.InQuad(AwEase.Seg(t, tl.whiteoutTime, tl.whiteoutTime + WHITEOUT_RAMP)));
                s.whiteFront = Mathf.Min(1f, white);
                s.scale = 1f + punch;
                s.vignette = 0.22f * AwEase.InOutSine(AwEase.Seg(t, tl.pulseStartTime, tl.whiteoutTime))
                           * (1f - AwEase.Seg(t, tl.whiteoutTime, tl.climaxTime));
                float wRamp = AwEase.InQuad(AwEase.Seg(t, tl.whiteoutTime, tl.climaxTime));
                s.raysAlpha = c.rayIntensity * 0.5f * wRamp;
                s.glowAmp = c.glowIntensity * (0.1f + 0.5f * wRamp
                          + 0.3f * s.whiteFront * AwEase.Seg(t, tl.pulseStartTime, tl.whiteoutTime));
            }
            else if (t < tl.reforgeEndTime)
            {
                // Reforge : le blanc se déchire de bas en haut, l'or reconstruit le prime
                float rs = AwEase.Seg(t, tl.reforgeStartTime, tl.reforgeEndTime);
                s.phase = AwPhase.Reforge;
                s.whiteFront = 1f;
                s.progress = AwEase.InOutSine(rs);
                s.hybrid = 0f;                 // le front reste or — jamais de cendre ici
                s.emberCool = 0.45f;           // rémanence dorée sur la zone révélée
                s.jitter = 0.4f;
                s.scale = Mathf.Lerp(1.07f, 1f, AwEase.OutCubic(rs));
                s.raysAlpha = c.rayIntensity * (1f - 0.35f * rs);
                s.glowAmp = c.glowIntensity * (0.85f - 0.35f * rs);
                s.bright = 0.10f * (1f - rs);
            }
            else
            {
                // Apothéose : liseré d'or respirant, poussière de lumière
                float ass = AwEase.Seg(t, tl.reforgeEndTime, tl.duration);
                s.phase = AwPhase.Apotheose;
                s.progress = 1f; s.whiteFront = 0f;
                s.rim = (0.5f - 0.28f * ass) * (0.65f + 0.35f * Mathf.Sin(t * 5.5f));
                s.raysAlpha = c.rayIntensity * 0.65f * (1f - AwEase.InOutSine(ass));
                s.glowAmp = c.glowIntensity * 0.5f * (1f - ass);
                s.bright = 0.05f * (1f - ass);
            }
            return s;
        }
    }

    /// <summary>
    /// Champ de hauteur CPU par cellule d'art — MÊME formule que le shader.
    /// Sert au spawn des braises/étincelles exactement sur le front visible.
    /// Convention : x = colonne, y = rangée avec y = 0 EN BAS (comme le shader et SetPixels).
    /// </summary>
    public sealed class ArtworkNoiseField
    {
        private float[] _noise;       // 256×256 partagé
        private float[] _cellNoise;   // par cellule d'art
        private int _w, _h;

        public int Width => _w;
        public int Height => _h;

        public void Build(int artW, int artH, float noiseUvScale, int seed)
        {
            _noise ??= ArtworkNoise.Generate(seed);
            if (_cellNoise == null || _cellNoise.Length < artW * artH) _cellNoise = new float[artW * artH];
            _w = artW; _h = artH;
            for (int y = 0; y < artH; y++)
            for (int x = 0; x < artW; x++)
                _cellNoise[y * artW + x] = ArtworkNoise.SampleAt(_noise,
                    ((x + 0.5f) / artW) * noiseUvScale, ((y + 0.5f) / artH) * noiseUvScale);
        }

        public float HeightAt(int x, int y, bool consumeFromTop, float dirWeight)
        {
            float grad = consumeFromTop ? 1f - y / (float)(_h - 1) : y / (float)(_h - 1);
            return Mathf.Lerp(_cellNoise[y * _w + x], grad, dirWeight);
        }

        /// <summary>
        /// Cellules proches du front (|h − p| &lt; band). Écrit des paires (x, y) dans buffer,
        /// retourne le nombre de paires. Stride adaptatif pour les grands artworks. Zéro alloc.
        /// </summary>
        public int FrontCells(float progress, bool consumeFromTop, float dirWeight, float band, int[] buffer)
        {
            float p = progress * (1f + 4f * band) - 2f * band;
            int stride = Mathf.Max(1, Mathf.RoundToInt(_w / 96f));
            int count = 0;
            for (int y = 0; y < _h; y += stride)
            for (int x = 0; x < _w; x += stride)
            {
                if (Mathf.Abs(HeightAt(x, y, consumeFromTop, dirWeight) - p) < band)
                {
                    if (count * 2 + 1 >= buffer.Length) return count;
                    buffer[count * 2] = x; buffer[count * 2 + 1] = y; count++;
                }
            }
            return count;
        }
    }
}
