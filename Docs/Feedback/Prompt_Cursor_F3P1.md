# Prompt Cursor — F3-P1 : Émetteurs du groupe B (briques d'événements + émissions systèmes)

> **Chantier SFX/VFX — gate F3, partie 1.** Réf : charte v1.1 (§1 « un état = quatre moments », §2, §4 groupe B), `Audit_Cible_F3_Langage_Etat.md`, Go du 03/08 (avenant 8 systèmes + heal · P1→P2→P3 · classification par règles + constantes marqueurs). HEAD de référence : `8889453`.
> **Principe** : l'émission vit UNIQUEMENT dans les systèmes centraux — jamais dans les handlers (~80 poseurs de buffs, retrofit interdit). Les sons v0 étant déjà branchés sur le catalogue (F2-P1), ce gate rend les états **audibles** sans un seul visuel nouveau. Aucune scène, aucun asset, aucun builder — **un seul commit code**.
> **Constat d'audit affiné** (post-Go, conforme à la classification validée) : les DoT ennemis sont des **buffs marqueurs** (`kram_burn`, `boule_de_feu_burn`, `pusamair_poison`) — leurs pose/fin sont donc émis par `BuffReceiver` via la classification ; les tick systems n'émettent que les **ticks**.

---

## DEMANDE

Ajouter les briques d'événements par-buff à `BuffReceiver`, la classification `FeedbackCauses`, un point d'entrée statique null-safe au service, et les émissions `Play` aux sites exacts listés — comportement gameplay STRICTEMENT inchangé (le diff ne contient que des événements et des émissions).

## PÉRIMÈTRE — fichiers

**À créer :**
- `Assets/_Project/Scripts/Gameplay/Feedback/FeedbackCauses.cs`

**À modifier (zones décrites uniquement) :**
- `Assets/_Project/Scripts/Gameplay/Feedback/CombatFeedbackService.cs` (1 méthode statique)
- `Assets/_Project/Scripts/Gameplay/Buffs/BuffReceiver.cs`
- `Assets/_Project/Scripts/Gameplay/Passives/Handlers/StunSystem.cs`
- `Assets/_Project/Scripts/Gameplay/Passives/Handlers/FreezeSystem.cs`
- `Assets/_Project/Scripts/Gameplay/Passives/Handlers/AllyDotSystem.cs`
- `Assets/_Project/Scripts/Gameplay/Passives/Handlers/BurnTickSystem.cs`
- `Assets/_Project/Scripts/Gameplay/Passives/Handlers/PoisonTickSystem.cs`
- `Assets/_Project/Scripts/Enemies/EnemyShieldSystem.cs`
- `Assets/_Project/Scripts/Gameplay/CharacterBall.cs` (**zone heal UNIQUEMENT** — 1 émission)

**INTERDIT** : tous les handlers de contenu (`Passives/Handlers/**` hors les 5 systèmes listés, `Scripts/Enemies/Passives/**`), `JuiceDirector`, `FxPool`/`FeedbackCatalog`/`FeedbackBundle`/`FeedbackEventId`, `TurnManager`, `RunManager`, `BuffData.cs` (pas de flag IsDebuff — Go Q3), toute scène, tout asset. Aucune logique de gameplay modifiée (les émissions s'insèrent APRÈS les effets, jamais avant, jamais dans une condition existante).

## SPÉCIFICATION

### 1. `CombatFeedbackService` — point d'entrée statique

```csharp
/// <summary> Émission null-safe pour les systèmes de gameplay (no-op hors combat). </summary>
public static void PlayEvent(FeedbackEventId id, in FeedbackContext ctx) => Instance?.Play(id, in ctx);
```
Rien d'autre dans ce fichier.

### 2. Constantes de marqueurs — visibilité

Passer `private const` → `public const` (valeurs INCHANGÉES) : `BurnTickSystem.KramBurnBuffId`, `BurnTickSystem.BouleDeFeuBurnBuffId`, `PoisonTickSystem.PoisonBuffId`, `PoisonTickSystem.CarrierBuffId`, `FreezeSystem.FreezeBuffId`. (`StunSystem.StunBuffId` est déjà public.) Chaque système reste l'unique source de vérité de ses ids.

### 3. `FeedbackCauses` (nouveau, namespace `ChezArthur.Gameplay.Feedback`)

```csharp
public enum BuffFeedbackKind { None, Buff, Debuff, Shield, Burn, Poison }
public static class FeedbackCauses
{
    public static BuffFeedbackKind Classify(BuffData b)
}
```
Règles, dans cet ordre : `b == null` → None · BuffId ∈ { `StunSystem.StunBuffId`, `FreezeSystem.FreezeBuffId`, `PoisonTickSystem.CarrierBuffId` } → **None** (marqueurs : leurs systèmes émettent) · BuffId ∈ { `KramBurnBuffId`, `BouleDeFeuBurnBuffId` } → **Burn** · BuffId == `PoisonBuffId` → **Poison** · `StatType == Shield` → **Shield** · `StatType == MissChance || DamageAmplification` (Value > 0) → **Debuff** · sinon `Value < 0` → **Debuff**, sinon **Buff**. Zéro alloc (comparaisons de chaînes internées + switch).

### 4. `BuffReceiver` — événements par-buff + émissions

**a) Événements** (en plus d'`OnBuffsChanged`, qui ne bouge pas) :
```csharp
public enum BuffRemovalReason { Expired, Dispelled, Consumed, Replaced }
public event System.Action<BuffData> OnBuffAdded;
public event System.Action<BuffData, BuffRemovalReason> OnBuffRemoved;
```

**b) Contexte** : cacher à l'Awake `_ownerBall = GetComponent<CharacterBall>()` (null sur ennemi — voulu). Helper privé `EmitFor(BuffData b, bool applied)` : classifie, mappe, construit `FeedbackContext.At(transform.position)` + `Target = transform` + `TargetBall = _ownerBall`, puis `CombatFeedbackService.PlayEvent(...)`. Mapping :

