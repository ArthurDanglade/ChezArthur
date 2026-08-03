using UnityEngine;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Flash + flinch quand l'allié encaisse (portage défense d'EnemyHitReaction, sans wind-up).
    /// Le squash passe par CharacterBallFloat pour éviter deux écrivains LateUpdate.
    /// </summary>
    public class AllyHitReaction : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const int MIN_DAMAGE_TO_REACT = 5;
        private const float COOLDOWN_SECONDS = 0.1f;
        private const float FLASH_DURATION = 0.08f;
        private const string FlashShaderName = "ChezArthur/SpriteFlash";

        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static Material s_flashMaterial;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private CharacterBall _ball;
        private CharacterBallFloat _float;
        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _mpb;
        private float _flashTimer;
        private float _lastReactUnscaled = -999f;
        private System.Action<int> _damagedHandler;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Initialise l'abonnement dégâts et bascule le matériau SpriteFlash partagé.
        /// </summary>
        public void Initialize(CharacterBall ball)
        {
            _ball = ball;
            _float = GetComponent<CharacterBallFloat>();
            EnsureFlashMaterial();
            ResolveRenderer();

            if (_renderer != null && s_flashMaterial != null)
            {
                if (_renderer.sharedMaterial == null
                    || _renderer.sharedMaterial.shader == null
                    || _renderer.sharedMaterial.shader.name != FlashShaderName)
                {
                    _renderer.sharedMaterial = s_flashMaterial;
                }
            }

            if (_ball != null)
            {
                _damagedHandler = OnDamagedHandler;
                _ball.OnDamaged += _damagedHandler;
            }
        }

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════

        private void OnDestroy()
        {
            if (_ball != null && _damagedHandler != null)
                _ball.OnDamaged -= _damagedHandler;
        }

        private void LateUpdate()
        {
            if (_flashTimer <= 0f)
                return;

            if (_renderer == null)
                ResolveRenderer();

            _flashTimer -= Time.deltaTime;
            float amount = _flashTimer > 0f
                ? Mathf.Clamp01(_flashTimer / FLASH_DURATION)
                : 0f;
            SetFlash(amount);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void OnDamagedHandler(int amount)
        {
            if (amount < MIN_DAMAGE_TO_REACT)
                return;

            float now = Time.unscaledTime;
            if (now - _lastReactUnscaled < COOLDOWN_SECONDS)
                return;

            _lastReactUnscaled = now;
            _flashTimer = FLASH_DURATION;
            _float?.TriggerHitFlinch(1f);
        }

        private void ResolveRenderer()
        {
            if (_ball != null)
                _renderer = _ball.VisualRenderer;
        }

        private void SetFlash(float amount)
        {
            if (_renderer == null)
                return;

            if (_mpb == null)
                _mpb = new MaterialPropertyBlock();

            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FlashAmountId, amount);
            _renderer.SetPropertyBlock(_mpb);
        }

        private static void EnsureFlashMaterial()
        {
            if (s_flashMaterial != null)
                return;

            Shader shader = Shader.Find(FlashShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[AllyHitReaction] Shader {FlashShaderName} introuvable.");
                return;
            }

            s_flashMaterial = new Material(shader);
            s_flashMaterial.name = "AllyHitReaction_SpriteFlash_Shared";
            s_flashMaterial.SetFloat(FlashAmountId, 0f);
        }
    }
}
