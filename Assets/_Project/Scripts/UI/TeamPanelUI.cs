using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay;
using ChezArthur.Hub.Pages;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche la liste des personnages avec leurs stats actuelles.
    /// </summary>
    public class TeamPanelUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject characterEntryPrefab;
        [SerializeField] private CharacterDetailPopup detailPopup;
        [SerializeField] private ValiseSectionUI valiseSection;
        [SerializeField] private ItemSectionUI itemSection;

        private List<CharacterEntryUI> _entries = new List<CharacterEntryUI>();

        /// <summary>
        /// Rafraîchit la liste des personnages de l'équipe.
        /// </summary>
        public void Refresh()
        {
            // Nettoie les anciennes entrées
            foreach (var entry in _entries)
            {
                if (entry != null)
                    Destroy(entry.gameObject);
            }
            _entries.Clear();

            if (turnManager == null || contentParent == null || characterEntryPrefab == null) return;

            // Crée une entrée pour chaque allié
            foreach (var participant in turnManager.Participants)
            {
                if (!participant.IsAlly) continue;

                CharacterBall character = participant as CharacterBall;
                if (character == null) continue;

                GameObject entryGO = Instantiate(characterEntryPrefab, contentParent);
                CharacterEntryUI entry = entryGO.GetComponent<CharacterEntryUI>();

                if (entry != null)
                {
                    entry.Setup(character, OnEntryClicked);
                    _entries.Add(entry);
                }
            }

            if (valiseSection != null)
                valiseSection.Refresh();

            if (itemSection != null)
                itemSection.Refresh();
        }

        /// <summary>
        /// Ouvre la fiche détail du personnage cliqué (stats live en combat).
        /// </summary>
        private void OnEntryClicked(CharacterBall ball)
        {
            if (detailPopup != null)
                detailPopup.OpenLive(ball);
            else
                Debug.LogWarning("[TeamPanelUI] detailPopup non assigné.");
        }
    }
}
