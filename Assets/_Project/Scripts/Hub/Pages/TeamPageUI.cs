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

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            Debug.Log("[TeamPageUI] OnEnable appelé");
            // Attendre une frame avant le refresh pour que l'UI soit prête
            StartCoroutine(DelayedRefresh());

            // S'abonner aux changements
            if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                PersistentManager.Instance.Characters.OnTeamChanged += RefreshTeamSlots;
                PersistentManager.Instance.Characters.OnCharacterAdded += RefreshDisplay;
            }
        }

        private IEnumerator DelayedRefresh()
        {
            Debug.Log("[TeamPageUI] DelayedRefresh - avant yield");
            yield return null; // Attend une frame
            Debug.Log("[TeamPageUI] DelayedRefresh - après yield, avant RefreshDisplay");
            RefreshDisplay();
            Debug.Log("[TeamPageUI] DelayedRefresh - après RefreshDisplay");
        }

        private void OnDisable()
        {
            // Se désabonner
            if (PersistentManager.Instance != null && PersistentManager.Instance.Characters != null)
            {
                PersistentManager.Instance.Characters.OnTeamChanged -= RefreshTeamSlots;
                PersistentManager.Instance.Characters.OnCharacterAdded -= RefreshDisplay;
            }
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

            var teamIds = PersistentManager.Instance.Characters.GetSelectedTeamIds();

            for (int i = 0; i < teamSlots.Length; i++)
            {
                if (teamSlots[i] == null) continue;

                if (i < teamIds.Count)
                {
                    var (data, owned) = PersistentManager.Instance.Characters.GetCharacterWithData(teamIds[i]);
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
            Debug.Log("[TeamPageUI] RefreshCollection - début");
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

            Debug.Log($"[TeamPageUI] RefreshCollection - {_spawnedCards.Count} cartes créées");
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
            Debug.Log($"[TeamPageUI] OnCardClicked appelé pour {data?.CharacterName ?? "null"}");
            if (detailPopup != null)
            {
                Debug.Log("[TeamPageUI] Ouverture du popup");
                detailPopup.Open(data, owned);
            }
            else
            {
                Debug.LogWarning("[TeamPageUI] detailPopup est null !");
            }
        }
    }
}
