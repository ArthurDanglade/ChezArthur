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
        public const int MAX_PRESETS = 5;

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private List<OwnedCharacter> _ownedCharacters;
        private List<string>[] _teamPresets;
        private int _activePresetIndex = 0;
        private CharacterDatabase _database;

        // ═══════════════════════════════════════════
        public int ActivePresetIndex => _activePresetIndex;

        // ═══════════════════════════════════════════
        // EVENTS
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
            _teamPresets = new List<string>[MAX_PRESETS];
            for (int i = 0; i < MAX_PRESETS; i++)
                _teamPresets[i] = new List<string>(MAX_TEAM_SIZE);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — CHARGEMENT/SAUVEGARDE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Charge les données depuis SaveData (5 presets + preset actif).
        /// </summary>
        public void LoadFromSaveData(
            List<OwnedCharacter> owned,
            int activePresetIndex,
            List<string> teamPreset0,
            List<string> teamPreset1,
            List<string> teamPreset2,
            List<string> teamPreset3,
            List<string> teamPreset4,
            List<string> legacySelectedTeamIds = null)
        {
            _ownedCharacters = new List<OwnedCharacter>();
            _teamPresets = new List<string>[MAX_PRESETS];
            for (int i = 0; i < MAX_PRESETS; i++)
                _teamPresets[i] = new List<string>(MAX_TEAM_SIZE);

            if (owned != null)
            {
                foreach (OwnedCharacter c in owned)
                {
                    if (c != null)
                        _ownedCharacters.Add(c);
                }
            }

            CopyPreset(teamPreset0, _teamPresets[0]);
            CopyPreset(teamPreset1, _teamPresets[1]);
            CopyPreset(teamPreset2, _teamPresets[2]);
            CopyPreset(teamPreset3, _teamPresets[3]);
            CopyPreset(teamPreset4, _teamPresets[4]);

            // Migration legacy : si aucun preset rempli mais ancienne équipe présente.
            if (AreAllPresetsEmpty() && legacySelectedTeamIds != null && legacySelectedTeamIds.Count > 0)
                CopyPreset(legacySelectedTeamIds, _teamPresets[0]);

            _activePresetIndex = Mathf.Clamp(activePresetIndex, 0, MAX_PRESETS - 1);
        }

        /// <summary>
        /// Retourne les listes internes pour la sauvegarde (pas de copie).
        /// </summary>
        public (
            List<OwnedCharacter> owned,
            int activePresetIndex,
            List<string> teamPreset0,
            List<string> teamPreset1,
            List<string> teamPreset2,
            List<string> teamPreset3,
            List<string> teamPreset4) GetSaveData()
        {
            return (
                _ownedCharacters,
                _activePresetIndex,
                _teamPresets[0],
                _teamPresets[1],
                _teamPresets[2],
                _teamPresets[3],
                _teamPresets[4]);
        }

        /// <summary>
        /// Change le preset actif (0-4).
        /// </summary>
        public void SwitchPreset(int index)
        {
            int clamped = Mathf.Clamp(index, 0, MAX_PRESETS - 1);
            if (clamped == _activePresetIndex) return;
            _activePresetIndex = clamped;
            OnTeamChanged?.Invoke();
        }

        /// <summary>
        /// Retourne les IDs d'équipe d'un preset spécifique.
        /// </summary>
        public IReadOnlyList<string> GetPresetTeamIds(int presetIndex)
        {
            int clamped = Mathf.Clamp(presetIndex, 0, MAX_PRESETS - 1);
            return _teamPresets[clamped];
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
            return _teamPresets[_activePresetIndex];
        }

        /// <summary>
        /// Retourne l'équipe sélectionnée avec les données complètes.
        /// </summary>
        public List<(CharacterData data, OwnedCharacter owned)> GetSelectedTeam()
        {
            var team = new List<(CharacterData, OwnedCharacter)>();
            List<string> activeTeam = _teamPresets[_activePresetIndex];
            foreach (string id in activeTeam)
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
            List<string> activeTeam = _teamPresets[_activePresetIndex];
            if (activeTeam.Count >= MAX_TEAM_SIZE) return false;
            if (activeTeam.Contains(characterId)) return false;
            if (!OwnsCharacter(characterId)) return false;

            activeTeam.Add(characterId);
            OnTeamChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Retire un personnage de l'équipe.
        /// </summary>
        public bool RemoveFromTeam(string characterId)
        {
            List<string> activeTeam = _teamPresets[_activePresetIndex];
            bool removed = activeTeam.Remove(characterId);
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
            return _teamPresets[_activePresetIndex].Contains(characterId);
        }

        /// <summary>
        /// Définit l'équipe complète (remplace l'existante).
        /// </summary>
        public void SetTeam(List<string> characterIds)
        {
            List<string> activeTeam = _teamPresets[_activePresetIndex];
            activeTeam.Clear();
            if (characterIds == null) return;

            foreach (string id in characterIds)
            {
                if (activeTeam.Count >= MAX_TEAM_SIZE) break;
                if (OwnsCharacter(id) && !activeTeam.Contains(id))
                {
                    activeTeam.Add(id);
                }
            }
            OnTeamChanged?.Invoke();
        }

        /// <summary>
        /// Vide l'équipe.
        /// </summary>
        public void ClearTeam()
        {
            _teamPresets[_activePresetIndex].Clear();
            OnTeamChanged?.Invoke();
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════
        private static void CopyPreset(List<string> source, List<string> target)
        {
            if (target == null) return;
            target.Clear();
            if (source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                string id = source[i];
                if (string.IsNullOrEmpty(id)) continue;
                if (target.Contains(id)) continue;
                if (target.Count >= MAX_TEAM_SIZE) break;
                target.Add(id);
            }
        }

        private bool AreAllPresetsEmpty()
        {
            for (int i = 0; i < MAX_PRESETS; i++)
            {
                if (_teamPresets[i] != null && _teamPresets[i].Count > 0)
                    return false;
            }
            return true;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES — SPÉCIALISATION
        // ═══════════════════════════════════════════

        /// <summary>
        /// Définit la spécialisation active d'un personnage par index.
        /// -1 = spé de base, 0+ = spé alternative.
        /// Vérifie que le personnage a le niveau requis.
        /// </summary>
        public bool SetSpecialization(string characterId, int specIndex)
        {
            OwnedCharacter owned = GetOwnedCharacter(characterId);
            if (owned == null) return false;

            CharacterData data = _database != null ? _database.GetById(characterId) : null;
            if (data == null) return false;

            if (specIndex == -1)
            {
                owned.SetSpecialization(-1);
                return true;
            }

            var available = data.GetAvailableSpecializations(owned.level);
            int listIndex = specIndex + 1;
            if (listIndex >= available.Count) return false;
            if (owned.level < available[listIndex].unlockLevel) return false;

            owned.SetSpecialization(specIndex);
            return true;
        }

        /// <summary>
        /// Retourne la SpecializationData active d'un personnage.
        /// </summary>
        public SpecializationData GetActiveSpecialization(string characterId)
        {
            var (data, owned) = GetCharacterWithData(characterId);
            if (data == null || owned == null) return null;
            return data.GetSpecialization(owned.GetSpecialization());
        }

        /// <summary>
        /// Retourne les spécialisations disponibles pour un personnage à son niveau actuel, avec indicateur actif.
        /// </summary>
        public List<(SpecializationData spec, int unlockLevel, bool isActive)> GetAvailableSpecializations(string characterId)
        {
            var result = new List<(SpecializationData spec, int unlockLevel, bool isActive)>();
            var (data, owned) = GetCharacterWithData(characterId);
            if (data == null || owned == null) return result;

            var available = data.GetAvailableSpecializations(owned.level);
            int activeIndex = owned.GetSpecialization();

            for (int i = 0; i < available.Count; i++)
            {
                int specIndex = i == 0 ? -1 : i - 1;
                bool isActive = (activeIndex == specIndex);
                result.Add((available[i].spec, available[i].unlockLevel, isActive));
            }
            return result;
        }
    }
}
