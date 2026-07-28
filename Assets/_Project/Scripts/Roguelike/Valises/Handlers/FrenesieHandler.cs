using UnityEngine;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.UI;
using ChezArthur.Audio;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Valise Frénésie : stacks ATK par kill d'étage + MégaCrit (synergie Critique).
    /// </summary>
    public class FrenesieHandler : IValiseEffectHandler
    {
        private const float MegaCritChance = 0.20f;
        private const float MegaCritDamageMultiplier = 1f;
        private const float MegaCritLabelScale = 2.1f;

        private AudioClip _megaCritSfx;

        /// <summary>
        /// Injecte le SFX MégaCrit (assigné depuis le bridge / Inspector).
        /// </summary>
        public void SetMegaCritSfx(AudioClip clip)
        {
            _megaCritSfx = clip;
        }

        public void OnTriggered(ValiseEffectContext context, ValiseInstance valise)
        {
            if (context == null || ValiseManager.Instance == null) return;

            if (context.Trigger == ValiseTrigger.OnAllyKill)
            {
                ValiseManager.Instance.AddStackToValise("valise_frenesie");
                ValiseInstance frenesie = ValiseManager.Instance.GetActiveValise("valise_frenesie");
                if (frenesie != null)
                    Debug.Log($"[Valise] Frénésie stacks: {frenesie.InternalStacks}");
                return;
            }

            if (context.Trigger == ValiseTrigger.OnCriticalHit)
                TryMegaCrit(context);
        }

        public void OnStageStart(ValiseEffectContext context, ValiseInstance valise)
        {
            if (ValiseManager.Instance == null) return;
            ValiseManager.Instance.ResetStacksOnValise("valise_frenesie");
            Debug.Log("[Valise] Frénésie stacks: 0 (début d'étage)");
        }

        public void OnRunStart(ValiseEffectContext context, ValiseInstance valise) { }

        private void TryMegaCrit(ValiseEffectContext context)
        {
            SynergyManager synergyManager = SynergyManager.Instance;
            if (synergyManager == null || !synergyManager.IsSynergyActive("synergie_critique_frenesie"))
                return;

            Enemy enemy = context.TargetEnemy;
            int damage = context.DamageAmount;
            if (enemy == null || enemy.IsDead || damage <= 0)
                return;

            if (Random.value >= MegaCritChance)
                return;

            int bonusDamage = Mathf.Max(1, Mathf.RoundToInt(damage * MegaCritDamageMultiplier));
            Vector3 labelPos = enemy.transform.position;

            enemy.TakeDamage(bonusDamage, isCrit: false);

            if (enemy.IsDead)
                Debug.Log("[Valise] MégaCrit létale (kill non crédité à l'allié)");

            if (FloatingNumberSpawner.Instance != null)
                FloatingNumberSpawner.Instance.ShowLabel("MÉGACRIT !", UiTheme.Gold, labelPos, MegaCritLabelScale);

            if (SfxManager.Instance != null && _megaCritSfx != null)
                SfxManager.Instance.PlaySfx(_megaCritSfx);

            Debug.Log($"[Valise] MÉGACRIT ! {bonusDamage} dégâts bonus sur {enemy.name}");
        }
    }
}
