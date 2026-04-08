using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Enemies;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de vie world space pour ennemi.
    /// S'abonne aux événements de dégâts et mort pour se mettre à jour.
    /// </summary>
    public class EnemyHPBar : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image fillImage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _enemy;
        private System.Action<int> _damagedHandler;
        private System.Action _deathHandler;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise la barre avec un ennemi et branche les événements.
        /// </summary>
        public void Initialize(Enemy enemy)
        {
            if (_enemy != null)
            {
                if (_damagedHandler != null)
                    _enemy.OnDamaged -= _damagedHandler;
                if (_deathHandler != null)
                    _enemy.OnDeath -= _deathHandler;
            }

            _enemy = enemy;
            if (enemy == null)
            {
                if (fillImage != null)
                    fillImage.fillAmount = 0f;
                return;
            }

            _damagedHandler = _ => UpdateDisplay();
            _deathHandler = OnEnemyDeath;

            enemy.OnDamaged += _damagedHandler;
            enemy.OnDeath += _deathHandler;

            UpdateDisplay();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void UpdateDisplay()
        {
            if (_enemy == null) return;

            int current = _enemy.CurrentHp;
            int max = Mathf.Max(1, _enemy.MaxHp);
            float ratio = (float)current / max;
            if (fillImage != null)
                fillImage.fillAmount = ratio;

            // Pas de changement de couleur — on garde le sprite
            // rouge de l'artiste tel quel
        }

        private void OnEnemyDeath()
        {
            if (fillImage != null)
                fillImage.fillAmount = 0f;
            gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnDestroy()
        {
            if (_enemy == null) return;

            if (_damagedHandler != null)
                _enemy.OnDamaged -= _damagedHandler;
            if (_deathHandler != null)
                _enemy.OnDeath -= _deathHandler;
        }
    }
}
