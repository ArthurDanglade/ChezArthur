# RUI — Règles d'usage (v1)

Source : maquette Galerie validée (RUI-D6) + audit §3.

1. **Raretés jamais croisées** : personnages = `RarityBadgeView` / `RarityVisualLibrary` (BR I1/I2). Valises & bonus = liseré + label via `UiTheme.Valise*` / `Bonus*` — jamais les couleurs perso sur une valise.
2. **3 niveaux de surface max** : Deep / Panel / Elevated (`CreatePanel(1..3)`). Pas de 4ᵉ fond improvisé.
3. **PageScaffold obligatoire** pour toute page (Header 112 / Titre / Scroll / Footer 152). Titres dans la zone titre — jamais sous le header.
4. **Popups = micro-décisions seulement** (RUI-D2). Contenus riches → pages.
5. **Chip synergie fermé par défaut** (extensible au tap).
6. **Touch min 96** (`UiTheme.TouchTargetMin`).
7. **Boutons** : Primary / Secondary / Danger / Locked+condition (`SetSubLabel`) — une seule famille `HubButtonUI`.
8. **Typo** : `UiTextStyle` uniquement (Display/H1/H2/Body/Caption/Chip) via `UiKitFactory.ApplyTextStyle`.
