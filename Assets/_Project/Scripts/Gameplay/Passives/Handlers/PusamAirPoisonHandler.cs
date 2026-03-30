using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Handler pour « No Déo » (pusamair_poison).
    /// Empoisonne les ennemis au contact. Le DOT est géré par <see cref="PoisonTickSystem"/>.
    /// </summary>
    /// <remarks>
    /// Le PassiveData doit utiliser le trigger <see cref="PassiveTrigger.OnHitEnemy"/> (pas Permanent).
    /// Prévoir un MaxStacks élevé sur l'asset (ex. 99) pour que chaque touche ennemie appelle encore TryTrigger.
    /// </remarks>
    public class PusamAirPoisonHandler : ISpecialPassiveHandler
    {
        private const string PoisonBuffId = "pusamair_poison";

        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (context.HitEnemy == null) return;

            BuffReceiver enemyBuffReceiver = context.HitEnemy.BuffReceiver;
            if (enemyBuffReceiver == null) return;

            if (enemyBuffReceiver.HasBuff(PoisonBuffId)) return;

            var poisonBuff = new BuffData
            {
                BuffId = PoisonBuffId,
                Source = context.Owner,
                StatType = BuffStatType.DamageAmplification,
                Value = 0f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            };
            enemyBuffReceiver.AddBuff(poisonBuff);
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            return 0f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance) { }
    }
}
