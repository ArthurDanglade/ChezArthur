using System.Collections.Generic;
using ChezArthur.Missions;
using ChezArthur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// Page Hub Missions — TabBar 4 couches, bonus conditionnel, liste scrollable.
    /// Gate 4.b : IMissionProvider = MissionsProviderReal (MissionManager).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MissionsPageUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string NavTabId = "missions";
        private static readonly string[] LayerLabels =
        {
            "Quotidien", "Hebdo", "Saison", "Permanent"
        };

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Structure")]
        [SerializeField] private TabBarUI tabBar;
        [SerializeField] private RectTransform layerBonusRoot;
        [SerializeField] private MissionEntryUI layerBonusEntry;
        [SerializeField] private ScrollRect missionScroll;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private MissionEntryUI entryTemplate;

        [Header("Empty Saison")]
        [SerializeField] private GameObject seasonEmptyRoot;
        [SerializeField] private TextMeshProUGUI seasonEmptyLabel;

        [Header("Nav badge")]
        [SerializeField] private HubNavBarUI navBar;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private IMissionProvider _provider;
        private MissionLayer _currentLayer = MissionLayer.Daily;
        private readonly List<MissionUiEntry> _buffer = new List<MissionUiEntry>(16);
        private readonly List<MissionEntryUI> _spawned = new List<MissionEntryUI>(16);
        private bool _tabBarInited;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (entryTemplate != null)
                entryTemplate.gameObject.SetActive(false);

            EnsureProvider();
            EnsureTabBar();
        }

        private void OnEnable()
        {
            EnsureProvider();
            EnsureTabBar();

            if (_provider != null)
                _provider.OnChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (_provider != null)
                _provider.OnChanged -= Refresh;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Branche un provider (tests) ; en prod Awake utilise MissionsProviderReal.Shared.
        /// </summary>
        public void SetProvider(IMissionProvider provider)
        {
            if (_provider != null)
                _provider.OnChanged -= Refresh;

            _provider = provider;

            if (isActiveAndEnabled && _provider != null)
                _provider.OnChanged += Refresh;

            Refresh();
        }

        /// <summary> Rafraîchit couche courante + badge nav. </summary>
        public void Refresh()
        {
            RefreshLayer(_currentLayer);
            RefreshNavBadge();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureProvider()
        {
            if (_provider != null)
                return;

            MissionsProviderReal.Shared.EnsureBound();
            _provider = MissionsProviderReal.Shared;
        }

        private void EnsureTabBar()
        {
            if (_tabBarInited || tabBar == null)
                return;

            tabBar.Init(LayerLabels, OnLayerTabSelected, defaultIndex: 0);
            _tabBarInited = true;
        }

        private void OnLayerTabSelected(int index)
        {
            _currentLayer = (MissionLayer)Mathf.Clamp(index, 0, 3);
            RefreshLayer(_currentLayer);
        }

        private void RefreshLayer(MissionLayer layer)
        {
            bool isSeason = layer == MissionLayer.Seasonal;
            bool hasProvider = _provider != null;

            _buffer.Clear();
            if (hasProvider)
                _provider.GetMissions(layer, _buffer);

            bool showSeasonEmpty = isSeason && (!hasProvider || _buffer.Count == 0);
            if (seasonEmptyRoot != null)
                seasonEmptyRoot.SetActive(showSeasonEmpty);
            if (seasonEmptyLabel != null && showSeasonEmpty)
            {
                seasonEmptyLabel.text = "La saison arrive bientôt";
                seasonEmptyLabel.color = UiTheme.TextMuted;
                seasonEmptyLabel.fontSize = UiTypography.Caption;
            }

            if (missionScroll != null)
                missionScroll.gameObject.SetActive(!showSeasonEmpty);

            // Ligne bonus conditionnelle (Daily/Weekly si exposée)
            bool hasBonus = false;
            MissionUiEntry bonus = default;
            if (hasProvider && !showSeasonEmpty)
                hasBonus = _provider.TryGetLayerBonus(layer, out bonus);

            if (layerBonusRoot != null)
                layerBonusRoot.gameObject.SetActive(hasBonus);
            if (hasBonus && layerBonusEntry != null)
                layerBonusEntry.Bind(bonus, OnClaimRequested);

            ClearSpawned();
            if (showSeasonEmpty || entryTemplate == null || listContent == null)
                return;

            for (int i = 0; i < _buffer.Count; i++)
            {
                MissionEntryUI row = Instantiate(entryTemplate, listContent);
                row.gameObject.SetActive(true);
                row.name = "MissionEntry_" + i;
                row.Bind(_buffer[i], OnClaimRequested);
                _spawned.Add(row);
            }
        }

        private void RefreshNavBadge()
        {
            if (navBar == null)
                navBar = FindObjectOfType<HubNavBarUI>();

            if (navBar == null)
                return;

            bool visible = _provider != null && _provider.HasAnyClaimable();
            navBar.SetBadge(NavTabId, visible);
        }

        private void OnClaimRequested(string missionId)
        {
            if (_provider == null || string.IsNullOrEmpty(missionId))
                return;

            // Claim → OnMissionsChanged (refresh liste) + AddTals → OnDataChanged (header).
            if (_provider.TryClaim(missionId))
                Refresh();
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(_spawned[i].gameObject);
                else
                    DestroyImmediate(_spawned[i].gameObject);
            }

            _spawned.Clear();
        }
    }
}
