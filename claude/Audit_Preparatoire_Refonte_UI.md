# Audit préparatoire — Chantier RUI : Refonte UI (écrans, menus, containers)

**Take Five Games — Track Zero** · 13 août 2026 · **v1.4 — RUI0 CLOS ✅ (§8.2) · RUI1 OUVERT : maquette Galerie en verdict (§9)**
Objet : refondre la structure de 12 écrans (hub + run) pour qu'ils soient **limpides, ergonomiques, premium et skinnables** — le joueur voit, il comprend direct ; l'artiste UI/UX superposera ses assets sur des layouts définitifs (sauf proposition innovante de sa part).
Matière : 12 screenshots device fournis par Arthur le 13/08 + sa liste de douleurs par écran. Canal code : ancres HEAD prouvées par Cursor à chaque gate (méthode BR).

---

## 0. Décisions actées (interview du 13/08)

| Réf. | Décision | Statut |
|---|---|---|
| RUI-D1 | **Run d'abord** après le socle : header combat/fiche ennemi → décisions de run (bonus, sacrifice, shop) → pause. Le hub suit, la saison ferme. La valeur joueur par minute de jeu commande l'ordre. | FIGÉ |
| RUI-D2 | **Contenus riches → pages plein écran** (saison, difficulté, résultat d'invocation — pattern Dokkan). Les popups ne portent plus que des micro-décisions (confirmations, sacrifice). | FIGÉ |
| RUI-D3 | **Placeholder neutre premium skinnable** : panneaux/boutons sobres, hiérarchie nette, 9-slice aux zones nommées. Le layout est définitif, l'habillage se remplace sans toucher la structure — c'est le contrat artistes. | FIGÉ |
| RUI-D4 | **Écran saison en dernier**, après le contrat MT2-0 (récompenses/objectifs/économie pas figés — zéro double travail). | FIGÉ |
| RUI-D5 | Méthode (consigne Arthur) : **écran trop brouillon → on écrase la mise en page et on reconstruit au builder** — jamais de rafistolage d'un layout cassé. | FIGÉ |

---

## 1. Vérité terrain visuelle — diagnostics par écran

### Hub

**H1 · Home — CTA Start a Run / Boss Rush**
Constat : deux barres pleine largeur plates posées sur une illustration qui, elle, est belle — le contraste containers/art est violent ; Boss Rush verrouillé ressemble à un bouton mort plutôt qu'à une promesse.
Direction : bloc CTA compact et travaillé (hiérarchie primaire/secondaire), état verrouillé lisible (condition + pictogramme, langage « verrouillé » du socle) qui donne envie de débloquer.

**H2 · Home — header (profil, monnaies, saison)**
Constat : rangée hétérogène (nom, zone masquée, « Preview éveil », compteur) sans logique de groupe ; le bouton saison n'a aucun statut visuel.
Direction : une barre profil/monnaies structurée (gauche identité, droite ressources), entrée saison avec son état (score, temps restant en badge). « Preview éveil » sort de l'UI de prod (→ §2, debug).

**H3 · Home — bande shop / lecteur lofi / news**
Constat : trois langages visuels différents côte à côte, ça fait brouillon — alors que le lecteur « Lofi du train » est une vraie idée d'identité.
Direction : bande utilitaire unifiée (mêmes containers, mêmes espacements) ; le lecteur, traité comme objet signature, décision diégétique ou utilitaire à RUI5 (point ouvert).

**H4 · Page Difficulté** *(actuellement popup, layout cassé)*
Constat : textes superposés illisibles au centre, aucune échelle — zéro sentiment de progression, on ne comprend rien (constat partagé).
Direction : **page** (RUI-D2) reconstruite de zéro (RUI-D5) : échelle verticale de difficultés en cartes — nom, modificateurs, récompenses, état (actif / battu / verrouillé + condition), position hebdo. Le joueur doit *voir* la montagne à gravir.

**H5 · Page Saison** *(actuellement popup « Score : 105 »)*
Constat : un battle-pass réduit à des lignes de texte et UN palier visible ; récompenses invisibles, objectifs absents, aucune explication du système.
Direction : **page** avec piste de paliers scrollable (12 paliers, états réclamé/atteint/verrouillé, récompense visible par palier), header score + fin de saison, section « comment marquer des points ». **Traité en dernier, sur les données du contrat MT2-0 (RUI-D4).**

**H6 · Page Résultat d'invocation** *(actuellement popup au récap gris vide — cassé)*
Constat : le moment de jouissance gacha atterrit sur un rectangle gris ; c'est une fenêtre alors que la référence (Dokkan) est une page.
Direction : **page plein écran** : grille de cartes résultat (badges de rareté BR, marqueur NOUVEAU, niveaux), tap → détail. Frontière actée : RUI livre la **structure de page**, la **mise en scène** (beats, cadence) reste au chantier INV. `GachaSummaryBuilder` (croisé en BR2) est l'ancre code.

### Run

**R1 · Header combat** *(cas le plus chargé fourni : synergie + HP boss + frise de tour)*
Constat : bandeau noir plein posé sur l'arène, informations flottantes sans zones (DBG, étage, or, frise, nom, synergie, barre HP boss perdue en haut du terrain) — surchargé et pourtant illisible.
Direction : header à **trois zones** (contexte étage/ressources · frise de tour compacte avec perso actif mis en avant + nom · boss : nom + barre HP dédiée) ; synergie en **chip** discret extensible au tap. Rien ne flotte, tout a une zone.

**R2 · Caisses HP alliés (bas de combat)**
Constat : quatre cadres corrects mais bruts, alignements et styles disparates avec le reste.
Direction : composant unique `ListRow` combat (nom, HP, états), rythme d'espacement du socle — sobre, c'est du combat.

**R3 · Fiche ennemi**
Constat : tout est là (et le lore est excellent — une force du jeu) mais tout se touche : sections faiblement séparées, stats en ligne de texte, l'intention noyée.
Direction : header identité (portrait + nom + type NORMAL/…), **intention en ligne mise en avant** (c'est l'info tactique n°1), stats en 4 cellules `StatCell`, passifs en cartes courtes, **lore en encart stylé** (le texte mérite sa scène).

**R4 · Sélection de bonus**
Constat : le moment roguelike par excellence sans aucune cérémonie — titre géant tronqué sous le header, cartes plates, la rareté n'existe pas visuellement, « x4 » flottant.
Direction : page de choix propre (titre dans la zone titre du `PageScaffold`), cartes de choix avec **langage de rareté bonus** (cadre/accent par rareté — palette bonus distincte, §2), type (VALISE/ITEM) et rareté clairement séparés. Le juice (apparition, pops) restera au chantier SFX/VFX — RUI livre un layout animable.

**R5 · Sacrifice**
Constat : la structure perds/gagne est saine ; ce qui manque : savoir **ce qu'on possède** (slots du haut = icônes muettes) et ce que fait chaque valise ; deltas peu parlants.
Direction : slots enrichis (nom, niveau, résumé au tap), comparatif complet et hiérarchisé, confirmation en dernier recours visuel. Vérité terrain préalable : la lane gameplay avait SacrificeUI « en refonte » — état à clarifier à RUI0.

**R6 · Shop gare**
Constat : titre « GARE » fantôme sous le header, cartes produit inégales, « Acheter » vert identique partout, monnaies ambiguës (carré blanc ?), le footer « Continuer » chevauche les caisses HP.
Direction : page shop (scrim sur l'arène), cartes produit standard (rareté bonus visible, prix + monnaie lisibles), section soin séparée, footer dans une zone réservée — plus aucun chevauchement.

**R7 · Pause — onglet état de la game**
Constat : onglets illisibles, sections personnages/valises/synergies/items collées et au même poids, fond bleu uni hors DA.
Direction : vraie `TabBar`, sections en cartes séparées (`SectionHeader` + `ListRow`), **fond = arène capturée, assombrie/floutée** (idée d'Arthur, actée — one-shot au moment de la pause, pas de flou temps réel).

**R8 · Pause — onglet paramètres**
Constat : sliders nus au centre du vide, RESTART/MAIN MENU en boutons blancs bruts au milieu, aucun groupe.
Direction : groupes en cartes (Audio · Langue · Run), actions destructives stylées danger et séparées, langue en segmented control. Même fond que R7.

---

## 2. Problèmes transverses (la racine, avant les écrans)

1. **Aucun langage commun de containers** — chaque écran improvise (noir plein, bleu uni, blanc brut, vert cru). C'est LA cause du « brouillon/moche » : le socle (§3) règle 80 % du problème avant même de toucher un écran.
2. **Hiérarchie typographique inexistante** — tout au même poids ; titres qui passent SOUS le header (« Choisis un bonus », « GARE ») faute de zones réservées.
3. **Des popups qui portent des contenus de page** (saison, difficulté, résultat) + deux layouts cassés (difficulté superposée, récap gris) → RUI-D2 + RUI-D5.
4. **Deux échelles de rareté sans langages distincts** : rareté perso (SR/SSR/LR — servie par le chantier BR, invariants I1/I2) et rareté bonus/valises (labels NOUVELLE VALISE / ITEM / RARE / ÉPIQUE mélangés sur les mêmes cartes). Le socle définit une **palette rareté bonus** séparée — jamais les couleurs perso pour les valises, confusion interdite.
5. **Éléments debug dans l'UI de prod** (« Preview éveil » partout, « DBG », étage debug) → isolation dans un canvas debug gated dès RUI0 (« Preview éveil » = outil de calibrage AW utile, on le garde, on le sort de la prod).
6. **Chevauchements** (« Continuer » sur les caisses HP, titres sous header) → `PageScaffold` avec zones réservées (header safe, titre, scroll, footer actions).
7. La **DA des illustrations est bonne** (home, arène, portraits) — le chantier ne touche pas à l'art, il construit les containers qui le méritent.

---

## 3. Socle RUI1 — design system light

- **Tokens** (extension d'`UiTheme` existant, pas un 2ᵉ système) : grille d'espacement (base 8), rayons, tailles tactiles minimales, styles TMP nommés (Display/H1/H2/Body/Caption/Chip), couleurs sémantiques (surface 1/2/3, stroke, accents, success/danger/locked), palette rareté perso (existante, I1) + **palette rareté bonus distincte**.
- **Composants** : `UiPanel` (9-slice, zones nommées = contrat artiste) · `SectionHeader` · `AppButton` (primary/secondary/danger/**locked avec condition**) · `TabBar` · `ListRow` · `StatCell` · `RewardChip` · `PageScaffold` (header/titre/scroll/footer) · `PopupScaffold` (micro-décisions) · fond pause (capture arène downscale + blur one-shot).
- **Builders éditeur idempotents par écran** (culture maison — c'est eux qui écrasent/reconstruisent, RUI-D5) + **écran Galerie** (démo de tous les composants) pour valider le langage une fois, pas douze.
- **Livrable de sortie du socle : le contrat artistes** — doc de nommage des slots skinnables (9-slice, fonds, ornements, états) pour la superposition d'assets sans retouche de structure.

## 4. Gates

| Gate | Périmètre |
|---|---|
| **RUI0** | Vérité terrain code & dettes : **absorption de la lane Refonte Hub** (doc rétroactif des builders 5.x — TeamPageRebuilder, CharacterCardPolishBuilder, DetailPopupPolishBuilder…, décisions implicites), purge des champs morts popup (dette BR §9), inventaire canvases/scènes/prefabs des 12 écrans, état réel SacrificeUI (lane gameplay), **isolation debug** (canvas gated). |
| **RUI1** | Socle : tokens + composants + builders + Galerie → **contrat artistes v1**. |
| **RUI2** | Run core : header combat 3 zones, HP boss, chip synergie, caisses HP, fiche ennemi. |
| **RUI3** | Décisions de run : sélection bonus, sacrifice, shop gare. |
| **RUI4** | Pause : onglets, état, paramètres, fond arène flouté. |
| **RUI5** | Hub : home (CTA, header, bande + verdict lecteur lofi), page Difficulté. |
| **RUI6** | Page Résultat d'invocation (structure Dokkan-like, badges BR) — coordination INV (mise en scène chez eux). |
| **RUI7** | Page Saison — après contrat MT2-0 (RUI-D4). |

Boucle par gate (méthode BR reconduite) : audit ciblé HEAD → **maquette avant code** (wireframe, preview HTML interactive à la demande — pattern AW0/INV0) → Go → prompt Cursor → contrôle diff → checklist device (dont multi-ratios 16:9 → 20:9) → commit par lane. Un gate à la fois.

## 5. Dépendances inter-lanes

| Lane | Rapport au chantier |
|---|---|
| Refonte Hub (5.x) | **Absorbée à RUI0** — elle cesse d'exister sans doc ; ses dettes (champs morts popup) sont soldées là. |
| INV (polish invocation) | Frontière récap : RUI = structure de page, INV = mise en scène/beats. `GachaSummaryBuilder` partagé — coordination au gate RUI6. |
| Gameplay (SacrificeUI/jauge) | État réel à RUI0, avant de toucher RUI3. |
| MT2 (saisons) | Fournit les données de la page saison (RUI-D4). |
| SFX/VFX (F4) | Le juice des nouveaux écrans reste chez F4 — RUI livre des layouts sobres et animables. |
| MT5 (plan directeur) | RUI4/RUI5 couvrent sa partie visuelle — à noter au plan directeur à la clôture RUI5. |
| BR (clos) | Invariants I1/I2 consommés partout où la rareté perso apparaît (résultat invocation notamment). |

## 6. Points ouverts

1. Lecteur lofi : objet diégétique du décor ou barre utilitaire ? (verdict RUI5 — c'est une signature, ne pas l'aplatir par accident.)
2. Résolutions cibles de test à confirmer (device principal d'Arthur + extrêmes 16:9 / 20:9).
3. Previews HTML interactives par écran avant les prompts : à la demande, gate par gate (recommandé pour R1 header combat et H4 difficulté au minimum — les deux plus structurants).
4. Monnaies : inventaire des devises affichées (or, Tals, carré blanc du shop ?) et de leurs icônes — clarification à RUI0, l'ambiguïté actuelle du shop en dépend.

## 7. Directive RUI0 (à coller dans Cursor au Go d'Arthur)

```
OUVERTURE RUI0 — vérité terrain & dettes (chantier Refonte UI,
audit : claude/Audit_Preparatoire_Refonte_UI.md)

ÉTAPE 1 = RAPPORT UNIQUEMENT, aucun code. Livre, avec ancres prouvées
(fichiers / menus / signatures réelles copiées) :

1. LANE REFONTE HUB — doc rétroactif : liste complète des builders
   "Chez Arthur/Refonte Hub/*" (TeamPageRebuilder, CharacterCardPolishBuilder,
   DetailPopupPolishBuilder, autres ?), numérotation 5.x connue (5.c.1…),
   ce que chacun pose/modifie, décisions implicites embarquées (styles, tailles,
   couleurs posées en dur). Objectif : écrire ce qui existe avant de
   reconstruire par-dessus.
2. INVENTAIRE DES 12 ÉCRANS de l'audit (H1→H6, R1→R8) : pour chacun —
   scène ou prefab, canvas porteur (+ sorting), scripts UI propriétaires,
   builder existant oui/non, popup ou page aujourd'hui.
3. UITHEME : contenu exact actuel (tokens existants) + tout autre endroit où
   couleurs/styles UI sont définis en dur (candidats à centralisation RUI1).
4. SACRIFICEUI / JAUGE DE PRESSION : état réel de la refonte lane gameplay
   (finie ? en cours ? abandonnée ?) — RUI3 en dépend.
5. MONNAIES : inventaire des devises affichées (or, Tals, carré blanc du shop,
   autres) — data, icônes, surfaces d'affichage.
6. DEBUG : inventaire des éléments debug dans l'UI de prod (« Preview éveil »,
   « DBG », étage debug, autres) — qui les pose, où ils vivent.
7. CHAMPS MORTS POPUP (dette BR §9) : typeText / rarityChipText /
   rarityChipFrame — refs restantes, plan de purge (champ + GO + builders
   alignés, pattern A1).

ÉTAPE 2 (APRÈS le Go sur le rapport) = deux actions de dette, commits séparés :
  a) purge des champs morts popup (pattern A1, builders alignés) ;
  b) isolation debug : racine/canvas Debug gated (toggle dev), l'UI de prod
     n'affiche plus AUCUN élément debug — « Preview éveil » reste accessible
     en mode dev (outil de calibrage AW, on le garde).

INTERDITS : aucun changement visuel de prod à RUI0, aucun refactor d'écran
(c'est RUI1+), zones gameplay/combat intouchées, .cursorrules en vigueur
(proposition → Go → code).
LIVRABLE : rapport structuré (7 points, ancres prouvées) PUIS attente du Go étape 2.
```

*Prochaine étape : Go RUI0 d'Arthur → rapport Cursor (7 points, ancres prouvées) → Go étape 2 (purge + isolation debug) → clôture RUI0 → socle RUI1, la fondation de tout le reste.*

---

## 8. RUI0 étape 1 — rapport contrôlé (13/08) → GO ÉTAPE 2

Rapport `Audits/RUI0_Terrain_Report.md` : **conforme à la directive** — 7 points, ancres prouvées, zéro code. Contrôle : verdict favorable, avec **7 recalibrages** du chantier consignés ici (le rapport corrige plusieurs hypothèses de l'audit v1) :

| Réf. | Recalibrage |
|---|---|
| RC1 | **Le socle n'est pas à inventer, il est à consolider.** La lane Refonte Hub a déjà poussé des tokens dans `UiTheme` (BgDeep/Panel/Elevated, Space1–6, Radius*, HeaderHeight 112, NavHeight 152, TouchTargetMin 96…) et il existe `UiKitFactory` + `UIKitSandboxBuilder` + generators. **RUI1 = extension + règles d'usage + Galerie héritière du Sandbox** — pas un 2ᵉ système (le §3 reste valable, recentré). |
| RC2 | **Les 3 palettes de rareté existent déjà** dans `UiTheme` (perso / valise / bonus). Le problème du §2.4 est l'**usage** confus sur les cartes shop/bonus, pas l'absence de tokens. RUI1 écrit les règles d'usage, RUI3 les applique. |
| RC3 | **Deuxième lane non documentée découverte** : famille de menus `Take Five Games/UI/*` (stylers Sacrifice/Bonus, gates 6b.x). Le tableau du rapport l'absorbe aussi. À RUI1 : décision de normalisation — tout nouveau builder RUI sous **une seule** famille de menus. |
| RC4 | **H6** : la cible vivante est `SummaryScene` (flow gacha) ; `PullResultPopup` legacy = OBSOLÈTE → RUI6 part de SummaryScene et purge le legacy. |
| RC5 | **Jauge de pression = gameplay LIVE**, pas une dette UI. Verdict d'intégration (header 3 zones ou liseré périphérique) → RUI2. |
| RC6 | **Une seule devise : Tals** (méta + run). « Gold » = couleur d'accent, « carré blanc » = icône manquante — l'ambiguïté du shop est un bug d'icônes/labels, réglé à RUI3/RUI5. |
| RC7 | Anomalies notées pour leurs gates : `BonusPanelUI` absent de `Game.unity` (→ RUI4) ; « Étage » = prod légitime, reste en zone R1 (acté). |

**GO ÉTAPE 2 accordé, avec 3 précisions (P1–P3)** :

| Réf. | Précision |
|---|---|
| **P1** | Purge popup : trancher le sort du builder legacy `CharacterDetailPopupBuilder` (qui recrée encore typeText/chips). S'il est entièrement supplanté par `DetailPopupRebuilder` (5.c) → **mise à la retraite** (suppression ou garde obsolète), pas un alignement de plus. Sinon, aligner. À énoncer dans la mini-proposition avant code. |
| **P2** | Isolation debug : couvrir **les deux chemins** de Preview éveil (GO de scène `AwakeningCeremonyDebugPreview` ET création runtime `BtnPreviewEveil`). Attention : `GachaAnimationController.debugPreviewRoot` cherche ces objets pour les masquer en cérémonie — ce lookup doit rester **null-safe en prod** (objet absent ≠ NRE). DBG reste IMGUI gated compile (déjà correct) — le « canvas Debug » ne concerne que les éléments canvas. |
| **P3** | Deux commits séparés, staging I5 : a) `chore(popup):` purge dette typeText/rarityChip* (champs + GOs + builders alignés) · b) `chore(debug):` isolation UI dev. |

**Checklist étape 2** : popup s'ouvre sans régression (header, stats, badge BR animé) · re-run builders popup = 0 changement · build prod : aucun élément debug visible, cérémonie gacha sans erreur (lookup null-safe) · mode dev : Preview éveil accessible · re-run isolation = 0 changement.

*Prochaine étape : mini-proposition Cursor (P1 tranché) → code étape 2 → diff + checklist → 2 commits → clôture RUI0 au doc → RUI1 (socle consolidé RC1/RC2, Galerie, contrat artistes — maquette HTML de la Galerie proposée avant tout prompt).*

### 8.1 — Mini-proposition étape 2 contrôlée (13/08) → 2ᵉ GO

**P1 ACTÉ : retraite par suppression** de `CharacterDetailPopupBuilder.cs` (+ meta). Ancres prouvées : legacy Gate 3, auto-doc « NE JAMAIS ré-exécuter », recrée typeText/chips, dims obsolètes (440/240), même PopupGuid désormais servi par 5.c/5.c.1. Un builder interdit d'exécution qui recrée la dette n'a rien à faire dans l'éditeur — l'historique git est l'archive.

**2ᵉ Go accordé avec 2 amendements** : **E1** — la purge des GOs passe par un amendement de `DetailPopupPolishBuilder` (5.c.1, propriétaire du popup), pas un nouveau builder one-shot (on vient d'inventorier ~17 builders, pas de 18ᵉ pour une purge convergente). **E2** — le gating dev de Preview éveil réutilise la **même définition** que `DebugMenu` (`UNITY_EDITOR || DEVELOPMENT_BUILD` + auto-destroy) : une seule notion de « mode dev » dans le projet, pas deux.

Checklist complétée : compile propre après suppression du legacy (zéro référence restante) · re-run 5.c.1 = « 0 changement » post-purge · reste inchangé (§8).

### 8.2 — CLÔTURE RUI0 (13/08) ✅

Étape 2 livrée et contrôlée : `537b38a` `chore(popup)` (champs morts + GOs purgés, 5.c.1 amendé PurgeGo A1, legacy `CharacterDetailPopupBuilder` supprimé, note BR wiring) · `ec12186` `chore(debug)` (gate unifié `UNITY_EDITOR || DEVELOPMENT_BUILD` + Destroy release, les 2 chemins Preview couverts, lookup gacha null-safe confirmé). Checklist verte device (popup sans régression, re-run 0 changement, prod sans debug, dev avec Preview éveil).

**Reste à commiter : `docs(RUI0): audit v1.4 + terrain report`** (ce doc + `Audits/RUI0_Terrain_Report.md`) — demandé.

**Acquis RUI0** : lane Refonte Hub absorbée (17 builders documentés) · dettes popup et debug soldées · vérité terrain complète des 12 écrans · recalibrages RC1–RC7 actés. **GATE CLOS.**

---

## 9. RUI1 — OUVERT : socle & Galerie (maquette en verdict)

Périmètre (recentré RC1/RC2) : **consolider** `UiTheme` (styles TMP nommés, état locked, compléments grille) + **règles d'usage** (dont les 3 échelles de rareté, jamais croisées) + composants (§3) + **Galerie** héritière de `UIKitSandboxBuilder` + **contrat artistes v1**. Décision de normalisation à acter au passage : famille de menus unique pour les builders RUI (RC3).

**Maquette HTML de la Galerie livrée le 13/08** (`RUI1_Galerie_Maquette.html`) — proposition du langage visuel complet sur tokens placeholder à calibrer sur `UiTheme` réel. **Verdict attendu d'Arthur** : densités/espacements, accent primaire (ambre), langage des 3 raretés, boutons (dont locked-avec-condition), anatomie PageScaffold. Après verdict → directive RUI1 pour Cursor, écrite contre le langage validé.
