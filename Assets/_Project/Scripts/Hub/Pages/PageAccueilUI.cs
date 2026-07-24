using UnityEngine;
using ChezArthur.BossRush;
using ChezArthur.Core;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Page d'accueil — Option A : run normale en un tap, Boss Rush en secondaire.
    /// </summary>
    public class PageAccueilUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Actions")]
        [SerializeField] private HubButtonUI buttonLancerRun;
        [SerializeField] private HubButtonUI buttonBossRush;
        [SerializeField] private HubButtonUI buttonMagasin;
        [SerializeField] private HubButtonUI buttonNews;

        private const string BossRushLockedSub =
            "Bats au moins un boss pour débloquer le boss rush";

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            if (buttonLancerRun != null && buttonLancerRun.Button != null)
                buttonLancerRun.Button.onClick.AddListener(OnLancerRunClicked);

            if (buttonBossRush != null && buttonBossRush.Button != null)
                buttonBossRush.Button.onClick.AddListener(OnBossRushClicked);

            // Magasin / News : jamais câblés à un handler (placeholders).

            RefreshBossRushState();
        }

        private void OnDisable()
        {
            if (buttonLancerRun != null && buttonLancerRun.Button != null)
                buttonLancerRun.Button.onClick.RemoveListener(OnLancerRunClicked);

            if (buttonBossRush != null && buttonBossRush.Button != null)
                buttonBossRush.Button.onClick.RemoveListener(OnBossRushClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnLancerRunClicked()
        {
            if (PersistentManager.Instance != null)
                PersistentManager.Instance.SetPendingRunMode(GameRunMode.Normal);
            SceneLoader.LoadGame();
        }

        private void OnBossRushClicked()
        {
            // Garde défense (même si le bouton locked est non-interactable).
            BossRushManager mgr = BossRushManager.Instance;
            if (mgr == null || !mgr.IsUnlocked || mgr.RosterCount <= 0)
                return;

            if (PersistentManager.Instance != null)
                PersistentManager.Instance.SetPendingRunMode(GameRunMode.BossRush);
            SceneLoader.LoadGame();
        }

        /// <summary>
        /// Synchronise état locked + subLabel Boss Rush (retour Hub = OnEnable).
        /// </summary>
        private void RefreshBossRushState()
        {
            if (buttonBossRush == null)
                return;

            BossRushManager mgr = BossRushManager.Instance;
            bool unlocked = mgr != null && mgr.IsUnlocked && mgr.RosterCount > 0;

            buttonBossRush.Locked = !unlocked;
            if (unlocked)
            {
                int count = mgr.RosterCount;
                buttonBossRush.SetSubLabel($"{count} boss affrontables");
            }
            else
            {
                buttonBossRush.SetSubLabel(BossRushLockedSub);
            }
        }
    }
}
