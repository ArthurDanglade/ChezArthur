using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.UI;

namespace ChezArthur.Hub.Pages
{
    /// <summary>
    /// Interactions collection ↔ équipe (Gate 5.b/5.c) :
    /// tap → détail ; maintien → ajouter/retirer ; glisser → scroll.
    /// Feedbacks : radial hold, punch, shake, pulse Danger dock.
    /// </summary>
    public class TeamDragController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const float LongPressSeconds = 0.4f;
        private const float MoveCancelPx = 8f;

        private enum PressSourceKind
        {
            None,
            CollectionCard,
            TeamSlot
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références page")]
        [SerializeField] private TeamPageUI teamPageUI;
        [SerializeField] private TeamSlotUI[] teamSlots;
        [SerializeField] private ScrollRect collectionScroll;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private RectTransform teamDock;
        [SerializeField] private GameObject dragHintRoot;
        [SerializeField] private Graphic teamDockPulseGraphic;

        [Header("Hold FX")]
        [SerializeField] private Sprite holdRingSprite;

        [Header("Legacy")]
        [SerializeField] private Sprite ghostBorderSprite;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private int _pointerId = int.MinValue;
        private bool _pressActive;
        private bool _holdConsumed;
        private bool _movedBeyondCancel;
        private float _pressTime;
        private Vector2 _pressScreenPos;

        private PressSourceKind _sourceKind;
        private string _characterId;
        private CharacterCardUI _sourceCard;
        private TeamSlotUI _sourceSlot;

        private HoldProgressFX _holdFx;
        private Coroutine _feedbackRoutine;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void OnEnable()
        {
            RefreshHintVisibility();
        }

        private void OnDisable()
        {
            CancelPressImmediate();
        }

        private void Update()
        {
            if (!_pressActive || _holdConsumed || _movedBeyondCancel)
                return;

            if (!TryGetPointerScreenPos(_pointerId, out Vector2 screenPos))
            {
                CancelPressImmediate();
                return;
            }

            if ((screenPos - _pressScreenPos).sqrMagnitude > MoveCancelPx * MoveCancelPx)
            {
                _movedBeyondCancel = true;
                if (_sourceCard != null)
                    _sourceCard.MarkLifted();
                HideHoldFx();
                return;
            }

            float progress = (Time.unscaledTime - _pressTime) / LongPressSeconds;
            if (_holdFx != null)
                _holdFx.SetProgress(progress);

            if (progress >= 1f)
                ExecuteHoldAction();
        }

        // ═══════════════════════════════════════════
        // API PUBLIQUE — sources
        // ═══════════════════════════════════════════

        public void NotifyCardPointerDown(CharacterCardUI card, PointerEventData eventData)
        {
            if (card == null || eventData == null)
                return;
            if (_pressActive || _holdConsumed)
                return;
            if (_pointerId != int.MinValue && eventData.pointerId != _pointerId)
                return;

            _pointerId = eventData.pointerId;
            _pressActive = true;
            _holdConsumed = false;
            _movedBeyondCancel = false;
            _pressTime = Time.unscaledTime;
            _pressScreenPos = eventData.position;
            _sourceKind = PressSourceKind.CollectionCard;
            _sourceCard = card;
            _sourceSlot = null;
            _characterId = card.CharacterId;
            card.BeginPotentialDrag();
            BeginHoldFx((RectTransform)card.transform);
        }

        public void NotifySlotPointerDown(TeamSlotUI slot, PointerEventData eventData)
        {
            if (slot == null || eventData == null)
                return;
            if (slot.IsEmpty)
                return;
            if (_pressActive || _holdConsumed)
                return;
            if (_pointerId != int.MinValue && eventData.pointerId != _pointerId)
                return;

            _pointerId = eventData.pointerId;
            _pressActive = true;
            _holdConsumed = false;
            _movedBeyondCancel = false;
            _pressTime = Time.unscaledTime;
            _pressScreenPos = eventData.position;
            _sourceKind = PressSourceKind.TeamSlot;
            _sourceSlot = slot;
            _sourceCard = null;
            _characterId = slot.CharacterId;
            slot.BeginPotentialDrag();
            BeginHoldFx((RectTransform)slot.transform);
        }

        public void NotifyPointerUp(PointerEventData eventData)
        {
            if (eventData == null)
                return;
            if (_pointerId != int.MinValue && eventData.pointerId != _pointerId)
                return;

            if (!_pressActive && !_holdConsumed)
                return;

            HideHoldFx();

            if (_holdConsumed)
            {
                ClearPressState();
                return;
            }

            float distSq = (eventData.position - _pressScreenPos).sqrMagnitude;
            bool moved = _movedBeyondCancel
                         || eventData.dragging
                         || distSq > MoveCancelPx * MoveCancelPx;
            bool heldLong = Time.unscaledTime - _pressTime >= LongPressSeconds;
            bool skipTap = moved || heldLong;

            PressSourceKind kind = _sourceKind;
            CharacterCardUI card = _sourceCard;
            TeamSlotUI slot = _sourceSlot;
            ClearPressState();

            if (skipTap)
                return;

            if (kind == PressSourceKind.CollectionCard && card != null)
                card.NotifyShortTap();
            else if (kind == PressSourceKind.TeamSlot && slot != null && !slot.IsEmpty)
                OpenSlotDetail(slot);
        }

