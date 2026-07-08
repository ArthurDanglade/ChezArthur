using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;
using ChezArthur.Audio;

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

        [Header("Boutons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

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

            restartButton?.onClick.AddListener(OnRestartClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
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
            // Ferme le menu et retourne au Hub
            Time.timeScale = 1f;
            RunManager.Instance?.BankRunTals();
            SceneLoader.LoadHub();
        }
    }
}
