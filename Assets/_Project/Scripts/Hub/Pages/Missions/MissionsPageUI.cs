using System.Collections.Generic;
using ChezArthur.Missions;
using ChezArthur.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.Hub.Pages.Missions
{
    /// <summary>
    /// Page Hub Missions — TabBar compacte à icônes, bonus, liste, FX claim Tals.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MissionsPageUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string NavTabId = "missions";
        private const float TabBarFixedHeight = 108f;

        private static readonly string[] LayerLabels =
        {
            "Quotidien", "Hebdo", "Saison", "Permanent"
        };

        /// <summary> Glyphes placeholder (en attendant Dharu) — lettres ASCII sûres TMP. </summary>
        private static readonly string[] LayerGlyphs =
        {
            "Q", "H", "S", "P"
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

        [Header("FX claim")]
        [SerializeField] private TalsClaimFX claimFx;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private IMissionProvider _provider;
        private MissionLayer _currentLayer = MissionLayer.Daily;
        private readonly List<MissionUiEntry> _buffer = new List<MissionUiEntry>(16);
        private readonly List<MissionEntryUI> _spawned = new List<MissionEntryUI>(16);
        private readonly Dictionary<string, MissionEntryUI> _byId =
            new Dictionary<string, MissionEntryUI>(16);
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
            EnsureClaimFx();
        }

        private void OnEnable()
        {
            EnsureProvider();
            EnsureTabBar();
            EnsureClaimFx();

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

        public void SetProvider(IMissionProvider provider)
        {
            if (_provider != null)
                _provider.OnChanged -= Refresh;

            _provider = provider;

            if (isActiveAndEnabled && _provider != null)
                _provider.OnChanged += Refresh;

            Refresh();
        }

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

        private void EnsureClaimFx()
        {
            if (claimFx != null)
                return;

            claimFx = GetComponent<TalsClaimFX>();
            if (claimFx == null)
                claimFx = FindObjectOfType<TalsClaimFX>();
        }

        private void EnsureTabBar()
        {
            if (_tabBarInited || tabBar == null)
                return;

            tabBar.SetFixedItemHeight(TabBarFixedHeight);
            tabBar.Init(LayerLabels, null, LayerGlyphs, OnLayerTabSelected, defaultIndex: 0);
            _tabBarInited = true;

            // Verrouille la hauteur de la barre elle-même (plus d'étirement VLG).
            LayoutElement barLe = tabBar.GetComponent<LayoutElement>();
            if (barLe == null)
                barLe = tabBar.gameObject.AddComponent<LayoutElement>();
            barLe.minHeight = TabBarFixedHeight;
            barLe.preferredHeight = TabBarFixedHeight;
            barLe.flexibleHeight = 0f;
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
            {
                missionScroll.gameObject.SetActive(!showSeasonEmpty);
                LayoutElement scrollLe = missionScroll.GetComponent<LayoutElement>();
                if (scrollLe != null)
                {
                    scrollLe.flexibleHeight = 1f;
                    scrollLe.minHeight = 200f;
                }
            }

            bool hasBonus = false;
            MissionUiEntry bonus = default;
            if (hasProvider && !showSeasonEmpty)
                hasBonus = _provider.TryGetLayerBonus(layer, out bonus);

            if (layerBonusRoot != null)
                layerBonusRoot.gameObject.SetActive(hasBonus);

            _byId.Clear();
            if (hasBonus && layerBonusEntry != null)
            {
                layerBonusEntry.Bind(bonus, OnClaimRequested);
                if (!string.IsNullOrEmpty(bonus.Id))
                    _byId[bonus.Id] = layerBonusEntry;
            }

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
                if (!string.IsNullOrEmpty(_buffer[i].Id))
                    _byId[_buffer[i].Id] = row;
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

            RectTransform fromRt = null;
            int reward = 0;
            if (_byId.TryGetValue(missionId, out MissionEntryUI entry) && entry != null)
            {
                fromRt = entry.transform as RectTransform;
                reward = entry.BoundRewardTals;
            }

            if (!_provider.TryClaim(missionId))
                return;

            EnsureClaimFx();
            if (claimFx != null && fromRt != null && reward > 0)
                claimFx.Play(fromRt, reward);

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
