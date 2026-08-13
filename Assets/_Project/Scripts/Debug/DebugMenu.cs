using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.BossRush;
using ChezArthur.Gacha;
using ChezArthur.Hub.Pages;
using ChezArthur.Meta;
using ChezArthur.Missions;
using ChezArthur.Roguelike;
using ChezArthur.Backend;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ChezArthur.Characters;
using ChezArthur.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ChezArthur.Debugging;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.Gameplay.Feedback;
using ChezArthur.Gameplay.Passives.Handlers;
#endif

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Menu debug IMGUI (dev builds uniquement). Présent en release mais auto-détruit.
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références scène")]
        [SerializeField] private StageGenerator stageGenerator;
        [SerializeField] private TurnManager turnManager;

        [Header("Données (auto-remplies en Editor si vides)")]
        [SerializeField] private List<ValiseData> allValises = new List<ValiseData>();
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();
        [SerializeField] private List<EnemyData> allEnemies = new List<EnemyData>();
        [SerializeField] private List<BannerData> allBanners = new List<BannerData>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float ReferenceWidth = 540f;
        private const int LogBufferSize = 30;
        private const int LogEntryMaxLength = 120;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _panelOpen;
        private bool _inputLockHeld;
        private Vector2 _scrollPosition;
        private Vector2 _logScrollPosition;
        private readonly string[] _logBuffer = new string[LogBufferSize];
        private int _logCount;
        private int _logWriteIndex;
        private string _statusMessage = string.Empty;
        private int _restartStageNumber = 1;
        private int _debugDamageAmount = 50;
        private float _debugHealPercent = 0.10f;
        private string _giveCharacterId = "goat";
        private string _giveCharacterLevel = "1";
        private Vector2 _statesScrollPosition;
        private readonly Dictionary<string, ValiseImprovementRarity> _valiseRarityById =
            new Dictionary<string, ValiseImprovementRarity>();
        private readonly Dictionary<string, int> _valiseUpgradeCountById =
            new Dictionary<string, int>();
        private GUIStyle _panelStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized;
        private Texture2D _debugPortraitTexture;
        private readonly List<(string path, int refCount)> _portraitCacheSnapshot =
            new List<(string path, int refCount)>(16);
