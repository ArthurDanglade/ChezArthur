using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Localization
{
    /// <summary>
    /// Entrée clé → anglais d'une table de localisation.
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea(1, 3)]
        public string english;
    }

    /// <summary>
    /// Table d'overlay EN (le FR reste en place dans le jeu).
    /// </summary>
    [CreateAssetMenu(
        fileName = "LocalizationTable",
        menuName = "Chez Arthur/Localization/Localization Table",
        order = 30)]
    public class LocalizationTable : ScriptableObject
    {
        [SerializeField] private List<LocalizationEntry> entries = new List<LocalizationEntry>();

        /// <summary> Entrées de la table (lecture seule). </summary>
        public IReadOnlyList<LocalizationEntry> Entries => entries;

        /// <summary> Accès mutable pour les outils éditeur. </summary>
        public List<LocalizationEntry> EntriesMutable => entries;
    }
}
