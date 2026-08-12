# BR1 — Badges de rareté — rapport wiring

Date : 2026-08-12 22:20

## Traçabilité

- **Gate 5.c.1** = lane **Refonte Hub** (`DetailPopupPolishBuilder`, menus `Chez Arthur/Refonte Hub/Detail Popup — Polish 5.c.1`). Absente de la bibliothèque docs projet.
- Dette popup hors BR1 (propriétaire Refonte Hub) : `typeText`, `rarityChipText`, `rarityChipFrame` sérialisés morts.
- **BR-D5** : shine dans les frames SSR/LR → zéro overlay code (verdict « shine OFF » 5.c.1 respecté).
- **A1** : `badgeRarityText` / `badgeSprites` / `ApplyRarityBadge` purgés ; builders TeamPageRebuilder / CharacterCardPolishBuilder alignés.
- **§8.3 OK diff** sous conditions K1–K6 (doc v1.4).

## Écart MT0 documenté (une fois)

`CharacterDetailPopup.cs` mélange BR1 (`rarityBadge` Bind/SetPlaying) et polish 5.c.1 (Back flèche, nom=titre, InTeam) **déjà au HEAD** — pas de réécriture d'historique.

Hash de référence (dernier commit touchant le fichier) :
`f60723619512f4c207bc61fc467f3b2cb9436dbd` — polish 5.c.1 + BR1 déjà mélangés dans cet historique poussé (écart MT0 documenté une fois).

Discipline : commits suivants = staging sélectif par chemin (K1), jamais `git add .`.

## Conditions K1–K6 (pré-commit)

| Réf | Statut | Note |
|---|---|---|
| K1 | Prêt | Staging par chemin ; Hub TMP noise reverté ; hors BR1 : AndroidManifest, FeedbackCatalog, SFX, TMP Fallback |
| K2 | Fait | SR = Point (`badge_sr_*`) ; builder `EnsureSingleSpriteImport` filtre par préfixe ; SSR/LR = Bilinear |
| K3 | Fait | Sheets `badge_*_sheet.png` (+metas) **retirées** de `Assets/` ; vérité = `Frames/` |
| K4 | **Bloquant Arthur** | C8 A/B grille SSR/LR plein, device bas de gamme — voir checklist |
| K5 | Fait | mipmaps OFF, isReadable OFF, sRGB ON, FullRect, filtre par badge |
| K6 | À valider œil C3 | `framesPerSecond=10`, `idleFrameIndex=0` (SSR shine démarre à gauche ; LR frame 0 = pose train+shine). Ajuster lib sans code si besoin |

## Assets

- `Frames/badge_sr_00.png` — **1 frame** (statique)
- `Frames/badge_ssr_00…08.png` — **9 frames**
- `Frames/badge_lr_00…08.png` — **9 frames**
- Lib : GUIDs individuels uniquement

## Checklist C1–C10

| # | Vérif | Preuve code / static | Device (Arthur) |
|---|---|---|---|
| C1 | Badge grille SR/SSR/LR, coin HG, cadres OK | Bind + prefab playAnimation | ☐ œil (+ K2 SR Point) |
| C2 | Tap à travers badge → popup | `raycastTarget=false` forcé Bind | ☐ |
| C3 | Popup anim fluide, header sans texte rareté | SetPlaying Show/Hide | ☐ + K6 fps/idle |
| C4 | Popup fermé = anim coupée | `HidePopup` → SetPlaying(false) | ☐ |
| C5 | Post-gacha refresh badge | Bind dans Setup carte | ☐ |
| C6 | Ratios multi | anchors | ☐ |
| C7 | Builder re-run → 0 changement | A2 convergent | ☐ après import Unity |
| C8 | **BLOQUANT** profiler grille SSR/LR | flag playAnimation | ☐ K4 A/B |
| C9 | Ordre frames / boucle | `ToString("D2")` LoadFrameSequence | ☐ œil boucle |
| C10 | TeamSlot idle ×3, clic retrait OK | Bind + playAnimation=0 slots | ☐ |

## Convergence (dernier builder)

*À régénérer après re-run menu Unity post-K2/K3.*

**Résultat : en attente C8 device + re-run builder → commits K1.**
