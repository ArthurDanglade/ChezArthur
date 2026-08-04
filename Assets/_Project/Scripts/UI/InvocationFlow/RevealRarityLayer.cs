using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;
using ChezArthur.UI.ArtworkTransition;

namespace ChezArthur.UI.InvocationFlow
{
    /// <summary>
    /// Couche rareté de l'apparition : montée, sous-glow, liseré, punch (preview INV0 verbatim).
    /// Réutilise PixelParticleGraphic AW sans le modifier.
    /// </summary>
    public class RevealRarityLayer : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float TraumaDecay = 2.2f;
        private const float FlashDecay = 3.2f;
        private const float PunchSettle = 0.22f;
        private const float ShakeFreqX = 51f;
        private const float ShakeFreqY = 43f;
        private const float ShakeAmpX = 9f;
        private const float ShakeAmpY = 7f;

        private static readonly Color ColSR = new Color(0x7F / 255f, 0xB3 / 255f, 0xE6 / 255f, 1f);
        private static readonly Color ColSSR = new Color(0xF2 / 255f, 0xC1 / 255f, 0x4E / 255f, 1f);
        private static readonly Color ColLR = new Color(0xC0 / 255f, 0x8B / 255f, 0xF0 / 255f, 1f);
        private static readonly Color FlashWarm = new Color(1f, 0.973f, 0.918f, 0f);

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Config")]
        [SerializeField] private InvocationFlowConfig config;

        [Header("Références")]
        [SerializeField] private RawImage underglowImage;
        [SerializeField] private Image rimFrame;
        [SerializeField] private PixelParticleGraphic particles;
        [SerializeField] private RectTransform shakeContainer;
        [SerializeField] private Image flashOverlay;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _trauma;
        private float _flash;
        private Vector2 _shakeRest;
        private Coroutine _monteeRoutine;
        private Coroutine _punchRoutine;
        private Image[] _rimEdges;
        private CharacterRarity _activeRarity = CharacterRarity.SR;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>Consommé en INV2 par le material pixelate (montée).</summary>
        public float MonteeBrightness { get; private set; } = 0.45f;

        /// <summary>Consommé en INV2 par le material pixelate (résolution).</summary>
        public float ResolveBrightness { get; private set; } = 0.75f;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            if (shakeContainer != null)
                _shakeRest = shakeContainer.anchoredPosition;

            CacheRimEdges();
            ResetVisuals();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            if (_trauma > 0f)
            {
                _trauma = Mathf.Max(0f, _trauma - dt * TraumaDecay);
                ApplyShakeOffset();
            }
            else if (shakeContainer != null)
            {
                shakeContainer.anchoredPosition = _shakeRest;
            }

            if (_flash > 0f)
            {
                _flash = Mathf.Max(0f, _flash - dt * FlashDecay);
                ApplyFlashVisual();
            }

            if (particles != null && particles.AliveCount > 0)
                particles.Tick(dt);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Montée pulsée SSR/LR. SR : couche inactive (pas de glow).
        /// </summary>
        public void PlayMontee(CharacterRarity rarity, float dur)
        {
            StopMontee();
            _activeRarity = rarity;
            if (rarity == CharacterRarity.SR || dur <= 0.02f)
            {
                MonteeBrightness = 0.45f;
                SetUnderglowAlpha(0f);
                return;
            }

            _monteeRoutine = StartCoroutine(MonteeRoutine(rarity, dur));
        }

        /// <summary>
        /// Éval pure de résolution (t01 0..1). SR : underglow/rim = 0.
        /// </summary>
        public void ApplyResolve(CharacterRarity rarity, float t01)
        {
            _activeRarity = rarity;
            float t = Mathf.Clamp01(t01);
            ResolveBrightness = Mathf.Lerp(0.75f, 1f, t);

            float glow = config != null ? config.rarityGlowIntensity : 0.7f;
            bool charged = rarity != CharacterRarity.SR;

            float under = charged ? Mathf.Clamp01((t - 0.5f) * 2f) * glow * 0.7f : 0f;
            SetUnderglowColor(GetRarityColor(rarity));
            SetUnderglowAlpha(under);

            float rim = charged ? Mathf.Clamp01((t - 0.75f) * 4f) * glow : 0f;
            SetRim(GetRarityColor(rarity), rim);
        }

        /// <summary>
        /// Punch final : flash + trauma + burst d'étincelles + settle 0,22 s.
        /// </summary>
        public void Punch(CharacterRarity rarity)
        {
            StopPunch();
            _activeRarity = rarity;

            float punch = config != null ? config.punchIntensity : 0.7f;
            float flashAmt = (rarity == CharacterRarity.SR ? 0.25f : 0.5f) * punch;
            _flash = Mathf.Max(_flash, flashAmt);
            ApplyFlashVisual();

            float traumaAdd = GetTrauma(rarity) * punch;
            _trauma = Mathf.Clamp01(_trauma + traumaAdd);
            ApplyShakeOffset();

            int sparks = GetSparks(rarity);
            int count = Mathf.RoundToInt(sparks * (0.4f + punch));
            if (particles != null && count > 0)
            {
                Vector2 center = ResolveParticleCenter();
                particles.SetCenter(center);
                // API AW : SpawnBurst(Vector2) — une particule par appel, pas de teinte.
                for (int i = 0; i < count; i++)
                    particles.SpawnBurst(center);
            }

            _punchRoutine = StartCoroutine(PunchSettleRoutine());
        }

