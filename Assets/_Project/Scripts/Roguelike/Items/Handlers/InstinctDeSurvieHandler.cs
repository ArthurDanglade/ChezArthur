using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Donne un bonus d'ATK à un allié quand il passe sous un seuil de vie.
    /// </summary>
    public class InstinctDeSurvieHandler : IItemEffectHandler
    {
        private const float HP_THRESHOLD = 0.20f;
        private readonly HashSet<int> _triggeredAlliesThisStage = new HashSet<int>();

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyTakeDamage) return;
            if (context.SourceAlly == null) return;

            int allyId = context.SourceAlly.GetInstanceID();
            if (_triggeredAlliesThisStage.Contains(allyId)) return;

            float ratio = context.SourceAlly.MaxHp > 0
                ? (float)context.SourceAlly.CurrentHp / context.SourceAlly.MaxHp
                : 1f;
            if (ratio >= HP_THRESHOLD) return;

            if (context.SourceAlly.BuffReceiver == null) return;

            _triggeredAlliesThisStage.Add(allyId);
            context.SourceAlly.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = "instinct_survie_atk",
                Source = context.SourceAlly,
                StatType = BuffStatType.ATK,
                Value = item.Data.MainValue,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = false
            });
            Debug.Log($"[Item] {item.Data.ItemName} : +{Mathf.RoundToInt(item.Data.MainValue * 100f)}% ATK à {context.SourceAlly.Name} (<20% PV)");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            _triggeredAlliesThisStage.Clear();

            if (context == null || context.TurnManager == null) return;
            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.BuffReceiver == null) continue;
                ally.BuffReceiver.RemoveBuffsById("instinct_survie_atk");
            }
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item)
        {
            _triggeredAlliesThisStage.Clear();
        }
    }
}
