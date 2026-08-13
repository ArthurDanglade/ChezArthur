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
        /// <summary> Durée hint (s) — 0 = aucun. Utilisé pour caler le pitch d'un riser. </summary>
        public float DurationHint;

        /// <summary> Label forcé (stats F5-L2) — null/vide = bundle.labelTextFr. </summary>
        public string LabelOverride;

        /// <summary> Couleur du label override (si HasLabelColor). </summary>
        public Color LabelColor;

        /// <summary> True si LabelColor est renseigné. </summary>
        public bool HasLabelColor;

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
                CharacterId = null,
                DurationHint = 0f,
                LabelOverride = null,
                LabelColor = default,
                HasLabelColor = false
            };
        }
    }
}
