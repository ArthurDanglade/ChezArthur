using System;
using System.Collections.Generic;
using UnityEngine;
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

        [Header("Cartes")]
        [SerializeField] private List<BonusCard> bonusCards = new List<BonusCard>();

        private List<BonusData> _currentChoices = new List<BonusData>();

        /// <summary> Déclenché quand le joueur a choisi un bonus et que l'écran se ferme. </summary>
        public event Action OnSelectionComplete;

        private void Awake()
        {
            if (bonusCards == null) return;
            for (int i = 0; i < bonusCards.Count; i++)
            {
                if (bonusCards[i] != null)
                    bonusCards[i].OnCardSelected += OnBonusSelected;
            }
        }

        private void OnDestroy()
        {
            if (bonusCards == null) return;
            for (int i = 0; i < bonusCards.Count; i++)
            {
                if (bonusCards[i] != null)
                    bonusCards[i].OnCardSelected -= OnBonusSelected;
            }
        }

        /// <summary>
        /// Affiche l'écran de sélection avec 3 bonus et met le jeu en pause.
        /// </summary>
        public void Show()
        {
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
        }

        /// <summary>
        /// Cache l'écran de sélection et reprend le jeu.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            Time.timeScale = 1f;
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
