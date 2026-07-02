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
        private float _currentAngleDeg;
        private float _pendingLaunchBonus;
        private bool _configWarningLogged;
        private bool _ringViewWarningLogged;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Release dans la zone : Super Lancer déclenché. </summary>
        public event System.Action<CharacterBall> OnSuperLancer;

        /// <summary> Release hors zone : lancer normal effectué. </summary>
        public event System.Action<CharacterBall> OnNormalLaunch;

        /// <summary> Hits ricochet Super Lancer — total cumulé sur la run (événement à chaque incrément). </summary>
        public event System.Action<int> OnSuperHitCountChanged;

        /// <summary>
        /// Total de hits ennemis réalisés en vol Super Lancer sur la run en cours.
        /// Jamais remis à zéro en cours de run.
        /// </summary>
        public int RunSuperHitCount { get; private set; }

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

        private void Update()
        {
            if (_state != AimState.Aiming || config == null) return;

            float aimDuration = Time.time - _aimStartTime;
            float speed = config.GetRotationSpeed(aimDuration);
            float direction = config.Clockwise ? 1f : -1f;

            // Temps SCALÉ délibérément : le Bullet Time (synergie Crescendo+Furie)
            // ralentira timeScale et DOIT ralentir cet indicateur — ne pas passer
            // en unscaledDeltaTime. La pause annule la visée avant timeScale = 0.
            _currentAngleDeg = Mathf.Repeat(_currentAngleDeg + speed * direction * Time.deltaTime, 360f);
            ringView?.SetIndicatorAngle(_currentAngleDeg);

            float absDelta = Mathf.Abs(Mathf.DeltaAngle(config.ZoneCenterAngleDeg, _currentAngleDeg));
            float halfWindow = config.ZoneSizeNormalized * 180f;
            float degreesToEdge = Mathf.Max(0f, absDelta - halfWindow);
            bool inZone = config.IsInZone(_currentAngleDeg);
            JuiceDirector.Instance?.UpdateAimTension(degreesToEdge, inZone);
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

            _currentAngleDeg = config.IndicatorStartAngleDeg;

            // L'anneau apparaît centré sur le personnage, indicateur à l'angle de départ.
            ringView?.Show(ball.transform.position, config);

            JuiceDirector.Instance?.BeginAimTension();

            Debug.Log("[SuperLancer] Visée démarrée");
        }

        /// <summary> Annule la visée en cours sans lancer ni bonus. </summary>
        public void CancelAim()
        {
            if (_state != AimState.Aiming)
                return;

            ringView?.Hide();

            // Neutralité du raté : la tension s'ÉTEINT, aucun son d'échec, jamais.
            JuiceDirector.Instance?.EndAimTension();

            _state = AimState.Idle;
            _currentBall = null;
            _pendingLaunchBonus = 0f; // Un cancel ne laisse JAMAIS un bonus armé pour le lancer suivant.
            OnAimCancelled?.Invoke();
            Debug.Log("[SuperLancer] Visée annulée — neutre");
        }

        /// <summary> Résout le release au lâcher du drag. Retourne true si Super Lancer déclenché. </summary>
        public bool ResolveRelease()
        {
            if (_state != AimState.Aiming)
                return false;

            _state = AimState.Idle;

            // Neutralité du raté : la tension s'ÉTEINT, aucun son d'échec, jamais.
            JuiceDirector.Instance?.EndAimTension();

            CharacterBall ball = _currentBall;
            _currentBall = null;
            bool isSuper = config.IsInZone(_currentAngleDeg);
            if (isSuper)
            {
                ringView?.HideWithSuccessFlash();
                _pendingLaunchBonus = config.BaseLaunchForceBonus;
                OnSuperLancer?.Invoke(ball);
                Debug.Log($"[SuperLancer] SUPER LANCER ! Angle {_currentAngleDeg:0.0}°, bonus +{_pendingLaunchBonus:P0}");
            }
            else
            {
                ringView?.Hide();
                _pendingLaunchBonus = 0f;
                OnNormalLaunch?.Invoke(ball);
                Debug.Log($"[SuperLancer] Lancer normal — angle {_currentAngleDeg:0.0}° hors zone");
            }
            return isSuper;
        }

        /// <summary> Multiplicateur de bonus de force à appliquer au lancer (0 = aucun bonus). </summary>
        public float ConsumeLaunchBonus()
        {
            float bonus = _pendingLaunchBonus;
            _pendingLaunchBonus = 0f; // consommation unique, jamais de bonus résiduel
            return bonus;
        }

        /// <summary> Notifié par CharacterBall à chaque hit ennemi ; incrémente uniquement si le vol en cours est un Super Lancer. </summary>
        public void NotifyEnemyHit(CharacterBall ball)
        {
            if (ball == null || !ball.IsSuperLaunch) return;

            RunSuperHitCount++;
            OnSuperHitCountChanged?.Invoke(RunSuperHitCount);
            Debug.Log($"[SuperLancer] Hit Super #{RunSuperHitCount}");
        }

        /// <summary> Remet à zéro le compteur de run (nouvelle run). </summary>
        public void ResetRunSuperHitCount() => RunSuperHitCount = 0;
    }
}