        /// <summary>Reset complet des FX (alphas, trauma, flash, particules).</summary>
        public void ResetVisuals()
        {
            StopMontee();
            StopPunch();
            _trauma = 0f;
            _flash = 0f;
            MonteeBrightness = 0.45f;
            ResolveBrightness = 0.75f;
            if (shakeContainer != null)
                shakeContainer.anchoredPosition = _shakeRest;
            SetUnderglowAlpha(0f);
            SetRim(Color.white, 0f);
            ApplyFlashVisual();
            if (particles != null)
                particles.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator MonteeRoutine(CharacterRarity rarity, float dur)
        {
            float glow = config != null ? config.rarityGlowIntensity : 0.7f;
            Color col = GetRarityColor(rarity);
            SetUnderglowColor(col);
            float t = 0f;

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = dur > 0f ? Mathf.Clamp01(t / dur) : 1f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(u * 4f * Mathf.PI - Mathf.PI * 0.5f);
                MonteeBrightness = Mathf.Lerp(0.45f, 0.62f, pulse);
                SetUnderglowAlpha(pulse * glow * 0.55f);
                yield return null;
            }

            MonteeBrightness = 0.45f;
            SetUnderglowAlpha(0f);
            _monteeRoutine = null;
        }

        private IEnumerator PunchSettleRoutine()
        {
            float t = 0f;
            while (t < PunchSettle)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            _punchRoutine = null;
        }

        private void StopMontee()
        {
            if (_monteeRoutine != null)
            {
                StopCoroutine(_monteeRoutine);
                _monteeRoutine = null;
            }
        }

        private void StopPunch()
        {
            if (_punchRoutine != null)
            {
                StopCoroutine(_punchRoutine);
                _punchRoutine = null;
            }
        }

        private void ApplyShakeOffset()
        {
            if (shakeContainer == null)
                return;

            float t2 = _trauma * _trauma;
            if (t2 < 0.0004f)
            {
                shakeContainer.anchoredPosition = _shakeRest;
                return;
            }

            float T = Time.unscaledTime;
            float ox = Mathf.Sin(T * ShakeFreqX) * ShakeAmpX * t2;
            float oy = Mathf.Cos(T * ShakeFreqY) * ShakeAmpY * t2;
            shakeContainer.anchoredPosition = _shakeRest + new Vector2(ox, oy);
        }

        private void ApplyFlashVisual()
        {
            if (flashOverlay == null)
                return;

            Color fc = FlashWarm;
            fc.a = _flash * _flash;
            flashOverlay.color = fc;
        }

        private void SetUnderglowColor(Color col)
        {
            if (underglowImage == null)
                return;
            Color c = col;
            c.a = underglowImage.color.a;
            underglowImage.color = c;
        }

        private void SetUnderglowAlpha(float a)
        {
            if (underglowImage == null)
                return;
            Color c = underglowImage.color;
            c.a = Mathf.Clamp01(a);
            underglowImage.color = c;
            underglowImage.enabled = c.a > 0.001f;
        }

        private void SetRim(Color col, float intensity)
        {
            Color c = col;
            c.a = Mathf.Clamp01(intensity);

            if (rimFrame != null)
            {
                rimFrame.color = c;
                rimFrame.enabled = c.a > 0.001f;
            }

            if (_rimEdges != null)
            {
                for (int i = 0; i < _rimEdges.Length; i++)
                {
                    if (_rimEdges[i] == null)
                        continue;
                    _rimEdges[i].color = c;
                    _rimEdges[i].enabled = c.a > 0.001f;
                }
            }
        }

        private void CacheRimEdges()
        {
            if (rimFrame == null)
            {
                _rimEdges = null;
                return;
            }

            Image[] kids = rimFrame.GetComponentsInChildren<Image>(true);
            int count = 0;
            for (int i = 0; i < kids.Length; i++)
            {
                if (kids[i] != rimFrame)
                    count++;
            }

            if (count <= 0)
            {
                _rimEdges = null;
                return;
            }

            _rimEdges = new Image[count];
            int w = 0;
            for (int i = 0; i < kids.Length; i++)
            {
                if (kids[i] != rimFrame)
                    _rimEdges[w++] = kids[i];
            }
        }

        private Vector2 ResolveParticleCenter()
        {
            if (shakeContainer != null)
                return shakeContainer.rect.center;
            if (particles != null)
                return ((RectTransform)particles.transform).rect.center;
            return Vector2.zero;
        }

        private static Color GetRarityColor(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SSR: return ColSSR;
                case CharacterRarity.LR: return ColLR;
                default: return ColSR;
            }
        }

        private static int GetSparks(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SSR: return 26;
                case CharacterRarity.LR: return 40;
                default: return 6;
            }
        }

        private static float GetTrauma(CharacterRarity rarity)
        {
            switch (rarity)
            {
                case CharacterRarity.SSR: return 0.45f;
                case CharacterRarity.LR: return 0.6f;
                default: return 0.15f;
            }
        }
    }
}
