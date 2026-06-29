using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Source UNIQUE des tokens visuels du jeu : couleurs, noms de sprites, police, tailles,
    /// espacements. Toute l'identité UI passe par ici — changer un vrai asset = modifier une
    /// valeur ici puis relancer les générateurs. Aucune couleur/sprite en dur ailleurs.
    /// </summary>
    public static class UiTheme
    {
        // ════════════════════════════════════════
        // SPRITES (noms d'assets, chargés par nom)
        // ════════════════════════════════════════
        public const string SpriteCard = "card_rounded"; // panneau/carte arrondi 9-slice
        public const string SpriteCoin = "tals_coin";    // icône Tals
        public const string SpriteMenu = "menu_burger";  // icône menu

        // ════════════════════════════════════════
        // POLICE (nom de l'asset TMP ; vide = police TMP par défaut)
        // ════════════════════════════════════════
        public const string FontDefault = ""; // ex. "ChezArthur SDF" quand la vraie police arrivera

        // ════════════════════════════════════════
        // SURFACES
        // ════════════════════════════════════════
        public static readonly Color Surface       = Hex("1E2128"); // fond de carte
        public static readonly Color SurfaceBar     = Hex("14161E"); // barres / header (semi-opaque géré au cas par cas)
        public static readonly Color SurfaceGlobal = Hex("12141A"); // fond global d'écran
        public static readonly Color Frame         = Hex("2A2E38"); // cadre d'icône neutre
        public static readonly Color Filet         = Hex("3A3E52"); // séparateur fin

        // ════════════════════════════════════════
        // TEXTES
        // ════════════════════════════════════════
        public static readonly Color TextPrimary   = Hex("F2F4F8");
        public static readonly Color TextSecondary = Hex("CFD3DE");
        public static readonly Color TextMuted     = Hex("9EA3B3");
        public static readonly Color AccentSection = Hex("A8B0D4"); // titres de section

        // ════════════════════════════════════════
        // ONGLETS
        // ════════════════════════════════════════
        public static readonly Color TabActive   = Hex("2E3350");
        public static readonly Color TabInactive = Hex("181A20");

        // ════════════════════════════════════════
        // ACCENTS
        // ════════════════════════════════════════
        public static readonly Color Gold     = Hex("E6C45A"); // Tals / valeur
        public static readonly Color Positive = Hex("7CC77C"); // gain / après
        public static readonly Color Negative = Hex("E07A7A"); // malus

        // ════════════════════════════════════════
        // RARETÉ PERSONNAGE (SR / SSR / LR)
        // ════════════════════════════════════════
        public static readonly Color RaritySR  = Hex("99CCFF"); // bleu
        public static readonly Color RaritySSR = Hex("FFD700"); // or
        public static readonly Color RarityLR  = Hex("CC80FF"); // violet

        // ════════════════════════════════════════
        // RARETÉ VALISE (couleur = dernière amélioration prise)
        // ════════════════════════════════════════
        public static readonly Color ValiseCommune    = Hex("9098A8"); // gris
        public static readonly Color ValiseRare       = Hex("5B8DEF"); // bleu
        public static readonly Color ValiseEpique     = Hex("A95FD6"); // violet
        public static readonly Color ValiseLegendaire = Hex("F2C94C"); // or

        // ════════════════════════════════════════
        // RARETÉ BONUS (tons sombres teintés, lisibles)
        // ════════════════════════════════════════
        public static readonly Color BonusCommon   = Hex("3A3E48");
        public static readonly Color BonusUncommon = Hex("2E5E3A");
        public static readonly Color BonusRare     = Hex("2A4A7A");
        public static readonly Color BonusEpic     = Hex("4A2A6E");
        public static readonly Color BonusSpecial  = Hex("7A5E1E");

        // ════════════════════════════════════════
        // BADGES
        // ════════════════════════════════════════
        public static readonly Color BadgeNew      = Hex("29D778");
        public static readonly Color BadgeUpgrade  = Hex("F0C72E");
        public static readonly Color BadgeItem     = Hex("8F54F0");
        public static readonly Color BadgeDownside = Hex("D93D3D");

        // ════════════════════════════════════════
        // TYPOGRAPHIE (tailles)
        // ════════════════════════════════════════
        public const float FontTitle   = 38f; // gros titre (carte bonus)
        public const float FontName    = 30f; // nom de carte
        public const float FontHeader  = 24f; // titre de section
        public const float FontBody    = 22f; // description / stats
        public const float FontLabel   = 18f; // badge / niveau
        public const float FontCaption = 16f;

        // ════════════════════════════════════════
        // ESPACEMENTS
        // ════════════════════════════════════════
        public const int PadCard      = 24; // padding interne de carte
        public const int PadCompact   = 14;
        public const int SpacingRow   = 12;

        // ════════════════════════════════════════
        // HELPER
        // ════════════════════════════════════════
        /// <summary> Parse "RRGGBB" ou "RRGGBBAA" en Color. </summary>
        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString("#" + hex, out Color c) ? c : Color.magenta;
        }
    }
}
