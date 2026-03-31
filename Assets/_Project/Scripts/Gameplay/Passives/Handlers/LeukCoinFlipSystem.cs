using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime "Pile ou face" de Leuk :
    /// modifie instantanément les dégâts finaux avant application aux PV.
    /// </summary>
    public class LeukCoinFlipSystem : MonoBehaviour
    {
        private CharacterBall _owner;

        public void Initialize(CharacterBall owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 75% : dégâts divisés par 2. 25% : dégâts augmentés de 20%.
        /// </summary>
        public int ModifyDamage(int finalDamage)
        {
            if (finalDamage <= 0) return 0;
            if (_owner == null) return finalDamage;

            float roll = Random.value;
            if (roll < 0.75f)
                return Mathf.Max(1, Mathf.RoundToInt(finalDamage * 0.50f));

            return Mathf.Max(1, Mathf.RoundToInt(finalDamage * 1.20f));
        }
    }
}

