#if UNITY_EDITOR
using System.Collections.Generic;
using ChezArthur.Characters;
using ChezArthur.Gacha;
using ChezArthur.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// OUTIL DESTRUCTIF — fenêtre Gate 3 gacha.
    /// Construit TrainScene + rebuild DoorScene sur GachaAnimationUI (Hub).
    /// </summary>
    public static class GachaTrainSceneBuilder
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float RefHeight = 1920f;
        private const float TrainHeightRatio = 0.30f;
        private const float DoorHeightRatio = 0.55f;
        private const float DoorAspectW = 228f;
        private const float DoorAspectH = 342f;
        private const float GlowSizeMul = 1.4f;
        private const float TrainAnchorY = 0.45f;

        private const string TrainSpritePath =
            "Assets/_Project/Sprites/Gasha/train_side_sprite.png";
        private const string FlipbookPngPath =
            "Assets/_Project/Sprites/Gasha/flipbook_gacha_door.png";
        private const string FlipbookJsonPath =
            "Assets/_Project/Sprites/Gasha/flipbook_gacha_door.json";
        private const string CurveAssetPath =
            "Assets/_Project/Data/Gacha/TrainCurve_Depart.asset";
        private const string DoorDataPath =
            "Assets/_Project/Data/Flipbooks/PA_flipbook_gacha_door.asset";
        private const string RadialGlowPath = "Assets/_Project/Art/FX/fx_radial_glow.png";
        private const string GlowMatPath = "Assets/_Project/Art/FX/AwakeningGlow.mat";
        private const string WhitePixelPath = "Assets/_Project/Art/UI/ui_white_pixel.png";

        // ═══════════════════════════════════════════
        // MENU
        // ═══════════════════════════════════════════

        [MenuItem("Chez Arthur/UI/Construire scène train gacha (fenêtre Gate 3)")]
        private static void BuildMenu()
        {
            bool ok = EditorUtility.DisplayDialog(
                "Scène train gacha",
                "OUTIL DESTRUCTIF — fenêtre Gate 3 gacha.\n\n" +
                "Modifie GachaAnimationUI dans la scène ouverte (Hub).\n" +
                "Rebuild TrainScene + contenu DoorScene.\n\nContinuer ?",
                "Construire",
                "Annuler");
            if (!ok)
                return;

            try
            {
                BuildInOpenScene();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GachaTrainSceneBuilder] Échec : {ex}");
            }
        }

        // ═══════════════════════════════════════════
        // BUILD
        // ═══════════════════════════════════════════

        private static void BuildInOpenScene()
        {
            EnsureArtAssets();

            GachaAnimationController gac = FindGachaAnimationController();
            if (gac == null)
            {
                Debug.LogError(
                    "[GachaTrainSceneBuilder] GachaAnimationUI introuvable " +
                    "(ouvre Hub.unity puis relance).");
                return;
            }

            GameObject root = gac.gameObject;
            Undo.RegisterFullObjectHierarchyUndo(root, "Gate 3 — scène train gacha");

            Transform rootTf = root.transform;
            GameObject doorSceneGo = FindChildGo(rootTf, "DoorScene");
            if (doorSceneGo == null)
            {
                Debug.LogError("[GachaTrainSceneBuilder] DoorScene introuvable sous GachaAnimationUI.");
                return;
            }

            // ── TrainScene ──
            GameObject trainScene = EnsureChild(rootTf, "TrainScene");
            trainScene.SetActive(false);
            StretchFull(trainScene.GetComponent<RectTransform>());
            // Frère de CrankScene, AVANT DoorScene
            trainScene.transform.SetSiblingIndex(doorSceneGo.transform.GetSiblingIndex());

            ClearChildrenExcept(trainScene.transform);

            GameObject trainMask = CreateUiChild(trainScene.transform, "TrainMask");
            StretchFull(trainMask.GetComponent<RectTransform>());
            if (trainMask.GetComponent<RectMask2D>() == null)
                trainMask.AddComponent<RectMask2D>();

            Sprite trainSpriteAsset = LoadSprite(TrainSpritePath);
            GameObject trainSpriteGo = CreateUiChild(trainMask.transform, "TrainSprite");
            Image trainImg = trainSpriteGo.GetComponent<Image>();
            trainImg.sprite = trainSpriteAsset;
            trainImg.preserveAspect = true;
            trainImg.raycastTarget = false;
            if (trainSpriteAsset != null)
                trainImg.SetNativeSize();

            RectTransform trainRt = trainSpriteGo.GetComponent<RectTransform>();
            FitTrainSprite(trainRt);

            Button tapTrain = CreateTapCatcher(trainScene.transform, "TapCatcherTrain");

            // ── DoorScene contenu ──
            Transform doorTf = doorSceneGo.transform;
            GameObject landscape = FindChildGo(doorTf, "LandscapeLayer");
            GameObject wagon = FindChildGo(doorTf, "WagonInterior");

            // Supprimer anciens contenus remplacés (DoorPanel, LightEffect, etc.)
            DestroyDoorRebuildTargets(doorTf, landscape, wagon);

            Sprite glowSprite = LoadSprite(RadialGlowPath);
            Material glowMat = AssetDatabase.LoadAssetAtPath<Material>(GlowMatPath);

            GameObject rarityGlowGo = CreateUiChild(doorTf, "RarityGlow");
            Image rarityGlow = rarityGlowGo.GetComponent<Image>();
            rarityGlow.sprite = glowSprite;
            rarityGlow.material = glowMat;
            rarityGlow.raycastTarget = false;
            Color glowCol = rarityGlow.color;
            glowCol.a = 0f;
            rarityGlow.color = glowCol;

            GameObject doorViewGo = CreateUiChild(doorTf, "DoorView");
            // Retirer Image ajoutée par CreateUiChild, remplacer par RawImage
            Object.DestroyImmediate(doorViewGo.GetComponent<Image>());
            RawImage doorRaw = doorViewGo.AddComponent<RawImage>();
            doorRaw.enabled = false;
            doorRaw.raycastTarget = false;
            PortraitAnimator doorAnim = doorViewGo.GetComponent<PortraitAnimator>();
            if (doorAnim == null)
                doorAnim = doorViewGo.AddComponent<PortraitAnimator>();
            doorAnim.Initialize(doorRaw);
            FitDoorView(doorViewGo.GetComponent<RectTransform>());

            // Glow derrière la porte, ~1.4× la porte
            SizeGlowBehindDoor(rarityGlowGo.GetComponent<RectTransform>(), doorViewGo.GetComponent<RectTransform>());
            rarityGlowGo.transform.SetSiblingIndex(0);
            if (landscape != null)
                landscape.transform.SetSiblingIndex(0);
            if (wagon != null)
                wagon.transform.SetSiblingIndex(landscape != null ? 1 : 0);

            // Ordre final : Landscape → Wagon → RarityGlow → DoorView → Tap
            ReorderDoorChildren(doorTf, landscape, wagon, rarityGlowGo, doorViewGo);

            Button tapDoor = CreateTapCatcher(doorTf, "TapCatcherDoor");
            if (doorViewGo != null)
                doorViewGo.transform.SetSiblingIndex(
                    rarityGlowGo != null ? rarityGlowGo.transform.GetSiblingIndex() + 1 : 0);
            tapDoor.transform.SetAsLastSibling();

            // Smoke overlay : remonter sous GachaAnimationUI (dernier = au-dessus)
            Image smoke = FindSmokeImage(rootTf);
            if (smoke != null)
            {
                smoke.transform.SetParent(rootTf, false);
                StretchFull(smoke.rectTransform);
                smoke.transform.SetAsLastSibling();
                Color sc = smoke.color;
                sc.a = 0f;
                smoke.color = sc;
                smoke.gameObject.SetActive(false);
            }

            // ── TrainSequenceController ──
            TrainSequenceController seq = root.GetComponent<TrainSequenceController>();
            if (seq == null)
                seq = Undo.AddComponent<TrainSequenceController>(root);

            TrainCurveData curve =
                AssetDatabase.LoadAssetAtPath<TrainCurveData>(CurveAssetPath);
            AnimatedPortraitData doorData =
                AssetDatabase.LoadAssetAtPath<AnimatedPortraitData>(DoorDataPath);

            SerializedObject so = new SerializedObject(seq);
            so.FindProperty("trainScene").objectReferenceValue = trainScene;
            so.FindProperty("trainSprite").objectReferenceValue = trainRt;
            so.FindProperty("doorScene").objectReferenceValue = doorSceneGo;
            so.FindProperty("doorView").objectReferenceValue = doorAnim;
            so.FindProperty("doorRawImage").objectReferenceValue = doorRaw;
            so.FindProperty("rarityGlow").objectReferenceValue = rarityGlow;
            so.FindProperty("smokeTransition").objectReferenceValue = smoke;
            so.FindProperty("departCurve").objectReferenceValue = curve;
            so.FindProperty("doorFlipbook").objectReferenceValue = doorData;

            SerializedProperty tapsProp = so.FindProperty("tapButtons");
            tapsProp.arraySize = 2;
            tapsProp.GetArrayElementAtIndex(0).objectReferenceValue = tapTrain;
            tapsProp.GetArrayElementAtIndex(1).objectReferenceValue = tapDoor;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Câble GachaAnimationController.trainSequence
            SerializedObject gacSo = new SerializedObject(gac);
            SerializedProperty trainSeqProp = gacSo.FindProperty("trainSequence");
            if (trainSeqProp != null)
            {
                trainSeqProp.objectReferenceValue = seq;
                gacSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Smoke aussi sur GAC si champ présent
            SerializedProperty smokeProp = gacSo.FindProperty("smokeTransition");
            if (smokeProp != null && smoke != null)
            {
                smokeProp.objectReferenceValue = smoke;
                gacSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log(
                "[GachaTrainSceneBuilder] Câblage récap :\n" +
                $"  trainScene={(trainScene != null)}\n" +
                $"  trainSprite={(trainRt != null)} sprite={(trainSpriteAsset != null)}\n" +
                $"  doorScene={(doorSceneGo != null)}\n" +
                $"  doorView/RawImage={(doorAnim != null)}/{(doorRaw != null)}\n" +
                $"  rarityGlow={(rarityGlow != null)} mat={(glowMat != null)}\n" +
                $"  tapTrain/Door={(tapTrain != null)}/{(tapDoor != null)}\n" +
                $"  departCurve={(curve != null)} @ {CurveAssetPath}\n" +
                $"  doorFlipbook={(doorData != null)} @ {DoorDataPath}\n" +
                $"  smokeOverlay={(smoke != null)}");
        }

        // ═══════════════════════════════════════════
        // ART ASSETS
        // ═══════════════════════════════════════════

        private static void EnsureArtAssets()
        {
            TrainCurveImporter.ImportOrUpdate();

            string pngAbs = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), FlipbookPngPath));
            string jsonAbs = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), FlipbookJsonPath));

            if (System.IO.File.Exists(pngAbs) && System.IO.File.Exists(jsonAbs))
            {
                SsrPortraitImporter.ImportFlipbookFromSourceFiles(
                    "gacha_door", pngAbs, jsonAbs);
            }
            else
            {
                Debug.LogWarning(
                    "[GachaTrainSceneBuilder] flipbook_gacha_door.png/.json introuvables dans Sprites/Gasha.");
            }
        }

        // ═══════════════════════════════════════════
        // HIERARCHY HELPERS
        // ═══════════════════════════════════════════

        private static GachaAnimationController FindGachaAnimationController()
        {
            GachaAnimationController[] all =
                Object.FindObjectsOfType<GachaAnimationController>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.name == "GachaAnimationUI")
                    return all[i];
            }

            return all.Length > 0 ? all[0] : null;
        }

        private static GameObject FindChildGo(Transform parent, string name)
        {
            if (parent == null)
                return null;
            Transform t = parent.Find(name);
            return t != null ? t.gameObject : null;
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            GameObject existing = FindChildGo(parent, name);
            if (existing != null)
                return existing;

            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateUiChild(Transform parent, string name)
        {
            GameObject go = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = Color.white;
            return go;
        }

        private static void ClearChildrenExcept(Transform parent)
        {
            List<Transform> toDestroy = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
                toDestroy.Add(parent.GetChild(i));

            for (int i = 0; i < toDestroy.Count; i++)
                Undo.DestroyObjectImmediate(toDestroy[i].gameObject);
        }

        private static void DestroyDoorRebuildTargets(
            Transform doorTf,
            GameObject landscape,
            GameObject wagon)
        {
            List<Transform> toDestroy = new List<Transform>();
            for (int i = 0; i < doorTf.childCount; i++)
            {
                Transform child = doorTf.GetChild(i);
                if (landscape != null && child.gameObject == landscape)
                    continue;
                if (wagon != null && child.gameObject == wagon)
                    continue;
                toDestroy.Add(child);
            }

            for (int i = 0; i < toDestroy.Count; i++)
                Undo.DestroyObjectImmediate(toDestroy[i].gameObject);
        }

        private static void ReorderDoorChildren(
            Transform doorTf,
            GameObject landscape,
            GameObject wagon,
            GameObject rarityGlow,
            GameObject doorView)
        {
            int index = 0;
            if (landscape != null)
                landscape.transform.SetSiblingIndex(index++);
            if (wagon != null)
                wagon.transform.SetSiblingIndex(index++);
            if (rarityGlow != null)
                rarityGlow.transform.SetSiblingIndex(index++);
            if (doorView != null)
                doorView.transform.SetSiblingIndex(index++);

            Transform tap = doorTf.Find("TapCatcherDoor");
            if (tap != null)
                tap.SetAsLastSibling();
        }

        private static Button CreateTapCatcher(Transform parent, string name)
        {
            GameObject go = CreateUiChild(parent, name);
            StretchFull(go.GetComponent<RectTransform>());
            Image img = go.GetComponent<Image>();
            Sprite pixel = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePixelPath);
            img.sprite = pixel;
            Color c = Color.white;
            c.a = 0f;
            img.color = c;
            img.raycastTarget = true;

            Button btn = go.GetComponent<Button>();
            if (btn == null)
                btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            go.transform.SetAsLastSibling();
            return btn;
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
                return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void FitTrainSprite(RectTransform rt)
        {
            float targetH = RefHeight * TrainHeightRatio;
            float h = rt.sizeDelta.y > 1f ? rt.sizeDelta.y : targetH;
            float scale = targetH / h;
            rt.localScale = new Vector3(scale, scale, 1f);

            rt.anchorMin = new Vector2(0.5f, TrainAnchorY);
            rt.anchorMax = new Vector2(0.5f, TrainAnchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void FitDoorView(RectTransform rt)
        {
            float h = RefHeight * DoorHeightRatio;
            float w = h * (DoorAspectW / DoorAspectH);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void SizeGlowBehindDoor(RectTransform glow, RectTransform door)
        {
            if (glow == null || door == null)
                return;

            glow.anchorMin = door.anchorMin;
            glow.anchorMax = door.anchorMax;
            glow.pivot = door.pivot;
            glow.anchoredPosition = door.anchoredPosition;
            glow.sizeDelta = door.sizeDelta * GlowSizeMul;
            glow.localScale = Vector3.one;
        }

        private static Image FindSmokeImage(Transform root)
        {
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject.name == "SmokeTransition")
                    return images[i];
            }

            return null;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null)
                return s;

            // Fallback : sous-asset sprite
            Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is Sprite sp)
                    return sp;
            }

            return null;
        }
    }
}
#endif
