using UnityEngine;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Générateur de value-noise fBm déterministe — transposition exacte de la preview AW0.
    /// Sert à la fois à générer ArtworkNoise.png (éditeur) et le champ CPU du front (runtime).
    /// </summary>
    public static class ArtworkNoise
    {
        public const int SIZE = 256;
        public const int DEFAULT_SEED = 1337;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>Génère le champ 256×256 normalisé 0..1 (index y * SIZE + x).</summary>
        public static float[] Generate(int seed = DEFAULT_SEED)
        {
            uint state = (uint)seed;
            float Next()
            {
                state += 0x6D2B79F5u;
                uint t = state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296f;
            }

            // Grille de base 64×64
            var lattice = new float[64 * 64];
            for (int i = 0; i < lattice.Length; i++) lattice[i] = Next();

            float Smooth(float t) => t * t * (3f - 2f * t);
            float Sample(float x, float y, float freq)
            {
                float fx = (x * freq) % 64f, fy = (y * freq) % 64f;
                int x0 = ((int)Mathf.Floor(fx)) % 64, y0 = ((int)Mathf.Floor(fy)) % 64;
                int x1 = (x0 + 1) % 64, y1 = (y0 + 1) % 64;
                float tx = Smooth(fx - Mathf.Floor(fx)), ty = Smooth(fy - Mathf.Floor(fy));
                float a = lattice[y0 * 64 + x0], b = lattice[y0 * 64 + x1];
                float c = lattice[y1 * 64 + x0], d = lattice[y1 * 64 + x1];
                float top = a + (b - a) * tx, bot = c + (d - c) * tx;
                return top + (bot - top) * ty;
            }

            var data = new float[SIZE * SIZE];
            float mn = 1f, mx = 0f;
            for (int y = 0; y < SIZE; y++)
            for (int x = 0; x < SIZE; x++)
            {
                float u = x / (float)SIZE, v = y / (float)SIZE;
                float n = 0f, amp = 0.5f, freq = 6f;
                for (int o = 0; o < 4; o++) { n += amp * Sample(u * 64f, v * 64f, freq / 6f); amp *= 0.5f; freq *= 2f; }
                data[y * SIZE + x] = n;
                if (n < mn) mn = n;
                if (n > mx) mx = n;
            }
            float range = mx - mn;
            for (int i = 0; i < data.Length; i++) data[i] = (data[i] - mn) / range;
            return data;
        }

        /// <summary>Échantillonnage nearest + repeat — identique au sampling GPU (Point/Repeat).</summary>
        public static float SampleAt(float[] field, float u, float v)
        {
            int x = (((int)Mathf.Floor(u * SIZE)) % SIZE + SIZE) % SIZE;
            int y = (((int)Mathf.Floor(v * SIZE)) % SIZE + SIZE) % SIZE;
            return field[y * SIZE + x];
        }
    }
}
