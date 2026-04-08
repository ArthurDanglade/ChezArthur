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

        [SerializeField] private Color colorDamageEnemy = new Color(1f, 0.3f, 0.3f);
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
        /// Dégâts infligés à un ennemi (rouge).
        /// </summary>
        public void ShowDamageEnemy(int amount, Vector3 worldPos)
        {
            Spawn(amount.ToString(), colorDamageEnemy, worldPos, 1f);
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
        private void Spawn(string text, Color color, Vector3 worldPos, float scale = 1f)
        {
            if (floatingNumberPrefab == null) return;

            // Léger offset aléatoire pour éviter la superposition
            float offsetX = Random.Range(-randomOffsetX, randomOffsetX);
            Vector3 spawnPos = worldPos + new Vector3(offsetX, spawnOffsetY, 0f);

            GameObject go = Instantiate(floatingNumberPrefab, spawnPos, Quaternion.identity);
            FloatingNumber fn = go.GetComponent<FloatingNumber>();
            if (fn != null)
                fn.Initialize(text, color, scale);
        }
    }
}
