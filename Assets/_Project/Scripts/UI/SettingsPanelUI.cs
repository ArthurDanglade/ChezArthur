using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Audio;
using ChezArthur.Gameplay;
using ChezArthur.Localization;

namespace ChezArthur.UI
{
    /// <summary>
    /// Gère les paramètres (volume) et les boutons restart/quit.
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

        [Header("Références")]
        [SerializeField] private PauseMenuUI pauseMenuUI;

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
        }

        private void OnDestroy()
        {
            Loc.OnLanguageChanged -= RefreshLanguageButtons;
            if (frButton != null)
                frButton.onClick.RemoveListener(OnFrenchClicked);
            if (enButton != null)
                enButton.onClick.RemoveListener(OnEnglishClicked);
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
