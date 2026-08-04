# Audit préparatoire — MT0 Fondations (vérité terrain)

**Take Five Games — Track Zero** · 4 août 2026 · v1
Repo audité : `ArthurDanglade/ChezArthur`, branche `main`, commit **`3700a40`** (vérifié : `3700a4053dc097001ee26060b87c0a527916f6dc`). Canal : **clone direct du repo** (nouveau canal de lecture, remplace le pont device pour l'audit). Unity **2022.3.62f3**.
Conformité MT-D5 : `claude/raw_meta/` **non lu** — réservé à l'ouverture MT1-0 / MT2-0.

---

## 1. Synthèse exécutive

**Le fait majeur de cet audit : le sync GitHub du projet Claude était très en retard sur le repo réel.** L'état à HEAD dépasse largement l'état des lieux du Plan Directeur v1.1 : les chantiers parallèles d'Arthur ont déjà posé **un système de missions v0 complet** (daily/weekly/seasonal + UI hub + badge), **une rotation saisonnière v0** (horloge Paris, saisons de 5 semaines, 5 slots × 20 étages, 5 univers nommés, content gate), **un mode Boss Rush**, **un outillage debug riche** (menu in-game avec voyage dans le temps, cheats strippés release, dev menu), et un **settings assaini**. Le plan directeur passera en v1.2 pour refléter ça.

Conséquence : **MT0 se recadre de 4 gates pressentis à 3 gates réels.** Le gate « menu debug » est quasi soldé par l'existant ; le gate « flow » se requalifie en assainissement de sémantique. En revanche les deux constats critiques du plan directeur sur la save **se confirment et s'aggravent** : l'écriture n'est pas atomique, et surtout **une save corrompue est irrécupérable ET écrasée définitivement au premier SaveGame suivant** (§3.1). Le versioning existe (`saveVersion = 3`) mais **n'est jamais lu** : c'est un tampon, pas une chaîne de migration (§3.2).

---

## 2. Vérité terrain par axe

### 2.1 — Save (périmètre G1)

| Élément | État | Référence |
|---|---|---|
| Versioning | `CURRENT_SAVE_VERSION = 3`, **stampé à l'écriture seulement** (`data.saveVersion = 3` dans `Save()`). Jamais lu au chargement — aucun branchement, aucune migration. Champ `saveVersion = 0` par défaut (vieilles saves). | `SaveSystem.cs:14,28` · `SaveData.cs:30` |
| Écriture | `File.WriteAllText` direct — **non atomique**, pas de fichier temporaire, pas de flush contrôlé. Kill pendant l'écriture = fichier tronqué. | `SaveSystem.cs:30` |
| Corruption | `Load()` : exception de parse → `return new SaveData()`. **Aucune quarantaine, aucun backup.** | `SaveSystem.cs:60-64` |
| Écrasement | `LoadGame()` tourne dans `Awake()` du PersistentManager ; le premier `SaveGame()` venu (AddTals, drag d'équipe, hint…) **écrase le fichier corrompu avec la save vierge**. Perte définitive. | `PersistentManager.cs:114,355-397` |
| Contenu v3 | playerName, tals, bestStage, bestSuperLancerHits, personnages + 5 presets, pity par bannière, `lastDailyResetId`/`lastWeeklyResetId`/`lastSeasonId`, `missionProgress`, Boss Rush (unlocked/roster/majors/weeklyCounted), `accountScore` (monotone), `hintTeamDragSeen`. Legacy : `selectedTeamIds` (migration ad hoc dans CharacterManager) + `SanitizeAllTeamPresets()` post-load. | `SaveData.cs` · `PersistentManager.cs:484` |
| Fréquence | `SaveGame()` synchrone à chaque mutation (Tals, équipe, hints, éveil, gacha, fin de run…). ~16 sites d'appel. Acceptable aujourd'hui, débounce consigné en option. | grep `SaveGame` |
| PlayerPrefs | 6 fichiers, **tous préférences device** (volumes musique/SFX/train/vinyle/tals-pickup/cérémonie). Frontière de facto saine : prefs = device, save.json = progression. À documenter en G1. | SettingsPanelUI, AudioManager, SfxManager, SfxPlayer, TalsDropSystem, AwakeningCeremonyController:993 |

### 2.2 — Localisation (périmètre G2)

**Rien n'existe** (zéro occurrence de système de localisation). Échelle : **~210 fichiers runtime** contiennent des littéraux FR accentués (324 avec l'outillage éditeur/debug) — mélange de textes joueur et de logs internes (les logs restent FR, l'extraction devra trier). S'y ajoutent les **textes joueur portés par les SO** : missions (`GetResolvedDisplayName`), passifs (`passiveName`/`description`), lore. Textes latins uniquement → pas de risque de police pour l'EN.

### 2.3 — Outillage debug (gate pressenti G3 : quasi soldé)

| Outil | Contenu | Référence |
|---|---|---|
| `DebugMenu` (in-game, bouton DBG) | **RUN** : restart, restart à l'étage N, skip stage, passe tour, timescale ×1/×2/×4 · **META/SAISON** : horloge Paris live, daily/weekly ids, saison + semaine /5, univers par slot, **semaine ±1, +1/+7 jours, clear override** · **MISSIONS** : par layer (Daily/Weekly/Permanent), claim all, force resets, reset vierge · **BOSS RUSH** : force unlock, reset · **CHEATS** : god mode, one-shot, enemy god mode | `Debug/DebugMenu.cs` (1 649 l.) |
| `DebugCheats` | Flags globaux **strippés release** (`#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`) + reset au domain reload | `Debug/DebugCheats.cs` |
| `DevMenuUI` | Débloquer tous les persos, +100 000 Tals, best stage 50, reset save (double confirmation), bandeau FPS/min/mémoire | `DevTools/DevMenuUI.cs:114-117` |
| Divers | `GachaTestRunner`, `HitboxDebugOverlay` | `Debug/` |

**Manques identifiés (petits)** : forcer le pity à N, donner un perso ciblé (id + niveau), export/import de save pour la QA. → absorbés en G1, pas de gate dédié.

### 2.4 — Flow & machine d'états (périmètre G3 requalifié)

- Les deux « Temporaire » du plan directeur **sont toujours là à HEAD** : `GameManager` démarre en `Playing` (`GameManager.cs:17-18`) ; `RunManager.Start()` auto-lance la run (`RunManager.cs:163-164`).
- **Mais le flow réel est propre et fonctionne** : Hub = scène 0 du build, Game = scène 1 (SampleScene désactivée) ; `SceneLoader` (timeScale garanti) ; `AppBootstrap` (`RuntimeInitializeOnLoadMethod` : 60 fps, vSync 0, no-sleep) ; `PendingRunMode` Hub→Game propre (Normal/BossRush, consommé par `StartRun`).
- **`GameState.Menu` n'est référencé nulle part** (grep = 0 hors définition) : au Hub, l'état global reste `Playing`. La machine d'états est de facto une machine **de combat** à sémantique fausse hors combat. Risque faible aujourd'hui, piège demain (tuto, écrans méta, analytics d'états).
- Scène dev : `Dev/UIKitSandbox.unity` (hors build).

### 2.5 — Découvertes hors périmètre MT0 (recadrent le backlog)

| Système | État à HEAD | Impact backlog |
|---|---|---|
| **Meta/Saisons v0** | `GameClock` (fuseau Paris + fallback, ids daily/weekly, **override debug + DebugAdvanceDays**) · `SeasonRotationManager` (saison = 5 semaines, table 5×5, epoch **en dur** 20/07/2026, `CurrentSeasonId` "S1…", semaine forçable debug) · `UniverseContentConfig.asset` (content gate + fallback Ardacula, `forceArdaculaOnly`) · 5 univers nommés (`UniverseIds` : Ardacula/L'Ancien/Don Costardo/Faille/Troplin) · branché StageGenerator + missions | **MT2 passe de « créer » à « étendre »** : il manque la couche vision MT-D1 (portails, seuils, récompenses, cérémonie de reset) — pas le socle |
| **Missions v0** | `MissionManager` (698 l.) : layers Daily/Weekly/Seasonal/Permanent, catalogue SO (`MissionCatalog.asset`), triggers (`ReportCounter`/`ReportStageReached`/`ReportUniverseCompleted` lié au slot 1 saisonnier/`ReportCharacterObtained`), claims, resets par GameClock, planning hebdo par rôle (`WeeklyMissionSchedule` : ATK/DEF/SUP/ATK/DEF), snapshot de composition de run · **UI hub complète** (`Hub/Pages/Missions/` : page, entrées, badge nav, FX de claim) | **MT3 quasi soldé** → audit de complétude court (récompenses, équilibrage, missions login éventuelles) |
| **Boss Rush** | Mode complet : déblocage permanent, roster first-kill, boss majeurs, accroche mission hebdo, `GameRunMode` Hub→Game, contrôleur dédié | Nouveau pilier de contenu à intégrer au plan (non documenté en v1.1) |
| **Divers méta** | `accountScore` (prestige monotone) · `hintTeamDragSeen` (1er germe d'onboarding, « Gate 5.b ») · `BannerData.dateFinSaison` (bannières expirables, ticks UTC) · `CombatStatsTracker`, `InterStageGate`, `GameplayInputLock` | MT1 : le hint existant = à intégrer au moteur FTUE · MT4 : la brique bannières datées existe |
| **Écrans** | `PageAccueilUI` : les 3 boutons morts ont été **supprimés** (accueil = Lancer Run + Boss Rush) · `SettingsPanelUI` assaini : 3 sliders branchés (AudioManager/SfxManager/TalsDropSystem), le TODO obsolète a disparu | Constat « boutons morts » du plan v1.1 **périmé** · MT5 recadré (réintroduction Magasin/News = décision produit) |
| **Absents confirmés** | Reprise de run : **rien** (aucune persistance d'état de run) · CI : **aucun workflow** · Localisation : rien · Comptes/cloud/analytics/crash : rien | MT6, MT7, MT4 confirmés tels quels |

---

## 3. Constats critiques (numérotés)

1. **[CRITIQUE] Perte définitive sur save corrompue.** Chaîne réelle : parse en échec → `new SaveData()` silencieux → premier `SaveGame()` écrase le fichier. Le joueur perd tout, sans trace. À corriger en G1 (quarantaine + backup + garde anti-écrasement).
2. **[CRITIQUE] Écriture non atomique.** Kill/crash pendant `WriteAllText` = fichier tronqué → déclenche le constat n°1. G1.
3. **[MAJEUR] `saveVersion` jamais lu.** Illusion de versioning : le jour où un champ change de sens, aucun endroit pour migrer. La chaîne formelle (v0→v3 documentées + gabarit v4) est le cœur de G1.
4. **[MAJEUR] Resets et saisons sur horloge device.** `GameClock.UtcNow = DateTime.UtcNow` : reculer l'horloge du téléphone = re-farm des daily/weekly à volonté. Acceptable en dev ; **à durcir avant release** (monotone locale a minima, temps serveur en MT4). Consigné, hors périmètre MT0.
5. **[NOTABLE] Epoch et table de rotation en dur.** `SetEpochMondayParis` existe mais n'est jamais appelé ; la table 5×5 est dans le code. Pilotage à distance (changer le calendrier sans update) = MT4 (remote config), consigné.
6. **[NOTABLE] Sémantique `GameState` fausse hors combat** (§2.4). G3.
7. **[MINEUR] Fallback fuseau UTC+1 fixe** (`GameClock` si TZ introuvable) : DST faux dans ce cas. Android expose normalement l'IANA — risque faible, à vérifier par un log au premier lancement device (G1, gratuit).

## 4. Écarts Plan Directeur v1.1 ↔ code à HEAD

| Plan v1.1 disait | Réalité `3700a40` |
|---|---|
| « Pas de saisons » | Rotation saisonnière v0 complète (sans portails/seuils/récompenses) |
| « Missions : périmètre à vérifier » | Système + UI v0 complets, 4 layers |
| « MT0 gate 3 : créer le menu debug (dont voyage dans le temps) » | Existe déjà, voyage dans le temps inclus — reste 3 compléments mineurs |
| « Accueil : 3 boutons morts » | Supprimés (accueil = Run + Boss Rush) |
| « Settings : 2 sliders + TODO obsolète » | 3 sliders correctement branchés, TODO soldé |
| « Save non versionnée » | Versionnée **en écriture seulement** (constat n°3 : pas de migration) |
| Constats confirmés | Non-atomicité, absence backup/quarantaine, pas de reprise de run, pas de CI, pas de localisation, flow « temporaire » |

→ Plan Directeur à passer en **v1.2** après le Go (état des lieux corrigé, MT2 requalifié « étendre », MT3 requalifié « compléter », Boss Rush intégré).

---

## 5. Proposition de gates MT0 (recadrés : 3 gates)

### MT0-G1 — Save durcie + chaîne de migration *(petit, critique — ouvre le chantier)*

Périmètre fermé : `SaveSystem.cs` (+ nouveau `SaveMigrator.cs`) + compléments debug.
1. **Écriture atomique** : write vers `save.json.tmp` → `File.Replace` (avec backup automatique `save.json.bak`), flush explicite.
2. **Récupération** : parse en échec → le fichier fautif part en **quarantaine** (`save.json.corrupt-N`, jamais supprimé) → tentative de charge du `.bak` → sinon seulement, save neuve. Log explicite à chaque étape.
3. **Garde anti-écrasement** : tant que la quarantaine n'a pas été effectuée, aucun `Save()` ne peut toucher le fichier d'origine (le bug du constat n°1 devient structurellement impossible).
4. **Chaîne de migration** : `saveVersion` lu AVANT usage ; `SaveMigrator.MigrateToCurrent(data, from)` avec étapes v0→v1→v2→v3 explicites (l'actuel implicite documenté) + gabarit commenté pour v4+. `selectedTeamIds` legacy migre officiellement ici (comportement inchangé).
5. **Compléments debug** (absorbés) : forcer pity à N, donner perso par id + niveau, export/import de save (QA).
6. Doc courte en tête de `SaveSystem` : frontière PlayerPrefs (device) vs save.json (progression).

**Critères de test** : écriture interrompue simulée → relance charge le `.bak`, zéro perte · save corrompue à la main → quarantaine présente + `.bak` chargé + le `.corrupt` **n'est jamais écrasé** · save v0 réelle (si Arthur en retrouve une) → migrée, équipes/pity/records intacts · export→import → état identique · non-régression : run complète + gacha + missions claim + boss rush.

### MT0-G2 — Socle localisation FR/EN + écrans pilotes

1. **Décision d'outillage au gate** (proposition comparée custom SO vs Unity Localization, essai bref sur branche — reco livrée avec la proposition de gate).
2. Socle : table de clés, API d'accès unique, langue persistée (PlayerPrefs — préférence device), **repli FR** ; convention pour les textes portés par les SO (clé optionnelle + repli texte brut : legacy toléré, migration progressive).
3. **Pilotes de bout en bout** : page Paramètres + page Accueil migrées + sélecteur de langue.
4. La **migration de masse** (~210 fichiers + SO) est **hors gate** : lots opportunistes étalés (chaque lot = checklist visuelle), pilotée comme une dette suivie.

**Critères** : bascule FR↔EN à chaud sur les pilotes, zéro troncature, langue conservée au redémarrage, écrans non migrés strictement intacts.

### MT0-G3 — Sémantique d'états & flow assainis *(petit)*

1. Solder les deux « Temporaire » **par décision explicite** (proposition détaillée au gate ; pressenti : `GameState` documenté machine de combat, le Hub n'en dépend pas, démarrage cohérent par scène — OU état Menu réellement tenu au Hub ; l'audit fixe l'exigence, pas la solution).
2. `RunManager.Start()` auto-run : **assumé** (la scène Game EST une run) — commentaire remplacé par la règle actée + garde de double démarrage.

**Critères** : grep « Temporaire » = 0 dans `Core/` · run normale, Boss Rush, retour Hub, re-run, pause/resume/défaite : zéro régression · doc d'architecture d'états en tête de `GameManager`.

**Ordre : G1 → G2 → G3.** Un gate à la fois, boucle standard (proposition → Go → prompt Cursor → contrôle du diff → checklist → commit).

---

## 6. Points ouverts

1. Choix d'outillage localisation — tranché à la proposition G2.
2. Décision machine d'états (A : machine de combat documentée / B : état Hub réel) — tranchée à la proposition G3.
3. Arthur a-t-il une **vieille save réelle** (v0, avant versioning) pour le test de migration G1 ? Sinon on en fabrique une.
4. Débounce des `SaveGame` fréquents — différé, consigné (pas un problème mesuré aujourd'hui).
5. Sync du projet Claude en retard sur le repo (doublons d'anciennes versions dans la base de connaissances) — le clone direct devient le canal d'audit ; le sync se rattrapera seul.
6. `claude/raw_meta/` — scellé jusqu'à MT1-0 / MT2-0 (MT-D5).

---

*Prochaine étape : **Go d'Arthur sur MT0-G1** → proposition technique détaillée + prompt Cursor contre `3700a40` (ou HEAD du moment) + checklist de test.*
