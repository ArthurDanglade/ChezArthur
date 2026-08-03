using System;
using System.Collections.Generic;
using ChezArthur.Enemies;
using ChezArthur.Gameplay;
using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Forme de zone au sol (R7). Cercle : size.x = rayon (y ignoré). Rectangle : largeur × hauteur.
    /// </summary>
    public enum ZoneShape
    {
        Circle = 0,
        Rectangle = 1,
    }

    /// <summary>
    /// Nature de zone (R7) : persistante (contour) ou impact (remplissage imminent).
    /// </summary>
    public enum ZoneKind
    {
        Persistent = 0,
        Impact = 1,
    }

    /// <summary>
    /// Instance de zone au sol — handle pour les handlers G6 et l'intention G3-P2.
    /// Rendu + collider trigger + présence alliée. Pas d'Update propre (animé par le manager).
    /// </summary>
    public class GroundZone : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const float PERSISTENT_ALPHA = 0.28f;
        private const float PERSISTENT_HIGHLIGHT_ALPHA = 0.72f;
        private const float IMPACT_ALPHA_MIN = 0.55f;
        private const float IMPACT_ALPHA_MAX = 0.85f;
        private const float IMPACT_RAMP_SECONDS = 2f;
        private const float IMPACT_PULSE_PERIOD = 0.8f;
        private const float HIGHLIGHT_SCALE_AMP = 0.02f;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Enemy _owner;
        private ZoneKind _kind;
        private ZoneShape _shape;
        private Vector2 _size;
        private Color _tint = Color.white;
        private bool _highlighted;
        private float _age;
        private SpriteRenderer _fillRenderer;
        private SpriteRenderer _outlineRenderer;
        private CircleCollider2D _circleCollider;
        private BoxCollider2D _boxCollider;
        private readonly HashSet<CharacterBall> _alliesInside = new HashSet<CharacterBall>();
        private Transform _visualRoot;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public Enemy Owner => _owner;
        public ZoneKind Kind => _kind;
        public ZoneShape Shape => _shape;
        public Vector2 Size => _size;

        /// <summary>
        /// Traversée (Eaux Bénites) = Entered avec ally.IsMoving == true ;
        /// présence à la résolution = GetAlliesInside.
        /// </summary>
        public event Action<CharacterBall> OnAllyEntered;

        public event Action<CharacterBall> OnAllyExited;

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Remplit buffer avec les alliés vivants encore dans la zone. Retourne le compte.
        /// </summary>
        public int GetAlliesInside(List<CharacterBall> buffer)
        {
            if (buffer == null)
                return 0;

            buffer.Clear();
            PurgeDeadAllies();
            foreach (CharacterBall ally in _alliesInside)
            {
                if (ally != null && !ally.IsDead)
                    buffer.Add(ally);
            }

            return buffer.Count;
        }

        /// <summary>
        /// Intensification « prochain à jouer » (persistante) — consommé par G3-P2.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
        }

        public void SetWorldPosition(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
        }

        // ═══════════════════════════════════════════
        // API INTERNE (manager)
        // ═══════════════════════════════════════════

        internal void EnsureComponents()
        {
            if (_visualRoot != null)
                return;

            GameObject visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            _visualRoot = visualGo.transform;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_visualRoot, false);
            _fillRenderer = fillGo.AddComponent<SpriteRenderer>();
            _fillRenderer.sortingOrder = GroundZoneSystem.ZoneSortingOrder;

            GameObject outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(_visualRoot, false);
            _outlineRenderer = outlineGo.AddComponent<SpriteRenderer>();
            _outlineRenderer.sortingOrder = GroundZoneSystem.ZoneSortingOrder + 1;

            _circleCollider = gameObject.AddComponent<CircleCollider2D>();
            _circleCollider.isTrigger = true;
            _circleCollider.enabled = false;

            _boxCollider = gameObject.AddComponent<BoxCollider2D>();
            _boxCollider.isTrigger = true;
            _boxCollider.enabled = false;
        }

        internal void Activate(
            Enemy owner,
            ZoneKind kind,
            ZoneShape shape,
            Vector2 size,
            Vector2 worldPosition,
            Color tint,
            Sprite ringSprite,
            Sprite softDiscSprite,
            Sprite hollowRectSprite,
            Sprite tileSprite)
        {
            EnsureComponents();

            _owner = owner;
            _kind = kind;
            _shape = shape;
            _size = size;
            _tint = tint;
            _highlighted = false;
            _age = 0f;
            _alliesInside.Clear();
            OnAllyEntered = null;
            OnAllyExited = null;

            SetWorldPosition(worldPosition);
            gameObject.SetActive(true);

            ConfigureColliders();
            ConfigureSprites(ringSprite, softDiscSprite, hollowRectSprite, tileSprite);
            ApplyVisualFrame(0f);
        }

        internal void DeactivateForPool()
        {
            OnAllyEntered = null;
            OnAllyExited = null;
            _alliesInside.Clear();
            _owner = null;
            _highlighted = false;
            _age = 0f;

            if (_circleCollider != null)
                _circleCollider.enabled = false;
            if (_boxCollider != null)
                _boxCollider.enabled = false;

            gameObject.SetActive(false);
        }

        /// <summary> Animation visuelle — appelée par le LateUpdate unique du manager. </summary>
        internal void TickVisual(float deltaTime)
        {
            _age += deltaTime;
            ApplyVisualFrame(_age);
        }

        // ═══════════════════════════════════════════
        // TRIGGERS
        // ═══════════════════════════════════════════

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null)
                return;

            CharacterBall ally = other.GetComponent<CharacterBall>();
            if (ally == null)
                return;

            if (!_alliesInside.Add(ally))
                return;

            OnAllyEntered?.Invoke(ally);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null)
                return;

            CharacterBall ally = other.GetComponent<CharacterBall>();
            if (ally == null)
                return;

            if (!_alliesInside.Remove(ally))
                return;

            OnAllyExited?.Invoke(ally);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void ConfigureColliders()
        {
            if (_shape == ZoneShape.Circle)
            {
                _boxCollider.enabled = false;
                _circleCollider.enabled = true;
                _circleCollider.radius = Mathf.Max(0.05f, _size.x);
            }
            else
            {
                _circleCollider.enabled = false;
                _boxCollider.enabled = true;
                _boxCollider.size = new Vector2(Mathf.Max(0.05f, _size.x), Mathf.Max(0.05f, _size.y));
            }
        }

        private void ConfigureSprites(
            Sprite ringSprite,
            Sprite softDiscSprite,
            Sprite hollowRectSprite,
            Sprite tileSprite)
        {
            if (_shape == ZoneShape.Circle)
            {
                float diameter = Mathf.Max(0.1f, _size.x * 2f);
                if (_kind == ZoneKind.Persistent)
                {
                    _fillRenderer.enabled = false;
                    _outlineRenderer.enabled = true;
                    _outlineRenderer.sprite = ringSprite;
                    _outlineRenderer.drawMode = SpriteDrawMode.Simple;
                    _outlineRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);
                }
                else
                {
                    _outlineRenderer.enabled = false;
                    _fillRenderer.enabled = true;
                    _fillRenderer.sprite = softDiscSprite;
                    _fillRenderer.drawMode = SpriteDrawMode.Simple;
                    _fillRenderer.transform.localScale = new Vector3(diameter, diameter, 1f);
                }
            }
            else
            {
                float w = Mathf.Max(0.1f, _size.x);
                float h = Mathf.Max(0.1f, _size.y);
                if (_kind == ZoneKind.Persistent)
                {
                    _fillRenderer.enabled = false;
                    _outlineRenderer.enabled = true;
                    _outlineRenderer.sprite = hollowRectSprite;
                    _outlineRenderer.drawMode = SpriteDrawMode.Simple;
                    _outlineRenderer.transform.localScale = new Vector3(w, h, 1f);
                }
                else
                {
                    _outlineRenderer.enabled = false;
                    _fillRenderer.enabled = true;
                    _fillRenderer.sprite = tileSprite;
                    _fillRenderer.drawMode = SpriteDrawMode.Tiled;
                    _fillRenderer.size = new Vector2(w, h);
                    _fillRenderer.transform.localScale = Vector3.one;
                }
            }
        }

        private void ApplyVisualFrame(float age)
        {
            float alpha;
            float scaleMul = 1f;

            if (_kind == ZoneKind.Persistent)
            {
                alpha = _highlighted ? PERSISTENT_HIGHLIGHT_ALPHA : PERSISTENT_ALPHA;
                if (_highlighted)
                    scaleMul = 1f + Mathf.Sin(age * Mathf.PI) * HIGHLIGHT_SCALE_AMP;
            }
            else
            {
                if (age < IMPACT_RAMP_SECONDS)
                {
                    float t = age / IMPACT_RAMP_SECONDS;
                    alpha = Mathf.Lerp(IMPACT_ALPHA_MIN, IMPACT_ALPHA_MAX, t);
                }
                else
                {
                    float pulseT = ((age - IMPACT_RAMP_SECONDS) % IMPACT_PULSE_PERIOD) / IMPACT_PULSE_PERIOD;
                    float wave = 0.5f + 0.5f * Mathf.Sin(pulseT * Mathf.PI * 2f);
                    alpha = Mathf.Lerp(IMPACT_ALPHA_MIN, IMPACT_ALPHA_MAX, wave);
                }
            }

            Color c = _tint;
            c.a = alpha * _tint.a;

            if (_fillRenderer != null && _fillRenderer.enabled)
                _fillRenderer.color = c;
            if (_outlineRenderer != null && _outlineRenderer.enabled)
                _outlineRenderer.color = c;

            if (_visualRoot != null)
                _visualRoot.localScale = new Vector3(scaleMul, scaleMul, 1f);
        }

        private void PurgeDeadAllies()
        {
            if (_alliesInside.Count == 0)
                return;

            _alliesInside.RemoveWhere(static a => a == null || a.IsDead);
        }
    }
}
