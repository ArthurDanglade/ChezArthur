using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Base de données de tous les personnages du jeu.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Chez Arthur/Character Database", order = 2)]
    public class CharacterDatabase : ScriptableObject
    {
        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private List<CharacterData> allCharacters = new List<CharacterData>();

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public IReadOnlyList<CharacterData> AllCharacters => allCharacters;
        public int Count => allCharacters.Count;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retourne un personnage par son ID.
        /// </summary>
        public CharacterData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return allCharacters.Find(c => c != null && c.Id == id);
        }

        /// <summary>
        /// Retourne tous les personnages d'une rareté donnée.
        /// </summary>
        public List<CharacterData> GetByRarity(CharacterRarity rarity)
        {
            return allCharacters.FindAll(c => c != null && c.Rarity == rarity);
        }

        /// <summary>
        /// Retourne tous les personnages d'un rôle donné.
        /// </summary>
        public List<CharacterData> GetByRole(CharacterRole role)
        {
            return allCharacters.FindAll(c => c != null && c.Role == role);
        }

        /// <summary>
        /// Vérifie si un personnage existe dans la base.
        /// </summary>
        public bool Exists(string id)
        {
            return GetById(id) != null;
        }
    }
}
