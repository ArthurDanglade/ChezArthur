# Plan directeur — Chantiers Méta (MT) : « le jeu autour du jeu »

**Take Five Games — Track Zero** · 4 août 2026 · **v1.1**  
Réponse au constat du 04/08 : les chantiers visuels attendent les sprites de l’artiste ; G7 (calibrage) et les passes de tests profonds sont différés à l’arrivée des assets (acté par Arthur). Ce document cartographie tout ce qui peut avancer **sans un seul asset**, et dans quel ordre.

Vérité terrain : sync GitHub + docs projet au 04/08. **Réserve systématique** : le repo local d’Arthur peut être en avance sur un sync distant — chaque chantier MT s’ouvre donc par un audit à HEAD (pont device), comme d’habitude.  
**Sync local (04/08 soir)** : `HEAD` = `origin/main` = `0c304c9` — audit MT0 autorisé dès signal Arthur.

**Changelog v1.1** : décision **MT-D5** — fichiers saisons/tuto versés comme matière première non contractuelle (`claude/raw_meta/`).

---

## 1. État des lieux — le jeu dans son ensemble

| Pilier | État constaté | Verdict |
|---|---|---|
| **Combat & contenu U1** | Socle refondu complet (G1–G6c clos, 21 commits contrôlés, zéro diff rejeté). G7 calibrage différé — dépend SFX/VFX + assets + critère 3 minutes. | ✅ Sain, en pause volontaire |
| **SFX/VFX (F)** | F1 ✅ · F2 ✅ · F3 en clôture (mini-lot P3 livré) · F4–F5 à venir. Procédural + banques sons : **pas bloqué artiste**. | 🟢 Actif |
| **Gacha & reveal (AW/INV)** | AW clos 04/08 · INV ouvert : INV0 validé, prompt INV1 livré (à coller), INV2–INV3 à suivre. Procédural : **pas bloqué artiste**. | 🟢 Actif |
| **Méta / hub** | Embryonnaire. Accueil : 3 boutons morts (`Paramètres`, `Magasin`, `News` = `Debug.Log` « à implémenter »). Settings = 2 sliders PlayerPrefs + TODO obsolète. Pas de tuto/FTUE. Pas de saisons. Missions : référencées (call site SfxManager), périmètre exact à vérifier à HEAD. Flow marqué « temporaire » dans le code : `GameManager` démarre en `Playing`, `RunManager.Start()` auto-lance la run. | 🔴 Le vrai chantier |
| **Transverse technique** | Save JSON locale : non versionnée (aucun `schemaVersion`), écriture non atomique (`File.WriteAllText` direct — corruption possible sur kill), mono-slot, migration ad hoc (`selectedTeamIds` legacy). Reprise de run en cours : rien de trouvé (kill app = run perdue — à confirmer à HEAD). Pas de comptes / cloud save / classement / analytics / crash reporting. Textes FR en dur (code + textes joueur dans les SO). Licence Epidemic Sound à régler avant release (consigné F1). Pipeline build/CI non formalisé. | 🔴 Fondations à poser |

**Lecture de manager** : le cœur jouable est en avance sur son enveloppe. Tout ce qui transforme « un prototype excellent » en « un jeu qu’on installe, qu’on comprend et qu’on garde » reste à construire — et rien de cette liste ne demande un sprite. La pause artiste est une opportunité, pas un blocage.

---

## 2. Décisions actées (interview du 04/08)

| Réf. | Question | Décision Arthur |
|---|---|---|
| **MT-D1** | Saison v1 | **Portails + seuils + reset.** Pendant une saison : des portails ; un seuil à atteindre déterminant des récompenses selon le palier atteint. Passage de saison : seuil remis à 0, nouvelles récompenses, nouveaux portails **avec nouvel univers**. Contrat de design détaillé à écrire au gate MT2-0 (définition exacte d’un portail, métrique du seuil, cadence). |
| **MT-D2** | Backend | Ambition « système optimisé gacha type Dokkan » : comptes joueurs, données joueurs, saisons pilotées. Arthur ne s’est pas encore renseigné → **cahier des charges + comparatif à jour au chantier MT4** (Firebase / PlayFab / Unity Gaming Services). Ligne posée par le manager : **solution managée, pas de serveur maison à maintenir** pour une v1 solo — on garde la possibilité d’une bascule serveur ultérieure par la structure des données. |
| **MT-D3** | Langues | **FR + EN dès la v1** → socle de localisation posé en fondations, AVANT l’écriture des gros volumes de texte (tuto, missions, saisons). |
| **MT-D4** | Premier chantier | **MT0 Fondations — GO immédiat** (recommandation manager confirmée par Arthur). |
| **MT-D5** | Docs saisons / tuto | **Matière première non contractuelle**, versée dans `claude/raw_meta/` (après / parallèle soft de l’audit MT0). Sources : `FTUE_Tutoriel` + `Systeme_Saisons_Design_v1`. **Attention Arthur** : certains éléments sont incorrects ou non figés — ne rien prendre pour argent comptant. À l’ouverture MT1 / MT2-0 : confrontation au code ; interview **uniquement** sur trous, contradictions et points non figés — pas de ré-interview à vide. |

