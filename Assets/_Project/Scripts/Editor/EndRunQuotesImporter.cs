#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Importe les répliques de fin de run SR-LR dans les assets CharacterData.
    /// SSR-LR : table embarquée. SR : fichier Tools/end_run_quotes_sr.json (hors persos supprimés / non implémentés).
    /// </summary>
    public static class EndRunQuotesImporter
    {
        private const string SrQuotesJsonPath = "Tools/end_run_quotes_sr.json";

        private readonly struct QuoteSet
        {
            public readonly string DisplayName;
            public readonly string Rank1;
            public readonly string Mid;
            public readonly string Last;

            public QuoteSet(string displayName, string rank1, string mid, string last)
            {
                DisplayName = displayName;
                Rank1 = rank1;
                Mid = mid;
                Last = last;
            }
        }

        [Serializable]
        private class SrQuotesFile
        {
            public SrQuoteEntry[] characters;
        }

        [Serializable]
        private class SrQuoteEntry
        {
            public string displayName;
            public string rank1;
            public string mid;
            public string last;
        }

        /// <summary> SSR + LR embarqués (verbatim Gate 3). Faille / Iflo exclus (non implémentés). </summary>
        private static readonly QuoteSet[] SsrLrQuoteTable =
        {
            new QuoteSet(
                "Ardacula",
                "Mon règne ne cessera jamais. Je suis immortel, je règnerai sur tous les univers.",
                "Satisfaisant. Mais je mérite mieux. Je mérite toujours mieux.",
                "Je me vengerai. J'arrête pas de me venger, ça en devient fatiguant."),
            new QuoteSet(
                "Troplin",
                "Premier ! Une petite bière pour fêter ça ?",
                "Ma foi, je m'en contente. Petite bière pour fêter ça quand même ?",
                "Bon dernier... Petite bière pour me consoler ?"),
            new QuoteSet(
                "Don Costardo",
                "Mon empire s'étend. Je contrôlerai bientôt le premier cartel inter-univers.",
                "Acceptable. Dans mon cartel les 'acceptable' finissaient au fond de l'eau.",
                "Répétez ce classement à personne. Personne."),
            new QuoteSet(
                "L'Ancien N°1",
                "Comme au bon vieux temps. Je retrouve de bonnes sensations.",
                "J'ai été meilleur. Avant.",
                "Sérieusement, l'immortalité c'est nul. Faudrait rester jeune en même temps."),
            new QuoteSet(
                "Goat",
                "Bêêêêê.",
                "Bêêêêê.",
                "Bêêêêê."),
            new QuoteSet(
                "Brooke Heune",
                "Me voilà honorée de cette place. Voilà qui nourrira ma légende.",
                "Je laisse les autres exister. C'est généreux de ma part.",
                "Dernière. Je devrais tous vous éliminer pour ne pas salir ma légende."),
        };

        [MenuItem("Chez Arthur/Répliques/Importer répliques fin de run (SR-LR)")]
        public static void ImportEndRunQuotes()
        {
            CharacterData[] allCharacters = LoadAllCharacterData();
            QuoteSet[] allQuotes = BuildQuoteTable();
            int writtenCount = 0;
            int upToDateCount = 0;
            int missingCount = 0;
            int skippedEmptyCount = 0;
            bool anyWrite = false;

            for (int i = 0; i < allQuotes.Length; i++)
            {
                QuoteSet quotes = allQuotes[i];
                if (IsQuoteSetEmpty(quotes))
                {
                    skippedEmptyCount++;
                    Debug.LogWarning($"[QuotesImporter] ⚠ PAS DE RÉPLIQUES : {quotes.DisplayName}");
                    continue;
                }

                if (!TryFindCharacter(allCharacters, quotes.DisplayName, out CharacterData character))
                {
                    missingCount++;
                    Debug.LogWarning($"[QuotesImporter] ⚠ INTROUVABLE : {quotes.DisplayName}");
                    continue;
                }

                if (!TryWriteQuotes(character, quotes, out bool wrote))
                    continue;

                if (wrote)
                {
                    writtenCount++;
                    anyWrite = true;
                    Debug.Log(
                        $"[QuotesImporter] {character.CharacterName} ({character.Id}) : 3 répliques écrites");
                }
                else
                {
                    upToDateCount++;
                    Debug.Log(
                        $"[QuotesImporter] {character.CharacterName} ({character.Id}) : déjà à jour");
                }
            }

            if (anyWrite)
                AssetDatabase.SaveAssets();

            string summary = $"{writtenCount} écrits, {upToDateCount} à jour, {missingCount} introuvables";
            if (skippedEmptyCount > 0)
                summary += $", {skippedEmptyCount} sans répliques";
            Debug.Log($"[QuotesImporter] {summary}.");
        }

        private static QuoteSet[] BuildQuoteTable()
        {
            QuoteSet[] srQuotes = LoadSrQuotesFromJson();
            var combined = new List<QuoteSet>(SsrLrQuoteTable.Length + srQuotes.Length);
            combined.AddRange(SsrLrQuoteTable);
            combined.AddRange(srQuotes);
            return combined.ToArray();
        }

        private static QuoteSet[] LoadSrQuotesFromJson()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string jsonPath = Path.Combine(projectRoot, SrQuotesJsonPath);
            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[QuotesImporter] Fichier SR introuvable : {jsonPath}");
                return Array.Empty<QuoteSet>();
            }

            string json = File.ReadAllText(jsonPath);
            SrQuotesFile file = JsonUtility.FromJson<SrQuotesFile>(json);
            if (file?.characters == null || file.characters.Length == 0)
                return Array.Empty<QuoteSet>();

            var quotes = new List<QuoteSet>(file.characters.Length);
            for (int i = 0; i < file.characters.Length; i++)
            {
                SrQuoteEntry entry = file.characters[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.displayName))
                    continue;

                quotes.Add(new QuoteSet(
                    entry.displayName.Trim(),
                    entry.rank1 ?? string.Empty,
                    entry.mid ?? string.Empty,
                    entry.last ?? string.Empty));
            }

            return quotes.ToArray();
        }

        private static bool IsQuoteSetEmpty(QuoteSet quotes)
        {
            return string.IsNullOrWhiteSpace(quotes.Rank1)
                && string.IsNullOrWhiteSpace(quotes.Mid)
                && string.IsNullOrWhiteSpace(quotes.Last);
        }

        private static CharacterData[] LoadAllCharacterData()
        {
            string[] guids = AssetDatabase.FindAssets("t:CharacterData");
            var characters = new List<CharacterData>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (data != null)
                    characters.Add(data);
            }

            return characters.ToArray();
        }

        private static bool TryFindCharacter(
            CharacterData[] characters,
            string tableName,
            out CharacterData match)
        {
            match = null;
            string trimmedTableName = tableName.Trim();

            for (int i = 0; i < characters.Length; i++)
            {
                CharacterData character = characters[i];
                if (character == null)
                    continue;

                string assetName = character.CharacterName;
                if (!string.IsNullOrEmpty(assetName)
                    && string.Equals(assetName.Trim(), trimmedTableName, StringComparison.OrdinalIgnoreCase))
                {
                    match = character;
                    return true;
                }
            }

            string normalizedTableName = NormalizeLookupKey(trimmedTableName);
            for (int i = 0; i < characters.Length; i++)
            {
                CharacterData character = characters[i];
                if (character == null)
                    continue;

                string assetId = character.Id;
                if (!string.IsNullOrEmpty(assetId)
                    && string.Equals(NormalizeLookupKey(assetId), normalizedTableName, StringComparison.Ordinal))
                {
                    match = character;
                    return true;
                }
            }

            return false;
        }

        private static bool TryWriteQuotes(CharacterData character, QuoteSet quotes, out bool wroteAny)
        {
            wroteAny = false;
            if (character == null)
                return false;

            SerializedObject serializedObject = new SerializedObject(character);
            SerializedProperty rank1Prop = serializedObject.FindProperty("endRunQuoteRank1");
            SerializedProperty midProp = serializedObject.FindProperty("endRunQuoteMid");
            SerializedProperty lastProp = serializedObject.FindProperty("endRunQuoteLast");

            if (rank1Prop == null || midProp == null || lastProp == null)
            {
                Debug.LogError(
                    $"[QuotesImporter] Propriétés de répliques introuvables sur {character.name}.",
                    character);
                return false;
            }

            wroteAny |= TrySetStringProperty(rank1Prop, quotes.Rank1);
            wroteAny |= TrySetStringProperty(midProp, quotes.Mid);
            wroteAny |= TrySetStringProperty(lastProp, quotes.Last);

            if (wroteAny)
                serializedObject.ApplyModifiedProperties();

            return true;
        }

        private static bool TrySetStringProperty(SerializedProperty property, string value)
        {
            string target = value ?? string.Empty;
            if (string.Equals(property.stringValue, target, StringComparison.Ordinal))
                return false;

            property.stringValue = target;
            return true;
        }

        private static string NormalizeLookupKey(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var builder = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == ' ' || c == '\'' || c == '\u2019' || c == '_' || c == '°')
                    continue;

                builder.Append(char.ToLowerInvariant(c));
            }

            return builder.ToString();
        }
    }
}
#endif
