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

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnStageCompleted += OnStageCompleted;
                RunManager.Instance.OnTalsChanged += OnTalsChanged;
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
            talsText.text = $"{RunManager.Instance.TalsEarned}";
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
