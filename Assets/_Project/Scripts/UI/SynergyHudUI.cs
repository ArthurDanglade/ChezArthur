using System.Collections;
using System.Collections.Generic;
using ChezArthur.Roguelike;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChezArthur.UI
{
    /// <summary>
    /// Indicateur permanent des synergies actives : chips sous le TurnPill,
    /// panneau de détails au maintien (press → affiche, release → masque).
    /// </summary>
    public class SynergyHudUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float ChipFadeInDuration = 0.2f;
        private const float ChipScaleStart = 0.9f;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Chips")]
        [SerializeField] private RectTransform chipsContainer;
        [SerializeField] private GameObject chipTemplate;

        [Header("Détails (maintien)")]
        [SerializeField] private CanvasGroup detailsPanel;
        [SerializeField] private RectTransform detailsContainer;
        [SerializeField] private GameObject detailsRowTemplate;
        [SerializeField] private SynergyChipsPressHandler pressHandler;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly List<Coroutine> _chipAnimCoroutines = new List<Coroutine>();
        private bool _subscribedSynergyEvents;
        private bool _subscribedValiseEvents;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (chipTemplate != null)
                chipTemplate.SetActive(false);
            if (detailsRowTemplate != null)
                detailsRowTemplate.SetActive(false);

            HideDetailsPanelImmediate();
            StartCoroutine(DelayedInitialRebuild());
        }

        private void OnDestroy()
        {
            UnsubscribeSynergyEvents();
            UnsubscribeValiseEvents();
            StopAllChipAnimations();
        }

        /// <summary>
        /// Attend une frame pour laisser SynergyManager finir son recompte silencieux (Start).
        /// </summary>
        private IEnumerator DelayedInitialRebuild()
        {
            yield return null;

            SubscribeSynergyEvents();
            SubscribeValiseEvents();
            RebuildChips();
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS
        // ═══════════════════════════════════════════
        private void SubscribeSynergyEvents()
        {
            if (_subscribedSynergyEvents || SynergyManager.Instance == null)
                return;

            SynergyManager.Instance.OnSynergyActivated += OnSynergyChanged;
            SynergyManager.Instance.OnSynergyDeactivated += OnSynergyChanged;
            _subscribedSynergyEvents = true;
        }

        private void UnsubscribeSynergyEvents()
        {
            if (!_subscribedSynergyEvents || SynergyManager.Instance == null)
                return;

            SynergyManager.Instance.OnSynergyActivated -= OnSynergyChanged;
            SynergyManager.Instance.OnSynergyDeactivated -= OnSynergyChanged;
            _subscribedSynergyEvents = false;
        }

        private void SubscribeValiseEvents()
        {
            if (_subscribedValiseEvents || ValiseManager.Instance == null)
                return;

            ValiseManager.Instance.OnSlotsChanged += OnValiseSlotsChanged;
            _subscribedValiseEvents = true;
        }

        private void UnsubscribeValiseEvents()
        {
            if (!_subscribedValiseEvents || ValiseManager.Instance == null)
                return;

            ValiseManager.Instance.OnSlotsChanged -= OnValiseSlotsChanged;
            _subscribedValiseEvents = false;
        }

        private void OnSynergyChanged(SynergyData _)
        {
            RebuildChips();
        }

        private void OnValiseSlotsChanged()
        {
            RebuildChips();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES (press handler)
        // ═══════════════════════════════════════════

        /// <summary> Appelé au press sur la rangée de chips. </summary>
        public void OnChipsPressDown()
        {
            if (chipsContainer == null || !chipsContainer.gameObject.activeInHierarchy)
                return;

            RebuildDetailsRows();
            ShowDetailsPanel();
        }

        /// <summary> Appelé au release (ou sortie du collider UI). </summary>
        public void OnChipsPressUp()
        {
            HideDetailsPanelImmediate();
        }

        // ═══════════════════════════════════════════
        // CHIPS
        // ═══════════════════════════════════════════
        private void RebuildChips()
        {
            StopAllChipAnimations();

            if (chipsContainer == null || chipTemplate == null)
                return;

            ClearChipInstances();

            SynergyManager manager = SynergyManager.Instance;
            IReadOnlyList<SynergyData> synergies = manager != null ? manager.ActiveSynergies : null;
            int count = synergies != null ? synergies.Count : 0;

            if (count == 0)
            {
                chipsContainer.gameObject.SetActive(false);
                HideDetailsPanelImmediate();
                return;
            }

            chipsContainer.gameObject.SetActive(true);

            for (int i = 0; i < count; i++)
            {
                SynergyData data = synergies[i];
                if (data == null)
                    continue;

                GameObject chipGo = Instantiate(chipTemplate, chipsContainer);
                chipGo.SetActive(true);

                TextMeshProUGUI label = chipGo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = data.DisplayName;

                _chipAnimCoroutines.Add(StartCoroutine(AnimateChipIn(chipGo)));
            }
        }

        private void ClearChipInstances()
        {
            if (chipsContainer == null || chipTemplate == null)
                return;

            for (int i = chipsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = chipsContainer.GetChild(i);
                if (child.gameObject == chipTemplate)
                    continue;

                Destroy(child.gameObject);
            }
        }

        private IEnumerator AnimateChipIn(GameObject chipGo)
        {
            if (chipGo == null)
                yield break;

            CanvasGroup group = chipGo.GetComponent<CanvasGroup>();
            if (group == null)
                group = chipGo.AddComponent<CanvasGroup>();

            RectTransform rt = chipGo.transform as RectTransform;
            group.alpha = 0f;
            if (rt != null)
                rt.localScale = Vector3.one * ChipScaleStart;

            float elapsed = 0f;
            while (elapsed < ChipFadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ChipFadeInDuration);
                group.alpha = t;
                if (rt != null)
                    rt.localScale = Vector3.one * Mathf.Lerp(ChipScaleStart, 1f, t);
                yield return null;
            }

            group.alpha = 1f;
            if (rt != null)
                rt.localScale = Vector3.one;
        }

        private void StopAllChipAnimations()
        {
            for (int i = 0; i < _chipAnimCoroutines.Count; i++)
            {
                if (_chipAnimCoroutines[i] != null)
                    StopCoroutine(_chipAnimCoroutines[i]);
            }

            _chipAnimCoroutines.Clear();
        }

        // ═══════════════════════════════════════════
        // PANNEAU DÉTAILS
        // ═══════════════════════════════════════════
        private void RebuildDetailsRows()
        {
            if (detailsContainer == null || detailsRowTemplate == null)
                return;

            ClearDetailsRows();

            SynergyManager manager = SynergyManager.Instance;
            IReadOnlyList<SynergyData> synergies = manager != null ? manager.ActiveSynergies : null;
            if (synergies == null)
                return;

            for (int i = 0; i < synergies.Count; i++)
            {
                SynergyData data = synergies[i];
                if (data == null)
                    continue;

                GameObject rowGo = Instantiate(detailsRowTemplate, detailsContainer);
                rowGo.SetActive(true);

                TextMeshProUGUI[] texts = rowGo.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 0)
                    texts[0].text = data.DisplayName;
                if (texts.Length > 1)
                    texts[1].text = data.Description;
            }
        }

        private void ClearDetailsRows()
        {
            if (detailsContainer == null || detailsRowTemplate == null)
                return;

            for (int i = detailsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = detailsContainer.GetChild(i);
                if (child.gameObject == detailsRowTemplate)
                    continue;

                Destroy(child.gameObject);
            }
        }

        private void ShowDetailsPanel()
        {
            if (detailsPanel == null)
                return;

            detailsPanel.alpha = 1f;
            detailsPanel.blocksRaycasts = false;
            detailsPanel.interactable = false;
        }

        private void HideDetailsPanelImmediate()
        {
            if (detailsPanel == null)
                return;

            detailsPanel.alpha = 0f;
            detailsPanel.blocksRaycasts = false;
            detailsPanel.interactable = false;
        }
    }

    /// <summary>
    /// Capte press/release sur le fond de la rangée de chips (raycast cible unique).
    /// </summary>
    public class SynergyChipsPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private SynergyHudUI owner;

        public void SetOwner(SynergyHudUI hud)
        {
            owner = hud;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            owner?.OnChipsPressDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            owner?.OnChipsPressUp();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Relâchement si le doigt glisse hors de la rangée pendant le maintien.
            owner?.OnChipsPressUp();
        }
    }
}
