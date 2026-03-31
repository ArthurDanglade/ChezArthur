using UnityEngine;
using ChezArthur.Gameplay;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// Segment de lait : soigne un allié en mouvement puis disparaît.
    /// </summary>
    public class GoatMilkSegment : MonoBehaviour
    {
        private CharacterBall _source;

        public void Initialize(CharacterBall source)
        {
            _source = source;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || _source == null) return;

            CharacterBall ally = other.GetComponent<CharacterBall>();
            if (ally == null || ally.IsDead || ally == _source) return;
            if (!ally.IsMoving) return;

            int heal = Mathf.RoundToInt(ally.MaxHp * 0.15f);
            if (heal > 0)
                ally.Heal(heal);

            Destroy(gameObject);
        }
    }
}

