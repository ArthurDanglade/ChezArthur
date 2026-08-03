using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Pool de ParticleSystem en boucle — Release explicite uniquement (jamais auto-return).
    /// </summary>
    public class StatusLoopPool
    {
        // ═══════════════════════════════════════════
        // SINGLETON SCÈNE
        // ═══════════════════════════════════════════
        private static StatusLoopPool _shared;
        private static Transform _poolRoot;

        public static StatusLoopPool Shared
        {
            get
            {
                if (_shared == null || _poolRoot == null)
                    _shared = new StatusLoopPool();
                return _shared;
            }
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Dictionary<ParticleSystem, Stack<ParticleSystem>> _stacks =
            new Dictionary<ParticleSystem, Stack<ParticleSystem>>(8);
        private readonly Dictionary<ParticleSystem, ParticleSystem> _instanceToPrefab =
            new Dictionary<ParticleSystem, ParticleSystem>(16);
        private int _activeCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS
        // ═══════════════════════════════════════════
        public int ActiveLoopCount => _activeCount;

        private StatusLoopPool()
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("StatusLoopPoolRoot");
                _poolRoot = go.transform;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Obtient une instance (dépile ou Instantiate), parentée en local zero.
        /// </summary>
        public ParticleSystem Get(ParticleSystem prefab, Transform parent)
        {
            if (prefab == null)
                return null;

            if (!_stacks.TryGetValue(prefab, out Stack<ParticleSystem> stack))
            {
                stack = new Stack<ParticleSystem>(4);
                _stacks[prefab] = stack;
            }

            ParticleSystem instance = stack.Count > 0
                ? stack.Pop()
                : Object.Instantiate(prefab);

            _instanceToPrefab[instance] = prefab;

            Transform t = instance.transform;
            t.SetParent(parent, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;

            instance.gameObject.SetActive(true);
            instance.Play(true);
            _activeCount++;
            return instance;
        }

        /// <summary>
        /// Stoppe, clear, reparente au root pool et empile.
        /// </summary>
        public void Release(ParticleSystem instance)
        {
            if (instance == null)
                return;

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Transform t = instance.transform;
            t.SetParent(_poolRoot, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            instance.gameObject.SetActive(false);

            if (_activeCount > 0)
                _activeCount--;

            if (!_instanceToPrefab.TryGetValue(instance, out ParticleSystem prefab) || prefab == null)
                return;

            if (!_stacks.TryGetValue(prefab, out Stack<ParticleSystem> stack))
            {
                stack = new Stack<ParticleSystem>(4);
                _stacks[prefab] = stack;
            }

            stack.Push(instance);
        }
    }
}
