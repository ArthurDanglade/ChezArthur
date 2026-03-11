using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Gère les passifs actifs d'un CharacterBall pendant la run (stacks, triggers, bonus).
    /// Un composant par CharacterBall ; initialisé via InitializeForRun avec la spé et le niveau.
    /// </summary>
    public class CharacterPassiveRuntime : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<PassiveInstance> _activePassives;
        private CharacterBall _characterBall;
        private bool _initialized;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public IReadOnlyList<PassiveInstance> ActivePassives => _activePassives;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _activePassives = new List<PassiveInstance>(4);
            _characterBall = GetComponent<CharacterBall>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — Initialisation
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise les passifs pour la run à partir de la spécialisation et du niveau du personnage.
        /// </summary>
        public void InitializeForRun(SpecializationData spec, int characterLevel)
        {
            _activePassives.Clear();

            if (spec != null)
            {
                List<PassiveData> passives = spec.GetActivePassives(characterLevel);
                if (passives != null)
                {
                    for (int i = 0; i < passives.Count; i++)
                    {
                        PassiveData p = passives[i];
                        if (p != null)
                            _activePassives.Add(new PassiveInstance(p));
                    }
                }
            }

            _initialized = true;
            Debug.Log($"[CharacterPassiveRuntime] Initialisé avec {_activePassives.Count} passifs pour {gameObject.name}");
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — Triggers
        // ═══════════════════════════════════════════

        /// <summary>
        /// Notifie tous les passifs d'un trigger (ex: OnHitEnemy). Les passifs éligibles gagnent un stack.
        /// </summary>
        public void NotifyTrigger(PassiveTrigger trigger)
        {
            if (!_initialized || _activePassives == null) return;

            for (int i = 0; i < _activePassives.Count; i++)
                _activePassives[i].TryTrigger(trigger);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — Lecture des bonus
        // ═══════════════════════════════════════════

        /// <summary>
        /// Somme des bonus de stat pour l'effet donné (ex: BuffATK).
        /// </summary>
        public float GetStatBonus(PassiveEffect effect)
        {
            if (_activePassives == null) return 0f;

            float total = 0f;
            for (int i = 0; i < _activePassives.Count; i++)
            {
                if (_activePassives[i].Data != null && _activePassives[i].Data.Effect == effect)
                    total += _activePassives[i].GetStatBonus();
            }
            return total;
        }

        /// <summary>
        /// Somme des bonus d'effet "team" (BuffTeamATK, BuffTeamDEF) pour l'effet donné.
        /// </summary>
        public float GetTeamStatBonus(PassiveEffect effect)
        {
            if (effect != PassiveEffect.BuffTeamATK && effect != PassiveEffect.BuffTeamDEF) return 0f;
            return GetStatBonus(effect);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — Reset
        // ═══════════════════════════════════════════

        /// <summary>
        /// Reset les stacks des passifs dont la règle est ResetPerStage.
        /// </summary>
        public void ResetForNewStage()
        {
            if (_activePassives == null) return;

            for (int i = 0; i < _activePassives.Count; i++)
            {
                if (_activePassives[i].ShouldResetOnNewStage())
                    _activePassives[i].ResetStacks();
            }
        }

        /// <summary>
        /// Remet tous les stacks de tous les passifs à zéro.
        /// </summary>
        public void ResetAllStacks()
        {
            if (_activePassives == null) return;

            for (int i = 0; i < _activePassives.Count; i++)
                _activePassives[i].ResetStacks();
        }

        /// <summary>
        /// Reset tous les stacks puis vide la liste (changement de spé ; le perso sera réinitialisé avec la nouvelle spé).
        /// </summary>
        public void ResetForSpecSwitch()
        {
            if (_activePassives == null) return;

            for (int i = 0; i < _activePassives.Count; i++)
                _activePassives[i].ResetStacks();
            _activePassives.Clear();
        }
    }
}
