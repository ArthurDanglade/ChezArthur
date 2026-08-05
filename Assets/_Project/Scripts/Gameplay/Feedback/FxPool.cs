using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Pool de ParticleSystem one-shot par prefab (retour via OnParticleSystemStopped).
    /// </summary>
    public class FxPool
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int PrefetchCount = 4;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Transform _poolRoot;
        private readonly Dictionary<ParticleSystem, Stack<ParticleSystem>> _stacks =
            new Dictionary<ParticleSystem, Stack<ParticleSystem>>(8);
        private int _activeCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS
        // ═══════════════════════════════════════════
        public int ActiveCount => _activeCount;

        public FxPool(Transform poolRoot)
        {
            _poolRoot = poolRoot;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        public ParticleSystem Get(ParticleSystem prefab)
        {
            if (prefab == null)
                return null;

            if (!_stacks.TryGetValue(prefab, out Stack<ParticleSystem> stack))
            {
                stack = new Stack<ParticleSystem>(PrefetchCount);
                _stacks[prefab] = stack;
                Prefetch(prefab, stack);
            }

            ParticleSystem instance = stack.Count > 0 ? stack.Pop() : CreateInstance(prefab);
            if (instance == null)
                return null;

            instance.gameObject.SetActive(true);
            PooledFxReturner ret = instance.GetComponent<PooledFxReturner>();
            if (ret != null)
                ret.MarkActive(true);

            _activeCount++;
            return instance;
        }

        public void Release(ParticleSystem instance)
        {
            if (instance == null)
                return;

            PooledFxReturner ret = instance.GetComponent<PooledFxReturner>();
            ParticleSystem prefabKey = ret != null ? ret.PrefabKey : null;
            Vector3 restScale = ret != null ? ret.RestScale : Vector3.one;

            if (ret != null)
                ret.MarkActive(false);

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            Transform t = instance.transform;
            t.SetParent(_poolRoot, false);
            StatusFxSpriteFit.ResetTransform(t);
            // Conserve l'échelle de repos du prefab (pas forcément 1).
            t.localScale = restScale;
            instance.gameObject.SetActive(false);

            if (_activeCount > 0)
                _activeCount--;

            if (prefabKey == null)
                return;

            if (!_stacks.TryGetValue(prefabKey, out Stack<ParticleSystem> stack))
            {
                stack = new Stack<ParticleSystem>(PrefetchCount);
                _stacks[prefabKey] = stack;
            }

            stack.Push(instance);
        }

        public void NotifyDestroyedWhileActive()
        {
            if (_activeCount > 0)
                _activeCount--;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Prefetch(ParticleSystem prefab, Stack<ParticleSystem> stack)
        {
            for (int i = 0; i < PrefetchCount; i++)
            {
                ParticleSystem inst = CreateInstance(prefab);
                if (inst == null)
                    continue;
                inst.gameObject.SetActive(false);
                stack.Push(inst);
            }
        }

        private ParticleSystem CreateInstance(ParticleSystem prefab)
        {
            ParticleSystem instance = Object.Instantiate(prefab, _poolRoot);
            instance.name = prefab.name + "_pooled";

            ParticleSystem.MainModule main = instance.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            main.playOnAwake = false;

            PooledFxReturner ret = instance.GetComponent<PooledFxReturner>();
            if (ret == null)
                ret = instance.gameObject.AddComponent<PooledFxReturner>();

            ret.Bind(this, prefab, instance.transform.localScale);
            return instance;
        }
    }
}
