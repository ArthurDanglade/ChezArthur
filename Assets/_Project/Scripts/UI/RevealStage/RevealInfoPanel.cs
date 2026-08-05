using System;
using System.Collections;
using ChezArthur.Audio;
using ChezArthur.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.RevealStage
{
    /// <summary>
    /// Panneau d'info reveal — artwork roi, aucun scrim. Tokens UiTheme uniquement.
    /// </summary>
    public class RevealInfoPanel : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // PAYLOAD
        // ═══════════════════════════════════════════
        public struct Payload
        {
            public string name;
            public CharacterRarity rarity;
            public bool isNew;
            public int prevLevel;
            public int newLevel;
            public bool isMax;
            public (string label, int delta)[] statDeltas;
        }

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float VOL_STAMP = 0.8f;
        private const float VOL_TICK = 0.5f;
        private const float UNDERLINE_TARGET = 0.46f;
        private const float UNDERLINE_DELAY = 0.12f;
        private const float UNDERLINE_DUR = 0.3f;
        private const float RARITY_DELAY = 0.22f;
        private const float RARITY_DUR = 0.2f;
        private const int MAX_STAT_ROWS = 4;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RevealStageConfig _config;
        private RevealPixelFxGraphic _fx;
        private CanvasGroup _rootGroup;
        private TextMeshProUGUI _nameText;
        private RectTransform _nameRt;
        private Image _underline;
        private RectTransform _underlineRt;
        private TextMeshProUGUI _rarityText;
        private RectTransform _stampRt;
        private CanvasGroup _stampGroup;
        private TextMeshProUGUI _stampText;
        private Image _stampFrame;
        private TextMeshProUGUI _levelChip;
        private Image _chipFill;
        private RectTransform _chipFillRt;
        private readonly TextMeshProUGUI[] _statRows = new TextMeshProUGUI[MAX_STAT_ROWS];
        private readonly RectTransform[] _statRts = new RectTransform[MAX_STAT_ROWS];
        private readonly CanvasGroup[] _statGroups = new CanvasGroup[MAX_STAT_ROWS];
        private Sprite _whiteSprite;
        private bool _built;
        private Coroutine _routine;
        private Coroutine _titleRoutine;

        // ═══════════════════════════════════════════
        // FACTORY
        // ═══════════════════════════════════════════

        /// <summary>Crée ou récupère le panneau sous parent.</summary>
        public static RevealInfoPanel EnsureUnder(Transform parent)
        {
            if (parent == null)
                return null;

            Transform existing = parent.Find("RevealInfoPanel");
            RevealInfoPanel panel;
            if (existing != null)
            {
                panel = existing.GetComponent<RevealInfoPanel>();
                if (panel == null)
                    panel = existing.gameObject.AddComponent<RevealInfoPanel>();
            }
            else
            {
                GameObject go = new GameObject(
                    "RevealInfoPanel",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(RevealInfoPanel));
                go.transform.SetParent(parent, false);
                panel = go.GetComponent<RevealInfoPanel>();
            }

            panel.EnsureBuilt();
            return panel;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void Configure(RevealStageConfig config) => _config = config;

        /// <summary>FX optionnel pour le burst du stamp NOUVEAU.</summary>
        public void BindFx(RevealPixelFxGraphic fx) => _fx = fx;

        public void HideImmediate()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_titleRoutine != null)
            {
                StopCoroutine(_titleRoutine);
                _titleRoutine = null;
            }

            if (_rootGroup != null)
            {
                _rootGroup.alpha = 0f;
                _rootGroup.blocksRaycasts = false;
                _rootGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        /// <summary>Joue la séquence info — n'attend PAS de tap.</summary>
        public IEnumerator CoPlay(Payload p)
        {
            EnsureBuilt();
            gameObject.SetActive(true);
            ResetVisualState();

            if (_rootGroup != null)
                _rootGroup.alpha = 1f;

            float nameDelay = _config != null ? _config.nameDelay : 0.10f;
            float nameDur = _config != null ? _config.nameDur : 0.25f;
            float statusDelay = _config != null ? _config.statusDelay : 0.42f;
            float chipFill = _config != null ? _config.chipFill : 0.45f;
            float tickStagger = _config != null ? _config.tickStagger : 0.12f;

            Color rarityCol = CharacterRarityPalette.GetColor(p.rarity);

            // Carte-titre en parallèle (fenêtres absolues — rythme preview INVR0).
            if (_titleRoutine != null)
            {
                StopCoroutine(_titleRoutine);
                _titleRoutine = null;
            }

            _titleRoutine = StartCoroutine(CoTitle(rarityCol, p.name, p.rarity, nameDur, nameDelay));

            // Beat statut à statusDelay absolu (chevauche le titre si besoin).
            float t = 0f;
            while (t < statusDelay)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (p.isNew)
            {
                yield return PlayStamp();
            }
            else if (p.isMax)
            {
                _levelChip.text = "Nv. MAX";
                _levelChip.color = UiTheme.TextMuted;
                SetTmpAlpha(_levelChip, 1f);
            }
            else
            {
                yield return PlayDuplicate(p, chipFill, tickStagger);
            }
        }

        /// <summary>
        /// Nom / souligné / rareté sur horloge absolue (chevauchements preview INVR0).
        /// </summary>
        private IEnumerator CoTitle(
            Color rarityCol,
            string characterName,
            CharacterRarity rarity,
            float nameDur,
            float nameDelay)
        {
            _nameText.text = characterName ?? string.Empty;
            _nameText.color = WithAlpha(UiTheme.TextPrimary, 0f);
            SetAnchoredY(_nameRt, 10f);

            _underline.color = rarityCol;
            _underlineRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            float underlineMax = ((RectTransform)transform).rect.width * UNDERLINE_TARGET;

            _rarityText.text = rarity.ToString();
            _rarityText.color = WithAlpha(rarityCol, 0f);

            float underlineStart = nameDelay + UNDERLINE_DELAY;
            float rarityStart = nameDelay + RARITY_DELAY;
            float nameEnd = nameDelay + nameDur;
            float underlineEnd = underlineStart + UNDERLINE_DUR;
            float rarityEnd = rarityStart + RARITY_DUR;
            float end = Mathf.Max(nameEnd, Mathf.Max(underlineEnd, rarityEnd));

            float t = 0f;
            while (t < end)
            {
                t += Time.unscaledDeltaTime;

                if (t >= nameDelay)
                {
                    float u = Mathf.Clamp01((t - nameDelay) / Mathf.Max(0.0001f, nameDur));
                    float e = EaseOutBack(u);
                    SetTmpAlpha(_nameText, Mathf.Clamp01(u * 1.4f));
                    SetAnchoredY(_nameRt, Mathf.Lerp(10f, 0f, e));
                }

                if (t >= underlineStart)
                {
                    float u = Mathf.Clamp01((t - underlineStart) / UNDERLINE_DUR);
                    _underlineRt.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Horizontal, underlineMax * EaseOutCubic(u));
                }

                if (t >= rarityStart)
                {
                    float u = Mathf.Clamp01((t - rarityStart) / RARITY_DUR);
                    SetTmpAlpha(_rarityText, u);
                }

                yield return null;
            }

            SetTmpAlpha(_nameText, 1f);
            SetAnchoredY(_nameRt, 0f);
            _underlineRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, underlineMax);
            SetTmpAlpha(_rarityText, 1f);
            _titleRoutine = null;
        }

        // ═══════════════════════════════════════════
        // SÉQUENCES
        // ═══════════════════════════════════════════

        private IEnumerator PlayStamp()
        {
            _stampRt.gameObject.SetActive(true);
            _stampGroup.alpha = 1f;
            _stampRt.localRotation = Quaternion.Euler(0f, 0f, -5f);
            _stampRt.localScale = Vector3.zero;
            _stampText.color = UiTheme.BadgeNew;
            _stampFrame.color = UiTheme.BadgeNew;

            PlaySfx(_config != null ? _config.stampClip : null, VOL_STAMP);

            if (_fx != null)
            {
                Vector2 local = _stampRt.anchoredPosition;
                _fx.SpawnBurst(local, UiTheme.BadgeNew, 24);
            }

            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / 0.35f);
                _stampRt.localScale = Vector3.one * EaseOutBack(u);
                yield return null;
            }

            _stampRt.localScale = Vector3.one;
        }

        private IEnumerator PlayDuplicate(Payload p, float chipFillDur, float tickStagger)
        {
            _levelChip.text = $"Nv. {p.prevLevel} → {p.newLevel}";
            _levelChip.color = UiTheme.TextPrimary;
            SetTmpAlpha(_levelChip, 1f);

            // Mini-barre fill
            _chipFill.color = UiTheme.Gold;
            _chipFillRt.anchorMax = new Vector2(0f, 1f);
            float t = 0f;
            while (t < chipFillDur)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / chipFillDur);
                _chipFillRt.anchorMax = new Vector2(EaseOutCubic(u), 1f);
                yield return null;
            }

            _chipFillRt.anchorMax = new Vector2(1f, 1f);

            int count = p.statDeltas != null
                ? Mathf.Min(MAX_STAT_ROWS, p.statDeltas.Length)
                : 0;

            for (int i = 0; i < count; i++)
            {
                var (label, delta) = p.statDeltas[i];
                string sign = delta >= 0 ? "+" : string.Empty;
                _statRows[i].text = $"{label} {sign}{delta}";
                _statRows[i].color = delta >= 0 ? UiTheme.Positive : UiTheme.Negative;
                _statGroups[i].alpha = 0f;
                SetAnchoredY(_statRts[i], 8f);
                _statRts[i].gameObject.SetActive(true);

                PlaySfx(_config != null ? _config.statTickClip : null, VOL_TICK);

                float anim = 0f;
                while (anim < 0.22f)
                {
                    anim += Time.unscaledDeltaTime;
                    float u = Mathf.Clamp01(anim / 0.22f);
                    _statGroups[i].alpha = u;
                    SetAnchoredY(_statRts[i], Mathf.Lerp(8f, 0f, EaseOutCubic(u)));
                    yield return null;
                }

                _statGroups[i].alpha = 1f;
                SetAnchoredY(_statRts[i], 0f);

                float wait = 0f;
                while (wait < tickStagger)
                {
                    wait += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }

        // ═══════════════════════════════════════════
        // BUILD UI
        // ═══════════════════════════════════════════

        private void EnsureBuilt()
        {
            if (_built && _rootGroup != null && _nameText != null)
                return;

            RectTransform rootRt = transform as RectTransform;
            if (rootRt == null)
                rootRt = gameObject.AddComponent<RectTransform>();

            StretchBottom(rootRt, 0f, 0.28f);

            if (_rootGroup == null)
                _rootGroup = GetComponent<CanvasGroup>();
            if (_rootGroup == null)
                _rootGroup = gameObject.AddComponent<CanvasGroup>();

            _nameText = EnsureTmp(transform, "Name", UiTheme.CardFontName, FontStyles.Bold,
                TextAlignmentOptions.BottomLeft);
            _nameRt = _nameText.rectTransform;
            Place(_nameRt, 0.06f, 0.55f, 0.94f, 0.95f);
            _nameText.color = UiTheme.TextPrimary;

            _underlineRt = EnsureChild(transform, "Underline");
            Place(_underlineRt, 0.06f, 0.48f, 0.06f, 0.48f);
            _underlineRt.pivot = new Vector2(0f, 0.5f);
            _underlineRt.sizeDelta = new Vector2(0f, 3f);
            _underline = _underlineRt.gameObject.GetComponent<Image>();
            if (_underline == null)
                _underline = _underlineRt.gameObject.AddComponent<Image>();
            _underline.sprite = GetWhiteSprite();
            _underline.raycastTarget = false;

            _rarityText = EnsureTmp(transform, "Rarity", UiTheme.FontLabel, FontStyles.Normal,
                TextAlignmentOptions.BottomLeft);
            Place(_rarityText.rectTransform, 0.06f, 0.38f, 0.5f, 0.50f);

            // Stamp NOUVEAU
            _stampRt = EnsureChild(transform, "Stamp");
            Place(_stampRt, 0.55f, 0.55f, 0.92f, 0.92f);
            _stampGroup = _stampRt.gameObject.GetComponent<CanvasGroup>();
            if (_stampGroup == null)
                _stampGroup = _stampRt.gameObject.AddComponent<CanvasGroup>();
            _stampFrame = _stampRt.gameObject.GetComponent<Image>();
            if (_stampFrame == null)
                _stampFrame = _stampRt.gameObject.AddComponent<Image>();
            _stampFrame.sprite = GetWhiteSprite();
            _stampFrame.color = UiTheme.BadgeNew;
            _stampFrame.type = Image.Type.Sliced;
            // Cadre 3 px approximé via child fill sombre
            RectTransform fillRt = EnsureChild(_stampRt, "StampFill");
            StretchInset(fillRt, 3f);
            Image fillImg = fillRt.gameObject.GetComponent<Image>();
            if (fillImg == null)
                fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.sprite = GetWhiteSprite();
            fillImg.color = UiTheme.BgDeep;
            fillImg.raycastTarget = false;
            _stampText = EnsureTmp(_stampRt, "StampLabel", UiTheme.FontHeader, FontStyles.Bold,
                TextAlignmentOptions.Center);
            StretchFull(_stampText.rectTransform);
            _stampText.text = "NOUVEAU";
            _stampText.color = UiTheme.BadgeNew;
            _stampRt.gameObject.SetActive(false);

            _levelChip = EnsureTmp(transform, "LevelChip", UiTheme.FontBody, FontStyles.Bold,
                TextAlignmentOptions.BottomLeft);
            Place(_levelChip.rectTransform, 0.06f, 0.22f, 0.55f, 0.38f);
            SetTmpAlpha(_levelChip, 0f);

            // Mini-barre sous chip
            RectTransform trackRt = EnsureChild(transform, "ChipTrack");
            Place(trackRt, 0.06f, 0.18f, 0.45f, 0.21f);
            Image trackImg = trackRt.gameObject.GetComponent<Image>();
            if (trackImg == null)
                trackImg = trackRt.gameObject.AddComponent<Image>();
            trackImg.sprite = GetWhiteSprite();
            trackImg.color = WithAlpha(UiTheme.BorderSubtle, 0.6f);
            trackImg.raycastTarget = false;

            _chipFillRt = EnsureChild(trackRt, "ChipFill");
            _chipFillRt.anchorMin = Vector2.zero;
            _chipFillRt.anchorMax = new Vector2(0f, 1f);
            _chipFillRt.offsetMin = Vector2.zero;
            _chipFillRt.offsetMax = Vector2.zero;
            _chipFillRt.pivot = new Vector2(0f, 0.5f);
            _chipFill = _chipFillRt.gameObject.GetComponent<Image>();
            if (_chipFill == null)
                _chipFill = _chipFillRt.gameObject.AddComponent<Image>();
            _chipFill.sprite = GetWhiteSprite();
            _chipFill.color = UiTheme.Gold;
            _chipFill.raycastTarget = false;

            for (int i = 0; i < MAX_STAT_ROWS; i++)
            {
                _statRows[i] = EnsureTmp(transform, "Stat_" + i, UiTheme.FontLabel, FontStyles.Normal,
                    TextAlignmentOptions.MidlineLeft);
                _statRts[i] = _statRows[i].rectTransform;
                float y0 = 0.02f + i * 0.04f;
                Place(_statRts[i], 0.55f, y0, 0.94f, y0 + 0.06f);
                _statGroups[i] = _statRts[i].gameObject.GetComponent<CanvasGroup>();
                if (_statGroups[i] == null)
                    _statGroups[i] = _statRts[i].gameObject.AddComponent<CanvasGroup>();
                _statGroups[i].alpha = 0f;
                _statRts[i].gameObject.SetActive(false);
            }

            DisableRaycasts();
            _built = true;
        }

        private void ResetVisualState()
        {
            SetTmpAlpha(_nameText, 0f);
            SetAnchoredY(_nameRt, 10f);
            if (_underlineRt != null)
                _underlineRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            SetTmpAlpha(_rarityText, 0f);
            SetTmpAlpha(_levelChip, 0f);
            if (_stampRt != null)
                _stampRt.gameObject.SetActive(false);
            if (_chipFillRt != null)
                _chipFillRt.anchorMax = new Vector2(0f, 1f);
            for (int i = 0; i < MAX_STAT_ROWS; i++)
            {
                if (_statGroups[i] != null)
                    _statGroups[i].alpha = 0f;
                if (_statRts[i] != null)
                    _statRts[i].gameObject.SetActive(false);
            }
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private void PlaySfx(AudioClip clip, float vol)
        {
            if (clip == null || SfxManager.Instance == null)
                return;
            SfxManager.Instance.PlaySfx(clip, vol);
        }

        private Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;

            Texture2D tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _whiteSprite;
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent, string name, float size, FontStyles style, TextAlignmentOptions align)
        {
            Transform t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : CreateChild(parent, name);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = UiTheme.TextPrimary;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;
            // Outline sombre — lisibilité sans scrim (artwork roi)
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = WithAlpha(UiTheme.BgDeep, 0.85f);
            return tmp;
        }

        private static RectTransform EnsureChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null)
                return t as RectTransform;
            return CreateChild(parent, name).GetComponent<RectTransform>();
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Place(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void StretchBottom(RectTransform rt, float minY, float maxY)
        {
            rt.anchorMin = new Vector2(0f, minY);
            rt.anchorMax = new Vector2(1f, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
        }

        private void DisableRaycasts()
        {
            var graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].raycastTarget = false;
        }

        private static void SetTmpAlpha(TextMeshProUGUI tmp, float a)
        {
            if (tmp == null) return;
            Color c = tmp.color;
            c.a = a;
            tmp.color = c;
        }

        private static void SetAnchoredY(RectTransform rt, float y)
        {
            if (rt == null) return;
            Vector2 p = rt.anchoredPosition;
            p.y = y;
            rt.anchoredPosition = p;
        }

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }

        private static float EaseOutCubic(float x)
        {
            float inv = 1f - Mathf.Clamp01(x);
            return 1f - inv * inv * inv;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            x = Mathf.Clamp01(x);
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
