#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;
using ChezArthur.Characters;
using ChezArthur.Core;

namespace ChezArthur.DevTools
{
    /// <summary>
    /// Menu de développement in-game (éditeur + DEVELOPMENT_BUILD uniquement).
    /// Aucun prefab / aucune scène : UI créée à runtime, disponible partout via DontDestroyOnLoad.
    /// </summary>
    public class DevMenuUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int RequiredTouchCount = 4;
        private const float MinButtonHeight = 120f;
        private const float PanelWidth = 900f;
        private const int TalsGrantAmount = 100000;
        private const int BestStageTarget = 50;
        private const KeyCode EditorToggleKey = KeyCode.F1;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private bool _panelVisible;
        private bool _wasFourFingerDown;
        private bool _awaitingResetConfirm;
        private float _fps;
        private float _minFps = float.MaxValue;
        private float _fpsAccum;
        private int _fpsFrames;
        private Text _infoText;
        private Text _statusText;
        private GameObject _panelRoot;
        private Button _resetButton;

        // ═══════════════════════════════════════════
        // BOOTSTRAP
        // ═══════════════════════════════════════════

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            GameObject go = new GameObject("DevMenuUI");
            DontDestroyOnLoad(go);
            go.AddComponent<DevMenuUI>();
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            EnsureEventSystem();
            BuildUi();
            SetPanelVisible(false);
        }

        private void Update()
        {
            UpdateFps();
            HandleToggleInput();

            if (_panelVisible)
                RefreshInfoBanner();
        }

        // ═══════════════════════════════════════════
        // INPUT
        // ═══════════════════════════════════════════

        private void HandleToggleInput()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(EditorToggleKey))
            {
                SetPanelVisible(!_panelVisible);
                return;
            }
