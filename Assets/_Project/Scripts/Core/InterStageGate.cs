using System.Collections;
using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Core
{
    /// <summary>
    /// Helpers de synchronisation entre fin de combat et suites (bonus, Gare, étage suivant).
    /// </summary>
    public static class InterStageGate
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const float TalsCollectionTimeout = 2.5f;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Attend que toutes les pièces Tals cosmétiques soient collectées (timeout de sécurité).
        /// </summary>
        public static IEnumerator WaitForTalsCollection(float timeout = TalsCollectionTimeout)
        {
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (TalsDropSystem.Instance == null || !TalsDropSystem.Instance.HasPendingDrops)
                    yield break;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (TalsDropSystem.Instance != null && TalsDropSystem.Instance.HasPendingDrops)
                Debug.LogWarning("[InterStageGate] Timeout collecte Tals — poursuite du flux inter-étage.");
        }
    }
}
