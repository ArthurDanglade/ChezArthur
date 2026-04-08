using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bandeau animé affiché lors d'un switch de spécialisation.
    /// S'affiche brièvement puis disparaît.
    /// </summary>
    public class SpecSwitchBannerUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private GameObject bannerRoot;
        [SerializeField] private TextMeshProUGUI switchText;
        [SerializeField] private Image bannerBackground;

        [Header("Timing")]
        [SerializeField] private float displayDuration = 1.2f;
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Couleurs par rôle")]
        [SerializeField] private Color colorATK = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color colorDEF = new Color(0.2f, 0.8f, 0.3f);
        [SerializeField] private Color colorSUP = new Color(0.2f, 0.5f, 0.9f);

        // ═══════════════════════════════════════════
        // SINGLETON
        // ═══════════════════════════════════════════
        public static SpecSwitchBannerUI Instance { get; private set; }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Coroutine _currentCoroutine;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (bannerRoot != null) bannerRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Affiche le bandeau avec le nom de la spé et la couleur du rôle.
        /// </summary>
        public void Show(string specName, CharacterRole role)
        {
            if (bannerRoot == null) return;

            if (_currentCoroutine != null)
                StopCoroutine(_currentCoroutine);

            // Texte
            if (switchText != null)
                switchText.text = $"SWITCH → {specName.ToUpper()}";

            // Couleur selon le rôle
            Color roleColor = role switch
            {
                CharacterRole.Attacker => colorATK,
                CharacterRole.Defender => colorDEF,
                CharacterRole.Support => colorSUP,
                _ => Color.white
            };

            if (bannerBackground != null)
                bannerBackground.color = roleColor;

            if (switchText != null)
                switchText.color = Color.white;

            bannerRoot.SetActive(true);
            _currentCoroutine = StartCoroutine(AutoHide(roleColor));
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator AutoHide(Color baseColor)
        {
            // Affichage plein
            float elapsed = 0f;
            while (elapsed < displayDuration - fadeDuration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                float alpha = 1f - t;
                if (bannerBackground != null)
                    bannerBackground.color = new Color(
                        baseColor.r, baseColor.g, baseColor.b, alpha);
                if (switchText != null)
                    switchText.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            bannerRoot.SetActive(false);
        }
    }
}
