using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Système central Goat (tranche 1 - spé ATK) :
    /// - P3: suivi rebonds mur avant 1er ennemi + bonus ATK de lancer
    /// - P4: compteur de kills persistant run
    /// - buffs temporaires au switch de spé ATK
    /// </summary>
    public class GoatSystem : MonoBehaviour
    {
        private const string SimulatorAtkBuffId = "goat_simulator_atk";
        private const string TempAtkBuffId = "goat_atk_switch_tmp";
        private const string TempDefBuffId = "goat_def_switch_tmp";
        private const string TempLfBuffId = "goat_lf_switch_tmp";

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private CharacterPassiveRuntime _runtime;

        private int _simulatorWallCount;
        private bool _beforeFirstEnemy = true;
        private bool _simulatorVelocityBoostActive;
        private int _brutalKills;

        private bool _subscribedHitEnemy;
        private bool _subscribedStopped;

        public int BrutalKills => _brutalKills;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (owner == null) return;

            if (_subscribedHitEnemy && _owner != null && _owner != owner)
                _owner.OnHitEnemy -= HandleOwnerHitEnemy;
            if (_subscribedStopped && _owner != null && _owner != owner)
                _owner.OnStopped -= HandleOwnerStopped;

            _owner = owner;
            _turnManager = turnManager;
            _runtime = _owner.GetComponent<CharacterPassiveRuntime>();

            if (!_subscribedHitEnemy)
            {
                _owner.OnHitEnemy += HandleOwnerHitEnemy;
                _subscribedHitEnemy = true;
            }
            if (!_subscribedStopped)
            {
                _owner.OnStopped += HandleOwnerStopped;
                _subscribedStopped = true;
            }
        }

        public void AddBrutalKill()
        {
            _brutalKills = Mathf.Clamp(_brutalKills + 1, 0, 5);
        }

        public void RegisterWallBounce()
        {
            if (!IsAtkSpecActive()) return;
            if (!_beforeFirstEnemy) return;

            _simulatorWallCount = Mathf.Clamp(_simulatorWallCount + 1, 0, 99);
            SyncSimulatorAtkBuff();
        }

        /// <summary>
        /// Hook appelé après le decay mur dans CharacterBall (pour P3 switch bonus vélocité).
        /// </summary>
        public void TryApplyPostWallVelocityBoost()
        {
            if (!IsAtkSpecActive()) return;
            if (!_simulatorVelocityBoostActive || !_beforeFirstEnemy) return;

            Rigidbody2D rb = _owner != null ? _owner.GetComponent<Rigidbody2D>() : null;
            if (rb == null) return;

            rb.velocity *= 1.15f;
        }

        public void ActivateSimulatorSwitchBoost()
        {
            if (!IsAtkSpecActive()) return;
            _simulatorVelocityBoostActive = true;
        }

        public void NotifyEnemyHit()
        {
            HandleOwnerHitEnemy();
        }

        public void ApplyBerserkSwitchBonus()
        {
            ApplyTempBuff(TempLfBuffId, BuffStatType.LaunchForce, 0.05f, true);
        }

        public void ApplyHerdSwitchBonus()
        {
            ApplyTempBuff(TempAtkBuffId, BuffStatType.ATK, 0.05f, true);
        }

        public void ApplyBrutalSwitchBonus()
        {
            ApplyTempBuff(TempAtkBuffId, BuffStatType.ATK, 0.10f, true);
            ApplyTempBuff(TempLfBuffId, BuffStatType.LaunchForce, 0.10f, true);
        }

        public void ApplyBerserkFullTeamBonus(bool shouldApply)
        {
            if (!IsAtkSpecActive()) return;

            if (!shouldApply)
            {
                if (_owner?.BuffReceiver != null)
                {
                    _owner.BuffReceiver.RemoveBuffsById(TempAtkBuffId);
                    _owner.BuffReceiver.RemoveBuffsById(TempDefBuffId);
                }
                return;
            }

            ApplyTempBuff(TempAtkBuffId, BuffStatType.ATK, 0.10f, true);
            ApplyTempBuff(TempDefBuffId, BuffStatType.DEF, 0.10f, true);
        }

        private void HandleOwnerHitEnemy()
        {
            _beforeFirstEnemy = false;
            _simulatorVelocityBoostActive = false;
            _simulatorWallCount = 0;
            SyncSimulatorAtkBuff();
        }

        private void HandleOwnerStopped()
        {
            _beforeFirstEnemy = true;
            _simulatorVelocityBoostActive = false;
            _simulatorWallCount = 0;
            SyncSimulatorAtkBuff();
        }

        private void SyncSimulatorAtkBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.RemoveBuffsById(SimulatorAtkBuffId);
            if (_simulatorWallCount <= 0) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = SimulatorAtkBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = _simulatorWallCount * 0.10f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void ApplyTempBuff(string buffId, BuffStatType statType, float value, bool isPercent)
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = _owner,
                StatType = statType,
                Value = value,
                IsPercent = isPercent,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private bool IsAtkSpecActive()
        {
            return _runtime != null && _runtime.CurrentSpecIndex == -1;
        }

        private void OnDestroy()
        {
            if (_owner != null && _subscribedHitEnemy)
                _owner.OnHitEnemy -= HandleOwnerHitEnemy;
            if (_owner != null && _subscribedStopped)
                _owner.OnStopped -= HandleOwnerStopped;
        }
    }
}

