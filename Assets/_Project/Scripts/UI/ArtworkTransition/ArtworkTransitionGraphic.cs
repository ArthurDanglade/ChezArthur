using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.ArtworkTransition
{
    /// <summary>
    /// Graphic UI de la carte artwork : un quad plein rect, material instancié, textures poussées.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class ArtworkTransitionGraphic : MaskableGraphic
    {
        // ═══════════════════════════════════════════
        // PROPERTY IDS (cachés — zéro string par frame)
        // ═══════════════════════════════════════════
        private static readonly int IdMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int IdBackTex = Shader.PropertyToID("_BackTex");
        private static readonly int IdNoiseTex = Shader.PropertyToID("_NoiseTex");
        private static readonly int IdFrontRect = Shader.PropertyToID("_FrontRect");
        private static readonly int IdBackRect = Shader.PropertyToID("_BackRect");
        private static readonly int IdArtSize = Shader.PropertyToID("_ArtSize");
        private static readonly int IdPixelSize = Shader.PropertyToID("_PixelSize");
        private static readonly int IdNoiseScale = Shader.PropertyToID("_NoiseScale");
        private static readonly int IdProgress = Shader.PropertyToID("_Progress");
        private static readonly int IdDirMode = Shader.PropertyToID("_DirMode");
        private static readonly int IdDirWeight = Shader.PropertyToID("_DirWeight");
        private static readonly int IdBand = Shader.PropertyToID("_Band");
        private static readonly int IdHybrid = Shader.PropertyToID("_Hybrid");
        private static readonly int IdWhiteFront = Shader.PropertyToID("_WhiteFront");
        private static readonly int IdBright = Shader.PropertyToID("_Bright");
        private static readonly int IdRim = Shader.PropertyToID("_Rim");
        private static readonly int IdRimColor = Shader.PropertyToID("_RimColor");
        private static readonly int IdEmberCool = Shader.PropertyToID("_EmberCool");
        private static readonly int IdEdgeGain = Shader.PropertyToID("_EdgeGain");
        private static readonly int IdJitter = Shader.PropertyToID("_Jitter");
        private static readonly int IdTimeSeq = Shader.PropertyToID("_TimeSeq");
        private static readonly int IdColor = Shader.PropertyToID("_Color");

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Material partagé")]
        [SerializeField] private Material sharedMaterial;

        [Header("Noise (optionnel — sinon via material)")]
        [SerializeField] private Texture noiseTexture;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Material _matInstance;
        private Texture _frontTex;
        private Texture _backTex;
        private Rect _frontRect = new Rect(0f, 0f, 1f, 1f);
        private Rect _backRect = new Rect(0f, 0f, 1f, 1f);
        private Vector2 _artSize = new Vector2(96f, 128f);

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>Texture de la frame avant courante.</summary>
        public override Texture mainTexture =>
            _frontTex != null ? _frontTex : s_WhiteTexture;

        /// <summary>Taille art en texels (frame avant).</summary>
        public Vector2 ArtSizeTexels => _artSize;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            EnsureMaterialInstance();
        }

        protected override void OnDestroy()
        {
            if (_matInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(_matInstance);
                else
                    DestroyImmediate(_matInstance);
                _matInstance = null;
            }

            base.OnDestroy();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>Pousse la frame avant (texture + UV normalisé) et met à jour _ArtSize.</summary>
        public void SetFrontFrame(Texture tex, Rect uvRect)
        {
            EnsureMaterialInstance();
            _frontTex = tex;
            _frontRect = uvRect;

            if (tex != null)
            {
                float w = Mathf.Max(1f, tex.width * Mathf.Abs(uvRect.width));
                float h = Mathf.Max(1f, tex.height * Mathf.Abs(uvRect.height));
                _artSize = new Vector2(w, h);
            }

            if (_matInstance != null)
            {
                _matInstance.SetTexture(IdMainTex, tex != null ? tex : s_WhiteTexture);
                _matInstance.SetVector(IdFrontRect, RectToVector(uvRect));
                _matInstance.SetVector(IdArtSize, new Vector4(_artSize.x, _artSize.y, 0f, 0f));
            }

            SetMaterialDirty();
        }

        /// <summary>Pousse la frame arrière (texture + UV normalisé).</summary>
        public void SetBackFrame(Texture tex, Rect uvRect)
        {
            EnsureMaterialInstance();
            _backTex = tex;
            _backRect = uvRect;

            if (_matInstance != null)
            {
                _matInstance.SetTexture(IdBackTex, tex != null ? tex : s_WhiteTexture);
                _matInstance.SetVector(IdBackRect, RectToVector(uvRect));
            }

            SetMaterialDirty();
        }

        /// <summary>
        /// Applique l'état de transition au material (floats / couleurs shader).
        /// Aucune allocation.
        /// </summary>
        public void ApplyState(in TransitionState s, ArtworkTransitionConfig c, float timeSeq)
        {
            EnsureMaterialInstance();
            if (_matInstance == null || c == null)
                return;

            if (noiseTexture != null)
                _matInstance.SetTexture(IdNoiseTex, noiseTexture);

            _matInstance.SetFloat(IdPixelSize, c.pixelSize);
            _matInstance.SetFloat(IdNoiseScale, c.noiseUvScale);
            _matInstance.SetFloat(IdProgress, s.progress);
            _matInstance.SetFloat(IdDirMode, s.consumeFromTop ? 1f : 0f);
            _matInstance.SetFloat(IdDirWeight, c.dirWeight);
            _matInstance.SetFloat(IdBand, c.frontBand);
            _matInstance.SetFloat(IdHybrid, s.hybrid);
            _matInstance.SetFloat(IdWhiteFront, s.whiteFront);
            _matInstance.SetFloat(IdBright, s.bright);
            _matInstance.SetFloat(IdRim, s.rim);
            _matInstance.SetColor(IdRimColor, s.rimColor);
            _matInstance.SetFloat(IdEmberCool, s.emberCool);
            _matInstance.SetFloat(IdEdgeGain, s.edgeGain);
            _matInstance.SetFloat(IdJitter, s.jitter);
            _matInstance.SetFloat(IdTimeSeq, timeSeq);
            _matInstance.SetColor(IdColor, color);
        }

        // ═══════════════════════════════════════════
        // MESH
        // ═══════════════════════════════════════════

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Color32 col = Color.white;

            vh.AddVert(new Vector3(r.xMin, r.yMin), col, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(r.xMin, r.yMax), col, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(r.xMax, r.yMax), col, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(r.xMax, r.yMin), col, new Vector2(1f, 0f));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void EnsureMaterialInstance()
        {
            if (_matInstance != null)
            {
                material = _matInstance;
                return;
            }

            if (sharedMaterial == null)
                return;

            _matInstance = new Material(sharedMaterial);
            _matInstance.name = sharedMaterial.name + " (Instance)";
            material = _matInstance;

            if (noiseTexture != null)
                _matInstance.SetTexture(IdNoiseTex, noiseTexture);
        }

        private static Vector4 RectToVector(Rect r) =>
            new Vector4(r.x, r.y, r.width, r.height);
    }
}
