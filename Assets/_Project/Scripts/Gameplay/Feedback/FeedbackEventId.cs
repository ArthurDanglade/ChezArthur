namespace ChezArthur.Gameplay.Feedback
{
    /// <summary>
    /// Liste fermée des événements de feedback combat (charte §4).
    /// Tout ajout futur = avenant charte + entrée d'enum.
    /// </summary>
    public enum FeedbackEventId
    {
        // ── Groupe A — cœur existant (re-câblé en F2-P2) ──
        AllyLaunch = 0,
        SuperLaunch = 1,
        /// <summary> Complétude data — boucle gérée par JuiceDirector jusqu'à F2-P2 ; service V1 = one-shot only. </summary>
        AimTension = 2,
        WallBounce = 3,
        HitEnemy = 4,
        Crit = 5,
        Kill = 6,
        StageFinisher = 7,
        DefeatBeat = 8,

        // ── Groupe B — langage d'état (émetteurs en F3) ──
        HealReceived = 9,
        BuffApplied = 10,
        BuffExpired = 11,
        DebuffApplied = 12,
        DebuffExpired = 13,
        ShieldGained = 14,
        ShieldAbsorbed = 15,
        ShieldBroken = 16,
        BurnApplied = 17,
        BurnTick = 18,
        BurnEnded = 19,
        PoisonApplied = 20,
        PoisonTick = 21,
        PoisonEnded = 22,
        StunApplied = 23,
        StunEnded = 24,
        FreezeApplied = 25,
        FreezeEnded = 26,

        // ── Groupe C — axe ennemi & moments (émetteurs en F4) ──
        EnemyWindup = 27,
        EnemyLaunch = 28,
        EnemyHitAlly = 29,
        EnemyWallBounce = 30,
        SummonSpawned = 31,
        TurnRelay = 32,
        VictorySting = 33,
        BossDefeated = 34,
        SpecSwitch = 35,
        ZonePlaced = 36,
        ZoneCrossed = 37,
        Revive = 38,
        ExtraTurn = 39
    }
}
