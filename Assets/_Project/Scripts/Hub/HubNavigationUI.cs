using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Gère la barre de navigation en bas du Hub avec les 4 onglets (Accueil, Équipe, Invocation, Musique).
    /// </summary>
    public class HubNavigationUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private HubManager hubManager;

        [Header("Boutons onglets")]
        [SerializeField] private Button buttonAccueil;
        [SerializeField] private Button buttonEquipe;
        [SerializeField] private Button buttonInvocation;
        [SerializeField] private Button buttonMusique;

        [Header("Indicateurs visuels (actif = visible, inactif = caché)")]
        [SerializeField] private GameObject indicatorAccueil;
        [SerializeField] private GameObject indicatorEquipe;
        [SerializeField] private GameObject indicatorInvocation;
        [SerializeField] private GameObject indicatorMusique;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (hubManager == null) return;

            // Abonnement aux clics des onglets
            buttonAccueil?.onClick.AddListener(() => hubManager.ShowPage(0));
            buttonEquipe?.onClick.AddListener(() => hubManager.ShowPage(1));
            buttonInvocation?.onClick.AddListener(() => hubManager.ShowPage(2));
            buttonMusique?.onClick.AddListener(() => hubManager.ShowPage(3));

            // S'abonne au changement de page pour mettre à jour les indicateurs
            hubManager.OnPageChanged += SetActiveTab;

            // Sélectionne Accueil par défaut (indicateurs)
            SetActiveTab(0);
        }

        private void OnDestroy()
        {
            if (hubManager != null)
                hubManager.OnPageChanged -= SetActiveTab;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Met à jour les indicateurs visuels selon l'onglet actif.
        /// </summary>
        private void SetActiveTab(int index)
        {
            if (indicatorAccueil != null) indicatorAccueil.SetActive(index == 0);
            if (indicatorEquipe != null) indicatorEquipe.SetActive(index == 1);
            if (indicatorInvocation != null) indicatorInvocation.SetActive(index == 2);
            if (indicatorMusique != null) indicatorMusique.SetActive(index == 3);
        }
    }
}
