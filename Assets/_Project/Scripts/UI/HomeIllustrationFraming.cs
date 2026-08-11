using System.Collections;
using UnityEngine;
using ChezArthur.Hub;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cadrage Accueil : largeur = 100% page (wagon colle gauche/droite).
    /// Hauteur = 532 * (largeur/232), crop vertical via focusY.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HomeIllustrationFraming : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        /// <summary> Largeur native illustration (px). </summary>
        public const float NativeWidth = 232f;

        /// <summary> Hauteur native illustration (px). </summary>
        public const float NativeHeight = 532f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Point focal (fractions illustration)")]
        [Tooltip("0 = gauche, 1 = droite.")]
        [SerializeField] private float focusX = 0.5f;

        [Tooltip("Fraction depuis le HAUT. ↓ = illustration plus bas (≈ 0.28 Accueil).")]
        [SerializeField] private float focusY = 0.28f;

        [Header("Clearance nav (overlay)")]
        [Tooltip(
            "Si vrai : cover jusqu'en bas de page (nav par-dessus). " +
            "Si faux : réserve la hauteur BottomZone / NavHeight.")]
        [SerializeField] private bool coverUnderNav = true;

        [Tooltip(
            "BottomZone overlay : posY = hauteur nav. Ignoré si coverUnderNav.")]
        [SerializeField] private RectTransform bottomZone;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private RectTransform _rigRt;
        private RectTransform _parentRt;
        private float _lastParentW = float.MinValue;
        private float _lastParentH = float.MinValue;
        private float _lastBottomInset = float.MinValue;
        private bool _applyingLayout;
        private Coroutine _retryRoutine;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            Cache();
            Canvas.ForceUpdateCanvases();
            if (!ApplyFraming(force: true))
                ScheduleRetry();
        }

        private void OnDisable()
        {
            if (_retryRoutine != null)
            {
                StopCoroutine(_retryRoutine);
                _retryRoutine = null;
            }
        }

        private void LateUpdate()
        {
            // OnRectTransformDimensionsChange ne se déclenche pas toujours quand
            // seul le parent (PageAccueil / Canvas) change de taille → bandes latérales.
            ApplyFraming(force: false);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_applyingLayout)
                return;
            ApplyFraming(force: false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Interdit d'écrire sizeDelta pendant OnValidate (SendMessage).
            UnityEditor.EditorApplication.delayCall -= DeferredValidateFraming;
            UnityEditor.EditorApplication.delayCall += DeferredValidateFraming;
        }

        private void DeferredValidateFraming()
        {
            UnityEditor.EditorApplication.delayCall -= DeferredValidateFraming;
            if (this == null || !isActiveAndEnabled)
                return;
            Cache();
            Canvas.ForceUpdateCanvases();
            ApplyFraming(force: true);
        }
