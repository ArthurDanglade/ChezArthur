using ChezArthur.Enemies.Passives;

namespace ChezArthur.Enemies.Passives.Handlers
{
    /// <summary>
    /// Enregistrement central des factories de handlers spécialisés.
    /// </summary>
    public static class EnemyPassiveHandlerRegistry
    {
        /// <summary>
        /// Enregistre tous les handlers du jeu dans EnemyPassiveRuntime. Appeler au démarrage, avant tout Initialize().
        /// </summary>
        public static void RegisterAll()
        {
            EnemyPassiveRuntime.RegisterHandler("skarabe_devotion", () => new SkarabeDevotionHandler());
            EnemyPassiveRuntime.RegisterHandler("silence_gauge", () => new SilenceGaugeHandler());
            EnemyPassiveRuntime.RegisterHandler("pharao_observation", () => new PharaoObservationHandler());
            EnemyPassiveRuntime.RegisterHandler("machine_a_sous", () => new MachineASousHandler());
            EnemyPassiveRuntime.RegisterHandler("prototype", () => new PrototypeHandler());
            EnemyPassiveRuntime.RegisterHandler("reflet", () => new RefletHandler());
            EnemyPassiveRuntime.RegisterHandler("neant_phase", () => new NéantPhaseHandler());
            EnemyPassiveRuntime.RegisterHandler("coeur_desert", () => new CoeurDuDesertHandler());
            EnemyPassiveRuntime.RegisterHandler("parieur_endette", () => new ParieurEndetteHandler());
            EnemyPassiveRuntime.RegisterHandler("roux_lette", () => new RouxLetteHandler());
            EnemyPassiveRuntime.RegisterHandler("cha_teuh", () => new ChateuhHandler());
            EnemyPassiveRuntime.RegisterHandler("directeur", () => new DirecteurHandler());
            EnemyPassiveRuntime.RegisterHandler("la_maison", () => new LaMaisonHandler());
            EnemyPassiveRuntime.RegisterHandler("ombre_gardienne", () => new OmbreGardieneHandler());
            EnemyPassiveRuntime.RegisterHandler("grand_pretre", () => new GrandPretreHandler());
            EnemyPassiveRuntime.RegisterHandler("anubis", () => new AnubisHandler());
            EnemyPassiveRuntime.RegisterHandler("echo", () => new EchoHandler());
            EnemyPassiveRuntime.RegisterHandler("fissure", () => new FissureHandler());
            EnemyPassiveRuntime.RegisterHandler("anomalie", () => new AnomalieHandler());
            EnemyPassiveRuntime.RegisterHandler("lonbou", () => new LonbouHandler());
            EnemyPassiveRuntime.RegisterHandler("robot_blinde", () => new RobotBlindeHandler());
            EnemyPassiveRuntime.RegisterHandler("grilhor", () => new GrilhorHandler());
            EnemyPassiveRuntime.RegisterHandler("contremaitre", () => new ContreMaitreHandler());
            EnemyPassiveRuntime.RegisterHandler("systeme_central", () => new SystemeCentralHandler());
            EnemyPassiveRuntime.RegisterHandler("robotron", () => new RobotronHandler());
        }
    }
}
