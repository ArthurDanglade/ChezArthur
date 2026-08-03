# Prompt Cursor — F2-P1 : FeedbackCatalog + CombatFeedbackService (socle VFX data-driven)

> **Chantier SFX/VFX — gate F2, partie 1.** Réf : `Docs/Feedback/Charte_Feedback_Combat_F0.md` v1.1 (§3 budgets, §4 catalogue), plan §F2-P1 **validé** (Go 02/08 : enum figé · skip < 5 / steal ≥ 5 · pré-branchement banque v0). HEAD de référence : `6706de7`.
> **Code 100 % dormant** : rien dans le jeu n'appelle ce système avant F2-P2. Zéro changement de comportement en jeu après application — c'est le critère n°1 du diff.

---

## DEMANDE

Le socle data-driven du feedback : un enum figé des événements, un catalogue ScriptableObject (bundle par événement + overrides par personnage), un service runtime avec les garde-fous génériques de la charte (cooldowns, budget FX, familles de voix), un pool de particules à retour automatique, et l'outillage éditeur (builder idempotent + auditeur).

## PÉRIMÈTRE — fichiers

**À créer :**
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackEventId.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackContext.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackBundle.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackCatalog.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/CombatFeedbackService.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/FxPool.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/PooledFxReturner.cs`
- `Assets/_Project/Scripts/Editor/FeedbackCatalogBuilder.cs`
- `Assets/_Project/Scripts/Editor/FeedbackCatalogAuditor.cs`

**À modifier** : `Assets/_Project/Scripts/UI/CombatFeedbackPalette.cs` — **additif uniquement** (les 5 couleurs existantes ne bougent pas d'un octet).

**INTERDIT — ne touche à rien d'autre.** En particulier : `JuiceDirector`, `SfxPlayer`/`SfxManager`/`AudioManager`/`AudioBuses`, `CharacterBall`, `Enemy`, `TurnManager`, toute scène, tout asset existant, `Scripts/Enemies/**`, `Scripts/Gameplay/Passives/**` (zones gelées G6). Namespace : `ChezArthur.Gameplay.Feedback`.

## SPÉCIFICATION

### 1. `FeedbackEventId` — enum figé (liste fermée charte §4, tout ajout futur = avenant)

Valeurs explicites contiguës 0–39, commentaires de groupe :

```csharp
// ── Groupe A — cœur existant (re-câblé en F2-P2) ──
AllyLaunch = 0, SuperLaunch = 1, AimTension = 2, WallBounce = 3, HitEnemy = 4,
Crit = 5, Kill = 6, StageFinisher = 7, DefeatBeat = 8,
// ── Groupe B — langage d'état (émetteurs en F3) ──
HealReceived = 9, BuffApplied = 10, BuffExpired = 11, DebuffApplied = 12, DebuffExpired = 13,
ShieldGained = 14, ShieldAbsorbed = 15, ShieldBroken = 16,
BurnApplied = 17, BurnTick = 18, BurnEnded = 19,
PoisonApplied = 20, PoisonTick = 21, PoisonEnded = 22,
StunApplied = 23, StunEnded = 24, FreezeApplied = 25, FreezeEnded = 26,
// ── Groupe C — axe ennemi & moments (émetteurs en F4) ──
EnemyWindup = 27, EnemyLaunch = 28, EnemyHitAlly = 29, EnemyWallBounce = 30,
SummonSpawned = 31, TurnRelay = 32, VictorySting = 33, BossDefeated = 34,
SpecSwitch = 35, ZonePlaced = 36, ZoneCrossed = 37, Revive = 38, ExtraTurn = 39
```

`AimTension` : entrée présente pour la complétude data ; **le service V1 est one-shot only** — les boucles (tension de visée) restent gérées par JuiceDirector, commentaire explicite sur l'entrée.

### 2. `CombatFeedbackPalette.cs` — extension additive

Ajouter : `BuffUp #66B8FF`, `DebuffDown #B44DE6`, `Shield #7DE0FF`, `Stun #FFE066`, `Freeze #AEE9FF`, `Heal #4DFF66`, `Poison #80E633` (mêmes conventions que l'existant). Ajouter l'enum `FeedbackCause { None, Heal, BuffUp, DebuffDown, Shield, Burn, Poison, Stun, Freeze }` et `public static Color GetColor(FeedbackCause cause)` (switch ; `Burn` renvoie la couleur Burn existante ; `None` = blanc).

### 3. `FeedbackBundle` [Serializable]

Champs (avec Tooltips français) :
- **VFX** : `ParticleSystem vfxPrefab` (null = pas de visuel) · `FeedbackCause tintCause` + `enum TintMode { None, Cause, Custom }` + `Color customTint` · `enum AttachMode { World, FollowTarget }` · `float vfxScale = 1f`.
- **SFX** : `AudioClip[] clips` (tirage aléatoire) · `float volumeScale = 0.8f` · `float pitchMin = 0.96f` / `pitchMax = 1.04f` · `enum VoiceFamily { Impacts, Statuts, Moments, UI }` · `int cooldownMs = 100`.
- **Caméra/temps** : `float shakeTrauma = 0f` · `float hitstopMs = 0f`.
- **Réservés** : `enum HapticLevel { None, Light, Medium, Heavy } haptic = None` (consommé F4) · `bool respectsReduceMotion = true` (consommé F5).
- **Gouvernance** : `int emphasis = 2` (1–6, charte §3).
- `bool HasSfx` / `bool HasVfx` (propriétés).

### 4. `FeedbackCatalog` (ScriptableObject, `[CreateAssetMenu]` désactivé — création par builder)

- `List<Entry> entries` (`Entry { FeedbackEventId eventId; FeedbackBundle bundle; }`) + `List<CharacterOverride> overrides` (`{ string characterId; FeedbackEventId eventId; FeedbackBundle bundle; }`).
- `BuildRuntimeIndex()` : tableau `FeedbackBundle[40]` indexé par `(int)eventId` + `Dictionary<(string, int), FeedbackBundle>` pour les overrides. Appelé une fois par le service à l'Init. `Resolve(eventId, characterId)` : override si characterId non vide et présent, sinon défaut, sinon null. **Zéro LINQ, zéro alloc par appel.**

### 5. `FeedbackContext` (struct)

`Vector2 Position; Vector2 Direction; float Intensity01; Transform Target; CharacterBall TargetBall; string CharacterId;` + fabrique `static FeedbackContext At(Vector2 pos)` (Intensity01 = 1). Passée en `in` partout.

### 6. `CombatFeedbackService` (MonoBehaviour, singleton de scène — pattern maison Instance/OnDestroy)

- `[SerializeField] FeedbackCatalog _catalog;` — index construit à l'Awake (null-safe : sans catalogue, `Play` = no-op).
- `public void Play(FeedbackEventId id, in FeedbackContext ctx)` — ordre STRICT des garde-fous :
  1. Résolution bundle (override par `ctx.CharacterId`) ; absent → no-op + warning **unique par id** (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
  2. **Cooldown** par event : `float[40] _lastPlayTime` sur `Time.unscaledTime` ; trop tôt → skip total (compteur).
  3. **VFX budget** : si `ActiveFxCount >= 12` et `emphasis < 5` → skip VFX (compteur) ; sinon spawn via pool.
  4. **SFX familles de voix** : plafonds `Impacts 4 · Statuts 2 · Moments 2 · UI 1`. Suivi par tableaux de timestamps de fin (`now + clip.length / pitch`, unscaled) par famille. Famille pleine : `emphasis < 5` → skip son (compteur) ; `emphasis >= 5` → **vole le slot de la voix la plus ancienne de la même famille**. Lecture via `SfxPlayer.Instance.Play(clip, volumeScale, pitch)` avec clip aléatoire + pitch aléatoire dans [pitchMin, pitchMax].
  5. `shakeTrauma > 0` → `CameraShake` (référence `[SerializeField]`, null-safe). `hitstopMs > 0` → `ctx.TargetBall?.ApplyHitStop(hitstopMs / 1000f)` (seul porteur actuel — ignoré sinon).
  6. Haptic : no-op commenté « réservé F4 ».
- VFX spawn : position `ctx.Position`, rotation depuis `ctx.Direction` si non nulle (pattern `Quaternion.FromToRotation(Vector3.up, dir)` comme JuiceDirector), teinte selon TintMode (`main.startColor`, via `CombatFeedbackPalette.GetColor`), échelle `vfxScale`, `AttachMode.FollowTarget` → parenté à `ctx.Target` (reparenté au root du pool + échelle restaurée AU RETOUR — hygiène pooling obligatoire).
- Diagnostics publics : `int ActiveFxCount` (délégué au pool), `int SkippedFx`, `int SkippedVoices`, `int SkippedCooldown` (cumulatifs) + `[ContextMenu("DEV — Log et reset compteurs")]`.
- Dev harness : `[ContextMenu("DEV — Jouer tous les events (0,5 s)")]` (coroutine, log de chaque id joué) et `[ContextMenu("DEV — Spam HitEnemy x20 (200 ms)")]`.
- **Aucun Update()**. Aucune alloc par `Play` en régime stable (pas de string, pas de new, structs).

### 7. `FxPool` + `PooledFxReturner`

- `FxPool` (classe C#, propriété du service) : `Dictionary<ParticleSystem, Stack<ParticleSystem>>` par prefab + `Transform _poolRoot` (GO enfant du service) + `ActiveCount`. `Get(prefab)` : dépile ou instancie (préchauffe 4 instances au premier usage d'un prefab) ; force `main.stopAction = ParticleSystemStopAction.Callback` et ajoute/récupère `PooledFxReturner` (référence pool + prefab d'origine). `Release(instance)` : stop/clear, reparente au root, échelle locale restaurée, désactive, empile, décrémente.
- `PooledFxReturner` (MonoBehaviour) : `OnParticleSystemStopped()` → `Release`. Robustesse : `OnDestroy` décrémente si l'instance meurt hors pool (changement d'étage).

### 8. `FeedbackCatalogBuilder` (éditeur, idempotent, `[MenuItem("Chez Arthur/Feedback/Créer ou Mettre à Jour le Catalogue")]`)

**Règle d'idempotence : ne JAMAIS écraser une valeur existante non vide — créer les entrées manquantes, remplir uniquement les champs vides.** Rapport `Audits/FeedbackCatalog_<yyyyMMdd_HHmm>.md` (créées / complétées / intactes / clips câblés / slots sans clip).

1. Crée si besoin `Assets/_Project/Data/Feedback/FeedbackCatalog.asset` + une entrée par valeur de l'enum (40).
2. Crée si besoin `Assets/_Project/Prefabs/VFX/Feedback/FxPlaceholder.prefab` par `AssetDatabase.CopyAsset` de `Assets/_Project/Prefabs/ImpactBurst.prefab`, et le branche (si `vfxPrefab` vide) avec `TintMode.Cause` sur : `HealReceived` (Heal), `BuffApplied` (BuffUp), `DebuffApplied` (DebuffDown), `ShieldBroken` (Shield).
3. **Pré-branchement banque v0** (Go du 02/08) : scanne `Audio/SFX/Combat/**` , groupe par slot (nom sans `sfx_` ni `_<n>`), assigne `clips` (si vide) + graines par slot selon la table :

| Slot → Event | Famille | Cooldown ms | Emphase | Volume |
|---|---|---|---|---|
| `heal` → HealReceived | Statuts | 120 | 3 | 0.8 |
| `buff_up` → BuffApplied | Statuts | 120 | 3 | 0.75 |
| `debuff_down` → DebuffApplied | Statuts | 120 | 3 | 0.75 |
| `shield_gain` → ShieldGained | Statuts | 120 | 3 | 0.8 |
| `shield_hit` → ShieldAbsorbed | Statuts | 90 | 2 | 0.7 |
| `shield_break` → ShieldBroken | Statuts | 120 | **5** | 0.9 |
| `burn_apply` → BurnApplied | Statuts | 120 | 3 | 0.8 |
| `burn_tick` → BurnTick | Statuts | 120 | 1 | 0.6 |
| `poison_tick` → PoisonTick | Statuts | 120 | 1 | 0.6 |
| `stun_apply` → StunApplied | Statuts | 120 | 4 | 0.85 |
| `freeze_apply` → FreezeApplied | Statuts | 120 | 4 | 0.85 |
| `freeze_end` → FreezeEnded | Statuts | 120 | 2 | 0.7 |
| `enemy_windup` → EnemyWindup | Moments | 200 | 2 | 0.7 |
| `enemy_hit_ally` → EnemyHitAlly | **Impacts** | **70** | 4 | 0.85 |
| `turn_relay` → TurnRelay | UI | 150 | 1 | 0.35 |
| `victory_sting` → VictorySting | Moments | 1000 | **6** | 0.9 |
| `spec_switch` → SpecSwitch | UI | 150 | 2 | 0.6 |
| `summon_spawn` → SummonSpawned | Moments | 150 | 3 | 0.8 |
| `zone_place` → ZonePlaced | Statuts | 120 | 2 | 0.7 |
| `zone_cross` → ZoneCrossed | Statuts | 120 | 1 | 0.6 |

Pitch par défaut 0.96–1.04 partout. Les events sans clip v0 (BuffExpired, EnemyLaunch, Revive…) restent vides — c'est attendu.

### 9. `FeedbackCatalogAuditor` (éditeur, lecture seule, `[MenuItem("Chez Arthur/Feedback/Audit Catalogue Feedback")]`)

Rapport `Audits/` : entrée présente pour chacune des 40 valeurs ❌ sinon · clips null dans les tableaux ❌ · bornes (emphase 1–6, cooldown 0–2000, 0.5 ≤ pitchMin ≤ pitchMax ≤ 2) ❌ · **prefab VFX avec `main.loop = true` ❌** (les boucles d'état = driver F3, jamais ce pool) · override avec characterId vide ❌ · récap par groupe (A/B/C : slots avec son / avec visuel).

## CONVENTIONS

`.cursorrules` intégral : commentaires FRANÇAIS, noms ANGLAIS, bandeaux, zéro alloc/LINQ en hot path, pas de `FindObjectOfType` dans Update, éditeur sous `Scripts/Editor/` + `#if UNITY_EDITOR`. Compile sans warning.

## SÉQUENCE D'INTÉGRATION

1. Appliquer → compiler → **commit 1 (code)** : `feat(feedback): F2-P1 catalogue + service poolé + garde-fous`.
2. Exécuter le builder → vérifier le rapport → `Audit Catalogue Feedback` → **commit 2 (assets générés)** : `feat(feedback): F2-P1 catalogue asset + FxPlaceholder + rapports`. Aucune scène dans aucun des deux commits.

## CHECKLIST DE TEST (Play Mode, scène Game — SANS la committer : GO temporaire « FeedbackDev » + CombatFeedbackService + catalogue assigné, supprimé après test)

1. « Jouer tous les events » : les ~20 slots sonorisés v0 s'entendent avec variation de pitch ; les 4 events à FxPlaceholder affichent le burst teinté (vert soin, bleu buff, violet debuff, cyan bouclier).
2. Relancer la séquence : **zéro nouvel Instantiate de FX** (pool — vérifier au Profiler ou par log du pool).
3. « Spam HitEnemy ×20 » : jamais plus de 4 voix Impacts actives, cooldown visible aux compteurs (`SkippedCooldown`/`SkippedVoices` > 0).
4. Saturer Statuts (3 events statut en < 100 ms) : le 3ᵉ est skippé (emphase < 5) ; refaire avec `shield_break` (emphase 5) : il **vole** une voix Statuts.
5. Budget : demander > 12 FX rapprochés → `ActiveFxCount` plafonne à 12, `SkippedFx` > 0.
6. Event sans bundle (ex. `Revive`) : no-op + **un seul** warning console.
7. Profiler : 0 alloc GC par `Play` en spam stable (après préchauffe).
8. **Non-régression** : une run complète normale — strictement identique à avant (le système est dormant, rien ne l'appelle).
9. `git status` après tests : aucune scène modifiée.