#endif
            int touchCount = Input.touchCount;
            bool fourDown = touchCount >= RequiredTouchCount;
            if (fourDown && !_wasFourFingerDown)
                SetPanelVisible(!_panelVisible);
            _wasFourFingerDown = fourDown;
        }

        // ═══════════════════════════════════════════
        // UI CONSTRUCTION
        // ═══════════════════════════════════════════

        private void BuildUi()
        {
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2408f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            _panelRoot = CreatePanel(transform);
            CreateInfoBanner(_panelRoot.transform);
            CreateButton(_panelRoot.transform, "Débloquer tous les personnages", OnUnlockAllCharacters);
            CreateButton(_panelRoot.transform, "+100 000 Tals", OnAddTals);
            CreateButton(_panelRoot.transform, "Best stage = 50", OnSetBestStage);
            _resetButton = CreateButton(_panelRoot.transform, "Réinitialiser la sauvegarde", OnResetSaveClicked);
            _statusText = CreateStatusLabel(_panelRoot.transform);
        }

        private static GameObject CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(PanelWidth, 0f);

            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            ContentSizeFitter fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return panel;
        }

        private void CreateInfoBanner(Transform parent)
        {
            GameObject go = new GameObject("InfoBanner", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = 100f;
            le.preferredHeight = 100f;

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(16f, 8f);
            textRt.offsetMax = new Vector2(-16f, -8f);

            _infoText = textGo.GetComponent<Text>();
            _infoText.font = GetUiFont();
            _infoText.fontSize = 34;
            _infoText.color = Color.white;
            _infoText.alignment = TextAnchor.MiddleLeft;
            _infoText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _infoText.verticalOverflow = VerticalWrapMode.Overflow;
            _infoText.text = "FPS — | Min — | Mem — Mo";
        }

        private Button CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.45f, 0.85f, 1f);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = MinButtonHeight;
            le.preferredHeight = MinButtonHeight;

            Button button = go.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.30f, 0.55f, 0.95f, 1f);
            colors.pressedColor = new Color(0.15f, 0.32f, 0.65f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            Text text = textGo.GetComponent<Text>();
            text.font = GetUiFont();
            text.fontSize = 40;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;

            return button;
        }

        private Text CreateStatusLabel(Transform parent)
        {
            GameObject go = new GameObject("Status", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement le = go.GetComponent<LayoutElement>();
            le.minHeight = 60f;
            le.preferredHeight = 60f;

            Text text = go.GetComponent<Text>();
            text.font = GetUiFont();
            text.fontSize = 30;
            text.color = new Color(0.85f, 0.9f, 0.6f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "";
            return text;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            GameObject es = new GameObject("EventSystem");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void SetPanelVisible(bool visible)
        {
            _panelVisible = visible;
            if (_panelRoot != null)
                _panelRoot.SetActive(visible);

            if (!visible)
                ResetConfirmState();
        }

        // ═══════════════════════════════════════════
        // BANDEAU PERF
        // ═══════════════════════════════════════════

        private void UpdateFps()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f)
                return;

            _fpsAccum += dt;
            _fpsFrames++;
            if (_fpsAccum >= 0.25f)
            {
                _fps = _fpsFrames / _fpsAccum;
                _fpsAccum = 0f;
                _fpsFrames = 0;
                if (_fps < _minFps)
                    _minFps = _fps;
            }
        }

        private void RefreshInfoBanner()
        {
            if (_infoText == null)
                return;

            long allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            float allocatedMo = allocatedBytes / (1024f * 1024f);
            string minLabel = _minFps < float.MaxValue ? _minFps.ToString("0") : "—";
            _infoText.text = $"FPS {_fps:0}  |  Min {minLabel}  |  Mem {allocatedMo:0.0} Mo";
        }

        // ═══════════════════════════════════════════
        // ACTIONS
        // ═══════════════════════════════════════════

        private void OnUnlockAllCharacters()
        {
            ResetConfirmState();

            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
            {
                SetStatus("PersistentManager / Characters indisponible.");
                return;
            }

            CharacterDatabase database = FindLoadedCharacterDatabase();
            if (database == null)
            {
                SetStatus("CharacterDatabase introuvable (aucune instance chargée).");
                Debug.LogError("[DevMenuUI] Aucune CharacterDatabase chargée en mémoire. " +
                               "Impossible d'énumérer les personnages sans API publique d'accès.");
                return;
            }

            IReadOnlyList<CharacterData> all = database.AllCharacters;
            int added = 0;
            int skipped = 0;

            for (int i = 0; i < all.Count; i++)
            {
                CharacterData data = all[i];
                if (data == null || string.IsNullOrEmpty(data.Id))
                {
                    skipped++;
                    continue;
                }

                PersistentManager.Instance.Characters.AddCharacter(data.Id);
                added++;
            }

            PersistentManager.Instance.SaveGame();
            SetStatus($"Personnages traités : {added} (ignorés : {skipped}).");
        }

        private void OnAddTals()
        {
            ResetConfirmState();

            if (PersistentManager.Instance == null)
            {
                SetStatus("PersistentManager indisponible.");
                return;
            }

            PersistentManager.Instance.AddTals(TalsGrantAmount);
            PersistentManager.Instance.SaveGame();
            SetStatus($"+{TalsGrantAmount} Tals (total : {PersistentManager.Instance.Tals}).");
        }

        private void OnSetBestStage()
        {
            ResetConfirmState();

            if (PersistentManager.Instance == null)
            {
                SetStatus("PersistentManager indisponible.");
                return;
            }

            int before = PersistentManager.Instance.BestStage;
            PersistentManager.Instance.UpdateBestStage(BestStageTarget);
            PersistentManager.Instance.SaveGame();

            int after = PersistentManager.Instance.BestStage;
            if (after < BestStageTarget)
            {
                SetStatus($"Best stage inchangé ({after}) — UpdateBestStage n'abaisse pas le record.");
            }
            else
            {
                SetStatus($"Best stage : {before} → {after}.");
            }
        }

        private void OnResetSaveClicked()
        {
            if (!_awaitingResetConfirm)
            {
                _awaitingResetConfirm = true;
                if (_resetButton != null)
                {
                    Text label = _resetButton.GetComponentInChildren<Text>();
                    if (label != null)
                        label.text = "CONFIRMER le reset ?";
                    Image img = _resetButton.GetComponent<Image>();
                    if (img != null)
                        img.color = new Color(0.75f, 0.18f, 0.18f, 1f);
                }
                SetStatus("2e appui pour confirmer le reset.");
                return;
            }

            if (PersistentManager.Instance == null)
            {
                SetStatus("PersistentManager indisponible.");
                ResetConfirmState();
                return;
            }

            SaveSystem.DeleteSave();
            PersistentManager.Instance.LoadGame();
            ResetConfirmState();
            SetStatus("Sauvegarde réinitialisée.");
        }

        private void ResetConfirmState()
        {
            _awaitingResetConfirm = false;
            if (_resetButton == null)
                return;

            Text label = _resetButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = "Réinitialiser la sauvegarde";

            Image img = _resetButton.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.22f, 0.45f, 0.85f, 1f);
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
            Debug.Log("[DevMenuUI] " + message);
        }

        private static CharacterDatabase FindLoadedCharacterDatabase()
        {
            CharacterDatabase[] loaded = Resources.FindObjectsOfTypeAll<CharacterDatabase>();
            if (loaded == null || loaded.Length == 0)
                return null;

            for (int i = 0; i < loaded.Length; i++)
            {
                CharacterDatabase db = loaded[i];
                if (db != null && db.Count > 0)
                    return db;
            }

            return loaded[0];
        }

        private static Font GetUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
#endif
