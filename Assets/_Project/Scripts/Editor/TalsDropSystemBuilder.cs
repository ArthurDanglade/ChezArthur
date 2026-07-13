#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gameplay;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte TalsDropSystem en une passe : prefab pièce, wiring scène Game (HUD, caméra, SFX, slider volume).
    /// </summary>
    public static class TalsDropSystemBuilder
    {
        private const string ManagerName = "TalsDropSystem";
        private const string CoinContainerName = "CoinContainer";
        private const string CoinPrefabPath = "Assets/_Project/Prefabs/VFX/TalsDropCoin.prefab";
        private const string CoinSprite1Path = "Assets/_Project/Sprites/UI/Tals1.png";
        private const string CoinSprite2Path = "Assets/_Project/Sprites/UI/Tals2.png";
        private const string CoinSprite3Path = "Assets/_Project/Sprites/UI/Tals3.png";
        private const string PickupClip1Path = "Assets/_Project/Audio/SFX/Talsound1.mp3";
        private const string PickupClip2Path = "Assets/_Project/Audio/SFX/talsound2.mp3";

        [MenuItem("Chez Arthur/UI/Monter TalsDropSystem (pluie de tals)")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Monter TalsDropSystem");
            int undoGroup = Undo.GetCurrentGroup();

            Sprite[] coinSprites = LoadCoinSprites();
            GameObject coinPrefab = EnsureCoinPrefab(coinSprites.Length > 0 ? coinSprites[0] : null);
            GameUI gameUI = Object.FindObjectOfType<GameUI>(true);
            RectTransform counterTarget = FindCounterTarget(gameUI);
            Camera worldCamera = Camera.main;
            if (worldCamera == null)
                worldCamera = Object.FindObjectOfType<Camera>(true);

            AudioClip pickupClip1 = AssetDatabase.LoadAssetAtPath<AudioClip>(PickupClip1Path);
            AudioClip pickupClip2 = AssetDatabase.LoadAssetAtPath<AudioClip>(PickupClip2Path);

            TalsDropSystem system = EnsureManager(
                coinPrefab,
                coinSprites,
                counterTarget,
                gameUI,
                worldCamera,
                pickupClip1,
                pickupClip2);

            EnsureTalsPickupSlider();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            if (system == null)
            {
                Debug.LogError("[TalsDropSystemBuilder] Échec du montage — voir warnings ci-dessus.");
                return;
            }

            Selection.activeGameObject = system.gameObject;
            Debug.Log("[TalsDropSystemBuilder] Montage terminé (sprites Tals1-3, sons alternés, slider volume réglages).");
        }

        private static Sprite[] LoadCoinSprites()
        {
            Sprite s1 = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSprite1Path);
            Sprite s2 = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSprite2Path);
            Sprite s3 = AssetDatabase.LoadAssetAtPath<Sprite>(CoinSprite3Path);

            int count = 0;
            if (s1 != null) count++;
            if (s2 != null) count++;
            if (s3 != null) count++;
            if (count == 0)
            {
                Debug.LogWarning("[TalsDropSystemBuilder] Sprites UI introuvables (Tals1/2/3).");
                return System.Array.Empty<Sprite>();
            }

            Sprite[] sprites = new Sprite[count];
            int index = 0;
            if (s1 != null) sprites[index++] = s1;
            if (s2 != null) sprites[index++] = s2;
            if (s3 != null) sprites[index] = s3;
            return sprites;
        }

        private static GameObject EnsureCoinPrefab(Sprite sprite)
        {
            if (sprite == null)
            {
                Debug.LogWarning("[TalsDropSystemBuilder] Sprite prefab par défaut introuvable.");
                sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            }

            GameObject root = new GameObject("TalsDropCoin", typeof(SpriteRenderer));
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 20;

            root.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/VFX");

            if (AssetDatabase.LoadAssetAtPath<GameObject>(CoinPrefabPath) != null)
                AssetDatabase.DeleteAsset(CoinPrefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, CoinPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TalsDropSystemBuilder] Prefab pièce créé : {CoinPrefabPath}");
            return prefab;
        }

        private static TalsDropSystem EnsureManager(
            GameObject coinPrefab,
            Sprite[] coinSprites,
            RectTransform counterTarget,
            GameUI gameUI,
            Camera worldCamera,
            AudioClip pickupClip1,
            AudioClip pickupClip2)
        {
            if (coinPrefab == null)
            {
                Debug.LogWarning("[TalsDropSystemBuilder] coinPrefab null — manager non créé.");
                return null;
            }

            TalsDropSystem system = Object.FindObjectOfType<TalsDropSystem>(true);
            GameObject go = system != null ? system.gameObject : null;
            if (go == null)
            {
                go = new GameObject(ManagerName, typeof(TalsDropSystem));
                Undo.RegisterCreatedObjectUndo(go, "Créer TalsDropSystem");
            }

            Transform container = go.transform.Find(CoinContainerName);
            if (container == null)
            {
                GameObject containerGo = new GameObject(CoinContainerName);
                Undo.RegisterCreatedObjectUndo(containerGo, "Créer CoinContainer");
                containerGo.transform.SetParent(go.transform, false);
            }

            system = go.GetComponent<TalsDropSystem>();
            AudioClip[] pickupClips = BuildPickupClips(pickupClip1, pickupClip2);

            SerializedObject so = new SerializedObject(system);
            so.FindProperty("coinPrefab").objectReferenceValue = coinPrefab;

            SerializedProperty spritesProp = so.FindProperty("coinSprites");
            spritesProp.arraySize = coinSprites.Length;
            for (int i = 0; i < coinSprites.Length; i++)
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = coinSprites[i];

            so.FindProperty("counterTarget").objectReferenceValue = counterTarget;
            so.FindProperty("gameUI").objectReferenceValue = gameUI;
            so.FindProperty("worldCamera").objectReferenceValue = worldCamera;

            SerializedProperty clipsProp = so.FindProperty("pickupClips");
            clipsProp.arraySize = pickupClips.Length;
            for (int i = 0; i < pickupClips.Length; i++)
                clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = pickupClips[i];

            so.FindProperty("coinSortingOrder").intValue = 20;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (counterTarget == null)
                Debug.LogWarning("[TalsDropSystemBuilder] counterTarget introuvable — assigne TalsGroup manuellement.");
            if (gameUI == null)
                Debug.LogWarning("[TalsDropSystemBuilder] GameUI introuvable dans la scène.");
            if (worldCamera == null)
                Debug.LogWarning("[TalsDropSystemBuilder] Caméra introuvable — assigne Main Camera.");
            if (pickupClips.Length == 0)
                Debug.LogWarning($"[TalsDropSystemBuilder] Clips introuvables : {PickupClip1Path}, {PickupClip2Path}");

            return system;
        }

        private static void EnsureTalsPickupSlider()
        {
            SettingsPanelUI settings = Object.FindObjectOfType<SettingsPanelUI>(true);
            if (settings == null)
            {
                Debug.LogWarning("[TalsDropSystemBuilder] SettingsPanelUI introuvable — slider Tals non créé.");
                return;
            }

            Transform panel = settings.transform;
            Slider slider = null;

            Transform existing = panel.Find("TalsPickupSlider");
            if (existing != null)
                slider = existing.GetComponent<Slider>();

            if (slider == null)
            {
                Transform template = panel.Find("SFXSlider");
                if (template == null)
                {
                    Debug.LogWarning("[TalsDropSystemBuilder] SFXSlider introuvable — slider Tals non créé.");
                    return;
                }

                GameObject clone = Object.Instantiate(template.gameObject, panel);
                clone.name = "TalsPickupSlider";
                Undo.RegisterCreatedObjectUndo(clone, "Créer TalsPickupSlider");

                RectTransform rt = clone.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0f, -190f);

                Transform label = clone.transform.Find("SFXText");
                if (label != null)
                {
                    label.name = "TalsPickupText";
                    TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                        tmp.text = "Tals";
                }

                slider = clone.GetComponent<Slider>();
            }

            SerializedObject settingsSo = new SerializedObject(settings);
            settingsSo.FindProperty("talsPickupSlider").objectReferenceValue = slider;
            settingsSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip[] BuildPickupClips(AudioClip clip1, AudioClip clip2)
        {
            int count = 0;
            if (clip1 != null) count++;
            if (clip2 != null) count++;
            if (count == 0)
                return System.Array.Empty<AudioClip>();

            AudioClip[] clips = new AudioClip[count];
            int index = 0;
            if (clip1 != null) clips[index++] = clip1;
            if (clip2 != null) clips[index] = clip2;
            return clips;
        }

        private static RectTransform FindCounterTarget(GameUI gameUI)
        {
            if (gameUI != null)
            {
                SerializedObject so = new SerializedObject(gameUI);
                SerializedProperty talsTextProp = so.FindProperty("talsText");
                if (talsTextProp != null
                    && talsTextProp.objectReferenceValue is TextMeshProUGUI talsText
                    && talsText.transform.parent is RectTransform parentRt)
                {
                    return parentRt;
                }
            }

            RectTransform[] all = Object.FindObjectsOfType<RectTransform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;

                if (all[i].name.Trim() == "TalsGroup")
                    return all[i];
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int lastSlash = path.LastIndexOf('/');
            if (lastSlash <= 0)
                return;

            string parent = path.Substring(0, lastSlash);
            string name = path.Substring(lastSlash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
