using System.Collections.Generic;
using ChezArthur.Enemies.Passives;
using UnityEngine;
using UnityEngine.Serialization;

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
        [Tooltip("Identifiant unique (snake_case). Sert au câblage auto : combat_<id>.png.")]
        [SerializeField] private string id;
        [SerializeField] private string enemyName;
        [SerializeField] private EnemyType enemyType;
        [SerializeField] private int universeIndex;
        // 1 à 5, ou 0 = tous univers (post-100)
        [SerializeField] private EnemyRole enemyRole;
        [Tooltip("Description narrative courte affichée sur la fiche d'inspection.")]
        [TextArea]
        [SerializeField] private string description;
        [Tooltip("Sprite de l'ennemi en combat (pixel art). Unique visuel de l'ennemi.")]
        [FormerlySerializedAs("icon")]
        [SerializeField] private Sprite combatSprite;

        [Header("Stats de base")]
        [SerializeField] private int baseHp;
        [SerializeField] private int baseAtk;
        [SerializeField] private int baseDef;
        [SerializeField] private int baseSpeed;

        [Header("Physique")]
        [SerializeField] private float colliderWidth = 1f;
        [SerializeField] private float colliderHeight = 1f;
        [Tooltip("Multiplicateur de taille du sprite combat. 1 = remplit la hitbox ; >1 = plus grand que la hitbox.")]
        [SerializeField] private float combatVisualScale = 1f;

        [Header("Récompenses")]
        [SerializeField] private int talsReward;

        [Header("Passif (préparation future)")]
        [Tooltip("Description textuelle du passif (documentation). Le vrai système PassiveData viendra plus tard.")]
        [TextArea]
        [SerializeField] private string passiveDescription;
        [Tooltip("True si cet ennemi a un passif (système à implémenter).")]
        [SerializeField] private bool hasPassive;

        [Header("Passifs ennemis (data-driven)")]
        [SerializeField] private List<EnemyPassiveData> enemyPassives = new List<EnemyPassiveData>();

        [Header("Archétype (R2)")]
        [Tooltip("Valeur par défaut au spawn. L'archétype est un état RUNTIME de Enemy : Alucadra bascule Fixe → Mobile en combat (jamais un flag figé).")]
        [SerializeField] private EnemyArchetype defaultArchetype = EnemyArchetype.Mobile;

        [Header("Ciblage (R3)")]
        [SerializeField] private TargetSelectorData targetSelector = new TargetSelectorData();

        [Header("Lancer (R6)")]
        [Tooltip("Force de lancer FIXE de cet ennemi (R6 — zéro aléatoire). 0 = non calibré → repli runtime (33). Les vraies valeurs par fiche sont posées par le générateur d'assets U1 (G6a).")]
        [SerializeField] private float launchForce = 0f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public string Id => id;
        public string EnemyName => enemyName;
        public EnemyType EnemyType => enemyType;
        public int UniverseIndex => universeIndex;
        public EnemyRole EnemyRole => enemyRole;
        public string Description => description;
        public Sprite CombatSprite => combatSprite;

        public int BaseHp => baseHp;
        public int BaseAtk => baseAtk;
        public int BaseDef => baseDef;
        public int BaseSpeed => baseSpeed;

        public float ColliderWidth => colliderWidth;
        public float ColliderHeight => colliderHeight;
        public float CombatVisualScale => combatVisualScale;

        public int TalsReward => talsReward;

        public string PassiveDescription => passiveDescription;
        public bool HasPassive => hasPassive;

        /// <summary> Passifs ennemis pour EnemyPassiveRuntime (optionnel). </summary>
        public IReadOnlyList<EnemyPassiveData> Passives => enemyPassives;

        public EnemyArchetype DefaultArchetype => defaultArchetype;
        public TargetSelectorData TargetSelector => targetSelector;
        public float LaunchForce => launchForce;
    }
}
