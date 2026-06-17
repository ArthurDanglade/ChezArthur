using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Passives.Handlers;
using ChezArthur.Roguelike;
using ChezArthur.UI;

namespace ChezArthur.Core
{
    /// <summary>
    /// État d'une run roguelike.
    /// </summary>
    public enum RunState
    {
        /// <summary> Run pas encore démarrée. </summary>
        NotStarted,

        /// <summary> Run en cours (étages, Tals). </summary>
        InProgress,

        /// <summary> Run terminée en victoire. </summary>
        Victory,

        /// <summary> Run terminée en défaite. </summary>
        Defeat
    }

    /// <summary>
    /// Gère une run roguelike complète : étages, Tals, victoire/défaite. Singleton sans DontDestroyOnLoad (reset à chaque partie).
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private StageGenerator stageGenerator;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private CharacterBallFactory characterBallFactory;
        [SerializeField] private Arena arena;

        [Header("Positions de spawn des alliés")]
        [SerializeField] private List<Vector2> allySpawnPositions = new List<Vector2>
        {
            new Vector2(-3f, -6f),
            new Vector2(-1f, -6f),
            new Vector2(1f, -6f),
            new Vector2(3f, -6f)
        };

        [Header("Régénération entre étages")]
        [SerializeField] [Range(0f, 1f)] private float healPercentBetweenStages = 0.1f; // 10%

        [Header("UI Bonus")]
        [SerializeField] private BonusSelectionUI bonusSelectionUI;
        [SerializeField] private StageAnnouncerUI stageAnnouncerUI;
        [SerializeField] private SacrificeUIBridge sacrificeUIBridge;
        [Header("Mode sélection bonus")]
        [SerializeField] private bool useRoguelikePoolForBonusSelection;

        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int BONUS_SELECTION_INTERVAL = 3;
        private const int GARE_UNIVERSE_INTERVAL = 20;
        private const int POST_GAME_STAGE_START = 101;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static RunManager _instance;
        private RunState _currentState = RunState.NotStarted;
        private int _currentStage = 1;
        private int _talsEarned;
        private bool _talsBanked;
        private int _lastPostGameGareBlock = -1;
        private List<CharacterBall> _spawnedAllies = new List<CharacterBall>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Instance unique du RunManager (scène courante, pas DontDestroyOnLoad). </summary>
        public static RunManager Instance => _instance;

        /// <summary> État actuel de la run. </summary>
        public RunState CurrentState => _currentState;

        /// <summary> Étage actuel (commence à 1). </summary>
        public int CurrentStage => _currentStage;

        /// <summary> Tals du pool de run (gains et dépenses Gare). </summary>
        public int TalsEarned => _talsEarned;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché au démarrage d'une run. </summary>
        public event Action OnRunStarted;

        /// <summary> Déclenché quand le total de Tals change. Paramètre : nouveau total. </summary>
        public event Action<int> OnTalsChanged;

        /// <summary> Déclenché quand un étage est complété. Paramètre : numéro de l'étage complété. </summary>
        public event Action<int> OnStageCompleted;

        /// <summary> Déclenché à la fin de la run. Paramètre : true = victoire, false = défaite. </summary>
        public event Action<bool> OnRunEnded;

