# BR1 — Badges de rareté — rapport wiring / clôture

Date : 2026-08-13 19:50

## Traçabilité

- **Gate 5.c.1** = lane **Refonte Hub** (`DetailPopupPolishBuilder`). Absente de la bibliothèque docs projet.
- Dette popup hors BR1 : `typeText`, `rarityChipText`, `rarityChipFrame` → propriétaire Refonte Hub.
- **BR-D5** : shine dans les frames SSR/LR → zéro overlay code.
- **A1/A2** : purge orphelins + builder convergent.
- **§8.3** : OK diff sous K1–K6 (doc v1.4).

## Écart MT0 documenté (une fois)

`CharacterDetailPopup.cs` mélange BR1 + polish 5.c.1 déjà au HEAD.

Hash : `f60723619512f4c207bc61fc467f3b2cb9436dbd` — pas de réécriture d'historique.

## Conditions K1–K6

| Réf | Statut |
|---|---|
| K1 | OK — staging sélectif ; Feedback hors commit BR1 |
| K2 | OK — SR Point + builder par préfixe |
| K3 | OK — sheets retirées ; vérité = `Frames/` |
| K4 / C8 | **OK device (Arthur)** — anim grille conservée (pas de delta bloquant) |
| K5 | OK — mipmaps OFF, R/W OFF |
| K6 | OK œil — `fps=10`, `idleFrameIndex=0` |

## Assets

- SR : 1 frame · SSR : 9 · LR : 9 (`Frames/badge_*_XX.png`)

## Checklist C1–C10

| # | Résultat |
|---|---|
| C1–C7 | OK (Arthur) |
| C8 | OK (Arthur — A/B device, grille anim ON conservée) |
| C9–C10 | OK (Arthur) |

## Commits

Code/assets BR1 déjà au HEAD (commit bagué `43426db` et antérieurs).  
Ce rapport = clôture checklist + K.

**BR1 CLOS** → BR2 s'ouvre contre HEAD : RateUp, PullResult, dernier `GetRarityColor` (`CharacterEntryUI`). TeamSlot déjà servi (BR-D6).
