using System;
using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Characters;

namespace ChezArthur.Gacha
{
    /// <summary>
    /// Gère la logique de tirage gacha.
    /// </summary>
    public class GachaManager
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private Dictionary<string, int> _pityCounters = new Dictionary<string, int>();

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action<GachaPullResult> OnPullCompleted;

        // ═══════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════
        public GachaManager()
        {
            // Pity chargé via LoadPityData depuis PersistentManager
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Vérifie si le joueur peut effectuer un tirage.
        /// </summary>
        public bool CanPull(BannerData banner, bool isMulti)
        {
            if (banner == null) return false;
            if (!banner.IsActive()) return false;

            int cost = isMulti ? banner.CostMulti : banner.CostSingle;
            return PersistentManager.Instance != null && PersistentManager.Instance.Tals >= cost;
        }

        /// <summary>
        /// Effectue un tirage simple (x1).
        /// </summary>
        public GachaPullResult PullSingle(BannerData banner)
        {
            if (!CanPull(banner, false))
            {
                Debug.LogWarning("[GachaManager] Impossible de tirer : conditions non remplies.");
                return null;
            }

            if (!PersistentManager.Instance.SpendTals(banner.CostSingle))
            {
                Debug.LogWarning("[GachaManager] Échec SpendTals.");
                return null;
            }

            GachaPullResult result = new GachaPullResult
            {
                bannerId = banner.Id,
                talsSpent = banner.CostSingle
            };

            PulledCharacter pulled = RollCharacter(banner, false, null);
            if (pulled != null)
            {
                result.characters.Add(pulled);
            }

            ApplyPullResult(result);
            OnPullCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Effectue un tirage multiple (x10).
        /// </summary>
        public GachaPullResult PullMulti(BannerData banner)
        {
            if (!CanPull(banner, true))
            {
                Debug.LogWarning("[GachaManager] Impossible de tirer : conditions non remplies.");
                return null;
            }

            if (!PersistentManager.Instance.SpendTals(banner.CostMulti))
            {
                Debug.LogWarning("[GachaManager] Échec SpendTals.");
                return null;
            }

            IncrementPity(banner.Id);

            bool isPityTriggered = GetPityCount(banner.Id) >= banner.PityThreshold;

            GachaPullResult result = new GachaPullResult
            {
                bannerId = banner.Id,
                talsSpent = banner.CostMulti
            };

            // Dictionnaire pour suivre les niveaux pendant le multi
            Dictionary<string, int> tempLevels = new Dictionary<string, int>();

            for (int i = 0; i < 10; i++)
            {
                bool forceSSR = (i == 9 && isPityTriggered);
                PulledCharacter pulled = RollCharacter(banner, forceSSR, tempLevels);
                if (pulled != null)
                {
                    result.characters.Add(pulled);

                    // Mettre à jour le niveau temporaire pour les prochains tirages
                    tempLevels[pulled.characterId] = pulled.newLevel;
                }
            }

            if (result.characters.Exists(c => c.rarity == CharacterRarity.SSR))
            {
                ResetPity(banner.Id);
            }

            ApplyPullResult(result);
            OnPullCompleted?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Retourne le compteur de pity pour une bannière.
        /// </summary>
        public int GetPityCount(string bannerId)
        {
            return _pityCounters.TryGetValue(bannerId, out int count) ? count : 0;
        }

        /// <summary>
        /// Retourne les données de pity pour la sauvegarde.
        /// </summary>
        public Dictionary<string, int> GetPityData()
        {
            return new Dictionary<string, int>(_pityCounters);
        }

        /// <summary>
        /// Charge les données de pity depuis la sauvegarde.
        /// </summary>
        public void LoadPityData(Dictionary<string, int> data)
        {
            _pityCounters = data != null ? new Dictionary<string, int>(data) : new Dictionary<string, int>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — TIRAGE
        // ═══════════════════════════════════════════

        private PulledCharacter RollCharacter(BannerData banner, bool forceSSR, Dictionary<string, int> tempLevels = null)
        {
            CharacterRarity rarity;
            CharacterData character;

            if (forceSSR)
            {
                rarity = CharacterRarity.SSR;
                character = banner.RateUpSSR;
            }
            else
            {
                rarity = RollRarity(banner);
                character = RollCharacterFromPool(banner, rarity);
            }

            if (character == null)
            {
                Debug.LogWarning($"[GachaManager] Aucun personnage trouvé pour rareté {rarity}");
                return null;
            }

            // Vérifier si nouveau ou doublon
            bool ownsCharacter = PersistentManager.Instance.Characters.OwnsCharacter(character.Id);
            bool isInTempLevels = tempLevels != null && tempLevels.ContainsKey(character.Id);

            bool isNew = !ownsCharacter && !isInTempLevels;
            int previousLevel;
            int newLevel;

            if (isInTempLevels)
            {
                // Déjà tiré dans ce multi
                previousLevel = tempLevels[character.Id];
                newLevel = previousLevel + 1;
            }
            else if (ownsCharacter)
            {
                // Déjà possédé avant ce multi
                var owned = PersistentManager.Instance.Characters.GetOwnedCharacter(character.Id);
                previousLevel = owned.level;
                newLevel = previousLevel + 1;
            }
            else
            {
                // Nouveau personnage
                previousLevel = 0;
                newLevel = 1;
            }

            return new PulledCharacter
            {
                characterId = character.Id,
                rarity = rarity,
                isNew = isNew,
                isRateUp = (character == banner.RateUpSSR),
                previousLevel = previousLevel,
                newLevel = newLevel
            };
        }

        private CharacterRarity RollRarity(BannerData banner)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);

            if (roll < banner.RateLR)
            {
                return CharacterRarity.LR;
            }
            if (roll < banner.RateLR + banner.RateSSR)
            {
                return CharacterRarity.SSR;
            }
            return CharacterRarity.SR;
        }

        private CharacterData RollCharacterFromPool(BannerData banner, CharacterRarity rarity)
        {
            List<CharacterData> pool = rarity switch
            {
                CharacterRarity.LR => banner.LRPool,
                CharacterRarity.SSR => banner.RateUpSSR != null ? new List<CharacterData> { banner.RateUpSSR } : new List<CharacterData>(),
                CharacterRarity.SR => banner.SRPool,
                _ => null
            };

            if (pool == null || pool.Count == 0)
            {
                Debug.LogWarning($"[GachaManager] Pool vide pour rareté {rarity}");
                return null;
            }

            int index = UnityEngine.Random.Range(0, pool.Count);
            return pool[index];
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — PITY
        // ═══════════════════════════════════════════

        private void IncrementPity(string bannerId)
        {
            if (!_pityCounters.ContainsKey(bannerId))
            {
                _pityCounters[bannerId] = 0;
            }
            _pityCounters[bannerId]++;
        }

        private void ResetPity(string bannerId)
        {
            _pityCounters[bannerId] = 0;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES — APPLICATION
        // ═══════════════════════════════════════════

        private void ApplyPullResult(GachaPullResult result)
        {
            if (PersistentManager.Instance?.Characters == null) return;

            foreach (var pulled in result.characters)
            {
                if (string.IsNullOrEmpty(pulled.characterId)) continue;
                PersistentManager.Instance.Characters.AddCharacter(pulled.characterId);
            }

            PersistentManager.Instance.SaveGame();
        }
    }
}