        /// <summary> Déclenché quand une Gare doit s'ouvrir entre deux étages. </summary>
        public event Action OnGareRequired;
        /// <summary> Déclenché quand l'item Pacte de Sang demande un sacrifice. </summary>
        public event Action<IReadOnlyList<CharacterBall>> OnPacteDeSangRequired;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (combatManager != null)
            {
                combatManager.OnVictory += CompleteStage;
                combatManager.OnDefeat += HandleDefeat;
            }
        }

        private void Start()
        {
            // Temporaire : démarre automatiquement la run pour le prototype
            StartRun();
        }

        private void OnDestroy()
        {
            if (combatManager != null)
            {
                combatManager.OnVictory -= CompleteStage;
                combatManager.OnDefeat -= HandleDefeat;
            }
            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Démarre une nouvelle run (reset complet : stage, pool Tals, alliés, état).
        /// </summary>
        public void StartRun()
        {
            _currentStage = 1;
            _talsEarned = 0;
            _talsBanked = false;
            _lastPostGameGareBlock = -1;
            _currentState = RunState.InProgress;

            // Remet le jeu en état Playing
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);

            // Reset les bonus en début de run
            if (BonusManager.Instance != null)
                BonusManager.Instance.Initialize();
            if (bonusSelectionUI != null)
                bonusSelectionUI.SetUseRoguelikePool(useRoguelikePoolForBonusSelection);

            // Initialise les slots et la mémoire valises pour la run
            if (ValiseManager.Instance != null)
                ValiseManager.Instance.Initialize();

            // Initialise les slots et l'historique des items pour la run
            if (ItemManager.Instance != null)
                ItemManager.Instance.Initialize();

            // Initialise les compteurs de soins de la Gare pour la run
            if (GareManager.Instance != null)
                GareManager.Instance.Initialize();

            // Initialise le pont UI de sacrifice
            if (sacrificeUIBridge != null)
                sacrificeUIBridge.Initialize();

            // Détruit les anciennes balles spawnées (si re-run)
            for (int i = _spawnedAllies.Count - 1; i >= 0; i--)
            {
                if (_spawnedAllies[i] != null)
                    Destroy(_spawnedAllies[i].gameObject);
            }
            _spawnedAllies.Clear();

            // Récupère l'équipe depuis PersistentManager et spawn, ou fallback sur initialTeam
            List<CharacterBall> alliesToUse = null;
            if (characterBallFactory != null && PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                var team = PersistentManager.Instance.Characters.GetSelectedTeam();
                if (team != null && team.Count > 0)
                {
                    int teamCount = team.Count;
                    List<Vector2> dynamicPositions = GetAllySpawnPositions(teamCount);
                    List<CharacterBall> spawned = characterBallFactory.SpawnTeam(team, dynamicPositions, turnManager);
                    if (spawned != null && spawned.Count > 0)
                    {
                        _spawnedAllies.AddRange(spawned);
                        alliesToUse = spawned;
                    }
                }
            }

            if (alliesToUse == null)
                Debug.LogWarning("[RunManager] PersistentManager, équipe vide ou factory manquante : utilisation de l'équipe par défaut (initialAllies).");

            if (turnManager != null)
            {
                if (alliesToUse != null)
                    turnManager.Initialize(alliesToUse);
                else
                    turnManager.Initialize();

                turnManager.ReviveAllAllies();
                int allyCount = _spawnedAllies.Count;
                turnManager.ResetAlliesPositions(GetAllySpawnPositions(allyCount));

                if (PoisonTickSystem.Instance != null)
                    PoisonTickSystem.Instance.Initialize(turnManager);

                if (FreezeSystem.Instance != null)
                    FreezeSystem.Instance.Initialize(turnManager);

                if (StunSystem.Instance != null)
                    StunSystem.Instance.Initialize(turnManager);

                if (BurnTickSystem.Instance != null)
                    BurnTickSystem.Instance.Initialize(turnManager);
            }

            // Les bridges doivent s'initialiser APRÈS le spawn des alliés (abonnements par-allié).
            if (ItemEventBridge.Instance != null)
                ItemEventBridge.Instance.Initialize(turnManager);
            if (ValiseEventBridge.Instance != null)
                ValiseEventBridge.Instance.Initialize(turnManager);

            // Initialise les passifs de chaque allié selon sa spé et son niveau
            InitializeAlliesPassives();

            // Même logique que les étages suivants : OnStageStart + reset stacks (ZoneSystem, etc.)
            if (turnManager != null)
                ResetAlliesPassivesForNewStage();

            // Génère le premier étage
            if (stageGenerator != null)
                stageGenerator.GenerateStage(_currentStage);

            NotifyItemStageStart();

            OnRunStarted?.Invoke();
        }

        /// <summary>
        /// Ajoute des Tals au pool de run.
        /// Les multiplicateurs (salle spéciale, valise difficulté) sont appliqués en amont par l'appelant.
        /// </summary>
        public void AddTals(int amount)
        {
            _talsEarned += amount;
            OnTalsChanged?.Invoke(TalsEarned);
        }

        /// <summary>
        /// Déduit des Tals du pool de run. Retourne false si fonds insuffisants.
        /// </summary>
        public bool SpendTals(int amount)
        {
            if (amount <= 0 || _talsEarned < amount)
                return false;

            _talsEarned -= amount;
            OnTalsChanged?.Invoke(TalsEarned);
            return true;
        }

        /// <summary>
        /// Applique un soin à toute l'équipe vivante. Délègue au TurnManager.
        /// </summary>
        public void HealTeam(float ratio)
        {
            if (turnManager != null)
                turnManager.HealAllAllies(ratio);
        }

        /// <summary>
        /// True si au moins un allié vivant n'est pas full HP.
        /// </summary>
        public bool CanHealTeam()
        {
            if (turnManager == null)
                return false;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return false;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                if (ally.CurrentHp < ally.MaxHp)
                    return true;
            }

            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Relance une run via StartRun() (init complète normale), puis saute à l'étage demandé
        /// en régénérant l'étage pour remplacer celui créé par défaut à l'étage 1.
        /// </summary>
        public void DebugRestartRunAtStage(int stage)
        {
            StartRun();

            if (stage <= 1 || stageGenerator == null)
                return;

            _currentStage = stage;
            stageGenerator.GenerateStage(_currentStage);
        }
