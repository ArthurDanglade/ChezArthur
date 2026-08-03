# Prompt Cursor — F2-P2b : Migration data groupe A → catalogue (2 passes)

> **Chantier SFX/VFX — gate F2, partie 2b.** Réf : plan §F2-P2b **validé** (Go 03/08 : Super hors migration · tension + tick locaux · purge en fin de gate). HEAD de référence : `a182fae`.
> **Principe** : une seule source de vérité pour la data du groupe A — clips, prefabs de burst et volumes de base passent de la scène au `FeedbackCatalog`. **Les courbes restent code** (pitch/volume ∝ dégâts/vitesse, escalade, trauma, hitstop, couleurs mutées au spawn). Aucune valeur ne change : le builder **copie** la vérité scène, il n'invente ni ne corrige rien.
> **Deux passes Cursor, jamais d'un coup.** Passe 1 = accessors à repli legacy + outillage (commit 1 — jeu strictement inchangé, réf catalogue absente de la scène). Passe 2 = purge des champs migrés (commit 4) — **UNIQUEMENT quand Arthur l'annonce explicitement**, après validation A/B de la marche 3.

---

## DEMANDE

Faire du catalogue la source des clips/prefabs/volumes de base du groupe A, en 5 marches revertables : code à repli legacy → migration asset par builder → câblage scène → purge code → purge scène.

## PÉRIMÈTRE — fichiers

**Passe 1 — à modifier** : `Assets/_Project/Scripts/Gameplay/JuiceDirector.cs` (zones data UNIQUEMENT, listées en §3).
**Passe 1 — à créer** : `Assets/_Project/Scripts/Editor/JuiceDataMigrationBuilder.cs`.
**Passe 2 — à modifier** : `JuiceDirector.cs` uniquement (purge §5).

