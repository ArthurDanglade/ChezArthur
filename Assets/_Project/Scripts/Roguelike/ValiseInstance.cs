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
        public bool IsAtMaxLevel => _currentLevel >= MAX_LEVEL;
        /// <summary> Taux accumulé par amélioration (hors stacks internes). </summary>
        public float AccumulatedValue => _accumulatedValue;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise une nouvelle valise active avec son état de base.
        /// </summary>
        public ValiseInstance(ValiseData data)
        {
            Data = data;
            // Niveau 0 = pré-obtention ; la 1ère amélioration à l'obtention le passe à 1.
            _currentLevel = 0;
            _internalStacks = 0;
            _isActive = true;
            _accumulatedValue = 0f;
            _accumulatedSecondValue = 0f;
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
        /// Valeur de stat principale projetée SI on appliquait une amélioration de cette rareté
        /// et qu'on avait ce nombre de stacks. Lecture seule, ne mute rien.
        /// </summary>
        public float PeekStatValueAfterImprovement(ValiseImprovementRarity rarity, int projectedStacks)
        {
            if (Data == null) return 0f;
            int rarityMult = ValiseTypeUtility.GetLevelBonus(rarity);
            float projectedAccumulated = _accumulatedValue + Data.BaseValuePerLevel * rarityMult;
            if (Data.IsScalingValise)
                return projectedAccumulated * projectedStacks;
            return projectedAccumulated;
        }

        /// <summary>
        /// Valeur de stat secondaire projetée après une amélioration de cette rareté. Lecture seule.
        /// </summary>
        public float PeekSecondStatValueAfterImprovement(ValiseImprovementRarity rarity, int projectedStacks)
        {
            if (Data == null || !Data.HasSecondStat) return 0f;
            int rarityMult = ValiseTypeUtility.GetLevelBonus(rarity);
            float projected = _accumulatedSecondValue + Data.SecondValuePerLevel * rarityMult;
            if (Data.IsScalingValise)
                return projected * projectedStacks;
            return projected;
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
            if (Data.IsScalingValise)
            {
                if (_valuePerLevelOverride >= 0f)
                    return _valuePerLevelOverride * _currentLevel * _internalStacks;
                return _accumulatedValue * _internalStacks;
            }

            if (_valuePerLevelOverride >= 0f)
                return _valuePerLevelOverride * _currentLevel;
            return _accumulatedValue;
        }

        /// <summary>
        /// Retourne la valeur totale de la stat secondaire si configurée.
        /// </summary>
        public float GetTotalSecondStatValue()
        {
            if (!Data.HasSecondStat) return 0f;
            if (Data.IsScalingValise)
            {
                if (_valuePerLevelOverride >= 0f)
                    return _valuePerLevelOverride * _currentLevel * _internalStacks;
                return _accumulatedSecondValue * _internalStacks;
            }

            if (_valuePerLevelOverride >= 0f)
                return _valuePerLevelOverride * _currentLevel;
            return _accumulatedSecondValue;
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
