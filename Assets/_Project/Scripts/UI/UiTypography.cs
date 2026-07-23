namespace ChezArthur.UI
{
    /// <summary>
    /// Source unique des tailles typographiques. La police elle-même vient exclusivement
    /// du font asset par défaut de TMP Settings — aucun override de font asset autorisé
    /// sur les textes (swap de police prévu en fin de chantier, gate 7.4).
    /// </summary>
    public static class UiTypography
    {
        // ═══════════════════════════════════════════
        // RÔLES TYPO — tailles @ référence 1080
        // ═══════════════════════════════════════════

        /// <summary> Titres de page. Graisse recommandée : Bold. </summary>
        public const float Display = 64f;

        /// <summary> Sections / cartes. Graisse recommandée : SemiBold ou Bold. </summary>
        public const float Title = 44f;

        /// <summary> Texte courant. Graisse recommandée : Regular. </summary>
        public const float Body = 34f;

        /// <summary> Boutons / onglets. Graisse recommandée : Medium ou SemiBold. </summary>
        public const float Label = 30f;

        /// <summary> Méta, timers. Graisse recommandée : Regular. </summary>
        public const float Caption = 24f;
    }
}
