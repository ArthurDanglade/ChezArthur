using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Roguelike;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche des bandeaux d'annonce pour les salles spéciales et les boss.
    /// </summary>
    public class StageAnnouncerUI : MonoBehaviour
    {
        [Header("Panel Salle Spéciale")]
        [SerializeField] private GameObject specialRoomPanel;
        [SerializeField] private TextMeshProUGUI specialRoomTitleText;
        [SerializeField] private TextMeshProUGUI specialRoomEffectText;

        [Header("Panel Boss")]
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private TextMeshProUGUI bossTitleText;

        [Header("Configuration")]
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float bossPulseDuration = 0.3f;

        private Coroutine _currentCoroutine;

        private void Start()
        {
            // Cache les panels au démarrage
            if (specialRoomPanel != null)
                specialRoomPanel.SetActive(false);
            if (bossPanel != null)
                bossPanel.SetActive(false);

            // S'abonne aux events
            if (SpecialRoomManager.Instance != null)
                SpecialRoomManager.Instance.OnSpecialRoomChanged += OnSpecialRoomChanged;
        }

        private void OnDestroy()
        {
            if (SpecialRoomManager.Instance != null)
                SpecialRoomManager.Instance.OnSpecialRoomChanged -= OnSpecialRoomChanged;
        }

        /// <summary>
        /// Appelé quand une salle spéciale est activée.
        /// </summary>
        private void OnSpecialRoomChanged(SpecialRoomType roomType)
        {
            if (roomType == SpecialRoomType.None) return;

            ShowSpecialRoomAnnounce(roomType);
        }

        /// <summary>
        /// Affiche l'annonce de salle spéciale.
        /// </summary>
        public void ShowSpecialRoomAnnounce(SpecialRoomType roomType)
        {
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);

            string title = GetSpecialRoomTitle(roomType);
            string effect = GetSpecialRoomEffect(roomType);

            if (specialRoomTitleText != null)
                specialRoomTitleText.text = title;
            if (specialRoomEffectText != null)
                specialRoomEffectText.text = effect;

            _currentCoroutine = StartCoroutine(ShowPanelCoroutine(specialRoomPanel));
        }

        /// <summary>
        /// Affiche l'annonce de boss/milestone.
        /// </summary>
        public void ShowBossAnnounce(string title = "BOSS FIGHT")
        {
            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);

            if (bossTitleText != null)
                bossTitleText.text = title;

            _currentCoroutine = StartCoroutine(ShowBossPanelCoroutine());
        }

        private IEnumerator ShowPanelCoroutine(GameObject panel)
        {
            if (panel == null) yield break;

            panel.SetActive(true);
            yield return new WaitForSeconds(displayDuration);
            panel.SetActive(false);

            _currentCoroutine = null;
        }

        private IEnumerator ShowBossPanelCoroutine()
        {
            if (bossPanel == null) yield break;

            bossPanel.SetActive(true);

            // Effet de pulsation (clignotement)
            Image panelImage = bossPanel.GetComponent<Image>();
            Color originalColor = panelImage != null ? panelImage.color : Color.white;

            float elapsed = 0f;
            while (elapsed < displayDuration)
            {
                if (panelImage != null)
                {
                    float alpha = Mathf.PingPong(elapsed / bossPulseDuration, 1f) * 0.3f + 0.7f;
                    panelImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (panelImage != null)
                panelImage.color = originalColor;

            bossPanel.SetActive(false);
            _currentCoroutine = null;
        }

        private string GetSpecialRoomTitle(SpecialRoomType roomType)
        {
            switch (roomType)
            {
                case SpecialRoomType.HappyHour: return "Happy Hour";
                case SpecialRoomType.Horde: return "Horde";
                case SpecialRoomType.ClientVIP: return "Client VIP";
                default: return "Salle Spéciale";
            }
        }

        private string GetSpecialRoomEffect(SpecialRoomType roomType)
        {
            switch (roomType)
            {
                case SpecialRoomType.HappyHour: return "Soins x2";
                case SpecialRoomType.Horde: return "+4 ennemis";
                case SpecialRoomType.ClientVIP: return "1 ennemi fort, Tals x2";
                default: return "";
            }
        }
    }
}
