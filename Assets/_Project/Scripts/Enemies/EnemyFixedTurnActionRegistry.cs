using System;
using System.Collections;
using System.Collections.Generic;

namespace ChezArthur.Enemies
{
    /// <summary>
    /// Actions de tour des ennemis FIXES (R2). Un handler (archere_branches, garde…)
    /// enregistre sa coroutine d'action dans Initialize et se retire dans Cleanup.
    /// EnemyAI la joue à la place du drag ; sans action : placeholder G4-P3.
    /// </summary>
    public static class EnemyFixedTurnActionRegistry
    {
        private static readonly Dictionary<Enemy, Func<IEnumerator>> _actions =
            new Dictionary<Enemy, Func<IEnumerator>>(8);

        private static readonly List<Enemy> _purgeKeys = new List<Enemy>(8);

        public static void Register(Enemy enemy, Func<IEnumerator> actionFactory)
        {
            if (enemy == null || actionFactory == null)
                return;
            _actions[enemy] = actionFactory;
        }

        public static void Unregister(Enemy enemy)
        {
            if (enemy == null)
                return;
            _actions.Remove(enemy);
        }

        public static bool TryGet(Enemy enemy, out Func<IEnumerator> actionFactory)
        {
            actionFactory = null;
            PurgeStale();

            if (enemy == null || enemy.IsDead)
                return false;

            return _actions.TryGetValue(enemy, out actionFactory) && actionFactory != null;
        }

        private static void PurgeStale()
        {
            _purgeKeys.Clear();
            foreach (KeyValuePair<Enemy, Func<IEnumerator>> kv in _actions)
            {
                if (kv.Key == null || kv.Key.IsDead || kv.Value == null)
                    _purgeKeys.Add(kv.Key);
            }

            for (int i = 0; i < _purgeKeys.Count; i++)
                _actions.Remove(_purgeKeys[i]);
        }
    }
}
