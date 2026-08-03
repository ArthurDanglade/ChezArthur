using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Contexte de lecture d'un événement de feedback (passé en `in`, zéro alloc).
    /// </summary>
    public struct FeedbackContext
    {
        public Vector2 Position;
        public Vector2 Direction;
        public float Intensity01;
        public Transform Target;
        public CharacterBall TargetBall;
        public string CharacterId;

        /// <summary>
        /// Contexte minimal à une position (intensité 1).
        /// </summary>
        public static FeedbackContext At(Vector2 pos)
        {
            return new FeedbackContext
            {
                Position = pos,
                Direction = Vector2.zero,
                Intensity01 = 1f,
                Target = null,
                TargetBall = null,
                CharacterId = null
            };
        }
    }
}
