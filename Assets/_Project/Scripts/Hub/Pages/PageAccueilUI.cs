using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.BossRush;
using ChezArthur.Core;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Page d'accueil : Jouer ouvre le choix Run normale / Boss Rush.
    /// </summary>
    public class PageAccueilUI : MonoBehaviour
    {
        [Header("Boutons principaux")]
        [SerializeField] private Button buttonLancerRun;

        [Header("Boutons secondaires")]
        [SerializeField] private Button buttonParametres;
        [SerializeField] private Button buttonMagasin;
        [SerializeField] private Button buttonNews;

        [Header("Sélection de mode")]
        [SerializeField] private GameObject modeSelectRoot;
        [SerializeField] private Button buttonModeNormal;
        [SerializeField] private Button buttonModeBossRush;
        [SerializeField] private Button buttonModeCancel;
        [SerializeField] private TextMeshProUGUI bossRushLockedLabel;
        [SerializeField] private TextMeshProUGUI bossRushInfoLabel;

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

            if (buttonModeNormal != null)
                buttonModeNormal.onClick.AddListener(OnModeNormalClicked);

            if (buttonModeBossRush != null)
                buttonModeBossRush.onClick.AddListener(OnModeBossRushClicked);

            if (buttonModeCancel != null)
                buttonModeCancel.onClick.AddListener(HideModeSelect);

            HideModeSelect();
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
            if (buttonModeNormal != null)
                buttonModeNormal.onClick.RemoveListener(OnModeNormalClicked);
            if (buttonModeBossRush != null)
                buttonModeBossRush.onClick.RemoveListener(OnModeBossRushClicked);
            if (buttonModeCancel != null)
                buttonModeCancel.onClick.RemoveListener(HideModeSelect);
        }

        private void OnLancerRunClicked()
        {
            if (modeSelectRoot != null)
            {
                RefreshBossRushButtonState();
                modeSelectRoot.SetActive(true);
                return;
            }

            // Fallback si overlay pas câblé.
            LaunchMode(GameRunMode.Normal);
        }

        private void OnModeNormalClicked()
        {
            LaunchMode(GameRunMode.Normal);
        }

        private void OnModeBossRushClicked()
        {
            BossRushManager mgr = BossRushManager.Instance;
            if (mgr == null || !mgr.IsUnlocked || mgr.RosterCount <= 0)
                return;

            LaunchMode(GameRunMode.BossRush);
        }

        private void LaunchMode(GameRunMode mode)
        {
            HideModeSelect();
            if (PersistentManager.Instance != null)
                PersistentManager.Instance.SetPendingRunMode(mode);
            SceneLoader.LoadGame();
        }

        private void HideModeSelect()
        {
            if (modeSelectRoot != null)
                modeSelectRoot.SetActive(false);
        }

        private void RefreshBossRushButtonState()
        {
            BossRushManager mgr = BossRushManager.Instance;
            bool unlocked = mgr != null && mgr.IsUnlocked && mgr.RosterCount > 0;

            if (buttonModeBossRush != null)
                buttonModeBossRush.interactable = unlocked;

            if (bossRushLockedLabel != null)
            {
                bossRushLockedLabel.gameObject.SetActive(!unlocked);
                bossRushLockedLabel.text = "Bats au moins un boss pour débloquer le boss rush";
            }

            if (bossRushInfoLabel != null)
            {
                int count = mgr != null ? mgr.RosterCount : 0;
                int majors = mgr != null ? mgr.MajorUnlockedCount : 0;
                bossRushInfoLabel.text = unlocked
                    ? $"Roster : {count}  |  Majeurs : {majors}/20"
                    : string.Empty;
            }
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
