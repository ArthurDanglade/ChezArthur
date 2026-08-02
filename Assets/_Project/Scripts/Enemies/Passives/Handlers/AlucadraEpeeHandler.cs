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
    /// Alucadra — L'Épée Volante (alucadra_epee), fiche 5.6 phase 1.
    /// Compagnon ciblable hors TurnManager ; garde DR ; Transpercement Fixed + Saignement ;
    /// mort Épée → transformation Mobile (signale alucadra_loup via OnArchetypeChanged).
    /// Params : value=DR, sv1=dégâts transpercement, sv2=Saignement amp, sv3=tours Saignement.
    /// </summary>
    public class AlucadraEpeeHandler : EnemyPassiveHandlerBase, IEnemyIntentProvider
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_GARDE = "alucadra_garde_epee";
        private const string BUFF_SAIGNEMENT = "saignement";
        private const string EPEE_ID = "epee_volante";
        private const float SWORD_OFFSET_X = 1.2f;
        private const float SWORD_OFFSET_Y = 0.6f;
        private const float ARENA_MARGIN = 0.5f;
        private const float DASH_DURATION = 0.50f;
        private const float TRANSFORM_WINDUP = 0.50f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private Enemy _sword;
        private StageGenerator _stageGenerator;
        private bool _subscribedSwordDeath;
        private bool _subscribedOwnerDeath;
        private bool _transformed;
        private bool _released;
        private bool _phase1Active;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "alucadra_epee";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;
            _transformed = false;
            _phase1Active = true;

            _stageGenerator = Object.FindObjectOfType<StageGenerator>();
            SpawnSword();
            ApplyGarde();

            EnemyFixedTurnActionRegistry.Register(owner, ExecuteTranspercement);
            EnemyIntentSystem.RegisterProvider(owner, this);

            if (_sword != null && !_subscribedSwordDeath)
            {
                _sword.OnDeath += OnSwordDeath;
                _subscribedSwordDeath = true;
            }

            if (owner != null && !_subscribedOwnerDeath)
            {
                owner.OnDeath += OnOwnerDeath;
                _subscribedOwnerDeath = true;
            }
        }

        public override void Cleanup()
        {
            ReleaseAllResources();
            base.Cleanup();
        }

        public override void ResetForNewStage()
        {
            // Nouvelle instance attendue par étage — flag de sécurité.
            _transformed = false;
            _phase1Active = true;
        }

        // ═══════════════════════════════════════════
        // IEnemyIntentProvider
        // ═══════════════════════════════════════════

        public bool TryGetIntent(out EnemyIntent intent)
        {
            intent = default;
            if (!IsReady || !_phase1Active || _data == null)
                return false;

            CharacterBall target = ResolveClosestAlly();
            intent.Kind = EnemyIntentKind.Special;
            intent.IconText = "!";
            intent.Target = target;
            intent.ShortLabel = target != null
                ? "Transpercement → " + target.Name
                : "Transpercement";
            return true;
        }

        public void OnTelegraphStateChanged(bool isTelegraphing)
        {
            // Pas de zone persistante — ligne d'aggro gérée par le système d'intent.
        }

        // ═══════════════════════════════════════════
        // SPAWN / GARDE
        // ═══════════════════════════════════════════

        private void SpawnSword()
        {
            if (_owner == null)
                return;

            EnemyData swordData = _stageGenerator != null
                ? _stageGenerator.FindEnemyDataById(EPEE_ID)
                : null;

            if (swordData == null)
            {
                Debug.LogWarning("[alucadra_epee] EnemyData epee_volante introuvable (StageGenerator).");
                return;
            }

            if (MidCombatSpawner.Instance == null)
            {
                Debug.LogWarning("[alucadra_epee] MidCombatSpawner.Instance null — Épée non spawnée.");
                return;
            }

            // Stats data hors échelle (placeholder) — recalibrage G7. Mult 1/1 volontaire.
            Vector3 pos = ClampToArena(
                _owner.transform.position + new Vector3(SWORD_OFFSET_X, SWORD_OFFSET_Y, 0f));

            _sword = MidCombatSpawner.Instance.SpawnCompanion(swordData, pos, 1f, 1f);
            IgnoreCollisionWithOwner(true);
        }

        /// <summary>
        /// L'Épée traverse Alucadra (dash / idle flottant) — zéro poussée physique.
        /// </summary>
        private void IgnoreCollisionWithOwner(bool ignore)
        {
            if (_owner == null || _sword == null)
                return;

            Collider2D ownerCol = _owner.GetComponent<Collider2D>();
            Collider2D swordCol = _sword.GetComponent<Collider2D>();
            if (ownerCol == null || swordCol == null)
                return;

            Physics2D.IgnoreCollision(swordCol, ownerCol, ignore);
        }

        private static Vector3 ClampToArena(Vector3 worldPos)
        {
            Arena arena = Object.FindObjectOfType<Arena>();
            if (arena == null)
                return worldPos;

            Bounds b = arena.Bounds;
            worldPos.x = Mathf.Clamp(worldPos.x, b.min.x + ARENA_MARGIN, b.max.x - ARENA_MARGIN);
            worldPos.y = Mathf.Clamp(worldPos.y, b.min.y + ARENA_MARGIN, b.max.y - ARENA_MARGIN);
            return worldPos;
        }

        private void ApplyGarde()
        {
            if (_owner?.BuffReceiver == null || _data == null)
                return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BUFF_GARDE,
                Source = null,
                EnemySource = null,
                StatType = BuffStatType.DamageReduction,
                Value = _data.Value,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniqueGlobal = true,
                UniquePerSource = false,
                ExpiresWithSource = false
            });
        }

        // ═══════════════════════════════════════════
        // TRANSPERCEMENT
        // ═══════════════════════════════════════════

        private IEnumerator ExecuteTranspercement()
        {
            if (!IsReady || !_phase1Active || _data == null)
                yield break;

            // Pose théâtrale Alucadra (placeholder — hurlement = cue SFX juice).
            _owner.PlayWindup(0.25f);

            CharacterBall target = ResolveClosestAlly();
            if (target == null || target.IsDead)
                yield break;

            if (_sword != null && !_sword.IsDead)
                yield return DashSwordThroughTarget(target);

            int dmg = Mathf.RoundToInt(_data.SpecialValue1);
            if (dmg > 0)
                target.TakeDamage(dmg);

            ApplyBleed(target);
        }

        private IEnumerator DashSwordThroughTarget(CharacterBall target)
        {
            Transform t = _sword.transform;
            Vector3 start = t.position;
            Vector3 end = target.transform.position;
            // Traversée : un peu au-delà de la cible.
            Vector3 beyond = end + (end - start).normalized * 0.4f;

            float half = DASH_DURATION * 0.5f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                if (_sword == null || _sword.IsDead)
                    yield break;
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                t.position = Vector3.Lerp(start, beyond, k);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                if (_sword == null || _sword.IsDead)
                    yield break;
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / half);
                t.position = Vector3.Lerp(beyond, start, k);
                yield return null;
            }

            t.position = start;
        }

        private void ApplyBleed(CharacterBall target)
        {
            if (target == null || target.IsDead || target.BuffReceiver == null || _data == null)
                return;

            int turns = Mathf.Max(1, Mathf.RoundToInt(_data.SpecialValue3));
            target.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BUFF_SAIGNEMENT,
                Source = null,
                EnemySource = _owner,
                StatType = BuffStatType.DamageAmplification,
                Value = _data.SpecialValue2,
                IsPercent = true,
                RemainingTurns = turns,
                RemainingCycles = -1,
                UniqueGlobal = true,
                UniquePerSource = false,
                ExpiresWithSource = false
            });
        }

        private CharacterBall ResolveClosestAlly()
        {
            if (_owner == null || _turnManager == null)
                return null;

            // O3 — selector vide = plus proche.
            return TargetSelectorResolver.Resolve(
                null,
                _owner.transform.position,
                _turnManager.GetAllies());
        }

        // ═══════════════════════════════════════════
        // TRANSFORMATION / MORTS
        // ═══════════════════════════════════════════

        private void OnSwordDeath()
        {
            if (_released || _transformed)
                return;
            // Mort Alucadra d'abord : pas de transformation, juste cleanup.
            if (_owner == null || _owner.IsDead)
                return;

            TriggerTransformation();
        }

        private void OnOwnerDeath()
        {
            // Désabonner avant Die() pour éviter OnSwordDeath → transformation fantôme.
            if (_sword != null && _subscribedSwordDeath)
            {
                _sword.OnDeath -= OnSwordDeath;
                _subscribedSwordDeath = false;
            }

            // Victoire jamais bloquée : compagnon orphelin meurt avec Alucadra.
            if (_sword != null && !_sword.IsDead)
                _sword.Die();

            ReleaseAllResources();
        }

        private void TriggerTransformation()
        {
            _transformed = true;
            _phase1Active = false;

            _owner?.BuffReceiver?.RemoveBuffsById(BUFF_GARDE);
            UnregisterPhase1Combat();

            // Whiteout placeholder — shader d'éveil = passe juice (commentaire acté).
            // Hurlement = cue SFX listée, non implémentée.
            _owner?.PlayWindup(TRANSFORM_WINDUP);

            SwapToAltFrames();

            // Signal R2 : alucadra_loup s'active via OnArchetypeChanged.
            _owner?.SetArchetype(EnemyArchetype.Mobile);

            // Handler phase 1 inerte — désabonnements complets.
            ReleaseAllResources();
        }

        private void SwapToAltFrames()
        {
            if (_owner == null || _owner.Data == null)
                return;

            var player = _owner.GetComponentInChildren<SpriteSheetPlayer>();
            if (player == null)
                return;

            IReadOnlyList<Sprite> alt = _owner.Data.IdleFramesAlt;
            if (alt != null && alt.Count > 0)
                player.SetFrames(alt, _owner.Data.IdleFps);
            else if (_owner.Data.IdleFrames != null && _owner.Data.IdleFrames.Count > 0)
                player.SetFrames(_owner.Data.IdleFrames, _owner.Data.IdleFps);
            // Sinon : jamais de sprite cassé — on laisse l'idle courant.
        }

        private void UnregisterPhase1Combat()
        {
            if (_owner == null)
                return;
            EnemyFixedTurnActionRegistry.Unregister(_owner);
            EnemyIntentSystem.UnregisterProvider(_owner);
        }

        private void ReleaseAllResources()
        {
            if (_released)
                return;
            _released = true;
            _phase1Active = false;

            UnregisterPhase1Combat();

            if (_sword != null && _subscribedSwordDeath)
            {
                _sword.OnDeath -= OnSwordDeath;
                _subscribedSwordDeath = false;
            }

            if (_owner != null && _subscribedOwnerDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedOwnerDeath = false;
            }

            _sword = null;
            _stageGenerator = null;
        }
    }
}
