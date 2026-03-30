using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Flash » (lumino_blind) : aveugle un ennemi (OnHitEnemy). Un seul aveuglé à la fois ; au 3e ennemi distinct
    /// aveuglé dans l'étage, « Flash général » (-15 % ATK/DEF sur tous les ennemis jusqu'à la fin de l'étage).
    /// V1 : <see cref="StunSystem"/> pour skip de tour (pas encore de tir aléatoire).
    /// </summary>
    /// <remarks>
    /// Si l'ancienne cible avait aussi un stun Elfert, RemoveStunFromEnemy retire tout stun sur cet ennemi (limitation V1).
    /// </remarks>
    public class LuminoBlindHandler : ISpecialPassiveHandler
    {
        private const string BuffBlindId = "lumino_blind";
        private const string FlashAtkId = "lumino_flash_atk";
        private const string FlashDefId = "lumino_flash_def";

        private static readonly HashSet<Enemy> UniqueBlindedThisStage = new HashSet<Enemy>();
        private static bool _flashTriggeredThisStage;
        private static Enemy _currentLuminoBlindTarget;

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitEnemy == null) return;
            CharacterBall owner = context.Owner;
            if (owner == null) return;

            TurnManager tm = context.TurnManager;
            if (tm == null) return;

            Enemy hit = context.HitEnemy;
            if (hit.IsDead) return;

            if (_currentLuminoBlindTarget != null && _currentLuminoBlindTarget != hit && StunSystem.Instance != null)
                StunSystem.Instance.RemoveStunFromEnemy(_currentLuminoBlindTarget);

            RemoveBlindBuffFromAllEnemies(tm);

            if (StunSystem.Instance != null)
                StunSystem.Instance.StunEnemy(hit, owner);

            BuffReceiver hitBr = hit.BuffReceiver;
            if (hitBr != null)
            {
                hitBr.AddBuff(new BuffData
                {
                    BuffId = BuffBlindId,
                    Source = owner,
                    StatType = BuffStatType.Speed,
                    Value = 0f,
                    IsPercent = false,
                    RemainingTurns = 1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }

            _currentLuminoBlindTarget = hit;

            bool newlyCounted = UniqueBlindedThisStage.Add(hit);
            if (newlyCounted && UniqueBlindedThisStage.Count >= 3 && !_flashTriggeredThisStage)
            {
                _flashTriggeredThisStage = true;
                ApplyFlashGeneral(tm);
            }
        }

        private static void RemoveBlindBuffFromAllEnemies(TurnManager turnManager)
        {
            IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
            if (participants == null) return;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;

                Enemy enemy = p as Enemy;
                if (enemy == null) continue;

                BuffReceiver br = enemy.BuffReceiver;
                if (br != null)
                    br.RemoveBuffsById(BuffBlindId);
            }
        }

        private static void ApplyFlashGeneral(TurnManager turnManager)
        {
            IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
            if (participants == null) return;

            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant p = participants[i];
                if (p == null || p.IsAlly || p.IsDead) continue;

                Enemy enemy = p as Enemy;
                if (enemy == null) continue;

                BuffReceiver br = enemy.BuffReceiver;
                if (br == null) continue;

                br.AddBuff(new BuffData
                {
                    BuffId = FlashAtkId,
                    Source = null,
                    StatType = BuffStatType.ATK,
                    Value = -0.15f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });

                br.AddBuff(new BuffData
                {
                    BuffId = FlashDefId,
                    Source = null,
                    StatType = BuffStatType.DEF,
                    Value = -0.15f,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            UniqueBlindedThisStage.Clear();
            _flashTriggeredThisStage = false;
            _currentLuminoBlindTarget = null;
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
