# PROMPT CURSOR — MT2-G1 : data & socle saisons (save v4, score, snapshot rotation, rollover)

> Chez Arthur — Unity 2022.3, C#. `.cursorrules` strict (commentaires FR, noms EN, null-safe, zéro dépendance).
> Base : `main` à `d77a639` (ou HEAD). Contrat : `Systeme_Saisons_Design_v2.docx` + `Plan_Execution_MT2_Saisons.md` §1.
> Le multiplicateur de cran N'EXISTE PAS encore (gate G2) : partout où il intervient, valeur `1f` via le champ prévu.

## PÉRIMÈTRE — 9 FICHIERS MODIFIÉS + 1 CRÉÉ

Modifiés : `Core/SaveData.cs` · `Core/SaveSystem.cs` (1 ligne) · `Core/SaveMigrator.cs` · `Core/PersistentManager.cs` · `Core/RunManager.cs` · `Meta/GameClock.cs` · `Meta/SeasonRotationManager.cs` · `Gameplay/StageGenerator.cs` · `Debug/DebugMenu.cs`
Créé : `Meta/SeasonProgressManager.cs`
**RIEN D'AUTRE.** Interdits : missions, gacha, UI hors DebugMenu, scènes, `Feedback/**`, `UI/RevealStage/**`.

## 1. `SaveData.cs` — save v4

Nouveau bloc `// ── Saison (save v4) — REMIS À ZÉRO à chaque rollover ──` :
`string seasonId = ""` · `int bestScoreThisSeason` · `int bestStageThisSeason` · `float bestTierThisSeason = 1f` (cran du meilleur score) · `int runsThisSeason` · `List<int> claimedTiers` · `int prestigeTiersClaimed`.
Nouveau bloc `// ── Progression de COMPTE — JAMAIS touchée par un reset de saison ──` :
`List<int> unlockedDifficulties` (indices de cran ; 0 = x1 implicite, liste vide valide) · `long lastSeasonRolloverUtcTicks` · `long lastSeenUtcTicks` (garde anti-recul).
Nouveau `[Serializable] class SeasonRecapData` : `string seasonId; int finalScore; int bestStage; float bestTier; int runs; int lastTierReached; bool pending;` + champ `SeasonRecapData pendingSeasonRecap = new SeasonRecapData();` (dans le bloc compte — survit au reset).

## 2. `SaveSystem.cs`

`CURRENT_SAVE_VERSION = 4;` — rien d'autre.

## 3. `SaveMigrator.cs`

`MigrateV3ToV4(SaveData)` : docstring FR documentant le schéma v4 (champs additifs saison + compte, défauts corrects par construction) ; corps vide hors normalisation. `NormalizeNulls` : ajouter `claimedTiers`, `unlockedDifficulties`, `seasonId`, `pendingSeasonRecap` (null → instance neuve) et ses strings. Cascade : `if (from < 4) MigrateV3ToV4(data);`. Nouveau gabarit v5 commenté.

## 4. `PersistentManager.cs`

Champs privés + propriétés lecture pour tout le §1 ; wiring `SaveGame()`/`LoadGame()` symétrique au pattern existant ; `ResetAllData()` remet tout (saison ET compte saison). API publiques (chacune : mutation → `SaveGame()` → `OnDataChanged`) :
- `bool TryImproveSeasonScore(int score, int stage, float tier)` — n'écrit que si `score > bestScoreThisSeason` (met à jour score/stage/tier ensemble) ; retourne true si record.
- `void IncrementSeasonRuns()`.
- `void ApplySeasonRollover(string newSeasonId, SeasonRecapData recap)` — écrit le récap (`pending = true`), **remet à zéro uniquement le bloc saison** (§8.2 v2 strict), pose `seasonId = newSeasonId`, `lastSeasonRolloverUtcTicks = GameClock.UtcNow.Ticks`. Une seule Save à la fin.
- `void SetSeasonId(string id)` (première init, sans reset).
- Accès `PendingSeasonRecap` + `void MarkRecapShown()` (pending = false, Save).

## 5. `GameClock.cs` — garde anti-recul (couture temps de MT2-D4)

- `public static DateTime UtcNowGuarded` : max(`UtcNow`, plancher persisté) ; plancher = `PlayerPrefs` `GameClock_LastSeenUtcTicks`, mis à jour (écriture différée : au plus 1×/minute) quand `UtcNow` progresse ; si `UtcNow` < plancher − 5 min → retourner le plancher + `LogWarning` unique par session (« horloge reculée — temps gelé au plancher »).
- Les ids daily/weekly et `ParisNow` basculent sur `UtcNowGuarded` (l'override debug reste prioritaire, inchangé — le voyage dans le temps debug ne doit PAS être bloqué par la garde : l'override court-circuite le plancher).
- Commentaire de tête : « garde locale en attendant le temps serveur (MT4) — limitation assumée : PlayerPrefs effaçables ».

## 6. `SeasonRotationManager.cs` — 6 semaines + snapshot

- `private const int SEASON_LENGTH_WEEKS = 6;` — `CurrentSeasonId` = `weeks / SEASON_LENGTH_WEEKS` (**le cycle de rotation reste `SEASON_WEEK_COUNT = 5`, inchangé partout ailleurs**). Docstring : « saison 6 semaines (MT2-D1), rotation cycle 5 indépendant ; rollover lundi 00h00 Europe/Paris (MT2-D9) ».
- `[Serializable] public class RotationSnapshot { public int weekIndex; public int[] universeBySlot; }` + `public static RotationSnapshot BuildSnapshot()` (5 slots via la table courante) + `public static int GetUniverseForStage(RotationSnapshot s, int stageNumber)` (même arithmétique que `GetSlotIndexForStage`, null-safe → fallback live).

