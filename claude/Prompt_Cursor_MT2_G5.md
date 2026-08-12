# PROMPT CURSOR — MT2-G5 : rollover bout-en-bout (dernier gate du rail local)

> Chez Arthur — Unity 2022.3, C#. Base : `main` à `807a023` (ou HEAD).
> G5 est un **gate de validation** : très peu de code, une checklist d'intégrité S1→S2→S3 complète.
> Le code se limite à l'outillage de preuve.

## PÉRIMÈTRE CODE — 1 SEUL FICHIER

`Assets/_Project/Scripts/Debug/DebugMenu.cs` — section META/SAISON, un bouton **« Dump état saison »** :
log multiligne `[SeasonDump]` structuré, copiable, TOUT l'état pertinent :
```
[SeasonDump] ═══ ÉTAT SAISON ═══
seasonId save/calc : {seasonId} / {CurrentSeasonId} · semaine rotation : {CurrentWeekNumber}/5
score : {bestScoreThisSeason} (ét. {bestStageThisSeason} ×{bestTierThisSeason}) · runs : {runsThisSeason}
claims : [{claimedTiers triés}] · prestige réclamés : {prestigeTiersClaimed} · claimable : {GetPrestigeClaimableCount()}
COMPTE — crans : [{unlockedDifficulties}] · LR portail : [{pastSeasonLrIds}]
recap : pending={pending} credited={rewardsCredited} (S={seasonId}, score={finalScore}, tals={pendingTals}, lrLvl={pendingLrLevels})
Tals : {tals} · bestStage à vie : {bestStage} · fin de saison : {GetCurrentSeasonEndParis():yyyy-MM-dd} (reste {GetTimeUntilSeasonEnd()})
```
Null-safe. **RIEN D'AUTRE dans aucun autre fichier.**

## CHECKLIST BOUT-EN-BOUT (Arthur — Editor, save de test dédiée, Export de ta vraie save AVANT)

**Phase A — S1 vécue (état riche)**
1. Reset save (DevMenu) → Hub → Dump n°1 : saison Sn vierge, cran x1 seul, portail vide.
2. Vivre la saison : 1 run x1 abandonnée étage 4 (score 4) · `+50 score` ×2 (104) · claims paliers 1–4 (le 5 **volontairement non réclamé** — LR en attente) · `unlockStage` test → débloquer x1,5 → run x1,5 étage 2 (score 3, ne bat pas 104 : vérifier au Dump) · missions : claim 1 daily.
3. Rotation intra-saison : `Semaine +1` → nouvelle run → univers pos. 1 décalé (rotation vit PENDANT la saison, indépendante).
4. Dump n°2 (photo avant rollover) — vérifier chaque ligne cohérente.

**Phase B — Rollover S1→S2 (le cœur)**
5. Clear week force + `+7 jours` ×6 → retour Hub (ou Check rollover + re-entrée Hub) → **récap gate bloquant** : score final 104, palier 5 dans les récompenses (LR + Tals des paliers 5+ éligibles non réclamés), crédit à l'affichage (Tals avant/après au Dump), Goat obtenu/niveau +1.
6. Dump n°3 : seasonId avancé · score/stage/runs/claims/prestige à **0** · **crans conservés** · **pastSeasonLrIds contient le LR S1** · recap credited=true · Tals/bestStage/persos/éveils intacts · missions : seasonal layer resetée, daily/weekly cohérents.
7. Portail cumulatif : bannière portail → le LR S1 est tirable.
8. Re-consultation : « Revoir le dernier bilan » → chiffres S1, zéro re-crédit (Tals au Dump inchangés).

**Phase C — S2 + robustesse**
9. S2 : `+50 score` ×1, **ne rien réclamer** → rollover S2→S3 (+42 j) → gate : paliers 1–2 crédités automatiquement, recap S2 écrase S1 (consigné §3 : un seul pending).
10. Kill app pendant S3 → relance → état intact (Dump n°4 == attendu), pas de double gate.
11. **Anti-recul** (hors override) : horloge Windows −1 h → warning, ids stables → remettre.
12. **Migration profonde** : poser `fixture_save_v0.json` → lancement → chaîne v0→v5 en un chargement, log migration, jeu intact, saison vierge propre.
13. FR/EN sur page saison + récap S3 (fallback FR OK, zéro troncature).
14. Restaurer ta vraie save (Import) + `unlockStage` remis à 50 si touché + smoke final (run, gacha, missions, Boss Rush, crans).

**Critère de sortie du rail : 14/14.** → MT2 rail local CLOS ; G6 « live » attend le gate temps serveur MT4.
