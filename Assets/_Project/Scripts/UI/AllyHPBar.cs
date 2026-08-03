using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Feedback;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de vie allié + vraie barre de shield bleue au-dessus (juicy / lisible).
    /// </summary>
    public class AllyHPBar : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly Color ShieldFillColor = new Color(0.2f, 0.75f, 1f, 1f);
        private static readonly Color ShieldTrackColor = new Color(0.05f, 0.2f, 0.35f, 0.85f);
        private const float ShieldGap = 4f;
        private const float ShieldHeight = 14f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image shieldFillImage;
        [SerializeField] private TextMeshProUGUI shieldText;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterBall _character;
        private Action<int> _damagedHandler;
        private Action<int> _healedHandler;
        private Action _statsChangedHandler;
        private Action _buffsChangedHandler;
        private Image _shieldTrack;
        private bool _shieldVisualBuilt;
        private StatusPipsRail _pipsRail;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Associe la barre à un personnage, s'abonne aux events et met à jour l'affichage.
        /// </summary>
        public void Initialize(CharacterBall character)
        {
            Unsubscribe();
            if (_pipsRail != null)
                _pipsRail.Unbind();

            _character = character;
            EnsureShieldVisual();

            if (character == null)
            {
                if (nameText != null) nameText.text = "";
                if (fillImage != null) fillImage.fillAmount = 0f;
                if (hpText != null) hpText.text = "";
                if (shieldText != null) shieldText.text = "";
                SetShieldVisible(false);
                return;
            }

            if (nameText != null)
                nameText.text = character.Name;

            _damagedHandler = _ => UpdateDisplay();
            character.OnDamaged += _damagedHandler;

            _healedHandler = _ => UpdateDisplay();
            character.OnHealed += _healedHandler;

            _statsChangedHandler = UpdateDisplay;
            character.OnStatsChanged += _statsChangedHandler;

            if (character.BuffReceiver != null)
            {
                _buffsChangedHandler = UpdateDisplay;
                character.BuffReceiver.OnBuffsChanged += _buffsChangedHandler;
            }

            UnitStatusFx statusFx = character.GetComponent<UnitStatusFx>();
            EnsurePipsRail().Bind(statusFx);

            UpdateDisplay();
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            Unsubscribe();
            if (_pipsRail != null)
                _pipsRail.Unbind();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private StatusPipsRail EnsurePipsRail()
        {
            if (_pipsRail != null)
                return _pipsRail;

            GameObject railGo = new GameObject("StatusPipsRail");
            railGo.transform.SetParent(transform, false);
            _pipsRail = railGo.AddComponent<StatusPipsRail>();
            return _pipsRail;
        }

        private void Unsubscribe()
        {
            if (_character != null)
            {
                if (_damagedHandler != null)
                    _character.OnDamaged -= _damagedHandler;
                if (_healedHandler != null)
                    _character.OnHealed -= _healedHandler;
                if (_statsChangedHandler != null)
                    _character.OnStatsChanged -= _statsChangedHandler;

                if (_character.BuffReceiver != null && _buffsChangedHandler != null)
                    _character.BuffReceiver.OnBuffsChanged -= _buffsChangedHandler;
            }

            _damagedHandler = null;
            _healedHandler = null;
            _statsChangedHandler = null;
            _buffsChangedHandler = null;
        }

        private void UpdateDisplay()
        {
            if (_character == null) return;

            int current = _character.CurrentHp;
            int max = Mathf.Max(1, _character.MaxHp);
            float ratio = (float)current / max;

            if (fillImage != null)
            {
                fillImage.fillAmount = ratio;
                fillImage.color = GetColorForRatio(ratio);
            }

            // HP seul sur la barre verte — le shield a son propre label.
            if (hpText != null)
                hpText.text = $"{current}/{max}";

            float shield = _character.BuffReceiver != null
                ? _character.BuffReceiver.GetShieldAmount()
                : 0f;

            UpdateShieldFill(shield, max);
        }

        private void EnsureShieldVisual()
        {
            if (_shieldVisualBuilt) return;
            if (fillImage == null) return;

            RectTransform hpRt = fillImage.rectTransform;
            Transform parent = hpRt.parent;

            if (_shieldTrack == null)
            {
                GameObject trackGo = new GameObject("ShieldBar");
                trackGo.transform.SetParent(parent, false);
                // Au-dessus de la barre HP dans la hiérarchie visuelle.
                trackGo.transform.SetSiblingIndex(hpRt.GetSiblingIndex());

                _shieldTrack = trackGo.AddComponent<Image>();
                _shieldTrack.color = ShieldTrackColor;
                _shieldTrack.raycastTarget = false;

                RectTransform trackRt = _shieldTrack.rectTransform;
                trackRt.anchorMin = hpRt.anchorMin;
                trackRt.anchorMax = hpRt.anchorMax;
                trackRt.pivot = hpRt.pivot;
                trackRt.sizeDelta = new Vector2(hpRt.sizeDelta.x, ShieldHeight);
                // Deuxième barre clairement au-dessus de la HP.
                trackRt.anchoredPosition = hpRt.anchoredPosition +
                    new Vector2(0f, hpRt.rect.height * 0.5f + ShieldHeight * 0.5f + ShieldGap);
            }

            if (shieldFillImage == null)
            {
                GameObject fillGo = new GameObject("ShieldFill");
                fillGo.transform.SetParent(_shieldTrack.transform, false);
                shieldFillImage = fillGo.AddComponent<Image>();
                shieldFillImage.sprite = fillImage.sprite;
                shieldFillImage.type = Image.Type.Filled;
                shieldFillImage.fillMethod = Image.FillMethod.Horizontal;
                shieldFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                shieldFillImage.fillAmount = 0f;
                shieldFillImage.color = ShieldFillColor;
                shieldFillImage.raycastTarget = false;

                RectTransform sRt = shieldFillImage.rectTransform;
                sRt.anchorMin = Vector2.zero;
                sRt.anchorMax = Vector2.one;
                sRt.offsetMin = new Vector2(2f, 2f);
                sRt.offsetMax = new Vector2(-2f, -2f);
            }

            if (shieldText == null)
            {
                GameObject textGo = new GameObject("ShieldText");
                textGo.transform.SetParent(_shieldTrack.transform, false);
                shieldText = textGo.AddComponent<TextMeshProUGUI>();
                shieldText.fontSize = 14f;
                shieldText.fontStyle = FontStyles.Bold;
                shieldText.alignment = TextAlignmentOptions.Right;
                shieldText.color = Color.white;
                shieldText.raycastTarget = false;
                shieldText.enableWordWrapping = false;

                RectTransform tRt = shieldText.rectTransform;
                tRt.anchorMin = Vector2.zero;
                tRt.anchorMax = Vector2.one;
                tRt.offsetMin = new Vector2(4f, 0f);
                tRt.offsetMax = new Vector2(-4f, 0f);
            }

            SetShieldVisible(false);
            _shieldVisualBuilt = true;
        }

        private void UpdateShieldFill(float shield, int maxHp)
        {
            EnsureShieldVisual();
            if (shieldFillImage == null || _shieldTrack == null) return;

            if (shield <= 0.5f)
            {
                SetShieldVisible(false);
                if (shieldText != null)
                    shieldText.text = "";
                return;
            }

            SetShieldVisible(true);
            float ratio = Mathf.Clamp01(shield / Mathf.Max(1f, maxHp));
            // Au moins un peu de fill pour le juice même sur petit shield.
            shieldFillImage.fillAmount = Mathf.Max(0.12f, ratio);
            shieldFillImage.color = ShieldFillColor;

            if (shieldText != null)
                shieldText.text = $"+{Mathf.RoundToInt(shield)}";
        }

        private void SetShieldVisible(bool visible)
        {
            if (_shieldTrack != null)
                _shieldTrack.gameObject.SetActive(visible);
            else if (shieldFillImage != null)
                shieldFillImage.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Retourne une couleur selon le ratio PV : vert > jaune > rouge.
        /// </summary>
        private static Color GetColorForRatio(float ratio)
        {
            if (ratio <= 0.25f)
                return Color.red;
            if (ratio <= 0.5f)
                return Color.Lerp(Color.red, Color.yellow, (ratio - 0.25f) * 4f);
            return Color.Lerp(Color.yellow, Color.green, (ratio - 0.5f) * 2f);
        }
    }
}
