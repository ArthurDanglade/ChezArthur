using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime Anty Cype :
    /// suivi des ennemis scannés, bonus DEF cumulés, effet full-scan,
    /// et mode "dossier partagé" (allié scanner consommable).
    /// </summary>
    public class AntyCypeScanSystem : MonoBehaviour
    {
        private const string ScanDebuffId = "antycype_scan";
        private const string ScannerBuffId = "antycype_scanner";
        private const string OwnerDefBuffId = "antycype_def";
        private const string FullScanDefBuffId = "antycype_fullscan_def";
        private const string FullScanDmgReductionBuffId = "antycype_fullscan_dmgred";
        private const string FullScanTeamAtkBuffId = "antycype_fullscan_atk";

        private static AntyCypeScanSystem _instance;
        public static AntyCypeScanSystem Instance => _instance;

        private CharacterBall _owner;
        private TurnManager _turnManager;
        private bool _enhanced;
        private readonly HashSet<Enemy> _scannedEnemies = new HashSet<Enemy>();
        private bool _fullScanTriggered;

        public void Initialize(CharacterBall owner, TurnManager turnManager)
        {
            if (_instance != null && _instance != this)
                _instance = this;
            else if (_instance == null)
                _instance = this;

            _owner = owner;
            _turnManager = turnManager;
        }

        public void SetEnhanced(bool value)
        {
            _enhanced = value;
        }

        public void ScanEnemy(Enemy enemy)
        {
            if (_owner == null || _turnManager == null) return;
            if (enemy == null || enemy.IsDead) return;
            if (_scannedEnemies.Contains(enemy)) return;

            _scannedEnemies.Add(enemy);

            if (enemy.BuffReceiver != null)
            {
                enemy.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = ScanDebuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            RefreshOwnerDefBuff();
            TryTriggerFullScan();
        }

        public void MarkAllyAsScanner(CharacterBall ally)
        {
            if (!_enhanced || _owner == null || ally == null || ally.IsDead) return;
            if (ally.BuffReceiver == null) return;

            ally.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = ScannerBuffId,
                Source = _owner,
                StatType = BuffStatType.ATK,
                Value = 0f,
                IsPercent = false,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public void TryAllyScan(CharacterBall ally, Enemy enemy)
        {
            if (!_enhanced || ally == null || enemy == null) return;
            if (ally.BuffReceiver == null) return;
            if (!ally.BuffReceiver.HasBuff(ScannerBuffId)) return;

            ScanEnemy(enemy);
            ally.BuffReceiver.RemoveBuffsById(ScannerBuffId);
        }

        public void ResetForStage()
        {
            _scannedEnemies.Clear();
            _fullScanTriggered = false;

            if (_owner != null && _owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.RemoveBuffsById(OwnerDefBuffId);
                _owner.BuffReceiver.RemoveBuffsById(FullScanDefBuffId);
            }

            if (_turnManager != null)
            {
                var participants = _turnManager.Participants;
                for (int i = 0; i < participants.Count; i++)
                {
                    if (participants[i] is Enemy e && e.BuffReceiver != null)
                    {
                        e.BuffReceiver.RemoveBuffsById(ScanDebuffId);
                        e.BuffReceiver.RemoveBuffsById(FullScanDmgReductionBuffId);
                    }
                }

                var allies = _turnManager.GetAllies();
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        CharacterBall ally = allies[i];
                        if (ally == null || ally.BuffReceiver == null) continue;
                        ally.BuffReceiver.RemoveBuffsById(ScannerBuffId);
                        ally.BuffReceiver.RemoveBuffsById(FullScanTeamAtkBuffId);
                    }
                }
            }
        }

        private void RefreshOwnerDefBuff()
        {
            if (_owner == null || _owner.BuffReceiver == null) return;

            float defBonus = _scannedEnemies.Count * 0.10f;
            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = OwnerDefBuffId,
                Source = _owner,
                StatType = BuffStatType.DEF,
                Value = defBonus,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void TryTriggerFullScan()
        {
            if (_fullScanTriggered || _turnManager == null || _owner == null) return;

            int livingEnemies = 0;
            int livingScanned = 0;

            var participants = _turnManager.Participants;
            for (int i = 0; i < participants.Count; i++)
            {
                Enemy enemy = participants[i] as Enemy;
                if (enemy == null || enemy.IsDead) continue;

                livingEnemies++;
                if (_scannedEnemies.Contains(enemy))
                    livingScanned++;
            }

            if (livingEnemies == 0 || livingScanned < livingEnemies) return;

            _fullScanTriggered = true;

            if (_owner.BuffReceiver != null)
            {
                _owner.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = FullScanDefBuffId,
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

            for (int i = 0; i < participants.Count; i++)
            {
                Enemy enemy = participants[i] as Enemy;
                if (enemy == null || enemy.IsDead || enemy.BuffReceiver == null) continue;

                enemy.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = FullScanDmgReductionBuffId,
                    Source = _owner,
                    StatType = BuffStatType.ATK,
                    Value = -0.15f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            if (_enhanced)
            {
                var allies = _turnManager.GetAllies();
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        CharacterBall ally = allies[i];
                        if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;
                        ally.BuffReceiver.AddBuff(new BuffData
                        {
                            BuffId = FullScanTeamAtkBuffId,
                            Source = _owner,
                            StatType = BuffStatType.ATK,
                            Value = 0.10f,
                            IsPercent = true,
                            RemainingTurns = -1,
                            RemainingCycles = -1,
                            UniquePerSource = false,
                            UniqueGlobal = true
                        });
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}

