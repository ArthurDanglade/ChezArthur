# Plan directeur — Chantiers Méta (MT) : « le jeu autour du jeu »

**Take Five Games — Track Zero** · 4 août 2026 · **v1.2** (màj 04/08 soir — audit MT0 intégré : l'état réel à HEAD dépasse largement l'ancien sync ; **MT0-G1 lancé**)
Réponse au constat du 04/08 : les chantiers visuels attendent les sprites de l'artiste ; G7 (calibrage) et les passes de tests profonds sont différés à l'arrivée des assets (acté par Arthur). Ce document cartographie tout ce qui peut avancer **sans un seul asset**, et dans quel ordre.
Vérité terrain : **audit direct du repo cloné** (`Audit_Preparatoire_MT0_Fondations.md`, `3700a40`/`1151ae0`) — le clone direct est désormais le canal d'audit ; le sync du projet Claude, en retard, n'est plus une source de vérité.

---

## 1. État des lieux — le jeu dans son ensemble (corrigé à HEAD)

| Pilier | État constaté | Verdict |
|---|---|---|
| **Combat & contenu U1** | Socle refondu complet (G1–G6c clos, 21 commits contrôlés). G7 calibrage différé — dépend SFX/VFX + assets + critère 3 minutes. | ✅ Sain, en pause volontaire |
| **SFX/VFX (F)** | F1 ✅ · F2 ✅ · F3 en clôture (mini-lot P3) · F4–F5 à venir. **Pas bloqué artiste.** | 🟢 Actif |
| **Gacha & reveal (AW/INV)** | AW clos · INV : INV0 ✅, INV1 committé (`e1de2bb`), INV2–INV3 à suivre. **Pas bloqué artiste.** | 🟢 Actif |
| **Méta / hub** | **Socle v0 réel, bien plus avancé que documenté en v1.1** : missions v0 complètes (Daily/Weekly/Seasonal/Permanent + UI + badge + planning hebdo par rôle) · rotation saisonnière v0 (GameClock Paris, saisons 5 semaines, 5 slots × 20 étages, 5 univers nommés, content gate `UniverseContentConfig`) · **Boss Rush** complet (déblocage, roster first-kill, accroche hebdo) · `accountScore` (prestige) · 1er hint d'onboarding (`hintTeamDragSeen`) · bannières expirables (`dateFinSaison`) · settings assaini (3 sliders branchés) · accueil = Run + Boss Rush (boutons morts supprimés) · outillage debug riche (menu in-game avec **voyage dans le temps**, cheats strippés release, dev menu). **Manque la couche produit** : tuto/FTUE, portails + seuils + récompenses de saison (vision MT-D1), comptes, Magasin/News. | 🟠 Socle posé, couches produit à construire |
| **Transverse technique** | Save : versionnée **en écriture seulement** (`saveVersion = 3` jamais lu — pas de migration), écriture non atomique, **corruption = perte définitive par écrasement** (constats critiques n°1–2 de l'audit) → **périmètre exact de MT0-G1, lancé**. Resets/saisons sur **horloge device** (recul d'horloge = re-farm — à durcir MT4). Epoch + table de rotation **en dur** (pilotage remote = MT4). Pas de reprise de run. Pas de comptes/cloud/classement/analytics/crash. Pas de localisation (~210 fichiers runtime à littéraux FR + textes SO). Licence Epidemic Sound à régler avant release. Aucune CI. | 🔴 Fondations en cours (MT0) |

**Lecture de manager** : le cœur jouable ET le socle méta v0 sont là. Ce qui reste est exactement ce qui se construit sans artiste : durcissement des fondations, tuto, couche saison « portails/seuils », comptes et services, localisation. La pause artiste reste une opportunité.

---

## 2. Décisions actées (interview du 04/08)

| Réf. | Question | Décision Arthur |
|---|---|---|
| **MT-D1** | Saison v1 | **Portails + seuils + reset.** Pendant une saison : des portails ; un seuil à atteindre déterminant des récompenses par palier. Passage de saison : seuil remis à 0, nouvelles récompenses, nouveaux portails **avec nouvel univers**. Contrat détaillé au gate MT2-0. *(Note v1.2 : la rotation/horloge/saison-id existent déjà — MT2 = la couche portails/seuils/récompenses par-dessus.)* |
| **MT-D2** | Backend | Ambition « type Dokkan » : comptes, données joueurs, saisons pilotées. **Cahier des charges + comparatif à jour au chantier MT4** (Firebase / PlayFab / UGS). Ligne manager : solution managée, pas de serveur maison en solo ; données structurées pour bascule ultérieure. |
| **MT-D3** | Langues | **FR + EN dès la v1** → socle localisation en MT0-G2, avant les gros volumes de texte. |
| **MT-D4** | Premier chantier | **MT0 Fondations — GO.** Audit livré et acté le 04/08 ; **G1 lancé** (prompt Cursor livré). |
| **MT-D5** | Matière première saisons & tuto | Fichiers de précisions d'Arthur dans `claude/raw_meta/` (repo). **Base non contractuelle, scellée** jusqu'à MT1-0 / MT2-0 : lecture puis interview ciblée trous/contradictions/points non figés. |

---

## 3. Backlog des chantiers MT

Méthode par chantier : audit préparatoire à HEAD → interview → gates fermés → proposition → Go → prompt Cursor → contrôle du diff → checklist → commit.

