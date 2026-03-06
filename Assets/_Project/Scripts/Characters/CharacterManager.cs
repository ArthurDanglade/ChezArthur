using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Characters
{
    /// <summary>
    /// Gère les personnages possédés par le joueur et l'équipe sélectionnée.
    /// </summary>
    public class CharacterManager
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const int MAX_TEAM_SIZE = 4;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<OwnedCharacter> _ownedCharacters;
        private List<string> _selectedTeamIds;
        private CharacterDatabase _database;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        public event Action OnCharacterAdded;
        public event Action OnCharacterLevelUp;
        public event Action OnTeamChanged;

        // ═══════════════════════════════════════════
        // CONSTRUCTEUR
        // ═══════════════════════════════════════════
        public CharacterManager(CharacterDatabase database)
        {
            _database = database;
            _ownedCharacters = new List<OwnedCharacter>();
            _selectedTeamIds = new List<string>();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — CHARGEMENT/SAUVEGARDE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Charge les données depuis SaveData. Copie les éléments dans les listes internes.
        /// </summary>
        public void LoadFromSaveData(List<OwnedCharacter> owned, List<string> teamIds)
        {
            _ownedCharacters = new List<OwnedCharacter>();
            _selectedTeamIds = new List<string>();

            if (owned != null)
            {
                foreach (OwnedCharacter c in owned)
                {
                    if (c != null)
                        _ownedCharacters.Add(c);
                }
            }

            if (teamIds != null)
            {
                foreach (string id in teamIds)
                {
                    if (!string.IsNullOrEmpty(id))
                        _selectedTeamIds.Add(id);
                }
            }
        }

        /// <summary>
        /// Retourne les listes internes pour la sauvegarde (pas de copie).
        /// </summary>
        public (List<OwnedCharacter> owned, List<string> team) GetSaveData()
        {
            return (_ownedCharacters, _selectedTeamIds);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — PERSONNAGES POSSÉDÉS
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retourne tous les personnages possédés.
        /// </summary>
        public IReadOnlyList<OwnedCharacter> GetOwnedCharacters()
        {
            return _ownedCharacters;
        }

        /// <summary>
        /// Retourne un personnage possédé par son ID.
        /// </summary>
        public OwnedCharacter GetOwnedCharacter(string characterId)
        {
            return _ownedCharacters.Find(c => c.characterId == characterId);
        }

        /// <summary>
        /// Vérifie si le joueur possède un personnage.
        /// </summary>
        public bool OwnsCharacter(string characterId)
        {
            return GetOwnedCharacter(characterId) != null;
        }

        /// <summary>
        /// Ajoute un personnage (nouvelle obtention ou doublon).
        /// Retourne true si nouveau, false si doublon (level up).
        /// </summary>
        public bool AddCharacter(string characterId)
        {
            if (_database == null || !_database.Exists(characterId))
            {
                Debug.LogWarning($"[CharacterManager] Personnage inconnu : {characterId}");
                return false;
            }

            OwnedCharacter existing = GetOwnedCharacter(characterId);
            if (existing != null)
            {
                // Doublon → level up
                bool leveledUp = existing.AddLevel(CharacterData.MAX_LEVEL);
                if (leveledUp)
                {
                    OnCharacterLevelUp?.Invoke();
                }
                return false;
            }
            else
            {
                // Nouveau personnage
                OwnedCharacter newChar = new OwnedCharacter(characterId);
                _ownedCharacters.Add(newChar);
                OnCharacterAdded?.Invoke();
                return true;
            }
        }

        /// <summary>
        /// Retourne les données complètes d'un personnage possédé (CharacterData + OwnedCharacter).
        /// </summary>
        public (CharacterData data, OwnedCharacter owned) GetCharacterWithData(string characterId)
        {
            OwnedCharacter owned = GetOwnedCharacter(characterId);
            CharacterData data = _database != null ? _database.GetById(characterId) : null;
            return (data, owned);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — ÉQUIPE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Retourne les IDs de l'équipe sélectionnée.
        /// </summary>
        public IReadOnlyList<string> GetSelectedTeamIds()
        {
            return _selectedTeamIds;
        }

        /// <summary>
        /// Retourne l'équipe sélectionnée avec les données complètes.
        /// </summary>
        public List<(CharacterData data, OwnedCharacter owned)> GetSelectedTeam()
        {
            var team = new List<(CharacterData, OwnedCharacter)>();
            foreach (string id in _selectedTeamIds)
            {
                var charData = GetCharacterWithData(id);
                if (charData.data != null && charData.owned != null)
                {
                    team.Add(charData);
                }
            }
            return team;
        }

        /// <summary>
        /// Ajoute un personnage à l'équipe (si pas déjà dedans et équipe pas pleine).
        /// </summary>
        public bool AddToTeam(string characterId)
        {
            if (_selectedTeamIds.Count >= MAX_TEAM_SIZE) return false;
            if (_selectedTeamIds.Contains(characterId)) return false;
            if (!OwnsCharacter(characterId)) return false;

            _selectedTeamIds.Add(characterId);
            OnTeamChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Retire un personnage de l'équipe.
        /// </summary>
        public bool RemoveFromTeam(string characterId)
        {
            bool removed = _selectedTeamIds.Remove(characterId);
            if (removed)
            {
                OnTeamChanged?.Invoke();
            }
            return removed;
        }

        /// <summary>
        /// Vérifie si un personnage est dans l'équipe.
        /// </summary>
        public bool IsInTeam(string characterId)
        {
            return _selectedTeamIds.Contains(characterId);
        }

        /// <summary>
        /// Définit l'équipe complète (remplace l'existante).
        /// </summary>
        public void SetTeam(List<string> characterIds)
        {
            _selectedTeamIds.Clear();
            if (characterIds == null) return;

            foreach (string id in characterIds)
            {
                if (_selectedTeamIds.Count >= MAX_TEAM_SIZE) break;
                if (OwnsCharacter(id) && !_selectedTeamIds.Contains(id))
                {
                    _selectedTeamIds.Add(id);
                }
            }
            OnTeamChanged?.Invoke();
        }

        /// <summary>
        /// Vide l'équipe.
        /// </summary>
        public void ClearTeam()
        {
            _selectedTeamIds.Clear();
            OnTeamChanged?.Invoke();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — SPÉCIALISATION
        // ═══════════════════════════════════════════

        /// <summary>
        /// Définit la spécialisation d'un personnage.
        /// </summary>
        public bool SetSpecialization(string characterId, SpecializationType type)
        {
            OwnedCharacter owned = GetOwnedCharacter(characterId);
            if (owned == null) return false;

            CharacterData data = _database != null ? _database.GetById(characterId) : null;
            if (data == null) return false;

            int specLevel = data.GetSpecializationLevel();
            if (specLevel < 0 || owned.level < specLevel) return false;

            owned.SetSpecialization(type);
            return true;
        }
    }
}
