using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Veuve en Deuil (veuve_courbe) — fiche 5.5 : courbes ATK/DEF, Voile anti-heal, Déchirure.
    /// Courbes via OnHpChanged (dispatch runtime, pas d'abo direct). Mobile : pas de Fixed / intent.
    /// Params : value=amplitude 0.40, sv1=fraction Voile, sv2=seuil Déchirure.
    /// </summary>
    public class VeuveCourbeHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_CHAGRIN = "veuve_chagrin";
        private const string BUFF_CARAPACE = "veuve_carapace";
        private const string BUFF_VOILE = "veuve_voile";
        private const string DECHIRURE_ANNOUNCE = "LE VOILE SE DÉCHIRE";
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private StageAnnouncerUI _announcer;
        private SpriteRenderer _spriteRenderer;
        private MaterialPropertyBlock _mpb;
        private Color _baseColor = Color.white;
        private bool _hasBaseColor;
        private bool _tearConsumed;
        private bool _subscribedDeath;
        private bool _released;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "veuve_courbe";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;
            _tearConsumed = false;

            // FindObjectOfType une seule fois — nullable (repli ShowLabel).
            _announcer = Object.FindObjectOfType<StageAnnouncerUI>();

            CacheVisual();
            ApplyCurves(owner != null ? owner.CurrentHp : 0, owner != null ? owner.MaxHp : 1);
            ApplyVoileToAllAllies();
            UpdateVeilVisual(1f);

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
            _tearConsumed = false;
        }

        public override void OnHpChanged(int currentHp, int maxHp)
        {
            if (!IsReady || _data == null)
                return;

            float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;
            ApplyCurves(currentHp, maxHp);
            UpdateVeilVisual(_tearConsumed ? 0f : ratio);

            // Déchirure : une fois sous le seuil (y compris gros hit unique — TakeDamage → NotifyHpChanged).
            if (!_tearConsumed && ratio < _data.SpecialValue2)
                TriggerTear();
        }

        // ═══════════════════════════════════════════
        // COURBES
        // ═══════════════════════════════════════════

        private void ApplyCurves(int currentHp, int maxHp)
        {
            if (_owner?.BuffReceiver == null || _data == null)
                return;

            float ratio = maxHp > 0 ? (float)currentHp / maxHp : 0f;

            // Chagrin Rageur : ATK front-loadée (pleine vie = max).
            float atkBonus = _data.Value * ratio;

            // Carapace de Deuil : DEF montante — plafond D27 explicite (+value max).
            float defBonus = Mathf.Min(_data.Value, _data.Value * (1f - ratio));

            ApplySelfBuff(BUFF_CHAGRIN, BuffStatType.ATK, atkBonus);
            ApplySelfBuff(BUFF_CARAPACE, BuffStatType.DEF, defBonus);
        }

        private void ApplySelfBuff(string buffId, BuffStatType stat, float value)
        {
            if (_owner?.BuffReceiver == null)
                return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = buffId,
                Source = null,
                EnemySource = null,
                StatType = stat,
                Value = value,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniqueGlobal = true,
                UniquePerSource = false,
                ExpiresWithSource = false
            });
        }

        // ═══════════════════════════════════════════
        // VOILE
        // ═══════════════════════════════════════════

        private void ApplyVoileToAllAllies()
        {
            if (_owner == null || _data == null || _turnManager == null)
                return;

            var allies = _turnManager.GetAllies();
            if (allies == null)
                return;

            float veil = -Mathf.Abs(_data.SpecialValue1);
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead || ally.BuffReceiver == null)
                    continue;

                // Retrait à la mort de la Veuve = gratuit via ExpiresWithSource (socle G4-P4).
                // AUCUN RemoveBuffsById à la mort dans ce handler.
                ally.BuffReceiver.AddBuff(new BuffData
                {
                    BuffId = BUFF_VOILE,
                    Source = null,
                    EnemySource = _owner,
                    StatType = BuffStatType.HealReceived,
                    Value = veil,
                    IsPercent = true,
                    RemainingTurns = -1,
                    RemainingCycles = -1,
                    UniqueGlobal = true,
                    UniquePerSource = false,
                    ExpiresWithSource = true
                });
            }
        }

        private void RemoveVoileFromAllAllies()
        {
            if (_turnManager == null)
                return;

            var allies = _turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.BuffReceiver == null)
                    continue;
                ally.BuffReceiver.RemoveBuffsById(BUFF_VOILE);
            }
        }

        // ═══════════════════════════════════════════
        // DÉCHIRURE
        // ═══════════════════════════════════════════

        private void TriggerTear()
        {
            _tearConsumed = true;
            RemoveVoileFromAllAllies();
            UpdateVeilVisual(0f);

            if (_announcer != null)
                _announcer.ShowBossAnnounce(DECHIRURE_ANNOUNCE);
            else if (_owner != null)
            {
                FloatingNumberSpawner.Instance?.ShowLabel(
                    DECHIRURE_ANNOUNCE,
                    CombatFeedbackPalette.LienRetroSoin,
                    _owner.transform.position,
                    1.6f);
            }

            // Placeholder whiteout léger — PlayWindup réutilisé (vraies particules = juice).
            _owner?.PlayWindup(0.35f);
        }

        // ═══════════════════════════════════════════
        // VISUEL VOILE (MPB placeholder)
        // ═══════════════════════════════════════════

        private void CacheVisual()
        {
            if (_owner == null)
                return;

            _spriteRenderer = _owner.GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
                return;

            _mpb = new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_mpb);
            if (_mpb.HasProperty(ColorPropId) || _spriteRenderer.sharedMaterial != null)
            {
                _baseColor = _spriteRenderer.color;
                _hasBaseColor = true;
            }
        }

        /// <summary>
        /// Assombrissement / pulse teinte ∝ ratio PV. Dense à pleine vie, se dissipe en faiblissant.
        /// Vraies particules = passe juice (commentaire acté).
        /// </summary>
        private void UpdateVeilVisual(float hpRatio)
        {
            if (_spriteRenderer == null || !_hasBaseColor)
                return;

            float veil = Mathf.Clamp01(hpRatio);
            // Assombrit + teinte froide proportionnelle au voile restant.
            Color c = _baseColor;
            c.r *= Mathf.Lerp(1f, 0.55f, veil);
            c.g *= Mathf.Lerp(1f, 0.55f, veil);
            c.b *= Mathf.Lerp(1f, 0.75f, veil);
            c.a = _baseColor.a;

            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorPropId, c);
            _spriteRenderer.SetPropertyBlock(_mpb);
            _spriteRenderer.color = c;
        }

        private void ResetVisual()
        {
            if (_spriteRenderer == null || !_hasBaseColor)
                return;

            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorPropId, _baseColor);
            _spriteRenderer.SetPropertyBlock(_mpb);
            _spriteRenderer.color = _baseColor;
        }

        // ═══════════════════════════════════════════
        // HYGIÈNE
        // ═══════════════════════════════════════════

        private void OnOwnerDeath()
        {
            // Voile alliés : ExpiresWithSource — socle, pas de RemoveBuffs ici.
            ReleaseAllResources();
        }

        private void ReleaseAllResources()
        {
            if (_released)
                return;
            _released = true;

            ResetVisual();

            if (_owner != null && _subscribedDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedDeath = false;
            }

            _announcer = null;
            _spriteRenderer = null;
            _mpb = null;
        }
    }
}
