# Plan d'exécution — MT2 Saisons (score, crans, piste, rotation)

**Take Five Games — Track Zero** · 5 août 2026 · v1 — **contrat consolidé, soumis au Go** (3 arbitrages restants, §2)
**Le contrat = `Systeme_Saisons_Design_v2.docx`** (Arthur, 05/08 — remplace intégralement la v1, erratums E1–E10) **+ les décisions et amendements du présent §1–§3.** À pousser dans `claude/raw_meta/` (v2 à côté de la v1).
Méthode standard par gate. Coexistence : MT2 ne touche pas `Feedback/**`, handlers de contenu, `UI/RevealStage/**` ; scènes/data en commits séparés par lane.

---

## 1. Décisions actées (interview 05/08 + v2)

| Réf. | Décision | Source |
|---|---|---|
| **MT2-D2** | Portails = bannières d'invocation datées (`dateFinSaison` existant). Le **portail cumulatif des LR** (v2 §7.3) en est un cas : bannière permanente au pool grossissant, LR de la saison N invocable à partir de N+1. | Interview + v2 |
| **MT2-D3** | Économie : **mono-monnaie Tals** (E2). Piste = Tals (placeholders, repère 30–40 % des gains d'une saison) + **LR de saison** aux paliers 5/8/10/12 (niveaux 1–4, sémantique doublons existante). Pas de valises/objets (E1), pas de cosmétiques (E3), pas de fragments (E4). | v2 |
| **MT2-D4** | **Rail d'abord** : tout en local derrière `ITimeSource` (device + garde anti-recul monotone), tests via voyage dans le temps debug. Gates « live » (rollover public, rotation synchronisée) **après le gate temps serveur MT4**. Aucune saison publique avant (D8/§11 v2 respecté). | Interview |
| **MT2-D5** | Métrique = **score de saison** : `meilleur (étage × multiplicateur de cran) sur une seule run` (`bestScoreThisSeason`), distinct de `bestStage` à vie (intact). Boss Rush exclu. Abandon volontaire = étage atteint. Cran verrouillé au lancement. **Garde runs debug** (jamais de score via `DebugRestartRunAtStage`/cheats). | v2 §6 |
| **MT2-D6** | **Crans de difficulté** : x1 / x1,5 / x2 / x3 / x5 au lancement, déblocage universel « étage 50 du cran précédent », **progression de compte jamais réinitialisée** (`unlockedDifficulties` isolé du bloc saison en save). v1 des crans = scaling stats simple ; les axes riches (densité, élites, patterns) **attendent la refonte ennemis** (dépendance actée v2 §5.4). | v2 §5 |
| **MT2-D7** | Fin de saison : récompenses **acquises à la fin de saison** quelle que soit la connexion ; **écran récap bloquant** au premier lancement suivant (`pendingSeasonRecap`), créditant à l'affichage, re-consultable depuis la page saison. | v2 §9 |
| **MT2-D8** | UI : bouton **Saison au centre du header** (record d'étage retiré du header), page saison (4 questions, piste centrée sur la position), **sélecteur de cran au lancement** (2 touches max, rotation de la semaine visible). | v2 §10 |

## 2. Arbitrages restants (au Go de ce contrat)

1. **Durée de saison — conflit à trancher** : interview d'aujourd'hui = 5 semaines (alignement code) ; v2 §8.1 = **6 semaines** (rythme de prod solo + « un cycle de rotation complet plus une semaine »). **Reco manager révisée : suivre la v2 (6 semaines)** — la rotation reste un cycle de 5 indépendant (le code tourne déjà en modulo 5), seule la dérivation de `seasonId` se découple du cycle (petit refactor propre). Le v2 est postérieur et son argument tient.
2. **Heure de rotation** (point ouvert v2 §15) : « dimanche soir, heure fixe, temps serveur ». **Reco : lundi 00h00 Europe/Paris** — c'est « dimanche soir » vécu, et c'est **exactement l'ancrage déjà codé** (`GetMondayOfCurrentWeekParis`). Zéro refactor.
3. **LR de saison — dépendance contenu actée ?** Le système se construit maintenant (plomberie niveaux 1–4, portail cumulatif), mais **le lancement de la S1 publique exige un LR nouveau avec assets** (artwork, spés, passifs). À inscrire au plan artiste. *(Le placeholder Goat/LR existant sert aux tests.)*

## 3. Confrontation v2 ↔ code (vérité terrain, HEAD `94c97cf`)

| Point v2 | Code | Conséquence |
|---|---|---|
| Rotation hebdo 5 univers | ✅ `SeasonRotationManager` (table 5×5, slots 20 étages, modulo post-100 ≈ « arène du train ») | Conforme — v2 §3/§4 documentent l'existant |
| Rotation « au lancement, jamais en cours » (§4.3) | ❌ `StageGenerator` résout l'univers **à chaque étage** → un rollover mi-run décalerait les univers | **Exigence G1 : snapshot de rotation capturé au StartRun** |
| Temps serveur / bornes | ❌ horloge device (`GameClock`) | MT2-D4 (rail + ITimeSource) puis MT4 |
| `bestStage` à vie intact | ✅ | Conforme |
| Score/crans/piste/récap/pages | ❌ n'existent pas | Cœur du chantier |
| Save versionnée (§13 « voir doc backend ») | ✅ **déjà fait** (MT0-G1 : `SaveMigrator`, gabarit v4 prêt pour les 10 champs) | Point soldé |
| Sélecteur au lancement | 🟡 `PageAccueilUI` → `SceneLoader.LoadGame()` direct (+ `PendingRunMode` Boss Rush) | G2 : sélecteur inséré dans ce flux |
| Header | 🟡 `HubHeaderUI` (pseudo + Tals + record) | G4 : refonte D8 |

## 4. Gates pressentis (à figer au Go)

| Gate | Périmètre | Note |
|---|---|---|
| **MT2-G1** | **Data & socle** : `SeasonManager` (seasonId découplé du cycle si 6 sem), save **v4** (10 champs §13, `unlockedDifficulties` hors bloc saison), enregistrement du score (fin de run + abandon, gardes debug/BossRush), **snapshot de rotation au StartRun**, `ITimeSource` + garde anti-recul | Testable intégralement via l'override GameClock existant |
| **MT2-G2** | **Crans** : sélecteur au lancement (2 touches, rotation visible), déblocage étage 50, multiplicateurs en SO (placeholders), application scaling stats v1, verrouillage en run | Calibrage différé (refonte ennemis) |
| **MT2-G3** | **Piste** : grille SO 12 paliers + prestige, claims, acquisition fin de saison + auto-crédit au récap, plomberie LR de saison (niveaux par paliers) + portail cumulatif (bannière) | LR S1 = dépendance assets (arbitrage 3) |
| **MT2-G4** | **Pages** : page saison (4 questions, piste centrée), header refondu (bouton Saison centre, retrait record), écran récap bloquant re-consultable | Textes via `Loc` dès l'écriture |
| **MT2-G5** | **Rollover local bout-en-bout** : reset saison (tableau §8.2 strict), récap, re-déroulé complet S1→S2 via voyage dans le temps, checklist d'intégrité | Clôture du « rail » |
| **MT2-G6 (live)** | Après gate temps serveur MT4 : bornes réelles, rotation synchronisée, heure actée | Hors rail — déclenché par MT4 |

## 5. Journal

| Date | Étape | Verdict |
|---|---|---|
| 05/08 | MT2-0 ouvert : raw_meta v1 lu, confrontation, interview (durée/portails/économie/serveur) | 3 décisions actées, Q3 renvoyée au nouveau doc |
| 05/08 | **v2 reçue et consolidée** | Contrat §1–§4 — **en attente : Go global + 3 arbitrages (§2)** + push de la v2 dans `claude/raw_meta/` |
