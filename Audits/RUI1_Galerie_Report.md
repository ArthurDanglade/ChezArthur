# RUI1 — Galerie socle — rapport

Date : 2026-08-13 23:55
Mode : APPLIQUER

## Origine des composants (G1)

- `UiTextStyle / ApplyTextStyle` → naissance RUI1 (map tokens existants)
- `CreatePanel(1..3)` → wrap PanelSurface + panelLevel (Hub panelLevel=0 inchangé)
- `CreateSectionHeader` → naissance RUI1
- `CreateButton Primary/Secondary/Locked` → wrap HubButtonUI existant
- `CreateButton Danger` → extension HubButtonUI.ButtonVariant
- `CreateTabBar` → WRAPPER TabBarUI existant (G1)
- `CreateListRow / StatCell / Chip / RewardChip` → naissance RUI1
- `CreatePageScaffold / PopupScaffold` → naissance RUI1
- `Rarity badges perso` → RarityBadgeView + RarityVisualLibrary (BR I1/I2)
- `Rareté valise/bonus` → tokens UiTheme.Valise* / Bonus* (liseré+label)

## Sandbox historique (G3)

- UIKitSandbox couvre PanelSurface samples / boutons / pills / TabBar.
- Galerie RUI1 couvre typo + surfaces 1..3 + 4 boutons + SectionHeader + ListRow + StatCell + chips + raretés réelles + RewardChip + TabBar + scaffolds.
- **Verdict G3** : couverture ≥ sandbox → retraite sandbox possible (scène + builder) après checklist device ; sinon consigner le delta.


## Prefabs

- `Assets/_Project/Prefabs/UI/RUI/ListRow.prefab` ✓

**Résultat : scène Galerie construite (10 sections).**
