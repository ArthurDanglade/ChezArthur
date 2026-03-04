using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Gère le menu pause avec onglets (Équipe, Bonus, Paramètres).
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private GameObject teamPanel;
        [SerializeField] private GameObject bonusPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Boutons onglets")]
        [SerializeField] private Button teamTabButton;
        [SerializeField] private Button bonusTabButton;
        [SerializeField] private Button settingsTabButton;

        [Header("Boutons actions")]
        [SerializeField] private Button openMenuButton; // Bouton dans le HUD pour ouvrir
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resumeButton;

        [Header("Couleurs onglets")]
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = Color.gray;

        private bool _isPaused;

        private void Start()
        {
            // Cache le menu au démarrage
            if (pauseMenuRoot != null)
                pauseMenuRoot.SetActive(false);

            // Connecte les boutons
            openMenuButton?.onClick.AddListener(OpenMenu);
            closeButton?.onClick.AddListener(CloseMenu);
            resumeButton?.onClick.AddListener(CloseMenu);

            teamTabButton?.onClick.AddListener(() => ShowTab(0));
            bonusTabButton?.onClick.AddListener(() => ShowTab(1));
            settingsTabButton?.onClick.AddListener(() => ShowTab(2));
        }

        /// <summary>
        /// Ouvre le menu pause et met le jeu en pause.
        /// </summary>
        public void OpenMenu()
        {
            if (pauseMenuRoot == null) return;

            _isPaused = true;
            pauseMenuRoot.SetActive(true);
            Time.timeScale = 0f;

            // Affiche l'onglet Équipe par défaut
            ShowTab(0);

            // Rafraîchit les données
            RefreshAllPanels();
        }

        /// <summary>
        /// Ferme le menu et reprend le jeu.
        /// </summary>
        public void CloseMenu()
        {
            if (pauseMenuRoot == null) return;

            _isPaused = false;
            pauseMenuRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        private void ShowTab(int tabIndex)
        {
            // Cache tous les panels
            if (teamPanel != null) teamPanel.SetActive(false);
            if (bonusPanel != null) bonusPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Reset couleurs des onglets
            SetTabColor(teamTabButton, inactiveTabColor);
            SetTabColor(bonusTabButton, inactiveTabColor);
            SetTabColor(settingsTabButton, inactiveTabColor);

            // Affiche le panel sélectionné
            switch (tabIndex)
            {
                case 0:
                    if (teamPanel != null) teamPanel.SetActive(true);
                    SetTabColor(teamTabButton, activeTabColor);
                    break;
                case 1:
                    if (bonusPanel != null) bonusPanel.SetActive(true);
                    SetTabColor(bonusTabButton, activeTabColor);
                    break;
                case 2:
                    if (settingsPanel != null) settingsPanel.SetActive(true);
                    SetTabColor(settingsTabButton, activeTabColor);
                    break;
            }
        }

        private void SetTabColor(Button button, Color color)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = color;
            button.colors = colors;
        }

        private void RefreshAllPanels()
        {
            // Rafraîchit chaque panel
            teamPanel?.GetComponent<TeamPanelUI>()?.Refresh();
            bonusPanel?.GetComponent<BonusPanelUI>()?.Refresh();
        }

        private void Update()
        {
            // Touche Échap pour toggle le menu
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isPaused)
                    CloseMenu();
                else
                    OpenMenu();
            }
        }
    }
}
