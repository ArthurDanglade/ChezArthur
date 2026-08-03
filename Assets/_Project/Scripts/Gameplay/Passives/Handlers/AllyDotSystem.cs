using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Feedback;
using ChezArthur.UI;

namespace ChezArthur.Gameplay.Passives.Handlers
{
    /// <summary>
    /// DOT génériques sur ALLIÉS (premier du jeu) — Brûlure (R8).
    /// Auto-bootstrap : déviation documentée vs BurnTickSystem (câblé par RunManager, gelé).
    /// Source = attribution seulement : la Brûlure survit à la mort du lanceur
    /// (seul ExpiresWithSource lie un debuff à la vie de sa source).
    /// </summary>
    public class AllyDotSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // TYPES PRIVÉS
        // ═══════════════════════════════════════════

        private struct DotEntry
        {
            public CharacterBall Target;
            public float PercentPerCycle;
            public int RemainingCycles;
            public Enemy Source;
        }

        // ═══════════════════════════════════════════
        // SINGLETON / AUTO-BOOTSTRAP
        // ═══════════════════════════════════════════

        public static AllyDotSystem Instance { get; private set; }

        private static AllyDotSystem EnsureInstance()
        {
            if (Instance != null)
                return Instance;

            // Scène courante uniquement — PAS de DontDestroyOnLoad (meurt et renaît avec Game).
            var go = new GameObject("AllyDotSystem");
            return go.AddComponent<AllyDotSystem>();
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private readonly List<DotEntry> _dots = new List<DotEntry>(8);
        private TurnManager _turnManager;
        private bool _subscribedRun;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureTurnManagerSubscription();
            EnsureRunManagerSubscription();
        }

        private void OnDestroy()
        {
            UnsubscribeTurnManager();
            UnsubscribeRunManager();
            if (Instance == this)
                Instance = null;
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Applique une Brûlure : % PV max par cycle, durée en cycles.
        /// Réapplication sur la même cible = REMPLACEMENT (valeur + durée) — politique G4, retouchable au G7.
        /// </summary>
        public static void ApplyBurn(CharacterBall target, float percentMaxHpPerCycle, int cycles, Enemy source)
        {
            if (target == null || target.IsDead || percentMaxHpPerCycle <= 0f || cycles <= 0)
                return;

            AllyDotSystem sys = EnsureInstance();
            sys.ApplyBurnInternal(target, percentMaxHpPerCycle, cycles, source);
        }

        /// <summary> Vide tous les DOT alliés (fin d'étage). </summary>
        public static void ClearAllDots()
        {
            if (Instance == null)
                return;
            Instance._dots.Clear();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyBurnInternal(CharacterBall target, float percentMaxHpPerCycle, int cycles, Enemy source)
        {
            for (int i = 0; i < _dots.Count; i++)
            {
                if (!ReferenceEquals(_dots[i].Target, target))
                    continue;

                DotEntry replaced = _dots[i];
                replaced.PercentPerCycle = percentMaxHpPerCycle;
                replaced.RemainingCycles = cycles;
                replaced.Source = source;
                _dots[i] = replaced;
                EmitBurnApplied(target);
                return;
            }

            _dots.Add(new DotEntry
            {
                Target = target,
                PercentPerCycle = percentMaxHpPerCycle,
                RemainingCycles = cycles,
                Source = source
            });
            EmitBurnApplied(target);
        }

        private static void EmitBurnApplied(CharacterBall target)
        {
            if (target == null) return;
            FeedbackContext ctx = FeedbackContext.At(target.transform.position);
            ctx.Target = target.transform;
            ctx.TargetBall = target;
            CombatFeedbackService.PlayEvent(FeedbackEventId.BurnApplied, in ctx);
        }

        private void EnsureTurnManagerSubscription()
        {
            if (_turnManager == null)
                _turnManager = FindObjectOfType<TurnManager>();

            if (_turnManager == null)
                return;

            _turnManager.OnCycleStarted -= OnCycleStarted;
            _turnManager.OnCycleStarted += OnCycleStarted;
        }

        private void UnsubscribeTurnManager()
        {
            if (_turnManager == null)
                return;
            _turnManager.OnCycleStarted -= OnCycleStarted;
            _turnManager = null;
        }

        private void EnsureRunManagerSubscription()
        {
            if (_subscribedRun || RunManager.Instance == null)
                return;

            RunManager.Instance.OnStageCompleted += OnStageCompleted;
            _subscribedRun = true;
        }

        private void UnsubscribeRunManager()
        {
            if (!_subscribedRun)
                return;

            if (RunManager.Instance != null)
                RunManager.Instance.OnStageCompleted -= OnStageCompleted;

            _subscribedRun = false;
        }

        private void OnStageCompleted(int _)
        {
            ClearAllDots();
        }

        private void OnCycleStarted()
        {
            // Re-résolution paresseuse si TurnManager perdu après reload.
            if (_turnManager == null)
                EnsureTurnManagerSubscription();

            for (int i = _dots.Count - 1; i >= 0; i--)
            {
                DotEntry entry = _dots[i];
                CharacterBall target = entry.Target;
                if (target == null || target.IsDead)
                {
                    _dots.RemoveAt(i);
                    continue;
                }

                int damage = Mathf.Max(1, Mathf.CeilToInt(target.EffectiveMaxHp * entry.PercentPerCycle));
                target.SuppressNextDamagePopup();
                target.TakePureDamage(damage);
                FloatingNumberSpawner.Instance?.ShowBurn(damage, target.transform.position);

                FeedbackContext tickCtx = FeedbackContext.At(target.transform.position);
                tickCtx.Target = target.transform;
                tickCtx.TargetBall = target;
                CombatFeedbackService.PlayEvent(FeedbackEventId.BurnTick, in tickCtx);

                entry.RemainingCycles--;
                if (entry.RemainingCycles <= 0)
                {
                    _dots.RemoveAt(i);
                    FeedbackContext endCtx = FeedbackContext.At(target.transform.position);
                    endCtx.Target = target.transform;
                    endCtx.TargetBall = target;
                    CombatFeedbackService.PlayEvent(FeedbackEventId.BurnEnded, in endCtx);
                }
                else
                    _dots[i] = entry;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Outillage checklist G4-P4 — aucun code de jeu ne l'appelle.
        /// </summary>
        [ContextMenu("DEV — Brûler l'allié le plus proche (3 %, 2 cycles)")]
        private void DevBurnClosestAlly()
        {
            EnsureTurnManagerSubscription();
            if (_turnManager == null)
            {
                Debug.LogWarning("[AllyDotSystem] TurnManager introuvable.");
                return;
            }

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            CharacterBall closest = null;
            float bestSqr = float.MaxValue;
            Vector2 origin = transform.position;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall a = allies[i];
                if (a == null || a.IsDead)
                    continue;
                float sqr = ((Vector2)a.transform.position - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    closest = a;
                }
            }

            if (closest == null)
            {
                Debug.LogWarning("[AllyDotSystem] Aucun allié vivant.");
                return;
            }

            ApplyBurn(closest, 0.03f, 2, null);
            Debug.Log($"[AllyDotSystem] DEV burn → {closest.Name} (3 %, 2 cycles)");
        }
#endif
    }
}
