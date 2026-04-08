using System.Collections.Generic;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Bouclier ennemi à HP dédiés, absorption avant les PV, régénération optionnelle par cycle, fragments (ex. Cœur du Désert).
    /// </summary>
    public class EnemyShieldSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // STRUCT INTERNE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Fragment de bouclier avec HP propres.
        /// Utilisé par Le Cœur du Désert et tout boss à phases.
        /// </summary>
        private struct EnemyShieldFragment
        {
            public int CurrentHp;
            public int MaxHp;
            public bool IsAlive => CurrentHp > 0;
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private Enemy _owner;
        private TurnManager _turnManager;
        private EnemyPassiveRuntime _passiveRuntime;

        private int _shieldHp;
        private int _shieldMaxHp;
        private bool _shieldActive;

        private bool _regenEnabled;
        private float _regenFraction;
        private bool _subscribed;

        private List<EnemyShieldFragment> _fragments;
        private bool _hasFragments;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> True si le bouclier principal est actif. </summary>
        public bool ShieldActive => _shieldActive;

        /// <summary> HP actuels du bouclier principal. </summary>
        public int ShieldHp => _shieldHp;

        /// <summary> HP max du bouclier principal. </summary>
        public int ShieldMaxHp => _shieldMaxHp;

        /// <summary> Nombre de fragments encore en vie. </summary>
        public int AliveFragmentCount { get; private set; }

        /// <summary> True si tous les fragments sont détruits. </summary>
        public bool AllFragmentsDestroyed => _hasFragments && AliveFragmentCount == 0;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise le système. Appelé par EnemyPassiveRuntime ou StageGenerator.
        /// </summary>
        public void Initialize(Enemy owner, TurnManager turnManager)
        {
            UnsubscribeCycle();

            _owner = owner;
            _turnManager = turnManager;
            _passiveRuntime = _owner != null ? _owner.GetComponent<EnemyPassiveRuntime>() : null;

            _shieldHp = 0;
            _shieldMaxHp = 0;
            _shieldActive = false;

            _regenEnabled = false;
            _regenFraction = 0f;

            if (_fragments == null)
                _fragments = new List<EnemyShieldFragment>(8);
            else
                _fragments.Clear();

            _hasFragments = false;
            AliveFragmentCount = 0;
        }

        /// <summary>
        /// Active un bouclier absorbant X% des HP max de l'ennemi.
        /// </summary>
        public void ActivateShield(float hpFraction)
        {
            if (_owner == null)
                return;

            _shieldMaxHp = Mathf.RoundToInt(_owner.MaxHp * hpFraction);
            _shieldHp = _shieldMaxHp;
            _shieldActive = _shieldMaxHp > 0;
        }

        /// <summary>
        /// Active la régénération automatique du bouclier par cycle.
        /// </summary>
        public void EnableShieldRegen(float regenFraction)
        {
            _regenEnabled = true;
            _regenFraction = regenFraction;
            SubscribeCycleIfNeeded();
        }

        /// <summary>
        /// Intercepte les dégâts entrants. Retourne les dégâts restants après absorption bouclier.
        /// </summary>
        public int AbsorbDamage(int incomingDamage)
        {
            if (incomingDamage <= 0)
                return incomingDamage;

            if (!_shieldActive || _shieldHp <= 0)
                return incomingDamage;

            int absorbed = _shieldHp < incomingDamage ? _shieldHp : incomingDamage;
            _shieldHp -= absorbed;
            int remaining = incomingDamage - absorbed;

            if (_shieldHp <= 0)
            {
                _shieldHp = 0;
                _shieldActive = false;
                NotifyShieldBroken();
            }

            return remaining;
        }

        /// <summary>
        /// Configure des fragments avec HP fixes (Le Cœur du Désert).
        /// </summary>
        public void SetupFragments(int count, int hpPerFragment)
        {
            if (_fragments == null)
                _fragments = new List<EnemyShieldFragment>(count > 0 ? count : 4);
            else
                _fragments.Clear();

            AliveFragmentCount = 0;

            for (int i = 0; i < count; i++)
            {
                _fragments.Add(new EnemyShieldFragment
                {
                    CurrentHp = hpPerFragment,
                    MaxHp = hpPerFragment
                });
                AliveFragmentCount++;
            }

            _hasFragments = _fragments.Count > 0;
        }

        /// <summary>
        /// Inflige des dégâts à un fragment spécifique. Retourne true si le fragment vient d'être détruit.
        /// </summary>
        public bool DamageFragment(int fragmentIndex, int damage)
        {
            if (damage <= 0 || _fragments == null)
                return false;
            if (fragmentIndex < 0 || fragmentIndex >= _fragments.Count)
                return false;

            EnemyShieldFragment frag = _fragments[fragmentIndex];
            if (!frag.IsAlive)
                return false;

            frag.CurrentHp -= damage;
            if (frag.CurrentHp < 0)
                frag.CurrentHp = 0;

            _fragments[fragmentIndex] = frag;

            if (frag.CurrentHp <= 0)
            {
                AliveFragmentCount = Mathf.Max(0, AliveFragmentCount - 1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Régénère tous les fragments encore en vie à X% de leurs HP max.
        /// </summary>
        public void RegenFragments(float fraction)
        {
            if (_fragments == null || _fragments.Count == 0)
                return;

            for (int i = 0; i < _fragments.Count; i++)
            {
                EnemyShieldFragment frag = _fragments[i];
                if (!frag.IsAlive)
                    continue;

                int add = Mathf.RoundToInt(frag.MaxHp * fraction);
                frag.CurrentHp = frag.MaxHp < frag.CurrentHp + add ? frag.MaxHp : frag.CurrentHp + add;
                _fragments[i] = frag;
            }
        }

        /// <summary>
        /// Délégué au handler pour l’instant : approximation via AllFragmentsDestroyed.
        /// </summary>
        public bool WereAllFragmentsDestroyedSimultaneously()
        {
            return AllFragmentsDestroyed;
        }

        /// <summary>
        /// Remet le bouclier à zéro pour un nouvel étage.
        /// </summary>
        public void ResetForNewStage()
        {
            UnsubscribeCycle();

            _shieldHp = 0;
            _shieldMaxHp = 0;
            _shieldActive = false;

            _regenEnabled = false;
            _regenFraction = 0f;

            _fragments?.Clear();
            _hasFragments = false;
            AliveFragmentCount = 0;
        }

        /// <summary>
        /// Nettoie les abonnements.
        /// </summary>
        public void Cleanup()
        {
            UnsubscribeCycle();
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void OnDestroy()
        {
            Cleanup();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void SubscribeCycleIfNeeded()
        {
            if (_subscribed || _turnManager == null || !_regenEnabled)
                return;

            _turnManager.OnCycleStarted += OnCycleStarted;
            _subscribed = true;
        }

        private void UnsubscribeCycle()
        {
            if (_turnManager != null && _subscribed)
                _turnManager.OnCycleStarted -= OnCycleStarted;

            _subscribed = false;
        }

        private void OnCycleStarted()
        {
            if (!_regenEnabled || _shieldMaxHp <= 0 || _owner == null || _owner.IsDead)
                return;

            int regen = Mathf.RoundToInt(_shieldMaxHp * _regenFraction);
            if (regen <= 0)
                return;

            int before = _shieldHp;
            _shieldHp = _shieldMaxHp < _shieldHp + regen ? _shieldMaxHp : _shieldHp + regen;
            _shieldActive = _shieldHp > 0;

            if (_shieldHp > before)
                NotifyShieldRegenerated();
        }

        private void NotifyShieldBroken()
        {
            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTrigger(EnemyPassiveTrigger.OnShieldBroken);
        }

        private void NotifyShieldRegenerated()
        {
            if (_passiveRuntime != null)
                _passiveRuntime.NotifyTrigger(EnemyPassiveTrigger.OnShieldRegenerated);
        }
    }
}
