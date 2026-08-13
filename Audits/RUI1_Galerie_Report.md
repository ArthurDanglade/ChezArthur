# RUI1 — Galerie socle — rapport

Date : 2026-08-13 23:40
Mode : CODE LIVRÉ (scène à générer via menu APPLIQUER)

## Origine des composants (G1)

- `UiTextStyle` / `ApplyTextStyle` → naissance RUI1 (map tokens existants)
- `CreatePanel(1..3)` → wrap `PanelSurface` + `panelLevel` (Hub `panelLevel=0` inchangé)
- `CreateSectionHeader` → naissance RUI1
- `CreateButton Primary/Secondary/Locked` → wrap `HubButtonUI` existant
- `CreateButton Danger` → extension `HubButtonUI.ButtonVariant`
- `CreateTabBar` → **WRAPPER** `TabBarUI` existant (G1)
- `CreateListRow` / `StatCell` / `Chip` / `RewardChip` → naissance RUI1
- `CreatePageScaffold` / `PopupScaffold` → naissance RUI1
- Rarity badges perso → `RarityBadgeView` + `RarityVisualLibrary` (BR I1/I2) — **G2**
- Rareté valise/bonus → tokens `UiTheme.Valise*` / `Bonus*` (liseré+label)

## Sandbox historique (G3)

- UIKitSandbox : PanelSurface samples / boutons / pills / TabBar
- Galerie RUI1 : typo + surfaces 1..3 + 4 boutons + SectionHeader + ListRow + StatCell + chips + raretés réelles + RewardChip + TabBar + scaffolds
- **Verdict G3 provisoire** : couverture ≥ sandbox → retraite sandbox (scène + builder) **après** checklist device OK ; sinon consigner le delta au rapport post-APPLIQUER

## Action Arthur

1. Menu `Chez Arthur/RUI/Galerie (RUI1) — APPLIQUER`
2. Ouvrir `Scenes/Dev/RUIGalerie.unity`
3. Comparer aux 10 sections de `Audits/RUI1/RUI1_Galerie_Maquette.html`
