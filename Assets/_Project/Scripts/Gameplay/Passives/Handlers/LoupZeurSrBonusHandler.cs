using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Handler pour « Bande de loosers » (loupzeur_sr_bonus).
    /// ATK +15 % par allié SR vivant (hors Loup Zeur). +10 % si toute l'équipe vivante est SR (Loup Zeur compris).
    /// Passif Permanent : logique uniquement dans GetStatBonus.
    /// </summary>
    public class LoupZeurSrBonusHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            // Passif Permanent — jamais invoqué via TryTrigger.
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null || context.TurnManager == null) return 0f;

            IReadOnlyList<CharacterBall> allies = context.TurnManager.GetAllies();
            if (allies == null) return 0f;

            int otherSrAlive = 0;
            bool everyAliveIsSr = true;
            int aliveCount = 0;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                aliveCount++;
                CharacterData data = ally.Data;
                if (data == null) { everyAliveIsSr = false; continue; }

                if (data.Rarity != CharacterRarity.SR)
                    everyAliveIsSr = false;

                if (ally != context.Owner && data.Rarity == CharacterRarity.SR)
                    otherSrAlive++;
            }

            float bonus = otherSrAlive * passiveData.Value;

            if (everyAliveIsSr && aliveCount > 0)
                bonus += 0.10f;

            return bonus;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
