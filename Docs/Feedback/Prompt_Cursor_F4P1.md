# Prompt Cursor — F4-P1 : émetteurs groupe C + rail pastilles boss

> **Chantier SFX/VFX — gate F4-P1.** Go du 04/08 (phasage + périmètre actés, crit dramatique = P2, ZoneCrossed différé, slots sans clip = silencieux). HEAD de référence : `1151ae0`.
> Branche les **11 émetteurs du groupe C** (événements 27–36 + 38–39, hors `ZoneCrossed` 37 et hors crit dramatique) sur `CombatFeedbackService.PlayEvent`, ajoute les seeds manquants au builder catalogue, porte le rail de pastilles sur la barre boss. **Émission systèmes-only : zéro ligne dans les handlers de contenu** — tous les sites ci-dessous sont des funnels qu'ils traversent déjà. Iso-ressenti partout ailleurs. **2 commits** (code / asset régénéré).

---

## PÉRIMÈTRE — fichiers

**À modifier (code, commit 1) :**
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackContext.cs` (+1 champ `DurationHint`)
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackBundle.cs` (+1 champ `fitPitchToDuration`)
- `Assets/_Project/Scripts/Gameplay/Feedback/CombatFeedbackService.cs` (pitch calé durée dans `TryPlaySfx` uniquement)
- `Assets/_Project/Scripts/Enemies/Enemy.cs` (3 sites : `PlayWindup`, `Launch`, `OnCollisionEnter2D`)
- `Assets/_Project/Scripts/Gameplay/TurnManager.cs` (relais + tour bonus)
- `Assets/_Project/Scripts/Gameplay/CombatManager.cs` (mort de boss)
- `Assets/_Project/Scripts/Gameplay/CharacterBall.cs` (site unique : `Revive(float)`)
- `Assets/_Project/Scripts/Enemies/MidCombatSpawner.cs` (2 sites : `SpawnEnemy`, `SpawnCompanion`)
- `Assets/_Project/Scripts/Gameplay/GroundZoneSystem.cs` (`CreateZoneInternal`)
- `Assets/_Project/Scripts/UI/SpecSwitchBannerUI.cs` (`Show`)
- `Assets/_Project/Scripts/UI/DefeatUI.cs` (`PlayDefeatSequence`, branche victoire)
- `Assets/_Project/Scripts/UI/BossHPBarUI.cs` (rail pastilles, portage du pattern EnemyHPBar)
- `Assets/_Project/Scripts/Editor/FeedbackCatalogBuilder.cs` (5 seeds + 3 champs de seed)

**Asset régénéré au menu (commit 2)** : `Assets/_Project/Data/Feedback/FeedbackCatalog.asset` — menu `Chez Arthur/Feedback/Créer ou Mettre à Jour le Catalogue`. **Mise à jour EN PLACE, GUID intact.**

**INTERDIT** : handlers de contenu (`Enemies/Passives/Handlers/**`, `Gameplay/Passives/Handlers/**`, `Roguelike/**` — ils traversent les funnels), `JuiceDirector`, `EnemyHitReaction`/`AllyHitReaction` (visuels inchangés), `FeedbackVfxBuilder`, prefabs VFX, toute scène, `ZoneCrossed` (différé G3), consommation de la colonne `haptic` (P2), presets audio. Ajouter `using ChezArthur.Gameplay.Feedback;` où nécessaire (leçon `0c304c9`).

## SPÉCIFICATION

### Bloc 1 — Pitch calé sur la durée (wind-up, gabarit riser Super)

1. `FeedbackContext` : champ `public float DurationHint;` (0 = aucun). `At(pos)` l'initialise à 0.
2. `FeedbackBundle` : champ `public bool fitPitchToDuration = false;` à côté de `pitchMin`/`pitchMax` (L69–70), tooltip « Pitch = longueur du clip / DurationHint du contexte (riser calé) ».
3. `CombatFeedbackService` : `TryPlaySfx(FeedbackBundle bundle)` → `TryPlaySfx(FeedbackBundle bundle, float durationHint)` (appel dans `Play` : `TryPlaySfx(bundle, ctx.DurationHint)`). Dans la méthode, après `PickClip` :
```csharp
float pitch;
if (bundle.fitPitchToDuration && durationHint > 0.05f)
    pitch = Mathf.Clamp(clip.length / durationHint, 0.5f, 2f);   // le clip épouse la durée du wind-up
else
{
    pitch = Random.Range(bundle.pitchMin, bundle.pitchMax);
    if (pitch < 0.01f) pitch = 1f;
}
```
L'estimation de durée de voix (`clip.length / pitch`) et le reste sont inchangés.

### Bloc 2 — Enemy.cs (3 sites)

