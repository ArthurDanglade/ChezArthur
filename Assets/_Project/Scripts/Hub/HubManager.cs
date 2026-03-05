using System;
using UnityEngine;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Contrôleur principal du Hub. Gère l'affichage des 4 pages (Accueil, Équipe, Invocation, Musique).
    /// </summary>
    public class HubManager : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Pages")]
        [Tooltip("Index 0 = Accueil, 1 = Équipe, 2 = Invocation, 3 = Musique")]
        [SerializeField] private GameObject[] pages;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _currentPageIndex;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Index de la page actuellement affichée (0 = Accueil, 1 = Équipe, 2 = Invocation, 3 = Musique). </summary>
        public int CurrentPageIndex => _currentPageIndex;

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
            // Affiche la page Accueil par défaut
            ShowPage(0);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche la page correspondant à l'index et cache les autres.
        /// </summary>
        /// <param name="index">0 = Accueil, 1 = Équipe, 2 = Invocation, 3 = Musique.</param>
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
    }
}
