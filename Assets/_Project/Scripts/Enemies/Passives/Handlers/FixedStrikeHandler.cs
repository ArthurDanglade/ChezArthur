using System.Collections;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Frappe fixe générique (fixed_strike) — portée sv1, dégâts sv2.
    /// Hors portée : retranchement (pulse, zéro effet chiffré — O1).
    /// Libellé intent = passiveName du passif porteur (réutilisable tous univers).
    /// </summary>
    public class FixedStrikeHandler : EnemyPassiveHandlerBase, IEnemyIntentProvider
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const float PUNCH_DURATION = 0.20f;
        private const float PUNCH_DISTANCE = 0.35f;
        private const float RETRENCH_DURATION = 0.25f;
        private const float RETRENCH_SCALE_PEAK = 1.12f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private bool _subscribedDeath;
        private bool _released;
        private Transform _visual;
        private Vector3 _visualBaseLocalScale = Vector3.one;
        private Vector3 _visualBaseLocalPos = Vector3.zero;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "fixed_strike";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;
            CacheVisual();

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

        // ═══════════════════════════════════════════
        // IEnemyIntentProvider
        // ═══════════════════════════════════════════

        public bool TryGetIntent(out EnemyIntent intent)
        {
            intent = default;
            if (!IsReady || _data == null)
                return false;

            CharacterBall target = ResolveTarget();
            float range = _data.SpecialValue1;
            bool inRange = target != null
                && Vector2.Distance(_owner.transform.position, target.transform.position) <= range;

            intent.Kind = EnemyIntentKind.Special;
            intent.IconText = inRange ? "!" : "◆";

            string actionName = !string.IsNullOrEmpty(_data.PassiveName)
                ? _data.PassiveName
                : "Frappe";

            if (inRange)
            {
                intent.Target = target;
                intent.ShortLabel = actionName + " → " + target.Name;
            }
            else
            {
                intent.Target = null;
                intent.ShortLabel = "Se retranche";
            }

            return true;
        }

        public void OnTelegraphStateChanged(bool isTelegraphing)
        {
            // Pas de zone persistante — le système d'intent gère anneau/ligne.
        }

        // ═══════════════════════════════════════════
        // ACTION DE TOUR
        // ═══════════════════════════════════════════

        private IEnumerator ExecuteTurnAction()
        {
            if (!IsReady || _data == null)
                yield break;

            CharacterBall target = ResolveTarget();
            float range = _data.SpecialValue1;
            bool inRange = target != null
                && !target.IsDead
                && Vector2.Distance(_owner.transform.position, target.transform.position) <= range;

            if (inRange)
                yield return PunchRoutine(target);
            else
                yield return RetrenchRoutine();
        }

        private CharacterBall ResolveTarget()
        {
            if (_owner == null || _turnManager == null)
                return null;

            TargetSelectorData selector = _owner.Data != null ? _owner.Data.TargetSelector : null;
            return TargetSelectorResolver.Resolve(
                selector,
                _owner.transform.position,
                _turnManager.GetAllies());
        }

        private IEnumerator PunchRoutine(CharacterBall target)
        {
            CacheVisual();
            if (_visual == null || target == null)
            {
                ApplyDamage(target);
                yield break;
            }

            Vector3 basePos = _visual.localPosition;
            Vector2 dir = ((Vector2)target.transform.position - (Vector2)_owner.transform.position);
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;
            dir.Normalize();

            // Punch dans l'espace local du Visual (approx. direction monde).
            Vector3 peak = basePos + (Vector3)(dir * PUNCH_DISTANCE);

            float t = 0f;
            while (t < PUNCH_DURATION)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / PUNCH_DURATION);
                // Aller-retour : 0→1→0
                float wave = k < 0.5f ? (k * 2f) : (2f - k * 2f);
                _visual.localPosition = Vector3.Lerp(basePos, peak, wave);
                yield return null;
            }

            _visual.localPosition = basePos;
            ApplyDamage(target);
        }

        private void ApplyDamage(CharacterBall target)
        {
            if (target == null || target.IsDead || _data == null)
                return;
            int dmg = Mathf.RoundToInt(_data.SpecialValue2);
            if (dmg > 0)
                target.TakeDamage(dmg);
        }

        private IEnumerator RetrenchRoutine()
        {
            CacheVisual();
            if (_visual == null)
                yield break;

            Vector3 baseScale = _visualBaseLocalScale;
            SpriteRenderer sr = _visual.GetComponent<SpriteRenderer>();
            Color baseColor = sr != null ? sr.color : Color.white;

            float t = 0f;
            while (t < RETRENCH_DURATION)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / RETRENCH_DURATION);
                float wave = k < 0.5f ? (k * 2f) : (2f - k * 2f);
                _visual.localScale = baseScale * Mathf.Lerp(1f, RETRENCH_SCALE_PEAK, wave);
                if (sr != null)
                {
                    Color c = baseColor;
                    c.r = Mathf.Lerp(baseColor.r, 1f, wave * 0.5f);
                    c.g = Mathf.Lerp(baseColor.g, 1f, wave * 0.5f);
                    c.b = Mathf.Lerp(baseColor.b, 1f, wave * 0.35f);
                    sr.color = c;
                }

                yield return null;
            }

            _visual.localScale = baseScale;
            if (sr != null)
                sr.color = baseColor;
            // Aucun effet chiffré (O1).
        }

        // ═══════════════════════════════════════════
        // VISUEL
        // ═══════════════════════════════════════════

        private void CacheVisual()
        {
            if (_owner == null)
                return;

            if (_visual != null)
                return;

            SpriteRenderer sr = _owner.GetComponentInChildren<SpriteRenderer>();
            _visual = sr != null ? sr.transform : _owner.transform;
            _visualBaseLocalScale = _visual.localScale;
            _visualBaseLocalPos = _visual.localPosition;
        }

        // ═══════════════════════════════════════════
        // HYGIÈNE
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

            if (_visual != null)
            {
                _visual.localScale = _visualBaseLocalScale;
                _visual.localPosition = _visualBaseLocalPos;
            }

            if (_owner != null && _subscribedDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedDeath = false;
            }

            EnemyFixedTurnActionRegistry.Unregister(_owner);
            EnemyIntentSystem.UnregisterProvider(_owner);
            _visual = null;
        }
    }
}
