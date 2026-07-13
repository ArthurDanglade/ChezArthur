using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Characters;
using ChezArthur.Enemies;
using ChezArthur.Gameplay.Buffs;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Applique les effets ennemis et alliés pendant la Rupture de pression (Gates 5–6).
    /// Ennemis : buffs ATK / résistance / force de lancer + halo rouge pulsant.
    /// Alliés : salves DEF, soin SUP, bonus ATK ; refus du switch de spé joueur.
    /// </summary>
    public class RuptureEffectsSystem : MonoBehaviour
    {
        private const string RuptureAtkBuffId = "rupture_atk";
        private const string RuptureResistanceBuffId = "rupture_resistance";
        private const string RuptureLaunchForceBuffId = "rupture_launchforce";
        private const string RuptureDefShieldBuffId = "rupture_def_shield";
        private const string RuptureAtkAllyBuffId = "rupture_atk_ally";

        private static RuptureEffectsSystem _instance;

        /// <summary> Instance unique de la scène courante. </summary>
        public static RuptureEffectsSystem Instance => _instance;

        [Header("Buffs ennemis (calibrage Gate 8)")]
        [SerializeField] private float atkBonusPercent = 0.25f;
        [SerializeField] private float damageReductionPercent = 0.25f;
        [SerializeField] private float launchForceBonusPercent = 0.30f;

        [Header("Halo")]
        [Tooltip("Sprite du halo. Null = glow radial généré procéduralement.")]
        [SerializeField] private Sprite haloSpriteOverride;
        [SerializeField] private Color haloColor = new Color(1f, 0f, 0f, 0.55f);
        [Tooltip("Échelle locale du halo, relative au visuel ennemi (parenté).")]
        [SerializeField] private float haloScale = 1.6f;
        [SerializeField] private float pulseSpeed = 2.2f;
        [SerializeField] private float pulseAmplitude = 0.15f;
        [SerializeField] private int haloSortingOffset = -1;

        [Header("Effets alliés (calibrage Gate 8)")]
        [Tooltip("Bouclier accordé à toute l'équipe = % des HP max du DEF qui joue sa salve.")]
        [SerializeField] private float defShieldPercent = 0.15f;
        [Tooltip("Soin d'équipe unique par rupture = % des HP max de chaque allié.")]
        [SerializeField] private float supHealPercent = 0.15f;
        [Tooltip("Bonus ATK cumulatif par allié ATK vivant (spé active), appliqué au perso ATK en tour.")]
        [SerializeField] private float atkPerAtkAllyPercent = 0.10f;

        [Header("Audio (null toléré)")]
        [SerializeField] private AudioClip defSalveClip;
        [SerializeField] private AudioClip supHealClip;
        [SerializeField] private AudioClip switchDeniedClip;

        private TurnManager _turnManager;
        private bool _ruptureActive;
        private bool _supHealDone;
        private readonly HashSet<CharacterBall> _defSalveDone = new HashSet<CharacterBall>();
        private Transform _haloPoolContainer;
        private Sprite _runtimeHaloSprite;
        private Texture2D _runtimeHaloTexture;

        private readonly List<SpriteRenderer> _haloPool = new List<SpriteRenderer>(16);

        private struct TrackedEnemyEntry
        {
            public Enemy Enemy;
            public SpriteRenderer HaloRenderer;
        }

        private readonly List<TrackedEnemyEntry> _trackedEnemies = new List<TrackedEnemyEntry>(16);

        /// <summary> Déclenché quand le joueur tente un switch de spé refusé pendant la Rupture (Gate 7). </summary>
        public event Action OnSpecSwitchDenied;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;

            var poolGo = new GameObject("RuptureHaloPool");
            poolGo.transform.SetParent(transform, false);
            _haloPoolContainer = poolGo.transform;

            if (haloSpriteOverride == null)
                CreateProceduralHaloSprite();
        }

        private void OnDestroy()
        {
            Cleanup();

            if (_instance == this)
                _instance = null;

            if (_runtimeHaloSprite != null)
                Destroy(_runtimeHaloSprite);

            if (_runtimeHaloTexture != null)
                Destroy(_runtimeHaloTexture);
        }

        private void Update()
        {
            if (!_ruptureActive)
                return;

            float alphaPulse = haloColor.a + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude;
            alphaPulse = Mathf.Clamp01(alphaPulse);

            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
            {
                TrackedEnemyEntry entry = _trackedEnemies[i];
                Enemy enemy = entry.Enemy;

                if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
                {
                    RemoveTrackedAt(i);
                    continue;
                }

                SpriteRenderer halo = entry.HaloRenderer;
                if (halo == null)
                    continue;

                Color c = halo.color;
                c.r = haloColor.r;
                c.g = haloColor.g;
                c.b = haloColor.b;
                c.a = alphaPulse;
                halo.color = c;
            }
        }

        /// <summary> Branche le système sur le TurnManager (ex. RunManager.StartRun). </summary>
        public void Initialize(TurnManager turnManager)
        {
            Cleanup();
            _turnManager = turnManager;

            if (_turnManager != null)
            {
                _turnManager.OnEnemyAddedMidCombat += HandleEnemyAddedMidCombat;
                _turnManager.OnTurnChanged += HandleTurnStarted;
            }

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null)
            {
                pressure.OnRuptureTriggered += HandleRuptureTriggered;
                pressure.OnRuptureEnded += HandleRuptureEnded;
                _ruptureActive = pressure.IsInRupture;

                if (_ruptureActive)
                    ApplyToAllEnemies();
            }
            else
            {
                _ruptureActive = false;
            }
        }

        /// <summary> Désabonne le système des événements combat / pression. </summary>
        public void Cleanup()
        {
            if (_ruptureActive)
                RemoveFromAll();

            _ruptureActive = false;

            if (_turnManager != null)
            {
                _turnManager.OnEnemyAddedMidCombat -= HandleEnemyAddedMidCombat;
                _turnManager.OnTurnChanged -= HandleTurnStarted;
            }

            _turnManager = null;

            PressureGaugeSystem pressure = PressureGaugeSystem.Instance;
            if (pressure != null)
            {
                pressure.OnRuptureTriggered -= HandleRuptureTriggered;
                pressure.OnRuptureEnded -= HandleRuptureEnded;
            }
        }

        /// <summary>
        /// Signale un switch de spé refusé pendant la Rupture (verrou joueur via DragDropController).
        /// </summary>
        public void NotifySpecSwitchDenied()
        {
            Debug.Log("[Pression] Switch de spé refusé — Rupture active.");
            PlaySfxClip(switchDeniedClip);
            OnSpecSwitchDenied?.Invoke();
        }

        private void ResetAllyRuptureState()
        {
            _defSalveDone.Clear();
            _supHealDone = false;
        }

        private void HandleRuptureTriggered()
        {
            _ruptureActive = true;
            ResetAllyRuptureState();
            ApplyToAllEnemies();
        }

        private void HandleRuptureEnded()
        {
            _ruptureActive = false;
            ResetAllyRuptureState();
            RemoveFromAll();
        }

        private void HandleTurnStarted(ITurnParticipant p)
        {
            if (!_ruptureActive || p == null || p.IsDead)
                return;

            if (p is not CharacterBall ball)
                return;

            SpecializationData activeSpec = ball.ActiveSpec;
            if (activeSpec == null)
                return;

            switch (activeSpec.Role)
            {
                case CharacterRole.Defender:
                    ApplyDefSalveIfNeeded(ball);
                    break;
                case CharacterRole.Support:
                    ApplySupHealIfNeeded(ball);
                    break;
                case CharacterRole.Attacker:
                    ApplyAtkAllyBonus(ball);
                    break;
            }
        }

        private void ApplyDefSalveIfNeeded(CharacterBall defBall)
        {
            if (defBall == null || defBall.IsDead || _turnManager == null)
                return;

            if (!_defSalveDone.Add(defBall))
                return;

            int shieldAmount = Mathf.RoundToInt(defBall.MaxHp * defShieldPercent);
            if (shieldAmount <= 0)
                return;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies != null)
            {
                for (int i = 0; i < allies.Count; i++)
                {
                    CharacterBall ally = allies[i];
                    if (ally == null || ally.IsDead)
                        continue;

                    BuffReceiver br = ally.BuffReceiver;
                    if (br == null)
                        continue;

                    br.AddBuff(new BuffData
                    {
                        BuffId = RuptureDefShieldBuffId,
                        Source = defBall,
                        StatType = BuffStatType.Shield,
                        Value = shieldAmount,
                        IsPercent = false,
                        RemainingTurns = -1,
                        RemainingCycles = -1,
                        UniquePerSource = true,
                        UniqueGlobal = false
                    });
                }
            }

            Debug.Log(
                $"[Rupture] Salve de protection de {defBall.Name} : bouclier {shieldAmount} " +
                "pour toute l'équipe.");
            PlaySfxClip(defSalveClip);
        }

        private void ApplySupHealIfNeeded(CharacterBall supBall)
        {
            if (supBall == null || supBall.IsDead || _turnManager == null)
                return;

            if (_supHealDone)
                return;

            _supHealDone = true;
            _turnManager.HealAllAllies(supHealPercent);

            Debug.Log(
                $"[Rupture] Soin d'équipe par {supBall.Name} : " +
                $"{supHealPercent * 100f:F0}% des HP max de chaque allié.");
            PlaySfxClip(supHealClip);
        }

        private void ApplyAtkAllyBonus(CharacterBall atkBall)
        {
            if (atkBall == null || atkBall.IsDead)
                return;

            int atkAllyCount = CountAliveAtkAllies();
            if (atkAllyCount <= 0)
                return;

            BuffReceiver br = atkBall.BuffReceiver;
            if (br == null)
                return;

            float atkBonus = atkAllyCount * atkPerAtkAllyPercent;

            br.RemoveBuffsById(RuptureAtkAllyBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureAtkAllyBuffId,
                Source = atkBall,
                StatType = BuffStatType.ATK,
                Value = atkBonus,
                IsPercent = true,
                RemainingTurns = 1,
                RemainingCycles = -1,
                UniquePerSource = true,
                UniqueGlobal = false
            });

            Debug.Log(
                $"[Rupture] {atkBall.Name} : ATK +{atkBonus * 100f:F0}% ({atkAllyCount} perso(s) ATK)");
        }

        private int CountAliveAtkAllies()
        {
            if (_turnManager == null)
                return 0;

            IReadOnlyList<CharacterBall> allies = _turnManager.GetAllies();
            if (allies == null)
                return 0;

            int count = 0;
            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                SpecializationData spec = ally.ActiveSpec;
                if (spec != null && spec.Role == CharacterRole.Attacker)
                    count++;
            }

            return count;
        }

        private void PlaySfxClip(AudioClip clip)
        {
            if (clip == null)
                return;

            SfxManager sfx = SfxManager.Instance;
            if (sfx != null)
                sfx.PlaySfx(clip);
        }

        private void HandleEnemyAddedMidCombat(Enemy enemy)
        {
            if (!_ruptureActive)
                return;

            ApplyToEnemy(enemy);
        }

        private void ApplyToAllEnemies()
        {
            if (_turnManager == null)
                return;

            int applied = 0;
            IReadOnlyList<ITurnParticipant> participants = _turnManager.Participants;
            if (participants != null)
            {
                for (int i = 0; i < participants.Count; i++)
                {
                    if (participants[i] is Enemy enemy && ApplyToEnemy(enemy))
                        applied++;
                }
            }

            Debug.Log(
                $"[Rupture] Effets appliqués à {applied} ennemis " +
                $"(+{atkBonusPercent * 100f:F0}% ATK, +{damageReductionPercent * 100f:F0}% résistance, " +
                $"+{launchForceBonusPercent * 100f:F0}% force)");
        }

        private bool ApplyToEnemy(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead)
                return false;

            if (IsEnemyTracked(enemy))
                return false;

            ApplyBuffsToEnemy(enemy);

            SpriteRenderer haloRenderer = AcquireHalo();
            if (haloRenderer != null)
            {
                Transform haloTransform = haloRenderer.transform;
                haloTransform.SetParent(enemy.transform, false);
                haloTransform.localPosition = Vector3.zero;
                haloTransform.localScale = Vector3.one * haloScale;

                Sprite sprite = haloSpriteOverride != null ? haloSpriteOverride : _runtimeHaloSprite;
                haloRenderer.sprite = sprite;

                Color c = haloColor;
                haloRenderer.color = c;

                SpriteRenderer enemyRenderer = enemy.GetComponentInChildren<SpriteRenderer>();
                if (enemyRenderer != null)
                {
                    haloRenderer.sortingLayerID = enemyRenderer.sortingLayerID;
                    haloRenderer.sortingOrder = enemyRenderer.sortingOrder + haloSortingOffset;
                }

                haloRenderer.gameObject.SetActive(true);
            }

            _trackedEnemies.Add(new TrackedEnemyEntry
            {
                Enemy = enemy,
                HaloRenderer = haloRenderer
            });

            return true;
        }

        private void RemoveFromEnemy(Enemy enemy)
        {
            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
            {
                if (_trackedEnemies[i].Enemy == enemy)
                {
                    RemoveTrackedAt(i);
                    return;
                }
            }
        }

        private void RemoveFromAll()
        {
            int count = _trackedEnemies.Count;

            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
                RemoveTrackedAt(i);

            if (count > 0)
                Debug.Log($"[Rupture] Effets retirés de {count} ennemis");
        }

        private void RemoveTrackedAt(int index)
        {
            if (index < 0 || index >= _trackedEnemies.Count)
                return;

            TrackedEnemyEntry entry = _trackedEnemies[index];
            Enemy enemy = entry.Enemy;

            if (enemy != null && !enemy.IsDead)
                RemoveBuffsFromEnemy(enemy);

            ReleaseHalo(entry.HaloRenderer);
            _trackedEnemies.RemoveAt(index);
        }

        private void ApplyBuffsToEnemy(Enemy enemy)
        {
            BuffReceiver br = enemy.BuffReceiver;
            if (br == null)
                return;

            br.RemoveBuffsById(RuptureAtkBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureAtkBuffId,
                Source = null,
                StatType = BuffStatType.ATK,
                Value = atkBonusPercent,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            br.RemoveBuffsById(RuptureResistanceBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureResistanceBuffId,
                Source = null,
                StatType = BuffStatType.DamageReduction,
                Value = damageReductionPercent,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });

            br.RemoveBuffsById(RuptureLaunchForceBuffId);
            br.AddBuff(new BuffData
            {
                BuffId = RuptureLaunchForceBuffId,
                Source = null,
                StatType = BuffStatType.LaunchForce,
                Value = launchForceBonusPercent,
                IsPercent = true,
                RemainingTurns = -1,
                RemainingCycles = -1,
                UniquePerSource = false,
                UniqueGlobal = true
            });
        }

        private void RemoveBuffsFromEnemy(Enemy enemy)
        {
            BuffReceiver br = enemy.BuffReceiver;
            if (br == null)
                return;

            br.RemoveBuffsById(RuptureAtkBuffId);
            br.RemoveBuffsById(RuptureResistanceBuffId);
            br.RemoveBuffsById(RuptureLaunchForceBuffId);
        }

        private bool IsEnemyTracked(Enemy enemy)
        {
            for (int i = 0; i < _trackedEnemies.Count; i++)
            {
                if (_trackedEnemies[i].Enemy == enemy)
                    return true;
            }

            return false;
        }

        private SpriteRenderer AcquireHalo()
        {
            int lastIndex = _haloPool.Count - 1;
            if (lastIndex >= 0)
            {
                SpriteRenderer pooled = _haloPool[lastIndex];
                _haloPool.RemoveAt(lastIndex);
                return pooled;
            }

            var haloGo = new GameObject("RuptureHalo");
            haloGo.transform.SetParent(_haloPoolContainer, false);
            var renderer = haloGo.AddComponent<SpriteRenderer>();
            haloGo.SetActive(false);
            return renderer;
        }

        private void ReleaseHalo(SpriteRenderer haloRenderer)
        {
            if (haloRenderer == null)
                return;

            haloRenderer.transform.SetParent(_haloPoolContainer, false);
            haloRenderer.gameObject.SetActive(false);
            _haloPool.Add(haloRenderer);
        }

        private void CreateProceduralHaloSprite()
        {
            const int size = 64;

            _runtimeHaloTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            // Exception volontaire au Point — c'est un glow, pas du pixel art combat.
            _runtimeHaloTexture.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float maxDist = center;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxDist;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha * (3f - 2f * alpha);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            _runtimeHaloTexture.SetPixels(pixels);
            _runtimeHaloTexture.Apply();

            _runtimeHaloSprite = Sprite.Create(
                _runtimeHaloTexture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                64f);
        }
    }
}