## 7. `SeasonProgressManager.cs` — NOUVEAU (`ChezArthur.Meta`, statique)

- `EnsureSeasonCurrent()` : garde `PersistentManager` ; si `save.seasonId` vide → `SetSeasonId(CurrentSeasonId)` ; si différent de `SeasonRotationManager.CurrentSeasonId` → construire `SeasonRecapData` depuis l'état courant (`lastTierReached` = 0 pour l'instant, G3 le calculera) → `ApplySeasonRollover`. Log clair `[Season]`.
- Appelée : depuis `MissionManager.ApplyResetsIfNeeded` ? **NON — interdit de toucher les missions.** Appel : `RunManager.StartRun()` (avant le snapshot) + `DebugMenu` (bouton). Le hub la rattrapera en G4 (consigné).
- `ReportStageReached(int stage, float tierMultiplier, bool isBossRush, bool tainted)` : garde BossRush/tainted → return ; `score = Mathf.RoundToInt(stage * tierMultiplier)` ; `TryImproveSeasonScore(score, stage, tierMultiplier)`.

## 8. `RunManager.cs`

- Champs : `private SeasonRotationManager.RotationSnapshot _rotationSnapshot;` (+ propriété publique lecture) · `private float _currentDifficultyMultiplier = 1f;` (+ propriété ; G2 le pilotera) · `private bool _seasonTainted;`.
- `StartRun()` (après la garde existante) : `SeasonProgressManager.EnsureSeasonCurrent();` · `_rotationSnapshot = SeasonRotationManager.BuildSnapshot();` · `_seasonTainted = false;` · si mode Normal : `PersistentManager.Instance?.IncrementSeasonRuns();`.
- **Report par étage atteint** (couvre l'abandon par construction) : au point où un étage démarre/est généré (même endroit qui fixe `_currentStage` à la génération — y compris l'étage 1) : `SeasonProgressManager.ReportStageReached(_currentStage, _currentDifficultyMultiplier, IsBossRush, IsSeasonTainted());`.
- `IsSeasonTainted()` : `_seasonTainted` OU (sous `#if UNITY_EDITOR || DEVELOPMENT_BUILD`) un cheat actif (`DebugCheats.GodMode || OneShot || EnemyGodMode`) ; en release : `_seasonTainted` seul.
- `DebugRestartRunAtStage` : pose `_seasonTainted = true` (avant StartRun ? non — StartRun le remet à false : poser APRÈS l'appel `StartRun()`).
- `UpdateBestStage` existant (l.687) : inchangé.

## 9. `StageGenerator.cs`

Ligne 632 : remplacer l'appel live par le snapshot : `logicalUniverse = RunManager.Instance != null && RunManager.Instance.RotationSnapshot != null ? SeasonRotationManager.GetUniverseForStage(RunManager.Instance.RotationSnapshot, stageNumber) : SeasonRotationManager.GetLogicalUniverseForStage(stageNumber);` (fallback live si pas de run — scènes de test). Rien d'autre dans ce fichier.

## 10. `DebugMenu.cs` — section META/SAISON étendue

Sous les labels existants : `Score saison : {bestScoreThisSeason} (ét. {bestStageThisSeason} ×{bestTierThisSeason})` · `Saison save : {seasonId} / calc : {CurrentSeasonId}` · `Runs : {runsThisSeason}` · boutons `Check rollover` (→ `EnsureSeasonCurrent()`) et `Recap pending : {pending}` en label. Null-safe.

## GARDE-FOUS
- Le reset de rollover ne touche JAMAIS : `unlockedDifficulties`, Tals, `bestStage`, personnages, éveils, Boss Rush, missions (bloc §8.2 v2 strict).
- Aucune modification de `MissionManager`/gacha/UI hub. Logs `[Season]`/`[GameClock]`. Pas de LINQ en boucle chaude.

## CHECKLIST (Arthur)
1. **Migration v3→v4** : save v3 réelle → lancement → log `[SaveMigrator] v3 → v4`, jeu intact, nouveaux champs à défaut sur disque après save.
2. **Score par étage** : run x1 jusqu'à l'étage 4 → quitter par le menu (abandon) → DebugMenu : score 4, stage 4, runs +1. Refaire une run jusqu'à 2 → score reste 4.
3. **Gardes** : run Boss Rush → score inchangé · « Restart à l'étage 30 » (debug) → score inchangé (tainted) · god mode actif → étages non scorants.
4. **Snapshot** : en run, `Semaine +1` au DebugMenu → les étages suivants de LA MÊME run gardent les univers du lancement ; la run suivante prend la nouvelle semaine.
5. **Rollover** : `+7 jours` ×6 (42 j) → `Check rollover` → log `[Season]`, recap pending = true, score/stage/runs/claims à 0, `seasonId` avancé, **Tals/bestStage/persos/unlockedDifficulties intacts**, missions inchangées.
6. **Anti-recul** (hors override debug) : reculer l'horloge Windows d'1 h → lancement → warning `[GameClock]`, daily id stable ; remettre l'heure.
7. **Non-régression smoke** : run + pause + gacha + missions claim + Boss Rush + fin de run.
