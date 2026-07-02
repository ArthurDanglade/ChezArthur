using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Pilote ComboUI avec les sémantiques du compteur permanent Super Lancer :
    /// pop à chaque hit Super, fondu après inactivité.
    /// Remplace l'ancien pilotage par JuiceDirector (combo cross-tours, supprimé).
    /// </summary>
    public class SuperLancerCounterDriver : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Tooltip("Sur le même GameObject si laissé vide")]
        [SerializeField] private ComboUI _comboUI;
        [Tooltip("Durée d'affichage après le dernier hit avant fondu")]
        [SerializeField] private float _displayDuration = 2.5f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _hideTimer;
        private bool _visible;
        private bool _subscribed;
        private bool _warnedMissingSystem;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_comboUI == null)
                _comboUI = GetComponent<ComboUI>();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void Start()
        {
            if (_subscribed)
                return;

            if (TrySubscribe())
                return;

            if (!_warnedMissingSystem)
            {
                Debug.LogWarning("[SuperLancerCounterDriver] SuperLancerSystem absent — compteur inerte.");
                _warnedMissingSystem = true;
            }
        }

        private void OnDisable()
        {
            if (SuperLancerSystem.Instance != null && _subscribed)
                SuperLancerSystem.Instance.OnSuperHitCountChanged -= HandleHit;

            _subscribed = false;
        }

        private void Update()
        {
            if (!_visible) return;

            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
            {
                _comboUI?.EndCombo();
                _visible = false;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private bool TrySubscribe()
        {
            if (_subscribed || SuperLancerSystem.Instance == null)
                return _subscribed;

            SuperLancerSystem.Instance.OnSuperHitCountChanged += HandleHit;
            _subscribed = true;
            return true;
        }

        private void HandleHit(int total)
        {
            _comboUI?.OnCombo(total);
            _hideTimer = _displayDuration;
            _visible = true;
        }
    }
}
