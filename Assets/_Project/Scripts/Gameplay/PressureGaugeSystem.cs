using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Passives.Handlers;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Jauge de pression (0–100). Composant central du système — triggers et rupture câblés aux Gates 2–4.
    /// Singleton de scène (pas de DontDestroyOnLoad).
    /// </summary>
    public class PressureGaugeSystem : MonoBehaviour
    {
        private const float GaugeMax = 100f;

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        private static PressureGaugeSystem _instance;

        /// <summary> Instance unique de la scène courante. </summary>
        public static PressureGaugeSystem Instance => _instance;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Montée (points de jauge)")]
        [Tooltip("Points ajoutés quand un tour ennemi commence (montée progressive).")]
        [SerializeField] private float enemyTurnRise = 8f;
        [Tooltip("Points ajoutés quand un allié ATK commence son tour (montée progressive).")]
        [SerializeField] private float allyAtkTurnRise = 6f;
        [Tooltip("Durée de la montée progressive pendant l'action (secondes).")]
        [SerializeField] private float riseDuration = 0.85f;

        [Header("Descente (points de jauge)")]
        [Tooltip("Points retirés quand un allié DEF encaisse des dégâts ennemis.")]
        [SerializeField] private float defHitDrop = 10f;
        [Tooltip("Points retirés quand un allié joue son tour en spécialisation SUP.")]
        [SerializeField] private float supTurnDrop = 8f;

        [Header("Rupture (utilisé au Gate 4)")]
        [Tooltip("Nombre de tours de rupture accordés par entité (Gate 4).")]
        [SerializeField] private int ruptureTurnsPerEntity = 2;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private float _gauge;
        private TurnManager _turnManager;
        private bool _isInRupture;
        private Coroutine _smoothRiseCoroutine;
        private readonly Dictionary<ITurnParticipant, int> _ruptureTurnCounts =
            new Dictionary<ITurnParticipant, int>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public float Value => _gauge;
        public float NormalizedValue => _gauge / GaugeMax;

        /// <summary> Points de montée par tour ennemi joué (Gate 2–3). </summary>
        public float EnemyTurnRise => enemyTurnRise;

        /// <summary> Points de montée par tour allié ATK (Gate 2–3). </summary>
        public float AllyAtkTurnRise => allyAtkTurnRise;

        /// <summary> Points de descente quand un allié DEF encaisse des dégâts (Gate 2–3). </summary>
        public float DefHitDrop => defHitDrop;

        /// <summary> Points de descente par tour allié SUP (Gate 2–3). </summary>
        public float SupTurnDrop => supTurnDrop;

        /// <summary> Tours de rupture par entité (Gate 4). </summary>
        public int RuptureTurnsPerEntity => ruptureTurnsPerEntity;

        public bool IsInRupture => _isInRupture;

        /// <summary>
        /// Proportion (0–1) d'entités photographiées satisfaites (morte ou ayant joué
        /// ruptureTurnsPerEntity tours). Alimente le drain visuel du manomètre (Gate 7).
        /// Retourne 0 si pas en rupture ou aucune entité trackée.
        /// </summary>
        public float RuptureProgress01
        {
            get
            {
                if (!_isInRupture || _ruptureTurnCounts.Count == 0)
                    return 0f;

                int satisfied = 0;
                int total = _ruptureTurnCounts.Count;

                foreach (KeyValuePair<ITurnParticipant, int> entry in _ruptureTurnCounts)
                {
                    if (IsRuptureEntrySatisfied(entry.Key, entry.Value))
                        satisfied++;
                }

                return satisfied / (float)total;
            }
        }

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand la jauge change. Paramètre : valeur normalisée (0–1). </summary>
        public event Action<float> OnGaugeChanged;

        /// <summary>
        /// Déclenché au début d'une rupture. Invoqué de manière synchrone pendant le traitement
        /// d'une fin de tour — les consommateurs (Gates 5/6/7) doivent le tolérer.
        /// </summary>
        public event Action OnRuptureTriggered;

        /// <summary> Déclenché à la fin d'une rupture (câblé au Gate 4). </summary>
        public event Action OnRuptureEnded;

        /// <summary> Déclenché quand la progression de rupture change. Paramètre : RuptureProgress01. </summary>
        public event Action<float> OnRuptureProgressChanged;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            _gauge = 0f;
        }

        private void OnDestroy()
        {
            Cleanup();
            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Branche le système sur le TurnManager (ex. RunManager.StartRun).
        /// </summary>
        public void Initialize(TurnManager turnManager)
        {
            Cleanup();
            _turnManager = turnManager;

            _isInRupture = false;
            _ruptureTurnCounts.Clear();

            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged += HandleTurnStarted;
                _turnManager.OnParticipantTurnEnded += HandleParticipantTurnEnded;
                _turnManager.OnParticipantDeath += HandleParticipantDeath;
                _turnManager.OnAllEnemiesDead += HandleAllEnemiesDead;
            }

            CharacterBall.OnAllyHitTaken += HandleAllyHitTaken;

            RunManager runManager = RunManager.Instance;
            if (runManager != null)
            {
                runManager.OnStageCompleted += HandleStageCompleted;
                runManager.OnRunEnded += HandleRunEnded;
            }

            ResetGauge("début de run");
        }

        /// <summary>
        /// Désabonne le système du TurnManager courant et des événements statiques.
        /// </summary>
        public void Cleanup()
        {
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= HandleTurnStarted;
                _turnManager.OnParticipantTurnEnded -= HandleParticipantTurnEnded;
                _turnManager.OnParticipantDeath -= HandleParticipantDeath;
                _turnManager.OnAllEnemiesDead -= HandleAllEnemiesDead;
            }

            StopSmoothRise();

            _turnManager = null;

            CharacterBall.OnAllyHitTaken -= HandleAllyHitTaken;

            RunManager runManager = RunManager.Instance;
            if (runManager != null)
            {
                runManager.OnStageCompleted -= HandleStageCompleted;
                runManager.OnRunEnded -= HandleRunEnded;
            }
        }

        /// <summary>
        /// Augmente la jauge (montée de pression).
        /// </summary>
        public void Increase(float amount, string reason)
        {
            // Jauge gelée pendant la Rupture (doc §4).
            if (IsInRupture)
                return;

            if (amount <= 0f)
                return;

            float previous = _gauge;
            ApplyDelta(amount, reason, isIncrease: true);

            if (previous < GaugeMax && _gauge >= GaugeMax && !_isInRupture)
                TriggerRupture();
        }

        /// <summary>
        /// Diminue la jauge (descente de pression).
        /// </summary>
        public void Decrease(float amount, string reason)
        {
            // Jauge gelée pendant la Rupture (doc §4).
            if (IsInRupture)
                return;

            if (amount <= 0f)
                return;

            ApplyDelta(-amount, reason, isIncrease: false);
        }

        /// <summary>
        /// Remet la jauge à zéro.
        /// </summary>
        public void ResetGauge(string reason)
        {
            StopSmoothRise();

            if (_gauge <= 0f)
                return;

            _gauge = 0f;
            Debug.Log($"[Pression] reset ({reason}) → 0/100");
            OnGaugeChanged?.Invoke(NormalizedValue);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Pose la jauge à une valeur absolue (0–100) sans déclencher la rupture sauf si valeur = 100.
        /// Usage : pré-remplir à 99 pour tester le déclenchement en jouant un tour.
        /// </summary>
        public void DebugSetGaugeAbsolute(float value, string reason = "debug")
        {
            if (_isInRupture)
                return;

            float target = Mathf.Clamp(value, 0f, GaugeMax);
            if (Mathf.Approximately(_gauge, target))
                return;

            float previous = _gauge;
            _gauge = target;
            Debug.Log($"[Pression] debug set ({reason}) → {_gauge:F0}/100");
            OnGaugeChanged?.Invoke(NormalizedValue);

            if (previous < GaugeMax && _gauge >= GaugeMax && !_isInRupture)
                TriggerRupture();
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ApplyDelta(float delta, string reason, bool isIncrease)
        {
            float previous = _gauge;
            _gauge = Mathf.Clamp(_gauge + delta, 0f, GaugeMax);

            if (Mathf.Approximately(_gauge, previous))
                return;

            float loggedAmount = Mathf.Abs(delta);
            string sign = isIncrease ? "+" : "-";
            Debug.Log($"[Pression] {sign}{loggedAmount} ({reason}) → {_gauge:F0}/100");
            OnGaugeChanged?.Invoke(NormalizedValue);
        }

        private void TriggerRupture()
        {
            StopSmoothRise();
            _isInRupture = true;
            _ruptureTurnCounts.Clear();

            int trackedCount = 0;
            if (_turnManager != null)
            {
                IReadOnlyList<ITurnParticipant> participants = _turnManager.Participants;
                if (participants != null)
                {
                    for (int i = 0; i < participants.Count; i++)
                    {
                        ITurnParticipant participant = participants[i];
                        if (participant != null && !participant.IsDead)
                        {
                            _ruptureTurnCounts[participant] = 0;
                            trackedCount++;
                        }
                    }
                }
            }

            Debug.Log(
                $"[Pression] RUPTURE DÉCLENCHÉE — {trackedCount} entités doivent jouer " +
                $"{ruptureTurnsPerEntity} tours.");

            OnRuptureTriggered?.Invoke();
            OnRuptureProgressChanged?.Invoke(0f);
        }

        private void CheckRuptureEnd()
        {
            if (!_isInRupture || _ruptureTurnCounts.Count == 0)
                return;

            foreach (KeyValuePair<ITurnParticipant, int> entry in _ruptureTurnCounts)
            {
                if (!IsRuptureEntrySatisfied(entry.Key, entry.Value))
                    return;
            }

            EndRupture("toutes les entités ont joué leurs tours");
        }

        private bool IsRuptureEntrySatisfied(ITurnParticipant participant, int turnCount)
        {
            return participant == null || participant.IsDead || turnCount >= ruptureTurnsPerEntity;
        }

        private void EndRupture(string reason)
        {
            if (!_isInRupture)
                return;

            StopSmoothRise();
            _isInRupture = false;
            _ruptureTurnCounts.Clear();

            Debug.Log($"[Pression] FIN DE RUPTURE ({reason})");

            OnRuptureEnded?.Invoke();

            ResetGauge("fin de rupture");
        }

        private void HandleTurnStarted(ITurnParticipant p)
        {
            if (_isInRupture || p == null || p.IsDead)
                return;

            if (!CanParticipantActThisTurn(p))
                return;

            float rise = GetTurnStartRise(p, out string reason);
            if (rise <= 0f)
                return;

            StartSmoothRise(rise, reason);
        }

        private static bool CanParticipantActThisTurn(ITurnParticipant p)
        {
            if (p is not Enemy enemy)
                return true;

            if (FreezeSystem.Instance != null && FreezeSystem.Instance.IsFrozenEnemy(enemy))
                return false;

            if (StunSystem.Instance != null && StunSystem.Instance.IsStunned(enemy))
                return false;

            return true;
        }

        private float GetTurnStartRise(ITurnParticipant p, out string reason)
        {
            reason = null;

            if (!p.IsAlly)
            {
                reason = "tour ennemi";
                return enemyTurnRise;
            }

            CharacterBall ally = p as CharacterBall;
            if (ally == null)
                return 0f;

            SpecializationData activeSpec = ally.ActiveSpec;
            if (activeSpec == null)
                return 0f;

            if (activeSpec.Role == CharacterRole.Attacker)
            {
                reason = "tour ATK allié";
                return allyAtkTurnRise;
            }

            return 0f;
        }

        private void StartSmoothRise(float totalAmount, string reason)
        {
            if (totalAmount <= 0f || riseDuration <= 0f)
            {
                Increase(totalAmount, reason);
                return;
            }

            StopSmoothRise();
            _smoothRiseCoroutine = StartCoroutine(SmoothRiseCoroutine(totalAmount, reason));
        }

        private void StopSmoothRise()
        {
            if (_smoothRiseCoroutine == null)
                return;

            StopCoroutine(_smoothRiseCoroutine);
            _smoothRiseCoroutine = null;
        }

        private IEnumerator SmoothRiseCoroutine(float totalAmount, string reason)
        {
            float elapsed = 0f;
            float applied = 0f;

            while (elapsed < riseDuration)
            {
                if (_isInRupture)
                {
                    _smoothRiseCoroutine = null;
                    yield break;
                }

                elapsed += Time.deltaTime;
                float targetApplied = totalAmount * Mathf.Clamp01(elapsed / riseDuration);
                float delta = targetApplied - applied;
                if (delta > 0f)
                {
                    Increase(delta, reason);
                    applied = targetApplied;
                }

                yield return null;
            }

            float remainder = totalAmount - applied;
            if (remainder > 0f && !_isInRupture)
                Increase(remainder, reason);

            _smoothRiseCoroutine = null;
        }

        private void HandleParticipantTurnEnded(ITurnParticipant p)
        {
            if (_isInRupture)
            {
                if (p != null && _ruptureTurnCounts.TryGetValue(p, out int count))
                {
                    count++;
                    _ruptureTurnCounts[p] = count;
                    Debug.Log($"[Pression] Rupture : {p.Name} {count}/{ruptureTurnsPerEntity}");
                    OnRuptureProgressChanged?.Invoke(RuptureProgress01);
                    CheckRuptureEnd();
                }

                return;
            }

            if (p == null || p.IsDead || !p.IsAlly)
                return;

            CharacterBall ally = p as CharacterBall;
            if (ally == null)
                return;

            SpecializationData activeSpec = ally.ActiveSpec;
            if (activeSpec == null)
                return;

            if (activeSpec.Role == CharacterRole.Support)
                Decrease(supTurnDrop, "tour SUP allié");
        }

        private void HandleParticipantDeath(ITurnParticipant p)
        {
            if (!_isInRupture)
                return;

            CheckRuptureEnd();
        }

        private void HandleAllEnemiesDead()
        {
            if (!_isInRupture)
                return;

            EndRupture("tous les ennemis vaincus");
        }

        private void HandleStageCompleted(int completedStage)
        {
            if (!_isInRupture)
                return;

            EndRupture("fin d'étage — filet de sécurité");
        }

        private void HandleRunEnded(bool victory)
        {
            if (!_isInRupture)
                return;

            EndRupture("fin de run");
        }

        private void HandleAllyHitTaken(CharacterBall ball, int damage)
        {
            if (ball == null || ball.IsDead)
                return;

            SpecializationData activeSpec = ball.ActiveSpec;
            if (activeSpec == null || activeSpec.Role != CharacterRole.Defender)
                return;

            // Descente par coup, forfaitaire — le montant des dégâts est ignoré (levier = defHitDrop).
            Decrease(defHitDrop, "coup encaissé par DEF");
        }

        // ═══════════════════════════════════════════
        // DEBUG (validation isolée)
        // ═══════════════════════════════════════════
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [ContextMenu("Debug +10")]
        private void DebugIncrease10()
        {
            Increase(10f, "debug");
        }

        [ContextMenu("Debug -10")]
        private void DebugDecrease10()
        {
            Decrease(10f, "debug");
        }

        [ContextMenu("Debug → 99 (pré-rupture)")]
        private void DebugSet99()
        {
            DebugSetGaugeAbsolute(99f, "debug pré-rupture");
        }

        [ContextMenu("Debug → Déclencher Rupture")]
        private void DebugTriggerRupture()
        {
            DebugSetGaugeAbsolute(GaugeMax, "debug rupture");
        }

        [ContextMenu("Debug → Forcer fin de Rupture")]
        private void DebugForceEndRupture()
        {
            DebugEndRupture();
        }

        /// <summary>
        /// Coupe la rupture en cours (menu debug / tests).
        /// </summary>
        public void DebugEndRupture()
        {
            EndRupture("debug");
        }
#endif
    }
}
