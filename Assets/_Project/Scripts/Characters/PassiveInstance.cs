namespace ChezArthur.Characters
{
    /// <summary>
    /// Instance runtime d'un passif : gère les stacks et le déclenchement pendant la run.
    /// Classe pure (pas de MonoBehaviour), légère et sans allocation.
    /// </summary>
    public class PassiveInstance
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly PassiveData _data;
        private int _currentStacks;
        private bool _isMaxed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public PassiveData Data => _data;
        public int CurrentStacks => _currentStacks;
        public bool IsMaxed => _isMaxed;
        public float TotalValue => _data != null ? _data.Value * _currentStacks : 0f;

        // ═══════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════
        public PassiveInstance(PassiveData data)
        {
            _data = data;
            _currentStacks = 0;
            _isMaxed = false;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Tente de déclencher le passif. Retourne true si un stack a été ajouté.
        /// </summary>
        public bool TryTrigger(PassiveTrigger trigger)
        {
            if (_data == null) return false;
            if (_data.Trigger != trigger) return false;
            if (_data.Trigger == PassiveTrigger.Permanent) return false;
            if (_isMaxed) return false;

            _currentStacks++;
            if (_currentStacks >= _data.MaxStacks)
                _isMaxed = true;
            return true;
        }

        /// <summary>
        /// Retourne le bonus de stat de ce passif (pour les effets buff). Permanent = pleine puissance.
        /// </summary>
        public float GetStatBonus()
        {
            if (_data == null) return 0f;

            if (_data.Trigger == PassiveTrigger.Permanent)
                return _data.Value * _data.MaxStacks;

            switch (_data.Effect)
            {
                case PassiveEffect.BuffATK:
                case PassiveEffect.BuffDEF:
                case PassiveEffect.BuffHP:
                case PassiveEffect.BuffSpeed:
                case PassiveEffect.BuffLaunchForce:
                case PassiveEffect.BuffTeamATK:
                case PassiveEffect.BuffTeamDEF:
                    return TotalValue;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Remet les stacks à zéro.
        /// </summary>
        public void ResetStacks()
        {
            _currentStacks = 0;
            _isMaxed = false;
        }

        /// <summary>
        /// True si les stacks doivent être reset à chaque nouvel étage.
        /// </summary>
        public bool ShouldResetOnNewStage()
        {
            return _data != null && _data.ResetRule == PassiveResetRule.ResetPerStage;
        }

        /// <summary>
        /// True si les stacks doivent être reset au changement de spé (tous les passifs reset au switch).
        /// </summary>
        public bool ShouldResetOnSpecSwitch()
        {
            return true;
        }
    }
}
