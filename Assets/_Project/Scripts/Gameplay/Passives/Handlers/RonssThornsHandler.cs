using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// « Ronces protectrices » (ronss_thorns) :
    /// quand Ronss touche un allié, applique une protection 2 tours (DR 30%).
    /// Un seul allié protégé à la fois par Ronss.
    /// </summary>
    public class RonssThornsHandler : ISpecialPassiveHandler
    {
        private const string ThornsBuffId = "ronss_thorns";

        // Clé = Ronss (owner), valeur = allié actuellement protégé.
        private readonly Dictionary<CharacterBall, CharacterBall> _currentProtectedByOwner = new Dictionary<CharacterBall, CharacterBall>();

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null || context.HitAlly == null) return;

            CharacterBall owner = context.Owner;
            CharacterBall newProtected = context.HitAlly;

            if (_currentProtectedByOwner.TryGetValue(owner, out CharacterBall oldProtected))
            {
                if (oldProtected != null && oldProtected != newProtected)
                {
                    BuffReceiver oldBr = oldProtected.BuffReceiver;
                    if (oldBr != null)
                        oldBr.RemoveBuffsById(ThornsBuffId);
                }
            }

            _currentProtectedByOwner[owner] = newProtected;

            BuffReceiver br = newProtected.BuffReceiver;
            if (br == null) return;

            br.AddBuff(new BuffData
            {
                BuffId = ThornsBuffId,
                Source = owner,
                StatType = BuffStatType.DamageReduction,
                Value = 0.30f,
                IsPercent = true,
                RemainingTurns = 2,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = true
            });
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.Owner == null) return;
            _currentProtectedByOwner.Remove(context.Owner);
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}

