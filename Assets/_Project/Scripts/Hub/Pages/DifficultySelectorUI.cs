using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Localization;
using ChezArthur.Meta;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Overlay de sélection de cran avant lancement de run (MT2-G2).
    /// Flux : Accueil Lancer → Open → tap cran → LoadGame (2 touches).
    /// </summary>
    public class DifficultySelectorUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Racine")]
        [SerializeField] private GameObject panelRoot;

        [Header("Textes")]
        [SerializeField] private TextMeshProUGUI rotationLabel;

        [Header("Crans")]
        [SerializeField] private HubButtonUI[] tierButtons = new HubButtonUI[5];

        [Header("Fermer")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button scrimButton;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            BindButtons(true);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (scrimButton != null)
                scrimButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            BindButtons(false);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (scrimButton != null)
                scrimButton.onClick.RemoveListener(Close);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche le sélecteur et rafraîchit rotation + états de cran.
        /// </summary>
        public void Open()
        {
            RefreshRotationLabel();
            RefreshTierButtons();
            if (panelRoot != null)
                panelRoot.SetActive(true);
            else
                gameObject.SetActive(true);
        }

        /// <summary>
        /// Ferme le sélecteur sans lancer.
        /// </summary>
        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void BindButtons(bool add)
        {
            if (tierButtons == null)
                return;

            for (int i = 0; i < tierButtons.Length; i++)
            {
                HubButtonUI hubBtn = tierButtons[i];
                if (hubBtn == null || hubBtn.Button == null)
                    continue;

                int captured = i;
                if (add)
                    hubBtn.Button.onClick.AddListener(() => OnTierClicked(captured));
                else
                    hubBtn.Button.onClick.RemoveAllListeners();
            }
        }

        private void RefreshRotationLabel()
        {
            if (rotationLabel == null)
                return;

            int u = SeasonRotationManager.GetCurrentUniverseAtSlot(0);
            string name = UniverseIds.GetDisplayName(u);
            string prefix = Loc.Tr(
                "ui.accueil.diff_rotation",
                "Pos. 1 cette semaine : {0}");
            rotationLabel.text = string.Format(prefix, name);
        }

        private void RefreshTierButtons()
        {
            DifficultyConfig config = DifficultyConfig.LoadDefault();
            PersistentManager pm = PersistentManager.Instance;
            int unlockStage = config.UnlockStage;

            if (tierButtons == null)
                return;

            int count = Mathf.Min(tierButtons.Length, config.TierCount);
            for (int i = 0; i < tierButtons.Length; i++)
            {
                HubButtonUI hubBtn = tierButtons[i];
                if (hubBtn == null)
                    continue;

                if (i >= count)
                {
                    hubBtn.gameObject.SetActive(false);
                    continue;
                }

                hubBtn.gameObject.SetActive(true);
                string label = config.GetLabel(i);
                hubBtn.SetLabel(label);

                bool unlocked = pm == null || pm.IsDifficultyUnlocked(i);
                hubBtn.Locked = !unlocked;

                if (!unlocked && i > 0)
                {
                    string prevLabel = config.GetLabel(i - 1);
                    string hintFmt = Loc.Tr(
                        "ui.accueil.diff_lock_hint",
                        "Étage {0} en {1}");
                    hubBtn.SetSubLabel(string.Format(hintFmt, unlockStage, prevLabel));
                }
                else
                {
                    hubBtn.SetSubLabel(string.Empty);
                }
            }
        }

        private void OnTierClicked(int index)
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm != null && !pm.IsDifficultyUnlocked(index))
                return;

            if (pm != null)
            {
                pm.SetPendingRunMode(GameRunMode.Normal);
                pm.SetPendingDifficulty(index);
            }

            Close();
            SceneLoader.LoadGame();
        }
    }
}
