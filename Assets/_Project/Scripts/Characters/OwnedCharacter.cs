using System;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Données d'un personnage possédé par le joueur (niveau, index de spécialisation active).
    /// </summary>
    [Serializable]
    public class OwnedCharacter
    {
        public string characterId;
        public int level;
        /// <summary>-1 = spé de base, 0 = première alternative, 1 = deuxième, etc.</summary>
        public int activeSpecIndex = -1;

        /// <summary>
        /// Constructeur par défaut (pour la sérialisation).
        /// </summary>
        public OwnedCharacter()
        {
            characterId = string.Empty;
            level = 1;
            activeSpecIndex = -1;
        }

        /// <summary>
        /// Constructeur avec ID.
        /// </summary>
        public OwnedCharacter(string id)
        {
            characterId = id;
            level = 1;
            activeSpecIndex = -1;
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
        /// Définit l'index de spécialisation active (-1 = base, 0+ = alternative).
        /// </summary>
        public void SetSpecialization(int specIndex)
        {
            activeSpecIndex = specIndex;
        }

        /// <summary>
        /// Retourne l'index de spécialisation active (-1 = base, 0+ = alternative).
        /// </summary>
        public int GetSpecialization() => activeSpecIndex;

        /// <summary>
        /// True si le personnage utilise la spécialisation de base (index -1).
        /// </summary>
        public bool IsUsingBaseSpec() => activeSpecIndex == -1;
    }
}
