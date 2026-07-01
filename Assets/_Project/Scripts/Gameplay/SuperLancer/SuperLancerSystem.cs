using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Cycle de vie de la visée Super Lancer (démarrage, annulation, résolution au release).
    /// La rotation de l'indicateur et le bonus de force arrivent aux gates ultérieurs.
    /// </summary>
    public class SuperLancerSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static SuperLancerSystem Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private SuperLancerConfig config;

        [Tooltip("Instance de scène du prefab SuperLancerRing, désactivée par défaut.")]
        [SerializeField] private SuperLancerRingView ringView;

        // ═══════════════════════════════════════════
        // ÉTAT
        // ═══════════════════════════════════════════
        private enum AimState { Idle, Aiming }

        private AimState _state = AimState.Idle;
        private CharacterBall _currentBall;
        private float _aimStartTime;
        private bool _configWarningLogged;
        private bool _ringViewWarningLogged;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Release dans la zone : Super Lancer déclenché. </summary>
        public event System.Action<CharacterBall> OnSuperLancer;

        /// <summary> Release hors zone : lancer normal effectué. </summary>
        public event System.Action<CharacterBall> OnNormalLaunch;

        /// <summary>
        /// Visée annulée (cancel zone, distance insuffisante, panneau bloquant) :
        /// strictement neutre pour tout le système.
        /// </summary>
        public event System.Action OnAimCancelled;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Démarre la visée Super Lancer sur la bille en cours de drag. </summary>
        public void BeginAim(CharacterBall ball)
        {
            if (config == null)
            {
                if (!_configWarningLogged)
                {
                    Debug.LogWarning("[SuperLancer] SuperLancerConfig manquant — système inerte.");
                    _configWarningLogged = true;
                }
                return;
            }

            if (_state == AimState.Aiming)
                return;

            _state = AimState.Aiming;
            _currentBall = ball;
            _aimStartTime = Time.time;

            if (ringView == null && !_ringViewWarningLogged)
            {
                Debug.LogWarning("[SuperLancer] RingView non assignée — système fonctionnel sans visuel");
                _ringViewWarningLogged = true;
            }

            // L'anneau apparaît centré sur le personnage, indicateur figé à l'angle
            // de départ — la rotation arrive au gate Logique du geste.
            ringView?.Show(ball.transform.position, config);

            Debug.Log("[SuperLancer] Visée démarrée");
        }

        /// <summary> Annule la visée en cours sans lancer ni bonus. </summary>
        public void CancelAim()
        {
            if (_state != AimState.Aiming)
                return;

            ringView?.Hide();

            _state = AimState.Idle;
            _currentBall = null;
            OnAimCancelled?.Invoke();
            Debug.Log("[SuperLancer] Visée annulée — neutre");
        }

        /// <summary> Résout le release au lâcher du drag. Retourne true si Super Lancer déclenché. </summary>
        public bool ResolveRelease()
        {
            if (_state != AimState.Aiming)
                return false;

            ringView?.Hide();

            _state = AimState.Idle;

            // La détection de zone arrive au gate Logique du geste.
            OnNormalLaunch?.Invoke(_currentBall);
            _currentBall = null;
            Debug.Log("[SuperLancer] Release résolu : lancer normal");
            return false;
        }

        /// <summary> Multiplicateur de bonus de force à appliquer au lancer (0 = aucun bonus). </summary>
        public float ConsumeLaunchBonus()
        {
            // Bonus de base + bonus valises branchés aux gates ultérieurs.
            return 0f;
        }
    }
}
