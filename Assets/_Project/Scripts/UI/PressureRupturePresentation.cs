using System.Collections;
using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Gameplay;

namespace ChezArthur.UI
{
    /// <summary>
    /// Orchestre l'annonce de Rupture : flash/bandeau élégant, SFX, shake, VFX optionnel.
    /// Les buffs gameplay restent gérés par RuptureEffectsSystem ; les VFX ennemis
    /// apparaissent en douceur via le délai auto du système d'effets.
    /// </summary>
    public class PressureRupturePresentation : MonoBehaviour
    {
        [Header("Annonce")]
        [SerializeField] private RuptureBannerUI ruptureBanner;
        [Tooltip("Fallback si le bandeau Rupture n'est pas monté.")]
        [SerializeField] private StageAnnouncerUI stageAnnouncer;
        [SerializeField] private string ruptureTitle = "RUPTURE !";
        [SerializeField] private string ruptureSubtitle = "La pression explose…";

        [Header("Audio")]
        [SerializeField] private AudioClip ruptureSfx;

        [Header("Impact")]
        [SerializeField] private CameraShake cameraShake;
        [SerializeField] [Range(0f, 1f)] private float ruptureShakeTrauma = 0.45f;

        [Header("VFX optionnel")]
        [Tooltip("Prefab VFX (ex. aura Aura 3) spawné au centre de l'arène.")]
        [SerializeField] private GameObject ruptureVfxPrefab;
        [SerializeField] private Transform vfxAnchor;
        [SerializeField] private float vfxLifetime = 2.5f;
        [Tooltip("Délai avant spawn VFX arène (aligné sur le pop titre).")]
        [SerializeField] private float vfxSpawnDelay = 0.2f;

        private bool _subscribed;
        private GameObject _activeVfx;
        private Coroutine _sequenceCoroutine;

        private void Start()
        {
            TrySubscribe();
            TryResolveRefs();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
                _sequenceCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void TryResolveRefs()
        {
            if (ruptureBanner == null)
                ruptureBanner = RuptureBannerUI.Instance != null
                    ? RuptureBannerUI.Instance
                    : Object.FindObjectOfType<RuptureBannerUI>(true);

            if (cameraShake == null)
                cameraShake = Object.FindObjectOfType<CameraShake>(true);
        }

        private void TrySubscribe()
        {
            if (_subscribed)
                return;

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure == null)
                return;

            pressure.OnRuptureTriggered += HandleRuptureTriggered;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null)
                pressure.OnRuptureTriggered -= HandleRuptureTriggered;

            _subscribed = false;
        }

        private void HandleRuptureTriggered()
        {
            TryResolveRefs();

            if (_sequenceCoroutine != null)
                StopCoroutine(_sequenceCoroutine);

            _sequenceCoroutine = StartCoroutine(PlayRuptureSequence());
        }

        private IEnumerator PlayRuptureSequence()
        {
            PlayRuptureSfx();

            if (cameraShake != null && ruptureShakeTrauma > 0f)
                cameraShake.AddTrauma(ruptureShakeTrauma);

            if (ruptureBanner != null)
            {
                // Bandeau + flash ; VFX arène légèrement après le début du pop.
                Coroutine bannerCo = StartCoroutine(
                    ruptureBanner.PlayAnnounceRoutine(ruptureTitle, ruptureSubtitle));

                if (ruptureVfxPrefab != null && vfxSpawnDelay > 0f)
                    yield return WaitUnscaled(vfxSpawnDelay);
                else if (ruptureVfxPrefab != null)
                    yield return null;

                SpawnRuptureVfx();

                yield return bannerCo;
            }
            else
            {
                // Fallback legacy
                if (stageAnnouncer != null)
                    stageAnnouncer.ShowDangerAnnounce(ruptureTitle, ruptureSubtitle);

                SpawnRuptureVfx();
                yield return WaitUnscaled(2f);
            }

            _sequenceCoroutine = null;
        }

        private void PlayRuptureSfx()
        {
            if (ruptureSfx == null)
                return;

            SfxManager sfx = SfxManager.Instance;
            if (sfx != null)
                sfx.PlaySfx(ruptureSfx);
        }

        private void SpawnRuptureVfx()
        {
            if (ruptureVfxPrefab == null)
                return;

            if (_activeVfx != null)
                Destroy(_activeVfx);

            Transform parent = vfxAnchor != null ? vfxAnchor : transform;
            _activeVfx = Instantiate(ruptureVfxPrefab, parent.position, Quaternion.identity, parent);
            if (vfxLifetime > 0f)
                Destroy(_activeVfx, vfxLifetime);
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
