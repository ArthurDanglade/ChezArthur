using System;
using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Données de ciblage par priorité de rôle (R3). Liste vide = plus proche (historique).
    /// </summary>
    [Serializable]
    public class TargetSelectorData
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Tooltip("Priorité de ciblage par spé ACTIVE, dans l'ordre (ex. [Support] ou [Defender, Support, Attacker]). Vide = allié vivant le plus proche (comportement historique).")]
        [SerializeField] private List<CharacterRole> priorityRoles = new List<CharacterRole>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public IReadOnlyList<CharacterRole> PriorityRoles => priorityRoles;
    }

    /// <summary>
    /// Résolution de cible data-driven (R3) — zéro LINQ, zéro allocation.
    /// </summary>
    public static class TargetSelectorResolver
    {
        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Résout la cible selon la liste de priorités (R3), sur les spés ACTIVES du moment.
        /// Pour chaque rôle dans l'ordre : s'il existe au moins un candidat de ce rôle,
        /// retourne le plus proche d'entre eux. Aucun rôle ne matche (ou liste vide) :
        /// repli « allié vivant le plus proche ». Retourne null si aucun candidat.
        /// Candidat = vivant ET IsTargetableByEnemies (Shado invisible exclu, comme l'IA actuelle).
        /// </summary>
        public static CharacterBall Resolve(
            TargetSelectorData selector,
            Vector2 fromPosition,
            IReadOnlyList<CharacterBall> allies)
        {
            if (allies == null || allies.Count == 0)
                return null;

            IReadOnlyList<CharacterRole> roles = selector != null ? selector.PriorityRoles : null;
            if (roles != null && roles.Count > 0)
            {
                for (int r = 0; r < roles.Count; r++)
                {
                    CharacterBall bestOfRole = FindClosestMatching(fromPosition, allies, roles[r], filterByRole: true);
                    if (bestOfRole != null)
                        return bestOfRole;
                }
            }

            return FindClosestMatching(fromPosition, allies, default, filterByRole: false);
        }

        /// <summary>
        /// Lecture canonique du rôle actif côté ciblage : spé ACTIVE d'abord, sinon Data.Role
        /// (même logique que EnemyPassiveRuntime.GetAllyRole).
        /// </summary>
        public static CharacterRole GetActiveRole(CharacterBall ally)
        {
            if (ally == null)
                return default;

            if (ally.ActiveSpec != null)
                return ally.ActiveSpec.Role;

            return ally.Data != null ? ally.Data.Role : default;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static CharacterBall FindClosestMatching(
            Vector2 fromPosition,
            IReadOnlyList<CharacterBall> allies,
            CharacterRole role,
            bool filterByRole)
        {
            CharacterBall closest = null;
            float closestSqr = float.MaxValue;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || !ally.IsTargetableByEnemies)
                    continue;

                if (filterByRole && GetActiveRole(ally) != role)
                    continue;

                float sqr = ((Vector2)ally.transform.position - fromPosition).sqrMagnitude;
                if (sqr < closestSqr)
                {
                    closestSqr = sqr;
                    closest = ally;
                }
            }

            return closest;
        }
    }
}
