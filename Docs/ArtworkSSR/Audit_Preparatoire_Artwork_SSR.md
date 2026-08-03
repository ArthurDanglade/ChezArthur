# Audit préparatoire — Chantier Artwork SSR (Déchéance & Ascension)

**Take Five Games — Track Zero** · 3 août 2026 · v1
*(Document AW0, figé tel quel. La vérité terrain locale a depuis été confirmée et amendée par le brief sync Cursor→Claude du 03/08 — voir `Plan_Execution_Artwork_SSR.md` §0 : data via `AnimatedPortraitData`, placeholders `GachaBurnDissolve`/`GachaPrimeBurnPlayer`/cœur `AwakeningCeremonyController` remplacés from scratch.)*

Chantier « AW » : les deux transitions d'artwork SSR — **Déchéance** (obtention : prime affiché ~1 s → brûlure → déchu) et **Ascension** (déblocage du prime en jouant : évolution façon Pokémon).
Référence visuelle jouable : artifact **`ssr-transitions-preview`** (preview HTML/WebGL, mêmes maths que le futur shader Unity).

---

## 0. Décisions actées (interview du 03/08)

| Réf. | Décision | Statut |
|---|---|---|
| AW-D1 | Déchéance = **hybride « chute »** : combustion dorée (couleur SSR) dont les braises virent cendre/violet à mesure que le déchu se révèle. L'effet raconte la chute de la légende. | FIGÉ |
| AW-D2 | Ascension = **pulses + reforge dorée** : anticipation par pulsations lumineuses accélérantes (white-out), climax, puis le prime se reconstruit par un front d'or — l'inverse exact de la brûlure, **même masque de noise**. La déchéance tombe (front descendant), l'ascension remonte (front montant). | FIGÉ |
| AW-D3 | Artworks **pixel art** → effet entièrement quantifié sur la grille de l'art (front par blocs, braises carrées), glow additif doux par-dessus (cohérence D1 charte F0 : matière pixel nette + énergie glow). | FIGÉ |
| AW-D4 | **Zéro asset payant** : shaders + scripts custom, tout procédural (cohérent D1/D2, budget 0 €, aucune dépendance). Pas de DOTween — coroutines + courbes comme l'existant. | FIGÉ |
| AW-D5 | Plomberie data (champs d'artworks prime/déchu, flag de déblocage, sauvegarde, déclencheur) : **déjà codée en local** (en avance sur le sync GitHub lu ici). Le chantier ne porte que sur les 2 séquences et leur intégration. Vérité terrain confirmée par le brief du 03/08. | ACTÉ |

---

## 1. Vérité terrain (sync GitHub au 03/08)

- **Gacha** : `GachaAnimationController` (crank → porte → reveal → récap), reveal = `characterArtwork` (UGUI `Image`) posé sec (`data.Portrait ?? data.Icon`), `ssrEffects` **non câblé en scène** (`fileID: 0`), `smokeTransition` entre persos, tap-to-continue, `revealDuration = 2`. Sons méta via `SfxManager` (`revealsound`, `risersound`, lever à remplacer — F1-P2 l'a traité).
- **Briques réutilisables** : `AwakeningDissolve` / `AwakeningGlowAdditive` (whiteout de l'éveil — parenté visuelle à respecter sans confusion : l'Ascension doit rester distincte et au-dessus), `SpriteFlash` (MPB), `CameraShake` (trauma², unscaled), coroutines + easing manuel partout (pas de DOTween).
- **Contraintes** : Unity 2022.3, portrait, Android d'abord ; `.cursorrules` (pooling FX, zéro alloc en boucle, injection SerializeField, builders éditeur idempotents, proposition → Go → code) ; audio via bus SFX (F1) ; `Time.timeScale` interdit hors pics existants — **aucun timescale dans ces séquences** (tout en temps réel, hitstop non nécessaire).
- **UI concernées** : reveal gacha (déchéance) ; fiche perso / écran de déblocage (ascension — cérémonie plein écran recommandée, pas un inline dans `CharacterDetailPopup`).

## 2. Découpage des séquences (valeurs par défaut de la preview)

**Déchéance (~3,5 s)** — front **descendant** (la chute), consumé depuis le haut :
1. **Apparition** (0 → 0,15 s) : flash blanc bref sur le prime, punch scale 1,12 → 1, trauma 0,4, sting brillant + corps grave.
2. **Contemplation** (~1,0 s, tunable) : liseré or pulsant sur le cadre, respiration légère, motes dorées montantes, shimmer doux — le joueur *voit ce qu'il pourrait avoir*.
3. **Ignition** (0,1 s) : whoosh d'allumage, crépitement démarre, micro-trauma.
4. **Combustion** (~1,45 s) : dissolve quantifié piloté par noise + gradient directionnel (`h = mix(noise, grad, 0.80)`), 4 bandes dures (charbon / cœur blanc-or / braise / préchauffe), braises pixel montantes spawnées **sur le front réel** (échantillonnage CPU du même noise), cendres teintées **par les pixels du prime** qui tombent, palette or → violet (`hybrid` en inQuad 22 % → 95 %), jitter chaleur ±1 px, vignette 0,16 → 0,38, 2 micro-traumas, crackle ∝ intensité.
5. **Retombée** (0,9 s) : bandes s'éteignent, rémanence braise refroidit, sag scale −1,6 %, whoosh descendant, liseré braise faible. État final : déchu, vignette 0,22.

**Ascension (~4,55 s)** — front **montant** (l'élévation), même masque de noise :
1. **Frémissement** (0,85 s) : jitter subtil du déchu, liseré chaud naissant, riser (s'éteint 70 ms avant le climax — le silence avant l'impact).
2. **Pulsations** (~1,25 s, 3 pulses à intervalles en accélération ratio 0,68) : white-out par vagues croissantes (pics 0,30 → 0,75 + plancher montant), punch scale croissant, note montante + trauma par pulse, étincelles convergentes.
3. **White-out** (0,30 s + 0,08 s tenu) : silhouette blanc pur, rayons + halo derrière, aspiration continue.
4. **Climax** : flash plein écran, boom + accord ré majeur détuné + sparkles, trauma 0,6, burst radial ~150 étincelles pixel.
5. **Reforge** (~1,10 s) : le blanc se déchire de bas en haut, front or/blanc (jamais de cendre), rémanence dorée sur le prime révélé, étincelles montantes sur le front, chœur chaud (saws détunées + shimmer montant), scale 1,07 → 1.
6. **Apothéose** (0,95 s) : liseré or respirant, poussière de lumière, rayons s'effacent.

## 3. Architecture Unity cible (AW1)

Voir `Plan_Execution_Artwork_SSR.md` §4 et `Prompt_Cursor_AW1_v2.md` (spec complète). Résumé : shader UI unifié des deux sens + shader additif · noise fBm seedé partagé GPU/CPU (byte-identique à la preview) · SO de tuning (= export preview) · maths d'évaluation pures du temps · particules pixel + glow en 1 draw call par couche · vue autonome Hub (shake local RectTransform) · driver séquenceur · `IPortraitFrameSource` comme unique point de contact avec le système de portraits · builders/auditeur éditeur idempotents.

## 4. Gates

| Gate | Périmètre | État |
|---|---|---|
| AW0 | Direction + preview jouable + valeurs par défaut | ✅ 03/08 (artifact `ssr-transitions-preview`) |
| AW1 | Shader + Config SO + Driver (code dormant, harness de test ContextMenu) | Go acté 03/08 — prompt v2 livré |
| AW2 | Intégration reveal gacha (déchéance, tout nouveau SSR) + adapter portraits + purge placeholders burn | après AW1 |
| AW3 | Cérémonie d'ascension : cœur AV remplacé, coque conservée | après AW2 |
| AW4 | Sons banque D2 + haptique (si D6 posé) + passe perf APK | fin de chantier |

## 5. Points ouverts (état au Go)

1. ~~Vérité terrain locale du système prime/déchu~~ — **résolu 03/08** (brief : `AnimatedPortraitData`, `isAwakened`, `awakeningCeremonySeen`, `prefersDechuArtwork`, `PortraitStateResolver`).
2. ~~Où vit la cérémonie d'ascension~~ — **résolu** : coque d'`AwakeningCeremonyController` conservée, cœur AV remplacé (AW3).
3. Doublons SSR (déjà possédé) : **hors scope** (décision Arthur 03/08).
4. Calibrage final sur les **vrais artworks** : preview jouée avec les vrais artworks ✅ ; retune fin sur device aux gates AW2/AW3 (export JSON preview → SO).
