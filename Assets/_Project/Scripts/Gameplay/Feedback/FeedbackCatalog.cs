using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Catalogue data-driven des bundles de feedback (défauts + overrides personnage).
    /// Créé uniquement via FeedbackCatalogBuilder — pas de CreateAssetMenu.
    /// </summary>
    public class FeedbackCatalog : ScriptableObject
    {
        public const int EventCount = 40;

        [System.Serializable]
        public class Entry
        {
            public FeedbackEventId eventId;
            public FeedbackBundle bundle = new FeedbackBundle();
        }

        [System.Serializable]
        public class CharacterOverride
        {
            public string characterId;
            public FeedbackEventId eventId;
            public FeedbackBundle bundle = new FeedbackBundle();
        }

        // ═══════════════════════════════════════════
        // SERIALIZED FIELDS
        // ═══════════════════════════════════════════
        [SerializeField] private List<Entry> entries = new List<Entry>();
        [SerializeField] private List<CharacterOverride> overrides = new List<CharacterOverride>();

        // ═══════════════════════════════════════════
        // RUNTIME INDEX
        // ═══════════════════════════════════════════
        private FeedbackBundle[] _byEvent;
        private Dictionary<(string, int), FeedbackBundle> _overrideMap;
        private bool _indexBuilt;

        public IReadOnlyList<Entry> Entries => entries;
        public IReadOnlyList<CharacterOverride> Overrides => overrides;

        // Accès éditeur (builder)
        public List<Entry> EntriesMutable => entries;
        public List<CharacterOverride> OverridesMutable => overrides;

        /// <summary>
        /// Construit l'index runtime une fois (appelé par le service à l'Awake).
        /// </summary>
        public void BuildRuntimeIndex()
        {
            if (_byEvent == null || _byEvent.Length != EventCount)
                _byEvent = new FeedbackBundle[EventCount];
            else
            {
                for (int i = 0; i < EventCount; i++)
                    _byEvent[i] = null;
            }

            if (_overrideMap == null)
                _overrideMap = new Dictionary<(string, int), FeedbackBundle>(overrides != null ? overrides.Count : 0);
            else
                _overrideMap.Clear();

            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Entry e = entries[i];
                    if (e == null || e.bundle == null)
                        continue;

                    int idx = (int)e.eventId;
                    if (idx < 0 || idx >= EventCount)
                        continue;

                    _byEvent[idx] = e.bundle;
                }
            }

            if (overrides != null)
            {
                for (int i = 0; i < overrides.Count; i++)
                {
                    CharacterOverride o = overrides[i];
                    if (o == null || o.bundle == null || string.IsNullOrEmpty(o.characterId))
                        continue;

                    _overrideMap[(o.characterId, (int)o.eventId)] = o.bundle;
                }
            }

            _indexBuilt = true;
        }

        /// <summary>
        /// Résout le bundle (override perso puis défaut). Null si absent.
        /// Zéro alloc par appel une fois l'index construit.
        /// </summary>
        public FeedbackBundle Resolve(FeedbackEventId eventId, string characterId)
        {
            if (!_indexBuilt)
                BuildRuntimeIndex();

            int idx = (int)eventId;
            if (idx < 0 || idx >= EventCount)
                return null;

            if (!string.IsNullOrEmpty(characterId) && _overrideMap != null)
            {
                if (_overrideMap.TryGetValue((characterId, idx), out FeedbackBundle ov))
                    return ov;
            }

            return _byEvent[idx];
        }
    }
}