#endif

        /// <summary>
        /// Appelé quand tous les ennemis sont morts (victoire d'étage).
        /// Gare prioritaire sur le bonus, puis sélection bonus, sinon étage suivant.
        /// </summary>
        public void CompleteStage()
        {
            int completedStage = _currentStage;
            _currentStage++;
            OnStageCompleted?.Invoke(completedStage);

            if (ShouldOpenGare(completedStage))
            {
                if (GareManager.Instance != null)
                {
                    Debug.Log($"[Gare] Ouverture étage {completedStage}");
                    GareManager.Instance.GenerateGare();
                    Debug.Log($"[Gare] Slots générés : {GareManager.Instance.GetCurrentSlots().Count}");
                    OnGareRequired?.Invoke();
                    return;
                }
            }

            // Bonus au premier étage (hook) puis tous les 3 étages à partir de l'étage 4
            bool isFirstStageHook = (completedStage == 1);
            bool isRegularBonusStage = (completedStage > 1 && (completedStage - 1) % BONUS_SELECTION_INTERVAL == 0);

            if (isFirstStageHook || isRegularBonusStage)
            {
                if (bonusSelectionUI != null)
                {
                    bonusSelectionUI.OnSelectionComplete += OnBonusSelectionComplete;
                    bonusSelectionUI.Show();
                    if (stageAnnouncerUI != null)
                        stageAnnouncerUI.Hide();
                    return;
                }
            }

            ContinueToNextStage();
        }

        /// <summary>
        /// Appelé par GareManager quand le joueur ferme la Gare.
        /// </summary>
        public void OnGareClosed()
        {
            ContinueToNextStage();
        }

        /// <summary>
        /// Appelé quand le joueur a choisi son bonus.
        /// </summary>
        private void OnBonusSelectionComplete()
        {
            if (bonusSelectionUI != null)
                bonusSelectionUI.OnSelectionComplete -= OnBonusSelectionComplete;

            ContinueToNextStage();
        }

        /// <summary>
        /// Continue vers l'étage suivant (repositionnement, heal, génération).
        /// </summary>
        private void ContinueToNextStage()
        {
            JuiceDirector.Instance?.ResetForNewStage();

            // Bloque les changements de tour pendant la transition
            if (turnManager != null)
                turnManager.SetTurnChangeEnabled(false);

            // Remet le jeu en état Playing pour l'étage suivant
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);

            // Repositionne les alliés vivants
            if (turnManager != null)
            {
                IReadOnlyList<CharacterBall> currentAllies = turnManager.GetAllies();
                int livingCount = 0;
                if (currentAllies != null)
                {
                    for (int i = 0; i < currentAllies.Count; i++)
                    {
                        if (currentAllies[i] != null && !currentAllies[i].IsDead)
                            livingCount++;
                    }
                }
                turnManager.ResetAlliesPositions(
                    GetAllySpawnPositions(Mathf.Max(1, livingCount)));
            }

            // Régénère les HP des alliés vivants (+ bonus « Bonne étoile » Elfert si présent dans l'équipe)
            float elfertHealBonus = 0f;
            if (turnManager != null)
            {
                IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
                if (allies != null)
                {
                    for (int i = 0; i < allies.Count; i++)
                    {
                        if (allies[i] == null || allies[i].IsDead) continue;
                        if (allies[i].Data != null && allies[i].Data.Id == "elfert")
                        {
                            elfertHealBonus = 0.05f;
                            break;
                        }
                    }
                }
            }

            float itemHealBonus = ItemManager.Instance != null
                ? ItemManager.Instance.GetHealBetweenStagesBonus()
                : 0f;
            float totalHeal = healPercentBetweenStages + elfertHealBonus + itemHealBonus;
            // Effet niv20 Vitalité : régénération entre étages x2.
            if (ValiseManager.Instance != null)
            {
                ValiseInstance vitalite = ValiseManager.Instance.GetActiveValise("valise_vitalite");
                if (vitalite != null && vitalite.IsLevel20Unlocked)
                    totalHeal *= 2f;
            }
            if (turnManager != null)
                turnManager.HealAllAllies(totalHeal);

            // Reset les stacks des passifs qui se reset par étage + Notify OnStageStart
            ResetAlliesPassivesForNewStage();
            // Effet niv20 Vitesse : l'allié le plus rapide joue 2x au premier cycle.
            ApplyVitesseLv20IfActive();

            // Reset l'ordre des tours (le plus rapide recommence)
            if (turnManager != null)
                turnManager.ResetTurnOrder();

            // Génère l'étage suivant
            if (stageGenerator != null)
                stageGenerator.GenerateStage(_currentStage);

            // Reset Frénésie après génération : le kill final de l'étage précédent
            // (OnKillEnemy) doit s'exécuter avant ce reset.
            if (ValiseEventBridge.Instance != null)
                ValiseEventBridge.Instance.NotifyStageStart();

            NotifyItemStageStart();

            // Réactive les changements de tour après un court délai (laisse le temps au personnage de s'arrêter)
            Invoke(nameof(ReenableTurnChange), 0.5f);
        }

        /// <summary>
        /// Verse le pool de run vers le hub (idempotent — un seul crédit par run).
        /// </summary>
        public void BankRunTals()
        {
            if (_talsBanked)
                return;

            if (_talsEarned > 0 && PersistentManager.Instance != null)
                PersistentManager.Instance.AddTals(_talsEarned);

            _talsBanked = true;
        }

        /// <summary>
        /// Termine la run (victoire ou défaite).
        /// </summary>
        public void EndRun(bool victory)
        {
            Debug.Log($"[RunManager] EndRun appelé, victory = {victory}");

            BankRunTals();

            _currentState = victory ? RunState.Victory : RunState.Defeat;
            OnRunEnded?.Invoke(victory);
        }

        /// <summary>
        /// Demande l'ouverture du flux UI de sacrifice pour Pacte de Sang.
        /// </summary>
        public void RequestPacteDeSang(IReadOnlyList<CharacterBall> allies)
        {
            OnPacteDeSangRequired?.Invoke(allies);
        }

        /// <summary>
        /// Demande un tour fantôme pour un allié.
        /// </summary>
        public void RequestGhostTurn(CharacterBall ally)
        {
            if (ally == null || turnManager == null) return;
            turnManager.RequestGhostTurn(ally);
        }

        /// <summary>
        /// Repositionne les alliés vivants sur les emplacements de spawn de l'arène.
        /// </summary>
        public void RepositionAlliesAtSpawn()
        {
            if (turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            int count = allies != null ? allies.Count : 0;
            turnManager.ResetAlliesPositions(GetAllySpawnPositions(Mathf.Max(1, count)));
        }

        /// <summary>
        /// Confirme le sacrifice Pacte de Sang — l'allié choisi meurt,
        /// les survivants reçoivent les bonus.
        /// </summary>
        public void ConfirmPacteDeSang(int allyIndex, float bonusValue)
        {
            if (turnManager == null) return;
            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null || allyIndex < 0 || allyIndex >= allies.Count) return;

            CharacterBall sacrifice = allies[allyIndex];
            if (sacrifice == null || sacrifice.IsDead) return;

            sacrifice.Die();

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally == sacrifice) continue;
                if (ally.BuffReceiver == null) continue;

                ally.BuffReceiver.AddBuff(new BuffData {
                    BuffId = "pacte_de_sang_atk", Source = null,
                    StatType = BuffStatType.ATK, Value = bonusValue,
                    IsPercent = true, RemainingTurns = -1, RemainingCycles = -1
                });
                ally.BuffReceiver.AddBuff(new BuffData {
                    BuffId = "pacte_de_sang_def", Source = null,
                    StatType = BuffStatType.DEF, Value = bonusValue,
                    IsPercent = true, RemainingTurns = -1, RemainingCycles = -1
                });
                ally.BuffReceiver.AddBuff(new BuffData {
                    BuffId = "pacte_de_sang_hp", Source = null,
                    StatType = BuffStatType.HP, Value = bonusValue,
                    IsPercent = true, RemainingTurns = -1, RemainingCycles = -1
                });
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void NotifyItemStageStart()
        {
            if (ItemManager.Instance == null || ItemEffectRegistry.Instance == null || turnManager == null)
                return;

            ItemEffectContext context = ItemEffectRegistry.Instance.GetSharedContext();
            context.TurnManager = turnManager;
            ItemManager.Instance.NotifyStageStart(context);
        }

        /// <summary>
        /// Calcule les positions de spawn des alliés dans
        /// le tiers bas de l'arène, réparties horizontalement.
        /// Toujours à l'intérieur des bounds actuels de l'arène.
        /// </summary>
        private List<Vector2> GetAllySpawnPositions(int count)
        {
            if (arena == null || count <= 0)
                return allySpawnPositions;

            Bounds b = arena.Bounds;

            // Tiers bas de l'arène (entre 10% et 25% de la hauteur)
            float yPos = b.min.y + b.size.y * 0.15f;

            // Répartition horizontale avec marge
            float margin = b.size.x * 0.15f;
            float usableWidth = b.size.x - (margin * 2f);

            var positions = new List<Vector2>(count);

            if (count == 1)
            {
                positions.Add(new Vector2(b.center.x, yPos));
                return positions;
            }

            float spacing = usableWidth / (count - 1);
            float startX = b.min.x + margin;

            for (int i = 0; i < count; i++)
            {
                float x = startX + spacing * i;
                positions.Add(new Vector2(x, yPos));
            }

            return positions;
        }

        private void HandleDefeat()
        {
            Debug.Log("[RunManager] HandleDefeat appelé");
            EndRun(false);
        }

        /// <summary>
        /// Applique l'effet niveau 20 de la valise Vitesse au début de l'étage.
        /// L'allié vivant le plus rapide reçoit un tour bonus pour le premier cycle.
        /// </summary>
        private void ApplyVitesseLv20IfActive()
        {
            if (ValiseManager.Instance == null || turnManager == null) return;
            ValiseInstance vitesse = ValiseManager.Instance.GetActiveValise("valise_vitesse");
            if (vitesse == null || !vitesse.IsLevel20Unlocked) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null || allies.Count == 0) return;

            CharacterBall fastest = null;
            int maxSpeed = int.MinValue;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;
                if (ally.Speed > maxSpeed)
                {
                    maxSpeed = ally.Speed;
                    fastest = ally;
                }
            }

            if (fastest != null)
                fastest.QueueExtraTurn(1);
        }

        private void ReenableTurnChange()
        {
            if (turnManager != null)
                turnManager.SetTurnChangeEnabled(true);
        }

        /// <summary>
        /// Retourne true si l'étage complété doit déclencher une Gare.
        /// </summary>
        private bool ShouldOpenGare(int completedStage)
        {
            // Jeu principal : Gare garantie après chaque boss de fin d'univers (20/40/60/80/100)
            if (completedStage < POST_GAME_STAGE_START)
                return completedStage % GARE_UNIVERSE_INTERVAL == 0;

            // Post-game : 20% par étage, max 1 par bloc de 20 (101-120, 121-140…)
            int block = (completedStage - POST_GAME_STAGE_START) / GARE_UNIVERSE_INTERVAL;
            if (block == _lastPostGameGareBlock)
                return false;

            if (UnityEngine.Random.value < 0.20f)
            {
                _lastPostGameGareBlock = block;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initialise le CharacterPassiveRuntime de chaque allié avec la bonne spécialisation et le bon niveau.
        /// </summary>
        private void InitializeAlliesPassives()
        {
            if (turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null) continue;

                CharacterPassiveRuntime runtime = ally.GetComponent<CharacterPassiveRuntime>();
                if (runtime == null) continue;

                CharacterData charData = ally.Data;
                if (charData == null) continue;

                int level = 1;
                int specIndex = -1;
                if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
                {
                    OwnedCharacter owned = PersistentManager.Instance.Characters.GetOwnedCharacter(charData.Id);
                    if (owned != null)
                    {
                        level = owned.level;
                        specIndex = owned.GetSpecialization();
                    }
                }

                SpecializationData spec = charData.GetSpecialization(specIndex);
                runtime.InitializeForRun(spec, level, specIndex);
            }
        }

        /// <summary>
        /// Reset les stacks des passifs ResetPerStage pour tous les alliés vivants. Notifie également OnStageStart.
        /// </summary>
        private void ResetAlliesPassivesForNewStage()
        {
            if (turnManager == null) return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null) return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead) continue;

                CharacterPassiveRuntime runtime = ally.GetComponent<CharacterPassiveRuntime>();
                if (runtime == null) continue;

                runtime.ResetForNewStage();
                runtime.NotifyTrigger(PassiveTrigger.OnStageStart);
            }
        }
    }
}
