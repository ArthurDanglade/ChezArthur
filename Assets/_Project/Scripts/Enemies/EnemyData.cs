using UnityEngine;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Données d'un ennemi (ScriptableObject) : identité, stats, physique et récompenses.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "Chez Arthur/Enemy Data", order = 1)]
    public class EnemyData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Identité")]
        [SerializeField] private string enemyName;
        [SerializeField] private EnemyType enemyType;
        [SerializeField] private Sprite icon;

        [Header("Stats de base")]
        [SerializeField] private int baseHp;
        [SerializeField] private int baseAtk;
        [SerializeField] private int baseDef;
        [SerializeField] private int baseSpeed;

        [Header("Physique")]
        [SerializeField] private float colliderWidth = 1f;
        [SerializeField] private float colliderHeight = 1f;

        [Header("Récompenses")]
        [SerializeField] private int talsReward;

        [Header("Passif (préparation future)")]
        [Tooltip("Description textuelle du passif (documentation). Le vrai système PassiveData viendra plus tard.")]
        [TextArea]
        [SerializeField] private string passiveDescription;
        [Tooltip("True si cet ennemi a un passif (système à implémenter).")]
        [SerializeField] private bool hasPassive;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string EnemyName => enemyName;
        public EnemyType EnemyType => enemyType;
        public Sprite Icon => icon;

        public int BaseHp => baseHp;
        public int BaseAtk => baseAtk;
        public int BaseDef => baseDef;
        public int BaseSpeed => baseSpeed;

        public float ColliderWidth => colliderWidth;
        public float ColliderHeight => colliderHeight;

        public int TalsReward => talsReward;

        public string PassiveDescription => passiveDescription;
        public bool HasPassive => hasPassive;
    }
}
