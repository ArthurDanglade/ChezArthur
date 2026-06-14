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

        [Header("Échelle visuelle en combat")]
        [Tooltip("Si activé, normalise Visual + Shadow via CharacterBall.ReferenceBallDiameter (racine scale = 1).")]
        [SerializeField] private bool normalizeCombatSpriteScale = true;

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

                SpriteRenderer visualRenderer = ball.VisualRenderer;
                if (visualRenderer != null && data.Icon != null)
                {
                    visualRenderer.sprite = data.Icon;
                    if (normalizeCombatSpriteScale)
                        ApplyUniformSpriteWorldSize(ball);
                }

                result.Add(ball);
            }

            return result;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Normalise Visual (max côté = ReferenceBallDiameter) et Shadow (ellipse ~85 % diamètre).
        /// La racine reste à scale 1 ; le collider est en unités monde sur CharacterBall.
        /// </summary>
        private void ApplyUniformSpriteWorldSize(CharacterBall ball)
        {
            if (ball == null) return;

            SpriteRenderer visualRenderer = ball.VisualRenderer;
            if (visualRenderer != null && visualRenderer.sprite != null)
            {
                float maxSide = Mathf.Max(
                    visualRenderer.sprite.bounds.size.x,
                    visualRenderer.sprite.bounds.size.y);
                if (maxSide > 0.0001f)
                {
                    float vScale = CharacterBall.ReferenceBallDiameter / maxSide;
                    ball.Visual.localScale = new Vector3(vScale, vScale, 1f);
                }
            }

            if (ball.ShadowRenderer != null && ball.ShadowRenderer.sprite != null)
            {
                float sMax = Mathf.Max(
                    ball.ShadowRenderer.sprite.bounds.size.x,
                    ball.ShadowRenderer.sprite.bounds.size.y);
                if (sMax > 0.0001f)
                {
                    float sScale = (CharacterBall.ReferenceBallDiameter * 0.85f) / sMax;
                    ball.ShadowTransform.localScale = new Vector3(sScale, sScale * 0.5f, 1f);
                }
            }

            ball.RefreshFloatBase();
        }
    }
}
