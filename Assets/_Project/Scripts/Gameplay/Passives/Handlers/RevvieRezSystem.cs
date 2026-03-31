using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Revvie :
    /// - maintient un allié marqué "résurrection programmée",
    /// - déclenche une résurrection unique par étage,
    /// - gère le lien vital (DEF temporaire sur Revvie + soin de l'allié marqué).
    /// </summary>
    public class RevvieRezSystem : MonoBehaviour
    {
        private const string RezMarkerBuffId = "revvie_rez_marker";
        private const string RezAtkBuffId = "revvie_rez_atk";
        private const string RezDefBuffId = "revvie_rez_def";
        private const string LinkDefBuffId = "revvie_link_def";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private bool _enhanced;
        private bool _rezUsedThisStage;

        private CharacterBall _markedAlly;
        private bool _subscribedToMarkedAllyDeath;
        private bool _subscribedToTurnChanged;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedToTurnChanged && _turnManager != null && _turnManager != turnManager)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedToTurnChanged = false;
            }

            _owner = owner;
            _turnManager = turnManager;

            if (!_subscribedToTurnChanged && _turnManager != null)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedToTurnChanged = true;
            }
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        public void RefreshRezMarker()
        {
            if (_owner == null || _turnManager == null) return;

            ClearCurrentMarker();

            var allies = _turnManager.GetAllies();
            if (allies == null) return;

            // Nettoie tous les marqueurs existants pour éviter les doublons.
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.BuffReceiver == null) continue;
                ally.BuffReceiver.RemoveBuffsById(RezMarkerBuffId);
            }

            CharacterBall weakest = null;
            float weakestRatio = float.MaxValue;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == _owner) continue;
                if (ally.MaxHp <= 0) continue;

                float ratio = (float)ally.CurrentHp / ally.MaxHp;
                if (ratio < weakestRatio)
                {
                    weakestRatio = ratio;
                    weakest = ally;
                }
            }

            if (weakest == null || weakest.BuffReceiver == null) return;

            _markedAlly = weakest;
            _markedAlly.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = RezMarkerBuffId,
                Source = _owner,
                StatType = BuffStatType.HP,
                Value = 0f,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            _markedAlly.OnDeath += OnMarkedAllyDeath;
            _subscribedToMarkedAllyDeath = true;
        }

        public void OnRevvieTakeDamage()
        {
            if (!_enhanced || _owner == null || _owner.BuffReceiver == null) return;
            if (_markedAlly == null || _markedAlly.IsDead) return;

            float defBonus = _markedAlly.EffectiveDef * 0.20f;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = LinkDefBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = defBonus,
                IsPercent = false,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        public void ResetForStage()
        {
            _rezUsedThisStage = false;
            ClearCurrentMarker();

            if (_owner != null && _owner.BuffReceiver != null)
                _owner.BuffReceiver.RemoveBuffsById(LinkDefBuffId);

            if (_turnManager != null)
            {
                var allies = _turnManager.GetAllies();
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        CharacterBall ally = allies[i];
                        if (ally == null || ally.BuffReceiver == null) continue;
                        ally.BuffReceiver.RemoveBuffsById(RezMarkerBuffId);
                        ally.BuffReceiver.RemoveBuffsById(RezAtkBuffId);
                        ally.BuffReceiver.RemoveBuffsById(RezDefBuffId);
                    }
                }
            }
        }

        private void OnMarkedAllyDeath()
        {
            if (_markedAlly == null) return;

            CharacterBall deadAlly = _markedAlly;

            if (!_rezUsedThisStage)
            {
                _rezUsedThisStage = true;

                deadAlly.Revive(0.20f);
                if (deadAlly.BuffReceiver != null)
                {
                    deadAlly.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = RezAtkBuffId,
                        Source = _owner,
                        StatType = BuffStatType.ATK,
                        Value = 0.20f,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = false,
                        UniqueGlobal = true
                    });
                    deadAlly.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = RezDefBuffId,
                        Source = _owner,
                        StatType = BuffStatType.DEF,
                        Value = 0.20f,
                        IsPercent = true,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = false,
                        UniqueGlobal = true
                    });
                }
            }

            ClearCurrentMarker();
            RefreshRezMarker();
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            if (_owner == null) return;
            if (!ReferenceEquals(participant, _owner)) return;

            RefreshRezMarker();

            if (_enhanced && _markedAlly != null && !_markedAlly.IsDead)
            {
                int heal = Mathf.RoundToInt(_markedAlly.MaxHp * 0.05f);
                if (heal > 0)
                    _markedAlly.Heal(heal);
            }

            if (_owner.BuffReceiver != null)
                _owner.BuffReceiver.RemoveBuffsById(LinkDefBuffId);
        }

        private void ClearCurrentMarker()
        {
            if (_markedAlly != null)
            {
                if (_markedAlly.BuffReceiver != null)
                    _markedAlly.BuffReceiver.RemoveBuffsById(RezMarkerBuffId);

                if (_subscribedToMarkedAllyDeath)
                    _markedAlly.OnDeath -= OnMarkedAllyDeath;
            }

            _markedAlly = null;
            _subscribedToMarkedAllyDeath = false;
        }

        private void OnDestroy()
        {
            ClearCurrentMarker();
            if (_subscribedToTurnChanged && _turnManager != null)
                _turnManager.OnTurnChanged -= OnTurnChanged;
        }
    }
}

