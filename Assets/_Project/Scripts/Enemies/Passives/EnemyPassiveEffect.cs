namespace ChezArthur.Enemies.Passives
{
    /// <summary>
    /// Effets applicables par le système de passifs ennemis standard.
    /// Les effets complexes utilisent SpecialHandler avec un identifiant string.
    /// </summary>
    public enum EnemyPassiveEffect
    {
        None,

        // ── Buffs sur soi ──────────────────────────────
        /// <summary> Augmente l'ATK de cet ennemi. </summary>
        BuffSelfATK,

        /// <summary> Augmente la DEF de cet ennemi. </summary>
        BuffSelfDEF,

        /// <summary> Augmente la SPD de cet ennemi. </summary>
        BuffSelfSPD,

        /// <summary> Augmente la force de lancement de cet ennemi. </summary>
        BuffSelfLaunchForce,

        /// <summary> Soigne cet ennemi d'un pourcentage de ses HP max. </summary>
        HealSelf,

        /// <summary> Applique un bouclier à cet ennemi. </summary>
        ShieldSelf,

        /// <summary> Ressuscite cet ennemi une fois à X% HP. </summary>
        ResurrectSelf,

        /// <summary> Rend cet ennemi immunisé aux dégâts pendant 1 tour. </summary>
        ImmunityOneTurn,

        // ── Buffs sur coéquipiers ──────────────────────
        /// <summary> Soigne un ou tous les coéquipiers. </summary>
        HealMate,

        /// <summary> Augmente l'ATK d'un ou tous les coéquipiers. </summary>
        BuffMateATK,

        /// <summary> Augmente la DEF d'un ou tous les coéquipiers. </summary>
        BuffMateDEF,

        // ── Debuffs sur alliés (équipe joueur) ─────────
        /// <summary> Réduit l'ATK d'un ou tous les alliés. </summary>
        DebuffAllyATK,

        /// <summary> Réduit la SPD d'un ou tous les alliés. </summary>
        DebuffAllySPD,

        /// <summary> Renvoie X% des dégâts reçus à l'attaquant allié. </summary>
        ReflectDamageToAttacker,

        /// <summary> Inflige X% HP max à tous les alliés. </summary>
        DamageAllAllies,

        /// <summary> Intercepte X% des soins reçus par les alliés. </summary>
        InterceptAllyHeal,

        /// <summary> Annule tous les buffs actifs sur tous les alliés. </summary>
        CancelAllyBuffs,

        /// <summary> Donne X% de chance à un allié de rater son prochain tour. </summary>
        ChanceToMissAlly,

        // ── Gestion des stacks ─────────────────────────
        /// <summary> Ajoute un stack au compteur interne de ce passif. </summary>
        AddStack,

        /// <summary> Remet à zéro les stacks du passif ciblé. </summary>
        ResetStack,

        // ── Handler externe ────────────────────────────
        /// <summary>
        /// Route vers un IEnemyPassiveHandler identifié par
        /// EnemyPassiveData.specialHandlerId.
        /// </summary>
        SpecialHandler,
    }
}
