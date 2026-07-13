using System.Globalization;

namespace ChezArthur.UI
{
    /// <summary>
    /// Formatage de nombres pour l'UI. Séparateur de milliers : espace
    /// insécable U+00A0 (PAS CultureInfo fr-FR : selon la version ICU elle
    /// produit U+202F, absent de nombreuses fonts TMP → glyphe manquant).
    /// </summary>
    public static class UiNumberFormat
    {
        /// <summary> 4820 → "4 820" (espace insécable). </summary>
        public static string Thousands(long value)
            => value.ToString("N0", CultureInfo.InvariantCulture)
                    .Replace(",", "\u00A0");
    }
}
