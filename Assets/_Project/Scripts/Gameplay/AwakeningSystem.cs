using UnityEngine;
using ChezArthur.Audio;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Enemies;
using ChezArthur.UI;

namespace ChezArthur.Gameplay
{
    /// <summary>
    /// Détecte le premier kill de boss d'univers par un SSR vivant lié, marque l'éveil
    /// sur l'OwnedCharacter persisté et annonce via SynergyBannerUI.
    /// Singleton de scène (pattern CombatStatsTracker).
    /// </summary>
    public class AwakeningSystem : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Références scène")]
        [Tooltip("TurnManager n'a pas d'Instance — injection scène (même pattern que RunManager).")]
        [SerializeField] private TurnManager turnManager;
        [Tooltip("StageGenerator n'a pas d'Instance — injection scène (Gate 1).")]
        [SerializeField] private StageGenerator stageGenerator;

        [Header("Audio")]
        [SerializeField] private AudioClip awakeningJingle;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static AwakeningSystem _instance;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static AwakeningSystem Instance => _instance;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnEnable()
        {
            Enemy.OnBossDefeated += HandleBossDefeated;
        }

        private void OnDisable()
        {
            Enemy.OnBossDefeated -= HandleBossDefeated;
        }

        private void OnDestroy()
        {
            Enemy.OnBossDefeated -= HandleBossDefeated;

            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private void HandleBossDefeated()
        {
            if (turnManager == null || stageGenerator == null)
                return;

            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
                return;

            CharacterManager characters = PersistentManager.Instance.Characters;
            int universe = stageGenerator.CurrentUniverseIndex;
            bool anyAwakened = false;

            var allies = turnManager.GetAllies();
            if (allies == null)
                return;

            for (int i = 0; i < allies.Count; i++)
            {
                CharacterBall ally = allies[i];
                if (ally == null || ally.IsDead)
                    continue;

                CharacterData data = ally.Data;
                if (data == null)
                    continue;

                if (data.Rarity != CharacterRarity.SSR)
                    continue;

                if (data.UniverseIndex != universe)
                    continue;

                // Instance persistée (même référence que la liste CharacterManager — P1).
                OwnedCharacter owned = characters.GetOwnedCharacter(data.Id);
                if (owned == null || owned.isAwakened)
                    continue;

                owned.isAwakened = true;
                anyAwakened = true;
                Debug.Log($"[AwakeningSystem] Éveil : {data.Id} (univers {universe})");

                if (data.AnimatedPortraitPrime != null)
                {
                    SynergyBannerUI.Instance?.EnqueueAnnouncement(
                        data.CharacterName + " a vaincu son boss",
                        "Nouvel artwork débloqué",
                        CharacterRarityPalette.SSR);
                    SfxManager.Instance?.PlaySfx(awakeningJingle);
                }
            }

            if (anyAwakened)
                PersistentManager.Instance.SaveGame();
        }
    }
}
