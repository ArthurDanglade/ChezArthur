using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Gameplay.Passives
{
    /// <summary>
    /// Registre central des handlers de passifs spéciaux.
    /// Singleton léger, initialisé une fois au démarrage.
    /// </summary>
    public class SpecialPassiveRegistry : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static SpecialPassiveRegistry _instance;
        private Dictionary<string, ISpecialPassiveHandler> _handlers;
        private PassiveContext _sharedContext;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static SpecialPassiveRegistry Instance => _instance;

        // ═══════════════════════════════════════════
        // UNITY LIFECYCLE
        // ═══════════════════════════════════════════
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _handlers = new Dictionary<string, ISpecialPassiveHandler>(32);
            _sharedContext = new PassiveContext();

            RegisterAllHandlers();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistre un handler pour un specialEffectId donné.
        /// </summary>
        public void RegisterHandler(string effectId, ISpecialPassiveHandler handler)
        {
            if (string.IsNullOrEmpty(effectId) || handler == null) return;
            _handlers[effectId] = handler;
        }

        /// <summary>
        /// Retourne le handler pour un effectId donné, ou null si non trouvé.
        /// </summary>
        public ISpecialPassiveHandler GetHandler(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return null;
            _handlers.TryGetValue(effectId, out ISpecialPassiveHandler handler);
            return handler;
        }

        /// <summary>
        /// Retourne le contexte partagé (réutilisable, pas d'allocation).
        /// L'appelant reçoit un contexte nettoyé.
        /// </summary>
        public PassiveContext GetSharedContext()
        {
            _sharedContext.Clear();
            return _sharedContext;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistre tous les handlers au démarrage.
        /// Ajouter ici chaque nouveau handler quand on crée un perso avec un effet spécial.
        /// </summary>
        private void RegisterAllHandlers()
        {
            // === EXEMPLES (à décommenter quand les handlers sont créés) ===
            // RegisterHandler("goat_milk_bottles", new GoatMilkBottlesHandler());
            // RegisterHandler("ardacula_lifesteal", new ArdaculaLifestealHandler());
            // RegisterHandler("zoneur_zone", new ZoneurZoneHandler());
            // RegisterHandler("kram_fire_trail", new KramFireTrailHandler());

            Debug.Log($"[SpecialPassiveRegistry] {_handlers.Count} handlers enregistrés.");
        }
    }
}
