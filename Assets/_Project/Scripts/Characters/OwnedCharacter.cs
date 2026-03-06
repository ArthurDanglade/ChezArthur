using System;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Données d'un personnage possédé par le joueur (niveau, spécialisation).
    /// </summary>
    [Serializable]
    public class OwnedCharacter
    {
        public string characterId;
        public int level;
        public SpecializationType specialization;

        /// <summary>
        /// Constructeur par défaut (pour la sérialisation).
        /// </summary>
        public OwnedCharacter()
        {
            characterId = string.Empty;
            level = 1;
            specialization = SpecializationType.None;
        }

        /// <summary>
        /// Constructeur avec ID.
        /// </summary>
        public OwnedCharacter(string id)
        {
            characterId = id;
            level = 1;
            specialization = SpecializationType.None;
        }

        /// <summary>
        /// Ajoute un niveau (doublon obtenu). Retourne true si level up effectué.
        /// </summary>
        public bool AddLevel(int maxLevel = 99)
        {
            if (level >= maxLevel) return false;
            level++;
            return true;
        }

        /// <summary>
        /// Définit la spécialisation (si pas déjà choisie ou si réversible).
        /// </summary>
        public void SetSpecialization(SpecializationType type)
        {
            specialization = type;
        }
    }
}
