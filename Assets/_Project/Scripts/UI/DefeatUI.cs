using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Écran de défaite affiché quand tous les alliés meurent.
    /// Séquence « finisher défaite » : slow-mo → fondu overlay → contenu → cascade ranking.
    /// </summary>
    public class DefeatUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES — timing présentation (unscaled)
        // ═══════════════════════════════════════════
        private const float OverlayFadeDuration = 0.45f;
        private const float ContentFadeDuration = 0.35f;
        private const float ContentSlidePixels = 24f;
        private const float EntryFadeDuration = 0.18f;
        private const float EntryStaggerDelay = 0.06f;
        private const float HideFadeDuration = 0.2f;

        [Header("Références UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private RectTransform contentFrameRect;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI stageReachedText;
        [SerializeField] private TextMeshProUGUI talsEarnedText;
        [SerializeField] private TextMeshProUGUI bonusCountText;
        [SerializeField] private TextMeshProUGUI bossesDefeatedText;
        [Tooltip("Ligne de récap des hits Super Lancer — optionnel, null = ignoré")]
        [SerializeField] private TMP_Text _superHitsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button buttonReturnHub;

        [Header("Classement de fin de run")]
        [SerializeField] private Transform rankingContainer;
        [SerializeField] private EndRunCharacterEntryUI entryPrefab;
        [SerializeField] private CharacterDatabase characterDatabase;

        [Header("Masquage HUD combat")]
        [Tooltip("Racines UI masquées à l'affichage (TeamPanel, SynergyHud, barre haut, etc.)")]
        [SerializeField] private GameObject[] combatHudRootsToHide;

        /// <summary> Déclenché quand le joueur clique sur Réessayer. </summary>
        public event Action OnRetryClicked;

        private readonly List<EndRunCharacterEntryUI> _spawnedEntries = new();
        private readonly List<GameObject> _hiddenCombatHudRoots = new();

        private Coroutine _showRoutine;
        private Vector3 _contentRestLocalPosition;
        private bool _contentRestPositionCached;

        private struct RankedEntry
        {
            public string Id;
            public CharacterData Data;
            public CharacterRunStats Stats;
            public long Score;
            public int SpawnIndex;
        }

        private void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(HandleRetryClicked);
            buttonReturnHub?.onClick.AddListener(OnReturnHubClicked);

            ResolvePresentationRefs();
            HideImmediate();
        }

        private void Start()
        {
            Debug.Log($"[DefeatUI] Start - RunManager.Instance est {(RunManager.Instance != null ? "présent" : "NULL")}");

            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunEnded += HandleRunEnded;
                Debug.Log("[DefeatUI] Abonné à OnRunEnded");
            }
            else
            {
                Debug.LogWarning("[DefeatUI] RunManager.Instance est null, impossible de s'abonner !");
            }
        }

        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);
            if (buttonReturnHub != null)
                buttonReturnHub.onClick.RemoveListener(OnReturnHubClicked);

            if (RunManager.Instance != null)
                RunManager.Instance.OnRunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(bool victory)
        {
            Debug.Log($"[DefeatUI] HandleRunEnded appelé, victory = {victory}");

            if (victory) return;

            Show();
        }

        /// <summary>
        /// Lance la séquence d'affichage de l'écran de défaite.
        /// Gate unique : cérémonies d'éveil d'abord si en attente.
        /// </summary>
        public void Show()
        {
            Debug.Log("[DefeatUI] Show appelé");

            AwakeningCeremonyController ceremony = AwakeningCeremonyController.Instance;
            if (ceremony != null && ceremony.HasPendingCeremonies)
            {
                ceremony.PlayCeremonies(BeginDefeatPresentation);
                return;
            }

            BeginDefeatPresentation();
        }

        /// <summary>
        /// Corps historique de Show — présentation défaite (après cérémonies éventuelles).
        /// </summary>
        private void BeginDefeatPresentation()
        {
            if (_showRoutine != null)
                StopCoroutine(_showRoutine);

            ResolvePresentationRefs();
            _showRoutine = StartCoroutine(PlayDefeatSequence());
        }

        /// <summary>
        /// Cache l'écran de défaite (fondu court si visible).
        /// </summary>
        public void Hide()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            if (panelRoot != null && panelRoot.activeSelf && panelCanvasGroup != null && panelCanvasGroup.alpha > 0.01f)
                StartCoroutine(HideFadeRoutine());
            else
                HideImmediate();
        }

        private IEnumerator PlayDefeatSequence()
        {
            UpdateStats();
            GenerateRanking(hideEntries: true);
            PreparePresentationHidden();

            bool beatDone = false;
            if (JuiceDirector.Instance != null)
                JuiceDirector.Instance.PlayDefeatBeat(() => beatDone = true);
            else
                beatDone = true;

            while (!beatDone)
                yield return null;

            Time.timeScale = 0f;
            HideCombatHud();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            float elapsed = 0f;
            while (elapsed < OverlayFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetPanelAlpha(Mathf.Clamp01(elapsed / OverlayFadeDuration));
                yield return null;
            }

            SetPanelAlpha(1f);
            CacheContentRestPosition();

            Vector3 slideStart = _contentRestLocalPosition + new Vector3(0f, -ContentSlidePixels, 0f);
            if (contentFrameRect != null)
                contentFrameRect.localPosition = slideStart;

            elapsed = 0f;
            while (elapsed < ContentFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / ContentFadeDuration);
                SetContentAlpha(t);
                if (contentFrameRect != null)
                    contentFrameRect.localPosition = Vector3.Lerp(slideStart, _contentRestLocalPosition, t);
                yield return null;
            }

            SetContentAlpha(1f);
            if (contentFrameRect != null)
                contentFrameRect.localPosition = _contentRestLocalPosition;

            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                EndRunCharacterEntryUI entry = _spawnedEntries[i];
                if (entry == null)
                    continue;

                yield return FadeEntryIn(entry);

                if (i < _spawnedEntries.Count - 1)
                    yield return new WaitForSecondsRealtime(EntryStaggerDelay);
            }

            EnableInteraction();
            _showRoutine = null;
            Debug.Log("[DefeatUI] Séquence de présentation terminée");
        }

        private IEnumerator FadeEntryIn(EndRunCharacterEntryUI entry)
        {
            CanvasGroup group = EnsureEntryCanvasGroup(entry);
            if (group == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < EntryFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(elapsed / EntryFadeDuration);
                yield return null;
            }

            group.alpha = 1f;
        }

        private IEnumerator HideFadeRoutine()
        {
            float startAlpha = panelCanvasGroup != null ? panelCanvasGroup.alpha : 1f;
            float elapsed = 0f;

            SetInteractionEnabled(false);

            while (elapsed < HideFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Clamp01(elapsed / HideFadeDuration);
                SetPanelAlpha(startAlpha * t);
                SetContentAlpha(t);
                yield return null;
            }

            HideImmediate();
        }

        private void HideImmediate()
        {
            ShowCombatHud();
            ResetPresentationState();

            if (panelRoot != null)
                panelRoot.SetActive(false);

            Time.timeScale = 1f;
        }

        private void PreparePresentationHidden()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            SetPanelAlpha(0f);
            SetContentAlpha(0f);
            SetInteractionEnabled(false);
            CacheContentRestPosition();
        }

        private void ResetPresentationState()
        {
            SetPanelAlpha(0f);
            SetContentAlpha(0f);
            SetInteractionEnabled(false);

            if (contentFrameRect != null && _contentRestPositionCached)
                contentFrameRect.localPosition = _contentRestLocalPosition;
        }

        private void EnableInteraction()
        {
            SetPanelAlpha(1f);
            SetContentAlpha(1f);
            SetInteractionEnabled(true);
        }

        private void SetInteractionEnabled(bool enabled)
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = enabled;
                panelCanvasGroup.blocksRaycasts = enabled;
            }

            if (contentCanvasGroup != null)
            {
                contentCanvasGroup.interactable = enabled;
                contentCanvasGroup.blocksRaycasts = enabled;
            }
        }

        private void SetPanelAlpha(float alpha)
        {
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = alpha;
        }

        private void SetContentAlpha(float alpha)
        {
            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = alpha;
        }

        private void CacheContentRestPosition()
        {
            if (contentFrameRect == null || _contentRestPositionCached)
                return;

            _contentRestLocalPosition = contentFrameRect.localPosition;
            _contentRestPositionCached = true;
        }

        private void ResolvePresentationRefs()
        {
            if (panelRoot == null)
                return;

            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();

            if (contentFrameRect == null)
            {
                Transform frame = panelRoot.transform.Find("EndRunContentFrame");
                if (frame != null)
                    contentFrameRect = frame as RectTransform;
            }

            if (contentCanvasGroup == null && contentFrameRect != null)
                contentCanvasGroup = contentFrameRect.GetComponent<CanvasGroup>();
        }

        private static CanvasGroup EnsureEntryCanvasGroup(EndRunCharacterEntryUI entry)
        {
            if (entry == null)
                return null;

            CanvasGroup group = entry.GetComponent<CanvasGroup>();
            if (group == null)
                group = entry.gameObject.AddComponent<CanvasGroup>();

            return group;
        }

        private void HideCombatHud()
        {
            _hiddenCombatHudRoots.Clear();
            if (combatHudRootsToHide == null)
                return;

            for (int i = 0; i < combatHudRootsToHide.Length; i++)
            {
                GameObject root = combatHudRootsToHide[i];
                if (root == null || !root.activeSelf)
                    continue;

                root.SetActive(false);
                _hiddenCombatHudRoots.Add(root);
            }
        }

        private void ShowCombatHud()
        {
            for (int i = 0; i < _hiddenCombatHudRoots.Count; i++)
            {
                GameObject root = _hiddenCombatHudRoots[i];
                if (root != null)
                    root.SetActive(true);
            }

            _hiddenCombatHudRoots.Clear();
        }

        private void UpdateStats()
        {
            int stageReached = RunManager.Instance != null ? RunManager.Instance.CurrentStage : 1;
            int talsEarned = RunManager.Instance != null ? RunManager.Instance.TalsEarned : 0;
            int bossesDefeated = RunManager.Instance != null ? RunManager.Instance.BossesDefeated : 0;

            if (titleText != null)
                titleText.text = "FIN DE GAME";

            if (stageReachedText != null)
                stageReachedText.text = $"Étage atteint : {stageReached}";

            if (talsEarnedText != null)
                talsEarnedText.text = $"Tals remportés : {talsEarned}";

            if (bossesDefeatedText != null)
                bossesDefeatedText.text = $"Boss vaincus : {bossesDefeated}";

            if (bonusCountText != null)
                bonusCountText.gameObject.SetActive(false);

            if (_superHitsText != null)
            {
                int runHits = SuperLancerSystem.Instance != null
                    ? SuperLancerSystem.Instance.RunSuperHitCount : 0;
                bool newRecord = PersistentManager.Instance != null
                    && PersistentManager.Instance.UpdateBestSuperLancerHits(runHits);
                int best = PersistentManager.Instance != null
                    ? PersistentManager.Instance.BestSuperLancerHits : runHits;
                _superHitsText.text = newRecord
                    ? $"Hits Super Lancer : {runHits} — NOUVEAU RECORD !"
                    : $"Hits Super Lancer : {runHits} (record : {best})";
            }
        }

        private void GenerateRanking(bool hideEntries)
        {
            ClearRankingEntries();

            CombatStatsTracker tracker = CombatStatsTracker.Instance;
            if (tracker == null)
            {
                Debug.LogWarning("[DefeatUI] CombatStatsTracker.Instance est null — classement ignoré.");
                return;
            }

            if (rankingContainer == null)
            {
                Debug.LogWarning("[DefeatUI] rankingContainer est null — classement ignoré.");
                return;
            }

            if (entryPrefab == null)
            {
                Debug.LogWarning("[DefeatUI] entryPrefab est null — classement ignoré.");
                return;
            }

            if (characterDatabase == null)
            {
                Debug.LogWarning("[DefeatUI] characterDatabase est null — classement ignoré.");
                return;
            }

            IReadOnlyList<string> trackedIds = tracker.TrackedCharacterIds;
            if (trackedIds == null || trackedIds.Count == 0)
            {
                Debug.LogWarning("[DefeatUI] TrackedCharacterIds vide — classement ignoré.");
                return;
            }

            var list = new List<RankedEntry>(trackedIds.Count);
            for (int i = 0; i < trackedIds.Count; i++)
            {
                string id = trackedIds[i];
                CharacterData data = characterDatabase.GetById(id);
                if (data == null)
                {
                    Debug.LogWarning($"[DefeatUI] CharacterData introuvable pour '{id}', entrée ignorée.");
                    continue;
                }

                CharacterRunStats stats = tracker.GetStatsFor(id);
                list.Add(new RankedEntry
                {
                    Id = id,
                    Data = data,
                    Stats = stats,
                    Score = stats.DamageDealt + stats.DamageTaken + stats.HealingDone,
                    SpawnIndex = i
                });
            }

            list.Sort((a, b) =>
            {
                int byScore = b.Score.CompareTo(a.Score);
                if (byScore != 0) return byScore;
                int byDealt = b.Stats.DamageDealt.CompareTo(a.Stats.DamageDealt);
                if (byDealt != 0) return byDealt;
                return a.SpawnIndex.CompareTo(b.SpawnIndex);
            });

            int teamSize = list.Count;
            for (int i = 0; i < teamSize; i++)
            {
                EndRunCharacterEntryUI entry = Instantiate(entryPrefab, rankingContainer);
                entry.Setup(list[i].Data, i + 1, teamSize, list[i].Stats);
                _spawnedEntries.Add(entry);

                if (hideEntries)
                {
                    CanvasGroup group = EnsureEntryCanvasGroup(entry);
                    if (group != null)
                        group.alpha = 0f;
                }
            }
        }

        private void ClearRankingEntries()
        {
            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                EndRunCharacterEntryUI entry = _spawnedEntries[i];
                if (entry != null)
                    Destroy(entry.gameObject);
            }

            _spawnedEntries.Clear();
        }

        private void HandleRetryClicked()
        {
            Hide();
            OnRetryClicked?.Invoke();

            if (RunManager.Instance != null)
                RunManager.Instance.StartRun();
        }

        private void OnReturnHubClicked()
        {
            Hide();
            SceneLoader.LoadHub();
        }
    }
}
