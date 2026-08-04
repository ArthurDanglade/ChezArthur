using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Localization
{
    /// <summary>
    /// API centrale de localisation. FR = source (défaut) ; EN = overlay.
    /// Ne throw jamais. PlayerPrefs = préférence device (frontière save G1).
    /// </summary>
    public static class Loc
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        public const string PREF_LANGUAGE = "Loc_Language";
        public const string CATALOG_RESOURCE = "LocalizationCatalog";

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static bool _languageResolved;
        private static GameLanguage _currentLanguage;
        private static bool _dictionaryResolved;
        private static bool _catalogMissingWarned;
        private static Dictionary<string, string> _englishByKey;

        // ═══════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════
        /// <summary> Émis après un changement de langue effectif. </summary>
        public static event Action OnLanguageChanged;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary> Langue active (lazy-init + persistence PlayerPrefs). </summary>
        public static GameLanguage CurrentLanguage
        {
            get
            {
                EnsureLanguageResolved();
                return _currentLanguage;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Change la langue et notifie les abonnés. No-op si identique.
        /// </summary>
        public static void SetLanguage(GameLanguage lang)
        {
            EnsureLanguageResolved();
            if (_currentLanguage == lang)
                return;

            _currentLanguage = lang;
            PlayerPrefs.SetInt(PREF_LANGUAGE, (int)lang);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// Traduit une clé. Entrée EN vide = « à traduire » → fallback FR silencieux.
        /// </summary>
        public static string Tr(string key, string frDefault)
        {
            if (frDefault == null)
                frDefault = "";

            if (CurrentLanguage != GameLanguage.English)
                return frDefault;

            EnsureDictionary();
            if (_englishByKey == null || string.IsNullOrEmpty(key))
                return frDefault;

            if (_englishByKey.TryGetValue(key, out string english)
                && !string.IsNullOrEmpty(english))
            {
                return english;
            }

            return frDefault;
        }

        /// <summary>
        /// Traduit puis formate. FormatException → log + pattern non formaté.
        /// </summary>
        public static string Format(string key, string frDefaultPattern, params object[] args)
        {
            string pattern = Tr(key, frDefaultPattern);
            if (args == null || args.Length == 0)
                return pattern;

            try
            {
                return string.Format(pattern, args);
            }
            catch (FormatException)
            {
                Debug.LogError($"[Loc] FormatException pour la clé « {key} ».");
                return pattern;
            }
        }

        /// <summary>
        /// Texte SO par convention de clé : "{prefix}.{id}.{field}".
        /// </summary>
        public static string TrId(string prefix, string id, string field, string frFallback)
        {
            string key = $"{prefix}.{id}.{field}";
            return Tr(key, frFallback);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        private static void EnsureLanguageResolved()
        {
            if (_languageResolved)
                return;

            if (PlayerPrefs.HasKey(PREF_LANGUAGE))
            {
                int stored = PlayerPrefs.GetInt(PREF_LANGUAGE, (int)GameLanguage.French);
                _currentLanguage = stored == (int)GameLanguage.English
                    ? GameLanguage.English
                    : GameLanguage.French;
            }
            else
            {
                _currentLanguage = Application.systemLanguage == SystemLanguage.French
                    ? GameLanguage.French
                    : GameLanguage.English;
                PlayerPrefs.SetInt(PREF_LANGUAGE, (int)_currentLanguage);
                PlayerPrefs.Save();
            }

            _languageResolved = true;
        }

        private static void EnsureDictionary()
        {
            if (_dictionaryResolved)
                return;

            _dictionaryResolved = true;
            _englishByKey = new Dictionary<string, string>();

            LocalizationCatalog catalog = Resources.Load<LocalizationCatalog>(CATALOG_RESOURCE);
            if (catalog == null)
            {
                if (!_catalogMissingWarned)
                {
                    Debug.LogWarning("[Loc] LocalizationCatalog introuvable dans Resources — fallback FR systématique.");
                    _catalogMissingWarned = true;
                }

                return;
            }

            catalog.BuildDictionary(_englishByKey);
        }
    }
}
