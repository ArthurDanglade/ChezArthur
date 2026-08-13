# Plan d'exécution — MT4 Backend & comptes

**Take Five Games — Track Zero** · 13 août 2026 · v1.3 — **G2-P2 en cours (collé) · P1/G1/G5 CLOS**

Contrat : `Cahier_Charges_Backend_MT4.md` · Go UGS 13/08. Offline-first non négociable.

| Gate | État |
|---|---|
| **MT4-0** | ✅ CLOS — cahier + Go UGS |
| **MT4-G1** | ✅ **CLOS** — Auth + Cloud Code + ancre `GameClock` + HF1 · `ebac2f3` / `0a11cb4` / `ac19184` |
| **MT4-G2** | ⏳ **P1 ✅ CLOS** · **P2 collé** — GPGS link + fix résumés équiv · terrain Editor + device §0 |
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
| 13/08 | G2-P2 prompt + colle | **EN COURS** — code collé ; §0 console Arthur ; Editor suite #07/#08 ; device MANUAL |

## G2-P2 — livré (code)

- `BackendService` : `LinkWithGoogleAsync` / `ConfirmSwitchToLinkedGoogleAsync` / `UnlinkGoogleAsync` (QA) · `#if UNITY_ANDROID && !UNITY_EDITOR`
- `CloudSaveSync` : résumés équivalents + fp divergents → auto dernier-écrivain
- `SettingsPanelUI` + `AccountRowBuilder` (menu Backend → Build Account Row)
- DBG COMPTE · suite G2 #07 équiv / #08 Editor inerte
- **Arthur** : importer GPGS v11+ · §0 SHA-1 ×2 · Web Client ID/Secret dashboard · builder scène · commit scène séparé
