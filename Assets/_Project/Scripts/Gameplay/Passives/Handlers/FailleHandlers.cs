using ChezArthur.Characters;
using ChezArthur.Gameplay.Passives;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Helper commun : assure FailleSystem sur le porteur.
    /// </summary>
    internal static class FailleSystemHelper
    {
        public static FailleSystem Ensure(PassiveContext context)
        {
            if (context.Owner == null) return null;

            FailleSystem system = context.Owner.GetComponent<FailleSystem>();
            if (system == null)
                system = context.Owner.gameObject.AddComponent<FailleSystem>();

            system.Initialize(context.Owner, context.TurnManager);
            return system;
        }
    }

    /// <summary>
    /// ATK P1 — Portails + dégâts aux ennemis qui traversent (5 % ATK Faille).
    /// </summary>
    public class FailleAtkPortalsHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkEnemyDamage();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s == null) return;
            s.EnableAtkEnemyDamage();
            s.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null)
            {
                s.EnableAtkEnemyDamage();
                s.OnSpecSwitched();
            }
        }
    }

    /// <summary>
    /// ATK P10 — +10 % ATK par traversée (max 10), stacks persistants, effet si spé ATK active.
    /// </summary>
    public class FailleAtkStacksHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkStacks();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            if (passiveData.Effect != PassiveEffect.BuffATK) return 0f;
            FailleSystem s = FailleSystemHelper.Ensure(context);
            return s != null ? s.GetTraverseStackAtkBonus() : 0f;
        }

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkStacks();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkStacks();
        }
    }

    /// <summary>
    /// ATK P15 — Repropulsion de Faille + ATK +20 % + double dégâts si Super.
    /// </summary>
    public class FailleAtkReboostHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkReboost();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkReboost();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableAtkReboost();
        }
    }

    /// <summary>
    /// SUP P1 — Portails + soin allié 5 % HP max à la traversée.
    /// </summary>
    public class FailleSupPortalsHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableSupAllyHeal();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s == null) return;
            s.EnableSupAllyHeal();
            s.ResetForStage();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null)
            {
                s.EnableSupAllyHeal();
                s.OnSpecSwitched();
            }
        }
    }

    /// <summary>
    /// SUP P10 — Buff équipe quand tous les alliés ont traversé + stacks Faille.
    /// </summary>
    public class FailleSupTeamHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableSupTeamBuff();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableSupTeamBuff();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null)
            {
                s.EnableSupTeamBuff();
                s.OnSpecSwitched();
            }
        }
    }

    /// <summary>
    /// SUP P15 — Repropulsion allié + ATK +20 % + double dégâts si Super.
    /// </summary>
    public class FailleSupReboostHandler : ISpecialPassiveHandler
    {
        public void OnTriggered(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableSupReboost();
        }

        public float GetStatBonus(PassiveContext context, PassiveData passiveData, PassiveInstance instance) => 0f;

        public void OnStageStart(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableSupReboost();
        }

        public void OnSpecSwitch(PassiveContext context, PassiveData passiveData, PassiveInstance instance)
        {
            FailleSystem s = FailleSystemHelper.Ensure(context);
            if (s != null) s.EnableSupReboost();
        }
    }
}