1. **`PlayWindup(float duration)` (L890)** — après l'appel `_hitReaction?.PlayWindup(duration)` (émettre même si `_hitReaction` est null — le son est le message) :
```csharp
FeedbackContext ctx = FeedbackContext.At(transform.position);
ctx.DurationHint = duration;
CombatFeedbackService.PlayEvent(FeedbackEventId.EnemyWindup, in ctx);
```
Couvre EnemyAI **et** les télégraphes des handlers (Alucadra, Veuve) sans les toucher.
2. **`Launch(Vector2 direction, float force)` (L694)** — après le `AddForce` : `PlayEvent(EnemyLaunch, ctx)` avec `ctx.Position = transform.position`, `ctx.Direction = dir`. (Bundle sans clip en v0 → silencieux propre, l'émetteur est en place pour les courses.)
3. **`OnCollisionEnter2D`** :
   - **Branche allié existante** — juste après `actualTarget.TakeDamage(damage);` (avant les appels ValiseEventBridge) :
```csharp
if (actualTarget.LastDamageReceived > 0)   // full-absorb bouclier = « tok » seul (charte §2), jamais le thud
{
    FeedbackContext hitCtx = FeedbackContext.At(collision.GetContact(0).point);
    hitCtx.Direction = _rb != null ? (Vector2)_rb.velocity.normalized : Vector2.zero;
    hitCtx.TargetBall = actualTarget;      // micro-hitstop du bundle sur l'allié touché
    CombatFeedbackService.PlayEvent(FeedbackEventId.EnemyHitAlly, in hitCtx);
}
```
   - **Nouvelle branche mur** — `else` de la branche allié : si `collision.gameObject.GetComponent<Enemy>() == null` (contact ennemi-ennemi silencieux) **et** `_hasBeenLaunched && !_hasStoppedForThisLaunch` **et** `collision.relativeVelocity.magnitude >= 2.5f` → `PlayEvent(EnemyWallBounce, At(collision.GetContact(0).point))`. Pas d'autre logique (pas de decay, pas de compteur) — le cooldown du bundle (120 ms) gère le spam.

### Bloc 3 — TurnManager (relais + tour bonus)

1. Champ privé `bool _relayArmed;`. Il passe à `false` à chaque (re)construction/reset de la séquence de participants (même méthode qui remet `_sequenceIndex`/`_ghostOverrideEntry` à zéro) — **le premier tour d'un étage est silencieux**.
2. Dans `NextTurn`, juste après `OnTurnChanged?.Invoke(CurrentParticipant)` (L299) :
```csharp
if (_relayArmed && _activeGhostAlly == null && CurrentParticipant != null)
{
    // Tick feutré de relais — jamais au premier tour d'une séquence, jamais pendant l'interlude fantôme.
    CombatFeedbackService.PlayEvent(FeedbackEventId.TurnRelay,
        FeedbackContext.At(CurrentParticipant.Position));
}
_relayArmed = true;
```
(Si `ITurnParticipant` n'expose pas de position : `Vector2.zero` — l'événement est UI, sans VFX.)
3. Les **2 autres** `OnTurnChanged?.Invoke` de `HandleParticipantStopped` (entrée d'interlude fantôme L909, rejeu L935) : **pas de relais**.
4. Branche tour bonus (L935, `ally.ConsumeQueuedExtraTurn()` vrai) — à la place du relais : `PlayEvent(ExtraTurn, At(position de l'ally))`.

### Bloc 4 — Moments (1 ligne d'émission par site)

| Site | Émission |
|---|---|
| `CombatManager.HandleEnemyDeath(Enemy enemy)` (L219), après `OnEnemyDeath?.Invoke` | Si `enemy.Data != null` et (`EnemyType == Boss/MiniBoss` **ou** `EnemyRole == Boss/MiniBoss` — gabarit MasseLourdeHandler L34–35) → `PlayEvent(BossDefeated, At(enemy.transform.position))`. Accent **en plus** du Kill existant, jamais à sa place. |
| `CharacterBall.Revive(float hpPercent)` (L1268), après `RestoreVisuals()` | `PlayEvent(Revive, ctx)` avec `ctx.Position = transform.position`, `ctx.TargetBall = this`. |
| `MidCombatSpawner.SpawnEnemy` (L94) **et** `SpawnCompanion` (L129), après le `AddComponent<UnitStatusFx>().Initialize()` | `PlayEvent(SummonSpawned, At(spawnPos))`. |
| `GroundZoneSystem.CreateZoneInternal`, une fois la zone initialisée | `PlayEvent(ZonePlaced, At(worldPosition))`. Son seul — la zone est son propre visuel. |
| `SpecSwitchBannerUI.Show(specName, role)` (L68), en entrée | `PlayEvent(SpecSwitch, At(Vector2.zero))`. |
| `DefeatUI.PlayDefeatSequence` (L168), dans la branche victoire (là où la défaite joue `PlayDefeatBeat`, symétrique) | `if (_lastRunWasVictory) PlayEvent(VictorySting, At(Vector2.zero));` — fin de **run** uniquement (jamais au stage clear — `CheckVictory` tire à chaque étage, ne rien y mettre), après le gate cérémonies d'éveil. Duck/reprise musique Hub = P2. |

### Bloc 5 — Rail pastilles boss (pt n°12, Go V4)

`BossHPBarUI` : portage à l'identique du pattern `EnemyHPBar` (L22/49–77) — champ privé `StatusPipsRail _pipsRail`, `EnsurePipsRail()` (création runtime `new GameObject("StatusPipsRail")` + `AddComponent`, ancré dans le conteneur de la barre boss, offset adapté à sa largeur), bind défensif : dans `Show(Enemy enemy)` (L103) → `UnbindStatus()` puis bind sur le `UnitStatusFx` de l'ennemi ; dans `Hide()` (L145) → `UnbindStatus()`. **Aucune scène, aucun prefab** — tout runtime, comme EnemyHPBar. Ne pas toucher `StatusPipsRail.cs`.

### Bloc 6 — Builder catalogue (seeds)

`FeedbackCatalogBuilder` :
1. Struct `Seed` : +3 champs optionnels `public bool FitPitchToDuration;` `public float ShakeTrauma;` `public int HitstopMs;` (défauts 0/false).
2. Seed existant `enemy_windup` : `FitPitchToDuration = true`. Seed existant `enemy_hit_ally` : `ShakeTrauma = 0.12f, HitstopMs = 50` (micro-hitstop + shake léger du bundle défense — le service les consomme déjà).
3. **5 nouveaux seeds** (bundles sans clip tant que les courses ne sont pas livrées — le service est silencieux proprement, prouvé `BurnEnded`) :
```csharp
new Seed { Slot = "enemy_launch",      EventId = FeedbackEventId.EnemyLaunch,     Family = Moments, CooldownMs = 150,  Emphasis = 2, Volume = 0.7f },
new Seed { Slot = "enemy_wall_bounce", EventId = FeedbackEventId.EnemyWallBounce, Family = Impacts, CooldownMs = 120,  Emphasis = 1, Volume = 0.4f },
new Seed { Slot = "boss_defeated",     EventId = FeedbackEventId.BossDefeated,    Family = Moments, CooldownMs = 1000, Emphasis = 6, Volume = 0.9f },
new Seed { Slot = "revive",            EventId = FeedbackEventId.Revive,          Family = Moments, CooldownMs = 300,  Emphasis = 4, Volume = 0.85f },
new Seed { Slot = "extra_turn",        EventId = FeedbackEventId.ExtraTurn,       Family = UI,      CooldownMs = 150,  Emphasis = 2, Volume = 0.6f },
```
4. La boucle de build transfère les 3 nouveaux champs vers le bundle. **Mise à jour en place** : les champs non pilotés par seed (vfxPrefab câblé au menu 2, etc.) restent intacts — vérifier au diff de l'asset que seules les entrées seedées bougent, et uniquement sur les champs seedés.

## SÉQUENCE

1. Appliquer → compiler → **commit 1 (code)** : `feat(feedback): F4-P1 — émetteurs groupe C (11 sites systèmes-only) + pitch calé wind-up + rail pastilles boss`.
2. Menu `Chez Arthur/Feedback/Créer ou Mettre à Jour le Catalogue` → **commit 2 (asset)** : `feat(feedback): F4-P1 — catalogue groupe C (5 seeds, windup riser, bundle défense)`. **Vérifier au diff : asset modifié EN PLACE (.meta/GUID intacts), seules les entrées seedées changent.**

## CHECKLIST DE TEST

1. **Wind-up** : inspiration grave audible avant **chaque** lancer ennemi, y compris télégraphes spéciaux (Alucadra, Veuve) ; la durée du son épouse le wind-up (0,25 s vs 0,35 s = pitchs différents, audible).
2. **Impact ennemi→allié** : thud sourd + micro-gel de l'allié touché + shake léger — on distingue à l'oreille qui frappe qui. Full-absorb bouclier : **tok seul, pas de thud** (poser un bouclier via l'injecteur, encaisser un coup faible).
3. **Rebond mur ennemi** : événement émis (compteurs DEV du service) — silencieux tant que pas de clip ; aucun tick sur contact ennemi-ennemi ni sur un ennemi à l'arrêt.
4. **Relais** : tick feutré à chaque passage de tour ; **silencieux** au premier tour de chaque étage, pendant l'interlude fantôme, et sur un rejeu de tour bonus (ExtraTurn émis à la place, silencieux clipless).
5. **Invocation** : son de spawn sur les invocations ET compagnons mid-combat ; rien au spawn d'étage (StageGenerator hors périmètre).
6. **Moments** : bannière de switch = son ; zone posée (Archère/Patriarche) = son ; boss tué = event émis (+ Kill inchangé) ; revive (Ticket Offert / debug) = event émis ; **victoire de fin de run = sting** (après cérémonies d'éveil éventuelles), stage clear intermédiaire = finisher inchangé sans sting, défaite = beat inchangé.
7. **Rail boss** : pastilles d'états visibles sur la barre boss (injecteur ÉTATS sur un boss), unbind propre au Hide — pas de pastilles fantômes au boss suivant.
8. **Non-régression** : run complète — groupe A/B identiques, aucun warning `Pas de bundle` en console, compteurs service sans skip anormal, profiler : zéro alloc récurrente sur les nouveaux chemins (structs `in`, pas de LINQ).
9. `git diff` commit 2 : catalogue seul, en place, GUID intact ; entrées non seedées strictement inchangées.
