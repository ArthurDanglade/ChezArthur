# Plan d'exécution — MT2 Saisons (score, crans, piste, rotation)

**Take Five Games — Track Zero** · 13 août 2026 · v1.7 — **G1–G5 CLOS · rail local MT2 CLOS · G6 attend MT4**

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

## 2. Confrontation v2 ↔ code (HEAD post-G2 `7c060ab`)

Conforme : rotation 5×5, score/snapshot/crans/piste/pages/rollover. **Rail local soldé G1–G5.** Reste : G6 live après gate temps serveur MT4.

### 2.1 Contrôle diff G2 (CLOS)

VALIDÉ 0 rejet (`6743b6f` + `7c060ab`). Scaling ×cran aux 2 getters ; EnemySummonSystem corrigé ; MidCombatSpawner transitif documenté ; unlock Normal+non-tainted ; Boss Rush x1 ; sélecteur HF1 OK.

### 2.2 Contrôle diff G3 (CLOS)

VALIDÉ 0 rejet (`f34aa4d`). Save v5 + wiring symétrique ; crédit unique structurel ; entitlements au rollover ; filtre portail scopé LR (non-LR intacts). **Process** : préférer ordre diff → terrain (HF1 attrapé avant run).

## 3. Gates — FIGÉS (Go 05/08)

| Gate | Périmètre | État |
|---|---|---|
| **MT2-G1** | **Data & socle** : save v4, migration V3→V4, `SeasonProgressManager`, score par étage (×1), snapshot rotation, `seasonId` 6 sem, anti-recul, debug META | ✅ **CLOS** — diff `53e7c1d` + checklist Arthur OK |
| **MT2-G2** | Crans : sélecteur au lancement, déblocage étage 50, multiplicateurs SO, scaling stats v1, verrouillage | ✅ **CLOS** — `6743b6f` + Hub `7c060ab` · checklist OK |
| **MT2-G3** | Piste : grille SO 12 + prestige, claims, attribution fin de saison, plomberie LR (niveaux 1–4) + portail cumulatif | ✅ **CLOS** — `f34aa4d` + portail/audit · checklist OK |
| **MT2-G4** | Pages : page saison, header refondu, écran récap (+ EnsureSeasonCurrent hub) | ✅ **CLOS** — code `00b7748` + Object fix `7606135` + Hub `807a023` · UI brute assumée (polish hors rail) |
| **MT2-G5** | Rollover local bout-en-bout S1→S2, Dump + suite auto + checklist 14 pts | ✅ **CLOS** — suite `791e35a` · 13 PASS / 0 FAIL + MANUAL FR/EN OK · **rail local CLOS** |
| **MT2-G6** | « Live » : bornes réelles, rotation synchronisée (lundi 00h00 Paris serveur) | déclenché par MT4 |

### 3.1 Consignés non bloquants (contrôle diff G1)

1. **Restart debug / taint** : fenêtre avant `_seasonTainted = true` peut écrire `score=1` sur saison vierge (bruit monotone négligeable).
2. **Plancher anti-recul** en PlayerPrefs = limitation assumée jusqu'à MT4 (prefs effaçables).
3. **`DebugAdvanceDays`** reste sur `UtcNow` brut (voulu — voyage debug non freiné).

### 3.2 Consignés non bloquants (G3)

1. **Un seul récap pending** : si un joueur saute deux saisons sans se connecter, seul le récap de la dernière survit (v2 « une saison à la fois ») ; entitlements de la saison écrasée perdus — cas marginal rail local, à re-durcir évent. au G6 live.

## 4. Journal

| Date | Étape | Verdict |
|---|---|---|
| 05/08 | MT2-0 : raw_meta v1 + interview + **v2 consolidée** | Contrat §1 |
| 05/08 | **Go contrat + 3 arbitrages** (6 sem · lundi 00h00 Paris · LR plomberie/plan artiste) | **ACTÉ** — docs `d77a639` |
| 05/08 | G1 — prompt | **Livré** |
| 11/08 | G1 — impl `53e7c1d` · contrôle diff ligne à ligne | **VALIDÉ** (0 rejet) · 3 consignés §3.1 |
| 11/08 | G1 — checklist Play Mode (Arthur) | **OK** → **G1 CLOS** |
| 11/08 | G2 — prompt Cursor | **Livré** |
| 11/08 | G2 — impl `6743b6f` + Hub `7c060ab` · checklist Arthur | **OK** → **G2 CLOS** (§2.1) |
| 11/08 | G3 — prompt Cursor | **Livré** |
| 12/08 | G3 — impl `f34aa4d` + builder portail · checklist Arthur | **OK** → **G3 CLOS** (§2.2) |
| 12/08 | G4 — prompt Cursor | **Livré** — colle → push → **contrôle avant terrain** |
| 12/08 | G4 — impl + Hub · checklist Arthur | **OK** → **G4 CLOS** (UI brute ; polish design hors rail) |
| 12/08 | G5 — prompt + Dump `DebugMenu` | **Livré** — Claude contrôle diff → Arthur checklist 14 pts |
| 13/08 | G5 — suite auto `SeasonIntegritySuite` (`791e35a`) | Play Hub → **Run suite G5** → coller `[G5Suite]` |
| 13/08 | G5 — terrain Arthur | **13 PASS / 0 FAIL** + FR/EN/portail/kill OK → **G5 CLOS** · **MT2 rail local CLOS** |
