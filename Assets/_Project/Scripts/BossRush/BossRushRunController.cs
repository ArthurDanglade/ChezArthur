using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.BossRush
{
    /// <summary>
    /// Orchestration d'une run Boss Rush : gauntlet 1v1, PV/KO conservés, pas de roguelike.
    /// </summary>
    public class BossRushRunController : MonoBehaviour
    {
        public static BossRushRunController Instance { get; private set; }

        [SerializeField] private StageGenerator stageGenerator;
        [SerializeField] private float delayBetweenEncounters = 0.75f;

        private readonly List<string> _queue = new List<string>(32);
        private int _index;
        private bool _busy;
        private string _currentEnemyId;

        public bool IsActive { get; private set; }
        public int CurrentIndex => _index;
        public int TotalCount => _queue.Count;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Démarre le gauntlet après spawn d'équipe (appelé par RunManager).
        /// </summary>
        public void BeginFromRoster()
        {
            _queue.Clear();
            _index = 0;
            _busy = false;
            _currentEnemyId = null;
            IsActive = true;

            BossRushManager mgr = BossRushManager.Instance;
            if (mgr == null || mgr.RosterCount == 0)
            {
                Debug.LogWarning("[BossRush] Roster vide — abandon.");
                IsActive = false;
                RunManager.Instance?.EndRun(false);
                return;
            }

            IReadOnlyList<string> roster = mgr.RosterIds;
            for (int i = 0; i < roster.Count; i++)
            {
                if (!string.IsNullOrEmpty(roster[i]))
                    _queue.Add(roster[i]);
            }

            Debug.Log($"[BossRush] Gauntlet démarré — {_queue.Count} ennemi(s).");
            SpawnCurrentEncounter();
        }

        /// <summary>
        /// Victoire d'un combat (tous ennemis morts).
        /// </summary>
        public void OnEncounterCleared()
        {
            if (!IsActive || _busy)
                return;

            if (!string.IsNullOrEmpty(_currentEnemyId))
                BossRushManager.Instance?.NotifyRushKill(_currentEnemyId);

            _index++;
            if (_index >= _queue.Count)
            {
                IsActive = false;
                Debug.Log("[BossRush] Gauntlet terminé — victoire.");
                RunManager.Instance?.EndRun(true);
                return;
            }

            StartCoroutine(AdvanceToNext());
        }

        public void Abort()
        {
            IsActive = false;
            _busy = false;
            _queue.Clear();
        }

        private IEnumerator AdvanceToNext()
        {
            _busy = true;
            // Pas de heal : PV et KO conservés. Juste un court délai + reset tours.
            if (delayBetweenEncounters > 0f)
                yield return new WaitForSecondsRealtime(delayBetweenEncounters);

            if (RunManager.Instance != null)
                RunManager.Instance.PrepareNextBossRushEncounter();

            SpawnCurrentEncounter();
            _busy = false;
        }

        private void SpawnCurrentEncounter()
        {
            if (_index < 0 || _index >= _queue.Count)
                return;

            string id = _queue[_index];
            _currentEnemyId = id;

            EnemyData data = null;
            if (BossRushManager.Instance != null)
                data = BossRushManager.Instance.ResolveEnemyData(id);

            if (data == null && stageGenerator != null)
                data = stageGenerator.FindEnemyDataById(id);

            if (data == null)
            {
                Debug.LogError($"[BossRush] EnemyData introuvable : {id} — skip.");
                _index++;
                if (_index >= _queue.Count)
                {
                    IsActive = false;
                    RunManager.Instance?.EndRun(true);
                }
                else
                    SpawnCurrentEncounter();
                return;
            }

            if (stageGenerator != null)
                stageGenerator.GenerateBossRushEncounter(data);

            // Compteur d'étage affiché = index+1 pour le HUD / missions hors rush.
            if (RunManager.Instance != null)
                RunManager.Instance.SetBossRushDisplayIndex(_index + 1, _queue.Count);
        }
    }
}
