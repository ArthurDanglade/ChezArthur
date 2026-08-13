# Prompt Cursor — F5-L2 (v2) : origine enrichie, callouts de source, éviction par priorité, pulse, compteur

> **Chantier SFX/VFX — F5-L2, spec = `Docs/Feedback` contrat F5-L + Avenant v2 Callouts (13/08).** Go rendu : éviction par priorité OK · emphase = L2b · son proc = L2b · Bouclier pose+casse sonores = **L2b** (rien d'audio ici). HEAD : `43426db` (pull d'abord).
> **L2 = jouable muet complet : chaque proc est attribué.** Aucun changement audio, aucun asset : **1 commit code**. Staging sélectif strict.

---

## PÉRIMÈTRE

**Nouveaux** : `Gameplay/Feedback/BuffOriginScope.cs` · `Gameplay/Feedback/UnitPulse.cs` · `Gameplay/Feedback/ProcActivationCounter.cs`
**À modifier** : `Buffs/BuffData.cs` (+enum champ) · `Buffs/BuffReceiver.cs` (estampille + composition stats) · `Feedback/FeedbackContext.cs` (+2 champs) · `Feedback/CombatFeedbackService.cs` (étape callout + map priorités) · `UI/FloatingNumberSpawner.cs` (priorités/éviction) · `CharacterPassiveRuntime.cs` + `Enemies/Passives/EnemyPassiveRuntime.cs` (scope autour du dispatch) · `Roguelike/ValiseEventBridge.cs` + `Roguelike/Items/ItemEventBridge.cs` (origine sans source) · `Characters/PassiveData.cs` + `Enemies/Passives/EnemyPassiveData.cs` (+`silentProc` bool, défaut false)
**INTERDIT** : handlers (`Passives/Handlers/**` des deux camps — ils traversent), `JuiceDirector`, builder/catalogue (zéro asset), `UnitStatusFx`/pips, scènes, `SettingsPanelUI`, tout audio.

## SPÉCIFICATION

### 1 — `BuffOriginScope` (pile statique, systems-only)

```csharp
public enum BuffOrigin { None, Passif, Valise, Objet }
```
Scope empilable : `Origin`, `Transform SourceUnit` (null pour Valise/Objet), `string DisplayName`, `string PassiveId`, `bool Silent`, `int ActivationId` (compteur statique incrémental). API : `Push(...)` / `Pop()` (pile `List` bornée, réentrance OK), `Current` (sommet ou défaut None). **Toujours try/finally autour du dispatch.**

### 2 — Dispatchers (les seuls points de pose du scope)

- `CharacterPassiveRuntime.NotifyTrigger` (L135) + `NotifyTriggerWithContext` (L161) et `EnemyPassiveRuntime.NotifyTrigger` (L224) : **dans la boucle par handler**, push d'un scope avec le `PassiveName`/`Id`/`silentProc` de la **PassiveData du handler courant** + `SourceUnit` = `_visual` du porteur (repli transform), ActivationId neuf par itération ; try/finally Pop. (Chaque handler = un passif nommé — c'est lui qu'on attribue.)
- `ValiseEventBridge` / `ItemEventBridge` : push `Valise` / `Objet` **sans SourceUnit** autour de leurs dispatches d'effets (mêmes try/finally).
- **Limite documentée (commentaire en tête de BuffOriginScope)** : le scope couvre la fenêtre SYNCHRONE — un handler qui applique son effet dans une coroutine sort du scope (Origin=None, pas de callout, aucun crash). Liste des procs asynchrones constatés → au rapport de contrôle.

### 3 — `BuffData` + estampille + composition stats (`BuffReceiver`)

