using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Core;

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
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            restartButton?.onClick.AddListener(OnRestartClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnMusicVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("MusicVolume", value);
            // TODO: Appliquer au AudioManager quand il existera
        }

        private void OnSFXVolumeChanged(float value)
        {
            PlayerPrefs.SetFloat("SFXVolume", value);
            // TODO: Appliquer au AudioManager quand il existera
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
            // Ferme le menu et retourne au menu principal
            Time.timeScale = 1f;

            // TODO: Charger la scène du menu principal quand elle existera
            // SceneManager.LoadScene("MainMenu");
            Debug.Log("[SettingsPanel] Retour au menu principal (scène non implémentée)");
        }
    }
}