#endif

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(gameObject);
#endif
        }

        private void OnEnable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Application.logMessageReceived += OnLogMessageReceived;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Application.logMessageReceived -= OnLogMessageReceived;
            ReleaseInputLockIfHeld();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (allValises == null || allValises.Count == 0)
                allValises = LoadAssets<ValiseData>();
            if (allItems == null || allItems.Count == 0)
                allItems = LoadAssets<ItemData>();
            if (allEnemies == null || allEnemies.Count == 0)
                allEnemies = LoadAssets<EnemyData>();
            if (allBanners == null || allBanners.Count == 0)
                allBanners = LoadAssets<BannerData>();
        }

        private static List<T> LoadAssets<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var list = new List<T>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    list.Add(asset);
            }

            return list;
        }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            float scale = Screen.width / ReferenceWidth;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            float invScale = 1f / scale;
            float screenW = Screen.width * invScale;
            float screenH = Screen.height * invScale;

            InitStylesIfNeeded();

            if (GUI.Button(new Rect(8f, 8f, 56f, 32f), "DBG"))
                TogglePanel();

            if (_panelOpen)
                DrawPanel(screenW, screenH);

            GUI.matrix = previousMatrix;
        }

        private void InitStylesIfNeeded()
        {
            if (_stylesInitialized)
                return;

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(2, 2, new Color(0.08f, 0.08f, 0.12f, 0.96f)) }
            };
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };
            _stylesInitialized = true;
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void TogglePanel()
        {
            _panelOpen = !_panelOpen;
            if (_panelOpen)
            {
                GameplayInputLock.Acquire();
                _inputLockHeld = true;
            }
            else
            {
                ReleaseInputLockIfHeld();
            }
        }

        private void ReleaseInputLockIfHeld()
        {
            if (!_inputLockHeld)
                return;

            GameplayInputLock.Release();
            _inputLockHeld = false;
        }

        private void DrawPanel(float screenW, float screenH)
        {
            float marginX = screenW * 0.1f;
            float marginY = screenH * 0.1f;
            float panelW = screenW - marginX * 2f;
            float panelH = screenH - marginY * 2f;
            Rect panelRect = new Rect(marginX, marginY, panelW, panelH);

            GUILayout.BeginArea(panelRect, _panelStyle);
            GUILayout.Label("DEBUG MENU", GUI.skin.box);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            DrawRunSection();
            GUILayout.Space(8f);
            DrawBackendSection();
            GUILayout.Space(8f);
            DrawMetaSeasonSection();
            GUILayout.Space(8f);
            DrawMissionsSection();
            GUILayout.Space(8f);
            DrawBossRushSection();
            GUILayout.Space(8f);
            DrawCheatsSection();
            GUILayout.Space(8f);
            DrawSaveGachaSection();
            GUILayout.Space(8f);
            DrawPressureSection();
            GUILayout.Space(8f);
            DrawPortraitLoaderSection();
            GUILayout.Space(8f);
            DrawSpecSection();
            GUILayout.Space(8f);
            DrawStatsSection();
            GUILayout.Space(8f);
            DrawDamageSection();
            GUILayout.Space(8f);
            DrawHealSection();
            GUILayout.Space(8f);
            DrawStatesSection();
            GUILayout.Space(8f);
            DrawValisesSection();
            GUILayout.Space(8f);
            DrawItemsSection();
            GUILayout.Space(8f);
            DrawEnemiesSection();
            GUILayout.Space(8f);
            DrawLogSection();

            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                GUILayout.Space(4f);
                GUILayout.Label(_statusMessage, _statusStyle);
            }

            if (GUILayout.Button("Fermer", GUILayout.Height(36f)))
                TogglePanel();

            GUILayout.EndArea();
        }

        private void DrawBackendSection()
        {
            GUILayout.Label("— BACKEND —", GUI.skin.box);

            string playerId = BackendService.PlayerId ?? "";
            string shortId = playerId.Length > 8 ? playerId.Substring(0, 8) : playerId;
            if (string.IsNullOrEmpty(shortId))
                shortId = "—";

            GUILayout.Label(
                $"init={BackendService.IsInitialized} signed={BackendService.IsSignedIn} id={shortId}");

            DateTime guarded = GameClock.UtcNowGuarded;
            DateTime device = DateTime.UtcNow;
            double deltaSec = (guarded - device).TotalSeconds;
            string synced = BackendService.HasServerTime || GameClock.HasServerTime ? "O" : "N";
            GUILayout.Label(
                $"server sync={synced} · guarded={guarded:HH:mm:ss} UTC · device={device:HH:mm:ss} · Δ={deltaSec:+0.0;-0.0;0}s");

            if (BackendService.HasServerTime)
                GUILayout.Label($"dernier sync : {BackendService.LastSyncUtc:yyyy-MM-dd HH:mm:ss} UTC");
            else
                GUILayout.Label("dernier sync : — (garde locale / offline)");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Re-sync temps serveur"))
                BackendService.SyncServerTime();
            if (GUILayout.Button("Ré-init backend"))
                BackendService.ForceReinitialize();
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Run suite G1 (backend/horloge)"))
                BackendIntegritySuite.Run();

            DrawCloudSection();
        }

        private void DrawCloudSection()
        {
            GUILayout.Space(8f);
            GUILayout.Label("— CLOUD —", GUI.skin.box);

            GUILayout.Label(
                $"état={CloudSaveSync.State} conflit={CloudSaveSync.HasPendingConflict}");
            if (CloudSaveSync.LastUploadBytes > 0)
            {
                GUILayout.Label(
                    $"dernier upload : {CloudSaveSync.LastUploadUtc:HH:mm:ss} UTC · {CloudSaveSync.LastUploadBytes} o");
            }
            else
            {
                GUILayout.Label("dernier upload : —");
            }

            string locFp = CloudSaveSync.LastLocalFingerprint;
            string cloudFp = CloudSaveSync.LastCloudFingerprint;
            string locShort = string.IsNullOrEmpty(locFp) ? "—" : (locFp.Length > 8 ? locFp.Substring(0, 8) : locFp);
            string cloudShort = string.IsNullOrEmpty(cloudFp) ? "—" : (cloudFp.Length > 8 ? cloudFp.Substring(0, 8) : cloudFp);
            GUILayout.Label($"fp local={locShort} · cloud={cloudShort}");

            SaveSummary cs = CloudSaveSync.LastCloudSummary;
            GUILayout.Label(
                $"cloud résumé : persos={cs.ownedCount} Tals={cs.tals} ét.={cs.bestStage} scoreS={cs.bestScoreThisSeason}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force upload"))
                _ = CloudSaveSync.UploadAsync(forcePlayerChoice: true);
            if (GUILayout.Button("Force compare"))
                CloudSaveSync.CompareAndResolveAsync();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Wipe cloud"))
            {
                if (!_cloudWipeArmed)
                {
                    _cloudWipeArmed = true;
                    _statusMessage = "Wipe cloud : 2e appui pour confirmer.";
                }
                else
                {
                    _cloudWipeArmed = false;
                    _ = CloudSaveSync.WipeCloudAsync();
                    _statusMessage = "Wipe cloud lancé.";
                }
            }
            if (GUILayout.Button("Simuler conflit"))
                CloudSaveSync.DebugSimulateConflict();
            GUILayout.EndHorizontal();
        }

        private bool _cloudWipeArmed;

        private void DrawRunSection()
        {
            GUILayout.Label("— RUN —", GUI.skin.box);
            RunManager run = RunManager.Instance;
            if (run != null)
            {
                GUILayout.Label($"Étage : {run.CurrentStage}");
                GUILayout.Label($"Tals : {run.TalsEarned}");
            }
            else
            {
                GUILayout.Label("RunManager absent");
            }

            if (GUILayout.Button("Restart run"))
            {
                if (run != null)
                    run.DebugRestartRunAtStage(1);
            }

            GUILayout.BeginHorizontal();
            string stageStr = GUILayout.TextField(_restartStageNumber.ToString(), GUILayout.Width(64f));
            if (int.TryParse(stageStr, out int parsedStage))
                _restartStageNumber = Mathf.Max(1, parsedStage);
            if (GUILayout.Button("Restart à l'étage N"))
            {
                if (run != null)
                    run.DebugRestartRunAtStage(_restartStageNumber);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Skip stage"))
                SkipCurrentStage();

            if (turnManager != null && turnManager.CurrentParticipant != null)
            {
                string side = turnManager.IsPlayerTurn ? "allié" : "ennemi";
                GUILayout.Label($"Tour actuel : {turnManager.CurrentParticipant.Name} ({side})");
            }

            if (GUILayout.Button("Passe tour"))
                DebugSkipCurrentTurn();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("x1"))
                Time.timeScale = 1f;
            if (GUILayout.Button("x2"))
                Time.timeScale = 2f;
            if (GUILayout.Button("x4"))
                Time.timeScale = 4f;
            GUILayout.EndHorizontal();
            GUILayout.Label($"Time scale : {Time.timeScale:0.#}");
        }

        private void DrawMetaSeasonSection()
        {
            GUILayout.Label("— META / SAISON —", GUI.skin.box);
            GUILayout.Label($"Paris : {GameClock.ParisNow:yyyy-MM-dd HH:mm}");
            GUILayout.Label($"Daily id : {GameClock.GetDailyResetId()}");
            GUILayout.Label($"Weekly id : {GameClock.GetWeeklyResetId()}");
            GUILayout.Label(
                $"Saison {SeasonRotationManager.CurrentSeasonId} — semaine {SeasonRotationManager.CurrentWeekNumber}/5");

            PersistentManager pm = PersistentManager.Instance;
            if (pm != null)
            {
                GUILayout.Label(
                    $"Score saison : {pm.BestScoreThisSeason} (ét. {pm.BestStageThisSeason} ×{pm.BestTierThisSeason})");
                GUILayout.Label(
                    $"Saison save : {pm.SeasonId} / calc : {SeasonRotationManager.CurrentSeasonId}");
                GUILayout.Label($"Runs : {pm.RunsThisSeason}");
                bool recapPending = pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.pending;
                SeasonRecapData recap = pm.PendingSeasonRecap;
                if (recapPending && recap != null)
                {
                    GUILayout.Label(
                        $"Recap pending : True | Tals={recap.pendingTals} LR×{recap.pendingLrLevels} " +
                        $"({recap.lrCharacterId}) credited={recap.rewardsCredited} lastTier={recap.lastTierReached}");
                }
                else
                {
                    GUILayout.Label($"Recap pending : {recapPending}");
                }

                string unlockedList = FormatUnlockedDifficulties(pm);
                GUILayout.Label($"Débloqués : {unlockedList}");

                int claimedCount = pm.ClaimedTiers != null ? pm.ClaimedTiers.Count : 0;
                int eligible = SeasonRewards.GetHighestEligibleTierNumber();
                GUILayout.Label($"Piste : éligible={eligible}/12 · claims={claimedCount}/12");
                GUILayout.Label($"Prestige claimable : {SeasonRewards.GetPrestigeClaimableCount()}");
            }
            else
            {
                GUILayout.Label("PersistentManager absent");
            }

            RunManager runMgr = RunManager.Instance;
            if (runMgr != null)
            {
                GUILayout.Label(
                    $"Cran run : x{runMgr.CurrentDifficultyMultiplier} (idx {runMgr.CurrentDifficultyIndex})");
            }

            int slot0 = SeasonRotationManager.GetCurrentUniverseAtSlot(0);
            GUILayout.Label($"Slot 1 (ét. 1–20) : {UniverseIds.GetDisplayName(slot0)} — {UniverseIds.GetThemeLabel(slot0)}");

            if (stageGenerator != null)
            {
                int spawnU = stageGenerator.CurrentUniverseIndex;
                GUILayout.Label(
                    $"Stage spawn U{spawnU} ({UniverseIds.GetThemeLabel(spawnU)}) / logique U{stageGenerator.CurrentLogicalUniverseIndex}");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Semaine -1"))
            {
                int w = SeasonRotationManager.CurrentWeekIndex;
                SeasonRotationManager.SetDebugForcedWeekIndex((w + 4) % 5);
            }
            if (GUILayout.Button("Semaine +1"))
            {
                int w = SeasonRotationManager.CurrentWeekIndex;
                SeasonRotationManager.SetDebugForcedWeekIndex((w + 1) % 5);
            }
            if (GUILayout.Button("Clear week force"))
                SeasonRotationManager.SetDebugForcedWeekIndex(null);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1 jour clock"))
                GameClock.DebugAdvanceDays(1);
            if (GUILayout.Button("+7 jours clock"))
                GameClock.DebugAdvanceDays(7);
            if (GUILayout.Button("Clear clock"))
                GameClock.SetDebugOverride(null);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Check rollover"))
                SeasonProgressManager.EnsureSeasonCurrent();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Unlock all crans"))
                PersistentManager.Instance?.UnlockAllDifficulties();
            if (GUILayout.Button("Reset crans"))
                PersistentManager.Instance?.ResetUnlockedDifficulties();
            GUILayout.EndHorizontal();

            // Debug piste (MT2-G3)
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+50 score"))
            {
                // Debug : bump artificiel du score saison (stage fictif 0).
                PersistentManager p = PersistentManager.Instance;
                if (p != null)
                    p.TryImproveSeasonScore(p.BestScoreThisSeason + 50, 0, 1f);
            }
            if (GUILayout.Button("Claim palier suivant"))
            {
                int next = SeasonRewards.GetNextClaimableTierIndex();
                if (next >= 0)
                    SeasonRewards.TryClaim(next);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Claim prestige"))
                SeasonRewards.ClaimAllPrestige();
            if (GUILayout.Button("Créditer récap pending"))
                SeasonRewards.CreditPendingRecap();
            GUILayout.EndHorizontal();

            TimeSpan untilEnd = SeasonRotationManager.GetTimeUntilSeasonEnd();
            GUILayout.Label($"Temps restant saison : {untilEnd.Days}j {untilEnd.Hours}h {untilEnd.Minutes}m");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ouvrir page saison"))
            {
                SeasonPageUI page = UnityEngine.Object.FindObjectOfType<SeasonPageUI>(true);
                page?.Open();
            }
            if (GUILayout.Button("Ouvrir récap (gate)"))
            {
                SeasonRecapUI recapUi = UnityEngine.Object.FindObjectOfType<SeasonRecapUI>(true);
                recapUi?.OpenAsGate();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Dump état saison"))
                DumpSeasonState();

            if (GUILayout.Button("Run suite G5 (intégrité)"))
                SeasonIntegritySuite.Run();

            if (GUILayout.Button("Run suite G6 (live)"))
                SeasonLiveIntegritySuite.Run();

            if (GameClock.HasDebugOverride)
                GUILayout.Label("Clock override ACTIF", _statusStyle);
        }

        /// <summary>
        /// Photo structurée de l'état saison (preuve G5 — avant/après rollover).
        /// </summary>
        private static void DumpSeasonState()
        {
            PersistentManager pm = PersistentManager.Instance;
            SeasonRecapData recap = pm != null ? pm.PendingSeasonRecap : null;

            string claims = FormatIntListSorted(pm != null ? pm.ClaimedTiers : null);
            string crans = FormatUnlockedDifficulties(pm);
            string portalLr = FormatStringList(pm != null ? pm.PastSeasonLrIds : null);

            TimeSpan remaining = SeasonRotationManager.GetTimeUntilSeasonEnd();
            DateTime endParis = SeasonRotationManager.GetCurrentSeasonEndParis();

            Debug.Log(
                "[SeasonDump] ═══ ÉTAT SAISON ═══\n" +
                $"seasonId save/calc : {(pm != null ? pm.SeasonId : "—")} / {SeasonRotationManager.CurrentSeasonId} " +
                $"· semaine rotation : {SeasonRotationManager.CurrentWeekNumber}/5\n" +
                $"score : {(pm != null ? pm.BestScoreThisSeason : 0)} " +
                $"(ét. {(pm != null ? pm.BestStageThisSeason : 0)} ×{(pm != null ? pm.BestTierThisSeason : 1f)}) " +
                $"· runs : {(pm != null ? pm.RunsThisSeason : 0)}\n" +
                $"claims : [{claims}] · prestige réclamés : {(pm != null ? pm.PrestigeTiersClaimed : 0)} " +
                $"· claimable : {SeasonRewards.GetPrestigeClaimableCount()}\n" +
                $"COMPTE — crans : [{crans}] · LR portail : [{portalLr}]\n" +
                $"recap : pending={(recap != null && recap.pending)} credited={(recap != null && recap.rewardsCredited)} " +
                $"(S={(recap != null ? recap.seasonId : "")}, score={(recap != null ? recap.finalScore : 0)}, " +
                $"tals={(recap != null ? recap.pendingTals : 0)}, lrLvl={(recap != null ? recap.pendingLrLevels : 0)})\n" +
                $"Tals : {(pm != null ? pm.Tals : 0)} · bestStage à vie : {(pm != null ? pm.BestStage : 0)} " +
                $"· fin de saison : {endParis:yyyy-MM-dd} " +
                $"(reste {remaining.Days}j {remaining.Hours}h {remaining.Minutes}m)");
        }

        private static string FormatIntListSorted(System.Collections.Generic.IReadOnlyList<int> source)
        {
            if (source == null || source.Count == 0)
                return "";

            int[] copy = new int[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i];
            System.Array.Sort(copy);

            System.Collections.Generic.List<string> parts =
                new System.Collections.Generic.List<string>(copy.Length);
            for (int i = 0; i < copy.Length; i++)
                parts.Add(copy[i].ToString());
            return string.Join(",", parts);
        }

        private static string FormatStringList(System.Collections.Generic.IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
                return "";

            System.Collections.Generic.List<string> parts =
                new System.Collections.Generic.List<string>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (!string.IsNullOrEmpty(source[i]))
                    parts.Add(source[i]);
            }

            return string.Join(",", parts);
        }

        private static string FormatUnlockedDifficulties(PersistentManager pm)
        {
            if (pm == null)
                return "—";

            System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>(6);
            parts.Add("0");
            var unlocked = pm.UnlockedDifficulties;
            if (unlocked != null)
            {
                for (int i = 0; i < unlocked.Count; i++)
                    parts.Add(unlocked[i].ToString());
            }

            return string.Join(",", parts);
        }

        private void DrawMissionsSection()
        {
            GUILayout.Label("— MISSIONS —", GUI.skin.box);
            MissionManager mm = MissionManager.Instance;
            if (mm == null || !mm.IsInitialized)
            {
                GUILayout.Label("MissionManager absent / non init (lance depuis Hub).");
                return;
            }

            DrawMissionLayerDebug(mm, MissionLayer.Daily, "Daily");
            DrawMissionLayerDebug(mm, MissionLayer.Weekly, "Weekly");
            DrawMissionLayerDebug(mm, MissionLayer.Permanent, "Permanent");

            if (mm.CurrentRunSnapshot != null)
            {
                MissionRunSnapshot s = mm.CurrentRunSnapshot;
                GUILayout.Label(
                    $"Run snap: roleOK={s.MatchesFullSeasonRole} ({WeeklyMissionSchedule.GetRoleDisplayName(s.SeasonRole)}) " +
                    $"sr={s.AllSr} switch={s.SpecSwitchOccurred}");
            }

            if (GUILayout.Button("Claim all Completed"))
            {
                List<MissionRuntimeEntry> buf = new List<MissionRuntimeEntry>(16);
                ClaimLayer(mm, MissionLayer.Daily, buf);
                ClaimLayer(mm, MissionLayer.Weekly, buf);
                ClaimLayer(mm, MissionLayer.Permanent, buf);
                _statusMessage = "Claims effectués.";
            }

            if (GUILayout.Button("Force apply resets"))
            {
                mm.DebugForceApplyResets();
                _statusMessage = "Resets réévalués.";
            }

            if (GUILayout.Button("RESET missions (vierge)"))
            {
                mm.DebugResetAllProgress();
                _statusMessage = "Missions remises à zéro.";
            }
        }

        private void DrawMissionLayerDebug(MissionManager mm, MissionLayer layer, string title)
        {
            GUILayout.Label(title + " :");
            List<MissionRuntimeEntry> buf = new List<MissionRuntimeEntry>(16);
            mm.GetEntriesForLayer(layer, buf);
            for (int i = 0; i < buf.Count; i++)
            {
                MissionRuntimeEntry e = buf[i];
                if (e?.Data == null)
                    continue;
                GUILayout.Label(
                    $"{e.Data.GetResolvedDisplayName()} | {e.CurrentValue}/{e.Data.TargetValue} | {e.State}");
            }
        }

        private static void ClaimLayer(MissionManager mm, MissionLayer layer, List<MissionRuntimeEntry> buf)
        {
            mm.GetEntriesForLayer(layer, buf);
            for (int i = 0; i < buf.Count; i++)
            {
                if (buf[i].IsClaimable)
                    mm.TryClaim(buf[i].Data.MissionId);
            }
        }

        private void DrawBossRushSection()
        {
            GUILayout.Label("— BOSS RUSH —", GUI.skin.box);
            BossRushManager mgr = BossRushManager.Instance;
            if (mgr == null)
            {
                GUILayout.Label("BossRushManager absent (Hub).");
                return;
            }

            GUILayout.Label($"Unlocked={mgr.IsUnlocked} roster={mgr.RosterCount} majors={mgr.MajorUnlockedCount}");
            if (GUILayout.Button("Force unlock (empty OK)"))
            {
                PersistentManager.Instance?.UnlockBossRush();
                mgr.LoadFromPersistent();
                _statusMessage = "Boss Rush unlocked flag ON.";
            }

            if (GUILayout.Button("RESET Boss Rush (vierge)"))
            {
                mgr.DebugResetAll();
                _statusMessage = "Boss Rush vidé + re-verrouillé.";
            }
        }

        private void DrawCheatsSection()
        {
            GUILayout.Label("— CHEATS —", GUI.skin.box);
            DebugCheats.GodMode = GUILayout.Toggle(DebugCheats.GodMode, "God mode");
            DebugCheats.OneShot = GUILayout.Toggle(DebugCheats.OneShot, "One-shot");
            DebugCheats.EnemyGodMode = GUILayout.Toggle(DebugCheats.EnemyGodMode, "Enemy god mode (1 PV min)");

            if (GUILayout.Button("Heal full team"))
            {
                if (RunManager.Instance != null)
                    RunManager.Instance.HealTeam(1f);
            }

            if (GUILayout.Button("+1000 Tals"))
            {
                if (RunManager.Instance != null)
                    RunManager.Instance.AddTals(1000);
            }

            GUILayout.Space(4f);
            GUILayout.Label("Faille (gate test)");
            if (GUILayout.Button("Donner Faille + équipe (nv.15)"))
                DebugGiveFailleForTest();
            if (GUILayout.Button("Portails Faille défaut (H/B)"))
            {
                if (FailleSystem.Instance != null)
                    FailleSystem.Instance.PlaceDefaultPortals();
                else
                    Debug.LogWarning("[Debug] FailleSystem absent (Faille pas en combat).");
            }
        }

        private void DrawSaveGachaSection()
        {
            GUILayout.Label("— SAVE / GACHA —", GUI.skin.box);

            if (GUILayout.Button("Export save"))
                DebugExportSave();

            if (GUILayout.Button("Import save_import.json"))
                DebugImportSave();

            if (GUILayout.Button("Pity → seuil-1 (toutes bannières)"))
                DebugForcePityNearThreshold();

            GUILayout.BeginHorizontal();
            GUILayout.Label("id", GUILayout.Width(24f));
            _giveCharacterId = GUILayout.TextField(_giveCharacterId ?? string.Empty);
            GUILayout.Label("nv", GUILayout.Width(24f));
            _giveCharacterLevel = GUILayout.TextField(_giveCharacterLevel ?? string.Empty, GUILayout.Width(48f));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Give perso"))
                DebugGiveCharacter();
        }

        private void DebugExportSave()
        {
            if (PersistentManager.Instance == null)
            {
                _statusMessage = "PersistentManager absent.";
                return;
            }

            PersistentManager.Instance.SaveGame();
            string source = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(source))
            {
                _statusMessage = "save.json introuvable après SaveGame.";
                return;
            }

            string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string dest = Path.Combine(Application.persistentDataPath, "save_export_" + stamp + ".json");
            File.Copy(source, dest, overwrite: true);
            _statusMessage = "Export OK : " + dest;
            Debug.Log("[DebugMenu] Export save → " + dest);
        }

        private void DebugImportSave()
        {
            if (PersistentManager.Instance == null)
            {
                _statusMessage = "PersistentManager absent.";
                return;
            }

            string importPath = Path.Combine(Application.persistentDataPath, "save_import.json");
            if (!File.Exists(importPath))
            {
                _statusMessage = "save_import.json absent dans persistentDataPath.";
                return;
            }

            SaveData parsed;
            try
            {
                string json = File.ReadAllText(importPath);
                parsed = JsonUtility.FromJson<SaveData>(json);
            }
            catch (System.Exception e)
            {
                _statusMessage = "fichier invalide, rien touché.";
                Debug.LogError("[DebugMenu] Import parse échoué : " + e.Message);
                return;
            }

            if (parsed == null)
            {
                _statusMessage = "fichier invalide, rien touché.";
                return;
            }

            SaveMigrator.MigrateToCurrent(parsed);
            SaveSystem.Save(parsed);
            PersistentManager.Instance.LoadGame();
            _statusMessage = "importé — redémarrage conseillé pour les managers de scène";
            Debug.Log("[DebugMenu] Import save depuis " + importPath);
        }

        private void DebugForcePityNearThreshold()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Gacha == null)
            {
                _statusMessage = "GachaManager absent.";
                return;
            }

            if (allBanners == null || allBanners.Count == 0)
            {
                _statusMessage = "Aucune bannière (allBanners vide).";
                return;
            }

            Dictionary<string, int> pity = PersistentManager.Instance.Gacha.GetPityData();
            if (pity == null)
                pity = new Dictionary<string, int>();

            int treated = 0;
            for (int i = 0; i < allBanners.Count; i++)
            {
                BannerData banner = allBanners[i];
                if (banner == null)
                    continue;

                int value = banner.PityThreshold - 1;
                if (value < 0)
                    value = 0;
                pity[banner.Id] = value;
                treated++;
            }

            PersistentManager.Instance.Gacha.LoadPityData(pity);
            PersistentManager.Instance.SaveGame();
            _statusMessage = "Pity → seuil-1 sur " + treated + " bannière(s).";
            Debug.Log("[DebugMenu] " + _statusMessage);
        }

        private void DebugGiveCharacter()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
            {
                _statusMessage = "CharacterManager absent.";
                return;
            }

            string id = (_giveCharacterId ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(id))
            {
                _statusMessage = "id perso vide.";
                return;
            }

            int level = 1;
            if (!int.TryParse(_giveCharacterLevel, out level))
                level = 1;

            CharacterManager cm = PersistentManager.Instance.Characters;
            bool addedOrLeveled = cm.AddCharacter(id);
            OwnedCharacter owned = cm.GetOwnedCharacter(id);
            if (owned == null)
            {
                _statusMessage = "Personnage inconnu : " + id;
                return;
            }

            // Écriture directe du niveau assumée (outil debug).
            if (level > 1)
                owned.level = Mathf.Clamp(level, 1, CharacterData.MAX_LEVEL);

            PersistentManager.Instance.SaveGame();
            _statusMessage = "Give " + id + " nv." + owned.level
                + (addedOrLeveled ? " (ajout/level-up AddCharacter)" : "");
            Debug.Log("[DebugMenu] " + _statusMessage);
        }

        private static void DebugGiveFailleForTest()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
            {
                Debug.LogWarning("[Debug] PersistentManager / Characters absent.");
                return;
            }

            CharacterManager cm = PersistentManager.Instance.Characters;
            const string failleId = "faille";
            cm.AddCharacter(failleId);

            OwnedCharacter owned = cm.GetOwnedCharacter(failleId);
            if (owned != null && owned.level < 15)
                owned.level = 15;

            cm.AddToTeam(failleId);
            PersistentManager.Instance.SaveGame();
            Debug.Log("[Debug] Faille donnée (nv.15) et ajoutée à l'équipe si place.");
        }

        private void DrawPressureSection()
        {
            GUILayout.Label("— PRESSION —", GUI.skin.box);

            var pressure = PressureGaugeSystem.Instance;
            if (pressure == null)
            {
                GUILayout.Label("PressureGaugeSystem absent de la scène.");
                return;
            }

            string ruptureLabel = pressure.IsInRupture
                ? $"RUPTURE ({pressure.RuptureProgress01 * 100f:F0}%)"
                : "normal";
            GUILayout.Label($"Jauge : {pressure.NormalizedValue * 100f:F0}% — {ruptureLabel}");

            GUI.enabled = !pressure.IsInRupture;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jauge → 99%"))
                pressure.DebugSetGaugeAbsolute(99f, "debug menu pré-rupture");

            if (GUILayout.Button("+10"))
                pressure.Increase(10f, "debug menu");

            if (GUILayout.Button("-10"))
                pressure.Decrease(10f, "debug menu");
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Déclencher rupture (100%)"))
                pressure.DebugSetGaugeAbsolute(100f, "debug menu rupture");
            GUI.enabled = true;

            GUI.enabled = pressure.IsInRupture;
            if (GUILayout.Button("Forcer fin de rupture"))
                pressure.DebugEndRupture();
            GUI.enabled = true;

            GUILayout.Space(4f);
            var ruptureFx = RuptureEffectsSystem.Instance;
            if (ruptureFx == null)
            {
                GUILayout.Label("RuptureEffectsSystem absent.");
                return;
            }

            GUILayout.Label($"Aura : {ruptureFx.ActiveAuraVariantId} ({ruptureFx.ActiveAuraVariantIndex + 1}/{Mathf.Max(1, ruptureFx.AuraVariantCount)})");
            GUILayout.BeginHorizontal();
            GUI.enabled = ruptureFx.AuraVariantCount > 0;
            if (GUILayout.Button("Aura ◀"))
                ruptureFx.CycleAuraVariant(-1);
            if (GUILayout.Button("Aura ▶"))
                ruptureFx.CycleAuraVariant(1);
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawPortraitLoaderSection()
        {
            GUILayout.Label("— PORTRAIT LOADER (cache) —", GUI.skin.box);

            if (GUILayout.Button("Load Portrait Kram"))
            {
                _debugPortraitTexture = PortraitLoader.Load("kramhoisi");
                _statusMessage = _debugPortraitTexture != null
                    ? $"Portrait Kram chargé ({_debugPortraitTexture.width}x{_debugPortraitTexture.height})"
                    : "Échec chargement portrait Kram (voir Console).";
            }

            if (GUILayout.Button("Release Portrait"))
            {
                PortraitLoader.Release(_debugPortraitTexture);
                _debugPortraitTexture = null;
                _statusMessage = "Portrait relâché.";
            }

            PortraitLoader.GetCacheSnapshot(_portraitCacheSnapshot);
            GUILayout.Label($"Entrées cache : {_portraitCacheSnapshot.Count}");
            for (int i = 0; i < _portraitCacheSnapshot.Count; i++)
            {
                (string path, int refCount) entry = _portraitCacheSnapshot[i];
                GUILayout.Label($"{entry.path} — refCount {entry.refCount}");
            }
        }

        private void DrawSpecSection()
        {
            GUILayout.Label("— SPÉ —", GUI.skin.box);
            if (turnManager == null)
            {
                GUILayout.Label("TurnManager non assigné.");
                return;
            }

            if (!turnManager.IsPlayerTurn)
            {
                GUILayout.Label("(tour ennemi — switch indisponible)");
                return;
            }

            CharacterBall ball = turnManager.CurrentParticipant as CharacterBall;
            if (ball == null || ball.IsDead)
            {
                GUILayout.Label("(aucun allié actif)");
                return;
            }

            if (ball.Data == null)
            {
                GUILayout.Label("(pas de CharacterData)");
                return;
            }

            GUILayout.Label($"Participant : {ball.Name}");
            string activeName = ball.ActiveSpec != null ? ball.ActiveSpec.SpecName : "(base)";
            GUILayout.Label($"Spé active : {activeName}");

            List<(SpecializationData spec, int unlockLevel)> available =
                ball.Data.GetAvailableSpecializations(ball.CharacterLevel);
            if (available == null || available.Count == 0)
            {
                GUILayout.Label("(aucune spé disponible)");
                return;
            }

            for (int i = 0; i < available.Count; i++)
            {
                SpecializationData spec = available[i].spec;
                if (spec == null) continue;

                int specIndex = ResolveSpecIndex(ball.Data, spec);
                bool isActive = ball.ActiveSpec != null && ReferenceEquals(ball.ActiveSpec, spec);

                GUILayout.BeginHorizontal();
                GUILayout.Label(
                    isActive ? $"{spec.SpecName} *" : spec.SpecName,
                    GUILayout.Width(180f));

                GUI.enabled = !isActive;
                if (GUILayout.Button("Activer", GUILayout.Width(72f)))
                    DebugSwitchSpec(ball, specIndex);
                GUI.enabled = true;

                GUILayout.EndHorizontal();
            }
        }

        private static int ResolveSpecIndex(CharacterData data, SpecializationData spec)
        {
            if (data == null || spec == null) return -1;

            SpecializationData baseSpec = data.GetSpecialization(-1);
            if (baseSpec != null && ReferenceEquals(baseSpec, spec))
                return -1;

            int count = data.GetSpecializationCount();
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(data.GetSpecialization(i), spec))
                    return i;
            }

            return -1;
        }

        private void DebugSwitchSpec(CharacterBall ball, int specIndex)
        {
            if (ball == null || ball.Data == null) return;

            SpecializationData targetSpec = ball.Data.GetSpecialization(specIndex);
            if (targetSpec == null) return;

            ball.SwitchSpecInCombat(specIndex);

            if (SpecSwitchBannerUI.Instance != null)
                SpecSwitchBannerUI.Instance.Show(targetSpec.SpecName, targetSpec.Role);

            Debug.Log($"[Debug] Switch spé → {targetSpec.SpecName}");
            _statusMessage = $"Spé : {targetSpec.SpecName}";
        }

        private void DrawStatsSection()
        {
            GUILayout.Label("— STATS —", GUI.skin.box);
            if (turnManager == null)
            {
                GUILayout.Label("TurnManager non assigné.");
                return;
            }

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null || allies.Count == 0)
            {
                GUILayout.Label("(aucun allié)");
                return;
            }

            bool anyLiving = false;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                anyLiving = true;
                GUILayout.Label(ally.Name, GUI.skin.box);
                GUILayout.Label($"PV : {ally.CurrentHp} / {ally.MaxHp}");
                GUILayout.Label($"ATK : {FormatIntStat(ally.Atk, ally.EffectiveAtk)}");
                GUILayout.Label($"DEF : {FormatIntStat(ally.Def, ally.EffectiveDef)}");
                GUILayout.Label($"Crit chance : {FormatPercentStat(ally.BaseCritChance, ally.EffectiveCritChance)}");
                GUILayout.Label($"Crit multi : {FormatFloatStat(ally.BaseCritMultiplier, ally.EffectiveCritMultiplier, "0.##")}");
                GUILayout.Label($"Force lancer : {FormatFloatStat(ally.BaseLaunchForceMultiplier, ally.EffectiveLaunchForceMultiplier, "0.##")}");
                GUILayout.Label($"Vitesse : {FormatIntStat(ally.BaseSpeed, ally.EffectiveSpeed)}");
                GUILayout.Space(4f);
            }

            if (!anyLiving)
                GUILayout.Label("(aucun allié vivant)");
        }

        private void DrawDamageSection()
        {
            GUILayout.Label("— DÉGÂTS —", GUI.skin.box);
            if (turnManager == null)
            {
                GUILayout.Label("TurnManager non assigné.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Montant : {_debugDamageAmount}", GUILayout.Width(120f));
            if (GUILayout.Button("10", GUILayout.Width(36f)))
                _debugDamageAmount = 10;
            if (GUILayout.Button("50", GUILayout.Width(36f)))
                _debugDamageAmount = 50;
            if (GUILayout.Button("100", GUILayout.Width(40f)))
                _debugDamageAmount = 100;
            if (GUILayout.Button("+", GUILayout.Width(28f)))
                _debugDamageAmount = Mathf.Min(9999, _debugDamageAmount + 10);
            if (GUILayout.Button("-", GUILayout.Width(28f)))
                _debugDamageAmount = Mathf.Max(1, _debugDamageAmount - 10);
            GUILayout.EndHorizontal();

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null || allies.Count == 0)
            {
                GUILayout.Label("(aucun allié)");
                return;
            }

            int livingShown = 0;
            for (int i = 0; i < allies.Count && livingShown < 4; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                livingShown++;
                int displayIndex = livingShown;
                GUILayout.Label($"Perso {displayIndex} — {ally.Name}  {ally.CurrentHp}/{ally.MaxHp}");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button($"-{_debugDamageAmount} brut", GUILayout.Width(80f)))
                    ally.DebugDamage(_debugDamageAmount);
                if (GUILayout.Button($"-{_debugDamageAmount} mob", GUILayout.Width(80f)))
                    ally.TakeDamage(_debugDamageAmount);

                int toOneHp = ally.CurrentHp - 1;
                if (GUILayout.Button("→ 1 PV", GUILayout.Width(64f)))
                {
                    if (toOneHp > 0)
                        ally.DebugDamage(toOneHp);
                }

                if (GUILayout.Button("Tuer", GUILayout.Width(56f)))
                    ally.DebugDamage(ally.CurrentHp);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("→20%", GUILayout.Width(52f)))
                    DebugDamageToHpPercent(ally, 0.20f);
                if (GUILayout.Button("→30%", GUILayout.Width(52f)))
                    DebugDamageToHpPercent(ally, 0.30f);
                if (GUILayout.Button("→50%", GUILayout.Width(52f)))
                    DebugDamageToHpPercent(ally, 0.50f);
                GUILayout.EndHorizontal();
            }

            if (livingShown == 0)
            {
                GUILayout.Label("(aucun allié vivant)");
                return;
            }

            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Toute l'équipe -{_debugDamageAmount} brut"))
                ApplyDebugDamageToAllLiving(_debugDamageAmount);
            if (GUILayout.Button($"Toute l'équipe -{_debugDamageAmount} mob"))
                ApplyMitigatedDamageToAllLiving(_debugDamageAmount);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Tuer toute l'équipe"))
                KillAllLivingAllies();
        }

        private static void DebugDamageToHpPercent(CharacterBall ally, float percent)
        {
            if (ally == null) return;

            int target = Mathf.RoundToInt(ally.EffectiveMaxHp * percent);
            if (ally.CurrentHp > target)
                ally.DebugDamage(ally.CurrentHp - target);
        }

        private void ApplyMitigatedDamageToAllLiving(int amount)
        {
            if (turnManager == null)
                return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                if (amount > 0)
                    ally.TakeDamage(amount);
            }
        }

        private void DrawHealSection()
        {
            GUILayout.Label("— SOIN —", GUI.skin.box);
            if (turnManager == null)
            {
                GUILayout.Label("TurnManager non assigné.");
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Soin : {Mathf.RoundToInt(_debugHealPercent * 100f)}%", GUILayout.Width(120f));
            if (GUILayout.Button("10%", GUILayout.Width(48f)))
                _debugHealPercent = 0.10f;
            if (GUILayout.Button("25%", GUILayout.Width(48f)))
                _debugHealPercent = 0.25f;
            GUILayout.EndHorizontal();

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null || allies.Count == 0)
            {
                GUILayout.Label("(aucun allié)");
                return;
            }

            int livingShown = 0;
            for (int i = 0; i < allies.Count && livingShown < 4; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                livingShown++;
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Perso {livingShown} — {ally.Name}", GUILayout.Width(180f));
                if (GUILayout.Button($"+{Mathf.RoundToInt(_debugHealPercent * 100f)}% PV", GUILayout.Width(96f)))
                    ally.Heal(Mathf.RoundToInt(ally.EffectiveMaxHp * _debugHealPercent));
                GUILayout.EndHorizontal();
            }

            if (livingShown == 0)
            {
                GUILayout.Label("(aucun allié vivant)");
                return;
            }

            if (GUILayout.Button("Soigner l'équipe"))
                HealAllLivingAllies();
        }

        private void HealAllLivingAllies()
        {
            if (turnManager == null)
                return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                ally.Heal(Mathf.RoundToInt(ally.EffectiveMaxHp * _debugHealPercent));
            }
        }

        private const string DebugBuffAtkId = "debug_buff_atk";
        private const string DebugDebuffAtkId = "debug_debuff_atk";

        private void DrawStatesSection()
        {
            GUILayout.Label("— ÉTATS —", GUI.skin.box);
            GUILayout.Label("Injecte via les vrais systèmes (UnitStatusFx / pastilles / teinte).");
            if (turnManager == null)
            {
                GUILayout.Label("TurnManager non assigné.");
                return;
            }

            _statesScrollPosition = GUILayout.BeginScrollView(_statesScrollPosition, GUILayout.Height(320f));

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            bool anyAlly = false;
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall ally = allies[i];
                    if (ally == null || ally.IsDead)
                        continue;

                    anyAlly = true;
                    GUILayout.Label($"{ally.Name}  {ally.CurrentHp}/{ally.MaxHp}", GUI.skin.box);
                    DrawBuffReceiverStates(ally.BuffReceiver);
                    DrawAllyStateInjectButtons(ally);
                    GUILayout.Space(4f);
                }
            }

            if (!anyAlly)
                GUILayout.Label("(aucun allié vivant)");

            GUILayout.Space(8f);
            GUILayout.Label("— Ennemis —", GUI.skin.box);

            IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
            bool anyEnemy = false;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    ITurnParticipant participant = participants[i];
                    if (participant == null || participant.IsAlly || participant.IsDead)
                        continue;

                    Enemy enemy = participant as Enemy;
                    if (enemy == null)
                        continue;

                    anyEnemy = true;
                    GUILayout.Label(
                        $"{enemy.Name}  {enemy.CurrentHp}/{enemy.MaxHp}  ATK {enemy.EffectiveAtk} DEF {enemy.EffectiveDef}",
                        GUI.skin.box);
                    DrawBuffReceiverStates(enemy.BuffReceiver);
                    DrawEnemyStateInjectButtons(enemy);
                    GUILayout.Space(4f);
                }
            }

            if (!anyEnemy)
                GUILayout.Label("(aucun ennemi vivant)");

            GUILayout.EndScrollView();
        }

        private void DrawAllyStateInjectButtons(CharacterBall ally)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Burn", GUILayout.Width(56f)))
            {
                AllyDotSystem.ApplyBurn(ally, 0.03f, 8, null);
                _statusMessage = $"Burn allié → {ally.Name}";
            }
            if (GUILayout.Button("Buff+", GUILayout.Width(56f)))
            {
                DebugApplyStatBuff(ally.BuffReceiver, DebugBuffAtkId, +0.25f, GetFirstLivingAlly());
                _statusMessage = $"Buff ATK → {ally.Name}";
            }
            if (GUILayout.Button("Debuff", GUILayout.Width(56f)))
            {
                DebugApplyStatBuff(ally.BuffReceiver, DebugDebuffAtkId, -0.25f, GetFirstLivingAlly());
                _statusMessage = $"Debuff ATK → {ally.Name}";
            }
            if (GUILayout.Button("Clear", GUILayout.Width(56f)))
            {
                DebugClearAllyStates(ally);
                _statusMessage = $"Clear états → {ally.Name}";
            }
            GUILayout.EndHorizontal();
        }

        private void DrawEnemyStateInjectButtons(Enemy enemy)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Burn", GUILayout.Width(48f)))
            {
                DebugApplyEnemyBurn(enemy);
                _statusMessage = $"Burn → {enemy.Name}";
            }
            if (GUILayout.Button("Poison", GUILayout.Width(52f)))
            {
                DebugApplyEnemyPoison(enemy);
                _statusMessage = $"Poison → {enemy.Name}";
            }
            if (GUILayout.Button("Stun", GUILayout.Width(48f)))
            {
                if (StunSystem.Instance == null)
                    _statusMessage = "StunSystem absent.";
                else
                {
                    StunSystem.Instance.StunEnemy(enemy, GetFirstLivingAlly());
                    _statusMessage = $"Stun → {enemy.Name}";
                }
            }
            if (GUILayout.Button("Gel", GUILayout.Width(48f)))
            {
                CharacterBall source = GetFirstLivingAlly();
                if (source == null)
                    _statusMessage = "Gel : besoin d'un allié vivant (source).";
                else if (FreezeSystem.Instance == null)
                    _statusMessage = "FreezeSystem absent.";
                else
                {
                    FreezeSystem.Instance.FreezeEnemy(enemy, source);
                    _statusMessage = $"Gel → {enemy.Name}";
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bouclier", GUILayout.Width(64f)))
            {
                EnemyShieldSystem shield = EnsureDebugEnemyShield(enemy);
                if (shield == null)
                    _statusMessage = $"Bouclier impossible → {enemy.Name}";
                else
                {
                    shield.ActivateShield(0.5f);
                    _statusMessage = $"Bouclier 50% → {enemy.Name}";
                }
            }
            if (GUILayout.Button("Buff+", GUILayout.Width(48f)))
            {
                DebugApplyStatBuff(enemy.BuffReceiver, DebugBuffAtkId, +0.25f, GetFirstLivingAlly());
                _statusMessage = $"Buff ATK → {enemy.Name}";
            }
            if (GUILayout.Button("Debuff", GUILayout.Width(56f)))
            {
                DebugApplyStatBuff(enemy.BuffReceiver, DebugDebuffAtkId, -0.25f, GetFirstLivingAlly());
                _statusMessage = $"Debuff ATK → {enemy.Name}";
            }
            if (GUILayout.Button("Clear", GUILayout.Width(48f)))
            {
                DebugClearEnemyStates(enemy);
                _statusMessage = $"Clear états → {enemy.Name}";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Combo Gel+Burn", GUILayout.Width(120f)))
            {
                CharacterBall source = GetFirstLivingAlly();
                if (source == null || FreezeSystem.Instance == null)
                    _statusMessage = "Combo Gel+Burn : allié + FreezeSystem requis.";
                else
                {
                    FreezeSystem.Instance.FreezeEnemy(enemy, source);
                    DebugApplyEnemyBurn(enemy);
                    _statusMessage = $"Combo Gel+Burn → {enemy.Name} (boucle gel, pastille burn)";
                }
            }
            if (GUILayout.Button("Combo Stun+Poison", GUILayout.Width(130f)))
            {
                if (StunSystem.Instance == null)
                    _statusMessage = "StunSystem absent.";
                else
                {
                    StunSystem.Instance.StunEnemy(enemy, GetFirstLivingAlly());
                    DebugApplyEnemyPoison(enemy);
                    _statusMessage = $"Combo Stun+Poison → {enemy.Name} (boucle stun, pastille poison)";
                }
            }
            GUILayout.EndHorizontal();
        }

        private CharacterBall GetFirstLivingAlly()
        {
            if (turnManager == null)
                return null;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return null;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall a = allies[i];
                if (a != null && !a.IsDead)
                    return a;
            }

            return null;
        }

        private static void DebugApplyStatBuff(
            BuffReceiver receiver, string buffId, float value, CharacterBall source)
        {
            if (receiver == null)
                return;

            receiver.RemoveBuffsById(buffId);
            receiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = source,
                StatType = BuffStatType.ATK,
                Value = value,
                IsPercent = true,
                RemainingTurns = 8,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private static void DebugApplyEnemyBurn(Enemy enemy)
        {
            if (enemy == null || enemy.BuffReceiver == null)
                return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br.HasBuff(BurnTickSystem.KramBurnBuffId))
                return;

            br.AddBuff(new BuffData
            {
                BuffId = BurnTickSystem.KramBurnBuffId,
                Source = null,
                StatType = BuffStatType.DamageAmplification,
                Value = 0f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private static void DebugApplyEnemyPoison(Enemy enemy)
        {
            if (enemy == null || enemy.BuffReceiver == null)
                return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br.HasBuff(PoisonTickSystem.PoisonBuffId))
                return;

            br.AddBuff(new BuffData
            {
                BuffId = PoisonTickSystem.PoisonBuffId,
                Source = null,
                StatType = BuffStatType.DamageAmplification,
                Value = 0f,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void DebugClearAllyStates(CharacterBall ally)
        {
            if (ally == null)
                return;

            AllyDotSystem.ClearBurn(ally);
            BuffReceiver br = ally.BuffReceiver;
            if (br == null)
                return;

            br.RemoveBuffsById(DebugBuffAtkId);
            br.RemoveBuffsById(DebugDebuffAtkId);
        }

        private void DebugClearEnemyStates(Enemy enemy)
        {
            if (enemy == null)
                return;

            BuffReceiver br = enemy.BuffReceiver;
            if (br != null)
            {
                br.RemoveBuffsById(BurnTickSystem.KramBurnBuffId);
                br.RemoveBuffsById(PoisonTickSystem.PoisonBuffId);
                br.RemoveBuffsById(DebugBuffAtkId);
                br.RemoveBuffsById(DebugDebuffAtkId);
            }

            if (StunSystem.Instance != null)
                StunSystem.Instance.RemoveStunFromEnemy(enemy);

            if (FreezeSystem.Instance != null
                && FreezeSystem.Instance.IsFrozenEnemy(enemy))
            {
                CharacterBall shatterSource = GetFirstLivingAlly();
                if (shatterSource != null)
                    FreezeSystem.Instance.ShatterEnemy(enemy, shatterSource);
            }

            EnemyShieldSystem shield = enemy.GetComponent<EnemyShieldSystem>();
            if (shield != null && shield.HasShieldPresence)
                shield.AbsorbDamage(999999);
        }

        /// <summary>
        /// Garantit un EnemyShieldSystem initialisé + UnitStatusFx branché (injecteur Bouclier).
        /// </summary>
        private EnemyShieldSystem EnsureDebugEnemyShield(Enemy enemy)
        {
            if (enemy == null)
                return null;

            EnemyShieldSystem shield = enemy.GetComponent<EnemyShieldSystem>();
            if (shield == null)
                shield = enemy.gameObject.AddComponent<EnemyShieldSystem>();

            // _owner null → ActivateShield no-op silencieux.
            shield.Initialize(enemy, turnManager);

            UnitStatusFx fx = enemy.GetComponent<UnitStatusFx>();
            if (fx != null)
                fx.EnsureShieldBinding();

            return shield;
        }

        private static void DrawBuffReceiverStates(BuffReceiver receiver)
        {
            if (receiver == null)
            {
                GUILayout.Label("  (pas de BuffReceiver)");
                return;
            }

            float shield = receiver.GetShieldAmount();
            if (shield > 0f)
                GUILayout.Label($"  Bouclier : {Mathf.RoundToInt(shield)}");

            IReadOnlyList<BuffData> buffs = receiver.ActiveBuffs;
            if (buffs == null || buffs.Count == 0)
            {
                if (shield <= 0f)
                    GUILayout.Label("  (aucun buff)");
                return;
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                BuffData buff = buffs[i];
                if (buff == null || buff.StatType == BuffStatType.Shield)
                    continue;

                GUILayout.Label($"  {buff.BuffId} {FormatBuffValue(buff)} ({FormatBuffTurns(buff.RemainingTurns)}t)");
            }
        }

        private static string FormatBuffValue(BuffData buff)
        {
            if (buff.IsPercent)
                return (buff.Value * 100f).ToString("0.#") + "%";
            return buff.Value.ToString("0.##");
        }

        private static string FormatBuffTurns(int remainingTurns)
        {
            return remainingTurns < 0 ? "∞" : remainingTurns.ToString();
        }

        private void ApplyDebugDamageToAllLiving(int amount)
        {
            if (turnManager == null)
                return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                if (amount > 0)
                    ally.DebugDamage(amount);
            }
        }

        private void KillAllLivingAllies()
        {
            if (turnManager == null)
                return;

            IReadOnlyList<CharacterBall> allies = turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                ally.DebugDamage(ally.CurrentHp);
            }
        }

        private void DrawLogSection()
        {
            GUILayout.Label("— LOG —", GUI.skin.box);

            if (GUILayout.Button("Clear"))
                ClearLogBuffer();

            _logScrollPosition = GUILayout.BeginScrollView(_logScrollPosition, GUILayout.Height(120f));
            if (_logCount == 0)
            {
                GUILayout.Label("(vide — messages avec [ uniquement)");
            }
            else
            {
                int start = (_logWriteIndex - _logCount + LogBufferSize) % LogBufferSize;
                for (int i = 0; i < _logCount; i++)
                {
                    string entry = _logBuffer[(start + i) % LogBufferSize];
                    if (!string.IsNullOrEmpty(entry))
                        GUILayout.Label(entry);
                }
            }
            GUILayout.EndScrollView();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(condition) || condition.IndexOf('[') < 0)
                return;

            string entry = condition.Length > LogEntryMaxLength
                ? condition.Substring(0, LogEntryMaxLength)
                : condition;

            _logBuffer[_logWriteIndex] = entry;
            _logWriteIndex = (_logWriteIndex + 1) % LogBufferSize;
            if (_logCount < LogBufferSize)
                _logCount++;
        }

        private void ClearLogBuffer()
        {
            for (int i = 0; i < LogBufferSize; i++)
                _logBuffer[i] = null;
            _logCount = 0;
            _logWriteIndex = 0;
        }

        private static string FormatIntStat(int baseValue, int effectiveValue)
        {
            return $"{baseValue} → {effectiveValue}{FormatDeltaSuffix(baseValue, effectiveValue)}";
        }

        private static string FormatFloatStat(float baseValue, float effectiveValue, string numberFormat)
        {
            return $"{baseValue.ToString(numberFormat)} → {effectiveValue.ToString(numberFormat)}{FormatDeltaSuffix(baseValue, effectiveValue)}";
        }

        private static string FormatPercentStat(float baseValue, float effectiveValue)
        {
            string baseStr = (baseValue * 100f).ToString("0.#") + "%";
            string effectiveStr = (effectiveValue * 100f).ToString("0.#") + "%";
            return $"{baseStr} → {effectiveStr}{FormatDeltaSuffix(baseValue, effectiveValue)}";
        }

        private static string FormatDeltaSuffix(float baseValue, float effectiveValue)
        {
            if (Mathf.Approximately(baseValue, effectiveValue))
                return string.Empty;

            if (Mathf.Abs(baseValue) > 0.0001f)
            {
                float deltaPercent = (effectiveValue - baseValue) / baseValue * 100f;
                return $" ({deltaPercent:+0.#;-0.#;0}%)";
            }

            float absoluteDelta = (effectiveValue - baseValue) * 100f;
            return $" ({absoluteDelta:+0.#;-0.#;0}%)";
        }

        private void DrawValisesSection()
        {
            GUILayout.Label("— VALISES —", GUI.skin.box);
            if (allValises == null || allValises.Count == 0)
            {
                GUILayout.Label("(liste vide)");
                return;
            }

            ValiseManager valiseManager = ValiseManager.Instance;
            for (int i = 0; i < allValises.Count; i++)
            {
                ValiseData data = allValises[i];
                if (data == null)
                    continue;

                string id = data.Id;
                ValiseImprovementRarity rarity = GetValiseRarity(id);
                int upgradeCount = GetValiseUpgradeCount(id);

                GUILayout.BeginHorizontal();
                string levelLabel = string.Empty;
                if (valiseManager != null)
                {
                    ValiseInstance active = valiseManager.GetActiveValise(id);
                    if (active != null)
                        levelLabel = $" (niv. {active.CurrentLevel})";
                }

                GUILayout.Label($"{data.ValiseName}{levelLabel}", GUILayout.Width(160f));

                if (GUILayout.Button("C", GUILayout.Width(28f)))
                    SetValiseRarity(id, ValiseImprovementRarity.Commune);
                if (GUILayout.Button("R", GUILayout.Width(28f)))
                    SetValiseRarity(id, ValiseImprovementRarity.Rare);
                if (GUILayout.Button("E", GUILayout.Width(28f)))
                    SetValiseRarity(id, ValiseImprovementRarity.Epique);
                if (GUILayout.Button("L", GUILayout.Width(28f)))
                    SetValiseRarity(id, ValiseImprovementRarity.Legendaire);

                GUILayout.Label(RarityShortLabel(rarity), GUILayout.Width(24f));

                string countStr = GUILayout.TextField(upgradeCount.ToString(), GUILayout.Width(40f));
                if (int.TryParse(countStr, out int parsedCount))
                    _valiseUpgradeCountById[id] = Mathf.Max(1, parsedCount);

                if (GUILayout.Button("Donner", GUILayout.Width(72f)))
                    GiveValise(data, rarity, GetValiseUpgradeCount(id));

                GUILayout.EndHorizontal();
            }
        }

        private void DrawItemsSection()
        {
            GUILayout.Label("— ITEMS —", GUI.skin.box);
            if (allItems == null || allItems.Count == 0)
            {
                GUILayout.Label("(liste vide)");
                return;
            }

            ItemManager itemManager = ItemManager.Instance;
            for (int i = 0; i < allItems.Count; i++)
            {
                ItemData data = allItems[i];
                if (data == null)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(data.ItemName, GUILayout.Width(200f));
                if (GUILayout.Button("Donner", GUILayout.Width(72f)))
                {
                    if (itemManager == null)
                        _statusMessage = "ItemManager absent.";
                    else if (!itemManager.TryAddItem(data))
                        _statusMessage = $"Échec item « {data.ItemName} » (slot plein ou déjà pris).";
                    else
                        _statusMessage = $"Item « {data.ItemName} » ajouté.";
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawEnemiesSection()
        {
            GUILayout.Label("— ENNEMIS —", GUI.skin.box);
            if (stageGenerator == null)
            {
                GUILayout.Label("StageGenerator non assigné.");
                return;
            }

            EnemyData forced = stageGenerator.DebugForcedEnemy;
            GUILayout.Label(forced != null
                ? $"Forçage actif : {forced.EnemyName} ({forced.EnemyRole})"
                : "Forçage actif : aucun");

            if (GUILayout.Button("Annuler le forçage"))
            {
                stageGenerator.DebugSetForcedEnemy(null);
                _statusMessage = "Forçage ennemi annulé.";
            }

            if (GUILayout.Button("Régénérer l'étage courant"))
            {
                stageGenerator.DebugRegenerateCurrentStage();
                _statusMessage = "Étage régénéré.";
            }

            if (allEnemies == null || allEnemies.Count == 0)
            {
                GUILayout.Label("(liste vide)");
                return;
            }

            for (int i = 0; i < allEnemies.Count; i++)
            {
                EnemyData data = allEnemies[i];
                if (data == null)
                    continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{data.EnemyName} ({data.EnemyRole})", GUILayout.Width(220f));
                if (GUILayout.Button("Forcer", GUILayout.Width(72f)))
                {
                    stageGenerator.DebugSetForcedEnemy(data);
                    _statusMessage = $"Ennemi forcé : {data.EnemyName}.";
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DebugSkipCurrentTurn()
        {
            if (turnManager == null)
            {
                _statusMessage = "TurnManager non assigné.";
                return;
            }

            ITurnParticipant current = turnManager.CurrentParticipant;
            if (current == null)
            {
                _statusMessage = "Aucun participant actif.";
                return;
            }

            string name = current.Name;
            turnManager.SkipCurrentTurn();
            _statusMessage = $"Tour passé : {name}.";
        }

        private void SkipCurrentStage()
        {
            if (turnManager == null)
            {
                _statusMessage = "TurnManager non assigné.";
                return;
            }

            IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;
            int killed = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                ITurnParticipant participant = participants[i];
                if (participant == null || participant.IsAlly || participant.IsDead)
                    continue;

                if (participant is Enemy enemy)
                {
                    enemy.TakeDamage(enemy.MaxHp);
                    killed++;
                }
            }

            _statusMessage = killed > 0
                ? $"Skip stage : {killed} ennemi(s) blessé(s) létalement."
                : "Aucun ennemi vivant à éliminer.";
        }

        private void GiveValise(ValiseData data, ValiseImprovementRarity rarity, int count)
        {
            ValiseManager valiseManager = ValiseManager.Instance;
            if (valiseManager == null)
            {
                _statusMessage = "ValiseManager absent.";
                return;
            }

            int success = 0;
            for (int i = 0; i < count; i++)
            {
                if (valiseManager.TryAddValise(data, rarity))
                    success++;
                else
                    break;
            }

            if (success == count)
                _statusMessage = $"Valise « {data.ValiseName} » ×{count} ({RarityShortLabel(rarity)}).";
            else if (success > 0)
                _statusMessage = $"Valise « {data.ValiseName} » : {success}/{count} (slots pleins ensuite).";
            else
                _statusMessage = $"Échec valise « {data.ValiseName} » (slots pleins ou sacrifice requis).";
        }

        private ValiseImprovementRarity GetValiseRarity(string id)
        {
            if (_valiseRarityById.TryGetValue(id, out ValiseImprovementRarity rarity))
                return rarity;
            return ValiseImprovementRarity.Commune;
        }

        private void SetValiseRarity(string id, ValiseImprovementRarity rarity)
        {
            _valiseRarityById[id] = rarity;
        }

        private int GetValiseUpgradeCount(string id)
        {
            if (_valiseUpgradeCountById.TryGetValue(id, out int count))
                return Mathf.Max(1, count);
            return 1;
        }

        private static string RarityShortLabel(ValiseImprovementRarity rarity) => rarity switch
        {
            ValiseImprovementRarity.Rare => "R",
            ValiseImprovementRarity.Epique => "E",
            ValiseImprovementRarity.Legendaire => "L",
            _ => "C"
        };
#endif
    }
}
