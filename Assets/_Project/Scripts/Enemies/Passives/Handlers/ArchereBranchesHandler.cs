using System.Collections;
using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Archère Précise (archere_branches) — zone persistante + branche A Pluie / B Perforante (R6/R7).
    /// Gabarit handlers Fixe U1. Params : value=DEF debuff, sv1=rayon, sv2=dégâts pluie, sv3=dégâts perforante.
    /// </summary>
    public class ArchereBranchesHandler : EnemyPassiveHandlerBase, IEnemyIntentProvider
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        /// <summary> Teinte ambre lisible de la zone persistante (contour R7). </summary>
        private static readonly Color ZoneTintAmber = new Color(1f, 0.55f, 0.15f, 1f);

        /// <summary> Couleur du trait perforant (gag flèches semi-transparentes). </summary>
        private static readonly Color PerforanteLineColor = new Color(1f, 0.92f, 0.55f, 0.72f);

        private const string BUFF_ID_PERFO = "archere_perfo";
        private const int RAIN_ARROW_COUNT = 12;
        private const float RAIN_DURATION = 0.60f;
        private const float PERFO_FLASH_DURATION = 0.15f;
        private const int ARROW_PX_W = 3;
        private const int ARROW_PX_H = 12;
        private const int ARROW_SORTING_ORDER = 14;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private GroundZone _zone;
        private readonly List<CharacterBall> _alliesInZone = new List<CharacterBall>(8);
        private readonly Vector2[] _rainStarts = new Vector2[RAIN_ARROW_COUNT];
        private readonly Vector2[] _rainLands = new Vector2[RAIN_ARROW_COUNT];
        private readonly float[] _rainDelays = new float[RAIN_ARROW_COUNT];
        private readonly float[] _rainFallDurs = new float[RAIN_ARROW_COUNT];
        private GameObject _vfxRoot;
        private SpriteRenderer[] _arrowRenderers;
        private Sprite _arrowSprite;
        private LineRenderer _perfoLine;
        private bool _subscribedDeath;
        private bool _released;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "archere_branches";

        // ═══════════════════════════════════════════
        // LIFECYCLE HANDLER
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;

            float radius = data != null ? data.SpecialValue1 : 2.5f;
            Vector2 pos = owner != null ? (Vector2)owner.transform.position : Vector2.zero;
            _zone = GroundZoneSystem.CreateZone(
                owner,
                ZoneKind.Persistent,
                ZoneShape.Circle,
                new Vector2(radius, radius),
                pos,
                ZoneTintAmber);

            EnsureVfxRoot();
            EnemyFixedTurnActionRegistry.Register(owner, ExecuteTurnAction);
            EnemyIntentSystem.RegisterProvider(owner, this);

            if (owner != null && !_subscribedDeath)
            {
                owner.OnDeath += OnOwnerDeath;
                _subscribedDeath = true;
            }
        }

        public override void Cleanup()
        {
            ReleaseAllResources();
            base.Cleanup();
        }

        public override void ResetForNewStage()
        {
            // Zone et registres sont recréés via Cleanup → Initialize du runtime.
        }

        // ═══════════════════════════════════════════
        // IEnemyIntentProvider
        // ═══════════════════════════════════════════

        public bool TryGetIntent(out EnemyIntent intent)
        {
            intent = default;
            if (!IsReady || _zone == null)
                return false;

            // Suivi knockback : la zone suit l'Archère à chaque lecture d'intent (état vivant R6).
            SyncZoneToOwner();

            if (IsRainBranch())
            {
                intent.Kind = EnemyIntentKind.Zone;
                intent.Target = null;
                intent.IconText = "v";
                intent.ShortLabel = "Pluie de flèches (zone)";
                return true;
            }

            CharacterBall target = ResolvePerforanteTarget();
            intent.Kind = EnemyIntentKind.Projectile;
            intent.Target = target;
            intent.IconText = "→";
            string nom = target != null ? target.Name : "?";
            intent.ShortLabel = "Flèche perforante → " + nom;
            return true;
        }

        public void OnTelegraphStateChanged(bool isTelegraphing)
        {
            if (_zone != null)
                _zone.SetHighlighted(isTelegraphing);
        }

        // ═══════════════════════════════════════════
        // ACTION DE TOUR (Fixed registry)
        // ═══════════════════════════════════════════

        private IEnumerator ExecuteTurnAction()
        {
            if (!IsReady || _data == null)
                yield break;

            SyncZoneToOwner();

            if (IsRainBranch())
                yield return RainRoutine();
            else
                yield return PerforanteRoutine();
        }

        // ═══════════════════════════════════════════
        // BRANCHE
        // ═══════════════════════════════════════════

        /// <summary>
        /// A = au moins un allié dans la zone ; B = zone vide.
        /// Réévaluée à chaque lecture d'intent ET à l'exécution (R6).
        /// </summary>
        private bool IsRainBranch()
        {
            if (_zone == null)
                return false;
            return _zone.GetAlliesInside(_alliesInZone) > 0;
        }

        private CharacterBall ResolvePerforanteTarget()
        {
            if (_owner == null || _owner.Data == null || _turnManager == null)
                return null;

            return TargetSelectorResolver.Resolve(
                _owner.Data.TargetSelector,
                _owner.transform.position,
                _turnManager.GetAllies());
        }

        // ═══════════════════════════════════════════
        // PLUIE (branche A)
        // ═══════════════════════════════════════════

        private IEnumerator RainRoutine()
        {
            EnsureVfxRoot();
            float radius = _data.SpecialValue1;
            Vector2 center = _owner.transform.position;
            int dmg = Mathf.RoundToInt(_data.SpecialValue2);

            // Prépare départs / atterrissages (stagger sur ~RAIN_DURATION, budget tour ~1 s).
            for (int i = 0; i < RAIN_ARROW_COUNT; i++)
            {
                float ang = (i / (float)RAIN_ARROW_COUNT) * Mathf.PI * 2f;
                float dist = radius * (0.25f + 0.55f * ((i % 5) / 4f));
                _rainLands[i] = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                _rainStarts[i] = _rainLands[i] + Vector2.up * (1.4f + 0.15f * (i % 3));
                _rainDelays[i] = (i / (float)RAIN_ARROW_COUNT) * 0.35f;
                _rainFallDurs[i] = 0.28f + 0.12f * ((i % 3) / 2f);

                if (_arrowRenderers != null && i < _arrowRenderers.Length && _arrowRenderers[i] != null)
                {
                    SpriteRenderer sr = _arrowRenderers[i];
                    float a = 0.45f + 0.25f * Mathf.Sin(i * 1.7f);
                    sr.color = new Color(1f, 0.95f, 0.7f, a);
                    sr.transform.position = _rainStarts[i];
                    sr.enabled = false;
                }
            }

            float elapsed = 0f;
            float total = RAIN_DURATION;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < RAIN_ARROW_COUNT; i++)
                {
                    if (_arrowRenderers == null || i >= _arrowRenderers.Length)
                        continue;
                    SpriteRenderer sr = _arrowRenderers[i];
                    if (sr == null)
                        continue;

                    float local = elapsed - _rainDelays[i];
                    if (local < 0f)
                        continue;

                    sr.enabled = true;
                    float k = Mathf.Clamp01(local / _rainFallDurs[i]);
                    sr.transform.position = Vector2.Lerp(_rainStarts[i], _rainLands[i], k * k);
                }

                yield return null;
            }

            if (_zone != null)
            {
                int n = _zone.GetAlliesInside(_alliesInZone);
                for (int i = 0; i < n; i++)
                {
                    CharacterBall ally = _alliesInZone[i];
                    if (ally != null && !ally.IsDead && dmg > 0)
                        ally.TakeDamage(dmg);
                }
            }

            HideArrows();
        }

        // ═══════════════════════════════════════════
        // PERFORANTE (branche B)
        // ═══════════════════════════════════════════

        private IEnumerator PerforanteRoutine()
        {
            CharacterBall target = ResolvePerforanteTarget();
            if (target == null || target.IsDead)
                yield break;

            EnsureVfxRoot();
            Vector3 from = _owner.transform.position;
            Vector3 to = target.transform.position;

            if (_perfoLine != null)
            {
                _perfoLine.enabled = true;
                _perfoLine.SetPosition(0, from);
                _perfoLine.SetPosition(1, to);
                _perfoLine.startColor = PerforanteLineColor;
                _perfoLine.endColor = PerforanteLineColor;
            }

            float elapsed = 0f;
            while (elapsed < PERFO_FLASH_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                if (_perfoLine != null)
                {
                    float a = 1f - Mathf.Clamp01(elapsed / PERFO_FLASH_DURATION);
                    Color c = PerforanteLineColor;
                    c.a = PerforanteLineColor.a * a;
                    _perfoLine.startColor = c;
                    _perfoLine.endColor = c;
                }

                yield return null;
            }

            if (_perfoLine != null)
                _perfoLine.enabled = false;

            int dmg = Mathf.RoundToInt(_data.SpecialValue3);
            if (dmg > 0)
                target.TakeDamage(dmg);

            ApplyPerforanteDebuff(target);
        }

        private void ApplyPerforanteDebuff(CharacterBall target)
        {
            if (target == null || target.BuffReceiver == null || _data == null)
                return;

            target.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BUFF_ID_PERFO,
                Source = null,
                EnemySource = _owner,
                StatType = BuffStatType.DEF,
                Value = -Mathf.Abs(_data.Value),
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniqueGlobal = true,
                UniquePerSource = false,
                ExpiresWithSource = _data.ExpiresWithSource
            });
        }

        // ═══════════════════════════════════════════
        // VFX / POOL
        // ═══════════════════════════════════════════

        private void EnsureVfxRoot()
        {
            if (_vfxRoot != null)
                return;

            _vfxRoot = new GameObject("ArchereBranches_VFX");
            if (_owner != null)
                _vfxRoot.transform.SetParent(_owner.transform, false);

            _arrowSprite = BuildArrowPlaceholderSprite();
            _arrowRenderers = new SpriteRenderer[RAIN_ARROW_COUNT];
            for (int i = 0; i < RAIN_ARROW_COUNT; i++)
            {
                var go = new GameObject("Arrow_" + i);
                go.transform.SetParent(_vfxRoot.transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _arrowSprite;
                sr.sortingOrder = ARROW_SORTING_ORDER;
                sr.enabled = false;
                // ~3×12 px world : PPU 32 → taille lisible.
                go.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
                _arrowRenderers[i] = sr;
            }

            var lineGo = new GameObject("PerfoLine");
            lineGo.transform.SetParent(_vfxRoot.transform, false);
            _perfoLine = lineGo.AddComponent<LineRenderer>();
            _perfoLine.positionCount = 2;
            _perfoLine.startWidth = 0.05f;
            _perfoLine.endWidth = 0.02f;
            Shader lineShader = Shader.Find("Sprites/Default");
            if (lineShader == null)
                lineShader = Shader.Find("Unlit/Color");
            if (lineShader != null)
                _perfoLine.material = new Material(lineShader);
            _perfoLine.sortingOrder = ARROW_SORTING_ORDER + 1;
            _perfoLine.enabled = false;
            _perfoLine.useWorldSpace = true;
        }

        private static Sprite BuildArrowPlaceholderSprite()
        {
            var tex = new Texture2D(ARROW_PX_W, ARROW_PX_H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color32(0, 0, 0, 0);
            var tip = new Color32(255, 240, 180, 200);
            var shaft = new Color32(255, 250, 210, 160);
            for (int y = 0; y < ARROW_PX_H; y++)
            {
                for (int x = 0; x < ARROW_PX_W; x++)
                {
                    if (y >= ARROW_PX_H - 2)
                        tex.SetPixel(x, y, tip);
                    else if (x == ARROW_PX_W / 2)
                        tex.SetPixel(x, y, shaft);
                    else
                        tex.SetPixel(x, y, clear);
                }
            }

            tex.Apply(false, false);
            return Sprite.Create(
                tex,
                new Rect(0, 0, ARROW_PX_W, ARROW_PX_H),
                new Vector2(0.5f, 0f),
                32f);
        }

        private void HideArrows()
        {
            if (_arrowRenderers == null)
                return;
            for (int i = 0; i < _arrowRenderers.Length; i++)
            {
                if (_arrowRenderers[i] != null)
                    _arrowRenderers[i].enabled = false;
            }
        }

        private void SyncZoneToOwner()
        {
            if (_zone == null || _owner == null)
                return;
            _zone.SetWorldPosition(_owner.transform.position);
        }

        // ═══════════════════════════════════════════
        // HYGIÈNE MORT / RELEASE
        // ═══════════════════════════════════════════

        private void OnOwnerDeath()
        {
            ReleaseAllResources();
        }

        private void ReleaseAllResources()
        {
            if (_released)
                return;
            _released = true;

            if (_owner != null && _subscribedDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedDeath = false;
            }

            EnemyFixedTurnActionRegistry.Unregister(_owner);
            EnemyIntentSystem.UnregisterProvider(_owner);

            if (_zone != null)
            {
                GroundZoneSystem.ReleaseZone(_zone);
                _zone = null;
            }

            if (_vfxRoot != null)
            {
                Object.Destroy(_vfxRoot);
                _vfxRoot = null;
            }

            _arrowRenderers = null;
            _perfoLine = null;
            if (_arrowSprite != null)
            {
                if (_arrowSprite.texture != null)
                    Object.Destroy(_arrowSprite.texture);
                Object.Destroy(_arrowSprite);
                _arrowSprite = null;
            }
        }
    }
}
