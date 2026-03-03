using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Écran de défaite affiché quand tous les alliés meurent.
    /// </summary>
    public class DefeatUI : MonoBehaviour
    {
        [Header("Références UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI stageReachedText;
        [SerializeField] private TextMeshProUGUI talsEarnedText;
        [SerializeField] private TextMeshProUGUI bonusCountText;
        [SerializeField] private Button retryButton;

        /// <summary> Déclenché quand le joueur clique sur Réessayer. </summary>
        public event Action OnRetryClicked;

        private void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetryClicked);

            // Cache l'écran au démarrage
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Start()
        {
            Debug.Log($"[DefeatUI] Start - RunManager.Instance est {(RunManager.Instance != null ? "présent" : "NULL")}");

            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunEnded += HandleRunEnded;
                Debug.Log("[DefeatUI] Abonné à OnRunEnded");
            }
            else
            {
                Debug.LogWarning("[DefeatUI] RunManager.Instance est null, impossible de s'abonner !");
            }
        }

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);

            if (RunManager.Instance != null)
                RunManager.Instance.OnRunEnded -= HandleRunEnded;
        }

        /// <summary>
        /// Appelé quand la run se termine.
        /// </summary>
        private void HandleRunEnded(bool victory)
        {
            Debug.Log($"[DefeatUI] HandleRunEnded appelé, victory = {victory}");

            if (victory) return;

            Show();
        }

        /// <summary>
        /// Affiche l'écran de défaite avec les stats de la run.
        /// </summary>
        public void Show()
        {
            Debug.Log("[DefeatUI] Show appelé");

            UpdateStats();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                Debug.Log("[DefeatUI] panelRoot activé");
            }
            else
            {
                Debug.LogWarning("[DefeatUI] panelRoot est NULL !");
            }

            Time.timeScale = 0f;
        }

        /// <summary>
        /// Cache l'écran de défaite.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            Time.timeScale = 1f;
        }

        /// <summary>
        /// Met à jour les textes avec les stats de la run.
        /// </summary>
        private void UpdateStats()
        {
            int stageReached = RunManager.Instance != null ? RunManager.Instance.CurrentStage : 1;
            int talsEarned = RunManager.Instance != null ? RunManager.Instance.TalsEarned : 0;
            int bonusCount = BonusManager.Instance != null ? BonusManager.Instance.ActiveBonusCount : 0;

            if (titleText != null)
                titleText.text = "Game Over";

            if (stageReachedText != null)
                stageReachedText.text = $"Étage atteint : {stageReached}";

            if (talsEarnedText != null)
                talsEarnedText.text = $"Tals gagnés : {talsEarned}";

            if (bonusCountText != null)
                bonusCountText.text = $"Bonus collectés : {bonusCount}";
        }

        private void HandleRetryClicked()
        {
            Hide();
            OnRetryClicked?.Invoke();

            // Relance une nouvelle run
            if (RunManager.Instance != null)
                RunManager.Instance.StartRun();
        }
    }
}
