namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Représente l'état runtime d'un item pendant une run.
    /// </summary>
    public class ItemInstance
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════

        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private readonly ItemData _data;
        private bool _isConsumed;
        private int _useCount;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public ItemData Data => _data;
        public bool IsConsumed => _isConsumed;
        public int UseCount => _useCount;

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════
        /// <summary>
        /// Initialise une nouvelle instance d'item pour la run.
        /// </summary>
        public ItemInstance(ItemData data)
        {
            _data = data;
            _isConsumed = false;
            _useCount = 0;
        }

        /// <summary>
        /// Marque l'item comme consommé et incrémente son compteur d'utilisation.
        /// </summary>
        public void Consume()
        {
            _isConsumed = true;
            _useCount++;
        }

        /// <summary>
        /// Incrémente le compteur d'utilisation sans consommer l'item.
        /// </summary>
        public void IncrementUseCount()
        {
            _useCount++;
        }

        /// <summary>
        /// Indique si l'item peut encore se déclencher.
        /// </summary>
        public bool CanTrigger()
        {
            return !_isConsumed;
        }
    }
}
