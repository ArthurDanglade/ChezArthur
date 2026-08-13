using ChezArthur.Characters;
using UnityEngine;

namespace ChezArthur.Enemies.Passives
{
    /// <summary>
    /// Politique de déclenchement des passifs à trigger de dégâts reçus (R5).
    /// Ne JAMAIS réordonner : sérialisé par int. PerHit = 0 = comportement historique.
    /// </summary>
    public enum EnemyPassiveMultiHitPolicy
    {
        /// <summary> Chaque hit déclenche (historique — défaut de sérialisation). </summary>
        PerHit = 0,
        /// <summary> 1 déclenchement max par tour (défaut ÉDITORIAL projet, contrat R5). </summary>
        PerTurn,
        /// <summary> 1 déclenchement max par cycle (D25). </summary>
        PerCycle,
    }

    /// <summary>
    /// Données d'un passif ennemi (ScriptableObject) : déclencheur, condition,
    /// effet standard ou handler spécialisé, et pool A/B optionnel.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyPassive", menuName = "Chez Arthur/Enemy/Passive Data", order = 2)]
    public class EnemyPassiveData : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════

        [Header("Identité")]
        [SerializeField] private string passiveName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Trigger")]
        [SerializeField] private EnemyPassiveTrigger trigger;

        [Header("Condition (optionnelle)")]
        [SerializeField] private EnemyPassiveCondition condition;
        [Tooltip("Seuil HP entre 0 et 1. Ex: 0.20 = 20% HP.")]
        [SerializeField] private float conditionThreshold = 0f;
        [SerializeField] private int conditionCount = 0;
        [SerializeField] private CharacterRole conditionRole;

        [Header("Effet standard")]
        [SerializeField] private EnemyPassiveEffect effect;
        [SerializeField] private float value;
        [SerializeField] private bool isPercentage;
        [Tooltip("Nombre max de stacks. 0 = pas de limite.")]
        [SerializeField] private int maxStacks = 0;
        [Tooltip("Valeur ajoutée par stack supplémentaire.")]
        [SerializeField] private float stackValue;

        [Header("Durée et limites")]
        [Tooltip("Nombre de cycles pendant lesquels l'effet est actif. 0 = permanent.")]
        [SerializeField] private int durationCycles = 0;
        [Tooltip("Nombre de tours pendant lesquels l'effet est actif. 0 = permanent.")]
        [SerializeField] private int durationTurns = 0;
        [Tooltip("Si true, cet effet ne peut se déclencher qu'une seule fois par étage.")]
        [SerializeField] private bool oneTimeOnly;
        [Tooltip("Si true, l'effet persiste entre les étages (rare, réservé aux boss).")]
        [SerializeField] private bool persistBetweenStages;

        [Header("Handler spécialisé")]
        [Tooltip("Identifiant du handler externe. Laisser vide pour utiliser le système standard.")]
        [SerializeField] private string specialHandlerId;
        [SerializeField] private float specialValue1;
        [SerializeField] private float specialValue2;
        [SerializeField] private float specialValue3;

        [Header("Politique multi-hit (R5)")]
        [Tooltip("Appliquée aux triggers OnTakeDamage / OnHitByAlly uniquement. PerHit = historique. Défaut projet pour tout nouveau passif à trigger de dégâts : PerTurn (posé par le générateur d'assets).")]
        [SerializeField] private EnemyPassiveMultiHitPolicy multiHitPolicy = EnemyPassiveMultiHitPolicy.PerHit;

        [Header("Buff partagé (D25)")]
        [Tooltip("Si renseigné : id de buff partagé entre tous les porteurs de ce passif → effet non cumulable (remplacement UniqueGlobal). Vide = id par instance (historique).")]
        [SerializeField] private string sharedBuffId;

        [Header("Liaison à la source (R8/D25)")]
        [Tooltip("Si true, les buffs/debuffs posés par ce passif meurent avec leur applicateur (aura du Garde, flèche perforante). Consommé par ApplyBuff.")]
        [SerializeField] private bool expiresWithSource;

        [Header("Feedback (F5-L2)")]
        [Tooltip("Callout F5-L2 supprimé (événement porté par une annonce T1 — Alucadra, Rupture…).")]
        [SerializeField] private bool silentProc;

        [Header("Pool A/B (Univers 5 — passifs aléatoires par étage)")]
        [Tooltip("Si true, un des deux passifs du pool est tiré aléatoirement au début de chaque étage.")]
        [SerializeField] private bool hasPool;
        [SerializeField] private EnemyPassiveData poolPassiveA;
        [SerializeField] private EnemyPassiveData poolPassiveB;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        public string PassiveName => passiveName;
        /// <summary> Clé stable pour compteur/dedup (nom d'asset SO). </summary>
        public string PassiveId => name;
        public string Description => description;
        public EnemyPassiveTrigger Trigger => trigger;
        public EnemyPassiveCondition Condition => condition;
        public float ConditionThreshold => conditionThreshold;
        public int ConditionCount => conditionCount;
        public CharacterRole ConditionRole => conditionRole;
        public EnemyPassiveEffect Effect => effect;
        public float Value => value;
        public bool IsPercentage => isPercentage;
        public int MaxStacks => maxStacks;
        public float StackValue => stackValue;
        public int DurationCycles => durationCycles;
        public int DurationTurns => durationTurns;
        public bool OneTimeOnly => oneTimeOnly;
        public bool PersistBetweenStages => persistBetweenStages;
        public string SpecialHandlerId => specialHandlerId;
        public float SpecialValue1 => specialValue1;
        public float SpecialValue2 => specialValue2;
        public float SpecialValue3 => specialValue3;
        public EnemyPassiveMultiHitPolicy MultiHitPolicy => multiHitPolicy;
        public string SharedBuffId => sharedBuffId;
        public bool ExpiresWithSource => expiresWithSource;
        public bool SilentProc => silentProc;
        public bool HasPool => hasPool;
        public EnemyPassiveData PoolPassiveA => poolPassiveA;
        public EnemyPassiveData PoolPassiveB => poolPassiveB;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retourne la description formatée.
        /// Remplace {value} par la valeur formatée (% ou flat).
        /// </summary>
        public string GetFormattedDescription()
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            string valueStr = isPercentage ? $"{value * 100f:0}%" : $"{value:0}";
            return description.Replace("{value}", valueStr);
        }

        /// <summary>
        /// Résout le pool A/B — tire aléatoirement A ou B si hasPool est true.
        /// Retourne this si pas de pool.
        /// </summary>
        public EnemyPassiveData ResolvePool()
        {
            if (!hasPool)
                return this;

            bool aNull = poolPassiveA == null;
            bool bNull = poolPassiveB == null;

            if (aNull && bNull)
                return this;
            if (aNull)
                return poolPassiveB;
            if (bNull)
                return poolPassiveA;

            return Random.value < 0.5f ? poolPassiveA : poolPassiveB;
        }
    }
}
