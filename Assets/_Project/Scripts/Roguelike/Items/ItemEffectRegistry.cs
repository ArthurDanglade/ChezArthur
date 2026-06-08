using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Registre central des handlers d'effets d'items.
    /// Singleton léger, initialisé une fois au démarrage.
    /// </summary>
    public class ItemEffectRegistry : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static ItemEffectRegistry _instance;
        private Dictionary<string, IItemEffectHandler> _handlers;
        private ItemEffectContext _sharedContext;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static ItemEffectRegistry Instance => _instance;

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
            _handlers = new Dictionary<string, IItemEffectHandler>(64);
            _sharedContext = new ItemEffectContext();

            RegisterAllHandlers();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PUBLIQUES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistre un handler pour un effectId donné.
        /// </summary>
        public void RegisterHandler(string effectId, IItemEffectHandler handler)
        {
            if (string.IsNullOrEmpty(effectId) || handler == null) return;
            _handlers[effectId] = handler;
        }

        /// <summary>
        /// Retourne le handler pour un effectId donné, ou null si non trouvé.
        /// </summary>
        public IItemEffectHandler GetHandler(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return null;
            _handlers.TryGetValue(effectId, out IItemEffectHandler handler);
            return handler;
        }

        /// <summary>
        /// Retourne le contexte partagé (réutilisable, pas d'allocation).
        /// L'appelant reçoit un contexte nettoyé.
        /// </summary>
        public ItemEffectContext GetSharedContext()
        {
            _sharedContext.Clear();
            return _sharedContext;
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistre tous les handlers au démarrage.
        /// Ajouter ici chaque nouveau handler d'item au fur et à mesure.
        /// </summary>
        private void RegisterAllHandlers()
        {
            RegisterHandler("ticket_offert", new TicketOffertHandler());
            RegisterHandler("contrat_de_fuite", new ContratDeFuiteHandler());
            RegisterHandler("vestige_univers", new VestigeUniversHandler());
            RegisterHandler("aspirateur_ame", new AspiratreurAmeHandler());
            RegisterHandler("petite_biere", new PetiteBiereHandler());
            RegisterHandler("sniper", new SniperHandler());
            RegisterHandler("masse_lourde", new MasseLourdeHandler());
            RegisterHandler("katana", new KatanaHandler());
            RegisterHandler("cible_marquee", new CibleMarqueeHandler());
            RegisterHandler("faux_de_la_mort", new FauxDeLaMortHandler());
            RegisterHandler("boule_de_feu", new BouleDeFeuHandler());
            RegisterHandler("miroir_brise", new MiroirBriseHandler());
            RegisterHandler("chaine_resonance", new ChaineResonanceHandler());
            RegisterHandler("ame_du_flipper", new AmeduFlipperHandler());
            RegisterHandler("canne_de_billard", new CanneDeBillardHandler());
            RegisterHandler("bouclier_a_pique", new BouclierAPiqueHandler());
            RegisterHandler("ame_collective", new AmeCollectiveHandler());
            RegisterHandler("lanceur_de_toile", new LanceurDeToileHandler());
            RegisterHandler("chevelure_jaune", new ChevelureJauneHandler());
            RegisterHandler("dernier_souffle", new DernierSouffleHandler());
            RegisterHandler("pacte_de_sang", new PacteDeSangHandler());
            RegisterHandler("epee_ancien_roi", new EpeeAncienRoiHandler());
            RegisterHandler("porte_monnaie", new PorteMonnaieHandler());
            RegisterHandler("instinct_survie", new InstinctDeSurvieHandler());
        }
    }
}
