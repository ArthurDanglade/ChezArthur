using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Instancie et configure des CharacterBall à partir de CharacterData (équipe du Hub).
    /// </summary>
    public class CharacterBallFactory : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Prefab")]
        [SerializeField] private CharacterBall ballPrefab;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Spawne une équipe de CharacterBall aux positions données.
        /// Chaque balle reçoit les données du personnage, l'icône et le TurnManager (pour les triggers d'équipe).
        /// </summary>
        /// <param name="team">Équipe (data + owned).</param>
        /// <param name="spawnPositions">Positions de spawn (ordre respecté).</param>
        /// <param name="turnManager">TurnManager à assigner à chaque balle (optionnel).</param>
        /// <returns>Liste des balles instanciées (vide si team null/vide ou prefab manquant).</returns>
        public List<CharacterBall> SpawnTeam(
            List<(CharacterData data, OwnedCharacter owned)> team,
            List<Vector2> spawnPositions,
            TurnManager turnManager = null)
        {
            var result = new List<CharacterBall>();

            if (team == null || team.Count == 0)
            {
                Debug.LogWarning("[CharacterBallFactory] SpawnTeam: team null ou vide.");
                return result;
            }

            if (ballPrefab == null)
            {
                Debug.LogWarning("[CharacterBallFactory] ballPrefab non assigné.");
                return result;
            }

            if (spawnPositions == null || spawnPositions.Count == 0)
            {
                Debug.LogWarning("[CharacterBallFactory] spawnPositions null ou vide.");
                return result;
            }

            int count = Mathf.Min(team.Count, spawnPositions.Count);

            for (int i = 0; i < count; i++)
            {
                var (data, owned) = team[i];
                if (data == null) continue;

                Vector2 pos = spawnPositions[i];
                Vector3 worldPos = new Vector3(pos.x, pos.y, 0f);

                CharacterBall ball = Instantiate(ballPrefab, worldPos, Quaternion.identity);
                ball.gameObject.name = "CharacterBall_" + data.CharacterName;

                ball.SetCharacterData(data);

                if (turnManager != null)
                    ball.SetTurnManager(turnManager);

                // Niveau et spé active : alignés sur la sauvegarde / l'équipe du Hub.
                if (owned != null)
                    ball.SetOwnedCharacter(owned, owned.level);

                SpriteRenderer sr = ball.GetComponent<SpriteRenderer>();
                if (sr != null && data.Icon != null)
                    sr.sprite = data.Icon;

                result.Add(ball);
            }

            return result;
        }
    }
}
