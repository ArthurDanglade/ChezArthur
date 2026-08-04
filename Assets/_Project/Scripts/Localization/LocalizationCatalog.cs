using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Localization
{
    /// <summary>
    /// Catalogue des tables EN, chargé via Resources.Load("LocalizationCatalog").
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocalizationCatalog",
        menuName = "Chez Arthur/Localization/Localization Catalog",
        order = 31)]
    public class LocalizationCatalog : ScriptableObject
    {
        [SerializeField] private List<LocalizationTable> tables = new List<LocalizationTable>();

        /// <summary> Tables référencées. </summary>
        public IReadOnlyList<LocalizationTable> Tables => tables;

        /// <summary> Accès mutable pour les outils éditeur. </summary>
        public List<LocalizationTable> TablesMutable => tables;

        /// <summary>
        /// Fusionne toutes les tables dans le dictionnaire cible.
        /// Clé dupliquée → warning, dernière gagne. Null-safe.
        /// </summary>
        public void BuildDictionary(Dictionary<string, string> into)
        {
            if (into == null)
                return;

            if (tables == null)
                return;

            for (int t = 0; t < tables.Count; t++)
            {
                LocalizationTable table = tables[t];
                if (table == null || table.Entries == null)
                    continue;

                IReadOnlyList<LocalizationEntry> entries = table.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    LocalizationEntry entry = entries[i];
                    if (entry == null || string.IsNullOrEmpty(entry.key))
                        continue;

                    string value = entry.english ?? "";
                    if (into.ContainsKey(entry.key))
                    {
                        Debug.LogWarning(
                            $"[Loc] Clé dupliquée « {entry.key} » — dernière table gagne ({table.name}).");
                    }

                    into[entry.key] = value;
                }
            }
        }
    }
}
