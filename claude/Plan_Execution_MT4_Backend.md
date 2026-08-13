# Plan d'exécution — MT4 Backend & comptes

**Take Five Games — Track Zero** · 13 août 2026 · v1.4 — **G3 collé · G2-P1 CLOS · G2-P2 device en attente**

Contrat : `Cahier_Charges_Backend_MT4.md` · Go UGS 13/08. Offline-first non négociable.

| Gate | État |
|---|---|
| **MT4-0** | ✅ CLOS — cahier + Go UGS |
| **MT4-G1** | ✅ **CLOS** |
| **MT4-G2** | ⏳ **P1 ✅ CLOS** · **P2 code validé** — device Google / §0 console en attente |
| **MT4-G3** | ⏳ **collé** — Remote Config overlay · suite `[G3Suite]` · §0 dashboard 4 JSON |
| **MT4-G4** | Analytics + RGPD |
| **MT4-G5** | ✅ **CLOS** |

## Journal

| Date | Étape | Verdict |
|---|---|---|
| 13/08 | G2-P1 cloud save + suite | **P1 CLOS** |
| 13/08 | G2-P2 code + scène Compte | code VALIDÉ Claude · device en attente |
| 13/08 | G3 Remote Config | **EN COURS** — colle code · terrain Editor + §0 dashboard |

## G3 — livré (code)

- `RemoteTuning` overlay (clones runtime, jamais mutation asset)
- `SeasonRotationManager.ApplyRemoteCalendar` (SetEpochMondayParis + length)
- `SeasonRewardsConfig` / `DifficultyConfig` ApplyOverride + swap cache
- Kill-switch `SeasonEnabled` + bandeau `SeasonPageUI`
- DBG **CONFIG** + suite `RemoteTuningIntegritySuite`
- Package `com.unity.remote-config`
- **Arthur §0** : 4 clés JSON neutres dashboard (voir prompt)
