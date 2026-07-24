using UnityEngine;
using UnityEngine.SceneManagement;
using ChezArthur.BossRush;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gacha;
using ChezArthur.Gameplay;
using ChezArthur.Meta;
using ChezArthur.Roguelike;

namespace ChezArthur.Missions
{
    /// <summary>
    /// Bridge événements gameplay / meta → MissionManager.
    /// Se rebind à chaque chargement de scène (RunManager, CombatManager, etc.).
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class MissionTriggerRelay : MonoBehaviour
    {
        private const int UniverseBlockSize = 20;

        private MissionManager _manager;
        private RunManager _run;
        private CombatManager _combat;
        private SuperLancerSystem _superLancer;
        private ValiseManager _valises;
        private ItemManager _items;
        private SynergyManager _synergies;
        private GachaManager _gacha;
        private bool _gachaBound;
        private bool _sceneHooked;
        private bool _specHooked;

        public void Bind(MissionManager manager)
        {
            _manager = manager;
            if (!_sceneHooked)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                _sceneHooked = true;
            }

            if (!_specHooked)
            {
                CharacterBall.OnAnySpecSwitchedInCombat += HandleSpecSwitch;
                _specHooked = true;
            }

            BindSceneSources();
            BindMetaSources();
        }

        public void Teardown()
        {
            if (_sceneHooked)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                _sceneHooked = false;
            }

            if (_specHooked)
            {
                CharacterBall.OnAnySpecSwitchedInCombat -= HandleSpecSwitch;
                _specHooked = false;
            }

            UnbindSceneSources();
            UnbindMetaSources();
            _manager = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BindSceneSources();
        }

        private void BindMetaSources()
        {
            if (_gachaBound)
                return;

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null || pm.Gacha == null)
                return;

            _gacha = pm.Gacha;
            _gacha.OnPullCompleted += HandlePullCompleted;
            _gachaBound = true;
        }

        private void UnbindMetaSources()
        {
            if (!_gachaBound || _gacha == null)
                return;

            _gacha.OnPullCompleted -= HandlePullCompleted;
            _gacha = null;
            _gachaBound = false;
        }

        private void BindSceneSources()
        {
            UnbindSceneSources();

            _run = RunManager.Instance;
            if (_run != null)
            {
                _run.OnRunStarted += HandleRunStarted;
                _run.OnStageReached += HandleStageReached;
                _run.OnStageCompleted += HandleStageCompleted;
            }

            _combat = Object.FindObjectOfType<CombatManager>();
            if (_combat != null)
                _combat.OnEnemyDeath += HandleEnemyDeath;

            _superLancer = SuperLancerSystem.Instance;
            if (_superLancer != null)
                _superLancer.OnSuperLancer += HandleSuperLancer;

            _valises = ValiseManager.Instance;
            if (_valises != null)
                _valises.OnValiseAdded += HandleValiseAdded;

            _items = ItemManager.Instance;
            if (_items != null)
                _items.OnItemAdded += HandleItemAdded;

            _synergies = SynergyManager.Instance;
            if (_synergies != null)
                _synergies.OnSynergyActivated += HandleSynergyActivated;
        }

        private void UnbindSceneSources()
        {
            if (_run != null)
            {
                _run.OnRunStarted -= HandleRunStarted;
                _run.OnStageReached -= HandleStageReached;
                _run.OnStageCompleted -= HandleStageCompleted;
                _run = null;
            }

            if (_combat != null)
            {
                _combat.OnEnemyDeath -= HandleEnemyDeath;
                _combat = null;
            }

            if (_superLancer != null)
            {
                _superLancer.OnSuperLancer -= HandleSuperLancer;
                _superLancer = null;
            }

            if (_valises != null)
            {
                _valises.OnValiseAdded -= HandleValiseAdded;
                _valises = null;
            }

            if (_items != null)
            {
                _items.OnItemAdded -= HandleItemAdded;
                _items = null;
            }

            if (_synergies != null)
            {
                _synergies.OnSynergyActivated -= HandleSynergyActivated;
                _synergies = null;
            }
        }

        private void HandleRunStarted()
        {
            _manager?.NotifyRunStarted();
        }

        private void HandleStageReached(int stage)
        {
            _manager?.ReportStageReached(stage);
        }

        private void HandleStageCompleted(int completedStage)
        {
            int local = ((completedStage - 1) % UniverseBlockSize) + 1;
            if (local != UniverseBlockSize)
                return;

            int logical = SeasonRotationManager.GetLogicalUniverseForStage(completedStage);
            _manager?.ReportUniverseCompleted(logical);
        }

        private void HandleEnemyDeath(Enemy enemy)
        {
            _manager?.ReportCounter(MissionTriggerType.EnemyKilled, 1);

            // Unlock roster Boss Rush uniquement en run normale.
            if (RunManager.Instance != null && RunManager.Instance.IsBossRush)
                return;

            if (enemy != null && enemy.Data != null)
                BossRushManager.Instance?.TryRegisterFirstKill(enemy.Data);
        }

        private void HandleSuperLancer(CharacterBall ball)
        {
            _manager?.ReportCounter(MissionTriggerType.SuperLancerSuccess, 1);
        }

        private void HandleValiseAdded(ValiseInstance instance)
        {
            _manager?.ReportCounter(MissionTriggerType.ValiseObtained, 1);
        }

        private void HandleItemAdded(ItemInstance instance)
        {
            _manager?.ReportCounter(MissionTriggerType.ItemObtained, 1);
        }

        private void HandleSpecSwitch(CharacterBall ball)
        {
            _manager?.NotifySpecSwitchInCombat();
        }

        private void HandleSynergyActivated(SynergyData data)
        {
            _manager?.NotifySynergyActivated();
        }

        private void HandlePullCompleted(GachaPullResult result)
        {
            if (_manager == null || result == null)
                return;

            _manager.ReportCounter(MissionTriggerType.GachaPull, 1);

            if (result.characters == null)
                return;

            for (int i = 0; i < result.characters.Count; i++)
            {
                PulledCharacter pulled = result.characters[i];
                if (pulled == null)
                    continue;
                if (pulled.isNew)
                    _manager.ReportCharacterObtained(pulled.rarity);
            }
        }
    }
}
