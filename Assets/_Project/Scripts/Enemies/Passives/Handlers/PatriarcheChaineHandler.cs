using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Enemies.Passives;
using ChezArthur.Gameplay;
using ChezArthur.Gameplay.Buffs;
using ChezArthur.UI;
using UnityEngine;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Patriarche — Chaîne Tournante (patriarche_chaine), fiche 5.7.
    /// Carrier OnHitByAlly + PerHit : chaque hit renvoie, le plafond D28 régule.
    /// Params : value=DR, sv1=fraction renvoyée, sv2=plafond/tour (fraction PV max attaquant).
    /// R9 : pas de plancher — mourir sur la chaîne est un choix lisible.
    /// </summary>
    public class PatriarcheChaineHandler : EnemyPassiveHandlerBase
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        private const string BUFF_DR = "patriarche_chaine_reduction";
        private const int LINK_COUNT = 8;
        private const float LINK_RADIUS = 0.85f;
        private const float LINK_SIZE = 0.18f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════

        private readonly Dictionary<CharacterBall, int> _reflectedThisTurn =
            new Dictionary<CharacterBall, int>(4);

        private int _localTurnStamp;
        private int _reflectResetStamp = -1;
        private bool _subscribedTurn;
        private bool _subscribedDeath;
        private bool _released;
        private GameObject _linksRoot;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉ
        // ═══════════════════════════════════════════

        public override string HandlerId => "patriarche_chaine";

        // ═══════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════

        public override void Initialize(Enemy owner, EnemyPassiveData data, TurnManager turnManager)
        {
            base.Initialize(owner, data, turnManager);
            _released = false;
            _localTurnStamp = 0;
            _reflectedThisTurn.Clear();

            ApplyReduction();
            BuildStaticLinks();

            // Timbre local — le _turnStamp du runtime est privé (pattern documenté).
            if (_turnManager != null && !_subscribedTurn)
            {
                _turnManager.OnTurnChanged += OnTurnChanged;
                _subscribedTurn = true;
            }

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
            _localTurnStamp = 0;
            _reflectedThisTurn.Clear();
        }

        // ═══════════════════════════════════════════
        // RENVOI (D28)
        // ═══════════════════════════════════════════

        public override void OnHitByAllyWithDamage(CharacterBall attacker, int damage)
        {
            // D28 — plafond par attaquant et par TOUR ; R9 — PAS de plancher :
            // mourir sur la chaîne est un choix lisible (« j'ai mal joué »).
            if (!IsReady || _data == null || attacker == null || attacker.IsDead || damage <= 0)
                return;

            // Reset paresseux du dictionnaire au changement de timbre (D28).
            if (_reflectResetStamp != _localTurnStamp)
            {
                _reflectedThisTurn.Clear();
                _reflectResetStamp = _localTurnStamp;
            }

            int cap = Mathf.RoundToInt(attacker.MaxHp * _data.SpecialValue2);
            _reflectedThisTurn.TryGetValue(attacker, out int already);
            int reflected = Mathf.RoundToInt(damage * _data.SpecialValue1);
            reflected = Mathf.Min(reflected, cap - already);
            if (reflected <= 0)
                return;

            _reflectedThisTurn[attacker] = already + reflected;

            // Attribution D12 complète (pattern AllyDotSystem).
            attacker.SuppressNextDamagePopup();
            attacker.TakeDamage(reflected);
            FloatingNumberSpawner.Instance?.ShowLabel(
                "-" + reflected,
                CombatFeedbackPalette.ChaineRenvoi,
                attacker.transform.position,
                1f);
        }

        private void OnTurnChanged(ITurnParticipant _)
        {
            _localTurnStamp++;
        }

        // ═══════════════════════════════════════════
        // BUFF / MAILLONS
        // ═══════════════════════════════════════════

        private void ApplyReduction()
        {
            if (_owner?.BuffReceiver == null || _data == null)
                return;

            _owner.BuffReceiver.AddBuff(new BuffData
            {
                BuffId = BUFF_DR,
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

        /// <summary>
        /// Anneau STATIQUE de 8 carrés autour du Visual.
        /// Rotation + clang = passe juice (commentaire acté).
        /// </summary>
        private void BuildStaticLinks()
        {
            if (_owner == null)
                return;

            Transform parent = _owner.transform;
            var visual = _owner.GetComponentInChildren<SpriteRenderer>();
            if (visual != null)
                parent = visual.transform;

            _linksRoot = new GameObject("PatriarcheChaineLinks");
            _linksRoot.transform.SetParent(parent, false);
            _linksRoot.transform.localPosition = Vector3.zero;

            Sprite square = CreateWhiteSquareSprite();
            for (int i = 0; i < LINK_COUNT; i++)
            {
                float angle = (Mathf.PI * 2f * i) / LINK_COUNT;
                var go = new GameObject("Link_" + i);
                go.transform.SetParent(_linksRoot.transform, false);
                go.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * LINK_RADIUS,
                    Mathf.Sin(angle) * LINK_RADIUS,
                    0f);
                go.transform.localScale = new Vector3(LINK_SIZE, LINK_SIZE, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = square;
                sr.color = new Color(0.62f, 0.70f, 0.78f, 0.85f);
                sr.sortingOrder = 12;
            }
        }

        private static Sprite CreateWhiteSquareSprite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color32[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
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

            if (_turnManager != null && _subscribedTurn)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
                _subscribedTurn = false;
            }

            if (_owner != null && _subscribedDeath)
            {
                _owner.OnDeath -= OnOwnerDeath;
                _subscribedDeath = false;
            }

            if (_linksRoot != null)
            {
                Object.Destroy(_linksRoot);
                _linksRoot = null;
            }

            _reflectedThisTurn.Clear();
        }
    }
}