### MT0 — Fondations *(EN COURS — `Plan_Execution_MT0_Fondations.md`)*

| Gate | Périmètre | État |
|---|---|---|
| **G1** | Save durcie (atomique, backup, quarantaine, garde anti-écrasement) + chaîne de migration réelle + compléments debug (pity, give perso, export/import) | ✅ **Go 04/08 — prompt + fixtures livrés** |
| **G2** | Socle localisation FR/EN + écrans pilotes (Paramètres, Accueil) ; migration de masse étalée hors gate | après G1 |
| **G3** | Sémantique d'états assainie (solder les deux « Temporaire ») | après G2 |

*(Le gate « menu debug » du plan v1.1 est soldé par l'existant — DebugMenu/DevMenuUI découverts à l'audit, voyage dans le temps inclus.)*

### MT1 — FTUE / Tuto

Moteur data-driven + script v1 sur le contenu actuel ; re-calibrage post-G7 = data. Intègre l'existant `hintTeamDragSeen` (premier hint posé). **Matière première MT-D5 lue à l'ouverture.** Dépend : MT0.

### MT2 — Saisons : portails & seuils *(requalifié « étendre »)*

Le socle existe (GameClock, rotation 5×5, saison-id, content gate, missions saisonnières). MT2 construit la couche vision MT-D1 : **MT2-0 = contrat de design** (portail ? métrique du seuil ? paliers ? cadence ? hors-saison ?) sur la matière première MT-D5, puis gates : seuils → récompenses, écran de bilan/reset de saison, portails, branchement News. Durcissements associés (avec MT4) : anti-recul d'horloge, epoch/table pilotables à distance. Dépend : MT0.

### MT3 — Missions / quotidien *(requalifié « compléter »)*

Système + UI v0 complets à HEAD. Reste : audit de complétude court (récompenses/équilibrage, missions login éventuelles, élargissement du catalogue). Après MT1/MT2.

### MT4 — Backend & comptes

MT4-0 = cahier des charges + comparatif à jour (comptes, cloud save, classements à reset saisonnier, remote config, coûts, RGPD — vérification web au moment de l'audit). Puis : auth anonyme + liaison Google, cloud save (sur save durcie G1), classement, **temps serveur + anti-recul** (constat n°4), **remote config du calendrier saisonnier** (constat n°5 — epoch/table en dur), Analytics + Crashlytics, consentement. Gacha résolu client en v1, données structurées pour bascule. Dépend : MT0-G1.

### MT5 — Settings, accessibilité & écrans du hub

Page Paramètres complète (langue — arrive avec G2 —, crédits, support, gestion save/compte ; vibration avec F4, reduce-motion avec F5). Magasin/News : boutons supprimés de l'accueil à HEAD — réintroduction = décision produit (monétisation, point ouvert). Dépend : MT0 ; coordination F4/F5.

### MT6 — Reprise de run

Confirmé absent à HEAD. Sérialisation d'une run en cours (étage, PV, valises, items, bonus, jauges, RNG) — attente de base mobile. Dépend : MT0-G1. À chiffrer par audit dédié.

### MT7 — Pipeline & préparation QA

Confirmé : aucune CI. Build Android reproductible, keystore + Play App Signing, CI compile, profil perf de référence, Play Console piste interne, **plan de tests profonds prêt** pour l'arrivée des assets. S'intercale à tout moment.

---

## 4. Séquence

```
En cours     : MT0-G1 (save durcie)        ← prompt livré, Arthur applique
En parallèle : INV2→INV3 · clôture F3-P3 · puis F4, F5
Ensuite      : MT0-G2 (localisation) → MT0-G3 (états)
Puis         : MT2-0 (contrat saisons, raw_meta ouvert) + MT1-0 (tuto, raw_meta ouvert)
               — ordre exact décidé à la clôture de MT0
Puis         : MT4 (backend) → MT2 exécution → MT5 · au fil de l'eau : MT7, MT6, MT3
Quand les assets arrivent : G7 + tests profonds (plan MT7) + contenu saisons (univers, portails)
```

Coexistence : les chantiers MT ne touchent jamais `Feedback/**`, les handlers de contenu, ni `UI/InvocationFlow/**` ; scènes en commits séparés par chantier.

---

## 5. Ce qu'on ne fait PAS maintenant

G7 et tout tuning (attend SFX/VFX + assets) · passes de tests profonds globales (différées — chaque gate garde sa checklist courte) · contenu U2+ visuel (assets) · design de monétisation (point ouvert).

---

## 6. Points ouverts

1. **Contrat de design saisons** (MT2-0) : portail / seuil / cadence — matière première MT-D5 en entrée.
2. **Monétisation v1** (IAP ? pub ? rien ?) — conditionne Magasin (MT5) et une partie de MT4.
3. **`Chez_Arthur_Roadmap_Globale.md.docx`** : toujours à convertir en .md vivant.
4. **Anti-triche horloge** (constat n°4) et **pilotage remote du calendrier** (constat n°5) — actés pour MT4, consignés.
5. **Licence Epidemic Sound** — à valider/remplacer avant release (rappel F1).
6. **Débounce des saves fréquentes** — consigné, non urgent (audit §6.4).

---

*Prochaine étape : Arthur applique `Prompt_Cursor_MT0_G1.md` dans Cursor → push → contrôle du diff ligne à ligne → checklist G1 (fixtures fournies) → commit → verdict au journal de `Plan_Execution_MT0_Fondations.md`.*
