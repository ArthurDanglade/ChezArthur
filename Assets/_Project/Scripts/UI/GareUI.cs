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
        [SerializeField] private Transform _healContainer;
        [SerializeField] private GareSlotCard _healCardPrefab;
        [SerializeField] private Transform _offersContainer;
        [SerializeField] private GareSlotCard _offerCardPrefab;
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
            ClearContainer(_healContainer);
            ClearContainer(_offersContainer);
            _spawnedCards.Clear();

            if (GareManager.Instance == null
                || _healContainer == null
                || _healCardPrefab == null
                || _offersContainer == null
                || _offerCardPrefab == null)
                return;

            IReadOnlyList<GareSlotData> slots = GareManager.Instance.GetCurrentSlots();
            for (int i = 0; i < slots.Count; i++)
            {
                GareSlotData slot = slots[i];
                if (slot == null)
                    continue;

                bool isHeal = slot.SlotType == GareSlotType.HealSmall
                    || slot.SlotType == GareSlotType.HealMedium
                    || slot.SlotType == GareSlotType.HealLarge;
                GareSlotCard prefab = isHeal ? _healCardPrefab : _offerCardPrefab;
                Transform parent = isHeal ? _healContainer : _offersContainer;

                GareSlotCard card = Instantiate(prefab, parent);
                card.Setup(slot, i, OnBuyClicked);
                _spawnedCards.Add(card);
            }

            RefreshAffordability();
        }

        private void OnBuyClicked(int index)
        {
            if (GareManager.Instance == null)
                return;

            IReadOnlyList<GareSlotData> slots = GareManager.Instance.GetCurrentSlots();
            bool wasHeal = index >= 0 && index < slots.Count &&
                (slots[index].SlotType == GareSlotType.HealSmall
                    || slots[index].SlotType == GareSlotType.HealMedium
                    || slots[index].SlotType == GareSlotType.HealLarge);

            if (!GareManager.Instance.TryPurchase(index))
                return;

            if (index >= 0 && index < _spawnedCards.Count)
            {
                if (wasHeal)
                    _spawnedCards[index].UpdateCost();
                else
                    _spawnedCards[index].MarkSold();
            }

            RefreshAffordability();
        }

        private void RefreshAffordability()
        {
            if (GareManager.Instance == null)
                return;

            IReadOnlyList<GareSlotData> slots = GareManager.Instance.GetCurrentSlots();
            for (int i = 0; i < _spawnedCards.Count && i < slots.Count; i++)
            {
                if (slots[i].IsPurchased)
                    _spawnedCards[i].MarkSold();
                else
                    _spawnedCards[i].SetAffordable(GareManager.Instance.CanPurchase(i));
            }
        }

        private void UpdateTals(int tals)
        {
            if (_talsText != null)
                _talsText.text = tals.ToString();

            RefreshAffordability();
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