1. `BuffData` : `public BuffOrigin Origin;` (défaut None — aucun handler modifié).
2. `AddBuff` : `buff.Origin = BuffOriginScope.Current.Origin;` (avant l'émission feedback).
3. À l'émission Buff/Debuff (helper existant), composer `ctx.LabelOverride` + `ctx.LabelColor` :
   - Signe `+`/`−` (**transitional — flèches sprites en L2b**) + abréviation : ATK · DEF · PV (HP) · VIT (Speed) · Force (LaunchForce) · Soins± (HealReceived). Cas nommés (mot seul, sans signe) : DamageReduction+ → `Protégé` · DamageAmplification+ → `Vulnérable` · MissChance+ → `Aveuglé`.
   - Suffixe : ` (valise)` / ` (objet)` **uniquement** (Origin Passif = pas de suffixe — le callout attribue ; None = rien).
   - Couleur : `CombatFeedbackPalette.GetColor(Buff)` / `(Debuff)` selon le sens réel (Value/StatType — réutiliser la classification existante de FeedbackCauses).
   - **Cache statique** `Dictionary<(BuffStatType, bool, BuffOrigin), string>` — zéro alloc en régime.
4. `FeedbackContext` : `public string LabelOverride;` + `public Color LabelColor; public bool HasLabelColor;` (défauts nulls/false dans `At`).

### 4 — Service : étape callout + priorités

1. Étape 6 (labels) : `string text = ctx.LabelOverride ?? bundle.labelTextFr ; Color col = ctx.HasLabelColor ? ctx.LabelColor : bundle.labelColor;` — reste inchangé.
2. **Étape 7 — callout de source** (immédiatement après) : si `BuffOriginScope.Current` porte `SourceUnit != null` + `DisplayName` non vide + `!Silent` :
   - Dedup activation : `_lastCalloutActivationId` statique — un `ActivationId` déjà callouté ne rejoue pas (un proc multi-cibles/multi-events = **un** mot). Puis dedup 0,6 s existant du spawner (clé lane source).
   - Texte : `DisplayName` en **CAPITALES** — `ToUpperInvariant()` via cache `Dictionary<string,string>` (accents conservés : « REPRÉSAILLES »).
   - Couleur : **la cause de l'événement en cours** (map eventId→FeedbackCause, cf. 3.) — le lien source→cible est chromatique.
   - `ShowStateLabel(sourceId, texte, couleur, posSource, LabelPriority.Proc)` + `UnitPulse` sur la source (AddComponent paresseux sur `_visual`).
   - `ProcActivationCounter.Increment(sourceId, passiveId)`.
3. **Map priorités des labels d'état** (switch code, service) : Stun/Freeze → Control · Burn/Poison → Dot · Shield* → Shield · LabelOverride (stats) → Stat.

### 5 — Spawner : priorités & éviction (amende §4 du contrat)

`public enum LabelPriority { Control = 0, Dot = 1, Shield = 2, Proc = 3, Stat = 4 }` — `ShowStateLabel(..., LabelPriority priority)`.
- **File de lane occupée** : le candidat remplace la file seulement si `priority <` (strictement plus urgent) **ou** priorité égale (le plus récent gagne — comportement actuel). Moins prioritaire → droppé.
- **Plafond global 3** : `Control` **bypasse** le plafond (rare par nature — la garantie « les contrôles s'affichent toujours » du §4) ; les autres sont droppés à plafond plein. Jamais d'éviction d'un label **déjà affiché**.
- Dedup 0,6 s / 0,8 s / lanes / budgets chiffres : inchangés.

### 6 — `UnitPulse` (Visual only) & `ProcActivationCounter`

- `UnitPulse` : `PulseOnce()` — scale localScale ×1,0→1,08→1,0 sur ~0,25 s, timer manuel `Update` (early-out si inactif), capture/restore l'échelle de base, **jamais le Rigidbody2D**, ré-entrant (re-pulse = repart du pic). Pattern EnemyHitReaction, zéro alloc.
- `ProcActivationCounter` : statique, `Dictionary<(int, string), int>`, `Increment`, reset sur `RunManager.OnRunStarted` (abonnement posé par `CombatFeedbackService.Awake`, désabo OnDestroy). **Aucune UI** — log dev-only optionnel `[Proc] NAME ×N` throttlé. Graine gacha, consommateur futur.

### 7 — `silentProc`

`PassiveData` + `EnemyPassiveData` : `[SerializeField] private bool silentProc;` + propriété publique, tooltip « Callout F5-L2 supprimé (événement porté par une annonce T1 — Alucadra, Rupture…) ». Défaut false — **aucun asset modifié dans ce lot** (Arthur cochera les concernés en inspecteur).

## SÉQUENCE
Code → compiler → **1 commit** : `feat(feedback): F5-L2 — callouts de source (scope enrichi, pulse, compteur) + stats/origine + éviction par priorité`.

## CHECKLIST (muet — le critère : chaque action est attribuée)
1. Proc Kram (Représailles) : « REPRÉSAILLES » orange **sur Kram** + pulse du corps + « Brûlure » orange sur la cible — lien chromatique lisible. Proc multi-cibles : **un seul** callout.
2. Symétrie : un proc allié (Colère du Garde…) annonce pareil au-dessus de l'allié.
3. Stats : `+ATK` bleu · `−DEF` violet · `Vulnérable`/`Aveuglé`/`Protégé` · ` (valise)`/` (objet)` sur les sources sans unité · **plus aucun « (passif) »**.
4. Éviction : écran chargé (injecteur + fight) → contrôles toujours affichés (bypass), stats sautent en premier ; jamais un label affiché coupé en cours.
5. `silentProc` coché en inspecteur sur un passif de test → zéro callout.
6. Capitales accentuées : « É »/« Û » rendus par la police pixel — sinon le noter (fallback L2b).
7. Compteur : log dev `[Proc] ×N` cohérent ; reset entre deux runs.
8. Procs asynchrones (effet posé en coroutine) : lister ceux qui n'appellent pas de callout → rapport de contrôle.
9. Non-régression : chiffres/D12/pastilles/boucles inchangés ; profiler : zéro alloc en régime (caches chauds) ; `git status` : **aucun asset**.
