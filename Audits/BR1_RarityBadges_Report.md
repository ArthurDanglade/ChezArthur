# BR1 — Badges de rareté — rapport wiring

Date : 2026-08-12 01:06

## Traçabilité

- **Gate 5.c.1** = lane **Refonte Hub** (`DetailPopupPolishBuilder`, menus `Chez Arthur/Refonte Hub/Detail Popup — Polish 5.c.1`). Absente de la bibliothèque docs projet.
- Dette popup hors BR1 (propriétaire Refonte Hub) : `typeText`, `rarityChipText`, `rarityChipFrame` sérialisés morts.
- **BR-D5** : 5.c.1 a coupé le shine popup — ne pas réintroduire un shine SSR/LR sans retrouver ce verdict.
- **A1** : refs `badgeRarityText` / `badgeSprites` aussi dans `TeamPageRebuilder` et `CharacterCardPolishBuilder` → alignés pour ne plus recréer les orphelins.

- Dossier Rarity/ déjà présent ✓
- Import OK `badge_sr_sheet.png` ✓
- Import OK `badge_ssr_sheet.png` ✓
- Import OK `badge_lr_sheet.png` ✓
- RarityVisualLibrary.asset déjà présent ✓
- Librairie frames déjà à jour ✓

## CharacterCard.prefab

- BadgeText déjà absent ✓
- RarityBadgeView déjà présent ✓
- Placement haut-gauche déjà OK ✓
- rarityBadge déjà câblé ✓

## CharacterDetailPopup.prefab

- RarityBadge déjà présent sous Artwork ✓
- Placement badge popup déjà OK ✓
- Back déjà OK ✓
- InTeamBadge déjà off ✓
- NameText → titre StatsPanel ✓
- RarityBadgeView déjà présent ✓
- rarityBadge déjà câblé ✓

## TeamSlotUI (scène Hub)

- TeamSlotUI traités : 4

## Convergence

- Popup : nom = titre encart stats

**Résultat : 1 changement(s)** — re-run jusqu'à 0.
