using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// UI de sélection des bonus (affichée tous les 3 étages, non branchée au RunManager dans cette étape).
    /// </summary>
    public class BonusSelectionUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private BonusPool bonusPool;
        [SerializeField] private GameObject panelRoot;
        [Header("Mode Roguelike")]
        [SerializeField] private bool useRoguelikePool = false;
        [SerializeField] private RoguelikeSelectionPool roguelikePool;

        [Header("Cartes")]
        [SerializeField] private List<BonusCard> bonusCards = new List<BonusCard>();

        private List<BonusData> _currentChoices = new List<BonusData>();
        private List<RoguelikeOption> _currentRoguelikeOptions = new List<RoguelikeOption>();

        /// <summary> Déclenché quand le joueur a choisi un bonus et que l'écran se ferme. </summary>
        public event Action OnSelectionComplete;

        private void Awake()
        {
            if (bonusCards == null) return;
            for (int i = 0; i < bonusCards.Count; i++)
            {
                if (bonusCards[i] != null)
                {
                    bonusCards[i].OnCardSelected += OnBonusSelected;
                    bonusCards[i].OnRoguelikeOptionSelected += OnRoguelikeOptionSelectedHandler;
                }
            }
        }

        private void OnDestroy()
        {
            if (bonusCards == null) return;
            for (int i = 0; i < bonusCards.Count; i++)
            {
                if (bonusCards[i] != null)
                {
                    bonusCards[i].OnCardSelected -= OnBonusSelected;
                    bonusCards[i].OnRoguelikeOptionSelected -= OnRoguelikeOptionSelectedHandler;
                }
            }
        }

        /// <summary>
        /// Affiche l'écran de sélection avec 3 bonus et met le jeu en pause.
        /// </summary>
        public void Show()
        {
            if (useRoguelikePool && roguelikePool != null)
            {
                _currentRoguelikeOptions = roguelikePool.GenerateOptions(3);

                for (int i = 0; i < bonusCards.Count; i++)
                {
                    if (bonusCards[i] == null) continue;
                    if (i < _currentRoguelikeOptions.Count)
                    {
                        Debug.Log($"[BonusSelectionUI] Option {i}: {_currentRoguelikeOptions[i]?.Type}");
                        bonusCards[i].SetupRoguelike(_currentRoguelikeOptions[i]);
                        bonusCards[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        bonusCards[i].gameObject.SetActive(false);
                    }
                }

                if (panelRoot != null)
                    panelRoot.SetActive(true);

                Time.timeScale = 0f;
                if (GameManager.Instance != null)
                    GameManager.Instance.ChangeState(GameState.Paused);
                return;
            }

            if (bonusPool == null)
            {
                Debug.LogWarning("[BonusSelectionUI] bonusPool est null.", this);
                return;
            }

            _currentChoices = bonusPool.GetRandomBonuses(3);

            for (int i = 0; i < bonusCards.Count; i++)
            {
                if (bonusCards[i] == null) continue;
                if (i < _currentChoices.Count)
                {
                    bonusCards[i].Setup(_currentChoices[i]);
                    bonusCards[i].gameObject.SetActive(true);
                }
                else
                {
                    bonusCards[i].gameObject.SetActive(false);
                }
            }

            if (panelRoot != null)
                panelRoot.SetActive(true);

            Time.timeScale = 0f;
            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Paused);
        }

        /// <summary>
        /// Cache l'écran de sélection et reprend le jeu.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Active ou désactive le mode pool roguelike.
        /// </summary>
        public void SetUseRoguelikePool(bool enabled)
        {
            useRoguelikePool = enabled;
        }

        private void OnRoguelikeOptionSelectedHandler(RoguelikeOption option)
        {
            if (option == null) return;

            switch (option.Type)
            {
                case RoguelikeOptionType.ValiseNew:
                case RoguelikeOptionType.ValiseUpgrade:
                    if (ValiseManager.Instance != null && option.ValiseData != null)
                        ValiseManager.Instance.TryAddValise(option.ValiseData, option.ValiseRarity);
                    if (roguelikePool != null)
                        roguelikePool.NotifyValiseSelected(option.ValiseRarity);
                    break;

                case RoguelikeOptionType.Item:
                    if (ItemManager.Instance != null && option.ItemData != null)
                        ItemManager.Instance.TryAddItem(option.ItemData);
                    break;
            }

            Hide();
            OnSelectionComplete?.Invoke();
        }

        private void OnBonusSelected(BonusData bonus)
        {
            if (BonusManager.Instance != null && bonus != null)
                BonusManager.Instance.AddBonus(bonus);

            if (bonusPool != null)
            {
                if (bonus != null && bonus.IsSpecialBonus)
                    bonusPool.ResetPity();
                else
                    bonusPool.IncrementPity();
            }

            Hide();
            OnSelectionComplete?.Invoke();
        }
    }
}
