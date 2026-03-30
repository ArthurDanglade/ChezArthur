using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Fait défiler horizontalement une Image UI en boucle infinie via deux instances côte à côte.
    /// Utilise Time.unscaledDeltaTime pour fonctionner même si le timeScale est à 0 (Hub en pause).
    /// </summary>
    public class UIParallaxScroller : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Configuration")]
        [SerializeField] private float scrollSpeed = 20f;

        [Header("Références")]
        [Tooltip("Copie de l'image (même sprite). Si null, elle est créée automatiquement au Start().")]
        [SerializeField] private RectTransform duplicateImage;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rectTransform;
        private RectTransform _duplicateRectTransform;
        private float _width;
        private bool _initialized;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            _rectTransform = transform as RectTransform;
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_rectTransform == null || _duplicateRectTransform == null) return;

            float delta = scrollSpeed * Time.unscaledDeltaTime;
            if (delta == 0f) return;

            Vector2 p1 = _rectTransform.anchoredPosition;
            Vector2 p2 = _duplicateRectTransform.anchoredPosition;

            p1.x += delta;
            p2.x += delta;

            // Scroll vers la gauche (scrollSpeed négatif) : on recycle quand une image est entièrement sortie à gauche.
            if (scrollSpeed < 0f)
            {
                if (p1.x <= -_width)
                    p1.x = p2.x + _width;

                if (p2.x <= -_width)
                    p2.x = p1.x + _width;
            }
            // Scroll vers la droite (scrollSpeed positif) : on recycle quand une image est entièrement sortie à droite.
            else
            {
                if (p1.x >= _width)
                    p1.x = p2.x - _width;

                if (p2.x >= _width)
                    p2.x = p1.x - _width;
            }

            _rectTransform.anchoredPosition = p1;
            _duplicateRectTransform.anchoredPosition = p2;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void InitializeIfNeeded()
        {
            if (_initialized) return;
            if (_rectTransform == null) return;

            // Largeur utilisée pour le repositionnement : on prend en compte le scale horizontal.
            float scaleX = Mathf.Abs(_rectTransform.localScale.x);
            _width = _rectTransform.rect.width * (scaleX <= 0f ? 1f : scaleX);
            if (_width <= 0f) return;

            if (duplicateImage == null)
            {
                duplicateImage = CreateDuplicate();
                if (duplicateImage == null) return;
            }

            _duplicateRectTransform = duplicateImage;

            // Positionne la copie juste à droite du premier pour garantir la continuité.
            Vector2 p1 = _rectTransform.anchoredPosition;
            Vector2 p2 = _duplicateRectTransform.anchoredPosition;
            p2.x = p1.x + _width;
            p2.y = p1.y;
            _duplicateRectTransform.anchoredPosition = p2;

            _initialized = true;
        }

        private RectTransform CreateDuplicate()
        {
            // On duplique l'UI GameObject pour garder exactement les mêmes composants/paramètres (sprite, material, etc.).
            GameObject duplicateGo = Instantiate(gameObject, _rectTransform.parent);
            duplicateGo.name = $"{gameObject.name}_Duplicate";

            // Empêche le duplicata d'exécuter aussi le scroller.
            UIParallaxScroller scroller = duplicateGo.GetComponent<UIParallaxScroller>();
            if (scroller != null)
                scroller.enabled = false;

            // Optionnel : s'assure que le duplicata est rendu derrière/devant selon l'ordre actuel.
            duplicateGo.transform.SetSiblingIndex(transform.GetSiblingIndex());

            RectTransform rt = duplicateGo.transform as RectTransform;
            if (rt == null) return null;

            // On force le même scale pour que la largeur reste cohérente.
            rt.localScale = _rectTransform.localScale;

            return rt;
        }
    }
}