---

## 3. Backlog des chantiers MT

Chaque chantier suit la méthode standard : audit préparatoire à HEAD → interview → gates fermés → proposition → Go → prompt Cursor → contrôle du diff ligne à ligne → checklist → commit.

### MT0 — Fondations *(GO — ouvre maintenant)*

Périmètre pressenti (~4 gates, à figer à l’audit) :

1. **Save v2** : champ `schemaVersion` + chaîne de migrations, écriture atomique (fichier temporaire + remplacement), slot de secours anti-corruption, API d’accès unique. Pré-requis de TOUT le reste — saisons, tuto, missions et cloud écrivent tous dans la save.
2. **Socle localisation FR/EN** : table de strings (SO custom vs Unity Localization — à trancher à l’audit), inventaire des textes en dur (UI + textes joueur dans les SO passifs/persos), convention de clés. On rédige en FR, l’EN arrive par la data.
3. **Menu debug/cheats** (builds dev uniquement) : donner des Tals, débloquer des persos, sauter à l’étage N, forcer le pity, reset save, **voyage dans le temps** (indispensable pour tester les saisons sans attendre 6 semaines).
4. **Flow propre** : Boot → Hub → Game — suppression des deux « temporaire » consignés dans le code (démarrage `Playing`, auto-`StartRun`), point d’entrée unique d’initialisation des managers.

Valeur : dérisque tous les chantiers suivants ; court, testable sans device.

### MT1 — FTUE / Tuto

Moteur data-driven (étapes en SO : condition de déclenchement, cible surlignée, masque d’input, texte localisé, skip, reprise après interruption, progrès en save) + **script v1 sur le contenu actuel** : premier lancement → starters → première run guidée (drag, rebonds, super, choix de bonus, Gare) → premier gacha. Le re-calibrage après G7 et l’arrivée des assets = **de la data, pas du code**. Impact rétention D1 maximal.

**Matière première (MT-D5)** : `claude/raw_meta/FTUE_Tutoriel.md` — non contractuelle ; confrontation code + interview ciblée à l’ouverture.

Dépend : MT0 (save du progrès, localisation).

### MT2 — Saisons (portails & seuils)

**MT2-0 = contrat de design** (même statut que `Refonte_Ennemis_Design_U1` : le document fait foi) : qu’est-ce qu’un portail exactement (entrée de run par univers ?), métrique du seuil (meilleur étage de la saison ? cumul ?), paliers de récompenses, durée/cadence des saisons, comportement hors-saison.

Puis par gates : source de temps fiable (anti-recul d’horloge, heure réseau quand disponible), `SeasonCalendar` data-driven, état de saison en save (v2), seuils → récompenses, écran de bilan de fin de saison, branchement du bouton News.

**Note structurante** : le système se construit et se teste intégralement sur U1 — le « nouvel univers par saison » est du contenu qui arrivera avec les assets. On pose le rail avant le train.

**Matière première (MT-D5)** : `claude/raw_meta/Systeme_Saisons_Design_v1.md` — non contractuelle. Contient déjà des propositions (métrique `bestStageThisSeason`, durée 6 semaines, paliers, seuils + classement additif, etc.) **à valider / corriger** — ne remplace pas MT2-0. Confronter aussi à MT-D1 (portails + nouvel univers) : éventuelles contradictions à trancher à l’interview.

Dépend : MT0 ; synergie forte avec MT4 (calendrier pilotable à distance sans mise à jour).

### MT3 — Missions / quotidien *(optionnel v1 — à trancher)*

Daily/weekly, récompenses de connexion, succès. S’appuie sur MT0 + localisation. À cadrer après MT1/MT2 — l’existant « missions » aperçu côté SFX sera inventorié à l’audit.

### MT4 — Backend & comptes

