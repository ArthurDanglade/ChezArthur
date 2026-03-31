using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// P3 ATK - Dernier mot : ATK dynamique selon alliés vivants.
    /// </summary>
    public class AncienAtkLaststandHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK) return 0f;
            if (context.TurnManager == null) return 0f;

            int aliveAllies = 0;
            var participants = context.TurnManager.Participants;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    var p = participants[i];
                    if (p == null || !p.IsAlly || p.IsDead) continue;
                    if (ReferenceEquals(p, context.Owner)) continue;
                    aliveAllies++;
                }
            }

            if (aliveAllies <= 0)
                return 0.80f;

            return aliveAllies * 0.20f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            EnsureSystem(context);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        private static AncienSystem EnsureSystem(PassiveContext context)
        {
            if (context.Owner == null) return null;

            AncienSystem system = context.Owner.GetComponent<AncienSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<AncienSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }
}

