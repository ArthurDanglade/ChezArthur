using System.Collections;
using ChezArthur.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.RevealStage
{
    /// <summary>
    /// Harness éditeur — valide le stage sans toucher au gacha. Aucune scène commitée.
    /// </summary>
    public class RevealStageDevHarness : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Assets INVR")]
        [SerializeField] private RevealStageConfig config;
        [SerializeField] private Material revealLightMaterial;
        [SerializeField] private CharacterDatabase characterDatabase;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Canvas _canvas;
        private GameObject _stageRoot;
        private RawImage _rawImage;
        private CharacterArtworkView _artworkView;
        private RevealStageDirector _director;
        private RevealInfoPanel _infoPanel;
        private Coroutine _routine;
        private CharacterData _staticSr;
        private CharacterData _animated;

        // ═══════════════════════════════════════════
        // CONTEXT MENUS
        // ═══════════════════════════════════════════

        [ContextMenu("Arrival SR")]
        private void CtxArrivalSr() => Run(ArrivalRoutine(CharacterRarity.SR, false));

        [ContextMenu("Arrival SSR (éveil au snap)")]
        private void CtxArrivalSsr() => Run(ArrivalRoutine(CharacterRarity.SSR, false));

        [ContextMenu("Arrival LR")]
        private void CtxArrivalLr() => Run(ArrivalRoutine(CharacterRarity.LR, false));

        [ContextMenu("Arrival LR fakeout")]
        private void CtxArrivalLrFake() => Run(ArrivalRoutine(CharacterRarity.LR, true));

        [ContextMenu("Info nouveau")]
        private void CtxInfoNew() => Run(InfoRoutine(isNew: true, isMax: false));

        [ContextMenu("Info doublon (cascade)")]
        private void CtxInfoDup() => Run(InfoRoutine(isNew: false, isMax: false));

        [ContextMenu("Enchaînement ×3 (sortie→entrée chevauchées)")]
        private void CtxChain() => Run(ChainRoutine());

        [ContextMenu("Nettoyer")]
        private void CtxCleanup()
        {
            StopActive();
            if (_director != null)
                _director.ResetVisuals();
            if (_infoPanel != null)
                _infoPanel.HideImmediate();
            if (_stageRoot != null)
            {
                if (Application.isPlaying)
                    Destroy(_stageRoot);
                else
                    DestroyImmediate(_stageRoot);
                _stageRoot = null;
                _rawImage = null;
                _artworkView = null;
                _director = null;
                _infoPanel = null;
            }
        }

        // ═══════════════════════════════════════════
        // ROUTINES
        // ═══════════════════════════════════════════

        private IEnumerator ArrivalRoutine(CharacterRarity rarity, bool fakeout)
        {
            EnsureStage();
            CharacterData data = PickCharacter(rarity);
            if (data == null)
            {
                Debug.LogWarning("[RevealStageDevHarness] Aucun personnage pour " + rarity);
                yield break;
            }

            bool animated = data.AnimatedPortraitPrime != null;
            ShowArtwork(data, pauseAnim: animated);

            Vector2 focal = data.portraitFocalPoint;
            yield return _director.CoPlayArrival(
                rarity,
                fakeout,
                focal,
                onSnap: () =>
                {
                    if (animated && _artworkView != null)
                        _artworkView.SetAnimationPaused(false);
                });

            // Matériau final null (artwork brut)
            _director.ResetVisuals();
        }

        private IEnumerator InfoRoutine(bool isNew, bool isMax)
        {
            EnsureStage();
            CharacterData data = PickCharacter(CharacterRarity.SSR) ?? PickCharacter(CharacterRarity.SR);
            if (data == null)
                yield break;

            ShowArtwork(data, pauseAnim: false);
            _director.ResetVisuals();

            var payload = new RevealInfoPanel.Payload
            {
                name = data.CharacterName,
                rarity = data.Rarity,
                isNew = isNew,
                prevLevel = isMax ? 60 : 12,
                newLevel = isMax ? 60 : 15,
                isMax = isMax,
                statDeltas = isNew || isMax
                    ? null
                    : new (string, int)[]
                    {
                        ("HP", 40),
                        ("ATK", 12),
                        ("DEF", 8),
                        ("SPD", 3)
                    }
            };

            yield return _infoPanel.CoPlay(payload);
        }

        private IEnumerator ChainRoutine()
        {
            EnsureStage();
            CharacterRarity[] rarities =
            {
                CharacterRarity.SR,
                CharacterRarity.SSR,
                CharacterRarity.LR
            };

            float overlap = config != null ? config.entryOverlap : 0.15f;
            float exitDur = config != null ? config.exitDim : 0.28f;

            for (int i = 0; i < rarities.Length; i++)
            {
                CharacterData data = PickCharacter(rarities[i]);
                if (data == null)
                    continue;

                bool animated = data.AnimatedPortraitPrime != null;
                ShowArtwork(data, pauseAnim: animated);

                yield return _director.CoPlayArrival(
                    rarities[i],
                    fakeout: false,
                    data.portraitFocalPoint,
                    onSnap: () =>
                    {
                        if (animated && _artworkView != null)
                            _artworkView.SetAnimationPaused(false);
                    });

                if (i >= rarities.Length - 1)
                    break;

                // Sortie chevauchée : prochaine entrée naît avant la fin (entryOverlap)
                StartCoroutine(_director.CoPlayExit());
                float wait = Mathf.Max(0f, exitDur - overlap);
                float t = 0f;
                while (t < wait)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            _director.ResetVisuals();
        }

        // ═══════════════════════════════════════════
        // STAGE RUNTIME
        // ═══════════════════════════════════════════

        private void EnsureStage()
        {
            ResolveAssets();
            PickCharactersFromDb();

            if (_stageRoot != null)
                return;

            // Canvas overlay au-dessus du Hub
            GameObject canvasGo = new GameObject(
                "RevealStage_DEV_Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 5000;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            _stageRoot = new GameObject("RevealStage_DEV", typeof(RectTransform));
            _stageRoot.transform.SetParent(canvasGo.transform, false);
            StretchFull(_stageRoot.GetComponent<RectTransform>());

            // Backdrop charcoal
            GameObject bgGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_stageRoot.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bg = bgGo.GetComponent<Image>();
            bg.color = UiTheme.GachaStageCharcoal;
            bg.raycastTarget = false;

            // RawImage plein écran
            GameObject artGo = new GameObject("Artwork", typeof(RectTransform), typeof(RawImage));
            artGo.transform.SetParent(_stageRoot.transform, false);
            StretchFull(artGo.GetComponent<RectTransform>());
            _rawImage = artGo.GetComponent<RawImage>();
            _rawImage.color = Color.white;
            _rawImage.raycastTarget = false;

            _artworkView = artGo.AddComponent<CharacterArtworkView>();
            _artworkView.Configure(_rawImage);

            GameObject dirGo = new GameObject("Director", typeof(RectTransform));
            dirGo.transform.SetParent(_stageRoot.transform, false);
            _director = dirGo.AddComponent<RevealStageDirector>();
            _director.Wire(config, revealLightMaterial);
            _director.Bind(_rawImage, _artworkView);

            _infoPanel = RevealInfoPanel.EnsureUnder(_stageRoot.transform);
            _infoPanel.Configure(config);
            _infoPanel.BindFx(_director.Fx);
            _infoPanel.HideImmediate();

            // Garder canvasGo comme racine pour Nettoyer
            _stageRoot = canvasGo;
        }

        private void ShowArtwork(CharacterData data, bool pauseAnim)
        {
            if (_artworkView == null || data == null)
                return;

            if (data.AnimatedPortraitPrime != null)
                _artworkView.ShowState(data, data.AnimatedPortraitPrime);
            else
                _artworkView.Show(data);

            _artworkView.ForceCoverMode();
            _artworkView.SetAnimationPaused(pauseAnim);

            if (_director != null)
            {
                _director.Bind(_rawImage, _artworkView);
                _director.ArmDark();
            }
        }

        private CharacterData PickCharacter(CharacterRarity rarity)
        {
            if (rarity == CharacterRarity.SR && _staticSr != null)
                return _staticSr;
            if (rarity != CharacterRarity.SR && _animated != null)
            {
                if (_animated.Rarity == rarity)
                    return _animated;
            }

            if (characterDatabase == null)
                return null;

            var list = characterDatabase.GetByRarity(rarity);
            if (list == null || list.Count == 0)
                return null;

            for (int i = 0; i < list.Count; i++)
            {
                CharacterData c = list[i];
                if (c == null) continue;
                if (rarity == CharacterRarity.SR && c.AnimatedPortraitPrime == null)
                    return c;
                if (rarity != CharacterRarity.SR && c.AnimatedPortraitPrime != null)
                    return c;
            }

            return list[0];
        }

        private void PickCharactersFromDb()
        {
            if (characterDatabase == null)
            {
                CharacterDatabase[] loaded = Resources.FindObjectsOfTypeAll<CharacterDatabase>();
                if (loaded != null && loaded.Length > 0)
                    characterDatabase = loaded[0];
            }

            if (characterDatabase == null || characterDatabase.AllCharacters == null)
                return;

            var all = characterDatabase.AllCharacters;
            for (int i = 0; i < all.Count; i++)
            {
                CharacterData c = all[i];
                if (c == null) continue;
                if (_animated == null && c.AnimatedPortraitPrime != null)
                    _animated = c;
                if (_staticSr == null && c.AnimatedPortraitPrime == null && c.Rarity == CharacterRarity.SR)
                    _staticSr = c;
                if (_animated != null && _staticSr != null)
                    break;
            }
        }

        private void ResolveAssets()
        {
#if UNITY_EDITOR
            if (config == null)
            {
                config = UnityEditor.AssetDatabase.LoadAssetAtPath<RevealStageConfig>(
                    "Assets/_Project/Data/UI/RevealStageConfig.asset");
            }

            if (revealLightMaterial == null)
            {
                revealLightMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Project/Art/FX/RevealLight.mat");
            }
#endif
        }

        private void Run(IEnumerator routine)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[RevealStageDevHarness] Lancer le Play Mode pour les ContextMenus.");
                return;
            }

            StopActive();
            _routine = StartCoroutine(Wrap(routine));
        }

        private IEnumerator Wrap(IEnumerator inner)
        {
            yield return inner;
            _routine = null;
        }

        private void StopActive()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private void OnDestroy()
        {
            StopActive();
        }
    }
}
