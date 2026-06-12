using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Renvoie une partie des dégâts reçus à l'ennemi attaquant courant.
    /// </summary>
    public class BouclierAPiqueHandler : IItemEffectHandler
    {
        public void OnTriggered(ItemEffectContext context, ItemInstance item)
        {
            if (context == null || item == null || item.Data == null) return;
            if (context.Trigger != ItemTrigger.OnAllyTakeDamage) return;
            if (context.SourceAlly == null) return;
            if (context.TurnManager == null) return;

            int degatsRecus = context.DamageAmount;
            int renvoi = Mathf.RoundToInt(degatsRecus * item.Data.MainValue);
            if (renvoi <= 0) return;

            ITurnParticipant current = context.TurnManager.CurrentParticipant;
            if (current == null || current.IsAlly) return;

            Enemy attacker = current as Enemy;
            if (attacker == null || attacker.IsDead) return;
            attacker.TakePureDamage(renvoi);
            Debug.Log($"[Item] {item.Data.ItemName} : {degatsRecus} reçus → {renvoi} renvoyés");
        }

        public void OnStageStart(ItemEffectContext context, ItemInstance item) { }

        public void OnRunStart(ItemEffectContext context, ItemInstance item) { }
    }
}
