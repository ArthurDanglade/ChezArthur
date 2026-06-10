using UnityEngine;

namespace ChezArthur.Core
{
    /// <summary>
    /// Verrou global d'input gameplay. Tant qu'au moins un panneau bloquant est ouvert,
    /// IsLocked vaut true et le DragDropController ignore les inputs joueur.
    /// Compteur : supporte l'empilement de panneaux (ex. sélection -> sacrifice par-dessus).
    /// </summary>
    public static class GameplayInputLock
    {
        private static int _lockCount;

        /// <summary> True si au moins un panneau bloquant est ouvert. </summary>
        public static bool IsLocked => _lockCount > 0;

        /// <summary> Nombre de verrous actifs (debug). </summary>
        public static int LockCount => _lockCount;

        /// <summary> Acquiert un verrou (un panneau bloquant s'ouvre). </summary>
        public static void Acquire() => _lockCount++;

        /// <summary> Libère un verrou (un panneau bloquant se ferme). Clampé à 0. </summary>
        public static void Release()
        {
            if (_lockCount > 0) _lockCount--;
        }

        /// <summary> Réinitialise le compteur au démarrage. Sécurité si le domain reload est désactivé. </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad() => _lockCount = 0;
    }
}
