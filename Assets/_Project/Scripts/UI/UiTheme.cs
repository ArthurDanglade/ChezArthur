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
        // FONDS — "l'écrin sombre" (Refonte Hub)
        // ════════════════════════════════════════
        /// <summary> Fond de scène des pages unies. </summary>
        public static readonly Color BgDeep = Hex("0B0C10");
        /// <summary> Fond des panneaux (PanelSurface). </summary>
        public static readonly Color BgPanel = Hex("14161C");
        /// <summary> Cartes, entrées de liste, pills. </summary>
        public static readonly Color BgElevated = Hex("1B1E26");

        // ════════════════════════════════════════
        // BORDURES (Refonte Hub)
        // ════════════════════════════════════════
        /// <summary> Bordure fine par défaut (1–2 px). </summary>
        public static readonly Color BorderSubtle = Hex("2A2E38");

        // ════════════════════════════════════════
        // TEXTES
        // ════════════════════════════════════════
        public static readonly Color TextPrimary   = Hex("F2F0EB"); // chaud (calib gate 1.3)
        public static readonly Color TextSecondary = Hex("CFD3DE");
        public static readonly Color TextMuted     = Hex("9EA3B3");
        public static readonly Color AccentSection = Hex("A8B0D4"); // titres de section
        /// <summary> Texte désactivé / non interactif (Refonte Hub). </summary>
        public static readonly Color TextDisabled = Hex("5A5E68");

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
        public static readonly Color SynergyBroken = Hex("6E7382"); // annonce synergie rompue (désaturé)

        // Accents lofi Hub (calib gate 1.3 — AccentAmber tamisé)
        /// <summary> Accent principal (élément actif, focus, primaire). </summary>
        public static readonly Color AccentAmber = Hex("DE8B4F");
        /// <summary> Accent secondaire (parcimonieux). </summary>
        public static readonly Color AccentRose = Hex("D98C8C");
        /// <summary> Accent tertiaire. </summary>
        public static readonly Color AccentTeal = Hex("4A8C82");

        // ════════════════════════════════════════
        // ÉTATS (Refonte Hub)
        // ════════════════════════════════════════
        /// <summary> Succès / validation positive. </summary>
        public static readonly Color Success = Hex("6FBF73");
        /// <summary> Danger / erreur / destructif. </summary>
        public static readonly Color Danger = Hex("E06C5F");
        /// <summary> Voile modal (scrim) — noir à 65 % d'opacité. </summary>
        public static readonly Color ScrimOverlay = new Color(0f, 0f, 0f, 0.65f);

        // ════════════════════════════════════════
        // SUPER LANCER
        // ════════════════════════════════════════
        public static readonly Color SuperLancerZone      = Hex("FF9420");   // zone de réussite — orange chaud, famille de l'escalade combo
        public static readonly Color SuperLancerTrack     = Hex("FFFFFF46"); // piste de l'anneau, discrète
        public static readonly Color SuperLancerIndicator = Hex("F2F0EB");   // repère mobile (= TextPrimary)

        // ════════════════════════════════════════
        // RARETÉ PERSONNAGE (SR / SSR / LR)
        // ════════════════════════════════════════
        public static readonly Color RaritySR  = Hex("99CCFF"); // bleu
        public static readonly Color RaritySSR = Hex("FFD700"); // or
        public static readonly Color RarityLR  = Hex("CC80FF"); // violet

        // ════════════════════════════════════════
        // FICHE PERSONNAGE (CharacterDetailPopup)
        // ════════════════════════════════════════
        public static readonly Color CardPanel       = Hex("111117");   // fond du panneau d'infos
        public static readonly Color CardPanelEntry  = Hex("14141B");   // fond d'une entrée passive
        public static readonly Color CardHairline    = Hex("26262E");   // séparateurs fins / bordures discrètes
        public static readonly Color CardBorderMuted = Hex("3A3A42");   // bordures neutres (onglet inactif, ghost)
        public static readonly Color CardHeaderScrim = Hex("0A0A0E9E"); // bandeau header translucide sur artwork
        public static readonly Color CardArtworkDim  = Hex("0A0A0E80"); // assombrissement artwork en état déplié
        /// <summary> Panneau plié : laisse voir l'artwork (alpha ~65 %). </summary>
        public static readonly Color CardPanelCollapsed = Hex("111117A6");
        /// <summary> Lumière chaude d'éveil (flash + surexposition). </summary>
        public static readonly Color CeremonyLight = Hex("FFF1D6");

        // ════════════════════════════════════════
        // GACHA STAGE
        // ════════════════════════════════════════
        /// <summary> Fond exclusif invocation (charbon). </summary>
        public static readonly Color GachaStageCharcoal = Hex("0A0B0E");

        // Typo dédiée fiche (lisibilité mobile portrait)
        public const float CardFontName      = 46f;
        public const float CardFontMeta      = 26f; // niveau / rôle
        public const float CardFontChip      = 24f;
        public const float CardFontTab       = 24f;
        public const float CardFontStatValue = 32f;
        public const float CardFontStatLabel = 20f;
        public const float CardFontBody      = 24f; // backstory + descriptions passifs
        public const float CardFontButton    = 26f;
        // ════════════════════════════════════════
        // RÔLES (accents désaturés — soulignements/liserés uniquement, jamais de flood)
        // ════════════════════════════════════════
        public static readonly Color RoleAttacker = Hex("E24B40"); // rouge vif (bordures ATK)
        public static readonly Color RoleDefender = Hex("3DBF68"); // vert vif (bordures DEF)
        public static readonly Color RoleSupport  = Hex("3B9EF0"); // bleu vif (bordures SUP)
        public static readonly Color RoleNeutral  = Hex("6B6870");

        // ════════════════════════════════════════
        // RARETÉ VALISE (couleur = dernière amélioration prise)
        // ════════════════════════════════════════
        public static readonly Color ValiseCommune    = Hex("9098A8"); // gris
        public static readonly Color ValiseRare       = Hex("5B8DEF"); // bleu
        public static readonly Color ValiseEpique     = Hex("A95FD6"); // violet
        public static readonly Color ValiseLegendaire = Hex("F2C94C"); // or
        /// <summary> Accent or Hub (bordures PanelSurface) — alias ValiseLegendaire, pas de hex dupliqué. </summary>
        public static readonly Color AccentGold = ValiseLegendaire;

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
        // TYPE ENNEMI (fiche d'inspection)
        // ════════════════════════════════════════
        public static readonly Color EnemyTypeNormalColor   = Hex("8A93A6"); // gris-bleu neutre
        public static readonly Color EnemyTypeMiniBossColor = Hex("E08A3C"); // orange ambré
        /// <summary> Identique au fill barre boss (Negative). </summary>
        public static readonly Color EnemyTypeBossColor     = Negative;

        // ════════════════════════════════════════
        // TYPOGRAPHIE (tailles)
        // ════════════════════════════════════════
        public const float FontTitle   = 38f; // gros titre (carte bonus)
        public const float FontName    = 30f; // nom de carte
        public const float FontHeader  = 24f; // titre de section
        public const float FontBody    = 22f; // description / stats
        public const float FontLabel   = 18f; // badge / niveau
        public const float FontCaption = 16f;
        public const float FontCelebration = 60f; // bannière cérémonie

        // ════════════════════════════════════════
        // ESPACEMENTS
        // ════════════════════════════════════════
        public const int PadCard      = 24; // padding interne de carte
        public const int PadCompact   = 14;
        public const int SpacingRow   = 12;

        // ════════════════════════════════════════
        // DIMENSIONS — Refonte Hub (px @ référence 1080)
        // ════════════════════════════════════════
        public const float Space1 = 4f;
        public const float Space2 = 8f;
        public const float Space3 = 12f;
        public const float Space4 = 16f;
        public const float Space5 = 24f;
        public const float Space6 = 32f;

        public const float RadiusS = 8f;
        public const float RadiusM = 12f;
        public const float RadiusL = 16f;

        public const float BorderThin = 2f;
        public const float BorderFocus = 3f;

        public const float HeaderHeight = 176f;
        public const float NavHeight = 152f;
        public const float ButtonPrimaryH = 132f;
        public const float TouchTargetMin = 96f;

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
