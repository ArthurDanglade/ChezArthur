using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Synergie Crescendo + Mode Furie : ralenti global près de la zone Super Lancer.
    /// Seul effet du système autorisé à faciliter le geste.
    /// </summary>
    public class BulletTimeController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string SYNERGY_ID = "synergie_crescendo_mode_furie";
        private const float BULLET_TIME_SCALE = 0.42f;
        private const float APPROACH_DEGREES = 28f;
        private const float NORMAL_TIME_SCALE = 1f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static BulletTimeController _instance;
        private bool _isSlowing;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static BulletTimeController Instance => _instance;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            RestoreTimeScale();
            if (_instance == this)
                _instance = null;
        }

        private void OnDisable()
        {
            RestoreTimeScale();
        }

        private void Update()
        {
            // Pause / finisher / UI : ne jamais forcer un timeScale si le jeu est déjà à 0.
            if (Time.timeScale <= 0f)
            {
                _isSlowing = false;
                return;
            }

            bool synergyActive = SynergyManager.Instance != null &&
                                 SynergyManager.Instance.IsSynergyActive(SYNERGY_ID);

            SuperLancerSystem super = SuperLancerSystem.Instance;
            bool shouldSlow = synergyActive &&
                              super != null &&
                              super.IsAiming &&
                              (super.IsIndicatorInZone || super.DegreesToZoneEdge <= APPROACH_DEGREES);

            if (shouldSlow)
            {
                if (!_isSlowing || !Mathf.Approximately(Time.timeScale, BULLET_TIME_SCALE))
                {
                    Time.timeScale = BULLET_TIME_SCALE;
                    _isSlowing = true;
                }
            }
            else if (_isSlowing)
            {
                RestoreTimeScale();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Garantit l'existence du contrôleur (bootstrap depuis le bridge).
        /// </summary>
        public static BulletTimeController EnsureExists(Transform parent = null)
        {
            if (_instance != null)
                return _instance;

            GameObject go = new GameObject(nameof(BulletTimeController));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.AddComponent<BulletTimeController>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void RestoreTimeScale()
        {
            if (!_isSlowing) return;
            if (Time.timeScale > 0f)
                Time.timeScale = NORMAL_TIME_SCALE;
            _isSlowing = false;
        }
    }
}
