# Prompt Cursor — F2-P2a : Routage d'exécution du juice + hit-react allié

> **Chantier SFX/VFX — gate F2, partie 2a.** Réf : plan §F2-P2 **validé** (Go 03/08 : découpage P2a→P2b · plafonds groupe A dès P2a · hit-react seuil 5 / CD 100 ms / squash neutre). HEAD de référence : `f71efb6`.
> **Principe iso-ressenti** : le JuiceDirector garde toutes ses courbes et valeurs AU CHIFFRE PRÈS — on ne change que le CANAL d'exécution (sons → voix guidées, bursts → pool). **Filet de sécurité** : les nouvelles API sont statiques avec repli legacy — si le service n'est pas en scène (avant le commit scène), le comportement est strictement l'actuel.

---

## DEMANDE

Router les sons et bursts du JuiceDirector par les garde-fous du service (sans déplacer sa data — c'est P2b), corriger les 2 notes du contrôle F2-P1, ajouter la réaction corporelle de l'allié qui encaisse, livrer le builder de scène.

## PÉRIMÈTRE — fichiers

**À modifier :**
- `Assets/_Project/Scripts/Gameplay/Feedback/CombatFeedbackService.cs`
- `Assets/_Project/Scripts/Gameplay/JuiceDirector.cs` (UNIQUEMENT les sites d'exécution listés en §3)
- `Assets/_Project/Scripts/Gameplay/CharacterBallFloat.cs` (ajout d'un état « flinch »)
- `Assets/_Project/Scripts/Gameplay/CharacterBallFactory.cs` (1 insertion)

**À créer :**
- `Assets/_Project/Scripts/Gameplay/AllyHitReaction.cs`
- `Assets/_Project/Scripts/Editor/FeedbackSceneBuilder.cs`

**INTERDIT** : `CharacterBall.cs`, `Enemy.cs` (G6c actif), `SfxPlayer`/`SfxManager`/`AudioManager`/`AudioBuses`, `FxPool`/`PooledFxReturner`/`FeedbackCatalog`/`FeedbackBundle`, tout prefab, toute scène (le builder sera exécuté à part), zones gelées G6. Aucun renommage d'API existante.

## SPÉCIFICATION

### 1. `CombatFeedbackService` — corrections F2-P1 + API guidées statiques

**a) Garde anti-doublon** (note F2-P1) : `Awake` suit le pattern maison — `if (Instance != null && Instance != this) { Destroy(gameObject); return; }`.

**b) Fix scale VFX** (note F2-P1) : dans `SpawnVfx`, remplacer `t.localScale = Vector3.one * bundle.vfxScale` par `restScale × bundle.vfxScale`, où `restScale` vient du `PooledFxReturner` de l'instance (`GetComponent`, null-safe → repli `Vector3.one`).

**c) Refactor interne** : extraire l'acquisition de voix de `TryPlaySfx` en `private bool TryAcquireVoice(FeedbackBundle.VoiceFamily family, int emphasis, float durationEstimate)` (trouve un slot libre, sinon skip < 5 / vol du plus ancien ≥ 5, écrit `now + durationEstimate`, incrémente `SkippedVoices` sur refus). `TryPlaySfx` la consomme — comportement inchangé.

**d) Nouvelles API statiques** (les seules que JuiceDirector appellera) :
```csharp
/// <summary> Joue un one-shot sous plafonds de familles. Repli legacy (SfxPlayer direct) si aucun service en scène. </summary>
public static bool PlaySfxGuarded(FeedbackBundle.VoiceFamily family, AudioClip clip, float volume, float pitch, int emphasis)

/// <summary> Spawn un burst via le pool (budget FX). Repli legacy (Instantiate, stopAction du prefab conservé) si aucun service. play=false : l'appelant configure puis appelle Play(). </summary>
public static ParticleSystem SpawnFxGuarded(ParticleSystem prefab, Vector2 pos, Quaternion rot, float scaleMul, int emphasis, bool play = true)
```
- Chemin service : `PlaySfxGuarded` → `TryAcquireVoice(family, emphasis, clip.length / pitch)` puis `SfxPlayer.Instance.Play(clip, volume, pitch)` (false si voix refusée, compteur). `SpawnFxGuarded` → refus si `ActiveCount >= 12 && emphasis < 5` (`SkippedFx++`, null) ; sinon pool `Get`, position/rotation, `localScale = restScale × scaleMul`, `Play` si demandé.
- **Chemin legacy (Instance == null)** : `PlaySfxGuarded` → `SfxPlayer.Instance?.Play(clip, volume, pitch)`, return true ; `SpawnFxGuarded` → `Object.Instantiate(prefab, pos, rot)`, `localScale = prefab.localScale × scaleMul` (le prefab conserve son propre stopAction — ne pas y toucher), `Play` si demandé, return l'instance. **Strictement le comportement actuel.**

