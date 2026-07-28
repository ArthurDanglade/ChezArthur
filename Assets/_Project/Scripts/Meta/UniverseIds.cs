namespace ChezArthur.Meta
{
    /// <summary>
    /// Identifiants stables des univers (1–5). Faille remplace Morre Voeux à l'index 4.
    /// Thèmes ennemis : 1 Ardacula (gothique) ; 2 L'Ancien (arts martiaux) ;
    /// 3 Don Costardo (super-héros absurdes) ; 4 Faille (Failles n°) ;
    /// 5 Troplin (dragon / gobelin / fantasy montagne).
    /// </summary>
    public static class UniverseIds
    {
        public const int Count = 5;

        public const int Ardacula = 1;
        public const int Ancien = 2;
        public const int DonCostardo = 3;
        public const int Faille = 4;
        public const int Troplin = 5;

        /// <summary>
        /// Nom d'affichage pour l'UI / debug.
        /// </summary>
        public static string GetDisplayName(int universeId)
        {
            switch (universeId)
            {
                case Ardacula: return "Ardacula";
                case Ancien: return "L'Ancien";
                case DonCostardo: return "Don Costardo";
                case Faille: return "Faille";
                case Troplin: return "Troplin";
                default: return $"Univers {universeId}";
            }
        }

        /// <summary>
        /// Thème court (ennemis / décor), pour debug et outils.
        /// </summary>
        public static string GetThemeLabel(int universeId)
        {
            switch (universeId)
            {
                case Ardacula: return "Gothique";
                case Ancien: return "Arts martiaux";
                case DonCostardo: return "Super-héros absurdes";
                case Faille: return "Failles n°";
                case Troplin: return "Dragon / gobelin";
                default: return $"Univers {universeId}";
            }
        }

        /// <summary>
        /// True si l'id est dans la plage valide 1–5.
        /// </summary>
        public static bool IsValid(int universeId)
        {
            return universeId >= 1 && universeId <= Count;
        }
    }
}
