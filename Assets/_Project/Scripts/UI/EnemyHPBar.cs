using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Feedback;

namespace ChezArthur.UI
{
    /// <summary>
    /// Barre de vie world space pour ennemi.
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
        private StatusPipsRail _pipsRail;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        public void SetFill(float ratio)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        public void SetWidth(float width)
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null) return;

            Vector2 size = rt.sizeDelta;
            size.x = width;
            rt.sizeDelta = size;

            // Pas de changement de couleur — on garde le sprite
            // rouge de l'artiste tel quel
        }

        /// <summary>
        /// Bind pastilles sur le UnitStatusFx de l'ennemi (rebind propre pour pool).
        /// </summary>
        public void BindStatus(Enemy enemy)
        {
            UnbindStatus();
            if (enemy == null)
                return;

            UnitStatusFx fx = enemy.GetComponent<UnitStatusFx>();
            EnsurePipsRail().Bind(fx);
        }

        /// <summary>
        /// Détache les pastilles (obligatoire avant remise en pool).
        /// </summary>
        public void UnbindStatus()
        {
            if (_pipsRail != null)
                _pipsRail.Unbind();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private StatusPipsRail EnsurePipsRail()
        {
            if (_pipsRail != null)
                return _pipsRail;

            GameObject railGo = new GameObject("StatusPipsRail");
            railGo.transform.SetParent(transform, false);
            _pipsRail = railGo.AddComponent<StatusPipsRail>();
            return _pipsRail;
        }

        private void OnDestroy()
        {
            UnbindStatus();
        }
    }
}
