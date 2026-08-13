# BR2 — Badges cohérence invocation — CLOS

Date clôture : 2026-08-13 20:54

## Statut

**CLOS** — chantier badges rareté (BR1 + BR2) terminé. Pas d’extension showcase.

## KB1 — Chrome PullResult

- `ssrGlow` / `rarityTopBorder` : rôle **rareté uniquement** → purge champ + GO (A1), pas de masquage.
- Option B : badge remplace le chrome sur grille + single-card.

## KB2 — CharacterEntryUI (décision actée)

| | Local (avant) | UiTheme.Rarity* |
|---|---|---|
| SR | (0.6, 0.8, 1) | #99CCFF |
| SSR | (1, **0.84**, 0) | #FFD700 → G = **0.843137…** |
| LR | (0.8, 0.5, 1) | #CC80FF |

**Décision** : combat → `CharacterRarityPalette` / `UiTheme.RaritySSR` (**G = 0.843**).  
Écart **ΔG ≈ 0.003** consigné (pas absorbé dans un « ≈ »).

## GachaSummaryBuilder

Diff limité A1 : chrome out + câblage badge + pads icône.  
`CreateGlow` / `CreateTopBorder` laissés sans appelant (pas de purge méthodes).

## Surfaces

| Surface | Badges ? | Note |
|---|---|---|
| Collection / détail / TeamSlot | Oui (BR1) | |
| PullResult grille + single | Oui (BR2) | |
| RateUpPopupUI + prefab | Oui (BR2) | Orphelin UX : carte portail Gate 6.b ouvre **Personnages** (showcase), plus le bouton Rate Up |
| Showcase Personnages | Non | Hors scope — texte TMP volontaire à la clôture |
| Taux d’apparition | Non | Pourcentages, pas badges |

## Livrables commit

- `RateUpCharacterEntryUI` / prefab — badge idle, purge RarityText
- `PullResultEntryUI` + prefabs grille/single — badge, purge chrome
- `CharacterEntryUI` — palette
- `RarityBadgeBr2WiringTool` — menu BR2
- `GachaSummaryBuilder` — invariant A1

## Hors commit (autres lanes dirty)

Feedback burn/poison, Buffs, CharacterBall, packages-lock, Resources UnityPlayerAccount, TMP fallback, etc.