**MT4-0 = cahier des charges + comparatif à jour** (les offres bougent — vérification web au moment de l’audit) : Firebase / PlayFab / Unity Gaming Services sur les critères comptes, cloud save, classements à reset saisonnier, remote config, coûts au réel, RGPD.

Puis par gates : auth anonyme + liaison Google, cloud save (sync de la save v2, résolution de conflits), classement meilleur étage, remote config du calendrier de saisons, Analytics + Crashlytics, consentement RGPD.

Position manager : **le gacha reste résolu côté client en v1** (offline-friendly, zéro latence) mais les données (pity, historique de pulls) sont structurées pour permettre une bascule serveur si le jeu décolle — c’est là que se joue le « type Dokkan » à terme.

Dépend : MT0 (on ne synchronise pas une save non versionnée).

### MT5 — Settings, accessibilité & écrans du hub

Page Paramètres complète (volumes via le mixer F1, langue, crédits, support, gestion save/compte ; vibration arrive avec F4, « réduire les mouvements » avec F5 — on pose les emplacements), Magasin (attend la décision monétisation — point ouvert §6), News (branché sur MT2/MT4).

Dépend : MT0 ; coordination avec F4/F5 via le protocole de coexistence.

### MT6 — Reprise de run

Si confirmé absent à l’audit : sérialisation d’une run en cours (étage, PV des alliés, valises, items, bonus, jauges, état RNG) pour survivre au kill de l’app. Sur mobile c’est une attente de base — perdre une run de 25 minutes sur un appel = churn sec.

Dépend : MT0. Périmètre potentiellement gros : à auditer avant de chiffrer.

### MT7 — Pipeline & préparation QA

Build Android reproductible (script de build, keystore + Play App Signing, schéma de versioning), CI de compilation (GitHub Actions), profil de perf de référence sur l’APK actuel, Play Console piste interne, et **plan de tests profonds rédigé à l’avance** — pour que le jour où les assets arrivent, la phase de tests différée démarre sans friction.

Dépend : rien. S’intercale à tout moment.

---

## 4. Séquence recommandée

```
Maintenant   : MT0 (fondations)                              ← GO acté MT-D4
En parallèle : INV1→INV3 (en cours) · clôture F3-P3 · puis F4, F5
               + raw_meta saisons/tuto versés (MT-D5) — lecture à MT1/MT2-0 seulement
Ensuite      : MT2-0 (contrat saisons) + MT1 (tuto)
               — l’ordre exact entre les deux se décide à la clôture de MT0
Puis         : MT4 (backend) → MT2 exécution complète → MT5
Au fil de l’eau : MT7 (pipeline), MT6 (selon audit), MT3 (si retenu v1)
Quand les assets arrivent : G7 + passes de tests profonds (plan MT7 prêt)
                            + contenu de saisons (univers, portails)
```

**Règle de coexistence étendue aux MT** : les chantiers MT ne touchent jamais `Feedback/**`, les handlers de contenu, ni les fichiers INV ; éditions de scène en commits séparés par chantier (règle existante reconduite).

---

## 5. Ce qu’on ne fait PAS maintenant (protection du planning)

G7 et tout tuning de courbe (attend SFX/VFX + assets — acté) · passes de tests profonds globales (différées par Arthur — **mais chaque gate garde sa checklist courte : on ne merge jamais du code non testé**, seules les passes globales de calibrage/régression attendent) · contenu U2+ et refontes visuelles (assets) · design de monétisation (point ouvert, pas une urgence technique).

---

## 6. Points ouverts

1. **Contrat de design saisons** (MT2-0) : définitions portail / seuil / cadence — interview dédiée à l’ouverture (appuyée sur `raw_meta`, pas à vide). Confronter MT-D1 ↔ doc saisons (seuils + classement additif, durée 6 sem., etc.).
2. **Monétisation v1** (IAP ? pub ? rien ?) — conditionne le Magasin (MT5) et une partie de MT4.
3. **`Chez_Arthur_Roadmap_Globale.md.docx`** : illisible dans le sync (binaire non indexé) — à convertir en .md dans le projet pour que la roadmap redevienne une référence vivante et vérifiable.
4. **Missions / jauge** : périmètre réel à inventorier à HEAD (référencé côté SFX, non retrouvé dans le sync).
5. **Licence Epidemic Sound** (levier + banques) : à valider/remplacer avant release — rappel du constat F1.

---

*Prochaine étape : audit préparatoire MT0 à HEAD (`0c304c9` / sync OK) → proposition de gates figés → Go d’Arthur → premier prompt Cursor.*
