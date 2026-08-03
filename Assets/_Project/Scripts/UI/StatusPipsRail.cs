using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gameplay.Feedback;

namespace ChezArthur.UI
{
    /// <summary>
    /// Rail de pastilles losanges au-dessus d'une barre PV (construit par code).
    /// </summary>
    public class StatusPipsRail : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int MaxPips = 4;
        private const float PipSize = 10f;
        private const float PipSpacing = 12f;
        private const float OffsetY = 8f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private UnitStatusFx _source;
        private readonly Image[] _pips = new Image[MaxPips];
        private TextMeshProUGUI _overflow;
        private readonly FeedbackCause[] _buffer = new FeedbackCause[8];
        private bool _built;
        private System.Action _onPipsChanged;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public void Bind(UnitStatusFx source)
        {
            Unbind();
            _source = source;
            if (_source == null)
            {
                ClearVisuals();
                return;
            }

            EnsureBuilt();
            _onPipsChanged = Rebuild;
            _source.OnPipsChanged += _onPipsChanged;
            Rebuild();
        }

        public void Unbind()
        {
            if (_source != null && _onPipsChanged != null)
                _source.OnPipsChanged -= _onPipsChanged;

            _source = null;
            _onPipsChanged = null;
            ClearVisuals();
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            Unbind();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureBuilt()
        {
            if (_built)
                return;

            RectTransform self = transform as RectTransform;
            if (self == null)
                self = gameObject.AddComponent<RectTransform>();

            self.anchorMin = new Vector2(0.5f, 1f);
            self.anchorMax = new Vector2(0.5f, 1f);
            self.pivot = new Vector2(0.5f, 0f);
            self.anchoredPosition = new Vector2(0f, OffsetY);
            self.sizeDelta = new Vector2(MaxPips * PipSpacing + 24f, PipSize + 4f);

            for (int i = 0; i < MaxPips; i++)
            {
                GameObject pipGo = new GameObject($"Pip_{i}");
                pipGo.transform.SetParent(transform, false);
                Image img = pipGo.AddComponent<Image>();
                img.raycastTarget = false;
                img.color = Color.white;
                RectTransform rt = img.rectTransform;
                rt.sizeDelta = new Vector2(PipSize, PipSize);
                rt.localEulerAngles = new Vector3(0f, 0f, 45f);
                rt.anchoredPosition = new Vector2((i - (MaxPips - 1) * 0.5f) * PipSpacing, 0f);
                pipGo.SetActive(false);
                _pips[i] = img;
            }

            GameObject overflowGo = new GameObject("Overflow");
            overflowGo.transform.SetParent(transform, false);
            _overflow = overflowGo.AddComponent<TextMeshProUGUI>();
            _overflow.fontSize = 10f;
            _overflow.alignment = TextAlignmentOptions.Left;
            _overflow.raycastTarget = false;
            _overflow.color = Color.white;
            RectTransform ort = _overflow.rectTransform;
            ort.sizeDelta = new Vector2(28f, 14f);
            ort.anchoredPosition = new Vector2(MaxPips * 0.5f * PipSpacing + 8f, 0f);
            overflowGo.SetActive(false);

            _built = true;
        }

        private void Rebuild()
        {
            EnsureBuilt();
            if (_source == null)
            {
                ClearVisuals();
                return;
            }

            int count = _source.GetActivePips(_buffer);
            int show = count < MaxPips ? count : MaxPips;
            float startX = -(show - 1) * 0.5f * PipSpacing;

            for (int i = 0; i < MaxPips; i++)
            {
                if (i < show)
                {
                    _pips[i].gameObject.SetActive(true);
                    _pips[i].color = CombatFeedbackPalette.GetColor(_buffer[i]);
                    _pips[i].rectTransform.anchoredPosition = new Vector2(startX + i * PipSpacing, 0f);
                }
                else
                {
                    _pips[i].gameObject.SetActive(false);
                }
            }

            int overflow = count - MaxPips;
            if (overflow > 0 && _overflow != null)
            {
                _overflow.gameObject.SetActive(true);
                _overflow.text = $"+{overflow}";
                _overflow.rectTransform.anchoredPosition =
                    new Vector2(startX + show * PipSpacing + 4f, 0f);
            }
            else if (_overflow != null)
            {
                _overflow.gameObject.SetActive(false);
            }
        }

        private void ClearVisuals()
        {
            for (int i = 0; i < MaxPips; i++)
            {
                if (_pips[i] != null)
                    _pips[i].gameObject.SetActive(false);
            }

            if (_overflow != null)
                _overflow.gameObject.SetActive(false);
        }
    }
}
