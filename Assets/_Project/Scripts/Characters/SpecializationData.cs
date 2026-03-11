using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Profil complet d'une spécialisation : rôle, stats de base et croissance, passifs par palier de niveau.
    /// Chaque personnage peut avoir plusieurs spécialisations ; le profil "de base" est aussi une SpecializationData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpecialization", menuName = "Chez Arthur/Specialization Data", order = 2)]
    public class SpecializationData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string specName;
        [SerializeField] private CharacterRole role;

        [Header("Stats de base (niveau 1)")]
        [SerializeField] private int baseHp;
        [SerializeField] private int baseAtk;
        [SerializeField] private int baseDef;
        [SerializeField] private int baseSpeed;

        [Header("Croissance (par niveau)")]
        [SerializeField] private int hpPerLevel;
        [SerializeField] private int atkPerLevel;
        [SerializeField] private int defPerLevel;
        [SerializeField] private int speedPerLevel;

        [Header("Passifs")]
        [SerializeField] private List<PassiveSlot> passiveSlots = new List<PassiveSlot>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string SpecName => specName;
        public CharacterRole Role => role;
        public int BaseHp => baseHp;
        public int BaseAtk => baseAtk;
        public int BaseDef => baseDef;
        public int BaseSpeed => baseSpeed;
        public int HpPerLevel => hpPerLevel;
        public int AtkPerLevel => atkPerLevel;
        public int DefPerLevel => defPerLevel;
        public int SpeedPerLevel => speedPerLevel;
        public IReadOnlyList<PassiveSlot> PassiveSlots => passiveSlots;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>PV au niveau donné (base + croissance).</summary>
        public int GetHpAtLevel(int level)
        {
            int l = Mathf.Max(1, level);
            return baseHp + hpPerLevel * (l - 1);
        }

        /// <summary>ATK au niveau donné.</summary>
        public int GetAtkAtLevel(int level)
        {
            int l = Mathf.Max(1, level);
            return baseAtk + atkPerLevel * (l - 1);
        }

        /// <summary>DEF au niveau donné.</summary>
        public int GetDefAtLevel(int level)
        {
            int l = Mathf.Max(1, level);
            return baseDef + defPerLevel * (l - 1);
        }

        /// <summary>Speed au niveau donné.</summary>
        public int GetSpeedAtLevel(int level)
        {
            int l = Mathf.Max(1, level);
            return baseSpeed + speedPerLevel * (l - 1);
        }

        /// <summary>
        /// Retourne tous les passifs débloqués au niveau donné (unlockLevel &lt;= level).
        /// </summary>
        public List<PassiveData> GetActivePassives(int level)
        {
            var list = new List<PassiveData>();
            if (passiveSlots == null) return list;

            for (int i = 0; i < passiveSlots.Count; i++)
            {
                PassiveSlot slot = passiveSlots[i];
                if (slot != null && slot.UnlockLevel <= level && slot.PassiveData != null)
                    list.Add(slot.PassiveData);
            }
            return list;
        }

        /// <summary>
        /// Retourne la liste des slots passifs (pour l'UI).
        /// </summary>
        public IReadOnlyList<PassiveSlot> GetPassiveSlots() => passiveSlots;
    }
}