### 2. `CharacterBallFloat` — état « flinch » composé (pas de guerre de LateUpdate)

- Nouveau : `public void TriggerHitFlinch(float intensity01)` → `_flinchTimer = FLINCH_DURATION (const 0.15f)`, `_flinchIntensity = Mathf.Clamp01(intensity01)`.
- Dans la chaîne de priorité de `LateUpdate` : `arming > superCharge > launchStretch > **flinch** > float > lerp`. `ApplyFlinch()` : `k = (timer/duration) × intensity` ; `_visual.localScale = _visualBaseScale * (1f - 0.22f * k)` ; `localPosition = _visualBasePos` (squash neutre, sans knockback — la direction arrive en F4) ; timer en `Time.deltaTime` ; restauration base à 0.
- Aucun autre changement dans ce fichier.

### 3. `JuiceDirector` — routage site par site (valeurs/courbes INCHANGÉES)

Remplacer chaque `SfxPlayer.Instance.Play(...)` / `Instantiate(...)` par l'API statique, avec ce mapping exact (famille, emphase) :

| Site (méthode actuelle) | Appel routé |
|---|---|
| `PlayHitSfx` — hit normal | `PlaySfxGuarded(Impacts, clip, volume, pitch, 4)` |
| `PlayHitSfx` — crit | `PlaySfxGuarded(Impacts, _critClip, volume, pitch, 5)` |
| `PlayBounceWall` | `PlaySfxGuarded(Impacts, clip, volume, pitch, 2)` |
| `PlayKill` — killClip OU fallback hit grave | `PlaySfxGuarded(Impacts, clip, vol, pitch, 5)` |
| `PlayLaunchNormal` — swoosh | `PlaySfxGuarded(Moments, _launchClip, _launchVolume, pitch, 3)` |
| `SuperLaunchSequence` — riser de charge | `PlaySfxGuarded(Moments, _superChargeClip, _superChargeVolume, syncPitch, 5)` |
| `SuperLaunchSequence` — détonation (super OU launch) | `PlaySfxGuarded(Moments, clip, vol, pitch, 5)` |
| `SuperLaunchSequence` — couche sub | `PlaySfxGuarded(Moments, _superDetonationLayerClip, vol, 1f, 5)` |
| `UpdateAimTension` — tick d'entrée en zone | `PlaySfxGuarded(UI, _zoneEnterTickClip, vol, pitch, 2)` |
| `DefeatBeatRoutine` — stamp | `PlaySfxGuarded(Moments, _defeatStampClip, vol, 1f, 6)` |
| `SpawnImpactBurst` — `Instantiate` | `SpawnFxGuarded(_impactBurstPrefab, pos, rot, 1f, isCrit ? 5 : 4, play: false)` → si non-null : mutation couleur + burst count comme aujourd'hui, puis `ps.Play()` |
| `SpawnLaunchBurst` — `Instantiate` | `SpawnFxGuarded(_launchBurstPrefab, position, rot, scale, isSuper ? 5 : 3)` (le calcul de `scale` actuel devient `scaleMul` — supprimer le `transform.localScale` manuel) |
| `PlayKill` — death burst | `SpawnFxGuarded(_deathBurstPrefab, position, Quaternion.identity, 1f, 5)` |

**NE PAS TOUCHER** : la boucle de tension de visée (`GetTensionSource`/`BeginAimTension`/`UpdateAimTension` hors tick), le duck snapshots, tout le reste du fichier (hitstop, trauma, finisher, defeat, escalade — mêmes formules, mêmes champs). `using ChezArthur.Gameplay.Feedback;` en tête.

### 4. `AllyHitReaction` (nouveau — `Scripts/Gameplay/AllyHitReaction.cs`, namespace `ChezArthur.Gameplay`)

