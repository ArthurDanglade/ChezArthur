using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Retourne l'instance au pool quand le ParticleSystem s'arrête.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledFxReturner : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private FxPool _pool;
        private ParticleSystem _prefabKey;
        private Vector3 _restScale = Vector3.one;
        private bool _activeInPool;

        public ParticleSystem PrefabKey => _prefabKey;
        public Vector3 RestScale => _restScale;

        // ═══════════════════════════════════════════
        // API POOL
        // ═══════════════════════════════════════════

        public void Bind(FxPool pool, ParticleSystem prefabKey, Vector3 restScale)
        {
            _pool = pool;
            _prefabKey = prefabKey;
            _restScale = restScale;
        }

        public void MarkActive(bool active)
        {
            _activeInPool = active;
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void OnParticleSystemStopped()
        {
            if (_pool != null)
                _pool.Release(GetComponent<ParticleSystem>());
        }

        private void OnDestroy()
        {
            if (_activeInPool && _pool != null)
                _pool.NotifyDestroyedWhileActive();
        }
    }
}
