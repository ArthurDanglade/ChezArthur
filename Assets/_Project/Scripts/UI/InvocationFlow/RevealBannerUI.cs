using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;

namespace ChezArthur.UI.InvocationFlow
{
    /// <summary>
    /// Bandeau « artwork roi » — plein (nouveau) / compact (doublon). Timings verbatim preview INV0.
    /// </summary>
    public class RevealBannerUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float FadeOutDur = 0.15f;
        private const float RarityBarMaxPx = 86f;
        private const int StatChipCount = 4;

        private static readonly Color ColSR = new Color(0x7F / 255f, 0xB3 / 255f, 0xE6 / 255f, 1f);
        private static readonly Color ColSSR = new Color(0xF2 / 255f, 0xC1 / 255f, 0x4E / 255f, 1f);
        private static readonly Color ColLR = new Color(0xC0 / 255f, 0x8B / 255f, 0xF0 / 255f, 1f);
        private static readonly Color StatusNew = new Color(0.95f, 0.82f, 0.40f, 1f);
        private static readonly Color StatusDup = new Color(0.55f, 0.55f, 0.60f, 1f);
        private static readonly Color LevelFlashGold = new Color(1f, 0.85f, 0.35f, 1f);

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Config")]
        [SerializeField] private InvocationFlowConfig config;

        [Header("Racine")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform rootRect;

        [Header("Identité")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image rarityBar;
        [SerializeField] private TextMeshProUGUI levelChip;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Stats (plein uniquement)")]
        [SerializeField] private CanvasGroup[] statChipGroups;
        [SerializeField] private TextMeshProUGUI[] statChipLabels;
        [SerializeField] private RectTransform[] statChipRects;

        [Header("XP")]
        [SerializeField] private RectTransform xpLineFill;
        [SerializeField] private TextMeshProUGUI xpChip;

        [Header("Audio (optionnel)")]
        [SerializeField] private AudioSource oneshotSource;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Coroutine _playRoutine;
        private Coroutine _fadeRoutine;
        private Color _levelChipBase = Color.white;
        private float _xpLineFullWidth;
        private readonly float[] _statRestY = new float[StatChipCount];
        private bool _statRestCached;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (rootRect == null)
                rootRect = transform as RectTransform;

            if (levelChip != null)
                _levelChipBase = levelChip.color;

            if (xpLineFill != null)
                _xpLineFullWidth = Mathf.Max(1f, xpLineFill.sizeDelta.x);

            CacheStatRests();
            HideImmediate();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Bandeau plein (nouveau) : chips stats en cascade, barre rareté, XP.
        /// </summary>
        public void PlayFull(
            string nom,
            CharacterRarity rarity,
            int niveau,
            int[] stats,
            bool isNew,
            float xp01,
            float dur = -1f)
        {
            float duration = dur > 0f
                ? dur
                : (config != null ? config.bannerFullDuration : 0.9f);

            StopPlay();
            PrepareVisible();
            ApplyIdentity(nom, rarity, $"Nv. {niveau}", isNew, showStatChips: true);
            ApplyStatLabels(stats);
            PlayOneShot(config != null ? config.xpClip : null);

            // Harness : coroutine locale (GO doit être actif). Gacha préfère CoPlayFull.
            if (isActiveAndEnabled)
                _playRoutine = StartCoroutine(PlayFullRoutine(duration, Mathf.Clamp01(xp01)));
        }

        /// <summary>
        /// Variante yieldable — tourne sur l'appelant (Gacha), GO inactif-safe après PrepareVisible.
        /// </summary>
        public IEnumerator CoPlayFull(
            string nom,
            CharacterRarity rarity,
            int niveau,
            int[] stats,
            bool isNew,
            float xp01,
            float dur = -1f)
        {
            float duration = dur > 0f
                ? dur
                : (config != null ? config.bannerFullDuration : 0.9f);

            StopPlay();
            PrepareVisible();
            ApplyIdentity(nom, rarity, $"Nv. {niveau}", isNew, showStatChips: true);
            ApplyStatLabels(stats);
            PlayOneShot(config != null ? config.xpClip : null);
            yield return PlayFullRoutine(duration, Mathf.Clamp01(xp01));
        }

        /// <summary>
        /// Bandeau compact (doublon) : pas de chips stats, chip niveau A→B, flash level-up.
        /// </summary>
        public void PlayCompact(
            string nom,
            CharacterRarity rarity,
            int niveauAvant,
            int niveauAprès,
            float xp01,
            float dur = -1f)
        {
            float duration = dur > 0f
                ? dur
                : (config != null ? config.bannerCompactDuration : 0.4f);

            StopPlay();
            PrepareVisible();
            bool levelUp = niveauAprès > niveauAvant;
            ApplyIdentity(
                nom, rarity,
                $"Nv. {niveauAvant} → {niveauAprès}",
                isNew: false,
                showStatChips: false);
            PlayOneShot(config != null ? config.xpClip : null);

            if (isActiveAndEnabled)
                _playRoutine = StartCoroutine(
                    PlayCompactRoutine(duration, Mathf.Clamp01(xp01), levelUp));
        }

        /// <summary>Variante yieldable compact — safe si le prefab démarre inactif.</summary>
        public IEnumerator CoPlayCompact(
            string nom,
            CharacterRarity rarity,
            int niveauAvant,
            int niveauAprès,
            float xp01,
            float dur = -1f)
        {
            float duration = dur > 0f
                ? dur
                : (config != null ? config.bannerCompactDuration : 0.4f);

            StopPlay();
            PrepareVisible();
            bool levelUp = niveauAprès > niveauAvant;
            ApplyIdentity(
                nom, rarity,
                $"Nv. {niveauAvant} → {niveauAprès}",
                isNew: false,
                showStatChips: false);
            PlayOneShot(config != null ? config.xpClip : null);
            yield return PlayCompactRoutine(duration, Mathf.Clamp01(xp01), levelUp);
        }

        /// <summary>Masque immédiat (pas de fade).</summary>
        public void HideImmediate()
        {
            StopPlay();
            StopFade();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        /// <summary>Fade-out 0,15 s puis désactive.</summary>
        public void FadeOut()
        {
            StopPlay();
            StopFade();
            if (!gameObject.activeSelf)
                return;
            _fadeRoutine = StartCoroutine(FadeOutRoutine());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator PlayFullRoutine(float dur, float xp01)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = dur > 0f ? Mathf.Clamp01(t / dur) : 1f;
                EvalFullFrame(t, u, dur, xp01, animateLevelFlash: false);
                yield return null;
            }

            EvalFullFrame(dur, 1f, dur, xp01, animateLevelFlash: false);
            _playRoutine = null;
        }

        private IEnumerator PlayCompactRoutine(float dur, float xp01, bool levelUp)
        {
            bool playedLvlSfx = false;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = dur > 0f ? Mathf.Clamp01(t / dur) : 1f;
                EvalCompactFrame(t, u, dur, xp01, levelUp);

                // Level-up SFX une fois la ligne XP remplie
                if (levelUp && !playedLvlSfx)
                {
                    float xt = Mathf.Clamp01((t - 0.1f) / 0.5f);
                    if (xt >= 1f)
                    {
                        playedLvlSfx = true;
                        PlayOneShot(config != null ? config.levelUpClip : null);
                    }
                }

                yield return null;
            }

            EvalCompactFrame(dur, 1f, dur, xp01, levelUp);
            if (levelChip != null)
                levelChip.color = _levelChipBase;
            _playRoutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            float start = canvasGroup != null ? canvasGroup.alpha : 1f;
            float t = 0f;
            while (t < FadeOutDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / FadeOutDur);
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(start, 0f, u);
                yield return null;
            }

            HideImmediate();
            _fadeRoutine = null;
        }

