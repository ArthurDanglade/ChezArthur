using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Buffs
{
    /// <summary>
    /// Représente un buff temporaire actif sur un personnage.
    /// Pas d'allocation récurrente — créé une fois, décrémenté chaque tour/cycle, supprimé à expiration.
    /// </summary>
    public class BuffData
    {
        /// <summary>Identifiant unique du type de buff (ex: "brooke_sword", "benediction_animale").</summary>
        public string BuffId;

        /// <summary>Source du buff (le CharacterBall qui l'a appliqué). Peut être null.</summary>
        public CharacterBall Source;

        /// <summary>Type de stat buffée.</summary>
        public BuffStatType StatType;

        /// <summary>Valeur du buff (ex: 0.20 pour +20%).</summary>
        public float Value;

        /// <summary>True si la valeur est un % (multiplicatif). False si c'est du flat (additif).</summary>
        public bool IsPercent;

        /// <summary>Durée restante en tours du porteur (décrémenté à chaque fin de tour du porteur). -1 = permanent jusqu'à suppression manuelle.</summary>
        public int RemainingTurns;

        /// <summary>Durée restante en cycles (décrémenté à chaque fin de cycle complet). -1 = pas de durée en cycles.</summary>
        public int RemainingCycles;

        /// <summary>True si ce buff est unique par source (un seul buff de ce buffId par source).</summary>
        public bool UniquePerSource;

        /// <summary>True si ce buff est unique globalement (un seul buff de ce buffId total).</summary>
        public bool UniqueGlobal;
    }

    /// <summary>
    /// Types de stats pouvant être buffées/debuffées.
    /// </summary>
    public enum BuffStatType
    {
        ATK,
        DEF,
        HP,
        Speed,
        LaunchForce,
        DamageReduction,      // réduction de dégâts en % (ex: -30% dégâts subis)
        DamageAmplification,  // augmentation des dégâts subis (pour les debuffs ennemis)
        HealReceived,         // modificateur de soins reçus (+ ou -)
        Shield                // bouclier absorbant (Value = HP du bouclier)
    }
}
