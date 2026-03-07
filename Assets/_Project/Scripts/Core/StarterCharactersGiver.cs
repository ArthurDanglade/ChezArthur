using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.Core
{
    /// <summary>
    /// Donne les personnages de départ au joueur lors du premier lancement.
    /// </summary>
    public class StarterCharactersGiver : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [Header("Personnages de départ")]
        [SerializeField] private List<CharacterData> starterCharacters = new List<CharacterData>();

        [Header("Configuration")]
        [SerializeField] private bool giveOnStart = true;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Start()
        {
            if (giveOnStart)
            {
                TryGiveStarters();
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Donne les personnages de départ si le joueur n'en a pas encore.
        /// </summary>
        public void TryGiveStarters()
        {
            if (PersistentManager.Instance == null)
            {
                Debug.LogWarning("[StarterCharactersGiver] PersistentManager non trouvé.");
                return;
            }

            if (PersistentManager.Instance.Characters == null)
            {
                Debug.LogWarning("[StarterCharactersGiver] CharacterManager non initialisé.");
                return;
            }

            // Vérifie si le joueur a déjà des personnages
            var owned = PersistentManager.Instance.Characters.GetOwnedCharacters();
            if (owned != null && owned.Count > 0)
            {
                Debug.Log("[StarterCharactersGiver] Le joueur a déjà des personnages, pas de starters donnés.");
                return;
            }

            // Donne les starters
            int given = 0;
            foreach (var character in starterCharacters)
            {
                if (character == null) continue;

                bool isNew = PersistentManager.Instance.Characters.AddCharacter(character.Id);
                if (isNew)
                {
                    Debug.Log($"[StarterCharactersGiver] Personnage donné : {character.CharacterName}");
                    given++;
                }
            }

            // Sauvegarde si on a donné des personnages
            if (given > 0)
            {
                PersistentManager.Instance.SaveGame();
                Debug.Log($"[StarterCharactersGiver] {given} personnages de départ donnés !");
            }
        }

        /// <summary>
        /// Force le don des starters (pour debug, ignore la vérification).
        /// </summary>
        [ContextMenu("Force Give Starters")]
        public void ForceGiveStarters()
        {
            if (PersistentManager.Instance == null || PersistentManager.Instance.Characters == null)
            {
                Debug.LogError("[StarterCharactersGiver] Managers non disponibles.");
                return;
            }

            foreach (var character in starterCharacters)
            {
                if (character == null) continue;
                PersistentManager.Instance.Characters.AddCharacter(character.Id);
                Debug.Log($"[StarterCharactersGiver] Personnage forcé : {character.CharacterName}");
            }

            PersistentManager.Instance.SaveGame();
        }
    }
}
