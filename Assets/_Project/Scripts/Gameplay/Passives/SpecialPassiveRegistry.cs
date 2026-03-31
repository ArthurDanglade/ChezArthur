using System.Collections.Generic;
using UnityEngine;
using ChezArthur.Gameplay.Passives.Handlers;

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
            RegisterHandler("loupzeur_sr_bonus", new LoupZeurSrBonusHandler());
            RegisterHandler("loupzeur_alpha", new LoupZeurAlphaHandler());
            RegisterHandler("pusamair_poison", new PusamAirPoisonHandler());
            RegisterHandler("pusamair_spread", new PusamAirSpreadHandler());
            RegisterHandler("frigor_freeze", new FrigorFreezeHandler());
            RegisterHandler("frigor_shatter", new FrigorShatterHandler());
            RegisterHandler("zoneur_zone", new ZoneurZoneHandler());
            RegisterHandler("zoneur_zone_expand", new ZoneurExpandHandler());
            RegisterHandler("elfert_luck", new ElfertLuckHandler());
            RegisterHandler("elfert_badluck", new ElfertBadLuckHandler());
            RegisterHandler("lumino_light", new LuminoLightHandler());
            RegisterHandler("lumino_blind", new LuminoBlindHandler());
            RegisterHandler("kram_fire_trail", new KramFireTrailHandler());
            RegisterHandler("kram_fire_enhance", new KramFireEnhanceHandler());
            RegisterHandler("ronss_thorns", new RonssThornsHandler());
            RegisterHandler("ronss_roots", new RonssRootsHandler());
            RegisterHandler("morgan_anesthesia", new MorganAnesthesiaHandler());
            RegisterHandler("morgan_vomit", new MorganVomitHandler());
            RegisterHandler("phil_optimize", new PhilOptimizeHandler());
            RegisterHandler("phil_patch", new PhilPatchHandler());
            RegisterHandler("voltrain_wall", new VoltrainWallHandler());
            RegisterHandler("voltrain_discharge", new VoltrainDischargeHandler());
            RegisterHandler("lanshimmer_lance", new LansHimmerLanceHandler());
            RegisterHandler("lanshimmer_rain", new LansHimmerRainHandler());
            RegisterHandler("spenda_teleport", new SpendaTeleportHandler());
            RegisterHandler("spenda_vip", new SpendaVipHandler());
            RegisterHandler("leuk_coinflip", new LeukCoinFlipHandler());
            RegisterHandler("leuk_gamble", new LeukGambleHandler());
            RegisterHandler("shado_stealth", new ShadoStealthHandler());
            RegisterHandler("shado_steal", new ShadoStealHandler());
            RegisterHandler("daupou_propulsion", new DaupouPropulsionHandler());
            RegisterHandler("daupou_freestyle", new DaupouFreestyleHandler());
            RegisterHandler("tribulle_order", new TribulleOrderHandler());
            RegisterHandler("tribulle_double", new TribulleDoubleHandler());
            RegisterHandler("bouclar_shield", new BouclarShieldHandler());
            RegisterHandler("bouclar_repair", new BouclarRepairHandler());
            RegisterHandler("revvie_rez", new RevvieRezHandler());
            RegisterHandler("revvie_link", new RevvieLinkHandler());

            Debug.Log($"[SpecialPassiveRegistry] {_handlers.Count} handlers enregistrés.");
        }
    }
}
