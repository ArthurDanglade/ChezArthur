using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Beam de lien permanent (Confesseur, futurs liens) — LineRenderer 2 points, pulse alpha.
    /// LateUpdate early-out si endpoints nuls/inactifs. Zéro alloc.
    /// </summary>
    public class LinkBeamView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        /// <summary> Au-dessus des zones (−20), sous l'UI / télégraphe intent (12+). </summary>
        private const int SORTING_ORDER = 8;

        private const float WIDTH = 0.055f;
        private const float PULSE_SPEED = 2.2f;
        private const float PULSE_ALPHA_BASE = 0.42f;
        private const float PULSE_ALPHA_AMP = 0.22f;
        private const float INTENSE_ALPHA_BASE = 0.62f;
        private const float INTENSE_ALPHA_AMP = 0.28f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private LineRenderer _line;
        private Transform _a;
        private Transform _b;
        private Color _tint = Color.white;
        private bool _intense;
        private float _phase;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            _line = gameObject.GetComponent<LineRenderer>();
            if (_line == null)
                _line = gameObject.AddComponent<LineRenderer>();

            _line.positionCount = 2;
            _line.useWorldSpace = true;
            _line.startWidth = WIDTH;
            _line.endWidth = WIDTH * 0.75f;
            _line.sortingOrder = SORTING_ORDER;
            _line.numCapVertices = 2;

            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null)
                sh = Shader.Find("Unlit/Color");
            if (sh != null)
                _line.material = new Material(sh);

            _line.enabled = false;
        }

        private void LateUpdate()
        {
            if (_line == null)
                return;

            if (_a == null || _b == null
                || !_a.gameObject.activeInHierarchy
                || !_b.gameObject.activeInHierarchy)
            {
                if (_line.enabled)
                    _line.enabled = false;
                return;
            }

            if (!_line.enabled)
                _line.enabled = true;

            Vector3 pa = _a.position;
            Vector3 pb = _b.position;
            pa.z = 0f;
            pb.z = 0f;
            _line.SetPosition(0, pa);
            _line.SetPosition(1, pb);

            _phase += Time.unscaledDeltaTime * PULSE_SPEED;
            float wave = 0.5f + 0.5f * Mathf.Sin(_phase);
            float aBase = _intense ? INTENSE_ALPHA_BASE : PULSE_ALPHA_BASE;
            float aAmp = _intense ? INTENSE_ALPHA_AMP : PULSE_ALPHA_AMP;
            Color c = _tint;
            c.a = aBase + aAmp * wave;
            _line.startColor = c;
            _line.endColor = c;
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        public void SetEndpoints(Transform a, Transform b)
        {
            _a = a;
            _b = b;
        }

        public void SetTint(Color tint)
        {
            _tint = tint;
        }

        public void SetIntensity(bool intense)
        {
            _intense = intense;
        }

        public void SetVisible(bool visible)
        {
            if (_line != null)
                _line.enabled = visible && _a != null && _b != null;
            if (!visible)
            {
                _a = null;
                _b = null;
            }
        }
    }
}
