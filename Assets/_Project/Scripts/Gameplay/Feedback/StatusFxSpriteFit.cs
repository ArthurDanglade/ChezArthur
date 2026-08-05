using ChezArthur.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Place / scale un FX d'état pour qu'il entoure le sprite réel (allié ou ennemi).
    /// </summary>
    public static class StatusFxSpriteFit
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float DefaultDesignSize = 1.2f;
        private const float DefaultPadding = 1.2f;
        private const float MinScale = 0.35f;
        private const float MaxScale = 4.5f;
        private const float MinExtent = 0.08f;

        // Remap pack Y-up → plan XY (enfants pack souvent à −90° X).
        private static readonly Quaternion RemapYUpToXy = Quaternion.Euler(90f, 0f, 0f);

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Résout le SpriteRenderer visuel d'une unité (balle / ennemi / enfant Visual).
        /// </summary>
        public static SpriteRenderer ResolveRenderer(
            Transform unitRoot,
            CharacterBall ball,
            Enemy enemy)
        {
            if (ball != null && ball.VisualRenderer != null)
                return ball.VisualRenderer;

            if (unitRoot == null && enemy != null)
                unitRoot = enemy.transform;

            if (unitRoot == null)
                return null;

            Transform visual = FindChildVisual(unitRoot);
            if (visual != null)
            {
                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                    return sr;
            }

            SpriteRenderer[] renderers = unitRoot.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer best = null;
            float bestArea = -1f;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null || sr.sprite == null)
                    continue;
                if (!IsFiniteBounds(sr.bounds))
                    continue;

                float area = sr.bounds.size.x * sr.bounds.size.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = sr;
                }
            }

            return best;
        }

        /// <summary>
        /// Parent au renderer, centre sur bounds.sprite, scale ∝ taille sprite, sorting sync.
        /// </summary>
        public static void Apply(ParticleSystem fxRoot, SpriteRenderer target, float scaleMul = 1f)
        {
            if (fxRoot == null || target == null)
                return;

            StatusFxFitProfile profile = fxRoot.GetComponent<StatusFxFitProfile>();
            float design = profile != null ? profile.designSize : DefaultDesignSize;
            float padding = profile != null ? profile.padding : DefaultPadding;
            bool remap = profile == null || profile.remapYUpToXy;
            int orderOffset = profile != null ? profile.sortingOrderOffset : 2;

            if (design < 0.01f)
                design = DefaultDesignSize;
            if (padding < 0.01f)
                padding = DefaultPadding;

            Transform parent = target.transform;
            Transform t = fxRoot.transform;
            t.SetParent(parent, false);

            Sprite sprite = target.sprite;
            Vector3 localCenter = Vector3.zero;
            float extent = MinExtent;

            if (sprite != null)
            {
                Bounds b = sprite.bounds;
                if (IsFiniteBounds(b))
                {
                    localCenter = b.center;
                    extent = Mathf.Max(b.size.x, b.size.y);
                }
            }

            if (extent < MinExtent)
                extent = MinExtent;

            float scale = (extent * padding / design) * Mathf.Max(0.01f, scaleMul);
            scale = Mathf.Clamp(scale, MinScale, MaxScale);
            if (!IsFinite(scale))
                scale = 1f;

            t.localPosition = localCenter;
            t.localRotation = remap ? RemapYUpToXy : Quaternion.identity;
            t.localScale = Vector3.one * scale;

            SyncSorting(fxRoot, target, orderOffset);
        }

        /// <summary>
        /// Remet transform neutre (pool Release).
        /// </summary>
        public static void ResetTransform(Transform t)
        {
            if (t == null)
                return;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private static void SyncSorting(ParticleSystem root, SpriteRenderer target, int orderOffset)
        {
            ParticleSystemRenderer[] renderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);
            int order = target.sortingOrder + orderOffset;
            int layerId = target.sortingLayerID;

            for (int i = 0; i < renderers.Length; i++)
            {
                ParticleSystemRenderer r = renderers[i];
                if (r == null)
                    continue;
                r.sortingLayerID = layerId;
                r.sortingOrder = order;
                r.alignment = ParticleSystemRenderSpace.View;
                r.allowRoll = false;
            }
        }

        private static Transform FindChildVisual(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;
                if (string.Equals(child.name.Trim(), "Visual", System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return root.Find("Visual");
        }

        private static bool IsFiniteBounds(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 s = bounds.size;
            return IsFinite(c.x) && IsFinite(c.y) && IsFinite(c.z)
                && IsFinite(s.x) && IsFinite(s.y) && IsFinite(s.z)
                && s.x < 1000f && s.y < 1000f;
        }

        private static bool IsFinite(float v) => !(float.IsNaN(v) || float.IsInfinity(v));
    }
}
