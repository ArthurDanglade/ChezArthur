using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Runtime "Mise gagnante" de Leuk : conserve le bonus DEF run permanent (cap 40%).
    /// </summary>
    public class LeukGambleSystem : MonoBehaviour
    {
        private const string RunDefBuffId = "leuk_def_run";
        private const float MaxRunDef = 0.40f;

        private CharacterBall _owner;
        private float _permanentRunDef;

        public void Initialize(CharacterBall owner)
        {
            _owner = owner;
            // Compteur reset naturellement à chaque nouvelle run (nouveau CharacterBall / nouveau composant).
            if (_permanentRunDef < 0f)
                _permanentRunDef = 0f;
        }

        public float GetPermanentRunDef() => _permanentRunDef;

        /// <summary>
        /// Tente d'ajouter un bonus DEF run permanent ; applique un buff avec la valeur réellement ajoutée.
        /// </summary>
        public bool TryAddRunDef(float amount)
        {
            if (_owner == null || amount <= 0f) return false;
            if (_permanentRunDef >= MaxRunDef) return false;

            float actual = Mathf.Min(amount, MaxRunDef - _permanentRunDef);
            if (actual <= 0f) return false;

            _permanentRunDef += actual;

            BuffReceiver br = _owner.BuffReceiver;
            if (br != null)
            {
                br.AddBuff(new BuffData
                {
                    BuffId = RunDefBuffId,
                    Source = _owner,
                    StatType = BuffStatType.DEF,
                    Value = actual,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniquePerSource = false,
                    UniqueGlobal = false
                });
            }
            return true;
        }
    }
}

