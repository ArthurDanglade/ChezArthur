using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Bandeau d'initiative R1 : affiche les 4-5 prochains tours via TurnManager.PeekUpcoming.
    /// 100 % événementiel (aucun Update) — pastilles pré-créées, activation seulement.
    /// </summary>
    public class InitiativeBannerUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int PEEK_COUNT = 5;
        private const float CURRENT_SCALE = 1.15f;
        private const float NORMAL_SCALE = 1f;
        private const float CURRENT_FRAME_LIGHTEN = 0.35f;

        // ═══════════════════════════════════════════
        // TYPES SÉRIALISÉS
        // ═══════════════════════════════════════════

        [System.Serializable]
        public class SlotVisual
        {
            public RectTransform root;
            public Image frame;
            public Image icon;
            public GameObject separator;
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références")]
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private SlotVisual[] slots = new SlotVisual[PEEK_COUNT];

        [Header("Couleurs cadre")]
        [SerializeField] private Color allyFrameColor = new Color(62f / 255f, 107f / 255f, 143f / 255f, 1f);
        [SerializeField] private Color enemyFrameColor = new Color(143f / 255f, 62f / 255f, 62f / 255f, 1f);

        [Header("Layout")]
        [Tooltip("Offset Y ancré top-center (sous le header). Ajustable Inspector.")]
        [SerializeField] private float panelAnchoredY = -100f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<ITurnParticipant> _peekBuffer = new List<ITurnParticipant>(8);
        private Coroutine _delayedRefreshRoutine;
        private bool _subscribedTurnManager;
        private bool _subscribedRunManager;
        private RunManager _runManager;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (turnManager == null)
                turnManager = FindObjectOfType<TurnManager>();

            ApplyPanelAnchoredY();
        }

        private void Start()
        {
            Subscribe();
            RefreshDelayed();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (_delayedRefreshRoutine != null)
            {
                StopCoroutine(_delayedRefreshRoutine);
                _delayedRefreshRoutine = null;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — abonnements
        // ═══════════════════════════════════════════

        private void Subscribe()
        {
            if (turnManager != null && !_subscribedTurnManager)
            {
                turnManager.OnTurnChanged += HandleTurnChanged;
                turnManager.OnParticipantDeath += HandleParticipantDeath;
                turnManager.OnCycleStarted += HandleCycleStarted;
                turnManager.OnEnemyAddedMidCombat += HandleEnemyAddedMidCombat;
                _subscribedTurnManager = true;
            }

            _runManager = RunManager.Instance;
            if (_runManager != null && !_subscribedRunManager)
            {
                _runManager.OnStageReached += HandleStageReached;
                _runManager.OnRunStarted += HandleRunStarted;
                _subscribedRunManager = true;
            }
        }

        private void Unsubscribe()
        {
            if (turnManager != null && _subscribedTurnManager)
            {
                turnManager.OnTurnChanged -= HandleTurnChanged;
                turnManager.OnParticipantDeath -= HandleParticipantDeath;
                turnManager.OnCycleStarted -= HandleCycleStarted;
                turnManager.OnEnemyAddedMidCombat -= HandleEnemyAddedMidCombat;
                _subscribedTurnManager = false;
            }

            if (_runManager != null && _subscribedRunManager)
            {
                _runManager.OnStageReached -= HandleStageReached;
                _runManager.OnRunStarted -= HandleRunStarted;
                _subscribedRunManager = false;
            }

            _runManager = null;
        }

        private void HandleTurnChanged(ITurnParticipant _)
        {
            Refresh();
        }

        private void HandleParticipantDeath(ITurnParticipant _)
        {
            Refresh();
        }

        private void HandleCycleStarted()
        {
            Refresh();
        }

        private void HandleEnemyAddedMidCombat(Enemy _)
        {
            Refresh();
        }

        private void HandleStageReached(int _)
        {
            RefreshDelayed();
        }

        private void HandleRunStarted()
        {
            RefreshDelayed();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — refresh
        // ═══════════════════════════════════════════

        private void RefreshDelayed()
        {
            if (!isActiveAndEnabled)
                return;

            if (_delayedRefreshRoutine != null)
                StopCoroutine(_delayedRefreshRoutine);

            _delayedRefreshRoutine = StartCoroutine(RefreshAfterOneFrame());
        }

        private IEnumerator RefreshAfterOneFrame()
        {
            yield return null;
            _delayedRefreshRoutine = null;
            Refresh();
        }

        private void Refresh()
        {
            if (turnManager == null || canvasGroup == null)
                return;

            if (!turnManager.HasCurrentParticipant)
            {
                canvasGroup.alpha = 0f;
                HideAllSlots();
                return;
            }

            canvasGroup.alpha = 1f;

            int boundary;
            int written = turnManager.PeekUpcoming(PEEK_COUNT, _peekBuffer, out boundary);
            int slotCount = slots != null ? slots.Length : 0;

            for (int i = 0; i < slotCount; i++)
            {
                SlotVisual slot = slots[i];
                if (slot == null || slot.root == null)
                    continue;

                if (i >= written)
                {
                    slot.root.gameObject.SetActive(false);
                    continue;
                }

                ITurnParticipant participant = _peekBuffer[i];
                if (participant == null)
                {
                    slot.root.gameObject.SetActive(false);
                    continue;
                }

                slot.root.gameObject.SetActive(true);
                ApplySlot(slot, participant, isCurrent: i == 0);
                SetSeparatorActive(slot, boundary >= 0 && i == boundary);
            }
        }

        private void HideAllSlots()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                SlotVisual slot = slots[i];
                if (slot == null || slot.root == null)
                    continue;
                slot.root.gameObject.SetActive(false);
            }
        }

        private void ApplySlot(SlotVisual slot, ITurnParticipant participant, bool isCurrent)
        {
            bool isAlly = participant.IsAlly;
            Color frameColor = isAlly ? allyFrameColor : enemyFrameColor;
            if (isCurrent)
                frameColor = Color.Lerp(frameColor, Color.white, CURRENT_FRAME_LIGHTEN);

            if (slot.frame != null)
                slot.frame.color = frameColor;

            Sprite sprite = ResolveIcon(participant);
            if (slot.icon != null)
            {
                slot.icon.enabled = sprite != null;
                slot.icon.sprite = sprite;
                slot.icon.preserveAspect = true;
            }

            float scale = isCurrent ? CURRENT_SCALE : NORMAL_SCALE;
            slot.root.localScale = new Vector3(scale, scale, 1f);
        }

        private static void SetSeparatorActive(SlotVisual slot, bool active)
        {
            if (slot.separator == null)
                return;
            if (slot.separator.activeSelf != active)
                slot.separator.SetActive(active);
        }

        private static Sprite ResolveIcon(ITurnParticipant participant)
        {
            CharacterBall ally = participant as CharacterBall;
            if (ally != null)
            {
                CharacterData data = ally.Data;
                if (data == null)
                    return null;
                if (data.Icon != null)
                    return data.Icon;
                return data.CombatSprite;
            }

            Enemy enemy = participant as Enemy;
            if (enemy != null && enemy.Data != null)
                return enemy.Data.CombatSprite;

            return null;
        }

        private void ApplyPanelAnchoredY()
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null)
                return;

            Vector2 pos = rt.anchoredPosition;
            pos.y = panelAnchoredY;
            rt.anchoredPosition = pos;
        }
    }
}
