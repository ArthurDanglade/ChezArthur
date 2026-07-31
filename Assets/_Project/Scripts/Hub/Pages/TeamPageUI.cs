using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Page Équipe : dock slots + grille collection triée (Gate 5.a).
    /// </summary>
    public class TeamPageUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Slots Équipe (4)")]
        [SerializeField] private TeamSlotUI[] teamSlots;

        [Header("Grille Collection")]
        [SerializeField] private Transform collectionContainer;
        [SerializeField] private CharacterCardUI cardPrefab;

        [Header("Tri")]
        [SerializeField] private CollectionSortBar sortBar;

        [Header("Références")]
        [SerializeField] private CharacterDatabase characterDatabase;

        [Header("Popup Détails")]
        [SerializeField] private CharacterDetailPopup detailPopup;

        [Header("Drag 5.b")]
        [SerializeField] private TeamDragController dragController;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<CharacterCardUI> _spawnedCards = new List<CharacterCardUI>(32);
        private readonly List<OwnedCharacter> _sortBuffer = new List<OwnedCharacter>(64);
        private Comparison<OwnedCharacter> _compareByRarity;
        private Comparison<OwnedCharacter> _compareByLevel;
        private Comparison<OwnedCharacter> _compareByRecent;
        private bool _persistentEventsSubscribed;
        private bool _sortBarSubscribed;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public CharacterDetailPopup DetailPopup => detailPopup;
        public TeamSlotUI[] TeamSlots => teamSlots;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            SubscribePersistentEvents();
            SubscribeSortBar();
            StartCoroutine(DelayedRefresh());
        }

        private void OnDestroy()
        {
            UnsubscribePersistentEvents();
            UnsubscribeSortBar();
        }

        private IEnumerator DelayedRefresh()
        {
            yield return null;
            SubscribePersistentEvents();
            RefreshDisplay();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Rafraîchit équipe + collection.
        /// </summary>
        public void RefreshDisplay()
        {
            RefreshTeamSlots();
            RefreshCollection();
            if (dragController != null)
                dragController.RefreshHintVisibility();
        }

        /// <summary>
        /// Ouvre le popup détail (tap slot rempli / carte).
        /// </summary>
        public void OpenDetail(CharacterData data, OwnedCharacter owned)
        {
            OnCardClicked(data, owned);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — ABONNEMENTS
        // ═══════════════════════════════════════════

        private void SubscribePersistentEvents()
        {
            if (_persistentEventsSubscribed)
                return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            PersistentManager.Instance.Characters.OnTeamChanged += RefreshTeamSlots;
            PersistentManager.Instance.Characters.OnCharacterAdded += RefreshDisplay;
            _persistentEventsSubscribed = true;
        }

        private void UnsubscribePersistentEvents()
        {
            if (!_persistentEventsSubscribed)
                return;
            if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                PersistentManager.Instance.Characters.OnTeamChanged -= RefreshTeamSlots;
                PersistentManager.Instance.Characters.OnCharacterAdded -= RefreshDisplay;
            }

            _persistentEventsSubscribed = false;
        }

        private void SubscribeSortBar()
        {
            if (_sortBarSubscribed || sortBar == null)
                return;
            sortBar.OnSortModeChanged += HandleSortModeChanged;
            _sortBarSubscribed = true;
        }

        private void UnsubscribeSortBar()
        {
            if (!_sortBarSubscribed || sortBar == null)
                return;
            sortBar.OnSortModeChanged -= HandleSortModeChanged;
            _sortBarSubscribed = false;
        }

        private void HandleSortModeChanged(CollectionSortBar.SortMode _)
        {
            RefreshCollection();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — AFFICHAGE
        // ═══════════════════════════════════════════

        private void RefreshTeamSlots()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;
            if (teamSlots == null || teamSlots.Length == 0)
            {
                Debug.LogWarning("[TeamPageUI] teamSlots non assigné ou vide.", this);
                return;
            }

            CharacterManager characters = PersistentManager.Instance.Characters;
            IReadOnlyList<string> teamIds = characters.GetSelectedTeamIds();

            for (int i = 0; i < teamSlots.Length; i++)
            {
                if (teamSlots[i] == null)
                    continue;

                teamSlots[i].SetUiSlotIndex(i);
                if (dragController != null)
                    teamSlots[i].BindDragController(dragController);

                if (i < teamIds.Count)
                {
                    string id = teamIds[i];
                    var (data, owned) = characters.GetCharacterWithData(id);
                    if (data == null || owned == null)
                    {
                        Debug.LogWarning(
                            $"[TeamPageUI] Slot UI #{i} id='{id}' → data/owned NULL — slot vidé.");
                        teamSlots[i].SetEmpty();
                    }
                    else
                    {
                        teamSlots[i].SetCharacter(data, owned);
                    }
                }
                else
                {
                    teamSlots[i].SetEmpty();
                }
            }

            UpdateCardsTeamState();
        }

        private void RefreshCollection()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;
            if (collectionContainer == null || cardPrefab == null)
                return;

            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                if (_spawnedCards[i] != null)
                    Destroy(_spawnedCards[i].gameObject);
            }

            _spawnedCards.Clear();

            CharacterManager characters = PersistentManager.Instance.Characters;
            IReadOnlyList<OwnedCharacter> ownedCharacters = characters.GetOwnedCharacters();

            _sortBuffer.Clear();
            for (int i = 0; i < ownedCharacters.Count; i++)
            {
                if (ownedCharacters[i] != null)
                    _sortBuffer.Add(ownedCharacters[i]);
            }

            CollectionSortBar.SortMode mode = sortBar != null
                ? sortBar.CurrentMode
                : CollectionSortBar.SortMode.Rarity;

            SortOwnedBuffer(mode);

            ScrollRect scroll = collectionContainer.GetComponentInParent<ScrollRect>();
            RectTransform scrollViewport = scroll != null ? scroll.viewport : null;

            for (int i = 0; i < _sortBuffer.Count; i++)
            {
                OwnedCharacter owned = _sortBuffer[i];
                CharacterData data = characterDatabase != null
                    ? characterDatabase.GetById(owned.characterId)
                    : null;
                if (data == null)
                    continue;

                CharacterCardUI card = Instantiate(cardPrefab, collectionContainer);
                card.Setup(data, owned, OnCardClicked);
                card.SetInTeam(characters.IsInTeam(owned.characterId));
                if (dragController != null)
                    card.BindDragController(dragController);
                card.SetShineViewport(scrollViewport);
                _spawnedCards.Add(card);
            }
        }

        private void UpdateCardsTeamState()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            CharacterManager characters = PersistentManager.Instance.Characters;
            for (int i = 0; i < _spawnedCards.Count; i++)
            {
                CharacterCardUI card = _spawnedCards[i];
                if (card != null)
                    card.SetInTeam(characters.IsInTeam(card.CharacterId));
            }
        }

        private void OnCardClicked(CharacterData data, OwnedCharacter owned)
        {
            if (detailPopup != null)
                detailPopup.Open(data, owned);
            else
                Debug.LogWarning("[TeamPageUI] detailPopup est null !");
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — TRI (zéro alloc Comparison)
        // ═══════════════════════════════════════════

        private void SortOwnedBuffer(CollectionSortBar.SortMode mode)
        {
            if (_compareByRarity == null)
            {
                _compareByRarity = CompareByRarity;
                _compareByLevel = CompareByLevel;
                _compareByRecent = CompareByRecent;
            }

            switch (mode)
            {
                case CollectionSortBar.SortMode.Level:
                    _sortBuffer.Sort(_compareByLevel);
                    break;
                case CollectionSortBar.SortMode.Recent:
                    // AddCharacter append → index élevé = plus récent. Récent = inverse.
                    // Si l'ordre d'ajout n'est plus stable un jour → fallback index collection (TODO).
                    _sortBuffer.Sort(_compareByRecent);
                    break;
                default:
                    _sortBuffer.Sort(_compareByRarity);
                    break;
            }
        }

        private int CompareByRarity(OwnedCharacter a, OwnedCharacter b)
        {
            CharacterData da = ResolveData(a);
            CharacterData db = ResolveData(b);
            int ra = da != null ? RarityRank(da.Rarity) : -1;
            int rb = db != null ? RarityRank(db.Rarity) : -1;
            int cmp = rb.CompareTo(ra); // LR → SSR → SR
            if (cmp != 0)
                return cmp;

            int la = a != null ? a.level : 0;
            int lb = b != null ? b.level : 0;
            cmp = lb.CompareTo(la);
            if (cmp != 0)
                return cmp;

            string na = da != null ? da.CharacterName : string.Empty;
            string nb = db != null ? db.CharacterName : string.Empty;
            return string.CompareOrdinal(na, nb);
        }

        private int CompareByLevel(OwnedCharacter a, OwnedCharacter b)
        {
            int la = a != null ? a.level : 0;
            int lb = b != null ? b.level : 0;
            int cmp = lb.CompareTo(la);
            if (cmp != 0)
                return cmp;
            return CompareByRarity(a, b);
        }

        private int CompareByRecent(OwnedCharacter a, OwnedCharacter b)
        {
            // Ordre d'acquisition = ordre dans GetOwnedCharacters (append à l'ajout).
            int ia = IndexInOwned(a);
            int ib = IndexInOwned(b);
            return ib.CompareTo(ia); // plus récent d'abord
        }

        private int IndexInOwned(OwnedCharacter owned)
        {
            if (owned == null || PersistentManager.Instance == null
                || PersistentManager.Instance.Characters == null)
                return -1;

            IReadOnlyList<OwnedCharacter> list =
                PersistentManager.Instance.Characters.GetOwnedCharacters();
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], owned)
                    || (list[i] != null && list[i].characterId == owned.characterId))
                    return i;
            }

            return -1;
        }

        private CharacterData ResolveData(OwnedCharacter owned)
        {
            if (owned == null || characterDatabase == null)
                return null;
            return characterDatabase.GetById(owned.characterId);
        }

        private static int RarityRank(CharacterRarity rarity) => rarity switch
        {
            CharacterRarity.LR => 2,
            CharacterRarity.SSR => 1,
            CharacterRarity.SR => 0,
            _ => -1
        };
    }
}
