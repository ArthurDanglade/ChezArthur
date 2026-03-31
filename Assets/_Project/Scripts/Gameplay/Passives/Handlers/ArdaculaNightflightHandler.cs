using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P3 ATK - Vol de nuit : +15% LaunchForce et 5 rebonds sans perte de vitesse.
    /// </summary>
    public class ArdaculaNightflightHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffLaunchForce) return 0f;
            return 0.15f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ArdaculaSystem system = EnsureSystem(context);
            if (system != null)
                system.ApplyNightflightSwitchBonus();
        }

        private static ArdaculaSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            ArdaculaSystem system = context.Owner.GetComponent<ArdaculaSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<ArdaculaSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

