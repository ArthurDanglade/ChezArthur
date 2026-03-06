using System;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Ensemble des passifs d'un personnage selon son niveau et sa spécialisation.
    /// SR : base (1), lvl 5. SSR : base (1), lvl 5, spé au lvl 15 (2 options). LR : base (1), lvl 5, lvl 15, spé au lvl 20 (3 options).
    /// </summary>
    [Serializable]
    public class CharacterPassiveSet
    {
        [Header("Passif de base (Niveau 1)")]
        [SerializeField] private PassiveData basePassive;

        [Header("Passif Niveau 5 (tous)")]
        [SerializeField] private PassiveData level5Passive;

        [Header("Passif Niveau 15 (LR uniquement)")]
        [SerializeField] private PassiveData level15Passive;

        [Header("Spécialisations SSR (lvl 15) — 2 sur 3 disponibles")]
        [SerializeField] private PassiveData ssrSpecializationOption1;
        [SerializeField] private SpecializationType ssrSpecOption1Type;
        [SerializeField] private PassiveData ssrSpecializationOption2;
        [SerializeField] private SpecializationType ssrSpecOption2Type;

        [Header("Spécialisations LR (lvl 20) — 3 disponibles")]
        [SerializeField] private PassiveData lrSpecializationAttacker;
        [SerializeField] private PassiveData lrSpecializationDefender;
        [SerializeField] private PassiveData lrSpecializationSupport;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public PassiveData BasePassive => basePassive;
        public PassiveData Level5Passive => level5Passive;
        public PassiveData Level15Passive => level15Passive;
        public PassiveData SsrSpecializationOption1 => ssrSpecializationOption1;
        public SpecializationType SsrSpecOption1Type => ssrSpecOption1Type;
        public PassiveData SsrSpecializationOption2 => ssrSpecializationOption2;
        public SpecializationType SsrSpecOption2Type => ssrSpecOption2Type;
        public PassiveData LrSpecializationAttacker => lrSpecializationAttacker;
        public PassiveData LrSpecializationDefender => lrSpecializationDefender;
        public PassiveData LrSpecializationSupport => lrSpecializationSupport;

        /// <summary>
        /// Retourne le passif de spécialisation selon le choix.
        /// SSR : option1 ou option2 selon le type configuré. LR : Attacker / Defender / Support.
        /// </summary>
        public PassiveData GetSpecializationPassive(SpecializationType type)
        {
            if (type == SpecializationType.None) return null;
            if (type == ssrSpecOption1Type) return ssrSpecializationOption1;
            if (type == ssrSpecOption2Type) return ssrSpecializationOption2;
            return type switch
            {
                SpecializationType.Attacker => lrSpecializationAttacker,
                SpecializationType.Defender => lrSpecializationDefender,
                SpecializationType.Support => lrSpecializationSupport,
                _ => null
            };
        }
    }
}
