using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Core;
using ChezArthur.Audio;
using ChezArthur.Backend;
using ChezArthur.Gameplay;
using ChezArthur.Localization;

namespace ChezArthur.UI
{
    /// <summary>
    /// Paramètres (volume, langue, liaison compte Google).
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider talsPickupSlider;

        [Header("Boutons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Langue")]
        [SerializeField] private Button frButton;
        [SerializeField] private Button enButton;

        [Header("Compte")]
        [SerializeField] private TMP_Text accountStatusText;
        [SerializeField] private Button linkButton;

        [Header("Références")]
        [SerializeField] private PauseMenuUI pauseMenuUI;

        private bool _linkArmed;
        private bool _switchArmed;
        private TMP_Text _linkButtonLabel;

        private void Start()
        {
            // Initialise les sliders avec les valeurs sauvegardées
            if (musicSlider != null)
            {
                float initial = AudioManager.Instance != null
                    ? AudioManager.Instance.MusicVolume
                    : PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.5f);
                musicSlider.SetValueWithoutNotify(initial);
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxSlider != null)
            {
                float initial = SfxManager.Instance != null
                    ? SfxManager.Instance.CurrentVolume
                    : PlayerPrefs.GetFloat("AudioManager_SfxVolume", 1f);
                sfxSlider.SetValueWithoutNotify(initial);
                sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (talsPickupSlider != null)
            {
                float initial = TalsDropSystem.Instance != null
                    ? TalsDropSystem.Instance.PickupVolume
                    : TalsDropSystem.LoadSavedPickupVolume();
                talsPickupSlider.SetValueWithoutNotify(initial);
                talsPickupSlider.onValueChanged.AddListener(OnTalsPickupVolumeChanged);
            }

            restartButton?.onClick.AddListener(OnRestartClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);

            frButton?.onClick.AddListener(OnFrenchClicked);
            enButton?.onClick.AddListener(OnEnglishClicked);
            Loc.OnLanguageChanged += RefreshLanguageButtons;
            RefreshLanguageButtons();

            if (linkButton != null)
            {
                _linkButtonLabel = linkButton.GetComponentInChildren<TMP_Text>(true);
                linkButton.onClick.AddListener(OnLinkClicked);
            }

            BackendService.OnAccountStateChanged += RefreshAccount;
            Loc.OnLanguageChanged += RefreshAccount;
            RefreshAccount();
        }

        private void OnDestroy()
        {
            Loc.OnLanguageChanged -= RefreshLanguageButtons;
            Loc.OnLanguageChanged -= RefreshAccount;
            BackendService.OnAccountStateChanged -= RefreshAccount;
            if (frButton != null)
                frButton.onClick.RemoveListener(OnFrenchClicked);
            if (enButton != null)
                enButton.onClick.RemoveListener(OnEnglishClicked);
            if (linkButton != null)
                linkButton.onClick.RemoveListener(OnLinkClicked);
        }

        private void OnFrenchClicked()
        {
            Loc.SetLanguage(GameLanguage.French);
        }

        private void OnEnglishClicked()
        {
            Loc.SetLanguage(GameLanguage.English);
        }

        /// <summary>
        /// Met en évidence le bouton de la langue active (teint, pas alpha —
        /// l'alpha faisait croire que le bouton était désactivé / non cliquable).
        /// </summary>
        private void RefreshLanguageButtons()
        {
            bool isFr = Loc.CurrentLanguage == GameLanguage.French;
            SetButtonEmphasis(frButton, isFr);
            SetButtonEmphasis(enButton, !isFr);
        }

        private static void SetButtonEmphasis(Button button, bool active)
        {
            if (button == null)
                return;

            Image image = button.image;
            if (image == null)
                return;

            Color c = image.color;
            c.a = 1f;
            float v = active ? 1f : 0.55f;
            c.r = v;
            c.g = v;
            c.b = v;
            image.color = c;
        }

