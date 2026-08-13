# Plan d'exécution — MT4 Backend & comptes

**Take Five Games — Track Zero** · 13 août 2026 · v1.2 — **MT4-0 · G1 · G2-P1 · G5 CLOS · prochain G2-P2**

Contrat : `Cahier_Charges_Backend_MT4.md` · Go UGS 13/08. Offline-first non négociable.

| Gate | État |
|---|---|
| **MT4-0** | ✅ CLOS — cahier + Go UGS |
| **MT4-G1** | ✅ **CLOS** — Auth + Cloud Code + ancre `GameClock` + HF1 · `ebac2f3` / `0a11cb4` / `ac19184` |
| **MT4-G2** | ⏳ **P1 ✅ CLOS** — Cloud Save + conflits MT4-D2 · suite `[G2Suite]` 5 PASS · **P2 = liaison Google** |
| **MT4-G3** | Remote Config saisons |
| **MT4-G4** | Analytics + RGPD |
| **MT4-G5** | ✅ **CLOS** (= MT2-G6 live `bf90241` + suite `2cd0c69`) |

## Journal

| Date | Étape | Verdict |
|---|---|---|
| 13/08 | MT4-0 Go UGS | ACTÉ |
| 13/08 | G1 + HF1 + suite | **G1 CLOS** |
| 13/08 | MT2-G6 / MT4-G5 | **CLOS** — rail saisons LIVE · ouvre G2 |
| 13/08 | G2-P1 cloud save + suite | **P1 CLOS** — `d8fb37e` / `7805e84` / `d392c92` · terrain 5 PASS / 0 FAIL · #06 MANUAL hors auto |

## G2-P1 — livré

- `CloudSaveSync` + `SaveConflictDialog` (politique MT4-D2)
- Hooks `PersistentManager` / `BackendService` · DBG CLOUD
- Suite `CloudSaveIntegritySuite` — bouton **Run suite G2**
- Note : conflit UI si fingerprints `save.json` divergent même quand le résumé UI est identique (ex. `lastPlayed`) — comportement attendu P1
