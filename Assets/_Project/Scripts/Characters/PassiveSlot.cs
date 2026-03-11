using System;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Associe un passif à un palier de niveau de déblocage.
    /// </summary>
    [Serializable]
    public class PassiveSlot
    {
        [SerializeField] private PassiveData passiveData;
        [SerializeField] private int unlockLevel = 1;

        /// <summary>Passif associé.</summary>
        public PassiveData PassiveData => passiveData;

        /// <summary>Niveau requis pour débloquer ce passif (ex: 1, 10, 15, 20).</summary>
        public int UnlockLevel => unlockLevel;
    }
}
