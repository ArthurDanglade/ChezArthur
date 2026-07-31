using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Page vedette showcase (étage 1) — crop buste, chips spé, parallaxe, stats 5.c.
    /// </summary>
    public class ShowcaseFeaturedPage : MonoBehaviour, IPointerClickHandler
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Crop buste (anti-spoil)")]
        [SerializeField] private RectTransform cropWindow;
        [SerializeField] private RectTransform artworkRoot;
        [SerializeField] private CharacterArtworkView artworkView;
        [Tooltip("Ancrage normalise du crop dans l'artwork (0-1). Haut = visage.")]
        [SerializeField] private Vector2 bustAnchor = new Vector2(0.5f, 0.92f);
        [SerializeField] private Vector2 bustOffsetPx = Vector2.zero;
        [SerializeField] private float artworkScale = 1.55f;

        [Header("Parallaxe")]
        [SerializeField] private float parallaxFactor = 0.15f;

        [Header("Stats (meme source que popup 5.c)")]
        [SerializeField] private TextMeshProUGUI hpValue;
        [SerializeField] private TextMeshProUGUI hpLabel;
        [SerializeField] private TextMeshProUGUI atkValue;
        [SerializeField] private TextMeshProUGUI atkLabel;
        [SerializeField] private TextMeshProUGUI defValue;
        [SerializeField] private TextMeshProUGUI defLabel;
        [SerializeField] private TextMeshProUGUI speedValue;
        [SerializeField] private TextMeshProUGUI speedLabel;

        [Header("Infos")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI rarityBadge;
        [SerializeField] private Transform specChipsRow;
        [SerializeField] private Sprite specChipSprite;
        [SerializeField] private TextMeshProUGUI passiveNameText;
        [SerializeField] private TextMeshProUGUI passiveDescText;
        [SerializeField] private TextMeshProUGUI tapHintText;
        [SerializeField] private Image cardFrame;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterData _data;
        private OwnedCharacter _owned;
        private bool _isOwned;
        private Action<CharacterData, OwnedCharacter> _onOpenOwned;
        private RectTransform _scrollContent;
        private float _pageOriginX;
        private Vector2 _baseArtworkPos;
        private Coroutine _pulseCo;
        private int _selectedSpecIndex = -1;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void LateUpdate()
        {
            ApplyParallax();
        }

        private void OnDisable()
        {
            if (_pulseCo != null)
            {
                StopCoroutine(_pulseCo);
                _pulseCo = null;
            }

            if (artworkView != null)
                artworkView.Release();
        }

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        public void Bind(
            CharacterData data,
            RectTransform scrollContent,
            float pageOriginX,
            Action<CharacterData, OwnedCharacter> onOpenOwned)
        {
            _data = data;
            _scrollContent = scrollContent;
            _pageOriginX = pageOriginX;
            _onOpenOwned = onOpenOwned;

            _owned = null;
            _isOwned = false;
            if (data != null
                && PersistentManager.Instance != null
                && PersistentManager.Instance.Characters != null)
            {
                _owned = PersistentManager.Instance.Characters.GetOwnedCharacter(data.Id);
                _isOwned = _owned != null;
            }

            // Base (-1) par defaut ; owned → spé equipee.
            _selectedSpecIndex = _isOwned && _owned != null
                ? _owned.GetSpecialization()
                : -1;

            RefreshVisuals();
            LayoutBustCrop();
        }

        public void SetChipSprite(Sprite sprite)
        {
            specChipSprite = sprite;
        }

        /// <summary>
        /// Recadre le crop apres un resize device / fitter.
        /// </summary>
        public void NotifyLayoutChanged()
        {
            LayoutBustCrop();
            if (artworkView != null)
                artworkView.ForceCoverMode();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_data == null)
                return;

            // Clic sur une chip spé : ne pas ouvrir le detail.
            if (eventData != null
                && eventData.pointerPress != null
                && specChipsRow != null
                && eventData.pointerPress.transform.IsChildOf(specChipsRow))
                return;

            if (_isOwned && _owned != null)
            {
                _onOpenOwned?.Invoke(_data, _owned);
                return;
            }

            if (_pulseCo != null)
                StopCoroutine(_pulseCo);
            _pulseCo = StartCoroutine(PulseScale());
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void RefreshVisuals()
        {
            if (_data == null)
                return;

            // Toujours DECHU (anti-spoil prime) — lecture ShowState, pas Resolve.
            if (artworkView != null)
            {
                artworkView.ShowState(_data, _data.AnimatedPortraitDechu);
                artworkView.ForceCoverMode();
            }

            bustAnchor = _data.portraitFocalPoint;

            if (nameText != null)
                nameText.text = _data.CharacterName;

            if (rarityBadge != null)
            {
                rarityBadge.text = _data.Rarity.ToString();
                rarityBadge.color = CharacterRarityPalette.GetColor(_data.Rarity);
            }

            if (tapHintText != null)
            {
                tapHintText.gameObject.SetActive(true);
                tapHintText.text = _isOwned
                    ? "Appuyer pour details"
                    : "Invoquer pour reveler";
                tapHintText.color = _isOwned ? UiTheme.Gold : UiTheme.TextMuted;
            }

            if (cardFrame != null)
            {
                Color frame = UiTheme.Surface;
                frame.a = 0.96f;
                cardFrame.color = frame;
            }

            RebuildChips();
            RefreshSpecContent();
        }

        private void RebuildChips()
        {
            ShowcaseSpecChips.Rebuild(
                specChipsRow,
                _data,
                specChipSprite,
                _selectedSpecIndex,
                interactive: true,
                OnSpecChipClicked);
        }

        private void OnSpecChipClicked(int specIndex)
        {
            if (_selectedSpecIndex == specIndex)
                return;

            _selectedSpecIndex = specIndex;
            RebuildChips();
            RefreshSpecContent();
        }

        private void RefreshSpecContent()
        {
            if (_data == null)
                return;

            int level = _isOwned && _owned != null ? _owned.level : 1;
            SpecializationData spec = _data.GetSpecialization(_selectedSpecIndex);

            BindStat(hpValue, hpLabel, "PV", UiTheme.StatHp,
                spec != null ? spec.GetHpAtLevel(level).ToString() : "-");
            BindStat(atkValue, atkLabel, "ATK", UiTheme.StatAtk,
                spec != null ? spec.GetAtkAtLevel(level).ToString() : "-");
            BindStat(defValue, defLabel, "DEF", UiTheme.StatDef,
                spec != null ? spec.GetDefAtLevel(level).ToString() : "-");
            BindStat(speedValue, speedLabel, "VIT", UiTheme.StatSpeed,
                spec != null ? spec.GetSpeedAtLevel(level).ToString() : "-");

            PassiveData passive = FindFirstPassive(spec, level);
            if (passiveNameText != null)
                passiveNameText.text = passive != null ? passive.PassiveName : string.Empty;
            if (passiveDescText != null)
                passiveDescText.text = passive != null ? passive.Description : string.Empty;
        }

        private static void BindStat(
            TextMeshProUGUI value,
            TextMeshProUGUI label,
            string labelStr,
            Color color,
            string valueStr)
        {
            if (value != null)
            {
                value.text = valueStr;
                value.color = color;
            }

            if (label != null)
            {
                label.text = labelStr;
                label.color = color;
            }
        }

        private static PassiveData FindFirstPassive(SpecializationData spec, int level)
        {
            if (spec == null)
                return null;
            var slots = spec.GetPassiveSlots();
            if (slots == null)
                return null;
            for (int i = 0; i < slots.Count; i++)
            {
                PassiveSlot slot = slots[i];
                if (slot != null && slot.UnlockLevel <= level && slot.PassiveData != null)
                    return slot.PassiveData;
            }

            return null;
        }

        private void LayoutBustCrop()
        {
            if (artworkRoot == null || cropWindow == null)
                return;

            float w = cropWindow.rect.width;
            float h = cropWindow.rect.height;
            if (w < 1f)
                w = 400f;
            if (h < 1f)
                h = 400f;

            Vector2 focal = bustAnchor;
            if (focal.x < 0.05f && focal.y < 0.05f)
                focal = new Vector2(0.5f, 0.65f);
            focal.x = Mathf.Clamp01(focal.x);
            focal.y = Mathf.Clamp01(focal.y);

            float scale = Mathf.Max(1.65f, artworkScale);
            artworkRoot.anchorMin = new Vector2(0.5f, 0.5f);
            artworkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            artworkRoot.pivot = focal;
            artworkRoot.sizeDelta = new Vector2(w * scale, h * scale);
            artworkRoot.anchoredPosition = bustOffsetPx;
            _baseArtworkPos = artworkRoot.anchoredPosition;
        }

        private void ApplyParallax()
        {
            if (artworkRoot == null || _scrollContent == null)
                return;

            float delta = _scrollContent.anchoredPosition.x - _pageOriginX;
            artworkRoot.anchoredPosition = _baseArtworkPos
                + new Vector2(delta * parallaxFactor, 0f);
        }

        private IEnumerator PulseScale()
        {
            Transform t = transform;
            Vector3 baseScale = Vector3.one;
            t.localScale = baseScale;
            float dur = 0.18f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(elapsed / dur);
                float s = 1f + 0.045f * Mathf.Sin(u * Mathf.PI);
                t.localScale = baseScale * s;
                yield return null;
            }

            t.localScale = baseScale;
            _pulseCo = null;
        }
    }
}
