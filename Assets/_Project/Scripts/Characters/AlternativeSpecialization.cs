using System;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Spécialisation alternative débloquable à un certain niveau (ex: 15 pour SSR, 15 ou 20 pour LR).
    /// </summary>
    [Serializable]
    public class AlternativeSpecialization
    {
        [SerializeField] private SpecializationData specialization;
        [SerializeField] private int unlockLevel = 15;

        /// <summary>Données de la spécialisation.</summary>
        public SpecializationData Specialization => specialization;

        /// <summary>Niveau requis pour débloquer cette spé.</summary>
        public int UnlockLevel => unlockLevel;
    }
}