| Kind | applied = true | applied = false (Expired seulement) |
|---|---|---|
| Buff | `BuffApplied` | `BuffExpired` |
| Debuff | `DebuffApplied` | `DebuffExpired` |
| Shield | `ShieldGained` | **silence** (expiration douce ≠ casse — charte §1.5) |
| Burn | `BurnApplied` | `BurnEnded` |
| Poison | `PoisonApplied` | `PoisonEnded` |
| None | — | — |

**c) Sites** :
- `AddBuff` : les retraits internes d'unicité (UniqueGlobal/PerSource) → `OnBuffRemoved(b, Replaced)`, **sans feedback**. Après `_activeBuffs.Add` : `OnBuffAdded(buff)` + `EmitFor(buff, applied: true)`.
- `TickTurn` et `TickCycleBuffsFromApplicator` : chaque retrait à durée épuisée → `OnBuffRemoved(b, Expired)` + `EmitFor(b, applied: false)`.
- `RemoveBuffsById` / `RemoveBuffsBySource` : `OnBuffRemoved(b, Dispelled)`, **sans feedback** en P1 (nettoyages et marqueurs ; stun/gel émettent leur propre fin).
- `AbsorbDamageWithShield` : suivre localement `bool anyBroken` — vrai quand un buff Shield est retiré **dans la boucle d'absorption** (`Value <= 0.001f`), avec `OnBuffRemoved(b, Consumed)` ; les retraits de nettoyage pré-absorption (`Value <= 0` en entrée) restent silencieux. En fin de méthode, si `LastAbsorbedByShield > 0` : émettre UNE fois — `ShieldBroken` si `anyBroken`, sinon `ShieldAbsorbed`. Jamais les deux.

### 5. Systèmes — émissions sémantiques (chaque émission APRÈS l'effet)

