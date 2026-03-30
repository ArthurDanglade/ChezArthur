using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Passives;

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
        // Stocke les passifs gelés par index de spé. Clé = specIndex, valeur = liste de passifs avec stacks conservés.
        private Dictionary<int, List<PassiveInstance>> _frozenPassivesBySpec;
        // Index de la spé actuellement active (-1 = base).
        private int _currentSpecIndex;
        // Index de la spé enregistrée au début du tour.
        private int _specIndexAtTurnStart;
        private CharacterBall _characterBall;
        private bool _initialized;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public IReadOnlyList<PassiveInstance> ActivePassives => _activePassives;
        public int CurrentSpecIndex => _currentSpecIndex;
        public int SpecIndexAtTurnStart => _specIndexAtTurnStart;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _activePassives = new List<PassiveInstance>(4);
            _frozenPassivesBySpec = new Dictionary<int, List<PassiveInstance>>(3);
            _currentSpecIndex = -1;
            _specIndexAtTurnStart = -1;
            _characterBall = GetComponent<CharacterBall>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — Initialisation
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise les passifs pour la run à partir de la spécialisation et du niveau du personnage.
        /// </summary>
        public void InitializeForRun(SpecializationData spec, int characterLevel, int specIndex = -1)
        {
            _activePassives.Clear();
            _frozenPassivesBySpec.Clear();

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

            _currentSpecIndex = specIndex;
            _specIndexAtTurnStart = specIndex;
            _initialized = true;
            Debug.Log($"[CharacterPassiveRuntime] Initialisé avec {_activePassives.Count} passifs pour {gameObject.name}");
        }

        /// <summary>
        /// Change la spécialisation active en gelant les passifs actuels et en dégelant/créant ceux de la nouvelle spé.
        /// </summary>
        public void SwitchSpec(SpecializationData newSpec, int newSpecIndex, int characterLevel)
        {
            if (!_initialized || _activePassives == null) return;

            int previousSpecIndex = _currentSpecIndex;

            // Gèle la liste active actuelle avec ses stacks intacts.
            if (_activePassives.Count > 0)
            {
                if (_frozenPassivesBySpec.TryGetValue(previousSpecIndex, out List<PassiveInstance> previousFrozen))
                {
                    previousFrozen.Clear();
                    previousFrozen.AddRange(_activePassives);
                }
                else
                {
                    _frozenPassivesBySpec[previousSpecIndex] = new List<PassiveInstance>(_activePassives);
                }
            }

            _activePassives.Clear();

            // Dégèle les passifs de la spé ciblée si déjà rencontrée ; sinon on les crée.
            if (_frozenPassivesBySpec.TryGetValue(newSpecIndex, out List<PassiveInstance> frozenForTargetSpec))
            {
                _activePassives.AddRange(frozenForTargetSpec);
                _frozenPassivesBySpec.Remove(newSpecIndex);
            }
            else if (newSpec != null)
            {
                List<PassiveData> passives = newSpec.GetActivePassives(characterLevel);
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

            _currentSpecIndex = newSpecIndex;
            Debug.Log($"[CharacterPassiveRuntime] SwitchSpec {gameObject.name} : {previousSpecIndex} -> {_currentSpecIndex}");
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
            {
                bool triggered = _activePassives[i].TryTrigger(trigger);

                if (triggered)
                    TriggerSpecialHandler(_activePassives[i], trigger, null, null, 0);
            }
        }

        /// <summary>
        /// Notifie avec contexte complet (ennemi/allié touché, dégâts).
        /// Utilisé par CharacterBall quand des infos supplémentaires sont disponibles.
        /// </summary>
        public void NotifyTriggerWithContext(PassiveTrigger trigger, Enemy hitEnemy = null, CharacterBall hitAlly = null, int damageAmount = 0)
        {
            if (!_initialized || _activePassives == null) return;

            for (int i = 0; i < _activePassives.Count; i++)
            {
                bool triggered = _activePassives[i].TryTrigger(trigger);

                if (triggered)
                    TriggerSpecialHandler(_activePassives[i], trigger, hitEnemy, hitAlly, damageAmount);
            }
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

        /// <summary>
        /// Enregistre la spé active au début du tour.
        /// </summary>
        public void RecordSpecAtTurnStart()
        {
            _specIndexAtTurnStart = _currentSpecIndex;
        }

        /// <summary>
        /// Indique si la spé active a changé depuis le début du tour.
        /// </summary>
        public bool HasSwitchedSinceTurnStart()
        {
            return _currentSpecIndex != _specIndexAtTurnStart;
        }

        /// <summary>
        /// Notifie le trigger OnSpecSwitch si la spé a changé depuis le début du tour.
        /// </summary>
        public void NotifySpecSwitchIfNeeded()
        {
            if (HasSwitchedSinceTurnStart())
                NotifyTrigger(PassiveTrigger.OnSpecSwitch);
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

            if (_frozenPassivesBySpec == null) return;

            foreach (KeyValuePair<int, List<PassiveInstance>> kvp in _frozenPassivesBySpec)
            {
                List<PassiveInstance> list = kvp.Value;
                if (list == null) continue;

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].ShouldResetOnNewStage())
                        list[i].ResetStacks();
                }
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

            if (_frozenPassivesBySpec == null) return;

            foreach (KeyValuePair<int, List<PassiveInstance>> kvp in _frozenPassivesBySpec)
            {
                List<PassiveInstance> list = kvp.Value;
                if (list == null) continue;

                for (int i = 0; i < list.Count; i++)
                    list[i].ResetStacks();
            }
        }

        /// <summary>
        /// Reset tous les stacks puis vide toutes les structures de passifs (actifs + gelés).
        /// </summary>
        public void ClearAllPassives()
        {
            if (_activePassives == null) return;

            for (int i = 0; i < _activePassives.Count; i++)
                _activePassives[i].ResetStacks();
            _activePassives.Clear();

            if (_frozenPassivesBySpec != null)
            {
                foreach (KeyValuePair<int, List<PassiveInstance>> kvp in _frozenPassivesBySpec)
                {
                    List<PassiveInstance> list = kvp.Value;
                    if (list == null) continue;

                    for (int i = 0; i < list.Count; i++)
                        list[i].ResetStacks();
                    list.Clear();
                }
                _frozenPassivesBySpec.Clear();
            }

            _currentSpecIndex = -1;
            _specIndexAtTurnStart = -1;
        }

        /// <summary>
        /// Alias rétrocompatible : vide tous les passifs (utiliser ClearAllPassives()).
        /// </summary>
        public void ResetForSpecSwitch()
        {
            ClearAllPassives();
        }

        /// <summary>
        /// Route le passif vers un handler spécial si un specialEffectId est configuré.
        /// </summary>
        private void TriggerSpecialHandler(PassiveInstance instance, PassiveTrigger trigger, Enemy hitEnemy, CharacterBall hitAlly, int damageAmount)
        {
            if (instance == null || instance.Data == null) return;
            if (!instance.Data.HasSpecialEffect) return;

            SpecialPassiveRegistry registry = SpecialPassiveRegistry.Instance;
            if (registry == null) return;

            ISpecialPassiveHandler handler = registry.GetHandler(instance.Data.SpecialEffectId);
            if (handler == null) return;

            PassiveContext context = registry.GetSharedContext();
            context.Owner = _characterBall;
            context.TurnManager = _characterBall != null ? _characterBall.GetTurnManager() : null;
            context.Trigger = trigger;
            context.HitEnemy = hitEnemy;
            context.HitAlly = hitAlly;
            context.DamageAmount = damageAmount;

            handler.OnTriggered(context, instance.Data, instance);
        }
    }
}