        public void MarkHintSeenAndHide()
        {
            if (PersistentManager.Instance != null)
                PersistentManager.Instance.SetHintTeamDragSeen(true);

            if (dragHintRoot != null)
                dragHintRoot.SetActive(false);
        }

        public void RefreshHintVisibility()
        {
            if (dragHintRoot == null)
                return;

            bool seen = PersistentManager.Instance != null
                        && PersistentManager.Instance.HintTeamDragSeen;
            if (seen)
            {
                dragHintRoot.SetActive(false);
                return;
            }

            CharacterManager cm = GetCharacters();
            if (cm == null)
            {
                dragHintRoot.SetActive(false);
                return;
            }

            int teamCount = cm.GetSelectedTeamIds().Count;
            int ownedCount = cm.GetOwnedCharacters().Count;
            dragHintRoot.SetActive(teamCount < CharacterManager.MAX_TEAM_SIZE && ownedCount > 0);
        }

        /// <summary> Pulse Danger sur le dock (échec hold — équipe pleine). </summary>
        public void PulseDockDanger()
        {
            Graphic g = teamDockPulseGraphic;
            if (g == null && teamDock != null)
                g = teamDock.GetComponent<Graphic>();
            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = HoldFeedback.PlayDockDangerPulse(this, g);
        }

        // ═══════════════════════════════════════════
        // MAINTIEN
        // ═══════════════════════════════════════════

        private void ExecuteHoldAction()
        {
            _holdConsumed = true;
            if (_sourceCard != null)
                _sourceCard.MarkLifted();
            HideHoldFx();

            CharacterManager cm = GetCharacters();
            if (cm == null || string.IsNullOrEmpty(_characterId))
                return;

            if (_sourceKind == PressSourceKind.CollectionCard)
            {
                if (cm.IsInTeam(_characterId))
                    return;

                if (cm.GetSelectedTeamIds().Count >= CharacterManager.MAX_TEAM_SIZE)
                {
                    PlayFail(_sourceCard != null ? _sourceCard.transform : null, null);
                    return;
                }

                if (cm.AddToTeam(_characterId))
                {
                    PersistentManager.Instance.SaveGame();
                    MarkHintSeenAndHide();
                    PlaySuccess(
                        _sourceCard != null ? _sourceCard.transform : null,
                        _sourceCard != null ? _sourceCard.RarityBorderImage : null);
                }
            }
            else if (_sourceKind == PressSourceKind.TeamSlot)
            {
                if (cm.RemoveFromTeam(_characterId))
                {
                    PersistentManager.Instance.SaveGame();
                    PlaySuccess(
                        _sourceSlot != null ? _sourceSlot.transform : null,
                        null);
                    RefreshHintVisibility();
                }
            }
        }

        private void PlaySuccess(Transform target, Image border)
        {
            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = HoldFeedback.PlaySuccess(this, target, border);
        }

        private void PlayFail(Transform target, Image border)
        {
            if (_feedbackRoutine != null)
                StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = HoldFeedback.PlayFailShake(this, target);
            PulseDockDanger();
        }

        private void BeginHoldFx(RectTransform host)
        {
            HideHoldFx();
            if (host == null)
                return;
            _holdFx = HoldProgressFX.Ensure(host, holdRingSprite);
            if (_holdFx != null)
                _holdFx.ShowAt(Vector2.zero);
        }

        private void HideHoldFx()
        {
            if (_holdFx != null)
                _holdFx.Hide();
            _holdFx = null;
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private void CancelPressImmediate()
        {
            HideHoldFx();
            ClearPressState();
            _holdConsumed = false;
        }

        private void ClearPressState()
        {
            if (_sourceCard != null)
                _sourceCard.EndPotentialDrag();
            if (_sourceSlot != null)
                _sourceSlot.EndPotentialDrag();

            _pressActive = false;
            _holdConsumed = false;
            _movedBeyondCancel = false;
            _pointerId = int.MinValue;
            _sourceKind = PressSourceKind.None;
            _sourceCard = null;
            _sourceSlot = null;
            _characterId = null;
        }

        private void OpenSlotDetail(TeamSlotUI slot)
        {
            if (teamPageUI == null || slot == null)
                return;
            ResolveOwnedData(slot.CharacterId, out CharacterData data, out OwnedCharacter owned);
            if (data != null && owned != null)
                teamPageUI.OpenDetail(data, owned);
        }

        private static CharacterManager GetCharacters()
        {
            if (PersistentManager.Instance == null)
                return null;
            return PersistentManager.Instance.Characters;
        }

        private static void ResolveOwnedData(
            string id, out CharacterData data, out OwnedCharacter owned)
        {
            data = null;
            owned = null;
            CharacterManager cm = GetCharacters();
            if (cm == null || string.IsNullOrEmpty(id))
                return;
            var pair = cm.GetCharacterWithData(id);
            data = pair.data;
            owned = pair.owned;
        }

        private static bool TryGetPointerScreenPos(int pointerId, out Vector2 pos)
        {
            if (pointerId == -1)
            {
                pos = Input.mousePosition;
                return Input.GetMouseButton(0);
            }

            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.fingerId == pointerId)
                    {
                        pos = t.position;
                        return t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                    }
                }
            }

            pos = Input.mousePosition;
            return Input.GetMouseButton(0);
        }
    }
}
