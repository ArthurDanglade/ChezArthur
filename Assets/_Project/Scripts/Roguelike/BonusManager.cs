using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Gère les bonus actifs pendant une run. Stocke les bonus et fournit les modificateurs de stat ;
    /// les systèmes (CharacterBall, RunManager, etc.) interrogent le manager pour appliquer les bonus.
    /// </summary>
    public class BonusManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration")]
        [SerializeField] private int maxLegendaryBonuses = 1;
        [SerializeField] private int maxBrokenBonuses = 2;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<BonusData> _activeBonuses = new List<BonusData>();
        private Dictionary<BonusStatType, float> _cachedPercentModifiers = new Dictionary<BonusStatType, float>();
        private Dictionary<BonusStatType, float> _cachedFlatModifiers = new Dictionary<BonusStatType, float>();
        private bool _cacheValid;

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static BonusManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Liste des bonus actifs (lecture seule). </summary>
        public IReadOnlyList<BonusData> ActiveBonuses => _activeBonuses;

        /// <summary> Nombre de bonus actifs. </summary>
        public int ActiveBonusCount => _activeBonuses.Count;

        /// <summary> Nombre de bonus spéciaux légendaires actifs. </summary>
        public int LegendaryCount => GetSpecialRarityCount(SpecialBonusRarity.Legendary);

        /// <summary> Nombre de bonus spéciaux broken actifs. </summary>
        public int BrokenCount => GetSpecialRarityCount(SpecialBonusRarity.Broken);

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand un bonus est ajouté. </summary>
        public event Action<BonusData> OnBonusAdded;

        /// <summary> Déclenché quand tous les bonus sont réinitialisés. </summary>
        public event Action OnBonusesCleared;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Réinitialise la liste des bonus actifs et invalide le cache.
        /// </summary>
        public void Initialize()
        {
            _activeBonuses.Clear();
            InvalidateCache();
        }

        /// <summary>
        /// Ajoute un bonus si les limites (legendary/broken) sont respectées.
        /// </summary>
        /// <returns> True si le bonus a été ajouté, false si refusé (limite atteinte). </returns>
        public bool AddBonus(BonusData bonus)
        {
            if (bonus == null) return false;
            if (!CanAddBonus(bonus)) return false;

            _activeBonuses.Add(bonus);
            InvalidateCache();

            // Recalcule les HP de tous les alliés (au cas où le bonus modifie HP)
            CharacterBall[] allies = UnityEngine.Object.FindObjectsOfType<CharacterBall>();
            foreach (var ally in allies)
            {
                if (ally != null && !ally.IsDead)
                    ally.RecalculateHpAfterBonus();
            }

            OnBonusAdded?.Invoke(bonus);
            return true;
        }

        /// <summary>
        /// Retire un bonus de la liste et invalide le cache.
        /// </summary>
        public void RemoveBonus(BonusData bonus)
        {
            if (bonus == null) return;
            _activeBonuses.Remove(bonus);
            InvalidateCache();
        }

        /// <summary>
        /// Vide tous les bonus et déclenche OnBonusesCleared.
        /// </summary>
        public void ClearAllBonuses()
        {
            _activeBonuses.Clear();
            InvalidateCache();
            OnBonusesCleared?.Invoke();
        }

        /// <summary>
        /// Retourne les modificateurs cumulés pour un type de stat (percent et flat).
        /// Recalcule le cache si nécessaire.
        /// </summary>
        /// <param name="statType"> Type de stat. </param>
        /// <returns> percentMod : somme des % (ex. 0.25 pour +25%), flatMod : somme des valeurs flat. </returns>
        public (float percentMod, float flatMod) GetStatModifier(BonusStatType statType)
        {
            if (!_cacheValid)
                RecalculateCache();

            float percent = _cachedPercentModifiers.TryGetValue(statType, out float p) ? p : 0f;
            float flat = _cachedFlatModifiers.TryGetValue(statType, out float f) ? f : 0f;
            return (percent, flat);
        }

        /// <summary>
        /// Indique si un effet spécial est actif.
        /// </summary>
        public bool HasSpecialEffect(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return false;
            for (int i = 0; i < _activeBonuses.Count; i++)
            {
                if (_activeBonuses[i].IsSpecialBonus && _activeBonuses[i].SpecialEffectId == effectId)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Retourne la valeur de l'effet spécial (0 si non trouvé).
        /// </summary>
        public float GetSpecialEffectValue(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return 0f;
            for (int i = 0; i < _activeBonuses.Count; i++)
            {
                if (_activeBonuses[i].IsSpecialBonus && _activeBonuses[i].SpecialEffectId == effectId)
                    return _activeBonuses[i].SpecialValue;
            }
            return 0f;
        }

        /// <summary>
        /// Vérifie si le bonus peut être ajouté (limites legendary/broken pour les bonus spéciaux uniquement).
        /// </summary>
        public bool CanAddBonus(BonusData bonus)
        {
            if (bonus == null) return false;
            if (!bonus.IsSpecialBonus) return true;

            if (bonus.SpecialRarity == SpecialBonusRarity.Legendary)
                return LegendaryCount < maxLegendaryBonuses;
            if (bonus.SpecialRarity == SpecialBonusRarity.Broken)
                return BrokenCount < maxBrokenBonuses;

            return true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private int GetSpecialRarityCount(SpecialBonusRarity rarity)
        {
            int count = 0;
            for (int i = 0; i < _activeBonuses.Count; i++)
            {
                if (_activeBonuses[i].IsSpecialBonus && _activeBonuses[i].SpecialRarity == rarity)
                    count++;
            }
            return count;
        }

        private void RecalculateCache()
        {
            _cachedPercentModifiers.Clear();
            _cachedFlatModifiers.Clear();

            for (int i = 0; i < _activeBonuses.Count; i++)
            {
                BonusData b = _activeBonuses[i];

                if (b.MainStatType != BonusStatType.None)
                {
                    if (b.IsPercentage)
                        AddToDict(_cachedPercentModifiers, b.MainStatType, b.MainValue);
                    else
                        AddToDict(_cachedFlatModifiers, b.MainStatType, b.MainValue);
                }

                if (b.HasDownside && b.DownsideStatType != BonusStatType.None)
                {
                    float downside = -b.DownsideValue;
                    if (b.DownsideIsPercentage)
                        AddToDict(_cachedPercentModifiers, b.DownsideStatType, downside);
                    else
                        AddToDict(_cachedFlatModifiers, b.DownsideStatType, downside);
                }
            }

            _cacheValid = true;
        }

        private static void AddToDict(Dictionary<BonusStatType, float> dict, BonusStatType key, float value)
        {
            if (dict.TryGetValue(key, out float current))
                dict[key] = current + value;
            else
                dict[key] = value;
        }

        private void InvalidateCache()
        {
            _cacheValid = false;
        }
    }
}
