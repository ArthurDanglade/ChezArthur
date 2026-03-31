using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Goat P4 - Brutal.
    /// +10% ATK par ennemi tué (persist run, cap +50%).
    /// Au switch sur spé ATK: +10% ATK et +10% LaunchForce (1 tour).
    /// </summary>
    public class GoatBrutalHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.AddBrutalKill();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK) return 0f;

            GoatSystem system = EnsureSystem(context);
            if (system == null) return 0f;

            return system.BrutalKills * 0.10f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            GoatSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyBrutalSwitchBonus();
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

