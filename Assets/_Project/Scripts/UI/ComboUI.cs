using UnityEngine;
using TMPro;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche le compteur de combo ricochet (xN) avec pop, taille et fondu.
    /// </summary>
    public class ComboUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
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

        [Header("Maintien / fondu")]
        [SerializeField] private float _holdTime = 1.1f;
        [SerializeField] private float _fadeSpeed = 3f;

        private float _popScale = 1f;
        private float _alpha;
        private float _holdTimer;
        private float _sizeT;

        private void Awake()
        {
            if (_text == null) _text = GetComponent<TMP_Text>();
            _alpha = 0f;
            SetAlpha(0f);
        }

        public void OnCombo(int count)
        {
            if (_text == null || count < _minComboToShow) return;

            _text.text = "x" + count;
            _sizeT = Mathf.Clamp01((count - _minComboToShow) / Mathf.Max(1f, _countForMaxScale - _minComboToShow));
            _text.color = new Color(
                Mathf.Lerp(_lowColor.r, _highColor.r, _sizeT),
                Mathf.Lerp(_lowColor.g, _highColor.g, _sizeT),
                Mathf.Lerp(_lowColor.b, _highColor.b, _sizeT), 1f);
            _popScale = _popPunch;
            _alpha = 1f;
            _holdTimer = _holdTime;
        }

        private void Update()
        {
            if (_alpha <= 0f && _popScale <= 1.001f) return;

            _popScale = Mathf.Lerp(_popScale, 1f, _popLerpSpeed * Time.deltaTime);
            if (_holdTimer > 0f) _holdTimer -= Time.deltaTime;
            else _alpha = Mathf.MoveTowards(_alpha, 0f, _fadeSpeed * Time.deltaTime);

            transform.localScale = Vector3.one * (Mathf.Lerp(_baseScale, _maxScale, _sizeT) * _popScale);
            SetAlpha(_alpha);
        }

        private void SetAlpha(float a)
        {
            if (_text == null) return;
            Color c = _text.color;
            c.a = a;
            _text.color = c;
        }
    }
}
