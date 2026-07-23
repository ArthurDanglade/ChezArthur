using System;
using System.Collections;
using ChezArthur.Characters;
using ChezArthur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Bandeau reveal gacha : scrim transparent, barre XP cosmétique, anim niveau + stats.
    /// Slots AudioClip vides = silencieux (Epidemic à brancher plus tard).
    /// </summary>
    public class GachaRevealStatusUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float XP_FILL_DURATION = 0.85f;
        private const float STAT_TICK_DURATION = 0.55f;
        private const float LEVEL_POP_DURATION = 0.35f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Racine")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image scrimImage;

        [Header("Identité")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI rarityText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject maxBadge;

        [Header("XP")]
        [SerializeField] private Image xpFill;
        [SerializeField] private Image xpTrack;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI spdText;
        [SerializeField] private GameObject statsRow;

        [Header("Audio (Epidemic — optionnel)")]
        [SerializeField] private AudioClip xpProgressClip;
        [SerializeField] private AudioClip levelUpClip;
        [SerializeField] private AudioClip statTickClip;
        [SerializeField] private AudioClip maxConfirmClip;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Sprite _whiteSprite;
        private bool _built;
        private Coroutine _routine;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée le bandeau sous parent si les refs manquent (runtime Hub).
        /// </summary>
        public static GachaRevealStatusUI EnsureUnder(Transform revealScene)
        {
            if (revealScene == null)
                return null;

            Transform existing = revealScene.Find("RevealStatusOverlay");
            GachaRevealStatusUI ui;
            if (existing != null)
            {
                ui = existing.GetComponent<GachaRevealStatusUI>();
                if (ui == null)
                    ui = existing.gameObject.AddComponent<GachaRevealStatusUI>();
            }
            else
            {
                GameObject go = new GameObject(
                    "RevealStatusOverlay",
                    typeof(RectTransform),
                    typeof(CanvasGroup),
                    typeof(GachaRevealStatusUI));
                go.transform.SetParent(revealScene, false);
                ui = go.GetComponent<GachaRevealStatusUI>();
            }

            ui.EnsureBuilt();
            return ui;
        }

        public void HideImmediate()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.blocksRaycasts = false;
                rootGroup.interactable = false;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Branche les clips Epidemic (null = silencieux pour ce slot).
        /// </summary>
        public void ConfigureAudio(
            AudioClip xpProgress,
            AudioClip levelUp,
            AudioClip statTick,
            AudioClip maxConfirm)
        {
            if (xpProgress != null) xpProgressClip = xpProgress;
            if (levelUp != null) levelUpClip = levelUp;
            if (statTick != null) statTickClip = statTick;
            if (maxConfirm != null) maxConfirmClip = maxConfirm;
        }

        /// <summary>
        /// Joue l'anim de statut après résolution pixel. Skip via callback externe = Stop.
        /// </summary>
        public IEnumerator PlayStatus(CharacterData data, PulledCharacter pulled)
        {
            EnsureBuilt();
            gameObject.SetActive(true);

            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.blocksRaycasts = false;
            }

            ApplyIdentity(data, pulled);
            ResetXpAndStats(data, pulled);

            // Fade-in scrim.
            float fade = 0.28f;
            float t = 0f;
            while (t < fade)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / fade);
                if (rootGroup != null)
                    rootGroup.alpha = u * u * (3f - 2f * u);
                yield return null;
            }

            if (rootGroup != null)
                rootGroup.alpha = 1f;

            if (pulled.isNew)
            {
                yield return PlayNewCharacter(data);
            }
            else if (pulled.previousLevel >= CharacterData.MAX_LEVEL
                     || pulled.newLevel <= pulled.previousLevel)
            {
                yield return PlayAlreadyMax();
            }
            else
            {
                yield return PlayLevelUp(data, pulled);
            }
        }

        // ═══════════════════════════════════════════
        // BUILD UI
        // ═══════════════════════════════════════════

        private void EnsureBuilt()
        {
            if (_built && rootGroup != null && nameText != null)
                return;

            RectTransform rootRt = transform as RectTransform;
            if (rootRt == null)
                rootRt = gameObject.AddComponent<RectTransform>();

            StretchBottom(rootRt, 0f, 0.34f);

            if (rootGroup == null)
                rootGroup = GetComponent<CanvasGroup>();
            if (rootGroup == null)
                rootGroup = gameObject.AddComponent<CanvasGroup>();

            if (scrimImage == null)
            {
                Transform scrimT = transform.Find("Scrim");
                GameObject scrimGo = scrimT != null
                    ? scrimT.gameObject
                    : CreateChild(transform, "Scrim");
                scrimImage = scrimGo.GetComponent<Image>();
                if (scrimImage == null)
                    scrimImage = scrimGo.AddComponent<Image>();
                StretchFull(scrimGo.GetComponent<RectTransform>());
                scrimImage.sprite = GetWhiteSprite();
                scrimImage.type = Image.Type.Simple;
                scrimImage.color = new Color(0.04f, 0.04f, 0.06f, 0.55f);
                scrimImage.raycastTarget = false;
            }

            // Gradient approximé : second scrim plus opaque en bas.
            Transform gradT = transform.Find("ScrimBottom");
            if (gradT == null)
            {
                GameObject g = CreateChild(transform, "ScrimBottom");
                Image gi = g.AddComponent<Image>();
                gi.sprite = GetWhiteSprite();
                gi.color = new Color(0.02f, 0.02f, 0.03f, 0.72f);
                gi.raycastTarget = false;
                RectTransform grt = g.GetComponent<RectTransform>();
                grt.anchorMin = new Vector2(0f, 0f);
                grt.anchorMax = new Vector2(1f, 0.55f);
                grt.offsetMin = Vector2.zero;
                grt.offsetMax = Vector2.zero;
            }

            nameText = EnsureTmp(transform, "Name", 52f, FontStyles.Bold, TextAlignmentOptions.Center);
            rarityText = EnsureTmp(transform, "Rarity", 30f, FontStyles.Normal, TextAlignmentOptions.Center);
            levelText = EnsureTmp(transform, "Level", 34f, FontStyles.Bold, TextAlignmentOptions.Center);

            PlaceText(nameText, 0.78f, 0.96f);
            PlaceText(rarityText, 0.66f, 0.78f);
            PlaceText(levelText, 0.52f, 0.66f);

            if (maxBadge == null)
            {
                Transform mb = transform.Find("MaxBadge");
                if (mb == null)
                {
                    GameObject badge = CreateChild(transform, "MaxBadge");
                    Image bi = badge.AddComponent<Image>();
                    bi.sprite = GetWhiteSprite();
                    bi.color = new Color(0.85f, 0.65f, 0.15f, 0.9f);
                    RectTransform brt = badge.GetComponent<RectTransform>();
                    brt.anchorMin = new Vector2(0.5f, 0.52f);
                    brt.anchorMax = new Vector2(0.5f, 0.52f);
                    brt.sizeDelta = new Vector2(140f, 40f);
                    TextMeshProUGUI bt = EnsureTmp(badge.transform, "Label", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
                    bt.text = "MAX";
                    bt.color = Color.black;
                    StretchFull(bt.rectTransform);
                    maxBadge = badge;
                }
                else
                {
                    maxBadge = mb.gameObject;
                }
            }

            EnsureXpBar();
            EnsureStatsRow();

            _built = true;
        }

        private void EnsureXpBar()
        {
            if (xpTrack != null && xpFill != null)
                return;

            Transform trackT = transform.Find("XpTrack");
            GameObject trackGo = trackT != null ? trackT.gameObject : CreateChild(transform, "XpTrack");
            xpTrack = trackGo.GetComponent<Image>();
            if (xpTrack == null)
                xpTrack = trackGo.AddComponent<Image>();
            xpTrack.sprite = GetWhiteSprite();
            xpTrack.color = new Color(1f, 1f, 1f, 0.18f);
            RectTransform trt = trackGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.12f, 0.40f);
            trt.anchorMax = new Vector2(0.88f, 0.46f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            Transform fillT = trackGo.transform.Find("XpFill");
            GameObject fillGo = fillT != null ? fillT.gameObject : CreateChild(trackGo.transform, "XpFill");
            xpFill = fillGo.GetComponent<Image>();
            if (xpFill == null)
                xpFill = fillGo.AddComponent<Image>();
            xpFill.sprite = GetWhiteSprite();
            xpFill.type = Image.Type.Filled;
            xpFill.fillMethod = Image.FillMethod.Horizontal;
            xpFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            xpFill.color = new Color(0.95f, 0.82f, 0.35f, 1f);
            StretchFull(fillGo.GetComponent<RectTransform>());
            xpFill.fillAmount = 0f;
        }

        private void EnsureStatsRow()
        {
            if (statsRow != null && hpText != null)
                return;

            Transform rowT = transform.Find("StatsRow");
            GameObject row = rowT != null ? rowT.gameObject : CreateChild(transform, "StatsRow");
            statsRow = row;
            RectTransform rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.06f, 0.08f);
            rrt.anchorMax = new Vector2(0.94f, 0.36f);
            rrt.offsetMin = Vector2.zero;
            rrt.offsetMax = Vector2.zero;

            hpText = EnsureStatCell(row.transform, "HP", 0);
            atkText = EnsureStatCell(row.transform, "ATK", 1);
            defText = EnsureStatCell(row.transform, "DEF", 2);
            spdText = EnsureStatCell(row.transform, "SPD", 3);
        }

        private TextMeshProUGUI EnsureStatCell(Transform parent, string label, int index)
        {
            string name = "Stat_" + label;
            Transform t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : CreateChild(parent, name);
            RectTransform rt = go.GetComponent<RectTransform>();
            float x0 = index * 0.25f;
            rt.anchorMin = new Vector2(x0, 0f);
            rt.anchorMax = new Vector2(x0 + 0.25f, 1f);
            rt.offsetMin = new Vector2(4f, 0f);
            rt.offsetMax = new Vector2(-4f, 0f);

            TextMeshProUGUI labelTmp = EnsureTmp(go.transform, "Label", 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            labelTmp.text = label;
            labelTmp.color = new Color(1f, 1f, 1f, 0.55f);
            PlaceText(labelTmp, 0.55f, 1f);

            TextMeshProUGUI valueTmp = EnsureTmp(go.transform, "Value", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            valueTmp.color = Color.white;
            PlaceText(valueTmp, 0f, 0.55f);
            return valueTmp;
        }

        // ═══════════════════════════════════════════
        // SEQUENCES
        // ═══════════════════════════════════════════

        private void ApplyIdentity(CharacterData data, PulledCharacter pulled)
        {
            if (nameText != null)
                nameText.text = data != null ? data.CharacterName : pulled.characterId;

            if (rarityText != null && data != null)
            {
                rarityText.text = data.Rarity.ToString();
                rarityText.color = CharacterRarityPalette.GetColor(data.Rarity);
            }

            if (maxBadge != null)
                maxBadge.SetActive(false);
        }

        private void ResetXpAndStats(CharacterData data, PulledCharacter pulled)
        {
            if (xpFill != null)
                xpFill.fillAmount = 0f;

            int displayLevel = pulled.isNew
                ? 1
                : Mathf.Max(1, pulled.previousLevel);

            if (levelText != null)
                levelText.text = "Nv." + displayLevel;

            if (statsRow != null)
                statsRow.SetActive(data != null);

            if (data == null)
                return;

            SetStatTexts(
                data.GetHpAtLevel(displayLevel),
                data.GetAtkAtLevel(displayLevel),
                data.GetDefAtLevel(displayLevel),
                data.GetSpeedAtLevel(displayLevel));
        }

        private IEnumerator PlayNewCharacter(CharacterData data)
        {
            if (levelText != null)
            {
                levelText.text = "NOUVEAU";
                levelText.color = new Color(0.45f, 1f, 0.55f);
            }

            if (xpFill != null)
            {
                PlayOptional(xpProgressClip, 0.7f);
                yield return FillXp(0f, 1f, XP_FILL_DURATION * 0.7f);
            }

            if (data != null)
            {
                SetStatTexts(
                    data.GetHpAtLevel(1),
                    data.GetAtkAtLevel(1),
                    data.GetDefAtLevel(1),
                    data.GetSpeedAtLevel(1));
            }

            yield return null;
        }

        private IEnumerator PlayAlreadyMax()
        {
            if (levelText != null)
            {
                levelText.text = "Nv." + CharacterData.MAX_LEVEL;
                levelText.color = new Color(1f, 0.85f, 0.35f);
            }

            if (maxBadge != null)
                maxBadge.SetActive(true);

            if (xpFill != null)
            {
                xpFill.color = new Color(1f, 0.78f, 0.2f, 1f);
                xpFill.fillAmount = 1f;
            }

            PlayOptional(maxConfirmClip, 0.85f);
            yield return new WaitForSecondsRealtime(0.2f);
        }

        private IEnumerator PlayLevelUp(CharacterData data, PulledCharacter pulled)
        {
            int from = Mathf.Max(1, pulled.previousLevel);
            int to = Mathf.Clamp(pulled.newLevel, from + 1, CharacterData.MAX_LEVEL);

            if (levelText != null)
            {
                levelText.color = Color.yellow;
                levelText.text = "Nv." + from;
            }

            // Remplissage XP + son de progression.
            PlayOptional(xpProgressClip, 0.85f);
            yield return FillXp(0f, 1f, XP_FILL_DURATION);

            // Pop niveau.
            PlayOptional(levelUpClip, 1f);
            if (levelText != null)
                yield return PopLevelText(from, to);

            if (to >= CharacterData.MAX_LEVEL && maxBadge != null)
                maxBadge.SetActive(true);

            // Stats qui montent.
            if (data != null)
            {
                yield return AnimateStats(
                    data.GetHpAtLevel(from), data.GetHpAtLevel(to),
                    data.GetAtkAtLevel(from), data.GetAtkAtLevel(to),
                    data.GetDefAtLevel(from), data.GetDefAtLevel(to),
                    data.GetSpeedAtLevel(from), data.GetSpeedAtLevel(to));
            }

            if (xpFill != null)
                xpFill.fillAmount = to >= CharacterData.MAX_LEVEL ? 1f : 0.08f;
        }

        private IEnumerator FillXp(float from, float to, float duration)
        {
            if (xpFill == null)
                yield break;

            float elapsed = 0f;
            duration = Mathf.Max(0.05f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / duration);
                float eased = u * u * (3f - 2f * u);
                xpFill.fillAmount = Mathf.Lerp(from, to, eased);
                yield return null;
            }

            xpFill.fillAmount = to;
        }

        private IEnumerator PopLevelText(int from, int to)
        {
            if (levelText == null)
                yield break;

            float elapsed = 0f;
            Vector3 baseScale = Vector3.one;
            while (elapsed < LEVEL_POP_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / LEVEL_POP_DURATION);
                float punch = 1f + Mathf.Sin(u * Mathf.PI) * 0.22f;
                levelText.rectTransform.localScale = baseScale * punch;
                if (u > 0.35f)
                    levelText.text = "Nv." + to;
                yield return null;
            }

            levelText.text = "Nv." + to;
            levelText.rectTransform.localScale = baseScale;
        }

        private IEnumerator AnimateStats(
            int hp0, int hp1, int atk0, int atk1, int def0, int def1, int spd0, int spd1)
        {
            float elapsed = 0f;
            float lastTick = -1f;
            while (elapsed < STAT_TICK_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / STAT_TICK_DURATION);
                float eased = 1f - (1f - u) * (1f - u);

                int hp = Mathf.RoundToInt(Mathf.Lerp(hp0, hp1, eased));
                int atk = Mathf.RoundToInt(Mathf.Lerp(atk0, atk1, eased));
                int def = Mathf.RoundToInt(Mathf.Lerp(def0, def1, eased));
                int spd = Mathf.RoundToInt(Mathf.Lerp(spd0, spd1, eased));
                SetStatTexts(hp, atk, def, spd);

                // Tick sonore discret à chaque cran de progression.
                float tickSlot = Mathf.Floor(u * 6f);
                if (tickSlot > lastTick)
                {
                    lastTick = tickSlot;
                    PlayOptional(statTickClip, 0.45f);
                }

                yield return null;
            }

            SetStatTexts(hp1, atk1, def1, spd1);
        }

        private void SetStatTexts(int hp, int atk, int def, int spd)
        {
            if (hpText != null) hpText.text = hp.ToString();
            if (atkText != null) atkText.text = atk.ToString();
            if (defText != null) defText.text = def.ToString();
            if (spdText != null) spdText.text = spd.ToString();
        }

        private static void PlayOptional(AudioClip clip, float volume)
        {
            if (clip == null)
                return;
            GachaAnimationController.PlayGachaSfx(clip, volume);
        }

        // ═══════════════════════════════════════════
        // HELPERS UI
        // ═══════════════════════════════════════════

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

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent,
            string name,
            float size,
            FontStyles style,
            TextAlignmentOptions align)
        {
            Transform t = parent.Find(name);
            GameObject go = t != null ? t.gameObject : CreateChild(parent, name);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.text = string.Empty;
            return tmp;
        }

        private static void PlaceText(TextMeshProUGUI tmp, float anchorMinY, float anchorMaxY)
        {
            if (tmp == null)
                return;
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = new Vector2(0.06f, anchorMinY);
            rt.anchorMax = new Vector2(0.94f, anchorMaxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchBottom(RectTransform rt, float minY, float maxY)
        {
            rt.anchorMin = new Vector2(0f, minY);
            rt.anchorMax = new Vector2(1f, maxY);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
        }
    }
}
