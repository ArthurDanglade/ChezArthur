# Audit + Plan d'exécution — Chantier BR : Badges de rareté (SR · SSR · LR)

**Take Five Games — Track Zero** · 11 août 2026 · **v1.2 — OUVERT · BR1 EN COURS (2ᵉ Go accordé 11/08, §8.1)**
Périmètre : poser les **vrais badges de rareté** (assets Arthur, animés) sur le **popup détail personnage** et la **grille collection**, au service de la lisibilité de la rareté et de la jouissance de collection. Référence produit : **Dokkan Battle** (badge coin haut-gauche, gros, débordant du cadre — la rareté se lit d'un coup d'œil en grille, se contemple en fiche).
Canal : sync GitHub du projet (vérité terrain relevée le 11/08) ; prompts écrits contre HEAD au moment du gate (pattern AW-D5).

---

## 0. Décisions actées (interview du 11/08)

| Réf. | Décision | Statut |
|---|---|---|
| BR-D1 | Au popup détail, **le badge remplace le texte de rareté** : `typeText` passe de « SR • Attacker » à « Attacker » (rôle seul). La rareté n'est portée que par le badge, comme Dokkan. | FIGÉ |
| BR-D2 | Périmètre en gates : **BR1** = popup + collection · **BR2** = passe de cohérence (TeamSlot, RateUp, PullResult) + résorption de la dette couleurs · **BR3** = verdict device + clôture. Le **bandeau du reveal est exclu** : sa refonte appartient au chantier INV (I4) — il consommera la même librairie de visuels à son tour. | FIGÉ |
| BR-D3 | Les badges sont **animés** : Arthur transforme ses 3 GIF en **spritesheets** → flipbook léger côté code (ni Animator, ni DOTween). **Shine léger en plus sur SSR et LR** (SR animé sans shine). | FIGÉ |
| BR-D4 | **Popup = animé · grille = frame statique par défaut.** Raisons : perf canvas (50+ cartes qui re-dirty le Canvas à ~10 fps = rebuilds permanents sur mobile) et lisibilité (l'anim est illisible à ~40 px). Le flipbook reste activable par flag → verdict device en BR3, profiling à l'appui. | **FIGÉ** (Go du 11/08 — reco Cursor alignée) |
| BR-D5 | Shine SSR/LR : **à trancher à la livraison des spritesheets**. Si le shine est déjà dans les frames → rien à coder. Sinon → overlay additif léger réutilisant les briques AW (`UIAdditiveTint`, langage pixel + glow D1), popup uniquement. | OUVERT |

---

## 1. Vérité terrain (sync GitHub au 11/08)

**Rareté — modèle** : `CharacterRarity { SR, SSR, LR }` (`Scripts/Characters/CharacterRarity.cs`). Trois valeurs, pas de « R » : **les 3 badges couvrent 100 % du roster**. `CharacterData.Rarity` exposé partout.

**Surfaces qui affichent la rareté aujourd'hui** :

| Surface | Fichier | Affichage actuel | Sort |
|---|---|---|---|
| Grille collection (page Équipe) | `Hub/Pages/CharacterCardUI.cs` | cadre par rareté `rarityBorder.sprite = rarityFrameSprites[(int)data.Rarity]` (version récente du sync) | **BR1** : badge en overlay, cadres inchangés |
| Popup détail | `Hub/Pages/CharacterDetailPopup.cs` | texte `typeText` = « SR • Attacker » ; aucun badge | **BR1** : badge animé sur l'artwork, texte → rôle seul (BR-D1) |
| Slots d'équipe | `Hub/Pages/TeamSlotUI.cs` | `rarityBorder.color` teintée | **BR2** |
| Popup rate-up | `Hub/Pages/Invocation/RateUpCharacterEntryUI.cs` | `rarityText` + border teintée | **BR2** |
| Récap d'invocation | `Hub/Pages/Invocation/PullResultEntryUI.cs` | border teintée | **BR2** |
| Bandeau reveal | `GachaRevealStatusUI` | nom/rareté/niveau sur scrim | **hors périmètre** — chantier INV (I4), consommera la librairie BR |

**Dette relevée** : `GetRarityColor(CharacterRarity)` est **dupliqué à l'identique dans 4 fichiers** (CharacterCardUI ancien, TeamSlotUI, RateUpCharacterEntryUI, PullResultEntryUI) — même switch, mêmes couleurs (SR bleu clair `(0.6, 0.8, 1)` · SSR or `(1, 0.84, 0)` · LR violet `(0.8, 0.5, 1)`). L'indexation `rarityFrameSprites[(int)rarity]` est fragile (casse silencieusement si l'enum bouge). → BR2 centralise tout dans la librairie.

**Pièges identifiés** :
1. Le popup se cache via **CanvasGroup alpha=0, GameObject actif** (`HidePopup`). Un flipbook naïf continuerait de tourner popup fermé → le `Update` du badge doit être coupé par `HidePopup`/`ShowPopup`.
2. Le badge en grille est posé **par-dessus une carte cliquable** → `raycastTarget = false` obligatoire, sinon il vole les taps du `cardButton`.
3. L'index sync contient **deux versions** de certains fichiers (CharacterCardUI, CharacterDetailPopup, TeamSlotUI — anciennes et récentes) : le prompt BR1 est écrit contre la version récente, avec garde-fou « adapte les points d'ancrage au code réel à HEAD, ne réécris jamais un fichier entier ».

**Briques réutilisables** (chantier AW) : `UIAdditiveTint`, `PixelParticleGraphic`, noise partagé — candidates pour le shine BR-D5. Interdits habituels : socle `Scripts/UI/ArtworkTransition/**` figé, banque AW intouchée.

**Impacts nuls vérifiés** : sauvegarde (pur affichage), localisation (aucun texte ajouté ; BR-D1 retire même une chaîne composée — un souci de loc en moins), gameplay/combat (aucune zone touchée).

---

## 2. Contrat d'assets (à livrer avant BR1)

Les 3 badges existent en GIF (SR pièce · SSR lettrage or · LR emblème au train). À livrer en **spritesheets** :

| Spec | Valeur |
|---|---|
| Layout | grille ou bande, **frames de taille strictement identique**, fond transparent |
| Padding | **2 px entre frames** (évite le bleeding en filtrage bilinéaire) |
| Taille | ≤ 2048 px de côté ; frame source idéalement 256×256 |
| Nommage | `badge_sr_sheet.png` · `badge_ssr_sheet.png` · `badge_lr_sheet.png` |
| Destination | `Assets/_Project/Sprites/UI/Rarity/` |
| Cadence cible | 8–12 fps (à figer par badge dans la librairie) |

**Import Unity** : Sprite Mode **Multiple** (slice par grille), mipmaps OFF, Read/Write OFF, sRGB ON, Max Size 2048, compression **ASTC 6×6** d'abord (→ None si artefacts visibles sur les ors), filtre Point si rendu pixel-art strict (SR), Bilinear sinon — verdict à l'œil, par badge.

> **Offre** : envoie-moi les 3 GIF dans la conversation — je produis les spritesheets propres (frames alignées, padding, dimensions puissance de 2) + les valeurs exactes de découpe pour l'import. Ça t'évite l'outillage.

**Démarrage sans attendre** : le code BR1 est conçu pour que **1 frame = badge statique** (`Sprite[]` de taille 1 → flipbook inerte, zéro coût). On peut donc poser BR1 avec les PNG statiques actuels et brancher les sheets à leur livraison **sans retoucher une ligne de code** — seule la librairie (asset) change.

**Vigilance lisibilité** : le LR (train + fumée) est très détaillé — à ~40 px en grille il peut fondre. Verdict visuel au device en BR3 ; si illisible, variante simplifiée petit format (décision d'artiste, pas de code).

---

## 3. Architecture cible

**Une source de vérité** pour tous les visuels de rareté, consommée par toutes les surfaces (et par le chantier INV plus tard) :

1. **`Scripts/Characters/RarityVisualLibrary.cs`** — ScriptableObject (`Chez Arthur/Rarity Visual Library`), asset dans `ScriptableObjects/Config/`. Trois blocs sérialisés explicites (`srVisuals` / `ssrVisuals` / `lrVisuals` — **pas** de tableau indexé par cast d'enum) : `Sprite[] badgeFrames`, `int idleFrameIndex`, `float framesPerSecond`, `Color accentColor` (reprend les couleurs actuelles). API par switch, null-safe : `GetBadgeFrames`, `GetIdleFrame`, `GetFps`, `GetAccentColor`.
2. **`Scripts/UI/RarityBadgeView.cs`** — MonoBehaviour (+ Image) : `Bind(CharacterRarity)` pose la frame idle, force `preserveAspect = true` et `raycastTarget = false` ; `SetPlaying(bool)` pilote le flipbook (`enabled` seulement si `playAnimation && frames.Length > 1`). Update en temps non-scalé, n'assigne le sprite **qu'au changement de frame**, zéro alloc, tout est caché au Bind.
3. **Branchements** : `CharacterCardUI` (statique) et `CharacterDetailPopup` (animé, couplé à `ShowPopup`/`HidePopup`) reçoivent un champ `RarityBadgeView rarityBadge` + un appel `Bind` — diffs minimaux, existant intouché.
4. **Wiring par builder éditeur idempotent** (règle MT0 : jamais de bricolage manuel) : `[MenuItem "Chez Arthur/UI/BR1 — Poser les badges de rareté"]` — crée l'asset librairie s'il manque, ajoute le GO `RarityBadge` (Image + View) au prefab carte et au popup (prefab **ou** objet de scène — les deux cas gérés), ancres haut-gauche avec léger débord (Dokkan), assigne les références par SerializedObject, rapport dans `Audits/`, re-run = 0 changement.

**Placement par défaut (tunable inspector, verdict à l'œil)** : carte collection → coin haut-gauche, ~36 % de la largeur de la carte, débord ~−6 px hors cadre · popup → coin haut-gauche de l'artwork, ~140 px, sans recouvrir nom/niveau.

**Perf (budget mobile)** : grille = zéro Update actif, zéro dirty canvas ; popup = 1 flipbook ~10 fps, coupé popup fermé ; un seul asset SO partagé ; aucune alloc runtime après Bind.

---

## 4. Gates

| Gate | Périmètre | Entrée | Sortie |
|---|---|---|---|
| **BR1** | Librairie + View + badge posé (collection statique, popup animé + BR-D1) + builder + checklist device | Go Arthur (+ sheets, ou PNG 1-frame en attendant) | diff contrôlé ligne à ligne, checklist verte, commit `feat:` |
| **BR2** | Cohérence : badge sur TeamSlot / RateUp (remplace `rarityText`) / PullResult + **purge des 4 `GetRarityColor` dupliqués** → `accentColor` de la librairie + migration de `rarityFrameSprites` vers la librairie | BR1 clos | plus **une seule** définition des visuels de rareté dans le code |
| **BR3** | Verdict device : lisibilité LR en grille, anim grille oui/non (BR-D4, profiling), shine SSR/LR (BR-D5), tuning tailles/fps → clôture chantier | BR2 clos + sheets finales | chantier CLOS au doc |

Un gate à la fois ; le prompt BR2 sera écrit contre HEAD après la clôture de BR1.

---

## 5. Prompt Cursor — Gate BR1 (à coller après ton Go)

```
CONTEXTE
Chez Arthur — Unity 2022.3 LTS, mobile portrait, UGUI. Chantier BR (badges de rareté), gate BR1.
Objectif : poser les vrais badges de rareté (SR/SSR/LR) via une librairie centralisée :
1) grille collection (CharacterCardUI) — badge frame statique, coin haut-gauche de la carte ;
2) popup détail (CharacterDetailPopup) — badge ANIMÉ (flipbook) sur l'artwork, et le texte
   de rareté disparaît du header (le badge la porte seul).
Respecte .cursorrules : proposition AVANT code puis attente de mon Go explicite ; structure
de script maison ; commentaires FR / noms EN ; injection SerializeField ; zéro alloc en boucle.

VÉRITÉ TERRAIN
Travaille sur l'état RÉEL des fichiers à HEAD. Si un point d'ancrage cité diffère (le repo
bouge), adapte-toi au code réel — ne réécris JAMAIS un fichier entier, diffs minimaux.

ASSETS (déjà posés par moi avant ce prompt)
Assets/_Project/Sprites/UI/Rarity/ : badge_sr_sheet.png, badge_ssr_sheet.png, badge_lr_sheet.png
(Sprite Mode Multiple, déjà slicés — potentiellement 1 seule frame chacun pour l'instant :
le code doit traiter 1 frame = statique sans cas particulier).

FICHIERS À CRÉER
1) Assets/_Project/Scripts/Characters/RarityVisualLibrary.cs — namespace ChezArthur.Characters
   ScriptableObject, [CreateAssetMenu(fileName = "RarityVisualLibrary",
   menuName = "Chez Arthur/Rarity Visual Library")].
   Classe sérialisée interne RarityVisuals { Sprite[] badgeFrames; int idleFrameIndex = 0;
   float framesPerSecond = 10f; Color accentColor; } et TROIS champs explicites :
   srVisuals, ssrVisuals, lrVisuals (INTERDIT : tableau indexé par (int)rarity).
   Défauts accentColor : SR (0.6, 0.8, 1) · SSR (1, 0.84, 0) · LR (0.8, 0.5, 1).
   API publique par switch, null-safe (frames null/vides → retour null + LogWarning une seule
   fois par rareté, flag privé) : GetBadgeFrames(CharacterRarity), GetIdleFrame(CharacterRarity),
   GetFps(CharacterRarity), GetAccentColor(CharacterRarity).

2) Assets/_Project/Scripts/UI/RarityBadgeView.cs — namespace ChezArthur.UI
   [RequireComponent(typeof(Image))]. SerializeField : RarityVisualLibrary library;
   bool playAnimation. Privés cachés au Bind : _image, _frames, _frameCount, _fps,
   _frameIndex, _nextFrameTime.
   public void Bind(CharacterRarity rarity) :
     - récupère frames/fps/idle depuis library (null-safe : si rien → désactive l'Image et return) ;
     - pose la frame idle, _image.preserveAspect = true, _image.raycastTarget = false (forcé code) ;
     - enabled = playAnimation && _frameCount > 1 (sinon composant inerte, zéro coût).
   public void SetPlaying(bool playing) : enabled = playing && playAnimation && _frameCount > 1 ;
   remet la frame idle quand on stoppe.
   Update() : Time.unscaledTime ; avance l'index et n'assigne _image.sprite QUE quand la frame
   change. Aucune alloc, aucun GetComponent, aucune string.

3) Assets/_Project/Scripts/Editor/RarityBadgeWiringTool.cs
   [MenuItem("Chez Arthur/UI/BR1 — Poser les badges de rareté")] — builder IDEMPOTENT, Undo-safe,
   rapport écrit dans Audits/BR1_RarityBadges_Report.md (créé/complété), règle MT0 : refuse de
   tourner si la scène a des modifications non sauvées.
   - Crée ScriptableObjects/Config/RarityVisualLibrary.asset s'il n'existe pas, et y assigne les
     sprites slicés des 3 sheets (toutes les frames, ordre de slice) s'ils sont trouvés.
   - Localise le prefab de carte référencé par TeamPageUI.cardPrefab et le CharacterDetailPopup
     (gérer les 2 cas : prefab OU objet de la scène Hub).
   - Sur chacun, ajoute SI ABSENT (recherche par nom "RarityBadge") un enfant : Image +
     RarityBadgeView, ancre top-left. Défauts carte : ~36 % de la largeur de la carte, débord
     -6 px hors du cadre, playAnimation = false. Défauts popup : ancré au coin haut-gauche de
     artworkImage, ~140 px de large, playAnimation = true. Tout reste tunable à la main ensuite.
   - Assigne library sur les View, et le champ rarityBadge de CharacterCardUI et
     CharacterDetailPopup via SerializedObject/PrefabUtility (jamais de wiring runtime).
   - Re-run = rapport « 0 changement ».

FICHIERS À MODIFIER (diffs minimaux)
4) Assets/_Project/Scripts/Hub/Pages/CharacterCardUI.cs
   + [SerializeField] private RarityBadgeView rarityBadge;  (using ChezArthur.UI)
   Dans Setup(...), après le bloc rarityBorder existant :
   if (rarityBadge != null) rarityBadge.Bind(data.Rarity);
   NE TOUCHE PAS au reste (cadres rarityFrameSprites, clics, logs).

5) Assets/_Project/Scripts/Hub/Pages/CharacterDetailPopup.cs
   + [SerializeField] private RarityBadgeView rarityBadge;  (using ChezArthur.UI)
   - RefreshDisplay() : if (rarityBadge != null) rarityBadge.Bind(_currentData.Rarity);
   - typeText : supprime la rareté de la chaîne — rôle seul ("Attacker") dans TOUTES les branches
     (spé active et fallback base). Ne supprime aucun champ sérialisé.
   - ShowPopup() : if (rarityBadge != null) rarityBadge.SetPlaying(true);
     HidePopup() : if (rarityBadge != null) rarityBadge.SetPlaying(false);
     (le popup vit caché en CanvasGroup GameObject ACTIF : l'anim ne doit jamais tourner fermé.)

INTERDITS
- Scripts/UI/ArtworkTransition/** (socle AW figé), GachaAnimationController, GachaRevealStatusUI
  (chantier INV), TeamSlotUI / RateUpCharacterEntryUI / PullResultEntryUI (gate BR2), tout le combat.
- Pas d'Animator, pas d'AnimationClip, pas de DOTween, pas de Resources.Load, pas de singleton,
  pas de Find*. Pas de refactor opportuniste : les GetRarityColor dupliqués restent en place (BR2).

LIVRABLE
Liste des diffs fichier par fichier + rapport du builder. Je contrôle ligne à ligne avant commit
(feat: BR1 - badges de rareté collection + popup).
```

---

## 6. Checklist de test BR1 (device, à dérouler avant commit)

| # | Vérification |
|---|---|
| C1 | Grille collection : badge correct sur chaque carte (contrôler 1 SR, 1 SSR, 1 LR), coin haut-gauche, débord léger, cadres existants intacts |
| C2 | Taps : cliquer une carte À TRAVERS le badge ouvre bien le popup (raycast non volé) |
| C3 | Popup : badge animé fluide sur l'artwork, rareté correcte, ne recouvre ni nom ni niveau ; header sans mention texte de rareté (« Attacker » seul) |
| C4 | Popup fermé : l'anim est coupée (profiler ou log — aucun Update de RarityBadgeView actif hub au repos) |
| C5 | Tirage gacha → retour collection : le nouveau perso a son badge (refresh OK) |
| C6 | Ratios 16:9 / 19,5:9 / 20:9 : placements corrects, aucun débord cassé |
| C7 | Builder : re-run du menu → rapport « 0 changement » (idempotence) |
| C8 | Profiler grille : zéro alloc/frame imputable aux badges, pas de rebuild canvas continu |

---

## 7. Points ouverts

1. **BR-D5** — shine SSR/LR : dans les frames des sheets ou en overlay code (briques AW) → tranché à la livraison des spritesheets.
2. **BR-D4** — anim en grille : verdict device BR3, profiling à l'appui (coût canvas × nombre de cartes).
3. Lisibilité du badge LR à taille vignette (train détaillé) → verdict device BR3, variante simplifiée si besoin.
4. Conversion GIF → spritesheets : Arthur outille, ou envoie les GIF (je livre les sheets + valeurs de découpe).
5. Chantier suivant — **refonte UI écrans/menus/containers** : en attente des screenshots d'Arthur (audit ergonomique écran par écran : hiérarchie, zones de pouce, tailles tactiles, safe areas, responsive multi-ratios, grille d'espacement) — objectif : un kit de containers standardisés que les artistes UI/UX habilleront sans casser l'ergonomie.

---

## 8. Addendum Go BR1 (11/08) — drift terrain & prompt v1.1

**GO BR1 prononcé le 11/08 · BR-D4 FIGÉ.** Cursor a lu le plan et relevé un drift entre le sync du 11/08 (§1) et HEAD — le repo a bougé, comme anticipé (pattern AW-D5). Le drift est **accepté comme hypothèse de travail** ; il sera **prouvé pièce en main dans la proposition Cursor** (signatures réelles exigées ci-dessous) avant tout code.

**Drift rapporté par Cursor (à confirmer dans sa proposition)** :

| Point audit §1 | Réalité HEAD rapportée | Conséquence |
|---|---|---|
| 4× `GetRarityColor` dupliqués | Quasi résorbé via `CharacterRarityPalette` (reste un local dans `CharacterEntryUI`, combat) | Dette BR2 déjà largement payée par une autre lane ; règle d'unicité ci-dessous |
| Grille : cadres seulement | `CharacterCardUI` a déjà `badgeRarityImage` / `badgeSprites` / `ApplyRarityBadge` (statique + fallback texte) | BR1 grille = **migration**, pas superposition |
| Popup : « SR • Attacker » | Chip/texte rareté déjà retirés (« Gate 5.c.1 ») | BR-D1 en grande partie fait ; périmètre popup réduit |
| `Sprites/UI/Rarity/` | Dossier absent | Assets à poser (PNG 1-frame pour démarrer) |

**Traçabilité** : la réf. « Gate 5.c.1 » (retrait du chip rareté au popup) n'apparaît dans aucun doc projet de la bibliothèque — consigner quelle lane a touché le header du popup, pour l'historique.

**Périmètre BR2 ajusté** : badges TeamSlot / RateUp / PullResult + dernier `GetRarityColor` local (`CharacterEntryUI`, combat) + éventuelle migration des cadres vers la source unique — à re-cadrer contre HEAD à l'ouverture du gate.

### Prompt addendum BR1 v1.1 (à coller dans Cursor — prime sur le §5 en cas de conflit)

```
GO BR1 — ADDENDUM v1.1 (prime sur le §5 du plan en cas de conflit)

DÉCISIONS FIGÉES
- BR-D4 FIGÉ : popup animé · grille frame idle statique par défaut · flag playAnimation
  conservé partout pour le verdict device BR3.
- BR-D1 : si le texte/chip de rareté du header popup est déjà retiré à HEAD (5.c.1),
  ne touche pas au header — constate-le simplement dans ta proposition.

MÉTHODE (règle maison, rappel)
Étape 1 = PROPOSITION uniquement, aucun code : liste fichiers + diffs prévus, ET l'état HEAD
exact des ancres suivantes (copie les signatures réelles dans ta proposition) :
  - CharacterRarityPalette : type exact (ScriptableObject ? classe statique ?) + membres.
  - CharacterCardUI : badgeRarityImage / badgeSprites / ApplyRarityBadge (signatures + logique
    du fallback texte) + état des cadres (rarityFrameSprites toujours en place ?).
  - CharacterDetailPopup : contenu actuel du header (que reste-t-il post-5.c.1 ?)
    + ShowPopup/HidePopup.
  - CharacterEntryUI : le GetRarityColor local restant (périmètre BR2 — NE PAS y toucher en BR1).
Ensuite tu attends la 2ᵉ validation avant de coder.

RÈGLE D'UNICITÉ (remplace la création systématique de RarityVisualLibrary du §5)
Une SEULE source de visuels de rareté à la fin de BR1 :
  - Cas A — CharacterRarityPalette est un ScriptableObject : n'en crée pas un deuxième.
    Ajoute-y les blocs badge par rareté (Sprite[] badgeFrames, int idleFrameIndex,
    float framesPerSecond) + l'API null-safe du §5 (GetBadgeFrames/GetIdleFrame/GetFps).
    Aucun renommage de l'asset ni de la classe.
  - Cas B — CharacterRarityPalette est une classe statique (couleurs seulement) :
    crée RarityVisualLibrary (SO) comme au §5 mais SANS accentColor — les couleurs restent
    à la palette, zéro recouvrement entre les deux.
RarityBadgeView (§5, inchangé sinon) consomme cette source unique.

GRILLE — MIGRATION, PAS SUPERPOSITION
CharacterCardUI a déjà badgeRarityImage/badgeSprites/ApplyRarityBadge : on MIGRE.
  - badgeRarityImage devient le rendu du RarityBadgeView (playAnimation = false), OU
    ApplyRarityBadge se rebranche sur la source unique — tranche dans ta proposition ;
    objectif : UN seul chemin de rendu badge dans le code à la fin.
  - Supprime badgeSprites (tableau local) et le fallback texte : le null-safe de la source
    unique (warning + badge masqué) le remplace. Signale explicitement ce changement de
    comportement dans ta proposition.
BUILDER (§5, réduit) : créer Sprites/UI/Rarity/ + compléter la palette (cas A) ou créer
l'asset librairie (cas B) + assigner les frames + poser le badge POPUP. La carte a déjà son
Image : migration de références, pas de nouveau GameObject. Toujours idempotent, Undo-safe,
rapport Audits/, re-run = 0 changement.

POPUP (réduit)
Ajout RarityBadgeView (playAnimation = true) + Bind dans RefreshDisplay + SetPlaying(true)
dans ShowPopup / SetPlaying(false) dans HidePopup. Rien d'autre.

INTERDITS (en plus du §5)
CharacterEntryUI (combat — BR2) · CharacterRarityPalette au-delà du strict ajout du cas A ·
tout refactor opportuniste des cadres ou d'autre chose.

ASSETS
Crée Assets/_Project/Sprites/UI/Rarity/ ; PNG 1-frame fournis par Arthur pour démarrer
(spritesheets ensuite : zéro changement de code, seul l'asset source évolue). Import : §2.

CHECKLIST : C1–C8 inchangée (C3 devient une non-régression du header).
LIVRABLE : proposition (fichiers + diffs + ancres HEAD prouvées) PUIS attente du 2ᵉ Go.
```

*11/08 : addendum collé, proposition Cursor reçue avec ancres prouvées → contrôle §8.1.*

---

## 8.1 — 2ᵉ Go BR1 (11/08) — contrôle de la proposition Cursor

Proposition reçue avec ancres HEAD prouvées : **conforme à l'addendum v1.1**. Confirmations clés : **Cas B** (`CharacterRarityPalette` = classe statique, couleurs `UiTheme.Rarity*` → librairie SO **sans** accentColor) · grille = **migration** sur le GO `BadgeRarity` existant du prefab (View posé dessus, `playAnimation = false`) · popup = nouvel enfant sous l'artwork, header intouché (BR-D1 déjà satisfait par 5.c.1, champs legacy morts constatés) · piège CanvasGroup confirmé (L. ShowPopup/HidePopup) · collision de nom `RarityBadge` (showcase invocation) traitée par recherche scopée à la hiérarchie popup/artwork.

**2ᵉ GO ACCORDÉ, avec 2 amendements intégrés d'office** (stop-and-report si surprise) :

| Réf. | Amendement |
|---|---|
| **A1** | **Pas d'orphelins** : purger `badgeRarityText` (champ **et** GO enfant) et `RarityShortLabel` s'il n'est plus référencé — pas de champ « orphelin désactivé » (culture maison : purge complète propre, cf. verdict vignette INVR4). Vérifier zéro autre référence avant purge ; usage inattendu → stop et signale avant de coder. Filet visuel si asset manquant : la bordure teintée palette reste en place, la rareté reste lisible en grille. |
| **A2** | **Builder convergent** : un re-run après pose (ou remplacement) des PNG/sheets doit assigner les frames manquantes de la librairie — « 0 changement » seulement quand l'état cible est atteint. Le rapport liste ce qui a convergé. |

**Ordre d'exécution (Arthur)** : ① poser les 3 PNG 1-frame dans `Sprites/UI/Rarity/` → ② appliquer le code (A1/A2 inclus) → ③ lancer le menu builder → ④ contrôle du diff fichier par fichier ici → ⑤ checklist C1–C8 → ⑥ commit `feat: BR1 - badges de rareté collection + popup`.

**Consigné au passage** :
- **« Gate 5.c.1 » = lane Refonte Hub** (`DetailPopupPolishBuilder`, menus « Chez Arthur/Refonte Hub/… ») — lane active **absente de la bibliothèque docs projet**. Le futur chantier « refonte UI écrans » devra d'abord récupérer l'état de cette lane (numérotation 5.x, builders existants) pour ne pas la percuter. À reporter aussi dans le rapport BR1 (traçabilité).
- Dette popup (hors BR1, propriétaire : lane Refonte Hub) : `typeText`, `rarityChipText`, `rarityChipFrame` sérialisés morts → purge à programmer par leur lane.
- **BR-D5 (shine)** : 5.c.1 a explicitement coupé un shine du popup (« shine OFF »). Avant de réintroduire un shine SSR/LR (BR3/BR-D5), retrouver le verdict qui l'a éteint — le nouveau shine devra le respecter ou le renverser en connaissance de cause.

*Prochaine étape : Cursor code (A1/A2 inclus) → diff contrôlé ici ligne à ligne → checklist C1–C8 device → commit. BR2 s'ouvrira contre HEAD après clôture.*
