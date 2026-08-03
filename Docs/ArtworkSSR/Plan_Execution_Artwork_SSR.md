# Plan d'exécution — Chantier AW : transitions d'artwork SSR

**Take Five Games — Track Zero** · 3 août 2026 · **v1.1 — GO AW1 ACTÉ** (3 oui + amendements A–D intégrés, prompt v2 livré)
Compagnon de `Audit_Preparatoire_Artwork_SSR.md` (AW0) et du brief sync Cursor→Claude du 03/08 (vérité terrain locale). Référence visuelle : preview `ssr_transitions_preview.html` (artifact `ssr-transitions-preview`), jouée par Arthur avec les vrais artworks.

---

## 0. Cadre acté (brief + Go du 03/08)

1. **From scratch assumé** : tout l'existant in-game est un placeholder à remplacer, pas une base à patcher — `GachaBurnDissolve.shader` + `GachaPrimeBurnPlayer` (déchéance : braises soft aléatoires, pas d'hybride or→violet, pas de grille pixel, contemplation faible) et le cœur audiovisuel d'`AwakeningCeremonyController` v4 (whiteout + flash + fade, pas de reforge, pas le masque de noise du burn → symétrie AW-D2 absente).
2. Les deux beats sont à soigner ; doublons SSR **hors scope**.
3. **La plomberie data est saine et conservée** : `isAwakened`, `awakeningCeremonySeen`, `prefersDechuArtwork`, `PortraitStateResolver`. Portraits **animés** via `AnimatedPortraitData` (sheet Resources + timeline `PortraitFrame{cellIndex, duration}` + `GetCellUvRect`) et `CharacterArtworkView` — **pas** un simple `List<Sprite> + fps` (amendement B). Le chantier ne touche pas à la data.
4. Flux : Claude propose complet → Go Arthur → Cursor applique → contrôle du diff ligne à ligne.
5. Le sync GitHub du projet Claude est en retard sur le local : les prompts portent des gardes « aligne-toi sur l'existant sans élargir, signale ».
6. **Chemins confirmés** (amendement A) : textures + materials FX → `Assets/_Project/Art/FX/` (pas `Sprites/FX/`, jamais `Assets/Materials/` racine) ; config → `Data/UI/` ; prefab → `Prefabs/UI/` ; shaders → `Shaders/`.

## 1. Direction affinée (AW0 confirmé + portraits animés)

Le feel cible reste celui de la preview AW0 — elle corrige point par point les défauts constatés du placeholder : braises spawnées **sur le front réel** (échantillonnage CPU du même noise que le shader) vs aléatoires ; **hybride or → cendre violette** pendant la combustion ; front **quantifié sur la grille native de l'art** (4 bandes dures) ; **contemplation renforcée** (~1 s, liseré or pulsant, motes, respiration). Défauts v1 du SO = export preview du 03/08 (`Audit_Preparatoire_Artwork_SSR.md` §2) ; calibrage final sur device aux gates AW2/AW3.

Apport des portraits animés : la carte **continue de respirer pendant la brûlure et la reforge** (la vue interroge les sources de frames pendant toute la transition). Un artwork vivant qui brûle > une image figée qui brûle — upgrade gratuit du beat.

## 2. Durée d'ascension — **ACTÉ au Go : beat serré (~4,5 s), pas de greffe**

La reforge ne se greffe pas sur la cérémonie longue v4 ; le beat serré **remplace le cœur audiovisuel** de la cérémonie, dont on garde la coque (déclenchement, flags `awakeningCeremonySeen`, sortie tap). Motifs : rejouabilité (une cérémonie par SSR éveillé, toute la vie du jeu — la 10e doit encore faire plaisir), densité émotionnelle (anticipation compressée → climax plus fort ; l'emphase se mesure en intensité, pas en durée — charte F0 §3), cohérence du diptyque (3,5 s de chute / 4,55 s d'élévation, même masque, sens opposés), maintenabilité (un séquenceur, un SO). L'apothéose finit sur un état stable ; `SkipToEnd()` propre à tout moment.

## 3. Jeter / réutiliser

| Élément local | Sort |
|---|---|
| `GachaBurnDissolve.shader` | **Supprimé en AW2** (remplacé par `ArtworkTransition.shader` unifié) |
| `GachaPrimeBurnPlayer` | **Supprimé en AW2** (remplacé par `ArtworkTransitionDriver.PlayDecheance`) |
| Cœur AV d'`AwakeningCeremonyController` | **Remplacé en AW3** par `PlayAscension` ; la **coque** (déclenchement, flags, navigation, fond) est conservée |
| `AwakeningDissolve` / `AwakeningGlowAdditive` | **Intouchés** — gabarits réservés au combat (charte F0) |
| Data + `PortraitStateResolver` + flags + `AnimatedPortraitData` / `CharacterArtworkView` | **Conservés tels quels** — consommés en AW2/AW3 via un **adapter mince** implémentant `IPortraitFrameSource` (spécifié en AW2) |
| `SfxManager` / `AudioBuses` (F1) | **Réutilisés** — audio routé bus SFX, null-safe |
| Flow gacha (crank/porte/reveal/tap/récap) | **Conservé** — AW2 ne remplace que le moment d'artwork du reveal SSR |

