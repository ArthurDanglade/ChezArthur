using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Anneau de progression radial pendant un maintien (Gate 5.c).
    /// Zéro allocation en Update — SetProgress uniquement.
    /// </summary>
    public class HoldProgressFX : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float DefaultSize = 96f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform root;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée ou réutilise un FX enfant de <paramref name="host"/>.
        /// </summary>
        public static HoldProgressFX Ensure(
            RectTransform host,
            Sprite ringSprite,
            float size = DefaultSize)
        {
            if (host == null)
                return null;

            Transform existing = host.Find("HoldProgressFX");
            HoldProgressFX fx;
            if (existing != null)
            {
                fx = existing.GetComponent<HoldProgressFX>();
                if (fx == null)
                    fx = existing.gameObject.AddComponent<HoldProgressFX>();
            }
            else
            {
                GameObject go = new GameObject(
                    "HoldProgressFX",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(HoldProgressFX));
                go.transform.SetParent(host, false);
                fx = go.GetComponent<HoldProgressFX>();
                fx.root = (RectTransform)go.transform;
                fx.fillImage = go.GetComponent<Image>();
            }

            fx.Configure(ringSprite, size);
            fx.Hide();
            return fx;
        }

        public void ShowAt(Vector2 localCenter)
        {
            if (root == null)
                return;
            root.anchoredPosition = localCenter;
            root.gameObject.SetActive(true);
            SetProgress(0f);
        }

        public void SetProgress(float t01)
        {
            if (fillImage == null)
                return;
            fillImage.fillAmount = Mathf.Clamp01(t01);
        }

        public void Hide()
        {
            if (fillImage != null)
                fillImage.fillAmount = 0f;
            if (root != null)
                root.gameObject.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private void Configure(Sprite ringSprite, float size)
        {
            if (root == null)
                root = (RectTransform)transform;
            if (fillImage == null)
                fillImage = GetComponent<Image>();

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(size, size);
            root.localScale = Vector3.one;

            fillImage.raycastTarget = false;
            fillImage.preserveAspect = true;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
            fillImage.fillAmount = 0f;

            if (ringSprite != null)
                fillImage.sprite = ringSprite;

            Color c = UiTheme.AccentAmber;
            c.a = 0.6f;
            fillImage.color = c;
        }
    }
}
