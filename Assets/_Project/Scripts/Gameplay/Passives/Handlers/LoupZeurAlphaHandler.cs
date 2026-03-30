using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Handler pour « Alpha ou pas » (loupzeur_alpha).
    /// Si aucun SSR/LR vivant dans l'équipe → +50 % (ATK ou DEF selon le PassiveData). Sinon → 0.
    /// Passif Permanent : logique uniquement dans GetStatBonus.
    /// </summary>
    public class LoupZeurAlphaHandler : ISpecialPassiveHandler
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

            bool hasSsrOrLrAlive = false;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                CharacterData data = ally.Data;
                if (data == null) continue;

                if (data.Rarity == CharacterRarity.SSR || data.Rarity == CharacterRarity.LR)
                {
                    hasSsrOrLrAlive = true;
                    break;
                }
            }

            if (!hasSsrOrLrAlive)
                return passiveData.Value;

            return 0f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
