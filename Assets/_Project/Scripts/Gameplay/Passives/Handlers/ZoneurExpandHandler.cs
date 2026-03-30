using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Hors-jeu interdit » (zoneur_zone_expand) : agrandit la zone et active le buff de passage.
    /// </summary>
    public class ZoneurExpandHandler : ISpecialPassiveHandler
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

            zone.ExpandZone();
            zone.ResetCycleTracking();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
