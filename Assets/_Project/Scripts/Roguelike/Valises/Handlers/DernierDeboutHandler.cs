using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Dernier Debout : stacks = alliés spawnés puis morts ; override valeur lv20.
    /// </summary>
    public class DernierDeboutHandler : IValiseEffectHandler
    {
        private const float Lv20ValuePerLevel = 0.04f;

        private int _deadSpawnedAlliesCount;

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || ValiseManager.Instance == null) return;

            if (context.Trigger == ValiseTrigger.OnAllyDeath)
            {
                _deadSpawnedAlliesCount++;
                ValiseManager.Instance.SyncStacksToTarget("valise_dernier_debout", _deadSpawnedAlliesCount);
                Debug.Log($"[Valise] Dernier Debout stacks: {_deadSpawnedAlliesCount}");
                return;
            }

            if (context.Trigger == ValiseTrigger.OnValiseChanged)
            {
                ValiseManager.Instance.SyncStacksToTarget("valise_dernier_debout", _deadSpawnedAlliesCount);
                ApplyLv20Override(valise);
            }
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise) { }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise)
        {
            _deadSpawnedAlliesCount = 0;

            ValiseInstance active = valise;
            if (active == null && ValiseManager.Instance != null)
                active = ValiseManager.Instance.GetActiveValise("valise_dernier_debout");
            ApplyLv20Override(active);
        }

        private static void ApplyLv20Override(ValiseInstance valise)
        {
            if (valise == null) return;

            if (valise.IsLevel20Unlocked)
                valise.SetValuePerLevelOverride(Lv20ValuePerLevel);
            else
                valise.ClearValuePerLevelOverride();
        }
    }
}