Portage d'`EnemyHitReaction` côté défense, **sans wind-up et sans scale direct** (le squash passe par le Float pour éviter deux écrivains sur le même transform) :
- Constantes : `MIN_DAMAGE_TO_REACT = 5`, `COOLDOWN_SECONDS = 0.1f`, `FLASH_DURATION = 0.08f`.
- Matériau : `private static Material s_flashMaterial` — créé paresseusement de `Shader.Find("ChezArthur/SpriteFlash")` (inclus au build par les ennemis), partagé par tous les alliés. `Initialize(CharacterBall ball)` : cache `_ball`, `_float` (`GetComponent<CharacterBallFloat>`), s'abonne à `_ball.OnDamaged` (désabonnement en `OnDestroy`), et bascule `_ball.VisualRenderer.sharedMaterial` vers `s_flashMaterial` si ce n'est pas déjà un SpriteFlash (rendu identique à `_FlashAmount = 0` : le shader multiplie la couleur vertex).
- `OnDamagedHandler(int amount)` : ignore si `amount < MIN_DAMAGE_TO_REACT` ou cooldown actif (`Time.unscaledTime`) ; sinon `_flashTimer = FLASH_DURATION` + `_float?.TriggerHitFlinch(1f)`.
- `LateUpdate` : décroissance du flash via `MaterialPropertyBlock` (`_FlashAmount`, `PropertyToID` statique, MPB réutilisé — zéro alloc), early-out si timer ≤ 0. Renderer re-résolu paresseusement si null (swap de spé).

### 5. `CharacterBallFactory` — 1 insertion

Après `ball.RefreshCombatVisual();` : `ball.gameObject.AddComponent<AllyHitReaction>().Initialize(ball);` (commentaire : « Réaction corporelle défense — F2-P2a »).

### 6. `FeedbackSceneBuilder` (éditeur, idempotent, Undo-safe)

`[MenuItem("Chez Arthur/Feedback/Câbler Feedback Scène Combat")]` : dans la scène active — trouve ou crée le GO racine `CombatFeedbackService` ; ajoute le composant s'il manque ; assigne par `SerializedObject` : `_catalog` ← `AssetDatabase.LoadAssetAtPath<FeedbackCatalog>("Assets/_Project/Data/Feedback/FeedbackCatalog.asset")`, `_cameraShake` ← `FindObjectOfType<CameraShake>()` (warning si introuvable) ; `Undo` + `MarkSceneDirty` ; re-exécution = « déjà câblé ». ⚠️ **Ne PAS exécuter dans cette session Cursor** — exécution + commit `Game.unity` séparés (protocole G6).

## CONVENTIONS

`.cursorrules` intégral (commentaires FR, noms EN, bandeaux, zéro alloc en hot path, éditeur sous `Scripts/Editor/`). Compile sans warning.

## SÉQUENCE

1. Appliquer → **commit 1 (code)** : `feat(feedback): F2-P2a routage guidé du juice + hit-react allié + fixes F2-P1`. À ce stade le jeu tourne en repli legacy = comportement strictement actuel.
2. Builder scène → **commit 2 (scène)** : `feat(feedback): F2-P2a service en scène Game`.

## CHECKLIST DE TEST

1. **Avant commit scène** : une run normale — rigoureusement identique (replis legacy actifs, aucun service en scène).
2. Après builder : A/B jeu normal **indistinguable** — lancer, Super complet (riser calé sur le gel, détonation 2 couches), échelle de pitch des rebonds, hit/crit, kill, finisher, défaite, duck de visée.
3. Hits/kills en série : plus aucun `Instantiate` de burst après préchauffe (Profiler ou log pool) ; `ActiveFxCount` monte et redescend.
4. Allié encaissant ≥ 5 dégâts : flash blanc bref + squash ; dégât de contact 1 PV : **rien** ; coups rapprochés : flashs espacés ≥ 100 ms.
5. Drag ennemi/allié à fort spam : jamais plus de 4 voix Impacts simultanées, escalade de pitch conservée.
6. Repos allié strictement identique après swap de matériau (teinte, ombre, Shado invisible, ghost — `_FlashAmount` 0).
7. Slider SFX : agit toujours sur tout.
8. `git status` : la scène n'apparaît que dans le commit 2.
