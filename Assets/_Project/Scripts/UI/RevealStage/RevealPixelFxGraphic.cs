using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.RevealStage
{
    /// <summary>
    /// Particules pixel du stage reveal — autonome (AW PixelParticleGraphic non touché).
    /// Buffer fixe 512, Tick manuel, zéro alloc en boucle.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class RevealPixelFxGraphic : MaskableGraphic
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int CAPACITY = 512;
        private const uint RNG_SEED = 2026u;
        private const byte KindMote = 0;
        private const byte KindBurst = 1;

        // ═══════════════════════════════════════════
        // STRUCT INTERNE
        // ═══════════════════════════════════════════
        private struct Particle
        {
            public Vector2 pos, vel;
            public float gravity, life, age, size, baseAlpha;
            public Color color;
            public byte kind;
            public bool alive;
        }

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly Particle[] _particles = new Particle[CAPACITY];
        private int _count;
        private int _write;
        private uint _rng = RNG_SEED;
        private float _cellSize = 3f;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public override Texture mainTexture => s_WhiteTexture;
        public int AliveCount => _count;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        // ═══════════════════════════════════════════
        // CONFIG RUNTIME
        // ═══════════════════════════════════════════

        public void SetCellSize(float cell) => _cellSize = Mathf.Max(0.01f, cell);

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

        /// <summary>Intègre et tue les particules. Appeler chaque frame de lecture.</summary>
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
                p.vel.y += p.gravity * dt;
                p.pos.x += p.vel.x * dt;
                p.pos.y += p.vel.y * dt;

                // Alpha dérivé de baseAlpha (jamais réécrit en décroissance cumulative)
                float a = p.baseAlpha * (1f - k);
                if (p.kind == KindMote)
                    a = Mathf.Min(0.5f, a);
                p.color = new Color(p.color.r, p.color.g, p.color.b, a);

                alive++;
            }

            _count = alive;
            SetVerticesDirty();
        }

        // ═══════════════════════════════════════════
        // SPAWN
        // ═══════════════════════════════════════════

        /// <summary>
        /// Mote lente verticale — vie 1,2–2 s, alpha ≤ 0,5.
        /// </summary>
        public void SpawnMote(Vector2 posRect, Color c)
        {
            if (!TryBegin(out int idx)) return;
            ref Particle p = ref _particles[idx];
            p.pos = posRect;
            p.vel = new Vector2(SignedRange(4f), Range(6f, 18f));
            p.gravity = -8f;
            p.life = Range(1.2f, 2f);
            p.age = 0f;
            p.size = _cellSize;
            c.a = Mathf.Min(0.5f, c.a > 0.001f ? c.a : 0.45f);
            p.baseAlpha = c.a;
            p.color = c;
            p.kind = KindMote;
        }

        /// <summary>
        /// Burst radial — vitesses 60–260 px/s, gravité légère, vie 0,45–0,8 s.
        /// ~35 % blanches (cœur chaud).
        /// </summary>
        public void SpawnBurst(Vector2 posRect, Color c, int count)
        {
            int n = Mathf.Max(0, count);
            for (int i = 0; i < n; i++)
            {
                if (!TryBegin(out int idx)) return;
                ref Particle p = ref _particles[idx];
                float angle = Next() * Mathf.PI * 2f;
                float speed = Range(60f, 260f);
                p.pos = posRect;
                p.vel = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                p.gravity = -40f;
                p.life = Range(0.45f, 0.8f);
                p.age = 0f;
                p.size = (Next() < 0.4f ? 0.7f : 1f) * _cellSize;
                bool white = Next() < 0.35f;
                Color col = white ? Color.white : c;
                col.a = white ? 0.9f : 0.85f;
                p.baseAlpha = col.a;
                p.color = col;
                p.kind = KindBurst;
            }
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
                AddQuad(vh, px, py, half, matCol, ref vi);
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
            VertexHelper vh, float cx, float cy, float half, Color32 col, ref int vi)
        {
            vh.AddVert(new Vector3(cx - half, cy - half), col, new Vector2(0.48f, 0.48f));
            vh.AddVert(new Vector3(cx - half, cy + half), col, new Vector2(0.48f, 0.52f));
            vh.AddVert(new Vector3(cx + half, cy + half), col, new Vector2(0.52f, 0.52f));
            vh.AddVert(new Vector3(cx + half, cy - half), col, new Vector2(0.52f, 0.48f));
            vh.AddTriangle(vi, vi + 1, vi + 2);
            vh.AddTriangle(vi + 2, vi + 3, vi);
            vi += 4;
        }

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
