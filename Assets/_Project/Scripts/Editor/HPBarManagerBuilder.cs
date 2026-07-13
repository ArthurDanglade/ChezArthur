#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ChezArthur.UI;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Monte la hiérarchie et le wiring du HPBarManager (barres ennemies détachées).
    /// Crée un Canvas world space dédié + un prefab EnemyHPBar minimal + assigne le manager.
    /// </summary>
    public static class HPBarManagerBuilder
    {
        private const string BarsCanvasName = "EnemyHPBarsCanvas";
        private const string ManagerName = "HPBarManager";

        private const string SourcePrefabPath = "Assets/_Project/Prefabs/UI/EnemyHPBarPrefab.prefab";
        private const string OutputPrefabPath = "Assets/_Project/Prefabs/UI/EnemyHPBar_World.prefab";

        [MenuItem("Chez Arthur/UI/Monter HPBarManager (ennemis)")]
        public static void Build()
        {
            Undo.SetCurrentGroupName("Monter HPBarManager");
            int undoGroup = Undo.GetCurrentGroup();

            Canvas barsCanvas = EnsureBarsCanvas();
            EnemyHPBar barPrefab = EnsureBarPrefab();
            EnsureManager(barsCanvas, barPrefab);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[HPBarManagerBuilder] Montage terminé. À vérifier : HPBarManager actif + Canvas world space présent.");
        }

        private static Canvas EnsureBarsCanvas()
        {
            GameObject existing = GameObject.Find(BarsCanvasName);
            GameObject go = existing;
            if (go == null)
            {
                go = new GameObject(BarsCanvasName, typeof(RectTransform), typeof(Canvas));
                Undo.RegisterCreatedObjectUndo(go, "Créer canvas barres ennemies");
            }

            Canvas canvas = go.GetComponent<Canvas>();
            Undo.RecordObject(canvas, "Configurer canvas barres ennemies");
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvas.sortingOrder = 200;

            RectTransform rt = go.GetComponent<RectTransform>();
            Undo.RecordObject(rt, "Configurer RectTransform canvas barres ennemies");
            rt.position = Vector3.zero;
            rt.rotation = Quaternion.identity;
            rt.localScale = Vector3.one * 0.01f;
            rt.sizeDelta = new Vector2(2000f, 2000f);

            return canvas;
        }

        private static EnemyHPBar EnsureBarPrefab()
        {
            Sprite bgSprite = null;
            Sprite fillSprite = null;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            if (source != null)
            {
                Image[] imgs = source.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < imgs.Length; i++)
                {
                    if (imgs[i] == null) continue;
                    if (bgSprite == null && imgs[i].name.ToLower().Contains("background"))
                        bgSprite = imgs[i].sprite;
                    if (fillSprite == null && imgs[i].name.ToLower().Contains("fill"))
                        fillSprite = imgs[i].sprite;
                }
            }

            if (bgSprite == null)
                bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (fillSprite == null)
                fillSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject root = new GameObject("EnemyHPBar_World", typeof(RectTransform));
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(100f, 12f);

            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.SetParent(rootRt, false);
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            Image bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(1f, 1f, 1f, 0.85f);
            bgImg.raycastTarget = false;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.SetParent(bgRt, false);
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(1f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(0f, 10f);
            fillRt.anchoredPosition = Vector2.zero;

            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 1f;
            fillImg.color = Color.white;
            fillImg.raycastTarget = false;

            EnemyHPBar bar = root.AddComponent<EnemyHPBar>();
            SerializedObject so = new SerializedObject(bar);
            so.FindProperty("backgroundImage").objectReferenceValue = bgImg;
            so.FindProperty("fillImage").objectReferenceValue = fillImg;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/UI");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(OutputPrefabPath) != null)
                AssetDatabase.DeleteAsset(OutputPrefabPath);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab == null)
                return null;

            EnemyHPBar prefabBar = prefab.GetComponent<EnemyHPBar>();
            Debug.Log($"[HPBarManagerBuilder] Prefab barre créé : {OutputPrefabPath}");
            return prefabBar;
        }

        private static void EnsureManager(Canvas barsCanvas, EnemyHPBar barPrefab)
        {
            if (barsCanvas == null || barPrefab == null)
            {
                Debug.LogWarning("[HPBarManagerBuilder] barsCanvas ou barPrefab null — manager non créé.");
                return;
            }

            HPBarManager manager = Object.FindObjectOfType<HPBarManager>(true);
            GameObject go = manager != null ? manager.gameObject : null;
            if (go == null)
            {
                go = new GameObject(ManagerName, typeof(HPBarManager));
                Undo.RegisterCreatedObjectUndo(go, "Créer HPBarManager");
            }

            manager = go.GetComponent<HPBarManager>();
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("barsCanvas").objectReferenceValue = barsCanvas;
            so.FindProperty("barPrefab").objectReferenceValue = barPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
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

