using System;
using ChezArthur.Core;
using ChezArthur.Hub.Pages.Missions;
using ChezArthur.Meta;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Contrôleur principal du Hub. Gère l'affichage des 4 pages (Accueil, Équipe, Invocation, Missions).
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pages")]
        [Tooltip("Index 0 = Accueil, 1 = Équipe, 2 = Invocation, 3 = Missions")]
        [SerializeField] private GameObject[] pages;

        [Header("Saison (MT2-G4)")]
        [SerializeField] private SeasonRecapUI seasonRecapUI;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _currentPageIndex;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Index de la page actuellement affichée (0 = Accueil, 1 = Équipe, 2 = Invocation, 3 = Missions). </summary>
        public int CurrentPageIndex => _currentPageIndex;

        /// <summary> Page Accueil (index 0) — lecture seule (résolution gacha / outils). </summary>
        public GameObject AccueilPage =>
            pages != null && pages.Length > 0 ? pages[0] : null;

        /// <summary>
        /// Toutes les pages Hub (Accueil, Équipe, Invocation, Missions).
        /// Préférer ceci à canvas.Find("PageXxx") — les pages vivent sous PageContainer.
        /// </summary>
        public GameObject[] AllPages => pages;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Déclenché quand la page affichée change. Paramètre : index de la nouvelle page. </summary>
        public event Action<int> OnPageChanged;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            // Badge missions vivant dès l'arrivée Hub (page Missions peut être inactive).
            MissionsNavBadgeDriver.EnsureOn(this);
            // Bandeau Shop/Lofi/News : Accueil uniquement.
            TopUtilityPageVisibility.EnsureOn(this);

            // Rattrapage hub G1 : aligner saison + récap gate avant Accueil.
            SeasonProgressManager.EnsureSeasonCurrent();
            TryOpenPendingRecapGate();

            // Affiche la page Accueil par défaut
            ShowPage(0);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche la page correspondant à l'index et cache les autres.
        /// </summary>
        /// <param name="index">0 = Accueil, 1 = Équipe, 2 = Invocation, 3 = Missions.</param>
        public void ShowPage(int index)
        {
            if (pages == null || index < 0 || index >= pages.Length) return;

            _currentPageIndex = index;

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                    pages[i].SetActive(i == index);
            }

            OnPageChanged?.Invoke(index);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void TryOpenPendingRecapGate()
        {
            if (seasonRecapUI == null)
                return;

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
                return;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            if (recap != null && recap.pending && !recap.rewardsCredited)
                seasonRecapUI.OpenAsGate();
        }
    }
}
