using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Gameplay;

namespace ChezArthur.Hub
{
    /// <summary>
    /// Bouton debug Hub : rejoue la cérémonie d'éveil sans toucher la save.
    /// Se cache pendant l'anim. Auto-détruit hors Editor / Development Build.
    /// </summary>
    public class AwakeningCeremonyDebugButton : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Cible preview")]
        [SerializeField] private CharacterData previewCharacter;

        [Header("Refs cérémonie (si pas déjà en scène)")]
        [SerializeField] private AwakeningCeremonyView overlayPrefab;
        [SerializeField] private Material dissolveMaterial;

        [Header("Audio (obligatoire pour Preview Hub)")]
        [SerializeField] private AudioClip riserClip;
        [SerializeField] private AudioClip flashClip;
        [SerializeField] private AudioClip fanfareClip;
        [SerializeField] private AudioClip ambienceLoop;

        [Header("UI")]
        [SerializeField] private Button previewButton;
        [SerializeField] private bool buildButtonIfMissing = true;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(gameObject);
            return;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Start()
        {
            EnsureController();
            EnsureButton();

            if (previewButton == null)
            {
                Debug.LogWarning("[AwakeningCeremonyDebugButton] Aucun bouton UI.");
                return;
            }

            previewButton.onClick.RemoveListener(OnPreviewClicked);
            previewButton.onClick.AddListener(OnPreviewClicked);
        }

        private void OnDestroy()
        {
            if (previewButton != null)
                previewButton.onClick.RemoveListener(OnPreviewClicked);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureController()
        {
            AwakeningCeremonyController existing = AwakeningCeremonyController.Instance;
            if (existing != null)
            {
                // Réinjecte les clips même si le controller Hub existait sans audio.
                existing.Configure(
                    overlayPrefab,
                    dissolveMaterial,
                    riserClip,
                    flashClip,
                    fanfareClip,
                    ambienceLoop);
                return;
            }

            if (overlayPrefab == null || dissolveMaterial == null)
            {
                Debug.LogError(
                    "[AwakeningCeremonyDebugButton] Assigner overlayPrefab + dissolveMaterial, " +
                    "ou placer un AwakeningCeremonyController dans le Hub.");
                return;
            }

            GameObject go = new GameObject("AwakeningCeremonyController");
            AwakeningCeremonyController controller = go.AddComponent<AwakeningCeremonyController>();
            controller.Configure(
                overlayPrefab,
                dissolveMaterial,
                riserClip,
                flashClip,
                fanfareClip,
                ambienceLoop);
        }

        private void EnsureButton()
        {
            if (previewButton != null || !buildButtonIfMissing)
                return;

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[AwakeningCeremonyDebugButton] Aucun Canvas dans le Hub.");
                return;
            }

            GameObject root = new GameObject("BtnPreviewEveil", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(200f, 64f);

            Image image = root.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.85f);

            previewButton = root.AddComponent<Button>();
            previewButton.targetGraphic = image;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
            label.text = "Preview éveil";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 28f;
            label.color = Color.white;
        }

        private void OnPreviewClicked()
        {
            if (previewCharacter == null)
            {
                Debug.LogWarning("[AwakeningCeremonyDebugButton] previewCharacter non assigné.");
                return;
            }

            EnsureController();
            AwakeningCeremonyController controller = AwakeningCeremonyController.Instance;

            if (controller == null || controller.IsPlaying)
                return;

            if (previewButton != null)
                previewButton.gameObject.SetActive(false);

            controller.PlayPreview(previewCharacter, OnPreviewFinished);
        }

        private void OnPreviewFinished()
        {
            if (previewButton != null)
                previewButton.gameObject.SetActive(true);
        }
#endif
    }
}
