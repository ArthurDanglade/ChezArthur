using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Gère les onglets Collection et Team Setup de l'écran téléphone.
    /// </summary>
    public class PhoneTabController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Boutons d'onglets")]
        [SerializeField] private Button tabBtnCollection;
        [SerializeField] private Button tabBtnTeamSetup;

        [Header("Panels")]
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private GameObject teamSetupPanel;

        [Header("Sprites des onglets")]
        [SerializeField] private Sprite tabCollectionActive;
        [SerializeField] private Sprite tabCollectionInactive;
        [SerializeField] private Sprite tabTeamActive;
        [SerializeField] private Sprite tabTeamInactive;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Image _collectionTabImage;
        private Image _teamSetupTabImage;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public int CurrentTab { get; private set; } = 0;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (tabBtnCollection != null)
            {
                tabBtnCollection.onClick.AddListener(OnCollectionTabClicked);
                _collectionTabImage = tabBtnCollection.GetComponent<Image>();
            }

            if (tabBtnTeamSetup != null)
            {
                tabBtnTeamSetup.onClick.AddListener(OnTeamSetupTabClicked);
                _teamSetupTabImage = tabBtnTeamSetup.GetComponent<Image>();
            }
        }

        private void OnEnable()
        {
            ShowCollectionTab();
        }

        private void OnDestroy()
        {
            if (tabBtnCollection != null)
                tabBtnCollection.onClick.RemoveListener(OnCollectionTabClicked);

            if (tabBtnTeamSetup != null)
                tabBtnTeamSetup.onClick.RemoveListener(OnTeamSetupTabClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche l'onglet Collection et masque Team Setup.
        /// </summary>
        public void ShowCollectionTab()
        {
            CurrentTab = 0;
            SetPanelsState(true, false);
            UpdateTabVisuals();
        }

        /// <summary>
        /// Affiche l'onglet Team Setup et masque Collection.
        /// </summary>
        public void ShowTeamSetupTab()
        {
            CurrentTab = 1;
            SetPanelsState(false, true);
            UpdateTabVisuals();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void OnCollectionTabClicked()
        {
            ShowCollectionTab();
        }

        private void OnTeamSetupTabClicked()
        {
            ShowTeamSetupTab();
        }

        private void SetPanelsState(bool isCollectionActive, bool isTeamSetupActive)
        {
            if (collectionPanel != null)
                collectionPanel.SetActive(isCollectionActive);

            if (teamSetupPanel != null)
                teamSetupPanel.SetActive(isTeamSetupActive);
        }

        private void UpdateTabVisuals()
        {
            if (_collectionTabImage != null)
                _collectionTabImage.sprite = CurrentTab == 0 ? tabCollectionActive : tabCollectionInactive;

            if (_teamSetupTabImage != null)
                _teamSetupTabImage.sprite = CurrentTab == 1 ? tabTeamActive : tabTeamInactive;
        }
    }
}

