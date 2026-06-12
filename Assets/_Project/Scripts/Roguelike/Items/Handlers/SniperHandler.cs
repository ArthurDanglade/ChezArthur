using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Vulnérabilité +25 % au début d'étage tant que l'ennemi reste au-dessus de 90 % PV.
    /// </summary>
    public class SniperHandler : IItemEffectHandler
    {
        private const string BuffId = "sniper_vulnerability";
        private const float HP_THRESHOLD = 0.90f;

        private readonly Dictionary<Enemy, Action<int>> _hpWatchers = new Dictionary<Enemy, Action<int>>();

        public void OnTriggered(ItemEffectContext context, ItemInstance item) { }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            UnsubscribeAll();
            if (context == null || item == null || item.Data == null || context.TurnManager == null) return;

            IReadOnlyList<ITurnParticipant> participants = context.TurnManager.Participants;
            if (participants == null) return;

            for (int i = 0; i < participants.Count; i++)
            {
                if (participants[i] == null || participants[i].IsAlly || participants[i].IsDead) continue;
                Enemy enemy = participants[i] as Enemy;
                if (enemy == null) continue;

                ApplyVulnerability(enemy, item);
                SubscribeHpWatch(enemy, item);
            }
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item)
        {
            UnsubscribeAll();
        }

        private void ApplyVulnerability(Enemy enemy, ItemInstance item)
        {
            if (enemy.IsDead || enemy.BuffReceiver == null) return;

            enemy.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BuffId,
                Source = null,
                StatType = BuffStatType.DamageAmplification,
                Value = item.Data.MainValue,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
            Debug.Log($"[Item] {item.Data.ItemName} : vulnérabilité posée sur {enemy.Name} (+{Mathf.RoundToInt(item.Data.MainValue * 100f)}%)");
        }

        private void SubscribeHpWatch(Enemy enemy, ItemInstance item)
        {
            if (_hpWatchers.ContainsKey(enemy)) return;

            Action<int> handler = (_) => OnEnemyHpChanged(enemy, item);
            _hpWatchers[enemy] = handler;
            enemy.OnDamaged += handler;
        }

        private void OnEnemyHpChanged(Enemy enemy, ItemInstance item)
        {
            if (enemy == null || enemy.IsDead || item == null || item.Data == null) return;
            if (enemy.BuffReceiver == null || !enemy.BuffReceiver.HasBuff(BuffId)) return;

            float ratio = enemy.MaxHp > 0 ? (float)enemy.CurrentHp / enemy.MaxHp : 0f;
            if (ratio > HP_THRESHOLD) return;

            enemy.BuffReceiver.RemoveBuffsById(BuffId);
            Debug.Log($"[Item] {item.Data.ItemName} : vulnérabilité retirée de {enemy.Name} ({Mathf.RoundToInt(ratio * 100f)}% PV)");
        }

        private void UnsubscribeAll()
        {
            foreach (KeyValuePair<Enemy, Action<int>> pair in _hpWatchers)
            {
                if (pair.Key != null && pair.Value != null)
                    pair.Key.OnDamaged -= pair.Value;
            }
            _hpWatchers.Clear();
        }
    }
}
