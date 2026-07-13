using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;

namespace ChezArthur.UI
{
    /// <summary>
    /// Fiche d'inspection ennemi (press-and-hold) : en-tête, description, stats, passifs.
    /// Ne bloque jamais les raycasts — le doigt reste maître du drag.
    /// </summary>
    public class EnemyCardUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Container")]
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("En-tête")]
        [SerializeField] private Image spriteFrame;
        [SerializeField] private Image spriteImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image typeBadgeBg;
        [SerializeField] private TextMeshProUGUI typeText;

        [Header("Description")]
        [SerializeField] private GameObject descriptionBlock;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private TextMeshProUGUI atkText;
        [SerializeField] private TextMeshProUGUI defText;
        [SerializeField] private TextMeshProUGUI spdText;

        [Header("Passifs")]
        [SerializeField] private GameObject passiveBlock;
        [SerializeField] private TextMeshProUGUI passiveTitleText;
        [SerializeField] private TextMeshProUGUI passivesText;

        [Header("Animation")]
        [SerializeField] private float revealDelay = 0.15f;
        [SerializeField] private float fadeInDuration = 0.1f;
        [SerializeField] private float fadeOutDuration = 0.08f;

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static EnemyCardUI Instance { get; private set; }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _enemy;
        private Coroutine _revealRoutine;
        private bool _isShowing;
        private bool _hasRevealed;

        private int _cachedHpCur = int.MinValue;
        private int _cachedHpMax = int.MinValue;
        private int _cachedAtk = int.MinValue;
        private int _cachedDef = int.MinValue;
        private int _cachedSpd = int.MinValue;

        private readonly StringBuilder _passiveBuilder = new StringBuilder(256);
        private bool _cardRootIsHost;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Instance.gameObject.activeInHierarchy)
                {
                    Destroy(gameObject);
                    return;
                }

                Instance = null;
            }

            Instance = this;
            _isShowing = false;
            _hasRevealed = false;

            EnsureHostActive();
            ResolveCardRootHost();
            EnsureLayoutStructures();
            ApplyVisualStyle();
            SetVisuallyHidden();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            if (!_isShowing || _enemy == null)
                return;

            if (_enemy.IsDead || !_enemy.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            RefreshStats();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche la card avec les données de l'ennemi.
        /// </summary>
        public void Show(Enemy enemy)
        {
            if (enemy == null || enemy.Data == null)
                return;

            StopRevealRoutine();

            _enemy = enemy;
            EnemyData data = enemy.Data;

            ApplySprite(data);
            ApplyName(data);
            ApplyTypeStyle(data.EnemyType);
            ApplyDescription(data);
            ApplyPassives(data);
            ResetStatCache();
            RefreshStats();
            RebuildLayout();

            EnsureHostActive();
            ActivateCardRoot();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            _isShowing = true;
            _hasRevealed = false;
            _revealRoutine = StartCoroutine(RevealRoutine());
        }

        /// <summary>
        /// Masque la card.
        /// </summary>
        public void Hide()
        {
            if (!_isShowing)
                return;

            StopRevealRoutine();

            if (!_hasRevealed)
            {
                FinishHideImmediate();
                return;
            }

            _revealRoutine = StartCoroutine(FadeOutRoutine());
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator RevealRoutine()
        {
            if (revealDelay > 0f)
                yield return new WaitForSecondsRealtime(revealDelay);

            if (!_isShowing)
                yield break;

            float duration = Mathf.Max(0.01f, fadeInDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!_isShowing)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (canvasGroup != null)
                    canvasGroup.alpha = t;

                yield return null;
            }

            if (!_isShowing)
                yield break;

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            _hasRevealed = true;
            _revealRoutine = null;
        }

        private IEnumerator FadeOutRoutine()
        {
            float duration = Mathf.Max(0.01f, fadeOutDuration);
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                yield return null;
            }

            FinishHideImmediate();
            _revealRoutine = null;
        }

        private void FinishHideImmediate()
        {
            _isShowing = false;
            _hasRevealed = false;
            _enemy = null;

            SetVisuallyHidden();
            ResetStatCache();
        }

        private void EnsureHostActive()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        private void ResolveCardRootHost()
        {
            if (cardRoot == null)
                cardRoot = gameObject;

            _cardRootIsHost = cardRoot == gameObject;
        }

        private void SetVisuallyHidden()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (!_cardRootIsHost && cardRoot != null)
                cardRoot.SetActive(false);
        }

        private void ActivateCardRoot()
        {
            if (!_cardRootIsHost && cardRoot != null)
                cardRoot.SetActive(true);
        }

        private void StopRevealRoutine()
        {
            if (_revealRoutine == null)
                return;

            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        private void ApplySprite(EnemyData data)
        {
            if (spriteImage == null)
                return;

            Sprite sprite = data != null ? data.CombatSprite : null;
            if (sprite != null)
            {
                spriteImage.sprite = sprite;
                spriteImage.preserveAspect = true;
                spriteImage.enabled = true;
            }
            else
            {
                spriteImage.sprite = null;
                spriteImage.enabled = false;
            }
        }

        private void ApplyName(EnemyData data)
        {
            if (nameText != null)
                nameText.text = data != null ? data.EnemyName : string.Empty;
        }

        private void ApplyTypeStyle(EnemyType enemyType)
        {
            ResolveTypePresentation(enemyType, out string label, out Color color);

            if (typeText != null)
            {
                typeText.text = label;
                typeText.color = color;
            }

            if (typeBadgeBg != null)
            {
                Color badge = color;
                badge.a = 0.22f;
                typeBadgeBg.color = badge;
            }

            if (spriteFrame != null)
                spriteFrame.color = color;

            if (passiveTitleText != null)
                passiveTitleText.color = color;
        }

        private static void ResolveTypePresentation(EnemyType enemyType, out string label, out Color color)
        {
            switch (enemyType)
            {
                case EnemyType.Boss:
                    label = "BOSS";
                    color = UiTheme.EnemyTypeBossColor;
                    break;
                case EnemyType.MiniBoss:
                    label = "MINI-BOSS";
                    color = UiTheme.EnemyTypeMiniBossColor;
                    break;
                default:
                    label = "NORMAL";
                    color = UiTheme.EnemyTypeNormalColor;
                    break;
            }
        }

        private void ApplyDescription(EnemyData data)
        {
            string desc = data != null ? data.Description : null;
            if (string.IsNullOrWhiteSpace(desc) && data != null)
                desc = data.PassiveDescription;

            bool hasDescription = !string.IsNullOrWhiteSpace(desc);

            if (descriptionBlock != null)
                descriptionBlock.SetActive(hasDescription);

            if (!hasDescription)
                return;

            if (descriptionText != null)
                descriptionText.text = desc;
        }

        private void ApplyPassives(EnemyData data)
        {
            _passiveBuilder.Clear();

            if (data?.Passives != null)
            {
                for (int i = 0; i < data.Passives.Count; i++)
                {
                    EnemyPassiveData passive = data.Passives[i];
                    if (passive == null)
                        continue;

                    if (!string.IsNullOrEmpty(passive.PassiveName))
                    {
                        _passiveBuilder.Append("• ");
                        _passiveBuilder.Append(passive.PassiveName);
                    }

                    if (!string.IsNullOrEmpty(passive.Description))
                    {
                        if (_passiveBuilder.Length > 0 && _passiveBuilder[_passiveBuilder.Length - 1] != '\n')
                            _passiveBuilder.Append(": ");

                        _passiveBuilder.AppendLine(passive.Description);
                    }
                    else if (_passiveBuilder.Length > 0 && _passiveBuilder[_passiveBuilder.Length - 1] != '\n')
                    {
                        _passiveBuilder.AppendLine();
                    }

                    if (i < data.Passives.Count - 1)
                        _passiveBuilder.AppendLine();
                }
            }

            bool hasPassives = _passiveBuilder.Length > 0;

            if (passiveBlock != null)
                passiveBlock.SetActive(hasPassives);

            if (!hasPassives)
                return;

            if (passivesText != null)
            {
                int len = _passiveBuilder.Length;
                while (len > 0 && (_passiveBuilder[len - 1] == '\n' || _passiveBuilder[len - 1] == '\r'))
                    len--;

                passivesText.text = len > 0 ? _passiveBuilder.ToString(0, len) : string.Empty;
            }
        }

        private void ResetStatCache()
        {
            _cachedHpCur = int.MinValue;
            _cachedHpMax = int.MinValue;
            _cachedAtk = int.MinValue;
            _cachedDef = int.MinValue;
            _cachedSpd = int.MinValue;
        }

        private void RefreshStats()
        {
            if (_enemy == null)
                return;

            int hpCur = _enemy.CurrentHp;
            int hpMax = _enemy.MaxHp;
            int atk = _enemy.EffectiveAtk;
            int def = _enemy.EffectiveDef;
            int spd = _enemy.Speed;

            if (hpText != null && (hpCur != _cachedHpCur || hpMax != _cachedHpMax))
            {
                hpText.text = $"{hpCur} / {hpMax}";
                _cachedHpCur = hpCur;
                _cachedHpMax = hpMax;
            }

            if (atkText != null && atk != _cachedAtk)
            {
                atkText.text = atk.ToString();
                _cachedAtk = atk;
            }

            if (defText != null && def != _cachedDef)
            {
                defText.text = def.ToString();
                _cachedDef = def;
            }

            if (spdText != null && spd != _cachedSpd)
            {
                spdText.text = spd.ToString();
                _cachedSpd = spd;
            }
        }

        private void EnsureLayoutStructures()
        {
            EnsureVerticalSection(descriptionBlock, EnemyCardStyle.DescriptionSectionSpacing);
            EnsureVerticalSection(passiveBlock, EnemyCardStyle.PassiveSectionSpacing);
            ConfigureWrappingText(descriptionText);
            ConfigureWrappingText(passivesText);
            ConfigureWrappingText(passiveTitleText);
        }

        private void ApplyVisualStyle()
        {
            GameObject panel = _cardRootIsHost ? gameObject : cardRoot;
            EnemyCardStyle.Apply(
                panel,
                nameText,
                typeText,
                descriptionText,
                hpText,
                atkText,
                defText,
                spdText,
                passiveTitleText,
                passivesText,
                descriptionBlock,
                passiveBlock,
                spriteFrame);
        }

        private void RebuildLayout()
        {
            if (cardRoot == null)
                return;

            RectTransform root = cardRoot.transform as RectTransform;
            if (root != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private static void EnsureVerticalSection(GameObject section, float spacing)
        {
            if (section == null)
                return;

            VerticalLayoutGroup vlg = section.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = section.AddComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            vlg.spacing = spacing;

            ContentSizeFitter csf = section.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = section.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            LayoutElement le = section.GetComponent<LayoutElement>();
            if (le == null)
                le = section.AddComponent<LayoutElement>();

            le.flexibleWidth = 1f;
        }

        private static void ConfigureWrappingText(TextMeshProUGUI tmp)
        {
            if (tmp == null)
                return;

            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;

            LayoutElement le = tmp.GetComponent<LayoutElement>();
            if (le == null)
                le = tmp.gameObject.AddComponent<LayoutElement>();

            le.flexibleWidth = 1f;

            ContentSizeFitter csf = tmp.GetComponent<ContentSizeFitter>();
            if (csf == null)
                csf = tmp.gameObject.AddComponent<ContentSizeFitter>();

            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
