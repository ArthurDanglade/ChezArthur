using System.Collections;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Confesseur — Lien de Confession (confesseur_lien), fiche 5.4.
    ///
    /// Architecture : le passif data est OnStageStart / SpecialHandler — le runtime
    /// ne dispatche PAS OnTakeDamage / OnAllyHealed vers ce handler. Abonnements
    /// DIRECTS : owner.OnDamaged (transfert), connected.OnHealed (rétro-soin),
    /// connected.OnDeath (re-sélection). Désabonnement aux trois points : Cleanup,
    /// mort du propriétaire, re-sélection (ancien connecté libéré avant le nouveau).
    ///
    /// Params : value=fraction transfert, sv1=fraction rétro-soin, sv2=dégâts Sermon.
    /// Transfert = par hit (R5 ne s'applique pas — flux continu, commentaire acté).
    /// </summary>
    public class ConfesseurLienHandler : EnemyPassiveHandlerBase, IEnemyIntentProvider
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const float SERMON_FLIGHT = 0.40f;
        private const int SERMON_SORTING = 14;
        private const int SERMON_PX = 8;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private CharacterBall _connected;
        private LinkBeamView _beam;
        private GameObject _beamGo;
        private GameObject _sermonVfxGo;
        private SpriteRenderer _sermonSprite;
        private Sprite _sermonSpriteAsset;
        private bool _subscribedOwnerDeath;
        private bool _subscribedOwnerDamaged;
        private bool _subscribedConnected;
        private bool _released;
        private bool _telegraphing;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "confesseur_lien";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;

            BindOwnerDamage();
            BindOwnerDeath();

            EnemyFixedTurnActionRegistry.Register(owner, SermonAction);
            EnemyIntentSystem.RegisterProvider(owner, this);

            EnsureBeam();
            SelectConnected(flash: false);
        }

        public override void Cleanup()
        {
            ReleaseAllResources();
            base.Cleanup();
        }

        /// <summary>
        /// Exception R4 actée (fiche 5.4) : le lien est fixé par PERSONNAGE, pas par spé.
        /// Switcher la spé du connecté ne déplace jamais le lien — seule sa mort le déplace.
        /// (Sinon le lien se contournerait d'un tap.)
        /// </summary>
        public override void OnAllySpecSwitched(CharacterBall ally) { }

        // ═══════════════════════════════════════════
        // IEnemyIntentProvider
        // ═══════════════════════════════════════════

        public bool TryGetIntent(out EnemyIntent intent)
        {
            intent = default;
            if (!IsReady)
                return false;

            if (_connected == null || _connected.IsDead)
            {
                intent.Kind = EnemyIntentKind.Special;
                intent.Target = null;
                intent.IconText = "…";
                intent.ShortLabel = "Lien dormant";
                return true;
            }

            intent.Kind = EnemyIntentKind.Projectile;
            intent.Target = _connected;
            intent.IconText = "✝";
            intent.ShortLabel = "Sermon → " + _connected.Name;
            return true;
        }

        public void OnTelegraphStateChanged(bool isTelegraphing)
        {
            _telegraphing = isTelegraphing;
            if (_beam != null)
                _beam.SetIntensity(isTelegraphing);
        }

        // ═══════════════════════════════════════════
        // SÉLECTION / LIEN
        // ═══════════════════════════════════════════

        private void SelectConnected(bool flash)
        {
            UnbindConnected();

            CharacterBall next = ResolveConnectedCandidate();
            _connected = next;

            if (_connected == null)
            {
                if (_beam != null)
                    _beam.SetVisible(false);
                EnemyIntentSystem.RequestRefresh();
                return;
            }

            BindConnected();
            EnsureBeam();
            if (_beam != null)
            {
                _beam.SetTint(CombatFeedbackPalette.LienTransfert);
                _beam.SetEndpoints(_owner != null ? _owner.transform : null, _connected.transform);
                // Flash de re-sélection (cue SFX cloche listé, non implémenté) + intensité télégraphe.
                _beam.SetIntensity(_telegraphing || flash);
                _beam.SetVisible(true);
            }

            EnemyIntentSystem.RequestRefresh();
        }

        private CharacterBall ResolveConnectedCandidate()
        {
            if (_owner == null || _owner.Data == null || _turnManager == null)
                return null;

            return TargetSelectorResolver.Resolve(
                _owner.Data.TargetSelector,
                _owner.transform.position,
                _turnManager.GetAllies());
        }

        // ═══════════════════════════════════════════
        // TRANSFERT / RÉTRO-SOIN / MORT CONNECTÉ
        // ═══════════════════════════════════════════

        private void OnOwnerDamaged(int dmg)
        {
            if (!IsReady || _data == null || dmg <= 0)
                return;
            if (_connected == null || _connected.IsDead)
                return;

            int transfert = Mathf.RoundToInt(dmg * _data.Value);
            if (transfert <= 0)
                return;

            // Par hit, par nature (transfert continu — R5 ne s'applique pas).
            _connected.SuppressNextDamagePopup();
            _connected.TakeNonLethalDamage(transfert);

            FloatingNumberSpawner.Instance?.ShowLabel(
                "-" + transfert,
                CombatFeedbackPalette.LienTransfert,
                _connected.transform.position,
                1f);
        }

        private void OnConnectedHealed(int amount)
        {
            if (!IsReady || _data == null || amount <= 0)
                return;
            if (_owner == null || _owner.IsDead)
                return;

            int retro = Mathf.RoundToInt(amount * _data.SpecialValue1);
            if (retro <= 0)
                return;

            // Aucune boucle : on soigne le Confesseur, jamais le connecté.
            _owner.Heal(retro);
            FloatingNumberSpawner.Instance?.ShowLabel(
                "+" + retro,
                CombatFeedbackPalette.LienRetroSoin,
                _owner.transform.position,
                1f);
        }

        private void OnConnectedDeath()
        {
            if (_released)
                return;

            SelectConnected(flash: true);
            if (_beam != null && _connected != null)
            {
                // Flash bref d'intensité à la re-sélection.
                _beam.SetIntensity(true);
            }
        }

        // ═══════════════════════════════════════════
        // SERMON (Fixed registry)
        // ═══════════════════════════════════════════

        private IEnumerator SermonAction()
        {
            if (!IsReady || _data == null)
                yield break;

            CharacterBall target = _connected;
            if (target == null || target.IsDead)
            {
                // Edge : lien dormant — pose brève, fin de tour.
                float wait = 0.25f;
                float t = 0f;
                while (t < wait)
                {
                    t += Time.unscaledDeltaTime;
                    yield return null;
                }

                yield break;
            }

            EnsureSermonVfx();
            Vector3 from = _owner.transform.position;
            Vector3 to = target.transform.position;
            if (_sermonSprite != null)
            {
                _sermonSprite.enabled = true;
                _sermonSprite.transform.position = from;
                Color c = CombatFeedbackPalette.LienTransfert;
                c.a = 0.85f;
                _sermonSprite.color = c;
            }

            float elapsed = 0f;
            while (elapsed < SERMON_FLIGHT)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / SERMON_FLIGHT);
                if (_sermonSprite != null)
                    _sermonSprite.transform.position = Vector3.Lerp(from, to, k * k);
                yield return null;
            }

            if (_sermonSprite != null)
                _sermonSprite.enabled = false;

            // Canal NORMAL : mitigé, PEUT tuer — télégraphié « Sermon → {nom} » (D17).
            int dmg = Mathf.RoundToInt(_data.SpecialValue2);
            if (dmg > 0 && target != null && !target.IsDead)
                target.TakeDamage(dmg);
        }

        // ═══════════════════════════════════════════
        // VFX
        // ═══════════════════════════════════════════

        private void EnsureBeam()
        {
            if (_beam != null || _owner == null)
                return;

            _beamGo = new GameObject("Confesseur_LinkBeam");
            _beamGo.transform.SetParent(_owner.transform, false);
            _beam = _beamGo.AddComponent<LinkBeamView>();
            _beam.SetTint(CombatFeedbackPalette.LienTransfert);
        }

        private void EnsureSermonVfx()
        {
            if (_sermonSprite != null || _owner == null)
                return;

            _sermonVfxGo = new GameObject("Confesseur_SermonBolt");
            _sermonVfxGo.transform.SetParent(_owner.transform, false);
            _sermonSprite = _sermonVfxGo.AddComponent<SpriteRenderer>();
            _sermonSprite.sortingOrder = SERMON_SORTING;
            _sermonSprite.sprite = BuildSermonPlaceholderSprite();
            _sermonSprite.enabled = false;
            _sermonVfxGo.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
            _sermonSpriteAsset = _sermonSprite.sprite;
        }

        private static Sprite BuildSermonPlaceholderSprite()
        {
            var tex = new Texture2D(SERMON_PX, SERMON_PX, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color32(0, 0, 0, 0);
            var core = new Color32(220, 100, 140, 220);
            for (int y = 0; y < SERMON_PX; y++)
            {
                for (int x = 0; x < SERMON_PX; x++)
                {
                    int dx = x - SERMON_PX / 2;
                    int dy = y - SERMON_PX / 2;
                    tex.SetPixel(x, y, (dx * dx + dy * dy) <= 9 ? core : clear);
                }
            }

            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, SERMON_PX, SERMON_PX), new Vector2(0.5f, 0.5f), 32f);
        }

        // ═══════════════════════════════════════════
        // ABONNEMENTS
        // ═══════════════════════════════════════════

        private void BindOwnerDamage()
        {
            if (_owner == null || _subscribedOwnerDamaged)
                return;
            _owner.OnDamaged += OnOwnerDamaged;
            _subscribedOwnerDamaged = true;
        }

        private void UnbindOwnerDamage()
        {
            if (_owner != null && _subscribedOwnerDamaged)
            {
                _owner.OnDamaged -= OnOwnerDamaged;
                _subscribedOwnerDamaged = false;
            }
        }

        private void BindOwnerDeath()
        {
            if (_owner == null || _subscribedOwnerDeath)
                return;
            _owner.OnDeath += OnOwnerDeath;
            _subscribedOwnerDeath = true;
        }

        private void UnbindOwnerDeath()
        {
            if (_owner != null && _subscribedOwnerDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedOwnerDeath = false;
            }
        }

        private void BindConnected()
        {
            if (_connected == null || _subscribedConnected)
                return;
            _connected.OnDeath += OnConnectedDeath;
            _connected.OnHealed += OnConnectedHealed;
            _subscribedConnected = true;
        }

        private void UnbindConnected()
        {
            if (_connected != null && _subscribedConnected)
            {
                _connected.OnDeath -= OnConnectedDeath;
                _connected.OnHealed -= OnConnectedHealed;
                _subscribedConnected = false;
            }

            _connected = null;
        }

        private void OnOwnerDeath()
        {
            ReleaseAllResources();
        }

        // ═══════════════════════════════════════════
        // RELEASE
        // ═══════════════════════════════════════════

        private void ReleaseAllResources()
        {
            if (_released)
                return;
            _released = true;

            UnbindConnected();
            UnbindOwnerDamage();
            UnbindOwnerDeath();

            EnemyFixedTurnActionRegistry.Unregister(_owner);
            EnemyIntentSystem.UnregisterProvider(_owner);

            if (_beamGo != null)
            {
                Object.Destroy(_beamGo);
                _beamGo = null;
                _beam = null;
            }

            if (_sermonVfxGo != null)
            {
                Object.Destroy(_sermonVfxGo);
                _sermonVfxGo = null;
                _sermonSprite = null;
            }

            if (_sermonSpriteAsset != null)
            {
                if (_sermonSpriteAsset.texture != null)
                    Object.Destroy(_sermonSpriteAsset.texture);
                Object.Destroy(_sermonSpriteAsset);
                _sermonSpriteAsset = null;
            }
        }
    }
}
