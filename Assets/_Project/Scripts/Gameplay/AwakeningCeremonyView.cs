using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Conteneur de références UI pour l'overlay de cérémonie d'éveil.
    /// </summary>
    public class AwakeningCeremonyView : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CharacterArtworkView primeView;
        [SerializeField] private CharacterArtworkView dechuView;
        [SerializeField] private RawImage primeRawImage;
        [SerializeField] private RawImage dechuRawImage;
        [SerializeField] private RectTransform portraitContainer;
        [SerializeField] private Image ambientGlow;
        [SerializeField] private RectTransform raysRoot;
        [SerializeField] private Image raysImage;
        [SerializeField] private Image rimBloom;
        [SerializeField] private Image glowFront;
        [SerializeField] private RectTransform moteRoot;
        [SerializeField] private List<Image> moteImages = new List<Image>(14);
        [SerializeField] private Image flashOverlay;
        [SerializeField] private GameObject bannerRoot;
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private Image bannerFlash;
        [SerializeField] private TextMeshProUGUI hintText;
        [SerializeField] private Button tapButton;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public CanvasGroup CanvasGroup => canvasGroup;
        public CharacterArtworkView PrimeView => primeView;
        public CharacterArtworkView DechuView => dechuView;
        public RawImage PrimeRawImage => primeRawImage;
        public RawImage DechuRawImage => dechuRawImage;
        public RectTransform PortraitContainer => portraitContainer;
        public Image AmbientGlow => ambientGlow;
        public RectTransform RaysRoot => raysRoot;
        public Image RaysImage => raysImage;
        public Image RimBloom => rimBloom;
        public Image GlowFront => glowFront;
        public RectTransform MoteRoot => moteRoot;
        public IReadOnlyList<Image> MoteImages => moteImages;
        public Image FlashOverlay => flashOverlay;
        public GameObject BannerRoot => bannerRoot;
        public TextMeshProUGUI BannerText => bannerText;
        public Image BannerFlash => bannerFlash;
        public TextMeshProUGUI HintText => hintText;
        public Button TapButton => tapButton;
    }
}
