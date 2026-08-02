using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Alucadra — Légèreté + Prédateur (alucadra_loup), fiche 5.6 phase 2.
    /// Dormant jusqu'à OnArchetypeChanged(Mobile). Pas de provider : Charge auto G5.
    /// Params : value=pas Prédateur (0.10), sv1=−DEF, sv2=+SPD, sv3=+force.
    /// OnAllySpecSwitched ACTIF — miroir inverse de l'exception Confesseur (commentaire).
    /// </summary>
    public class AlucadraLoupHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_DEF = "alucadra_legerete_def";
        private const string BUFF_SPD = "alucadra_legerete_spd";
        private const string BUFF_LF = "alucadra_legerete_force";
        private const string BUFF_PRED = "alucadra_predateur";
        // 3 rôles existants (Attacker/Defender/Support) = plafond +30 % naturel.
        private const int MAX_DISTINCT_ROLES = 3;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private bool _activated;
        private bool _subscribedArchetype;
        private bool _subscribedOwnerDeath;
        private bool _released;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "alucadra_loup";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;
            _activated = false;

            if (owner != null && !_subscribedArchetype)
            {
                owner.OnArchetypeChanged += OnArchetypeChanged;
                _subscribedArchetype = true;
            }

            if (owner != null && !_subscribedOwnerDeath)
            {
                owner.OnDeath += OnOwnerDeath;
                _subscribedOwnerDeath = true;
            }

            // Déjà Mobile (edge) → activer immédiatement.
            if (owner != null && owner.Archetype == EnemyArchetype.Mobile)
                ActivatePhase2();
        }

        public override void Cleanup()
        {
            ReleaseAllResources();
            base.Cleanup();
        }

        public override void ResetForNewStage()
        {
            // Remet le loup dormant pour le prochain étage (si handler réutilisé).
            if (_activated)
                RemovePhase2Buffs();
            _activated = false;
        }

        /// <summary>
        /// Override ACTIF — miroir inverse de l'exception R4 du Confesseur :
        /// le Prédateur DOIT se réévaluer au switch de spé alliée.
        /// Flottants ATK avant→après : gratuits via capture socle G4-P2.
        /// </summary>
        public override void OnAllySpecSwitched(CharacterBall ally)
        {
            if (!_activated || !IsReady)
                return;
            ApplyPredator();
        }

        public override void OnAllyKilled(CharacterBall ally)
        {
            if (!_activated || !IsReady)
                return;
            ApplyPredator();
        }

        // ═══════════════════════════════════════════
        // ACTIVATION
        // ═══════════════════════════════════════════

        private void OnArchetypeChanged(EnemyArchetype archetype)
        {
            if (_released || _activated)
                return;
            if (archetype != EnemyArchetype.Mobile)
                return;

            ActivatePhase2();
        }

        private void ActivatePhase2()
        {
            if (_activated || !IsReady || _data == null)
                return;

            _activated = true;
            ApplyLegerete();
            ApplyPredator();
        }

        private void ApplyLegerete()
        {
            if (_owner?.BuffReceiver == null || _data == null)
                return;

            // DEF −sv1, SPD +sv2 lisibles au bandeau au prochain recalcul de cycle (R1).
            ApplySelfBuff(BUFF_DEF, BuffStatType.DEF, -Mathf.Abs(_data.SpecialValue1));
            ApplySelfBuff(BUFF_SPD, BuffStatType.Speed, Mathf.Abs(_data.SpecialValue2));
            ApplySelfBuff(BUFF_LF, BuffStatType.LaunchForce, Mathf.Abs(_data.SpecialValue3));
        }

        private void ApplyPredator()
        {
            if (_owner?.BuffReceiver == null || _data == null || _turnManager == null)
                return;

            int distinct = CountDistinctActiveRoles();
            // Plafond +30 % naturel (3 rôles) — pas de clamp artificiel au-delà.
            if (distinct > MAX_DISTINCT_ROLES)
                distinct = MAX_DISTINCT_ROLES;

            float bonus = _data.Value * distinct;
            ApplySelfBuff(BUFF_PRED, BuffStatType.ATK, bonus);
        }

        private int CountDistinctActiveRoles()
        {
            var allies = _turnManager.GetAllies();
            if (allies == null)
                return 0;

            bool hasAttacker = false;
            bool hasDefender = false;
            bool hasSupport = false;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                CharacterRole role = ResolveRole(ally);
                switch (role)
                {
                    case CharacterRole.Attacker:
                        hasAttacker = true;
                        break;
                    case CharacterRole.Defender:
                        hasDefender = true;
                        break;
                    case CharacterRole.Support:
                        hasSupport = true;
                        break;
                }
            }

            int n = 0;
            if (hasAttacker) n++;
            if (hasDefender) n++;
            if (hasSupport) n++;
            return n;
        }

        private static CharacterRole ResolveRole(CharacterBall ally)
        {
            if (ally.ActiveSpec != null)
                return ally.ActiveSpec.Role;
            if (ally.Data != null)
                return ally.Data.Role;
            return CharacterRole.Attacker;
        }

        private void ApplySelfBuff(string buffId, BuffStatType stat, float value)
        {
            if (_owner?.BuffReceiver == null)
                return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = null,
                EnemySource = null,
                StatType = stat,
                Value = value,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniqueGlobal = true,
                UniquePerSource = false,
                ExpiresWithSource = false
            });
        }

        private void RemovePhase2Buffs()
        {
            if (_owner?.BuffReceiver == null)
                return;
            _owner.BuffReceiver.RemoveBuffsById(BUFF_DEF);
            _owner.BuffReceiver.RemoveBuffsById(BUFF_SPD);
            _owner.BuffReceiver.RemoveBuffsById(BUFF_LF);
            _owner.BuffReceiver.RemoveBuffsById(BUFF_PRED);
        }

        // ═══════════════════════════════════════════
        // HYGIÈNE
        // ═══════════════════════════════════════════

        private void OnOwnerDeath()
        {
            ReleaseAllResources();
        }

        private void ReleaseAllResources()
        {
            if (_released)
                return;
            _released = true;

            if (_owner != null && _subscribedArchetype)
            {
                _owner.OnArchetypeChanged -= OnArchetypeChanged;
                _subscribedArchetype = false;
            }

            if (_owner != null && _subscribedOwnerDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedOwnerDeath = false;
            }
        }
    }
}
