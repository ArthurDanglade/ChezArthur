using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChezArthur.UI.InvocationFlow
{
    /// <summary>
    /// Pilote le voile pixel plein écran (Cover / Uncover / scrub).
    /// Matériau d'instance uniquement — temps non-scalé.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class PixelVeilController : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int CellsId = Shader.PropertyToID("_Cells");
        private static readonly int GlobalAlphaId = Shader.PropertyToID("_GlobalAlpha");

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Config")]
        [SerializeField] private InvocationFlowConfig config;

        [Header("Matériau partagé (PixelVeil.mat)")]
        [SerializeField] private Material sharedMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Image _image;
        private Material _mat;
        private Coroutine _routine;
        private float _progress;
        private bool _busy;
        private Action _pendingPeak;
        private Action _pendingDone;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public float Progress => _progress;
        public bool IsBusy => _busy;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.raycastTarget = false;

            if (sharedMaterial != null)
                _mat = new Material(sharedMaterial);
            else if (_image.material != null && _image.material.shader != null
                     && _image.material.shader.name == "ChezArthur/UI/PixelVeil")
                _mat = new Material(_image.material);
            else
            {
                Shader shader = Shader.Find("ChezArthur/UI/PixelVeil");
                _mat = shader != null ? new Material(shader) : null;
            }

            if (_mat != null)
            {
                _image.material = _mat;
                _mat.SetFloat(ProgressId, 0f);
                _mat.SetFloat(GlobalAlphaId, 1f);
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            StopRoutineInternal(invokeCallbacks: false);
            if (_mat != null)
            {
                if (Application.isPlaying)
                    Destroy(_mat);
                else
                    DestroyImmediate(_mat);
                _mat = null;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Couvre 0→1 sur veilDuration/2 (smoothstep). onPeak au sommet, onDone à la fin.
        /// </summary>
        public void Cover(Action onPeak, Action onDone)
        {
            StopRoutineInternal(invokeCallbacks: false);
            RecalcCells();
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            _pendingPeak = onPeak;
            _pendingDone = onDone;
            _routine = StartCoroutine(CoverRoutine());
        }

        /// <summary>Découvre 1→0 sur veilDuration/2 (smoothstep).</summary>
        public void Uncover(Action onDone)
        {
            StopRoutineInternal(invokeCallbacks: false);
            RecalcCells();
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            _pendingPeak = null;
            _pendingDone = onDone;
            _routine = StartCoroutine(UncoverRoutine());
        }

        /// <summary>
        /// Éval pure (scrub). p01 = fraction de timeline ;
        /// uncovering → Progress = 1 − p01, sinon Progress = p01.
        /// </summary>
        public void SetProgress(float p01, bool uncovering)
        {
            float p = Mathf.Clamp01(p01);
            ApplyProgress(uncovering ? 1f - p : p);
        }

        /// <summary>Stop + masque immédiat. Les callbacks en cours ne sont pas rejoués.</summary>
        public void HideImmediate()
        {
            StopRoutineInternal(invokeCallbacks: false);
            ApplyProgress(0f);
            if (_image != null)
                _image.enabled = false;
        }

        /// <summary>Alias Stop → HideImmediate (callbacks non rejoués).</summary>
        public void Stop() => HideImmediate();

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private IEnumerator CoverRoutine()
        {
            _busy = true;
            if (_image != null)
                _image.enabled = true;

            float dur = GetHalfDuration();
            float t = 0f;
            ApplyProgress(0f);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = dur > 0f ? Mathf.Clamp01(t / dur) : 1f;
                ApplyProgress(SmoothStep(u));
                yield return null;
            }

            ApplyProgress(1f);
            Action peak = _pendingPeak;
            _pendingPeak = null;
            peak?.Invoke();

            Action done = _pendingDone;
            _pendingDone = null;
            _busy = false;
            _routine = null;
            done?.Invoke();
        }

        private IEnumerator UncoverRoutine()
        {
            _busy = true;
            if (_image != null)
                _image.enabled = true;

            float dur = GetHalfDuration();
            float t = 0f;
            ApplyProgress(1f);

            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float u = dur > 0f ? Mathf.Clamp01(t / dur) : 1f;
                ApplyProgress(1f - SmoothStep(u));
                yield return null;
            }

            ApplyProgress(0f);
            if (_image != null)
                _image.enabled = false;

            Action done = _pendingDone;
            _pendingDone = null;
            _busy = false;
            _routine = null;
            done?.Invoke();
        }

        private void StopRoutineInternal(bool invokeCallbacks)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _busy = false;
            if (invokeCallbacks)
            {
                _pendingPeak?.Invoke();
                _pendingDone?.Invoke();
            }

            _pendingPeak = null;
            _pendingDone = null;
        }

        private void RecalcCells()
        {
            if (_mat == null)
                return;

            float cell = config != null ? Mathf.Max(1f, config.veilCellSize) : 14f;
            RectTransform rt = _image != null ? _image.rectTransform : null;
            float w = rt != null ? Mathf.Max(1f, rt.rect.width) : Screen.width;
            float h = rt != null ? Mathf.Max(1f, rt.rect.height) : Screen.height;
            float cols = Mathf.Max(1f, Mathf.Ceil(w / cell));
            float rows = Mathf.Max(1f, Mathf.Ceil(h / cell));
            _mat.SetVector(CellsId, new Vector4(cols, rows, 0f, 0f));
        }

        private void ApplyProgress(float p01)
        {
            _progress = Mathf.Clamp01(p01);
            if (_mat != null)
                _mat.SetFloat(ProgressId, _progress);
            if (_image != null && _progress > 0.0001f)
                _image.enabled = true;
        }

        private float GetHalfDuration()
        {
            float full = config != null ? config.veilDuration : 0.70f;
            return Mathf.Max(0.0001f, full * 0.5f);
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
