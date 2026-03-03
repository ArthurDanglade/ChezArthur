using System;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Interface commune pour les participants au combat (alliés et ennemis), utilisée par le TurnManager.
    /// </summary>
    public interface ITurnParticipant
    {
        // Identité
        /// <summary> Nom affiché du participant. </summary>
        string Name { get; }
        /// <summary> True si allié, false si ennemi. </summary>
        bool IsAlly { get; }

        // Stats
        /// <summary> Vitesse pour l'ordre des tours. </summary>
        int Speed { get; }
        /// <summary> Points de vie actuels. </summary>
        int CurrentHp { get; }
        /// <summary> Points de vie maximum. </summary>
        int MaxHp { get; }
        /// <summary> True si le participant est mort. </summary>
        bool IsDead { get; }

        // Physique
        /// <summary> Transform Unity du GameObject. </summary>
        Transform Transform { get; }
        /// <summary> True si le participant est encore en mouvement. </summary>
        bool IsMoving { get; }

        // Actions
        /// <summary> Lance le participant dans la direction avec la force donnée. </summary>
        void Launch(Vector2 direction, float force);
        /// <summary> Active ou désactive le mouvement (Dynamic / Kinematic). </summary>
        void SetMovable(bool canMove);

        // Events
        /// <summary> Déclenché quand le participant s'arrête. </summary>
        event Action OnStopped;
        /// <summary> Déclenché quand le participant meurt. </summary>
        event Action OnDeath;
    }
}
