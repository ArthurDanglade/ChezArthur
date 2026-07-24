using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Characters;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Gère l'affichage de la page Équipe (slots équipe + collection).
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

        [Header("Références")]
        [SerializeField] private CharacterDatabase characterDatabase;

        [Header("Popup Détails")]
        [SerializeField] private CharacterDetailPopup detailPopup;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<CharacterCardUI> _spawnedCards = new List<CharacterCardUI>();
        private bool _persistentEventsSubscribed;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            SubscribePersistentEvents();
            // Attendre une frame avant le refresh pour que l'UI soit prête
            StartCoroutine(DelayedRefresh());
        }

        private void OnDestroy()
        {
            UnsubscribePersistentEvents();
        }

        private IEnumerator DelayedRefresh()
        {
            yield return null; // Attend une frame
            // PersistentManager peut ne pas être prêt au tout premier OnEnable.
            SubscribePersistentEvents();
            RefreshDisplay();
        }

        /// <summary>
        /// S'abonne aux événements persistants une seule fois.
        /// Ne pas se désabonner dans OnDisable : la page peut être masquée
        /// (popup, autre onglet) et l'équipe serait alors modifiée sans refresh.
        /// </summary>
        private void SubscribePersistentEvents()
        {
            if (_persistentEventsSubscribed) return;
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;

            PersistentManager.Instance.Characters.OnTeamChanged += RefreshTeamSlots;
            PersistentManager.Instance.Characters.OnCharacterAdded += RefreshDisplay;
            _persistentEventsSubscribed = true;
        }

        private void UnsubscribePersistentEvents()
        {
            if (!_persistentEventsSubscribed) return;
            if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                PersistentManager.Instance.Characters.OnTeamChanged -= RefreshTeamSlots;
                PersistentManager.Instance.Characters.OnCharacterAdded -= RefreshDisplay;
            }
            _persistentEventsSubscribed = false;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Rafraîchit tout l'affichage (équipe + collection).
        /// </summary>
        public void RefreshDisplay()
        {
            RefreshTeamSlots();
            RefreshCollection();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour les 4 slots de l'équipe.
        /// </summary>
        private void RefreshTeamSlots()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;
            if (teamSlots == null || teamSlots.Length == 0)
            {
                Debug.LogWarning("[TeamPageUI] teamSlots non assigné ou vide — les emplacements d'équipe ne s'afficheront pas.", this);
                return;
            }

            var characters = PersistentManager.Instance.Characters;
            var teamIds = characters.GetSelectedTeamIds();

            for (int i = 0; i < teamSlots.Length; i++)
            {
                if (teamSlots[i] == null) continue;

                teamSlots[i].SetUiSlotIndex(i);

                if (i < teamIds.Count)
                {
                    string id = teamIds[i];
                    var (data, owned) = characters.GetCharacterWithData(id);
                    if (data == null || owned == null)
                    {
                        Debug.LogWarning($"[TeamPageUI] Slot UI #{i} id='{id}' → data ou owned NULL " +
                                         $"(database manager null ? id inconnu ?) — affichage vidé.");
                    }

                    teamSlots[i].SetCharacter(data, owned);
                }
                else
                {
                    teamSlots[i].SetEmpty();
                }
            }

            // Met à jour l'état "dans l'équipe" des cartes
            UpdateCardsTeamState();
        }

        /// <summary>
        /// Rafraîchit la grille de collection.
        /// </summary>
        private void RefreshCollection()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;
            if (collectionContainer == null || cardPrefab == null) return;

            // Supprime les anciennes cartes
            foreach (var card in _spawnedCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _spawnedCards.Clear();

            // Crée une carte pour chaque personnage possédé
            var ownedCharacters = PersistentManager.Instance.Characters.GetOwnedCharacters();

            foreach (var owned in ownedCharacters)
            {
                CharacterData data = characterDatabase != null ? characterDatabase.GetById(owned.characterId) : null;
                if (data == null) continue;

                CharacterCardUI card = Instantiate(cardPrefab, collectionContainer);
                card.Setup(data, owned, OnCardClicked);
                card.SetInTeam(PersistentManager.Instance.Characters.IsInTeam(owned.characterId));
                _spawnedCards.Add(card);
            }
        }

        /// <summary>
        /// Met à jour l'état "dans l'équipe" de toutes les cartes.
        /// </summary>
        private void UpdateCardsTeamState()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null) return;

            foreach (var card in _spawnedCards)
            {
                if (card != null)
                {
                    card.SetInTeam(PersistentManager.Instance.Characters.IsInTeam(card.CharacterId));
                }
            }
        }

        /// <summary>
        /// Appelé quand on clique sur une carte de la collection. Ouvre le popup détaillé.
        /// </summary>
        private void OnCardClicked(CharacterData data, OwnedCharacter owned)
        {
            if (detailPopup != null)
            {
                detailPopup.Open(data, owned);
            }
            else
            {
                Debug.LogWarning("[TeamPageUI] detailPopup est null !");
            }
        }
    }
}
