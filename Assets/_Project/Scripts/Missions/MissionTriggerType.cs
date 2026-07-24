namespace ChezArthur.Missions
{
    /// <summary>
    /// Type de déclencheur d'une mission.
    /// </summary>
    public enum MissionTriggerType
    {
        None = 0,

        /// <summary> Atteindre l'étage N (payload = stage). </summary>
        StageReached = 1,

        /// <summary> Super Lancer réussi (+1). </summary>
        SuperLancerSuccess = 2,

        /// <summary> Ennemi tué (+1). </summary>
        EnemyKilled = 3,

        /// <summary> Valise obtenue (+1). </summary>
        ValiseObtained = 4,

        /// <summary> Item obtenu (+1). </summary>
        ItemObtained = 5,

        /// <summary> Tirage gacha effectué (+1 par pull). </summary>
        GachaPull = 6,

        /// <summary> Personnage obtenu (filtre rareté optionnel). </summary>
        CharacterObtained = 7,

        /// <summary> Meilleur étage compte ≥ N (permanents). </summary>
        BestStageReached = 8,

        /// <summary> Bonus : toutes les missions de la couche complétées. </summary>
        LayerCompletionBonus = 9,

        /// <summary> Univers logique terminé (boss final local 20 vaincu). </summary>
        UniverseCompleted = 10,

        /// <summary> Boss vaincu en mode Boss Rush (+1, distincts gérés plus tard). </summary>
        BossRushBossDefeated = 11
    }
}