        /// <summary>
        /// Rafraîchit le statut compte / bouton Lier (Editor = inerte).
        /// </summary>
        public void RefreshAccount()
        {
            if (accountStatusText == null && linkButton == null)
                return;

            bool linked = BackendService.IsGoogleLinked;
            bool signedIn = BackendService.IsSignedIn;
            bool pendingSwitch = BackendService.PendingSwitchConfirm;

#if UNITY_ANDROID && !UNITY_EDITOR
            bool platformOk = true;
#else
            bool platformOk = false;
#endif

            if (accountStatusText != null)
            {
                if (pendingSwitch)
                {
                    accountStatusText.text = Loc.Tr(
                        "ui.compte.bascule_confirm",
                        "Ce compte Google possède déjà une sauvegarde — s'y connecter ? Votre partie anonyme actuelle restera sur cet appareil");
                }
                else if (linked)
                {
                    string name = BackendService.GoogleDisplayName;
                    if (string.IsNullOrEmpty(name))
                        name = Loc.Tr("ui.compte.lie_anon", "compte lié");
                    accountStatusText.text = Loc.Format(
                        "ui.compte.lie",
                        "Lié à Google ({0})",
                        name);
                }
                else
                {
                    accountStatusText.text = Loc.Tr(
                        "ui.compte.non_lie",
                        "Compte non lié — progression sur cet appareil uniquement");
                }
            }

            if (linkButton == null)
                return;

            if (linked && !pendingSwitch)
            {
                linkButton.gameObject.SetActive(false);
                _linkArmed = false;
                _switchArmed = false;
                return;
            }

            linkButton.gameObject.SetActive(true);

            if (!platformOk)
            {
                linkButton.interactable = false;
                SetLinkLabel(Loc.Tr("ui.compte.lier_editor", "Lier à Google (appareil uniquement)"));
                return;
            }

            if (!signedIn)
            {
                linkButton.interactable = false;
                SetLinkLabel(Loc.Tr("ui.compte.lier_offline", "Lier à Google (hors ligne)"));
                return;
            }

            linkButton.interactable = true;
            if (pendingSwitch)
            {
                SetLinkLabel(Loc.Tr("ui.compte.basculer", "Se connecter au compte Google"));
            }
            else
            {
                SetLinkLabel(Loc.Tr("ui.compte.lier", "Lier à Google"));
            }
        }

        private void SetLinkLabel(string text)
        {
            if (_linkButtonLabel != null)
                _linkButtonLabel.text = text;
        }

        private async void OnLinkClicked()
        {
            if (BackendService.PendingSwitchConfirm || _switchArmed)
            {
                if (!_switchArmed)
                {
                    _switchArmed = true;
                    if (accountStatusText != null)
                    {
                        accountStatusText.text = Loc.Tr(
                            "ui.compte.bascule_hint",
                            "2e appui pour confirmer la bascule.");
                    }

                    return;
                }

                _switchArmed = false;
                linkButton.interactable = false;
                await BackendService.ConfirmSwitchToLinkedGoogleAsync();
                RefreshAccount();
                return;
            }

            if (!_linkArmed)
            {
                _linkArmed = true;
                if (accountStatusText != null)
                {
                    accountStatusText.text = Loc.Tr(
                        "ui.compte.lier_hint",
                        "2e appui pour lier à Google.");
                }

                return;
            }

            _linkArmed = false;
            if (linkButton != null)
                linkButton.interactable = false;

            GoogleLinkResult result = await BackendService.LinkWithGoogleAsync();
            if (result == GoogleLinkResult.AlreadyLinkedNeedsConfirm)
                _switchArmed = false;

            RefreshAccount();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.SetMusicVolume(value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (SfxManager.Instance != null)
                SfxManager.Instance.SetVolume(value);
        }

        private void OnTalsPickupVolumeChanged(float value)
        {
            if (TalsDropSystem.Instance != null)
                TalsDropSystem.Instance.SetPickupVolume(value);
            else
            {
                PlayerPrefs.SetFloat(TalsDropSystem.PrefPickupVolume, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        private void OnRestartClicked()
        {
            // Ferme le menu et relance la run
            Time.timeScale = 1f;

            if (pauseMenuUI != null)
                pauseMenuUI.CloseMenu();

            if (RunManager.Instance != null)
                RunManager.Instance.StartRun();
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;

            AwakeningCeremonyController ceremony = AwakeningCeremonyController.Instance;
            if (ceremony != null && ceremony.IsPlaying)
                return;

            if (ceremony != null && ceremony.HasPendingCeremonies)
            {
                ceremony.PlayCeremonies(() =>
                {
                    RunManager.Instance?.BankRunTals();
                    SceneLoader.LoadHub();
                });
                return;
            }

            RunManager.Instance?.BankRunTals();
            SceneLoader.LoadHub();
        }
    }
}
