using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Passives.Handlers;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bouton discret « Repositionner portails » visible uniquement au tour de Faille
    /// une fois les portails déjà posés (Option B).
    /// </summary>
    public class FaillePlacementUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static FaillePlacementUI _instance;
        private Canvas _canvas;
        private GameObject _buttonRoot;
        private TextMeshProUGUI _hintText;
        private TurnManager _turnManager;
        private bool _subscribed;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_instance == this)
                _instance = null;
        }

        private void LateUpdate()
        {
            EnsureBuilt();
            RefreshVisibility();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Crée l'UI runtime si absente de la scène.
        /// </summary>
        public static void EnsureExists()
        {
            if (_instance != null) return;
            var go = new GameObject("FaillePlacementUI");
            go.AddComponent<FaillePlacementUI>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private void EnsureBuilt()
        {
            if (_buttonRoot != null) return;

            var canvasGo = new GameObject("FaillePlacementCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 80;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _buttonRoot = new GameObject("RepositionButton", typeof(RectTransform));
            _buttonRoot.transform.SetParent(canvasGo.transform, false);
            var rt = _buttonRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            rt.sizeDelta = new Vector2(280f, 56f);

            Image bg = _buttonRoot.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.92f);

            Button btn = _buttonRoot.AddComponent<Button>();
            btn.onClick.AddListener(OnRepositionClicked);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(_buttonRoot.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _hintText = textGo.AddComponent<TextMeshProUGUI>();
            _hintText.text = "Repositionner portails";
            _hintText.alignment = TextAlignmentOptions.Center;
            _hintText.fontSize = 22f;
            _hintText.color = Color.white;

            _buttonRoot.SetActive(false);

            _turnManager = Object.FindObjectOfType<TurnManager>();
            Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed || _turnManager == null) return;
            _turnManager.OnTurnChanged += OnTurnChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || _turnManager == null) return;
            _turnManager.OnTurnChanged -= OnTurnChanged;
            _subscribed = false;
        }

        private void OnTurnChanged(ITurnParticipant _)
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (_buttonRoot == null) return;

            FailleSystem system = FailleSystem.Instance;
            bool show = false;
            string label = "Repositionner portails";

            if (system != null
                && system.Owner != null
                && _turnManager != null
                && ReferenceEquals(_turnManager.CurrentParticipant, system.Owner)
                && _turnManager.IsPlayerTurn)
            {
                if (system.IsPlacementMode)
                {
                    show = true;
                    label = system.RequiresPlacement && !system.PortalsPlaced
                        ? "Placez 2 portails (bordures)"
                        : "Placement en cours…";
                }
                else if (system.PortalsPlaced)
                {
                    show = true;
                    label = "Repositionner portails";
                }
                else if (system.RequiresPlacement)
                {
                    show = true;
                    label = "Placez 2 portails (bordures)";
                }
            }

            if (_hintText != null)
                _hintText.text = label;

            if (_buttonRoot.activeSelf != show)
                _buttonRoot.SetActive(show);
        }

        private void OnRepositionClicked()
        {
            FailleSystem system = FailleSystem.Instance;
            if (system == null) return;

            if (system.IsPlacementMode)
                return;

            system.BeginPlacement();
            RefreshVisibility();
        }
    }
}
