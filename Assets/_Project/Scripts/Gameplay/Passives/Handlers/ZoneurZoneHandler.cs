using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Surface de réparation » (zoneur_zone) : crée / met à jour <see cref="ZoneSystem"/> sur Zoneur.
    /// </summary>
    public class ZoneurZoneHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            ZoneSystem zone = context.Owner.GetComponent<ZoneSystem>();
            if (zone == null)
            {
                zone = context.Owner.gameObject.AddComponent<ZoneSystem>();
                zone.Initialize(context.Owner, context.TurnManager, 2f);
            }
            else
                zone.Initialize(context.Owner, context.TurnManager, 2f);

            zone.ResetCycleTracking();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
