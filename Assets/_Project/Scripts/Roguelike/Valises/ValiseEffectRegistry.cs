using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Registre central des handlers d'effets de valises.
    /// Singleton léger, initialisé une fois au démarrage.
    /// </summary>
    public class ValiseEffectRegistry : MonoBehaviour
    {
        // ═══════════════════════════════════════════
        // VARIABLES PRIVÉES
        // ═══════════════════════════════════════════
        private static ValiseEffectRegistry _instance;
        private Dictionary<string, IValiseEffectHandler> _handlers;
        private ValiseEffectContext _sharedContext;

        // ═══════════════════════════════════════════
        // PROPRIÉTÉS PUBLIQUES
        // ═══════════════════════════════════════════
        public static ValiseEffectRegistry Instance => _instance;

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
            _handlers = new Dictionary<string, IValiseEffectHandler>(32);
            _sharedContext = new ValiseEffectContext();
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
        /// Garantit qu'un registre existe (bootstrap si absent de la scène).
        /// </summary>
        public static ValiseEffectRegistry EnsureExists(Transform parent = null)
        {
            if (_instance != null)
                return _instance;

            GameObject go = new GameObject(nameof(ValiseEffectRegistry));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.AddComponent<ValiseEffectRegistry>();
        }

        /// <summary>
        /// Enregistre un handler pour un effectId donné.
        /// </summary>
        public void RegisterHandler(string effectId, IValiseEffectHandler handler)
        {
            if (string.IsNullOrEmpty(effectId) || handler == null) return;
            _handlers[effectId] = handler;
        }

        /// <summary>
        /// Retourne le handler pour un effectId donné, ou null si non trouvé.
        /// </summary>
        public IValiseEffectHandler GetHandler(string effectId)
        {
            if (string.IsNullOrEmpty(effectId)) return null;
            _handlers.TryGetValue(effectId, out IValiseEffectHandler handler);
            return handler;
        }

        /// <summary>
        /// Retourne le contexte partagé (réutilisable, pas d'allocation).
        /// L'appelant reçoit un contexte nettoyé.
        /// </summary>
        public ValiseEffectContext GetSharedContext()
        {
            _sharedContext.Clear();
            return _sharedContext;
        }

        /// <summary>
        /// Notifie OnRunStart sur TOUS les handlers enregistrés (reset d'état).
        /// Indépendant des slots actifs — nécessaire au début de run à vide.
        /// </summary>
        public void NotifyRunStartAll(ValiseEffectContext context)
        {
            if (context == null || _handlers == null) return;

            context.Trigger = ValiseTrigger.OnRunStart;
            foreach (KeyValuePair<string, IValiseEffectHandler> pair in _handlers)
            {
                if (pair.Value == null) continue;
                pair.Value.OnRunStart(context, null);
            }
        }

        /// <summary>
        /// Injecte le SFX MégaCrit dans le handler Frénésie s'il est enregistré.
        /// </summary>
        public void SetMegaCritSfx(AudioClip clip)
        {
            if (GetHandler("frenesie") is FrenesieHandler frenesie)
                frenesie.SetMegaCritSfx(clip);
        }

        // ═══════════════════════════════════════════
        // MÉTHODES PRIVÉES
        // ═══════════════════════════════════════════

        /// <summary>
        /// Enregistre tous les handlers au démarrage.
        /// Ajouter ici chaque nouveau handler de valise au fur et à mesure.
        /// </summary>
        private void RegisterAllHandlers()
        {
            RegisterHandler("frenesie", new FrenesieHandler());
            RegisterHandler("renvoi", new RenvoiHandler());
            RegisterHandler("vol_de_vie", new VolDeVieHandler());
            RegisterHandler("discipline", new DisciplineHandler());
            RegisterHandler("cameleon", new CameleonHandler());
            RegisterHandler("dernier_debout", new DernierDeboutHandler());
            RegisterHandler("fortune", new FortuneHandler());
            RegisterHandler("equilibre", new EquilibreHandler());
            RegisterHandler("defense", new DefenseLv20Handler());
            RegisterHandler("crescendo", new CrescendoHandler());
            RegisterHandler("mode_furie", new ModeFurieHandler());
            RegisterHandler("pression_je_la_bois", new PressionJeLaBoisHandler());
            RegisterHandler("bouclier", new BouclierHandler());
        }
    }
}
