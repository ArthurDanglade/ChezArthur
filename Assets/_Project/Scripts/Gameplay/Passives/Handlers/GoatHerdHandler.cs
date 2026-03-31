using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Goat P2 - Herd Pressure.
    /// +15% ATK par ennemi vivant.
    /// Au switch sur spé ATK: +5% ATK (1 tour).
    /// </summary>
    public class GoatHerdHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK) return 0f;
            if (context.TurnManager == null) return 0f;

            int aliveEnemies = 0;
            var participants = context.TurnManager.Participants;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    var participant = participants[i];
                    if (participant == null || participant.IsAlly || participant.IsDead) continue;
                    aliveEnemies++;
                }
            }

            if (aliveEnemies == 1)
                return 0.50f;

            return aliveEnemies * 0.15f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyHerdSwitchBonus();
        }

        private static GoatSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            GoatSystem system = context.Owner.GetComponent<GoatSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<GoatSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