## 4. Architecture AW1 (résumé — détail complet dans `Prompt_Cursor_AW1_v2.md`)

**Socle 100 % nouveau et dormant** (aucun appelant modifié, aucune scène) : `ArtworkTransition.shader` (shader UI unifié des deux sens, 4 bandes dures quantifiées, hybride, whiteout, rim, jitter, uv-rects par frame) + `UIAdditiveTint.shader` · `ArtworkNoise.cs` (fBm seedé **byte-identique** à la preview, partagé GPU/CPU) · `ArtworkTransitionConfig.cs` (SO de tuning = export preview) · `ArtworkTransitionMath.cs` (timelines + évaluations **pures du temps**, transposition verbatim) · `ArtworkTransitionGraphic` / `PixelParticleGraphic` (1 draw call par couche, ring buffer préalloué) / `ArtworkTransitionView` (carte/rayons/halo/vignette/flash + **shake local** RectTransform) · `ArtworkTransitionDriver` (séquenceur, événements, émetteurs sur le front CPU, API `PlayDecheance`/`PlayAscension`/`SkipToEnd`/`SetTime`) · **`IPortraitFrameSource`** (seul point de contact avec le système de portraits — AW1 livre `StaticPortraitSource` + `SimpleFlipbookSource` dev-only ; l'adapter `AnimatedPortraitData` arrive en AW2) · harness ContextMenu (dont test flipbook pendant combustion) · builder d'assets idempotent (chemins §0.6) · auditeur lecture seule.

## 5. Gates

| Gate | Périmètre | Critère |
|---|---|---|
| **AW1 — Socle** (prompt v2 livré) | Fichiers ci-dessus, 2 commits (code / assets générés), zéro scène | Harness : les 2 séquences conformes à la preview ; frames animées avancent pendant la transition ; idempotence builder ; 0 alloc/frame en régime ; dormance vérifiée |
| **AW2 — Intégration déchéance** | Adapter mince `AnimatedPortraitData → IPortraitFrameSource` ; `GachaAnimationController.RevealCharacter` branche le driver pour **tout nouveau SSR** (décision Go n°1) ; purge `GachaBurnDissolve` + `GachaPrimeBurnPlayer` ; câblage scène par builder ; clips provisoires | A/B : flow gacha inchangé hors moment SSR ; déchéance in-game = preview, sur les vrais portraits animés |
| **AW3 — Intégration ascension** | Cœur AV d'`AwakeningCeremonyController` remplacé par `PlayAscension`, coque conservée, skip/tap | Cérémonie = beat serré ; flags/résolveur intacts ; re-visionnage OK |
| **AW4 — Sons & polish** | Clips banque D2 sur les slots, haptique (si D6 posé), passe perf APK, tuning final sur device | Checklist à l'aveugle : les 2 beats distincts à l'oreille ; 0 alloc confirmé sur APK |

Boucle par gate inchangée (méthode U1) : proposition → Go → prompt → push → contrôle diff ligne à ligne → checklist → commit.

## 6. Décisions au Go (03/08)

| Réf. | Décision | Statut |
|---|---|---|
| GO-1 | Déchéance jouée pour **tout nouveau SSR** (pas seulement rate-up) — appliqué en AW2 | ACTÉ |
| GO-2 | Ascension = **beat serré ~4,5 s**, coque cérémonie conservée, cœur AV remplacé en AW3 | ACTÉ |
| GO-3 | Purges différées : burn gacha en AW2, cœur cérémonie en AW3 ; AW1 dormant, placeholders intacts | ACTÉ |
| A | Chemins réels : `Art/FX/` pour textures + materials, `Data/UI/` config, `Prefabs/UI/`, `Shaders/` | INTÉGRÉ (prompt v2) |
| B | Portraits animés = `AnimatedPortraitData` (sheet + `PortraitFrame{cellIndex, duration}` + `GetCellUvRect`) ; AW1 expose `IPortraitFrameSource`, ne fige aucune API `Sprite[]` comme contrat ; adapter spécifié en AW2 | INTÉGRÉ (prompt v2) |
| C | Docs versionnés côté repo : ce dossier `Docs/ArtworkSSR/` (audit AW0 + plan), commit `docs(aw)` | LIVRÉ |
| D | `Prompt_Cursor_AW1_1.md` = doublon de téléchargement à ignorer ; **seule référence : `Prompt_Cursor_AW1_v2.md`** | CONFIRMÉ |

## 7. Journal

| Date | Gate | Verdict |
|---|---|---|
| 03/08 | AW0 — direction + preview | ✅ Validée (jouée avec vrais artworks) ; défauts v1 figés = export preview |
| 03/08 | AW1 — proposition + prompt v1 | Livrés — 3 questions posées |
| 03/08 | AW1 — **Go acté** (3 oui + amendements A–D) | **Prompt v2 livré** (`Prompt_Cursor_AW1_v2.md`) — prêt à coller ; prochaine étape : push Arthur → contrôle du diff ligne à ligne |
