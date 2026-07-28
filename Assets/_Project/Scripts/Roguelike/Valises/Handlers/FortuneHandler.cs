namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Fortune : stacks selon total de Tals (seuil 50, ou 40 en lv20).
    /// </summary>
    public class FortuneHandler : IValiseEffectHandler
    {
        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || valise == null || ValiseManager.Instance == null) return;
            if (context.Trigger != ValiseTrigger.OnTalsChanged) return;

            int threshold = valise.IsLevel20Unlocked ? 40 : 50;
            int targetStacks = threshold > 0 ? context.IntValue / threshold : 0;
            ValiseManager.Instance.SyncStacksToTarget("valise_fortune", targetStacks);
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise) { }
    }
}