#endif

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary> Recalcule immédiatement (builder / tests). </summary>
        public void Refresh()
        {
            Cache();
            Canvas.ForceUpdateCanvases();
            if (!ApplyFraming(force: true))
                ScheduleRetry();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void Cache()
        {
            if (_rigRt == null)
                _rigRt = (RectTransform)transform;
            if (_rigRt != null && _rigRt.parent != null)
                _parentRt = _rigRt.parent as RectTransform;
        }

        private void ScheduleRetry()
        {
            if (!isActiveAndEnabled)
                return;
            if (_retryRoutine != null)
                StopCoroutine(_retryRoutine);
            _retryRoutine = StartCoroutine(RetryFramingEndOfFrame());
        }

        private IEnumerator RetryFramingEndOfFrame()
        {
            // 2 frames : laisse le CanvasScaler / SafeArea finaliser les rects.
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyFraming(force: true);
            yield return null;
            Canvas.ForceUpdateCanvases();
            ApplyFraming(force: true);
            _retryRoutine = null;
        }

        /// <returns> false si le parent n'a pas encore de taille valide. </returns>
        private bool ApplyFraming(bool force)
        {
            Cache();
            if (_rigRt == null || _parentRt == null)
                return false;

            float parentW = _parentRt.rect.width;
            float parentH = _parentRt.rect.height;
            if (parentW < 1f || parentH < 1f)
                return false;

            float bottomInset = ComputeBottomInset();
            // Sécurité : ne jamais réserver plus que la page.
            if (bottomInset > parentH - 1f)
                bottomInset = Mathf.Max(0f, parentH * 0.15f);

            if (!force
                && Mathf.Approximately(parentW, _lastParentW)
                && Mathf.Approximately(parentH, _lastParentH)
                && Mathf.Approximately(bottomInset, _lastBottomInset))
            {
                // Reassert stretch si la scene a encore d'anciennes ancres centre.
                if (_rigRt != null
                    && (!Mathf.Approximately(_rigRt.anchorMin.x, 0f)
                        || !Mathf.Approximately(_rigRt.anchorMax.x, 1f)
                        || !Mathf.Approximately(_rigRt.sizeDelta.x, 0f)))
                {
                    float h = _rigRt.sizeDelta.y;
                    if (h < 1f)
                        h = NativeHeight * (parentW / NativeWidth);
                    _applyingLayout = true;
                    try
                    {
                        _rigRt.anchorMin = new Vector2(0f, 0.5f);
                        _rigRt.anchorMax = new Vector2(1f, 0.5f);
                        _rigRt.pivot = new Vector2(0.5f, 0.5f);
                        _rigRt.sizeDelta = new Vector2(0f, h);
                        _rigRt.anchoredPosition = new Vector2(0f, _rigRt.anchoredPosition.y);
                    }
                    finally
                    {
                        _applyingLayout = false;
                    }

                    NotifyParallaxRelayout();
                }

                return true;
            }

            float zoneW = parentW;
            float zoneH = Mathf.Max(1f, parentH - bottomInset);

            // Largeur forcee = largeur page (ancres stretch X). Hauteur proportionnelle.
            float scale = zoneW / NativeWidth;
            float rigH = NativeHeight * scale;

            float zoneBottom = -parentH * 0.5f + bottomInset;
            float zoneTop = parentH * 0.5f;
            float zoneCenterY = (zoneBottom + zoneTop) * 0.5f;

            // Focus Y depuis le HAUT ; X fige (largeur = parent).
            float focusLocalY = (0.5f - focusY) * rigH;
            float posY = zoneCenterY - focusLocalY;

            if (rigH <= zoneH + 0.5f)
            {
                posY = zoneCenterY;
            }
            else
            {
                float minY = zoneTop - rigH * 0.5f;
                float maxY = zoneBottom + rigH * 0.5f;
                if (minY > maxY)
                {
                    float mid = (minY + maxY) * 0.5f;
                    minY = mid;
                    maxY = mid;
                }

                posY = Mathf.Clamp(posY, minY, maxY);
            }

            if (_applyingLayout)
                return true;

            _applyingLayout = true;
            try
            {
                // Stretch horizontal : largeur = parent exact, colle L/R.
                _rigRt.anchorMin = new Vector2(0f, 0.5f);
                _rigRt.anchorMax = new Vector2(1f, 0.5f);
                _rigRt.pivot = new Vector2(0.5f, 0.5f);
                _rigRt.localScale = Vector3.one;
                _rigRt.sizeDelta = new Vector2(0f, rigH);
                _rigRt.anchoredPosition = new Vector2(0f, posY);
            }
            finally
            {
                _applyingLayout = false;
            }

            _lastParentW = parentW;
            _lastParentH = parentH;
            _lastBottomInset = bottomInset;

            // Aligne les calques sur la largeur finale (= ecran).
            NotifyParallaxRelayout();
            return true;
        }

        private void NotifyParallaxRelayout()
        {
            ParallaxManager parallax = GetComponentInChildren<ParallaxManager>(true);
            if (parallax != null)
                parallax.RelayoutToCurrentRect();
        }

        /// <summary>
        /// Réserve basse : 0 si cover sous nav ; sinon hauteur nav (BottomZone).
        /// </summary>
        private float ComputeBottomInset()
        {
            if (coverUnderNav)
                return 0f;

            if (bottomZone == null)
                return UiTheme.NavHeight;

            // BottomZoneNavClearance pose posY = hauteur NavigationBar (bleed inclus).
            return Mathf.Max(0f, bottomZone.anchoredPosition.y);
        }
    }
}
