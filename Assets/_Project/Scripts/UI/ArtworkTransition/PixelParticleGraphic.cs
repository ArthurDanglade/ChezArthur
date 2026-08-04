using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Particules pixel + glow en un seul draw call (mesh reconstruit dans OnPopulateMesh).
    /// Ring buffer préalloué — zéro alloc en régime stable.
    ///
    /// INV2 §E-A — extension additive sanctionnée post-clôture AW (unique) :
    /// surcharge <see cref="SpawnBurst(Vector2, Color)"/> pour teinter le punch
    /// d'apparition par rareté. Aucun autre appelant AW n'est modifié ; la
    /// surcharge historique délègue avec la paire or AW.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelParticleGraphic : MaskableGraphic
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int CAPACITY = 320;
        private const uint RNG_SEED = 2026u;
        private const byte KindEmber = 0;
        private const byte KindAsh = 1;
        private const byte KindMote = 2;
        private const byte KindConverge = 3;
        private const byte KindBurst = 4;
        private const byte KindReforge = 5;

        // ═══════════════════════════════════════════
        // STRUCT INTERNE
        // ═══════════════════════════════════════════
        private struct Particle
        {
            public Vector2 pos, vel;
            public float gravity, drag, sway, swayPhase, life, age, size, glowSize;
            public Color color;
            public byte kind;
            public bool alive;
            public Color hotColor; // couleur chaude Ember (fournie au spawn)
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Particle[] _particles = new Particle[CAPACITY];
        private int _count;
        private int _write;
        private uint _rng = RNG_SEED;
        private float _cellSize = 1f;
        private float _stageK = 1f;
        private float _glowIntensity = 1f;
        private Vector2 _center;
        private Texture _spriteTex;

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Texture glow (AwGlowSoft)")]
        [SerializeField] private Texture glowTexture;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public override Texture mainTexture =>
            _spriteTex != null ? _spriteTex : (glowTexture != null ? glowTexture : s_WhiteTexture);

        public int AliveCount => _count;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            _spriteTex = glowTexture;
            raycastTarget = false;
        }

        // ═══════════════════════════════════════════
        // CONFIG RUNTIME
        // ═══════════════════════════════════════════

        public void SetCellSize(float cell) => _cellSize = Mathf.Max(0.01f, cell);
        public void SetStageScaleK(float k) => _stageK = Mathf.Max(0.01f, k);
        public void SetGlowIntensity(float g) => _glowIntensity = Mathf.Max(0f, g);
        public void SetCenter(Vector2 center) => _center = center;

        /// <summary>Vide le ring buffer.</summary>
        public void Clear()
        {
            for (int i = 0; i < CAPACITY; i++)
                _particles[i].alive = false;
            _count = 0;
            _write = 0;
            SetVerticesDirty();
        }

        // ═══════════════════════════════════════════
        // TICK
        // ═══════════════════════════════════════════

        /// <summary>Intègre, vieillit et tue les particules. Appeler chaque frame de lecture.</summary>
        public void Tick(float dt)
        {
            if (dt <= 0f || _count <= 0)
            {
                if (_count > 0) SetVerticesDirty();
                return;
            }

            int alive = 0;
            for (int i = 0; i < CAPACITY; i++)
            {
                if (!_particles[i].alive)
                    continue;

                ref Particle p = ref _particles[i];
                p.age += dt;
                if (p.age >= p.life)
                {
                    p.alive = false;
                    continue;
                }

                float k = p.age / p.life;

                // Converge : redirection continue vers le centre
                if (p.kind == KindConverge)
                {
                    Vector2 to = _center - p.pos;
                    float dist = to.magnitude;
                    float killR = 46f * _stageK;
                    if (dist < killR)
                    {
                        p.alive = false;
                        continue;
                    }

                    float speed = (240f + 1300f * k * k) * _stageK;
                    if (dist > 0.0001f)
                        p.vel = to * (speed / dist);
                }

                // Drag exponentiel
                if (p.drag > 0f)
                    p.vel -= p.vel * (p.drag * dt);

                p.vel.y += p.gravity * dt;
                p.pos.x += p.vel.x * dt + Mathf.Sin(p.age * 5f + p.swayPhase) * p.sway * dt;
                p.pos.y += p.vel.y * dt;

                // Courbe couleur Ember
                if (p.kind == KindEmber)
                    p.color = EvalEmberColor(k, p.hotColor);
                else if (p.kind == KindAsh)
                {
                    Color c = p.hotColor;
                    c.a = (1f - k) * 0.85f;
                    p.color = c;
                }
                else if (p.kind == KindConverge)
                {
                    Color c = p.hotColor;
                    c.a = 1f - k;
                    p.color = c;
                }

                alive++;
            }

            _count = alive;
            SetVerticesDirty();
        }

        // ═══════════════════════════════════════════
        // SPAWN APIs
        // ═══════════════════════════════════════════

        /// <summary>Braise montante. hotColor = Lerp(Gold, AshViolet, hybrid).</summary>
        public void SpawnEmber(Vector2 pos, Color hotColor)
        {
            float k = _stageK;
            float cell = _cellSize;
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            p.pos = pos;
            p.vel = new Vector2(SignedRange(15f) * k, Range(35f, 110f) * k);
            p.gravity = -150f * k;
            p.drag = 0f;
            p.sway = 0f;
            p.swayPhase = Next() * Mathf.PI * 2f;
            p.life = Range(0.35f, 0.75f);
            p.age = 0f;
            p.size = (Next() < 0.4f ? 0.6f : 1f) * cell;
            p.glowSize = 0.7f;
            p.hotColor = hotColor;
            p.color = EvalEmberColor(0f, hotColor);
            p.kind = KindEmber;
        }

        /// <summary>Cendre tombante. baseColor fournie (teinte pixel prime).</summary>
        public void SpawnAsh(Vector2 pos, Color baseColor)
        {
            float k = _stageK;
            float cell = _cellSize;
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            p.pos = pos;
            p.vel = new Vector2(SignedRange(11f) * k, Range(-77f, -22f) * k);
            p.gravity = -26f * k;
            p.drag = 0f;
            p.sway = Range(14f, 26f) * k;
            p.swayPhase = Next() * Mathf.PI * 2f;
            p.life = Range(1.1f, 2.2f);
            p.age = 0f;
            p.size = Range(0.55f, 0.8f) * cell;
            p.glowSize = 0f;
            p.hotColor = baseColor;
            Color c = baseColor;
            c.a = 0.85f;
            p.color = c;
            p.kind = KindAsh;
        }

        /// <summary>Mote dorée montante (contemplation / apothéose).</summary>
        public void SpawnMote(Vector2 pos)
        {
            float k = _stageK;
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            p.pos = pos;
            p.vel = new Vector2(SignedRange(6f) * k, Range(9f, 25f) * k);
            p.gravity = 0f;
            p.drag = 0f;
            p.sway = Range(6f, 12f) * k;
            p.swayPhase = Next() * Mathf.PI * 2f;
            p.life = Range(1.6f, 2.8f);
            p.age = 0f;
            p.size = 3f * k;
            p.glowSize = 0.55f;
            p.hotColor = AwPalette.RimGold;
            p.color = AwPalette.RimGold;
            p.kind = KindMote;
        }

        /// <summary>Particule convergente (anneau → centre).</summary>
        public void SpawnConverge()
        {
            float k = _stageK;
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            float angle = Next() * Mathf.PI * 2f;
            float r = Range(300f, 450f) * k;
            p.pos = _center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            p.vel = Vector2.zero;
            p.gravity = 0f;
            p.drag = 0f;
            p.sway = 0f;
            p.swayPhase = 0f;
            p.life = Range(0.55f, 0.9f);
            p.age = 0f;
            p.size = Range(3f, 5f) * k;
            p.glowSize = 0.8f;
            bool white = Next() < 0.3f;
            p.hotColor = white ? Color.white : AwPalette.RimGold;
            p.color = p.hotColor;
            p.kind = KindConverge;
        }

        /// <summary>
        /// Burst radial (apparition / climax) — palette or AW historique.
        /// Délègue à <see cref="SpawnBurst(Vector2, Color)"/> (INV2 §E-A).
        /// </summary>
        public void SpawnBurst(Vector2 pos)
        {
            // Couleur or historique (GoldCore / Gold) — inchangée pour les appelants AW.
            SpawnBurst(pos, Next() < 0.45f ? AwPalette.GoldCore : AwPalette.Gold);
        }

        /// <summary>
        /// Burst radial teinté (INV2 §E-A — extension sanctionnée du socle AW).
        /// Utilise <paramref name="hot"/> à la place du couple or.
        /// </summary>
        public void SpawnBurst(Vector2 pos, Color hot)
        {
            float k = _stageK;
            float cell = _cellSize;
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            float angle = Next() * Mathf.PI * 2f;
            float speed = Range(190f, 520f) * k;
            p.pos = pos;
            p.vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            p.gravity = -60f * k;
            p.drag = 3.2f;
            p.sway = 0f;
            p.swayPhase = 0f;
            p.life = Range(0.45f, 0.95f);
            p.age = 0f;
            p.size = Range(0.6f, 1f) * cell;
            p.glowSize = 0.9f;
            p.hotColor = hot;
            p.color = p.hotColor;
            p.kind = KindBurst;
        }

        /// <summary>Étincelle de reforge montante.</summary>
        public void SpawnReforge(Vector2 pos)
        {
            float k = _stageK;
            float cell = _cellSize;
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            p.pos = pos;
            p.vel = new Vector2(SignedRange(10f) * k, Range(45f, 130f) * k);
            p.gravity = 20f * k;
            p.drag = 1.2f;
            p.sway = 0f;
            p.swayPhase = 0f;
            p.life = Range(0.4f, 0.85f);
            p.age = 0f;
            p.size = Range(0.6f, 1f) * cell;
            p.glowSize = 0.85f;
            p.hotColor = Next() < 0.4f ? AwPalette.GoldCore : AwPalette.RimGold;
            p.color = p.hotColor;
            p.kind = KindReforge;
        }

        // ═══════════════════════════════════════════
        // MESH
        // ═══════════════════════════════════════════

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_count <= 0)
                return;

            float cell = _cellSize;
            int vi = 0;

            for (int i = 0; i < CAPACITY; i++)
            {
                if (!_particles[i].alive)
                    continue;

                ref Particle p = ref _particles[i];
                float px = Mathf.Round(p.pos.x / cell) * cell;
                float py = Mathf.Round(p.pos.y / cell) * cell;
                float half = p.size * 0.5f;
                Color32 matCol = p.color;

                // Glow dessous (UV pleine texture)
                if (p.glowSize > 0.001f)
                {
                    float glowHalf = p.size * (5f + 4f * p.glowSize) * 0.5f;
                    Color gc = p.color;
                    gc.a = p.color.a * p.glowSize * 0.5f * _glowIntensity;
                    Color32 glowCol = gc;
                    AddQuad(vh, px, py, glowHalf, glowCol, 0f, 0f, 1f, 1f, ref vi);
                }

                // Matière dessus (UV centre 0.48–0.52 → couleur pleine)
                AddQuad(vh, px, py, half, matCol, 0.48f, 0.48f, 0.52f, 0.52f, ref vi);
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private bool TryBegin(out int idx)
        {
            if (_count >= CAPACITY)
            {
                idx = -1;
                return false;
            }

            // Cherche un slot libre depuis _write
            for (int n = 0; n < CAPACITY; n++)
            {
                int i = (_write + n) % CAPACITY;
                if (!_particles[i].alive)
                {
                    idx = i;
                    _particles[i].alive = true;
                    _write = (i + 1) % CAPACITY;
                    _count++;
                    return true;
                }
            }

            idx = -1;
            return false;
        }

        private static void AddQuad(
            VertexHelper vh, float cx, float cy, float half, Color32 col,
            float u0, float v0, float u1, float v1, ref int vi)
        {
            vh.AddVert(new Vector3(cx - half, cy - half), col, new Vector2(u0, v0));
            vh.AddVert(new Vector3(cx - half, cy + half), col, new Vector2(u0, v1));
            vh.AddVert(new Vector3(cx + half, cy + half), col, new Vector2(u1, v1));
            vh.AddVert(new Vector3(cx + half, cy - half), col, new Vector2(u1, v0));
            vh.AddTriangle(vi, vi + 1, vi + 2);
            vh.AddTriangle(vi + 2, vi + 3, vi);
            vi += 4;
        }

        private static Color EvalEmberColor(float k, Color hot)
        {
            Color c;
            if (k < 0.35f)
                c = Color.Lerp(AwPalette.GoldCore, hot, k / 0.35f);
            else
                c = Color.Lerp(hot, AwPalette.AshDark, (k - 0.35f) / 0.65f);
            c.a = 1f - k * k;
            return c;
        }

        // ─── Mulberry32 (seed 2026) ───
        private float Next()
        {
            _rng += 0x6D2B79F5u;
            uint t = _rng;
            t = (t ^ (t >> 15)) * (t | 1u);
            t ^= t + (t ^ (t >> 7)) * (t | 61u);
            return (t ^ (t >> 14)) / 4294967296f;
        }

        private float Range(float a, float b) => a + (b - a) * Next();
        private float SignedRange(float a) => Range(-a, a);
    }
}
