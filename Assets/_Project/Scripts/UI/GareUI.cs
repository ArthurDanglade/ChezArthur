using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Panneau de la Gare inter-univers (affichage des slots et fermeture).
    /// </summary>
    public class GareUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Panneau")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _talsText;
        [SerializeField] private Transform _cardsContainer;
        [SerializeField] private GareSlotCard _cardPrefab;
        [SerializeField] private Button _closeButton;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<GareSlotCard> _spawnedCards = new List<GareSlotCard>();

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (RunManager.Instance != null)
                RunManager.Instance.OnGareRequired += Show;

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnGareRequired -= Show;
                RunManager.Instance.OnTalsChanged -= UpdateTals;
            }

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(Close);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche la Gare et peuple les cartes depuis GareManager.
        /// </summary>
        public void Show()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(true);

            Time.timeScale = 0f;

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Paused);

            Populate();

            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnTalsChanged += UpdateTals;
                UpdateTals(RunManager.Instance.TalsEarned);
            }
        }

        /// <summary>
        /// Ferme la Gare et reprend le flux de run.
        /// </summary>
        public void Close()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
                GameManager.Instance.ChangeState(GameState.Playing);

            if (RunManager.Instance != null)
                RunManager.Instance.OnTalsChanged -= UpdateTals;

            if (GareManager.Instance != null)
                GareManager.Instance.CloseGare();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Populate()
        {
            ClearContainer(_cardsContainer);
            _spawnedCards.Clear();

            if (GareManager.Instance == null || _cardPrefab == null || _cardsContainer == null)
                return;

            IReadOnlyList<GareSlotData> slots = GareManager.Instance.GetCurrentSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                GareSlotData slot = slots[i];
                if (slot == null)
                    continue;

                GareSlotCard card = Instantiate(_cardPrefab, _cardsContainer);
                card.Setup(slot, i, OnBuyClicked);
                _spawnedCards.Add(card);
            }
        }

        private void OnBuyClicked(int index)
        {
            Debug.Log($"[Gare] Achat demandé slot {index}");
        }

        private void UpdateTals(int tals)
        {
            if (_talsText != null)
                _talsText.text = tals.ToString();

            if (GareManager.Instance == null)
                return;

            IReadOnlyList<GareSlotData> slots = GareManager.Instance.GetCurrentSlots();
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                GareSlotCard card = _spawnedCards[i];
                if (card == null)
                    continue;

                int slotIndex = card.SlotIndex;
                if (slotIndex < 0 || slotIndex >= slots.Count)
                    continue;

                GareSlotData slot = slots[slotIndex];
                bool canAfford = slot != null && tals >= slot.Cost;
                card.SetAffordable(canAfford);
            }
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null)
                return;

            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }
    }
}
