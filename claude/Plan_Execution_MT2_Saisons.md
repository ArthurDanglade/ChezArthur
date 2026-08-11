# Plan d'exécution — MT2 Saisons (score, crans, piste, rotation)

**Take Five Games — Track Zero** · 5 août 2026 · v1.1 — **CONTRAT ACTÉ (Go 05/08, docs `d77a639`) · gates figés · G1 : prompt livré**
**Contrat = `Systeme_Saisons_Design_v2.docx` + §1 ci-dessous.** Méthode standard par gate. Coexistence : jamais `Feedback/**`, handlers de contenu, `UI/RevealStage/**` ; scènes/data en commits séparés par lane.

---

## 1. Décisions actées (contrat complet)

| Réf. | Décision |
|---|---|
| **MT2-D1** | **Durée de saison : 6 semaines** (arbitrage 05/08 — la rotation reste un cycle de 5 indépendant ; `seasonId` découplé du cycle). |
| **MT2-D2** | Portails = bannières datées ; **portail cumulatif des LR** (saison N invocable dès N+1). |
| **MT2-D3** | Mono-monnaie Tals (E2) ; piste = Tals (placeholders, 30–40 % des gains d'une saison) + **LR de saison** paliers 5/8/10/12 (niveaux 1–4). Pas de valises/cosmétiques/fragments (E1/E3/E4). |
| **MT2-D4** | **Rail d'abord** : local derrière la couture temps (garde anti-recul), tests au voyage dans le temps ; gates « live » après le gate temps serveur MT4. Aucune saison publique avant. |
| **MT2-D5** | Score = `meilleur (étage atteint × cran) sur une run` (`bestScoreThisSeason`) ; `bestStage` à vie intact ; Boss Rush exclu ; abandon = étage atteint ; cran verrouillé au lancement ; **gardes debug** (restart débug/cheats = run non scorante). |
| **MT2-D6** | Crans x1/x1,5/x2/x3/x5 ; déblocage « étage 50 du cran précédent » ; **progression de compte jamais réinitialisée** (`unlockedDifficulties` isolé) ; v1 = scaling stats, axes riches après refonte ennemis. |
| **MT2-D7** | Récompenses acquises à la fin de saison ; **récap bloquant** au premier lancement suivant (`pendingSeasonRecap`), créditant à l'affichage, re-consultable. |
| **MT2-D8** | UI : bouton Saison au **centre du header** (record retiré), page saison (4 questions, piste centrée), sélecteur de cran au lancement (2 touches, rotation visible). |
| **MT2-D9** | **Rotation : lundi 00h00 Europe/Paris** (arbitrage 05/08 — ancrage déjà codé, « dimanche soir » vécu). |
| **MT2-D10** | **LR de saison : plomberie maintenant, assets S1 au plan artiste** (arbitrage 05/08). Goat (LR) = placeholder de test. |

## 2. Confrontation v2 ↔ code (HEAD `d77a639`)

Conforme : rotation 5×5, slots 20 étages, post-100, `bestStage` intact. Soldé : save versionnée (MT0-G1, gabarit v4). À créer : score/crans/piste/récap/pages. **Écarts d'implémentation vérifiés** : `StageGenerator.cs:632` résout l'univers en live à chaque étage (→ snapshot au StartRun, exigence G1) · l'abandon quitte par `SceneLoader.LoadHub` sans passer par `EndRun` (→ score enregistré **à chaque étage atteint**, l'abandon est couvert par construction) · `UpdateBestStage` appelé en fin de run `RunManager.cs:687` (point d'ancrage du report final).

## 3. Gates — FIGÉS (Go 05/08)

| Gate | Périmètre | État |
|---|---|---|
| **MT2-G1** | **Data & socle** : save v4 (champs §13 v2, `unlockedDifficulties` isolé), migration V3→V4, `SeasonProgressManager` (détection rollover, reset §8.2 strict, capture récap), enregistrement du score par étage atteint (gardes debug/BossRush, multiplicateur ×1 en attendant G2), **snapshot de rotation au StartRun** consommé par StageGenerator, `seasonId` 6 sem découplé du cycle 5, garde anti-recul horloge, debug saison étendu | ✅ **Prompt livré** (`Prompt_Cursor_MT2_G1.md`) |
| **MT2-G2** | Crans : sélecteur au lancement, déblocage étage 50, multiplicateurs SO, scaling stats v1, verrouillage | après G1 |
| **MT2-G3** | Piste : grille SO 12 + prestige, claims, acquisition fin de saison, plomberie LR (niveaux 1–4) + portail cumulatif | après G2 |
| **MT2-G4** | Pages : page saison, header refondu, écran récap | après G3 |
| **MT2-G5** | Rollover local bout-en-bout S1→S2 au voyage dans le temps, checklist d'intégrité | clôture rail |
| **MT2-G6** | « Live » : bornes réelles, rotation synchronisée (lundi 00h00 Paris serveur) | déclenché par MT4 |

## 4. Journal

| Date | Étape | Verdict |
|---|---|---|
| 05/08 | MT2-0 : raw_meta v1 + interview + **v2 consolidée** | Contrat §1 |
| 05/08 | **Go contrat + 3 arbitrages** (6 sem · lundi 00h00 Paris · LR plomberie/plan artiste) | **ACTÉ** — docs `d77a639` |
| 05/08 | G1 — prompt | **Livré** — Arthur colle → push → contrôle du diff |
