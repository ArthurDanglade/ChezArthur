using ChezArthur.Characters;
using UnityEngine;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Pardon pour la traînée » (kram_fire_trail) : lance / relance la traînée de feu de Kram Hoisi.
    /// </summary>
    /// <remarks>
    /// La traînée est recalculée à chaque lancement (segments nettoyés avant StartTrail).
    /// </remarks>
    public class KramFireTrailHandler : ISpecialPassiveHandler
    {
        private const float BaseRadiusDummy = 0f; // placeholder (pas de paramètre dans V1)

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            CharacterBall owner = context.Owner;

            FireTrailSystem fts = owner.GetComponent<FireTrailSystem>();
            if (fts == null)
            {
                fts = owner.gameObject.AddComponent<FireTrailSystem>();
                if (context.TurnManager != null)
                    fts.Initialize(owner, context.TurnManager);
            }

            if (context.TurnManager != null && fts != null)
                fts.Initialize(owner, context.TurnManager);

            if (fts != null)
            {
                fts.ClearTrail();
                fts.StartTrail();
            }
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;

            FireTrailSystem fts = context.Owner.GetComponent<FireTrailSystem>();
            if (fts != null)
                fts.ClearTrail();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

