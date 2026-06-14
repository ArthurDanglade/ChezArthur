using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;
using UnityEngine;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Guide lumineux » (lumino_light) : buff « Éclairé » sur l'allié touché (OnHitAlly)
    /// et bonus +3 % ATK/DEF équipe par boss vaincu (permanent, buff unique).
    /// </summary>
    public class LuminoLightHandler : ISpecialPassiveHandler
    {
        private const string BuffEclaireAtkId = "lumino_eclaire_atk";
        private const string BossAtkBuffId = "lumino_boss_atk";
        private const string BossDefBuffId = "lumino_boss_def";

        private static int s_subscriptionRefCount;
        private static int s_bossDefeatedCount;
        private static TurnManager s_turnManager;
        private static CharacterBall s_luminoOwner;

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitAlly == null) return;
            CharacterBall owner = context.Owner;
            if (owner == null) return;

            BuffReceiver br = context.HitAlly.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = BuffEclaireAtkId,
                Source = owner,
                StatType = BuffStatType.ATK,
                Value = 0.10f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureBossSubscription(context);
            ReapplyBossBuffs();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static void EnsureBossSubscription(PassiveContext context)
        {
            if (context.Owner == null) return;

            s_turnManager = context.TurnManager;
            s_luminoOwner = context.Owner;

            LuminoBossSubscriptionRelay relay = context.Owner.GetComponent<LuminoBossSubscriptionRelay>();
            if (relay != null) return;

            context.Owner.gameObject.AddComponent<LuminoBossSubscriptionRelay>();
            if (s_subscriptionRefCount == 0)
                Enemy.OnBossDefeated += OnBossDefeated;

            s_subscriptionRefCount++;
        }

        internal static void ReleaseBossSubscription()
        {
            s_subscriptionRefCount--;
            if (s_subscriptionRefCount > 0) return;

            s_subscriptionRefCount = 0;
            Enemy.OnBossDefeated -= OnBossDefeated;
        }

        private static void OnBossDefeated()
        {
            s_bossDefeatedCount++;
            Debug.Log($"[Passif] Lumino : boss vaincu → +{s_bossDefeatedCount * 3}% ATK/DEF équipe");
            ReapplyBossBuffs();
        }

        private static void ReapplyBossBuffs()
        {
            if (s_bossDefeatedCount <= 0 || s_turnManager == null || s_luminoOwner == null) return;

            float bonus = s_bossDefeatedCount * 0.03f;
            var allies = s_turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null) continue;

                BuffReceiver br = ally.BuffReceiver;
                br.AddBuff(new BuffData
                {
                    BuffId = BossAtkBuffId,
                    Source = s_luminoOwner,
                    StatType = BuffStatType.ATK,
                    Value = bonus,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });

                br.AddBuff(new BuffData
                {
                    BuffId = BossDefBuffId,
                    Source = s_luminoOwner,
                    StatType = BuffStatType.DEF,
                    Value = bonus,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = true
                });
            }
        }

        /// <summary>
        /// Relais de teardown : désabonne Lumino de <see cref="Enemy.OnBossDefeated"/> à la destruction du porteur.
        /// </summary>
        private sealed class LuminoBossSubscriptionRelay : MonoBehaviour
        {
            private void OnDestroy()
            {
                LuminoLightHandler.ReleaseBossSubscription();
            }
        }
    }
}
