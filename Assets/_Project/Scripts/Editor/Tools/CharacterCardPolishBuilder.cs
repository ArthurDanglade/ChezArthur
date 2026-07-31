#if UNITY_EDITOR
using System.Text;
using ChezArthur.Hub.Pages;
using ChezArthur.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Polish carte collection 5.a.2 — pas de chip spé, Nv centré, ATK/DEF/SUP, icon plein cadre.
    /// </summary>
    public static class CharacterCardPolishBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/UI/CharacterCard.prefab";
        private const string UndoLabel = "CharacterCard Polish 5.a.2";
        private const float BannerHeight = 44f;
        private const float BadgeSize = 44f;
        private const float BadgeOverhang = 6f;
        private const float AwakenDotSize = 16f;
        private const float InTeamStripH = 4f;
        private const float InTeamCheckSize = 20f;

        [MenuItem("Chez Arthur/Refonte Hub/CharacterCard — Polish 5.a.2 (DRY RUN)")]
        public static void DryRun() => Run(false);

        [MenuItem("Chez Arthur/Refonte Hub/CharacterCard — Polish 5.a.2 (APPLIQUER)")]
        public static void Apply()
        {
            if (!EditorUtility.DisplayDialog(
                    "CharacterCard 5.a.2",
                    "Retire chip spé, bandeau Nv centré + ATK/DEF/SUP, portrait jusqu'en haut du cadre.\n\nContinuer ?",
                    "Appliquer",
                    "Annuler"))
                return;

            Run(true);
        }

        private static void Run(bool apply)
        {
            var log = new StringBuilder(2048);
            log.AppendLine("═══════════════════════════════════════════");
            log.AppendLine($" CharacterCardPolish 5.a.2 — {(apply ? "APPLIQUER" : "DRY RUN")}");
            log.AppendLine("═══════════════════════════════════════════");

            Sprite spriteS = RoundedRectSpriteGenerator.LoadSpriteS();
            if (spriteS == null)
            {
                Debug.LogError("[CharacterCardPolish] RoundedRect_S manquant.");
                return;
            }

            if (!apply)
            {
                log.AppendLine("- [DRY] Restructurer bandeau + icon + purge RoleChip — À FAIRE");
                log.AppendLine("───────────────────────────────────────────");
                log.AppendLine(" À FAIRE=1 | CONFORMES=0 | ÉCHECS=0");
                Debug.Log(log.ToString());
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (root == null)
            {
                Debug.LogError($"[CharacterCardPolish] Prefab introuvable : {PrefabPath}");
                return;
            }

            try
            {
                // Purge RoleChip s'il existe
                Transform roleChipTx = FindChildTrim(root.transform, "RoleChip");
                if (roleChipTx != null)
                {
                    Object.DestroyImmediate(roleChipTx.gameObject);
                    log.AppendLine("- RoleChip purgé ✓");
                }

                CharacterCardUI cardUi = root.GetComponent<CharacterCardUI>();
                if (cardUi == null)
                    cardUi = root.AddComponent<CharacterCardUI>();

                Image rarityBorder = root.GetComponent<Image>();
                Button btn = root.GetComponent<Button>();
                if (btn != null)
                    btn.targetGraphic = rarityBorder;

                Image cardBg = EnsureImage(root.transform, "CardBackground", spriteS);
                StretchInset(cardBg.rectTransform, UiTheme.BorderFocus);
                cardBg.color = UiTheme.BgElevated;
                cardBg.raycastTarget = false;

                Image icon = EnsureImage(root.transform, "IconImage", null);
                icon.preserveAspect = false;
                icon.raycastTarget = false;
                // Haut du cadre → dessus bandeau
                RectTransform iconRt = icon.rectTransform;
                iconRt.anchorMin = Vector2.zero;
                iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = new Vector2(UiTheme.BorderFocus, BannerHeight);
                iconRt.offsetMax = new Vector2(-UiTheme.BorderFocus, -UiTheme.BorderFocus);

                // Badge
                Transform badgeTx = FindChildTrim(root.transform, "BadgeRarity");
                Image badgeImg;
                TextMeshProUGUI badgeTxt;
                if (badgeTx == null)
                {
                    GameObject badgeGo = new GameObject(
                        "BadgeRarity", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    badgeGo.transform.SetParent(root.transform, false);
                    badgeTx = badgeGo.transform;
                    badgeImg = badgeGo.GetComponent<Image>();
                    badgeImg.sprite = spriteS;
                    badgeImg.type = Image.Type.Sliced;
                    badgeTxt = CreateTmp(badgeTx, "BadgeText", "SR", UiTypography.Caption, UiTheme.TextPrimary);
                    badgeTxt.fontStyle = FontStyles.Bold;
                    StretchFull(badgeTxt.rectTransform);
                    badgeTxt.alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    badgeImg = badgeTx.GetComponent<Image>();
                    badgeTxt = badgeTx.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                RectTransform badgeRt = (RectTransform)badgeTx;
                badgeRt.anchorMin = new Vector2(1f, 1f);
                badgeRt.anchorMax = new Vector2(1f, 1f);
                badgeRt.pivot = new Vector2(1f, 1f);
                badgeRt.sizeDelta = new Vector2(BadgeSize, BadgeSize);
                badgeRt.anchoredPosition = new Vector2(BadgeOverhang, BadgeOverhang);

                // AwakenDot
                Image awaken = EnsureImage(root.transform, "AwakenDot", spriteS);
                RectTransform awakenRt = awaken.rectTransform;
                awakenRt.anchorMin = new Vector2(1f, 1f);
                awakenRt.anchorMax = new Vector2(1f, 1f);
                awakenRt.pivot = new Vector2(1f, 1f);
                awakenRt.sizeDelta = new Vector2(AwakenDotSize, AwakenDotSize);
                awakenRt.anchoredPosition = new Vector2(
                    BadgeOverhang - (BadgeSize - AwakenDotSize) * 0.5f,
                    BadgeOverhang - BadgeSize - UiTheme.Space1);
                awaken.color = UiTheme.AccentGold;
                awaken.gameObject.SetActive(false);

                // BottomBanner
                Image banner = EnsureImage(root.transform, "BottomBanner", spriteS);
                RectTransform bannerRt = banner.rectTransform;
                bannerRt.anchorMin = new Vector2(0f, 0f);
                bannerRt.anchorMax = new Vector2(1f, 0f);
                bannerRt.pivot = new Vector2(0.5f, 0f);
                bannerRt.sizeDelta = new Vector2(0f, BannerHeight);
                bannerRt.anchoredPosition = Vector2.zero;
                Color bc = UiTheme.BgElevated;
                bc.a = 0.85f;
                banner.color = bc;
                banner.raycastTarget = false;

                // Level — gauche, centré verticalement
                TextMeshProUGUI level = EnsureTmp(banner.transform, "LevelText", "Nv.1",
                    UiTypography.Caption, UiTheme.TextMuted);
                RectTransform levelRt = level.rectTransform;
                levelRt.anchorMin = new Vector2(0f, 0f);
                levelRt.anchorMax = new Vector2(0.55f, 1f);
                levelRt.offsetMin = new Vector2(UiTheme.Space2, 0f);
                levelRt.offsetMax = new Vector2(-UiTheme.Space1, 0f);
                level.alignment = TextAlignmentOptions.MidlineLeft;
                level.verticalAlignment = VerticalAlignmentOptions.Middle;
                level.gameObject.SetActive(true);

                // Role — droite ATK/DEF/SUP
                TextMeshProUGUI role = EnsureTmp(banner.transform, "RoleLabel", "ATK",
                    UiTypography.Caption, UiTheme.RoleAttacker);
                RectTransform roleRt = role.rectTransform;
                roleRt.anchorMin = new Vector2(0.45f, 0f);
                roleRt.anchorMax = new Vector2(1f, 1f);
                roleRt.offsetMin = new Vector2(UiTheme.Space1, 0f);
                roleRt.offsetMax = new Vector2(-UiTheme.Space2, 0f);
                role.fontStyle = FontStyles.Bold;
                role.alignment = TextAlignmentOptions.MidlineRight;
                role.verticalAlignment = VerticalAlignmentOptions.Middle;

                // NameText masqué s'il existe
                Transform nameTx = FindChildTrim(banner.transform, "NameText");
                if (nameTx == null)
                    nameTx = FindChildTrim(root.transform, "NameText");
                TextMeshProUGUI name = nameTx != null ? nameTx.GetComponent<TextMeshProUGUI>() : null;
                if (name != null)
                    name.gameObject.SetActive(false);

                // InTeam
                Transform inTeamTx = FindChildTrim(root.transform, "InTeamIndicator");
                if (inTeamTx == null)
                {
                    GameObject inTeam = new GameObject("InTeamIndicator", typeof(RectTransform));
                    inTeam.transform.SetParent(root.transform, false);
                    inTeamTx = inTeam.transform;
                    StretchFull((RectTransform)inTeamTx);
                    inTeam.SetActive(false);

                    Image strip = EnsureImage(inTeamTx, "InTeamStrip", null);
                    RectTransform stripRt = strip.rectTransform;
                    stripRt.anchorMin = new Vector2(0f, 0f);
                    stripRt.anchorMax = new Vector2(1f, 0f);
                    stripRt.pivot = new Vector2(0.5f, 0f);
                    stripRt.sizeDelta = new Vector2(0f, InTeamStripH);
                    strip.color = UiTheme.AccentAmber;

                    TextMeshProUGUI check = CreateTmp(
                        inTeamTx, "InTeamCheck", "OK", InTeamCheckSize, UiTheme.AccentAmber);
                    RectTransform checkRt = check.rectTransform;
                    checkRt.anchorMin = new Vector2(1f, 0f);
                    checkRt.anchorMax = new Vector2(1f, 0f);
                    checkRt.pivot = new Vector2(1f, 0f);
                    checkRt.sizeDelta = new Vector2(InTeamCheckSize, InTeamCheckSize);
                    checkRt.anchoredPosition = new Vector2(-UiTheme.Space1, InTeamStripH + UiTheme.Space1);
                }

                Image inTeamStrip = null;
                TextMeshProUGUI inTeamCheck = null;
                Transform stripTx = FindChildTrim(inTeamTx, "InTeamStrip");
                if (stripTx != null)
                    inTeamStrip = stripTx.GetComponent<Image>();
                Transform checkTx = FindChildTrim(inTeamTx, "InTeamCheck");
                if (checkTx != null)
                    inTeamCheck = checkTx.GetComponent<TextMeshProUGUI>();

                SerializedObject so = new SerializedObject(cardUi);
                so.FindProperty("cardBackground").objectReferenceValue = cardBg;
                so.FindProperty("iconImage").objectReferenceValue = icon;
                so.FindProperty("rarityBorder").objectReferenceValue = rarityBorder;
                so.FindProperty("badgeRarityImage").objectReferenceValue = badgeImg;
                so.FindProperty("badgeRarityText").objectReferenceValue = badgeTxt;
                so.FindProperty("badgeSprites").arraySize = 3;
                so.FindProperty("awakenDot").objectReferenceValue = awaken;
                so.FindProperty("bottomBanner").objectReferenceValue = banner;
                so.FindProperty("nameText").objectReferenceValue = name;
                so.FindProperty("levelText").objectReferenceValue = level;
                so.FindProperty("roleLabel").objectReferenceValue = role;
                so.FindProperty("inTeamIndicator").objectReferenceValue = inTeamTx.gameObject;
                so.FindProperty("inTeamStrip").objectReferenceValue = inTeamStrip;
                so.FindProperty("inTeamCheck").objectReferenceValue = inTeamCheck;
                so.FindProperty("cardButton").objectReferenceValue = btn;
                // Ancien champ roleChip éventuel → null
                SerializedProperty roleChipProp = so.FindProperty("roleChip");
                if (roleChipProp != null)
                    roleChipProp.objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                log.AppendLine("- CharacterCard.prefab polish ✓");
                log.AppendLine("  · RoleChip retiré");
                log.AppendLine("  · Icon plein cadre (haut → bandeau)");
                log.AppendLine("  · Bandeau : Nv. centré vertical + RoleLabel ATK/DEF/SUP");
                log.AppendLine("───────────────────────────────────────────");
                log.AppendLine(" À FAIRE=0 | CONFORMES=1 | ÉCHECS=0");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        private static Transform FindChildTrim(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name != null && c.name.Trim() == name)
                    return c;
            }

            return null;
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite)
        {
            Transform tx = FindChildTrim(parent, name);
            Image img;
            if (tx == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                img = go.GetComponent<Image>();
            }
            else
            {
                img = tx.GetComponent<Image>();
                if (img == null)
                    img = tx.gameObject.AddComponent<Image>();
            }

            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = Image.Type.Sliced;
            }

            return img;
        }

        private static TextMeshProUGUI EnsureTmp(
            Transform parent, string name, string text, float size, Color color)
        {
            Transform tx = FindChildTrim(parent, name);
            if (tx != null)
            {
                TextMeshProUGUI existing = tx.GetComponent<TextMeshProUGUI>();
                if (existing != null)
                {
                    existing.text = text;
                    existing.fontSize = size;
                    existing.color = color;
                    existing.raycastTarget = false;
                    return existing;
                }
            }

            return CreateTmp(parent, name, text, size, color);
        }

        private static TextMeshProUGUI CreateTmp(
            Transform parent, string name, string text, float size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchInset(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
#endif
