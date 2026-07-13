using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Enemies;

namespace ChezArthur.UI
{
    /// <summary>
    /// Gère les barres de vie ennemies en world space via un canvas dédié.
    /// Les barres sont détachées des prefabs ennemis : pas d'héritage rotation/scale.
    /// Pooling strict : aucune barre détruite en cours de run.
    /// </summary>
    public class HPBarManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SINGLETON (scène)
        // ═══════════════════════════════════════════
        public static HPBarManager Instance { get; private set; }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private Canvas barsCanvas;
        [SerializeField] private EnemyHPBar barPrefab;

        [Header("Placement")]
        [SerializeField] private float verticalMargin = 0.25f;

        [Header("Dimensionnement")]
        [SerializeField] private float widthPerWorldUnit = 80f;
        [SerializeField] private float minWidth = 60f;
        [SerializeField] private float maxWidth = 140f;

        // ═══════════════════════════════════════════
        // STRUCTURE INTERNE
        // ═══════════════════════════════════════════
        private sealed class BarEntry
        {
            public Enemy Enemy;
            public BoxCollider2D Collider;
            public EnemyHPBar Bar;
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<BarEntry> _active = new List<BarEntry>(16);
        private readonly Stack<EnemyHPBar> _pool = new Stack<EnemyHPBar>(16);

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

        private void LateUpdate()
        {
            // Perf : ~10 entrées max en pratique, coût négligeable.
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                BarEntry e = _active[i];
                if (e == null || e.Enemy == null)
                {
                    DetachAt(i);
                    continue;
                }

                if (e.Enemy.IsDead || !e.Enemy.gameObject.activeInHierarchy)
                {
                    DetachAt(i);
                    continue;
                }

                if (e.Bar == null)
                {
                    DetachAt(i);
                    continue;
                }

                Vector3 enemyPos = e.Enemy.transform.position;
                float scaleY = Mathf.Abs(e.Enemy.transform.localScale.y);

                float heightWorld = 0f;
                if (e.Collider != null)
                    heightWorld = e.Collider.size.y * scaleY;

                Vector3 worldPos =
                    enemyPos + Vector3.up * (heightWorld * 0.5f + verticalMargin);

                Transform barT = e.Bar.transform;
                barT.position = worldPos;
                barT.rotation = Quaternion.identity;

                int max = Mathf.Max(1, e.Enemy.MaxHp);
                float ratio = (float)e.Enemy.CurrentHp / max;
                e.Bar.SetFill(ratio);
            }
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════
        public void Attach(Enemy enemy)
        {
            if (enemy == null)
                return;

            if (IsTracked(enemy))
                return;

            if (barsCanvas == null || barPrefab == null)
                return;

            EnemyHPBar bar = GetOrCreateBar();
            if (bar == null)
                return;

            bar.gameObject.SetActive(true);

            BoxCollider2D collider = enemy.GetComponent<BoxCollider2D>();

            float widthWorld = 0f;
            if (collider != null)
            {
                float scaleX = Mathf.Abs(enemy.transform.localScale.x);
                widthWorld = collider.size.x * scaleX;
            }

            float width = Mathf.Clamp(widthWorld * widthPerWorldUnit, minWidth, maxWidth);
            bar.SetWidth(width);

            int max = Mathf.Max(1, enemy.MaxHp);
            float ratio = (float)enemy.CurrentHp / max;
            bar.SetFill(ratio);

            _active.Add(new BarEntry
            {
                Enemy = enemy,
                Collider = collider,
                Bar = bar
            });
        }

        public void Detach(Enemy enemy)
        {
            if (enemy == null)
                return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] != null && _active[i].Enemy == enemy)
                {
                    DetachAt(i);
                    return;
                }
            }
        }

        public void DetachAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                DetachAt(i);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private bool IsTracked(Enemy enemy)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                BarEntry e = _active[i];
                if (e != null && e.Enemy == enemy)
                    return true;
            }

            return false;
        }

        private EnemyHPBar GetOrCreateBar()
        {
            EnemyHPBar bar = _pool.Count > 0 ? _pool.Pop() : null;
            if (bar == null)
            {
                Transform parent = barsCanvas != null ? barsCanvas.transform : null;
                if (parent == null) return null;
                bar = Instantiate(barPrefab, parent);
            }

            return bar;
        }

        private void DetachAt(int index)
        {
            if (index < 0 || index >= _active.Count)
                return;

            BarEntry e = _active[index];
            _active.RemoveAt(index);

            if (e == null || e.Bar == null)
                return;

            e.Bar.gameObject.SetActive(false);
            _pool.Push(e.Bar);
        }
    }
}

