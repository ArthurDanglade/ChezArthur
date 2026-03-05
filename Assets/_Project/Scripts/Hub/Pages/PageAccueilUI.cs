using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Page d'accueil du Hub : bouton Lancer Run, Paramètres, Magasin, News.
    /// </summary>
    public class PageAccueilUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Boutons principaux")]
        [SerializeField] private Button buttonLancerRun;

        [Header("Boutons secondaires")]
        [SerializeField] private Button buttonParametres;
        [SerializeField] private Button buttonMagasin;
        [SerializeField] private Button buttonNews;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (buttonLancerRun != null)
                buttonLancerRun.onClick.AddListener(OnLancerRunClicked);

            if (buttonParametres != null)
                buttonParametres.onClick.AddListener(OnParametresClicked);

            if (buttonMagasin != null)
                buttonMagasin.onClick.AddListener(OnMagasinClicked);

            if (buttonNews != null)
                buttonNews.onClick.AddListener(OnNewsClicked);
        }

        private void OnDestroy()
        {
            if (buttonLancerRun != null)
                buttonLancerRun.onClick.RemoveListener(OnLancerRunClicked);
            if (buttonParametres != null)
                buttonParametres.onClick.RemoveListener(OnParametresClicked);
            if (buttonMagasin != null)
                buttonMagasin.onClick.RemoveListener(OnMagasinClicked);
            if (buttonNews != null)
                buttonNews.onClick.RemoveListener(OnNewsClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnLancerRunClicked()
        {
            SceneLoader.LoadGame();
        }

        private void OnParametresClicked()
        {
            Debug.Log("[PageAccueil] Paramètres (à implémenter)");
        }

        private void OnMagasinClicked()
        {
            Debug.Log("[PageAccueil] Magasin (à implémenter)");
        }

        private void OnNewsClicked()
        {
            Debug.Log("[PageAccueil] News (à implémenter)");
        }
    }
}