| Fichier · site | Émission |
|---|---|
| `StunSystem.StunEnemy` — fin de méthode (après AddBuff) | `StunApplied` @ enemy |
| `StunSystem.OnTurnChanged` — branche skip, après `RemoveStunBuff` | `StunEnded` @ enemy |
| `StunSystem.RemoveStunFromEnemy` — si un stun était réellement actif (Contains) | `StunEnded` @ enemy |
| `FreezeSystem.FreezeEnemy` — fin (après SetMovable) | `FreezeApplied` @ enemy |
| `FreezeSystem.ThawAndClearFrozenEnemy` — **avant** de nuller `_frozenEnemy`, **si `!_frozenEnemy.IsDead`** | `FreezeEnded` @ enemy (mort = silence, le kill couvre) |
| `AllyDotSystem.ApplyBurnInternal` — fin (nouvelle entrée OU refresh) | `BurnApplied` @ target |
| `AllyDotSystem.OnCycleStarted` — après TakeDamage/ShowBurn | `BurnTick` @ target |
| `AllyDotSystem.OnCycleStarted` — au RemoveAt pour `RemainingCycles <= 0` | `BurnEnded` @ target (retrait pour mort = silence) |
| `BurnTickSystem.OnTurnChanged` — après chaque tick (Kram et Boule de Feu) | `BurnTick` @ enemy |
| `PoisonTickSystem.OnTurnChanged` — après le tick | `PoisonTick` @ enemy |
| `EnemyShieldSystem.ActivateShield` — si le bouclier s'active réellement | `ShieldGained` @ owner |
| `EnemyShieldSystem.AbsorbDamage` — bouclier tombé à 0 (à côté de `NotifyShieldBroken`) | `ShieldBroken` @ owner |
| `EnemyShieldSystem.AbsorbDamage` — absorbé > 0 sans casse | `ShieldAbsorbed` @ owner |
| `EnemyShieldSystem.DamageFragment` — dégâts encaissés | `ShieldAbsorbed` @ owner ; puis si `AllFragmentsDestroyed` vient de devenir vrai → `ShieldBroken` |
| `EnemyShieldSystem` — régénérations (`EnableShieldRegen`/`RegenFragments`) | **silence** (V1 — anti-spam par cycle, réévalué au gate P3) |
| `CharacterBall.Heal` — dans le bloc `actualHeal > 0`, après les 2 invokes | `HealReceived` (ctx TargetBall = this) |
| `CharacterBall` — site « delta MaxHP » (~L1400) | **AUCUNE émission** (le son du buff couvre ; silence documenté en commentaire) |

Contexte partout : `FeedbackContext.At(position de l'unité)` + `Target` + `TargetBall` si allié. Familles/emphases/cooldowns : AUCUN paramètre dans les appels — tout vient du catalogue (`Play(id)` résout le bundle ; c'est la première mise en service du chemin haut niveau).

## CONVENTIONS

`.cursorrules` intégral : commentaires FRANÇAIS, noms ANGLAIS, bandeaux, zéro alloc en hot path (struct ctx, pas de LINQ, pas de string runtime), pas de logique conditionnelle de gameplay ajoutée. Compile sans warning.

## SÉQUENCE

1. Appliquer → compiler → **commit unique (code)** : `feat(feedback): F3-P1 émetteurs groupe B — events BuffReceiver + émissions systèmes`. Aucune scène, aucun asset, aucun builder.

## CHECKLIST DE TEST (Play Mode — les sons v0 sont la sonde)

Slots v0 audibles : heal, buff_up, debuff_down, shield_gain/hit/break, burn_apply, burn_tick, poison_tick, stun_apply, freeze_apply, freeze_end. **Attendus silencieux (pas de clip v0, no-op propre)** : buff/debuff_expired, burn_ended, poison_apply/ended, stun_ended.

1. Run normale : groupe A strictement inchangé (lancer/hit/crit/kill/défaite), zéro warning console.
2. Soin réel → son heal ; montée de PV par buff MaxHP → **pas** de son heal, son de buff seul.
3. Buff de stat posé → buff_up ; debuff → debuff_down ; refresh d'un même buff → un seul son (Replaced silencieux) ; expiration → silence propre (pas de clip) sans erreur.
4. Bouclier allié (Bouclar) : gain → clink ; encaisse → tok ; casse → verre, **jamais tok + verre sur le même coup** ; expiration par durée → silence.
5. Bouclier boss : activation → clink ; hits → tok ; rupture → verre ; fragments : dégâts → tok, destruction du dernier → verre ; régén → silence.
6. Burn ennemi (Kram / Boule de Feu) : pose → burn_apply (via BuffReceiver !) ; chaque tour → crépitement ; 2 ennemis qui brûlent le même tour → cooldown 120 ms = 1–2 sons max (voulu).
7. **Anti-double (LE test)** : stun posé → stun_apply UNIQUEMENT (aucun son de buff générique — marqueur exclu) ; gel posé → freeze_apply uniquement ; gel brisé par un allié → freeze_end + le debuff lancer du frappeur sonne en debuff_down (preuve du chemin générique).
8. Gel : dégel à la mort de l'ennemi → PAS de freeze_end (le kill couvre) ; dégel naturel au tour de Frigor → freeze_end.
9. Spam de poses simultanées → plafond Statuts (2 voix) + `SkippedVoices`/`SkippedCooldown` > 0, zéro crash.
10. Profiler : 0 alloc GC par émission en régime stable.
11. `git status` : aucune scène, aucun asset — un seul commit code.
