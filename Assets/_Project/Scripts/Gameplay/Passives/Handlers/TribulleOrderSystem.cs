using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Tribulle :
    /// le premier contact du lancer détermine le type d'effet à distribuer.
    /// </summary>
    public class TribulleOrderSystem : MonoBehaviour
    {
        private enum FirstContactType
        {
            None,
            Ally,
            Enemy,
            Wall
        }

        private const string TribulleAtkBuffId = "tribulle_atk";
        private const string TribulleDefBuffId = "tribulle_def";
        private const string TribulleSelfAtkBuffId = "tribulle_self_atk";
        private const string TribulleSelfDefBuffId = "tribulle_self_def";

        private CharacterBall _owner;
        private bool _enhanced;
        private FirstContactType _firstContact;
        private bool _buffApplied;
        private bool _isLaunching;

        private bool _subscribedHitEnemy;
        private bool _subscribedHitAlly;
        private bool _subscribedBounceWall;
        private bool _subscribedStopped;

        public void Initialize(CharacterBall owner)
        {
            if (owner == null) return;

            if (_owner != null && _owner != owner)
                UnsubscribeOwnerEvents();

            _owner = owner;
            SubscribeOwnerEvents();
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        public void ResetForLaunch()
        {
            _firstContact = FirstContactType.None;
            _buffApplied = false;
            _isLaunching = true;

            // Nettoyage des copies self d'un lancer précédent.
            if (_owner != null && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.RemoveBuffsById(TribulleSelfAtkBuffId);
                _owner.BuffReceiver.RemoveBuffsById(TribulleSelfDefBuffId);
            }
        }

        public void OnEnemyContact()
        {
            if (!_isLaunching || _buffApplied) return;
            if (_firstContact == FirstContactType.None)
                _firstContact = FirstContactType.Enemy;
        }

        public void OnWallContact()
        {
            if (!_isLaunching || _buffApplied) return;
            if (_firstContact == FirstContactType.None)
                _firstContact = FirstContactType.Wall;
        }

        public void OnAllyContact(CharacterBall ally)
        {
            if (!_isLaunching || _buffApplied || ally == null || ally.IsDead) return;
            if (ally.BuffReceiver == null) return;

            float buffValue = _enhanced ? 0.20f : 0.15f;

            switch (_firstContact)
            {
                case FirstContactType.None:
                case FirstContactType.Ally:
                    _firstContact = FirstContactType.Ally;
                    ally.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = TribulleAtkBuffId,
                        Source = _owner,
                        StatType = BuffStatType.ATK,
                        Value = buffValue,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });

                    if (_enhanced && _owner != null && _owner.BuffReceiver != null)
                    {
                        _owner.BuffReceiver.AddBuff(new BuffData
                        {
                            BuffId = TribulleSelfAtkBuffId,
                            Source = _owner,
                            StatType = BuffStatType.ATK,
                            Value = ally.EffectiveAtk,
                            IsPercent = false,
                            RemainingTurns = 1,
                            RemainingCycles = -1,
                            UniquePerSource = false,
                            UniqueGlobal = true
                        });
                    }
                    _buffApplied = true;
                    break;

                case FirstContactType.Enemy:
                    ally.BuffReceiver.AddBuff(new BuffData
                    {
                        BuffId = TribulleDefBuffId,
                        Source = _owner,
                        StatType = BuffStatType.DEF,
                        Value = buffValue,
                        IsPercent = true,
                        RemainingTurns = 1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });

                    if (_enhanced && _owner != null && _owner.BuffReceiver != null)
                    {
                        _owner.BuffReceiver.AddBuff(new BuffData
                        {
                            BuffId = TribulleSelfDefBuffId,
                            Source = _owner,
                            StatType = BuffStatType.DEF,
                            Value = ally.EffectiveDef,
                            IsPercent = false,
                            RemainingTurns = 1,
                            RemainingCycles = -1,
                            UniquePerSource = false,
                            UniqueGlobal = true
                        });
                    }
                    _buffApplied = true;
                    break;

                case FirstContactType.Wall:
                    int healPercent = Mathf.RoundToInt(ally.MaxHp * buffValue);
                    if (healPercent > 0)
                        ally.Heal(healPercent);

                    if (_enhanced && _owner != null)
                    {
                        int selfHeal = Mathf.RoundToInt(_owner.MaxHp * 0.10f);
                        if (selfHeal > 0)
                            _owner.Heal(selfHeal);
                    }
                    _buffApplied = true;
                    break;
            }
        }

        private void OnOwnerStopped()
        {
            _isLaunching = false;
        }

        private void SubscribeOwnerEvents()
        {
            if (_owner == null) return;

            if (!_subscribedHitEnemy)
            {
                _owner.OnHitEnemy += OnEnemyContact;
                _subscribedHitEnemy = true;
            }
            if (!_subscribedHitAlly)
            {
                _owner.OnHitAllyEvent += OnAllyContact;
                _subscribedHitAlly = true;
            }
            if (!_subscribedBounceWall)
            {
                _owner.OnBounceWallEvent += OnWallContact;
                _subscribedBounceWall = true;
            }
            if (!_subscribedStopped)
            {
                _owner.OnStopped += OnOwnerStopped;
                _subscribedStopped = true;
            }
        }

        private void UnsubscribeOwnerEvents()
        {
            if (_owner == null) return;

            if (_subscribedHitEnemy) _owner.OnHitEnemy -= OnEnemyContact;
            if (_subscribedHitAlly) _owner.OnHitAllyEvent -= OnAllyContact;
            if (_subscribedBounceWall) _owner.OnBounceWallEvent -= OnWallContact;
            if (_subscribedStopped) _owner.OnStopped -= OnOwnerStopped;

            _subscribedHitEnemy = false;
            _subscribedHitAlly = false;
            _subscribedBounceWall = false;
            _subscribedStopped = false;
        }

        private void OnDestroy()
        {
            UnsubscribeOwnerEvents();
        }
    }
}

