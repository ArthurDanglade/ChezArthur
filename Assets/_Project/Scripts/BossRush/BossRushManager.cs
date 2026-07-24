using System;
using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Missions;
using UnityEngine;

namespace ChezArthur.BossRush
{
    /// <summary>
    /// Roster Boss Rush : first-kill (Boss / MiniBoss / Elite), stats de base en combat.
    /// Compteur « majeurs » = EnemyRole.Boss uniquement (score / missions N boss).
    /// </summary>
    public class BossRushManager : MonoBehaviour
    {
        public static BossRushManager Instance { get; private set; }

        [Header("Résolution data")]
        [Tooltip("Pool global pour résoudre un enemy id → EnemyData (souvent le même que StageGenerator).")]
        [SerializeField] private List<EnemyData> enemyCatalog = new List<EnemyData>();

        private readonly List<string> _roster = new List<string>(32);
        private readonly List<string> _majorIds = new List<string>(24);
        private readonly HashSet<string> _rosterSet = new HashSet<string>();
        private readonly HashSet<string> _majorSet = new HashSet<string>();
        private readonly HashSet<string> _weeklyDistinctKills = new HashSet<string>();
        private bool _unlocked;

        public bool IsUnlocked => _unlocked;
        public IReadOnlyList<string> RosterIds => _roster;
        public IReadOnlyList<string> MajorBossIds => _majorIds;
        public int RosterCount => _roster.Count;
        public int MajorUnlockedCount => _majorIds.Count;

        public event Action OnRosterChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            LoadFromPersistent();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// True si l'ennemi est éligible au roster (boss / miniboss / élite).
        /// Préfère EnemyRole ; Elite via EnemyType.
        /// </summary>
        public static bool IsRushEligible(EnemyData data)
        {
            if (data == null)
                return false;

            if (data.EnemyRole == EnemyRole.Boss || data.EnemyRole == EnemyRole.MiniBoss)
                return true;

            return data.EnemyType == EnemyType.MobElite
                   || data.EnemyType == EnemyType.Boss
                   || data.EnemyType == EnemyType.MiniBoss;
        }

        public static bool IsMajorBoss(EnemyData data)
        {
            if (data == null)
                return false;
            return data.EnemyRole == EnemyRole.Boss || data.EnemyType == EnemyType.Boss;
        }

        /// <summary>
        /// First-kill en run normale : unlock + append roster (stats de base en rush).
        /// </summary>
        public bool TryRegisterFirstKill(EnemyData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id))
                return false;
            if (!IsRushEligible(data))
                return false;

            bool changed = false;

            if (!_unlocked)
            {
                _unlocked = true;
                changed = true;
            }

            if (_rosterSet.Add(data.Id))
            {
                _roster.Add(data.Id);
                changed = true;
                Debug.Log($"[BossRush] Nouveau roster : {data.Id} ({data.EnemyName})");
            }

            if (IsMajorBoss(data) && _majorSet.Add(data.Id))
            {
                _majorIds.Add(data.Id);
                changed = true;
            }

            if (!changed)
                return false;

            Persist();
            OnRosterChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Kill en mode Boss Rush : mission hebdo (boss distincts cette semaine).
        /// </summary>
        public void NotifyRushKill(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId))
                return;

            if (!_weeklyDistinctKills.Add(enemyId))
                return;

            PersistWeeklyKills();
            MissionManager.Instance?.ReportCounter(MissionTriggerType.BossRushBossDefeated, 1);
        }

        public void ClearWeeklyDistinctKills()
        {
            _weeklyDistinctKills.Clear();
            PersistWeeklyKills();
        }

        public EnemyData ResolveEnemyData(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId) || enemyCatalog == null)
                return null;

            for (int i = 0; i < enemyCatalog.Count; i++)
            {
                EnemyData d = enemyCatalog[i];
                if (d != null && d.Id == enemyId)
                    return d;
            }

            return null;
        }

        public void LoadFromPersistent()
        {
            Core.PersistentManager pm = Core.PersistentManager.Instance;
            if (pm == null)
                return;

            _unlocked = pm.BossRushUnlocked;
            _roster.Clear();
            _rosterSet.Clear();
            _majorIds.Clear();
            _majorSet.Clear();

            IReadOnlyList<string> saved = pm.BossRushEnemyIds;
            for (int i = 0; i < saved.Count; i++)
            {
                string id = saved[i];
                if (string.IsNullOrEmpty(id) || !_rosterSet.Add(id))
                    continue;
                _roster.Add(id);
            }

            IReadOnlyList<string> majors = pm.BossRushMajorBossIds;
            for (int i = 0; i < majors.Count; i++)
            {
                string id = majors[i];
                if (string.IsNullOrEmpty(id) || !_majorSet.Add(id))
                    continue;
                _majorIds.Add(id);
            }

            _weeklyDistinctKills.Clear();
            IReadOnlyList<string> weekly = pm.BossRushWeeklyCountedIds;
            for (int i = 0; i < weekly.Count; i++)
            {
                if (!string.IsNullOrEmpty(weekly[i]))
                    _weeklyDistinctKills.Add(weekly[i]);
            }
        }

        private void Persist()
        {
            Core.PersistentManager pm = Core.PersistentManager.Instance;
            if (pm == null)
                return;

            if (_unlocked)
                pm.UnlockBossRush();

            pm.SetBossRushRoster(new List<string>(_roster), new List<string>(_majorIds));
            pm.SetBossRushWeeklyCountedIds(new List<string>(_weeklyDistinctKills));
            pm.SaveGame();
        }

        private void PersistWeeklyKills()
        {
            Core.PersistentManager pm = Core.PersistentManager.Instance;
            if (pm == null)
                return;

            pm.SetBossRushWeeklyCountedIds(new List<string>(_weeklyDistinctKills));
            pm.SaveGame();
        }

#if UNITY_EDITOR
        public void EditorSetCatalog(List<EnemyData> catalog)
        {
            enemyCatalog = catalog != null ? new List<EnemyData>(catalog) : new List<EnemyData>();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
