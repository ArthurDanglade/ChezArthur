using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Roguelike;
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
        private Vector2 _statesScrollPosition;
        private readonly Dictionary<string, ValiseImprovementRarity> _valiseRarityById =
            new Dictionary<string, ValiseImprovementRarity>();
        private readonly Dictionary<string, int> _valiseUpgradeCountById =
            new Dictionary<string, int>();
        private GUIStyle _panelStyle;
        private GUIStyle _statusStyle;
        private bool _stylesInitialized;
        private Texture2D _debugPortraitTexture;
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
        }

        private static List<T> LoadAssets<T>() where T : Object
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
            DrawCheatsSection();
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
        }

        private void DrawPortraitLoaderSection()
        {
            GUILayout.Label("— PORTRAIT (Gate 2 test) —", GUI.skin.box);

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

        private void DrawStatesSection()
        {
            GUILayout.Label("— ÉTATS —", GUI.skin.box);
            if (turnManager == null)
            {
                GUILayout.Label("TurnManager non assigné.");
                return;
            }

            _statesScrollPosition = GUILayout.BeginScrollView(_statesScrollPosition, GUILayout.Height(180f));

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
                    GUILayout.Space(4f);
                }
            }

            if (!anyEnemy)
                GUILayout.Label("(aucun ennemi vivant)");

            GUILayout.EndScrollView();
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
