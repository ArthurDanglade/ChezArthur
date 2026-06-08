using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Représente l'état runtime d'une valise pendant une run.
    /// </summary>
    public class ValiseInstance
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int MIN_LEVEL = 1;
        private const int MAX_LEVEL = 99;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _currentLevel;
        private int _internalStacks;
        private bool _isActive;
        private float _accumulatedValue;
        private float _accumulatedSecondValue;
        private float _valuePerLevelOverride = -1f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public ValiseData Data { get; }
        public int CurrentLevel => _currentLevel;
        public int InternalStacks => _internalStacks;
        public bool IsActive => _isActive;
        public bool IsLevel20Unlocked => _currentLevel >= 20;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise une nouvelle valise active avec son état de base.
        /// </summary>
        public ValiseInstance(ValiseData data)
        {
            Data = data;
            _currentLevel = MIN_LEVEL;
            _internalStacks = 0;
            _isActive = true;
            _accumulatedValue = Data != null ? Data.BaseValuePerLevel : 0f;
            _accumulatedSecondValue = (Data != null && Data.HasSecondStat)
                ? Data.SecondValuePerLevel
                : 0f;
        }

        /// <summary>
        /// Applique une amélioration : +1 niveau visible, valeur pondérée par la rareté.
        /// </summary>
        public void AddImprovement(ValiseImprovementRarity rarity)
        {
            if (Data == null) return;

            _currentLevel = Mathf.Min(_currentLevel + 1, MAX_LEVEL);

            int rarityMultiplier = ValiseTypeUtility.GetLevelBonus(rarity);
            _accumulatedValue += Data.BaseValuePerLevel * rarityMultiplier;
            if (Data.HasSecondStat)
                _accumulatedSecondValue += Data.SecondValuePerLevel * rarityMultiplier;
        }

        /// <summary>
        /// Incrémente les stacks internes de scaling.
        /// </summary>
        public void AddStack()
        {
            _internalStacks++;
        }

        /// <summary>
        /// Réinitialise les stacks internes de scaling.
        /// </summary>
        public void ResetStacks()
        {
            _internalStacks = 0;
        }

        /// <summary>
        /// Définit si la valise est active dans un slot.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
        }

        /// <summary>
        /// Définit un override runtime de valeur par niveau.
        /// </summary>
        public void SetValuePerLevelOverride(float value)
        {
            _valuePerLevelOverride = value;
        }

        /// <summary>
        /// Supprime l'override runtime de valeur par niveau.
        /// </summary>
        public void ClearValuePerLevelOverride()
        {
            _valuePerLevelOverride = -1f;
        }

        /// <summary>
        /// Retourne la valeur totale de la stat principale.
        /// </summary>
        public float GetTotalStatValue()
        {
            float baseValue = _accumulatedValue;
            if (Data.IsScalingValise)
                baseValue += Data.BaseValuePerLevel * _internalStacks;

            if (_valuePerLevelOverride >= 0f)
            {
                baseValue = _valuePerLevelOverride * _currentLevel +
                    (Data.IsScalingValise ? _valuePerLevelOverride * _internalStacks : 0f);
            }

            return baseValue;
        }

        /// <summary>
        /// Retourne la valeur totale de la stat secondaire si configurée.
        /// </summary>
        public float GetTotalSecondStatValue()
        {
            if (!Data.HasSecondStat)
                return 0f;

            float secondValue = _accumulatedSecondValue;
            if (_valuePerLevelOverride >= 0f)
                secondValue = _valuePerLevelOverride * _currentLevel;
            return secondValue;
        }

        /// <summary>
        /// Retourne la valeur totale du downside si configuré.
        /// </summary>
        public float GetTotalDownsideValue()
        {
            if (!Data.HasDownside)
                return 0f;

            return Data.DownsideValuePerLevel * _currentLevel;
        }
    }
}
