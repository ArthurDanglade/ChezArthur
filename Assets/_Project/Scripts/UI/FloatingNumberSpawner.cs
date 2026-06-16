using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Singleton qui instancie des nombres flottants en monde.
    /// </summary>
    public class FloatingNumberSpawner : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private GameObject floatingNumberPrefab;
        [SerializeField] private float spawnOffsetY = 0.5f;
        [SerializeField] private float randomOffsetX = 0.4f;

        [Header("Crit / ampleur")]
        [SerializeField] private Color _enemyDamageColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color _critColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private float _critScaleMul = 1.5f;
        [SerializeField] private int _damageForMinScale = 20;
        [SerializeField] private int _damageForMaxScale = 300;
        [SerializeField] private float _minMagnitudeScale = 0.9f;
        [SerializeField] private float _maxMagnitudeScale = 1.4f;

        [SerializeField] private Color colorDamageAlly = new Color(1f, 0.6f, 0.2f);
        [SerializeField] private Color colorHeal = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color colorPoison = new Color(0.5f, 0.9f, 0.2f);
        [SerializeField] private Color colorBurn = new Color(1f, 0.5f, 0f);

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static FloatingNumberSpawner Instance { get; private set; }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Dégâts infligés à un ennemi (rouge, doré si crit).
        /// </summary>
        public void ShowDamageEnemy(int amount, Vector3 worldPos, bool isCrit = false)
        {
            Color color = isCrit ? _critColor : _enemyDamageColor;
            float magScale = Mathf.Lerp(_minMagnitudeScale, _maxMagnitudeScale,
                Mathf.Clamp01((float)(amount - _damageForMinScale) / Mathf.Max(1, _damageForMaxScale - _damageForMinScale)));
            Spawn(amount.ToString(), color, worldPos, magScale * (isCrit ? _critScaleMul : 1f), isCrit);
        }

        /// <summary>
        /// Dégâts infligés à un allié (orange).
        /// </summary>
        public void ShowDamageAlly(int amount, Vector3 worldPos)
        {
            Spawn(amount.ToString(), colorDamageAlly, worldPos, 1f);
        }

        /// <summary>
        /// Soin reçu (vert).
        /// </summary>
        public void ShowHeal(int amount, Vector3 worldPos)
        {
            Spawn("+" + amount.ToString(), colorHeal, worldPos, 0.85f);
        }

        /// <summary>
        /// Dégâts de poison (vert acide).
        /// </summary>
        public void ShowPoison(int amount, Vector3 worldPos)
        {
            Spawn(amount.ToString(), colorPoison, worldPos, 0.8f);
        }

        /// <summary>
        /// Dégâts de brûlure (orange foncé).
        /// </summary>
        public void ShowBurn(int amount, Vector3 worldPos)
        {
            Spawn(amount.ToString(), colorBurn, worldPos, 0.8f);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void Spawn(string text, Color color, Vector3 worldPos, float scale = 1f, bool isCrit = false)
        {
            if (floatingNumberPrefab == null) return;

            float offsetX = Random.Range(-randomOffsetX, randomOffsetX);
            Vector3 spawnPos = worldPos + new Vector3(offsetX, spawnOffsetY, 0f);

            GameObject go = Instantiate(floatingNumberPrefab, spawnPos, Quaternion.identity);
            FloatingNumber fn = go.GetComponent<FloatingNumber>();
            if (fn != null)
                fn.Initialize(text, color, scale, isCrit);
        }
    }
}
