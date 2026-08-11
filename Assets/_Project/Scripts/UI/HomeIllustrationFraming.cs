using System.Collections;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Cadrage cover de l'illustration Accueil : scale pour remplir la page.
    /// Par défaut le cover passe sous la nav (footer overlay) pour maximiser
    /// le décor. Point focal clampé pour couvrir la zone.
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
                return true;

            float zoneW = parentW;
            float zoneH = Mathf.Max(1f, parentH - bottomInset);

            // Cover strict : remplit TOUTE la zone (aucun pillarbox / letterbox).
            float scale = Mathf.Max(zoneW / NativeWidth, zoneH / NativeHeight);
            float rigW = NativeWidth * scale;
            float rigH = NativeHeight * scale;

            // Coordonnées parent locales (pivot page 0.5 / 0.5).
            float zoneLeft = -parentW * 0.5f;
            float zoneRight = parentW * 0.5f;
            float zoneBottom = -parentH * 0.5f + bottomInset;
            float zoneTop = parentH * 0.5f;
            float zoneCenterX = (zoneLeft + zoneRight) * 0.5f;
            float zoneCenterY = (zoneBottom + zoneTop) * 0.5f;

            // Focus en local rig (pivot 0.5 / 0.5) : focusY depuis le HAUT.
            float focusLocalX = (focusX - 0.5f) * rigW;
            float focusLocalY = (0.5f - focusY) * rigH;

            float posX = zoneCenterX - focusLocalX;
            float posY = zoneCenterY - focusLocalY;

            // Clamp : le rig doit toujours couvrir la zone.
            float minX = zoneRight - rigW * 0.5f;
            float maxX = zoneLeft + rigW * 0.5f;
            float minY = zoneTop - rigH * 0.5f;
            float maxY = zoneBottom + rigH * 0.5f;
            if (minX > maxX)
            {
                float mid = (minX + maxX) * 0.5f;
                minX = mid;
                maxX = mid;
            }
            if (minY > maxY)
            {
                float mid = (minY + maxY) * 0.5f;
                minY = mid;
                maxY = mid;
            }

            posX = Mathf.Clamp(posX, minX, maxX);
            posY = Mathf.Clamp(posY, minY, maxY);

            if (_applyingLayout)
                return true;

            _applyingLayout = true;
            try
            {
                _rigRt.anchorMin = new Vector2(0.5f, 0.5f);
                _rigRt.anchorMax = new Vector2(0.5f, 0.5f);
                _rigRt.pivot = new Vector2(0.5f, 0.5f);
                _rigRt.localScale = Vector3.one;
                _rigRt.sizeDelta = new Vector2(rigW, rigH);
                _rigRt.anchoredPosition = new Vector2(posX, posY);
            }
            finally
            {
                _applyingLayout = false;
            }

            _lastParentW = parentW;
            _lastParentH = parentH;
            _lastBottomInset = bottomInset;
            return true;
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
