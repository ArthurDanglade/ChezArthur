using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages.Invocation
{
    /// <summary>
    /// Chips de specialisation (showcase Personnages) — visuel fort, role-colore.
    /// </summary>
    public static class ShowcaseSpecChips
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float CHIP_H = 28f;
        private const float CHIP_PAD_X = 10f;
        private const float CHIP_MIN_W = 72f;

        // ═══════════════════════════════════════════
        // API
        // ═══════════════════════════════════════════

        /// <summary>
        /// Indices spé : base (-1) puis alternatives 0..n-1.
        /// </summary>
        public static void CollectIndices(CharacterData data, List<int> dest)
        {
            dest.Clear();
            if (data == null)
                return;

            if (data.GetSpecialization(-1) != null)
                dest.Add(-1);

            int alt = data.GetSpecializationCount();
            for (int i = 0; i < alt; i++)
            {
                if (data.GetSpecialization(i) != null)
                    dest.Add(i);
            }
        }

        public static string ShortLabel(SpecializationData spec)
        {
            if (spec == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(spec.SpecName))
                return spec.SpecName;

            return RoleShort(spec.Role);
        }

        public static string RoleShort(CharacterRole role)
        {
            return role switch
            {
                CharacterRole.Attacker => "Attaque",
                CharacterRole.Defender => "Defense",
                CharacterRole.Support => "Soutien",
                _ => "Spe"
            };
        }

        /// <summary>
        /// Remplit un conteneur HorizontalLayoutGroup avec des chips.
        /// interactive=true : bouton + callback ; sinon affichage seul.
        /// </summary>
        public static void Rebuild(
            Transform container,
            CharacterData data,
            Sprite chipSprite,
            int selectedSpecIndex,
            bool interactive,
            Action<int> onSelect)
        {
            if (container == null)
                return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(child.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }

            if (data == null)
                return;

            var indices = new List<int>(4);
            CollectIndices(data, indices);
            if (indices.Count == 0)
                return;

            for (int i = 0; i < indices.Count; i++)
            {
                int specIndex = indices[i];
                SpecializationData spec = data.GetSpecialization(specIndex);
                if (spec == null)
                    continue;

                // Liste : toutes les chips en visuel fort ; carte : seule la selection.
                bool selected = !interactive || specIndex == selectedSpecIndex;
                CreateChip(
                    container,
                    ShortLabel(spec),
                    spec.Role,
                    chipSprite,
                    selected,
                    interactive,
                    specIndex,
                    onSelect);
            }
        }

        // ═══════════════════════════════════════════
        // PRIVÉ
        // ═══════════════════════════════════════════

        private static void CreateChip(
            Transform parent,
            string label,
            CharacterRole role,
            Sprite chipSprite,
            bool selected,
            bool interactive,
            int specIndex,
            Action<int> onSelect)
        {
            GameObject go = new GameObject("SpecChip_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = CHIP_H;
            le.minHeight = CHIP_H;
            le.flexibleWidth = 0f;
            le.preferredWidth = Mathf.Max(CHIP_MIN_W, EstimateWidth(label));

            Image bg = go.AddComponent<Image>();
            if (chipSprite != null)
            {
                bg.sprite = chipSprite;
                bg.type = Image.Type.Sliced;
            }

            Color roleCol = RolePalette.GetColor(role);
            if (selected)
            {
                roleCol.a = 0.92f;
                bg.color = roleCol;
            }
            else
            {
                Color muted = UiTheme.SurfaceBar;
                muted.a = 0.7f;
                bg.color = muted;
            }

            bg.raycastTarget = interactive;

            if (interactive)
            {
                Button btn = go.AddComponent<Button>();
                btn.targetGraphic = bg;
                btn.transition = Selectable.Transition.None;
                int captured = specIndex;
                btn.onClick.AddListener(() => onSelect?.Invoke(captured));
            }

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lRt = labelGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = new Vector2(CHIP_PAD_X * 0.5f, 2f);
            lRt.offsetMax = new Vector2(-CHIP_PAD_X * 0.5f, -2f);

            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = UiTypography.Caption * 0.95f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            tmp.color = selected ? UiTheme.TextPrimary : RolePalette.GetColor(role);
        }

        private static float EstimateWidth(string label)
        {
            if (string.IsNullOrEmpty(label))
                return CHIP_MIN_W;
            // Approx Caption : ~7.5 px / char + padding.
            return CHIP_PAD_X * 2f + label.Length * 7.5f;
        }
    }
}
