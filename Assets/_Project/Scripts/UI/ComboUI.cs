using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche le compteur de combo ricochet (xN) avec pop, taille, jauge et fondu piloté par JuiceDirector.
    /// </summary>
    public class ComboUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _timerGauge; // Image en Filled / Radial 360
        [SerializeField] private int _minComboToShow = 2;

        [Header("Pop")]
        [SerializeField] private float _popPunch = 1.45f;
        [SerializeField] private float _popLerpSpeed = 12f;

        [Header("Taille selon le combo")]
        [SerializeField] private float _baseScale = 1f;
        [SerializeField] private float _maxScale = 1.8f;
        [SerializeField] private int _countForMaxScale = 12;

        [Header("Couleur selon le combo")]
        [SerializeField] private Color _lowColor = Color.white;
        [SerializeField] private Color _highColor = new Color(1f, 0.55f, 0.1f);

        [Header("Fondu")]
        [SerializeField] private float _fadeSpeed = 6f;

        private float _popScale = 1f;
        private float _alpha;
        private float _targetAlpha;
        private float _sizeT;

        private void Awake()
        {
            if (_text == null) _text = GetComponentInChildren<TMP_Text>();
            _alpha = 0f;
            _targetAlpha = 0f;
            ApplyVisual();
        }

        public void OnCombo(int count)
        {
            if (_text == null || count < _minComboToShow) return;

            _text.text = "x" + count;
            _sizeT = Mathf.Clamp01((count - _minComboToShow) / Mathf.Max(1f, _countForMaxScale - _minComboToShow));
            _popScale = _popPunch;
            _targetAlpha = 1f;
        }

        public void SetTimer(float fill01)
        {
            if (_timerGauge != null) _timerGauge.fillAmount = Mathf.Clamp01(fill01);
        }

        public void EndCombo() => _targetAlpha = 0f;

        private void Update()
        {
            _popScale = Mathf.Lerp(_popScale, 1f, _popLerpSpeed * Time.deltaTime);
            _alpha = Mathf.MoveTowards(_alpha, _targetAlpha, _fadeSpeed * Time.deltaTime);
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            transform.localScale = Vector3.one * (Mathf.Lerp(_baseScale, _maxScale, _sizeT) * _popScale);
            Color c = Color.Lerp(_lowColor, _highColor, _sizeT);
            c.a = _alpha;
            if (_text != null) _text.color = c;
            if (_timerGauge != null)
            {
                Color g = _timerGauge.color;
                g.a = _alpha;
                _timerGauge.color = g;
            }
        }
    }
}
