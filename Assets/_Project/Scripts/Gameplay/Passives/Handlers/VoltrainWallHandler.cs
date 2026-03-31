using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Mur sous tension » (voltrain_wall).
    /// Le point de contact mur est récupéré dans CharacterBall.OnCollisionEnter2D,
    /// puis transmis à ElectricWallSystem.RecordWallHit.
    /// </summary>
    public class VoltrainWallHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            ElectricWallSystem system = EnsureSystem(context);
            if (system != null)
                system.ClearElectricZone();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static ElectricWallSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            ElectricWallSystem system = context.Owner.GetComponent<ElectricWallSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<ElectricWallSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