**INTERDIT** : `CombatFeedbackService` · `FeedbackCatalog.cs` (`Resolve` est déjà lazy-idempotent — vérifié à l'audit, n'y touche pas) · `FeedbackBundle`/`FxPool`/`PooledFxReturner`/`FeedbackEventId` · `AllyHitReaction`, `CharacterBallFloat`, `CharacterBallFactory` · `SfxPlayer`/`SfxManager`/`AudioManager`/`AudioBuses` · `CharacterBall.cs`, `Enemy.cs` · toute scène · tout asset (`FeedbackCatalog.asset` n'est modifié QUE par le builder, marche 2, hors session Cursor) · zones gelées G6 (`Scripts/Enemies/**`, `Scripts/Gameplay/Passives/**`). Aucun renommage d'API existante.

## SPÉCIFICATION — PASSE 1

### 1. `JuiceDirector` — champ catalogue + résolution paresseuse

```csharp
[Header("Catalogue Feedback (F2-P2b)")]
[Tooltip("Source data du groupe A (clips / prefabs de burst / volumes de base). Câblé par builder.")]
[SerializeField] private FeedbackCatalog _feedbackCatalog;
```

6 refs privées non sérialisées `_launchBundle, _bounceBundle, _hitBundle, _critBundle, _killBundle, _defeatBundle` + `bool _catalogBundlesResolved`. Méthode `ResolveCatalogBundlesIfNeeded()` : early-out sur le bool ; le positionner à true ; si `_feedbackCatalog == null` → return (tout legacy) ; sinon 6 × `_feedbackCatalog.Resolve(FeedbackEventId.X, null)` (AllyLaunch, WallBounce, HitEnemy, Crit, Kill, DefeatBeat). Résolution **lazy au premier accès** (pas dans Awake — indépendance à l'ordre de chargement ; `Resolve` construit son index tout seul).

### 2. Accessors privés — repli legacy PAR PAYLOAD

**Règle-clé : le catalogue ne prime que si le payload existe (`HasSfx` / `HasVfx`).** Un bundle présent mais vide (entrée non migrée — cas réel : `Crit`, dont le champ scène est null) retombe sur le champ legacy. Ne JAMAIS tester seulement `bundle != null` : les 40 entrées existent toutes dans l'asset.

```csharp
private AudioClip[] GetHitClips()        // bundle HitEnemy.clips si HasSfx, sinon _hitClips
private AudioClip   GetCritClip()        // tirage aléatoire dans Crit.clips si HasSfx, sinon _critClip
private AudioClip[] GetBounceClips()     // WallBounce.clips si HasSfx, sinon _wallBounceClips
private AudioClip   GetKillClip()        // tirage dans Kill.clips si HasSfx, sinon _killClip
private AudioClip   GetLaunchClip()      // tirage dans AllyLaunch.clips si HasSfx, sinon _launchClip
private float       GetLaunchVolume()    // AllyLaunch.volumeScale si HasSfx, sinon _launchVolume
private AudioClip   GetDefeatStampClip() // tirage dans DefeatBeat.clips si HasSfx, sinon _defeatStampClip
private float       GetDefeatStampVolume() // DefeatBeat.volumeScale si HasSfx, sinon _defeatStampVolume
private ParticleSystem GetImpactBurstPrefab() // HitEnemy.vfxPrefab si HasVfx, sinon _impactBurstPrefab
private ParticleSystem GetLaunchBurstPrefab() // AllyLaunch.vfxPrefab si HasVfx, sinon _launchBurstPrefab
private ParticleSystem GetDeathBurstPrefab()  // Kill.vfxPrefab si HasVfx, sinon _deathBurstPrefab
```

Chaque accessor appelle `ResolveCatalogBundlesIfNeeded()` en tête. Tirages via `Random.Range` (zéro alloc). Les tirages multi-clips préparent la banque pro : aujourd'hui 1 clip par slot mono-clip → comportement identique.

### 3. Sites de lecture — swap mécanique (12 sites / 8 méthodes, AUCUNE courbe touchée)

| Site | Remplacement |
|---|---|
| `PlayLaunchNormal` (~L236) | `AudioClip clip = GetLaunchClip();` garde `clip != null` ; volume → `GetLaunchVolume()` |
| `SuperLaunchSequence` — repli détonation launch (~L280) | idem (`GetLaunchClip()` / `GetLaunchVolume()`) — emphase 5 inchangée |
| `SpawnLaunchBurst` (~L310) | `ParticleSystem prefab = GetLaunchBurstPrefab();` garde null + `SpawnFxGuarded(prefab, …)` |
| `PlayBounceWall` (~L388) | `AudioClip[] clips = GetBounceClips();` garde null/vide + tirage existant |
| `PlayKill` — death burst (~L409) | `ParticleSystem prefab = GetDeathBurstPrefab();` |
| `PlayKill` — son (~L415) | `AudioClip killClip = GetKillClip();` ; repli grave : `AudioClip[] hitClips = GetHitClips();` (mêmes 0.9f / 0.75f) |
| `DefeatBeatRoutine` (~L541) | `AudioClip stamp = GetDefeatStampClip();` ; volume → `GetDefeatStampVolume()` |
| `SpawnImpactBurst` (~L559) | `ParticleSystem prefab = GetImpactBurstPrefab();` garde null en tête |
| `PlayHitSfx` — crit (~L591) | `AudioClip critClip = GetCritClip();` puis `if (isCrit && critClip != null)` |
| `PlayHitSfx` — hit (~L598) | `AudioClip[] hitClips = GetHitClips();` garde + tirage existants |

**NE PAS TOUCHER** : familles/emphases des appels guidés (mapping P2a), toutes les formules de volume/pitch/scale, `_hitParticleColor`/`_critParticleColor` et la mutation du burst, le triptyque Super (`_superChargeClip`/`_superLaunchClip`/`_superDetonationLayerClip` + volumes — hors migration, Go Q1), la tension de visée et `_zoneEnterTickClip` (hors migration, Go Q2), duck, hitstop, trauma, finisher, defeat, escalade.

### 4. `JuiceDataMigrationBuilder` (éditeur, namespace `ChezArthur.EditorTools`) — 3 menus

**A. `[MenuItem("Chez Arthur/Feedback/Migrer JuiceDirector vers Catalogue")]`** — la migration (marche 2).
Préconditions : scène active contenant un `JuiceDirector` (`FindObjectOfType`, erreur claire sinon) ; catalogue chargé depuis `Assets/_Project/Data/Feedback/FeedbackCatalog.asset` (erreur sinon). Lit les valeurs du JuiceDirector par `SerializedObject` (noms exacts : `_hitClips`, `_critClip`, `_wallBounceClips`, `_killClip`, `_launchClip`, `_launchVolume`, `_defeatStampClip`, `_defeatStampVolume`, `_impactBurstPrefab`, `_launchBurstPrefab`, `_deathBurstPrefab`). Écrit le catalogue via `EntriesMutable` (+ `Undo.RecordObject`, `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets`).

Table de migration (copie, jamais d'invention — les métadonnées miroir documentent le mapping P2a, elles ne sont PAS consommées par JuiceDirector) :

| Entrée | clips ← | vfxPrefab ← | volumeScale ← | Miroir famille / emphase |
|---|---|---|---|---|
| `AllyLaunch` (0) | `[_launchClip]` | `_launchBurstPrefab` | `_launchVolume` | Moments / 3 |
| `WallBounce` (3) | `_wallBounceClips` | — | — | Impacts / 2 |
| `HitEnemy` (4) | `_hitClips` | `_impactBurstPrefab` | — | Impacts / 4 |
| `Crit` (5) | `[_critClip]` | — | — | Impacts / 5 |
| `Kill` (6) | `[_killClip]` | `_deathBurstPrefab` | — | Impacts / 5 |
| `DefeatBeat` (8) | `[_defeatStampClip]` | — | — | Moments / 6 |

Règles : **idempotence** — une entrée avec `HasSfx || HasVfx` est déjà migrée → INTACTE, jamais réécrite. Champ scène null → slot laissé vide + ligne LAISSÉE VIDE au rapport (cas attendu : `_critClip` est null en scène — le crit retombera sur les hits, comme aujourd'hui). Les clips mono-champ sont enveloppés en tableau de 1 (null exclu). Métadonnées miroir (voiceFamily/emphasis) écrites uniquement avec un payload. `pitchMin/Max`, `cooldownMs`, autres champs : non touchés (non consommés — courbes et plafonds restent code). volumeScale copié uniquement pour AllyLaunch et DefeatBeat (les autres volumes sont des courbes).
Rapport `Audits/JuiceDataMigration_<yyyyMMdd_HHmm>.md` : par entrée MIGRÉE (valeurs copiées listées) / INTACTE / LAISSÉE VIDE (raison). **Zéro écriture scène, zéro écriture JuiceDirector.**

**B. `[MenuItem("Chez Arthur/Feedback/Câbler Catalogue sur JuiceDirector (Scène)")]`** — marche 3. `SerializedObject` sur le JuiceDirector de la scène active : `_feedbackCatalog` ← asset (chemin const partagé). Idempotent (« déjà câblé »), `Undo` + `MarkSceneDirty`. ⚠️ **Ne PAS exécuter dans la session Cursor.**

**C. `[MenuItem("Chez Arthur/Feedback/Re-sérialiser Scène Combat (purge P2b)")]`** — marche 5. `MarkSceneDirty` sur la scène active + `SaveOpenScenes` + log (la re-save fait tomber les valeurs YAML des champs supprimés en passe 2). ⚠️ **Ne PAS exécuter dans la session Cursor.**

## SPÉCIFICATION — PASSE 2 (purge — SEULEMENT sur annonce explicite d'Arthur, après A/B marche 3)

1. Supprimer les 11 champs `[SerializeField]` migrés : `_hitClips`, `_critClip`, `_wallBounceClips`, `_killClip`, `_launchClip`, `_launchVolume`, `_defeatStampClip`, `_defeatStampVolume`, `_impactBurstPrefab`, `_launchBurstPrefab`, `_deathBurstPrefab` (et leurs attributs/tooltips).
2. Les accessors perdent le bras legacy : payload si `HasSfx`/`HasVfx`, sinon `null` (clips/prefabs) ou `0f` (volumes — jamais lus sans clip, les sites gardent leurs null-checks). Entrée vide = silence propre, zéro crash.
3. Rien d'autre : le triptyque Super, la tension, le tick, toutes les courbes et leurs champs restent sérialisés. Compile sans warning (aucun champ orphelin).

## CONVENTIONS

`.cursorrules` intégral : commentaires FRANÇAIS, noms ANGLAIS, bandeaux, zéro alloc/LINQ en hot path (accessors = comparaisons + index, résolution one-shot), éditeur sous `Scripts/Editor/` + `#if UNITY_EDITOR`. Compile sans warning.

## SÉQUENCE (5 marches / 5 commits — jamais code et scène dans le même commit)

1. **Passe 1** → compiler → **commit 1 (code)** : `feat(feedback): F2-P2b catalogue source du groupe A — accessors repli legacy + builder migration`. Jeu strictement inchangé (réf null en scène).
2. Arthur : menu **Migrer** → lire le rapport → `Audit Catalogue Feedback` → **commit 2 (asset + rapports)** : `feat(feedback): F2-P2b migration data groupe A vers catalogue`. Aucune scène.
3. Arthur : menu **Câbler** → **A/B complet** → **commit 3 (scène seule)** : `feat(feedback): F2-P2b câblage catalogue sur JuiceDirector`.
4. Sur annonce « A/B validé » : **Passe 2** → **commit 4 (code)** : `refactor(feedback): F2-P2b purge des champs legacy migrés`.
5. Arthur : menu **Re-sérialiser** → **commit 5 (scène seule)** : `chore(feedback): F2-P2b purge scène des valeurs migrées`. A/B final.

## CHECKLIST DE TEST

1. **Après commit 1** : une run normale rigoureusement identique (réf catalogue absente → tout legacy) ; compile 0 warning.
2. **Marche 2** : rapport = 5 entrées MIGRÉES (AllyLaunch, WallBounce, HitEnemy, Kill, DefeatBeat), `Crit` LAISSÉE VIDE (champ scène null — attendu), 0 INTACTE au premier run ; guids du rapport = guids scène (spot-check 2–3 dans `Game.unity`) ; re-run du menu → 5 INTACTES, `git status` propre ; diff du commit = catalogue + rapports uniquement ; `FeedbackCatalogAuditor` vert.
3. **Marche 3 — A/B au ressenti, le cœur du gate** : lancer, Super complet (riser + détonation 2 couches + repli launch si on vide temporairement le champ super), échelle de pitch des rebonds, hit, crit (doit sonner **comme un hit**, comme aujourd'hui — `_critClip` null), kill + fallback, défaite, bursts (impact teinté crit/normal, launch scale ∝ vitesse, death). Slider SFX. Diff scène = la seule propriété `_feedbackCatalog` sous le composant JuiceDirector.
4. **Robustesse marche 3** : dé-câbler la réf catalogue dans l'Inspector → replis legacy, aucun crash, aucun log d'erreur ; re-câbler.
5. **Marches 4–5** : A/B final identique ; diff scène du commit 5 = **suppressions sous le composant JuiceDirector uniquement** — tout autre delta = STOP, on regarde avant de committer ; `FeedbackCatalogAuditor` re-vert ; Profiler : 0 alloc/frame en spam stable (accessors inclus).
6. `git status` à chaque marche : la scène n'apparaît que dans les commits 3 et 5.