        private void EvalFullFrame(float t, float u, float dur, float xp01, bool animateLevelFlash)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Min(1f, u * 6f);

            if (rarityBar != null)
            {
                float w = Mathf.Min(1f, u * 3.5f) * RarityBarMaxPx;
                Vector2 sd = rarityBar.rectTransform.sizeDelta;
                sd.x = w;
                rarityBar.rectTransform.sizeDelta = sd;
            }

            EvalStatChips(t);
            EvalXp(t, u, dur, xp01);
            EvalStatus(t);

            if (animateLevelFlash && levelChip != null)
                ApplyLevelFlash(u);
        }

        private void EvalCompactFrame(float t, float u, float dur, float xp01, bool levelUp)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Min(1f, u * 6f);

            if (rarityBar != null)
            {
                float w = Mathf.Min(1f, u * 3.5f) * RarityBarMaxPx;
                Vector2 sd = rarityBar.rectTransform.sizeDelta;
                sd.x = w;
                rarityBar.rectTransform.sizeDelta = sd;
            }

            HideStatChips();
            EvalXp(t, u, dur, xp01);
            EvalStatus(t);

            if (levelUp && levelChip != null)
                ApplyLevelFlash(u);
        }

        private void EvalStatChips(float t)
        {
            if (statChipGroups == null)
                return;

            for (int i = 0; i < statChipGroups.Length && i < StatChipCount; i++)
            {
                float ct = Mathf.Clamp01((t - (0.16f + i * 0.06f)) / 0.2f);
                float eased = OutCubic(ct);
                if (statChipGroups[i] != null)
                    statChipGroups[i].alpha = ct;

                if (statChipRects != null && i < statChipRects.Length && statChipRects[i] != null)
                {
                    Vector2 ap = statChipRects[i].anchoredPosition;
                    ap.y = _statRestY[i] + 6f * (1f - eased);
                    statChipRects[i].anchoredPosition = ap;
                }
            }
        }

        private void EvalXp(float t, float u, float dur, float xp01)
        {
            float xt = Mathf.Clamp01((t - 0.1f) / 0.5f);
            float fill = OutCubic(xt) * xp01;

            if (xpLineFill != null)
            {
                Vector2 sd = xpLineFill.sizeDelta;
                sd.x = _xpLineFullWidth * fill;
                xpLineFill.sizeDelta = sd;
            }

            if (xpChip != null)
            {
                bool vis = t >= 0.15f * dur && t <= 0.9f * dur;
                SetTmpAlpha(xpChip, vis ? 0.9f : 0f);
            }
        }

        private void EvalStatus(float t)
        {
            if (statusText == null)
                return;
            // Fade 0,22 → 0,42 s
            float a = Mathf.Clamp01((t - 0.22f) / 0.2f);
            SetTmpAlpha(statusText, a);
        }

        private void ApplyLevelFlash(float u)
        {
            // Flash doré bref en début de bandeau compact level-up
            float flash = 1f - Mathf.Clamp01(u / 0.35f);
            levelChip.color = Color.Lerp(_levelChipBase, LevelFlashGold, flash);
        }

        private void ApplyIdentity(
            string nom,
            CharacterRarity rarity,
            string levelLabel,
            bool isNew,
            bool showStatChips)
        {
            if (nameText != null)
                nameText.text = nom ?? string.Empty;

            Color rc = GetRarityColor(rarity);
            if (rarityBar != null)
            {
                Color c = rc;
                c.a = 1f;
                rarityBar.color = c;
                Vector2 sd = rarityBar.rectTransform.sizeDelta;
                sd.x = 0f;
                rarityBar.rectTransform.sizeDelta = sd;
            }

            if (levelChip != null)
            {
                levelChip.text = levelLabel;
                levelChip.color = _levelChipBase;
            }

            if (statusText != null)
            {
                statusText.text = isNew ? "Nouveau" : "Doublon";
                statusText.color = isNew ? StatusNew : StatusDup;
                SetTmpAlpha(statusText, 0f);
            }

            if (xpChip != null)
            {
                xpChip.text = "+XP";
                SetTmpAlpha(xpChip, 0f);
            }

            if (xpLineFill != null)
            {
                Vector2 sd = xpLineFill.sizeDelta;
                sd.x = 0f;
                xpLineFill.sizeDelta = sd;
            }

            if (showStatChips)
                ResetStatChipsHidden();
            else
                HideStatChips();
        }

        private void ApplyStatLabels(int[] stats)
        {
            if (statChipLabels == null)
                return;

            string[] prefixes = { "HP ", "ATK ", "DEF ", "SPD " };
            for (int i = 0; i < statChipLabels.Length && i < StatChipCount; i++)
            {
                if (statChipLabels[i] == null)
                    continue;
                int v = (stats != null && i < stats.Length) ? stats[i] : 0;
                statChipLabels[i].text = prefixes[i] + v;
            }
        }

        private void ResetStatChipsHidden()
        {
            CacheStatRests();
            if (statChipGroups == null)
                return;
            for (int i = 0; i < statChipGroups.Length; i++)
            {
                if (statChipGroups[i] != null)
                    statChipGroups[i].alpha = 0f;
                if (statChipRects != null && i < statChipRects.Length && statChipRects[i] != null)
                {
                    Vector2 ap = statChipRects[i].anchoredPosition;
                    ap.y = _statRestY[i] + 6f;
                    statChipRects[i].anchoredPosition = ap;
                }
            }
        }

        private void HideStatChips()
        {
            if (statChipGroups == null)
                return;
            for (int i = 0; i < statChipGroups.Length; i++)
            {
                if (statChipGroups[i] != null)
                    statChipGroups[i].alpha = 0f;
            }
        }

        private void CacheStatRests()
        {
            if (_statRestCached || statChipRects == null)
                return;
            for (int i = 0; i < StatChipCount && i < statChipRects.Length; i++)
            {
                if (statChipRects[i] != null)
                    _statRestY[i] = statChipRects[i].anchoredPosition.y;
            }
            _statRestCached = true;
        }

        private void PrepareVisible()
        {
            // Prefab INV1 démarre inactif (m_IsActive:0) — forcer avant toute coroutine locale.
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            enabled = true;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        private void StopPlay()
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }
        }

        private void StopFade()
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null || oneshotSource == null)
                return;
            oneshotSource.PlayOneShot(clip);
        }

        private static void SetTmpAlpha(TextMeshProUGUI tmp, float a)
        {
            Color c = tmp.color;
            c.a = Mathf.Clamp01(a);
            tmp.color = c;
        }

        private static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inv = 1f - t;
            return 1f - inv * inv * inv;
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
    }
}
