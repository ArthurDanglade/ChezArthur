using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// UI basique : étage, Tals, tour actuel et barres de vie des alliés.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;

        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI stageText;
        [SerializeField] private TextMeshProUGUI talsText;
        [SerializeField] private TextMeshProUGUI turnText;

        [Header("Barres de vie des alliés")]
        [SerializeField] private List<AllyHPBar> allyHPBars = new List<AllyHPBar>();

        private Coroutine _talsPunchCoroutine;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnStageCompleted += OnStageCompleted;
                RunManager.Instance.OnTalsChanged += OnTalsChanged;
                RunManager.Instance.OnRunStarted += OnRunStarted;
                UpdateStageText();
                UpdateTalsText();
            }

            if (turnManager != null)
            {
                turnManager.OnTurnChanged += OnTurnChanged;
                UpdateTurnText();

                // Attend une frame pour que TurnManager soit initialisé
                StartCoroutine(InitializeAllyHPBarsDelayed());
            }
        }

        private IEnumerator InitializeAllyHPBarsDelayed()
        {
            // Attend la fin de la frame pour que tous les Start() soient exécutés
            yield return null;

            InitializeAllyHPBars();
        }

        private void OnDestroy()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnStageCompleted -= OnStageCompleted;
                RunManager.Instance.OnTalsChanged -= OnTalsChanged;
                RunManager.Instance.OnRunStarted -= OnRunStarted;
            }

            if (turnManager != null)
                turnManager.OnTurnChanged -= OnTurnChanged;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnStageCompleted(int completedStage)
        {
            UpdateStageText();
        }

        private void OnTalsChanged(int tals)
        {
            UpdateTalsText();
        }

        private void OnRunStarted()
        {
            UpdateStageText();
            UpdateTalsText();
            UpdateTurnText();

            // Réinitialise les barres de HP avec un délai pour laisser le temps aux alliés d'être ressuscités
            StartCoroutine(InitializeAllyHPBarsDelayed());
        }

        private void OnTurnChanged(ITurnParticipant participant)
        {
            UpdateTurnText();
        }

        private void UpdateStageText()
        {
            if (RunManager.Instance == null || stageText == null) return;
            stageText.text = $"Étage {RunManager.Instance.CurrentStage}";
        }

        private void UpdateTalsText()
        {
            if (RunManager.Instance == null || talsText == null) return;

            int inFlight = TalsDropSystem.Instance != null
                ? TalsDropSystem.Instance.InFlightAmount
                : 0;
            int display = Mathf.Max(0, RunManager.Instance.TalsEarned - inFlight);
            talsText.text = $"{display}";
        }

        /// <summary>
        /// Rafraîchit l'affichage des Tals (soustrait les pièces en vol).
        /// </summary>
        /// <param name="punch">Si vrai, applique un punch d'échelle sur le compteur.</param>
        public void RefreshTalsDisplay(bool punch)
        {
            UpdateTalsText();

            if (!punch || talsText == null)
                return;

            if (_talsPunchCoroutine != null)
            {
                StopCoroutine(_talsPunchCoroutine);
                talsText.transform.localScale = Vector3.one;
            }

            _talsPunchCoroutine = StartCoroutine(PunchTalsTextRoutine());
        }

        private IEnumerator PunchTalsTextRoutine()
        {
            Transform t = talsText.transform;
            const float duration = 0.15f;
            const float peakScale = 1.12f;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                float s = Mathf.Lerp(1f, peakScale, k);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                float s = Mathf.Lerp(peakScale, 1f, k);
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            t.localScale = Vector3.one;
            _talsPunchCoroutine = null;
        }

        private void UpdateTurnText()
        {
            if (turnText == null) return;
            if (turnManager != null && turnManager.CurrentParticipant != null)
                turnText.text = $"Tour : {turnManager.CurrentParticipant.Name}";
            else
                turnText.text = "";
        }

        private void InitializeAllyHPBars()
        {
            if (turnManager == null || allyHPBars == null) return;

            int barIndex = 0;
            IReadOnlyList<ITurnParticipant> participants = turnManager.Participants;

            for (int i = 0; i < participants.Count && barIndex < allyHPBars.Count; i++)
            {
                if (!participants[i].IsAlly) continue;

                CharacterBall ball = participants[i] as CharacterBall;
                if (ball == null) continue;

                if (allyHPBars[barIndex] != null)
                    allyHPBars[barIndex].Initialize(ball);
                barIndex++;
            }

            for (int j = barIndex; j < allyHPBars.Count; j++)
            {
                if (allyHPBars[j] != null)
                    allyHPBars[j].Initialize(null);
            }
        }
    }
}
