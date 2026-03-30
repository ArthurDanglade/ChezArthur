using ChezArthur.Characters;
using UnityEngine;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Tout brûle, tout flambe » (kram_fire_enhance) : active le mode niveau 10 sur la traînée.
    /// </summary>
    public class KramFireEnhanceHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
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

            if (fts != null)
            {
                // La traînée doit être en mode « enhanced » avant le prochain lancement.
                fts.SetEnhanced(true);
            }
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

