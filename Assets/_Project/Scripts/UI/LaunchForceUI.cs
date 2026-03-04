using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChezArthur.UI
{
    /// <summary>
    /// Affiche une jauge de force de lancement pendant le drag.
    /// </summary>
    public class LaunchForceUI : MonoBehaviour
    {
        [Header("Références UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Slider forceSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI percentText;

        [Header("Positionnement")]
        [SerializeField] private Vector2 offsetFromCharacter = new Vector2(1.5f, 1f);
        [SerializeField] private Camera cam;

        [Header("Couleurs")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color maxColor = Color.yellow;
        [SerializeField] private Color bonusColor = Color.cyan;

        private Transform _targetCharacter;

        /// <summary>
        /// Définit le personnage à suivre.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _targetCharacter = target;
        }

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        /// <summary>
        /// Affiche la jauge.
        /// </summary>
        public void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
        }

        /// <summary>
        /// Cache la jauge.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_targetCharacter == null || panelRoot == null || !panelRoot.activeSelf) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            // Convertit la position du personnage en position écran
            Vector3 worldPos = _targetCharacter.position + new Vector3(offsetFromCharacter.x, offsetFromCharacter.y, 0f);
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Positionne le panel
            panelRoot.transform.position = screenPos;
        }

        /// <summary>
        /// Met à jour la jauge avec le pourcentage de force (0-1 = 0-100%, >1 = bonus).
        /// </summary>
        public void UpdateForce(float normalizedForce, float maxMultiplier)
        {
            // Calcul du pourcentage affiché
            float displayPercent = normalizedForce * 100f;

            if (percentText != null)
                percentText.text = $"{Mathf.RoundToInt(displayPercent)}%";

            // Slider va de 0 à maxMultiplier (ex: 1.2 si +20% bonus)
            if (forceSlider != null)
            {
                forceSlider.maxValue = maxMultiplier;
                forceSlider.value = normalizedForce;
            }

            // Couleur selon le niveau
            if (fillImage != null)
            {
                if (normalizedForce > 1f)
                    fillImage.color = bonusColor; // Au-delà de 100%
                else if (normalizedForce >= 0.95f)
                    fillImage.color = maxColor; // Proche du max
                else
                    fillImage.color = normalColor; // Normal
            }
        }
    }
}
