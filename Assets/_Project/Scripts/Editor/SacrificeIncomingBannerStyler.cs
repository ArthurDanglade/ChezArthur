using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Pièce 2 — transforme la bande « Bonus entrant » en BANNIÈRE-RÉCOMPENSE horizontale,
    /// dans le langage visuel de BonusCard (tokens UiTheme, cadre d'icône, gros nom, effet).
    ///
    ///   [ cadre icône (couleur rareté) ]   Nom
    ///   [        ICÔNE               ]   Rareté (couleur rareté)
    ///                                     Effet (GetFormattedDescription)
    ///
    /// Récupère les os EXISTANTS par leur CHAMP SÉRIALISÉ (incomingIconImage, etc.) → aucune
    /// recherche par nom, donc pas de desync. Ne REPARENTE ni ne DÉTRUIT aucun enfant : on
    /// re-montre et on restyle en place, plus la CRÉATION du seul os manquant (l'effet), câblé
    /// à incomingEffectText. Les couleurs dépendantes de la rareté (cadre d'icône, label rareté)
    /// sont posées au RUNTIME (pièce 3) — ici on met des valeurs neutres.
    ///
    /// Non-destructif, idempotent (réutilise l'objet effet s'il existe), Undo-safe, auto-auditant.
    /// Réglages en LEVIERS ci-dessous. Utilise UiGen/UiTheme comme BonusCardRebuilder.
    /// </summary>
    public static class SacrificeIncomingBannerStyler
    {
        private const string UI_TYPE = "SacrificeUI";

        // ── Leviers ──
        private const float ICON_SIZE       = 100f;  // cadre d'icône carré
        private const float ICON_INSET      = 12f;   // marge icône dans son cadre
        private const float BAND_MIN_HEIGHT = 110f;  // plancher bannière (icône + marges)
        private const float NAME_FONT       = 30f;
        private const float RARITY_FONT     = 18f;
        private const float EFFECT_FONT     = 22f;

        // Champs sérialisés (noms EXACTS).
        private const string F_CONTAINER = "incomingContainer";
        private const string F_ICON      = "incomingIconImage";
        private const string F_BADGE     = "incomingBadgeBackground";
        private const string F_NAME      = "incomingNameText";
        private const string F_RARITY    = "incomingRarityText";
        private const string F_EFFECT    = "incomingEffectText";

        private const string EFFECT_OBJ_NAME = "IncomingEffectText";

        [MenuItem("Take Five Games/UI/Bonus entrant → bannière récompense")]
        public static void Apply()
        {
            Scene scene = SceneManager.GetActiveScene();
            MonoBehaviour ui = FindByTypeName(scene, UI_TYPE);
            if (ui == null) { Debug.LogError($"[Bannière] Composant '{UI_TYPE}' introuvable dans la scène active."); return; }

            SerializedObject so = new SerializedObject(ui);
            var report = new StringBuilder();
            report.AppendLine("═══════ [Bonus entrant → bannière récompense] rapport ═══════");

            Undo.SetCurrentGroupName("Bonus entrant → bannière récompense");
            int group = Undo.GetCurrentGroup();

            // ── Récupération des os par champ sérialisé ──
            Image iconImg   = RefImage(so, F_ICON, report);
            Image badgeImg  = RefImage(so, F_BADGE, report);
            TextMeshProUGUI nameTmp   = RefTmp(so, F_NAME, report);
            TextMeshProUGUI rarityTmp = RefTmp(so, F_RARITY, report);
            GameObject container = so.FindProperty(F_CONTAINER)?.objectReferenceValue as GameObject;
            if (container == null) report.AppendLine($"    ⚠ '{F_CONTAINER}' non câblé — layout conteneur ignoré.");

            // TextBlock = parent du nom (robuste, pas de recherche par nom).
            Transform textBlock = nameTmp != null ? nameTmp.transform.parent : null;

            // ── 1) Cadre d'icône (IncomingBadge) : re-montré, carré, sprite carte ──
            if (badgeImg != null)
            {
                Show(badgeImg.gameObject, "IncomingBadge (cadre icône)", report);
                Undo.RecordObject(badgeImg, "badge frame");
                badgeImg.sprite = UiGen.Card;
                badgeImg.type = Image.Type.Sliced;
                badgeImg.color = UiTheme.Frame;   // neutre — la rareté est posée au runtime
                EditorUtility.SetDirty(badgeImg);
                PrefabUtility.RecordPrefabInstancePropertyModifications(badgeImg);

                LayoutElement le = badgeImg.GetComponent<LayoutElement>() ?? Undo.AddComponent<LayoutElement>(badgeImg.gameObject);
                Undo.RecordObject(le, "badge size");
                le.minWidth = ICON_SIZE; le.preferredWidth = ICON_SIZE;
                le.minHeight = ICON_SIZE; le.preferredHeight = ICON_SIZE;
                le.flexibleWidth = 0f;
                EditorUtility.SetDirty(le);
                PrefabUtility.RecordPrefabInstancePropertyModifications(le);
                report.AppendLine($"    IncomingBadge → cadre {ICON_SIZE:0} px, sprite carte.");
            }

            // ── 1b) Icône (IncomingIcon) : re-montrée, remplit le cadre, preserveAspect ──
            if (iconImg != null)
            {
                Show(iconImg.gameObject, "IncomingIcon", report);
                Undo.RecordObject(iconImg, "icon");
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
                iconImg.enabled = true;
                EditorUtility.SetDirty(iconImg);
                RectTransform irt = iconImg.rectTransform;
                Undo.RecordObject(irt, "icon rect");
                irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
                irt.offsetMin = new Vector2(ICON_INSET, ICON_INSET);
                irt.offsetMax = new Vector2(-ICON_INSET, -ICON_INSET);
                PrefabUtility.RecordPrefabInstancePropertyModifications(irt);
                report.AppendLine("    IncomingIcon → plein cadre, preserveAspect.");
            }

            // ── 2) Colonne texte : nom, rareté, effet ──
            if (nameTmp != null)
            {
                StyleText(nameTmp, NAME_FONT, UiTheme.TextPrimary, FontStyles.Bold, false, "nom", report);
            }
            if (rarityTmp != null)
            {
                Show(rarityTmp.gameObject, "ReceiveRarity (label rareté)", report);
                // couleur neutre ici — la couleur de rareté est posée au runtime
                StyleText(rarityTmp, RARITY_FONT, UiTheme.TextMuted, FontStyles.Bold, false, "rareté", report);
            }

            // ── 2b) Effet : réutilise l'objet s'il existe, sinon le crée sous TextBlock ──
            TextMeshProUGUI effectTmp = so.FindProperty(F_EFFECT)?.objectReferenceValue as TextMeshProUGUI;
            if (effectTmp == null && textBlock != null)
            {
                Transform existing = FindDescendant(textBlock, EFFECT_OBJ_NAME);
                if (existing != null) effectTmp = existing.GetComponent<TextMeshProUGUI>();
            }
            if (effectTmp == null && textBlock != null)
            {
                var g = new GameObject(EFFECT_OBJ_NAME, typeof(RectTransform));
                ((RectTransform)g.transform).SetParent(textBlock, false);
                Undo.RegisterCreatedObjectUndo(g, "create effect");
                effectTmp = g.AddComponent<TextMeshProUGUI>();
                var font = UiGen.LoadFont(); if (font != null) effectTmp.font = font;
                effectTmp.overflowMode = TextOverflowModes.Overflow;
                g.AddComponent<LayoutElement>();
                report.AppendLine($"    {EFFECT_OBJ_NAME} → créé sous TextBlock.");
            }
            else if (effectTmp != null)
            {
                report.AppendLine($"    {EFFECT_OBJ_NAME} → déjà présent, réutilisé.");
            }
            else
            {
                report.AppendLine("    ⚠ TextBlock introuvable — champ effet NON créé.");
            }

            if (effectTmp != null)
            {
                StyleText(effectTmp, EFFECT_FONT, UiTheme.TextSecondary, FontStyles.Normal, true, "effet", report);
                effectTmp.alignment = TextAlignmentOptions.TopLeft;
                if (string.IsNullOrEmpty(effectTmp.text) || effectTmp.text == EFFECT_OBJ_NAME)
                    effectTmp.text = "Effet…"; // placeholder édition, écrasé au runtime
                // Câblage au champ incomingEffectText
                UiGen.Wire(so, F_EFFECT, effectTmp);
                report.AppendLine("    incomingEffectText → câblé.");
            }

            so.ApplyModifiedProperties();

            // ── 3) Conteneur : HLG horizontal (icône | texte), bannière remontée ──
            if (container != null)
            {
                HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    Undo.RecordObject(hlg, "banner hlg");
                    hlg.childAlignment = TextAnchor.MiddleLeft;
                    hlg.spacing = 20f;
                    hlg.padding = new RectOffset(16, 16, 14, 14);
                    hlg.childControlWidth = true; hlg.childControlHeight = true;
                    hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
                    EditorUtility.SetDirty(hlg);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(hlg);
                    report.AppendLine("    IncomingContainer HLG → icône | texte, alignés gauche.");
                }
                else report.AppendLine("    ⚠ IncomingContainer sans HLG — compo horizontale non appliquée.");

                LayoutElement le = container.GetComponent<LayoutElement>();
                if (le != null)
                {
                    Undo.RecordObject(le, "banner height");
                    le.minHeight = BAND_MIN_HEIGHT;
                    le.preferredHeight = -1f; // content-hug (grandit si l'effet wrappe)
                    EditorUtility.SetDirty(le);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(le);
                    report.AppendLine($"    IncomingContainer hauteur → minH {BAND_MIN_HEIGHT:0}, prefH auto.");
                }
            }

            // Rebuild pour un dump fidèle
            if (container != null && container.transform is RectTransform crt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(crt);

            if (container != null)
            {
                report.AppendLine($"── Structure de '{container.name}' (après réglages) ──");
                DumpTree(container.transform, report, 0);
            }

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            report.AppendLine("═══ Terminé. Applique la pièce 3 (runtime) pour peupler icône/nom/rareté/effet + couleur. ═══");
            Debug.Log(report.ToString());
        }

        // ── Helpers ──

        private static Image RefImage(SerializedObject so, string field, StringBuilder report)
        {
            var p = so.FindProperty(field);
            if (p == null) { report.AppendLine($"    ⚠ champ '{field}' INTROUVABLE (typo ?)."); return null; }
            var img = p.objectReferenceValue as Image;
            if (img == null) report.AppendLine($"    ⚠ '{field}' non câblé dans l'Inspector.");
            return img;
        }

        private static TextMeshProUGUI RefTmp(SerializedObject so, string field, StringBuilder report)
        {
            var p = so.FindProperty(field);
            if (p == null) { report.AppendLine($"    ⚠ champ '{field}' INTROUVABLE (typo ?)."); return null; }
            var t = p.objectReferenceValue as TextMeshProUGUI;
            if (t == null) report.AppendLine($"    ⚠ '{field}' non câblé dans l'Inspector.");
            return t;
        }

        private static void StyleText(TextMeshProUGUI t, float size, Color color, FontStyles style, bool wrap, string label, StringBuilder report)
        {
            Undo.RecordObject(t, "style text");
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = wrap;
            EditorUtility.SetDirty(t);
            PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            report.AppendLine($"    {t.name} → style {label} (font {size:0}, {(wrap ? "wrap" : "1 ligne")}).");
        }

        private static void Show(GameObject go, string label, StringBuilder report)
        {
            if (!go.activeSelf)
            {
                Undo.RecordObject(go, "show");
                go.SetActive(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                report.AppendLine($"    {label} → ré-affiché.");
            }
        }

        private static void DumpTree(Transform t, StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 2);
            LayoutElement le = t.GetComponent<LayoutElement>();
            string leStr = le != null ? $" LE(prefW={le.preferredWidth:0.#},prefH={le.preferredHeight:0.#},flexW={le.flexibleWidth:0.#})" : "";
            string groups = "";
            if (t.GetComponent<VerticalLayoutGroup>() != null) groups += " VLG";
            if (t.GetComponent<HorizontalLayoutGroup>() != null) groups += " HLG";
            RectTransform rt = t as RectTransform;
            string geo = rt != null ? $" size[{rt.rect.width:0.#}x{rt.rect.height:0.#}]" : "";
            sb.AppendLine($"    {indent}{t.name} (active={t.gameObject.activeSelf}){leStr}{groups}{geo}");
            foreach (Transform child in t) DumpTree(child, sb, depth + 1);
        }

        private static Transform FindDescendant(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        private static MonoBehaviour FindByTypeName(Scene scene, string typeName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null && mb.GetType().Name == typeName) return mb;
            return null;
        }
    }
}
