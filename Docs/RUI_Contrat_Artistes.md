# RUI — Contrat artistes v1

Règle : **habillage remplaçable sans toucher la structure**. Les builders posent des zones nommées + 9-slice ; les artistes swapent les sprites.

## Slots skinnables (noms stables)

| Composant | Slot / enfant | État |
|---|---|---|
| `PanelSurface` | Image racine (bordure) + enfant `Fill` | Deep/Panel/Elevated via `panelLevel` ; bordures Subtle/Amber/Gold |
| `HubButtonUI` | racine + `Fill` + `Label` + `SubLabel` | Primary / Secondary / Danger / Locked |
| `TabBarUI` | `TabItemTemplate` → `Fill` + `Label` (+ `Icon`) | Active / Inactive |
| `SectionHeaderUI` | `AccentBar` + `Title` + `Count` | — |
| `ListRowUI` | `Avatar` + `Name` + `Meta` + `HpBar` | Frame couleur = rareté perso (badge séparé) |
| `StatCellUI` | fond + `Label` + `Value` | Accent PV/ATK/DEF/VIT |
| `UiChipUI` / `RewardChipUI` | fond + `Label` / `Icon`+`Amount` | — |
| `RarityBadgeView` | Image (frames lib) | SR/SSR/LR — **ne pas** redessiner hors lib |
| `PageScaffold` | `HeaderZone` / `TitleZone` / `ScrollZone` / `FooterZone` | Hauteurs tokens |
| `PopupScaffold` | `Scrim` + `Card` | Micro-décision |

## Sprites 9-slice

Générés : `RoundedRect_S/M/L` (RadiusS/M/L). Remplacer = même noms / mêmes border slices.

## Interdit artistes

- Modifier la hiérarchie des zones scaffold
- Mélanger palette perso et valise
- Poser du texte hors zone titre sur une page
