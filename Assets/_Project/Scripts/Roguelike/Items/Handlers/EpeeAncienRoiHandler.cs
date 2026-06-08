using System.Collections.Generic;
using ChezArthur.Core;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Marque un allié en mode fantôme pour un dernier tour.
    /// </summary>
    public class EpeeAncienRoiHandler : IItemEffectHandler
    {
        private readonly HashSet<int> _ghostAllies = new HashSet<int>();

        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null) return;
            if (context.Trigger != ItemTrigger.OnAllyDeath) return;
            if (context.SourceAlly == null) return;

            int allyId = context.SourceAlly.GetInstanceID();
            if (_ghostAllies.Contains(allyId))
            {
                _ghostAllies.Remove(allyId);
                return;
            }

            _ghostAllies.Add(allyId);
            if (RunManager.Instance != null)
                RunManager.Instance.RequestGhostTurn(context.SourceAlly);
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item)
        {
            _ghostAllies.Clear();
        }

        public void OnRunStart(ItemEffectContext context, ItemInstance item)
        {
            _ghostAllies.Clear();
        }
    }
}
