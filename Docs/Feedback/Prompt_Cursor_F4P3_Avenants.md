# Prompt Cursor — F4-P3 avenants de clôture : haptique · duck · relais · présence ennemie · hygiène banque · heal utile · anti-spam buff

> **Chantier SFX/VFX — avenants verdicts P3.** Go du 05/08. HEAD à vérifier au démarrage (`git pull`).
> 7 blocs chirurgicaux. **2 commits** : code (+ manifest) / banque + catalogue régénéré. **Staging sélectif strict.**
> **AVANT le run du menu catalogue — étapes banque MANUELLES (Arthur)** : voir section BANQUE en bas.

---

## PÉRIMÈTRE

**Nouveau** : `Assets/Plugins/Android/AndroidManifest.xml`
**À modifier** : `HapticManager.cs` (logs catch) · `CombatFeedbackService.cs` (duck ~2 l.) · `TurnManager.cs` (armement relais ~4 l.) · `CharacterBall.cs` (gate Heal ~3 l.) · `BuffReceiver.cs` (gate émission buff) · `FeedbackCatalogBuilder.cs` (volumes + mute shield + re-câblage morts + seed poison_apply + cd buff)
**INTERDIT** : handlers, `SettingsPanelUI`, scènes, `JuiceDirector`, prefabs VFX, `UnitStatusFx`/pips (les pastilles ne bougent pas).

## SPÉCIFICATION

### A — Haptique : permission manquante (cause racine du KO device)
1. Créer `Assets/Plugins/Android/AndroidManifest.xml` minimal mergeable :
```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
  <uses-permission android:name="android.permission.VIBRATE" />
  <application />
</manifest>
```
(Unity n'injecte VIBRATE que s'il voit `Handheld.Vibrate` — notre chemin AndroidJavaObject ne le déclenche pas.)
2. `HapticManager` : dans chacun des 3 `catch` → log dev-only (`#if UNITY_EDITOR || DEVELOPMENT_BUILD` : `Debug.LogWarning("[Haptic] " + e.Message)`), prod reste silencieux. Plus jamais de KO muet.

### B — Duck statut→impact raffiné (verdict n°21)
`CombatFeedbackService.Play`, bloc `_statusSfxFrame` : n'enregistrer le frame **que si `bundle.emphasis >= 3`** (les ticks DoT emph 1–2 ne duckent plus les hits ; les poses d'état continuent). Commentaire : « verdict P3 n°21 — seuls les statuts marquants duckent l'impact ».

### C — Relais : tick au passage de cycle (verdict n°18)
`TurnManager` : retirer `_relayArmed = false;` de `RebuildCycleSequence()`. Le poser **uniquement** dans les chemins d'installation : la méthode d'enregistrement des ennemis (zone L199), `ResetTurnOrder()` (L352), la méthode d'enregistrement des alliés (L525). Les rebuilds lazy/fin de cycle (L797/L811) n'avalent plus de tick. Le premier tour d'un étage reste silencieux.

### D — Présence ennemie + DoT audibles (builder, data)
Seeds mis à jour : `enemy_windup` vol 0.7→**0.9**, emph 2→**3** · `enemy_hit_ally` vol 0.85→**1.0** · `turn_relay` vol 0.35→**0.5** · `burn_tick` vol 0.6→**0.8**, emph 1→**2** · `poison_tick` vol 0.6→**0.8**, emph 1→**2** · `buff_up` cd 120→**300**.
**Ces valeurs doivent s'appliquer aux entrées DÉJÀ clipées** → étendre le bloc de sync-avant-continue : quand un seed est marqué `ForceTuning = true` (nouveau champ bool du Seed, posé sur ces 6 seeds), écrire family/cd/emphasis/volume même si `HasSfx`. Les autres seeds gardent l'idempotence actuelle.

### E — Builder : hygiène banque, mute shield, re-câblage
1. **Re-câblage des morts** : dans la boucle, si un slot seedé a des clips dont une référence est nulle/manquante (fichier supprimé/remplacé), **vider et re-câbler depuis le scan** même si non-vide. (Couvre les remplacements de fichiers heal/buff.)
2. **Slots mutés** : liste `MutedSlots = { "shield_gain", "shield_hit", "shield_break" }` → clips **vidés**, jamais re-câblés par le scan. Commentaire : « verdict P3 : bouclier = visuel seul (arc/pulse/éclats + pastille conservés) ». Le haptic `shield_break` (Medium) est **conservé** — c'est du tactile, pas du SFX ; on tranchera au re-test.
3. **Nouveau seed** : `{ Slot = "poison_apply", EventId = PoisonApplied, Family = Statuts, CooldownMs = 120, Emphasis = 3, Volume = 0.8f }` — se câblera quand `sfx_poison_apply_1` arrivera (courses) ; silencieux propre d'ici là.
4. Rapport `Audits/` : lister slots mutés, re-câblés, sans clip.

### F — Heal utile seulement (verdict interview : « full vie = pollution »)
`CharacterBall.Heal(int, CharacterBall)` : l'émission `HealReceived` (son + VFX, l'event entier) ne part que si
```csharp
actualHeal > 0 && previousHp < Mathf.CeilToInt(EffectiveMaxHp * HealFeedbackMaxFraction) // const 0.98f
```
Commentaire : « charte §1.5 — silence sur le non-joueur : lifesteal/regen à (quasi) pleine vie ne produit rien ». Couvre inter-étages, vol de vie passif, full-vie. Le soin CHIFFRÉ (popup dégâts/soin existant) ne change pas — seul le feedback d'état est gaté.

### G — Anti-spam buff (verdict interview : méthode à ma main)
`BuffReceiver.AddBuff` :
1. Capturer `bool replacedSameId` dans les boucles UniqueGlobal / UniquePerSource (L73–96) — true si au moins un retrait `Replaced` du même `BuffId` dans CE call.
2. Déterminer `bool isPermanent` selon la **convention réelle** du champ de durée de `BuffData` (lire le fichier — durée en tours ; permanent = la valeur sentinelle existante. NE RIEN INVENTER : utiliser la même convention que le tick de fin de tour).
3. Skip **l'émission feedback complète** (BuffApplied/DebuffApplied : son + one-shot chevrons) si `replacedSameId || isPermanent`. Les pastilles/driver (`OnBuffAdded`) et le gameplay ne changent PAS — seul l'appel `PlayEvent` est gaté. Commentaire : « verdict P3 — un refresh ou une aura permanente n'est pas une décision du joueur (§1.5) ; le buff temporaire actif garde son feedback ».
Justification (consignée) : coupe la rafale d'installation (auras permanentes posées en masse au spawn) et les refreshs périodiques SANS toucher le cas voulu — buff cliqué/déclenché en combat = temporaire nouveau → feedback plein. Le cd 300 ms (bloc D) regroupe les multi-poses simultanées restantes. L'option « emphase ↓ » est rejetée : elle rendrait le feedback aléatoire (vol de voix) au lieu de sélectif.

## BANQUE — étapes manuelles Arthur AVANT le menu catalogue
1. `etats/` : remplacer le contenu de `sfx_heal_1` par **healingsoundfinal** (garder le nom-préfixe `sfx_heal_1.*` — l'extension peut changer) ; **supprimer** `sfx_heal_2/3` + metas. Poser **buffsoundfinal** en `sfx_buff_up_1.mp3` (supprimer l'ancien .wav + meta).
2. **Supprimer les 6 wav shield** (`sfx_shield_gain/hit/break_1..2`) + metas.
3. Burn (`sfx_burn_apply_1.mp3`) et freeze : déjà 1 clip — rien à faire.
4. Courses à venir (non bloquant) : `sfx_poison_apply_1` · pack ennemi qualité (`sfx_enemy_windup`, `sfx_enemy_launch`, `sfx_enemy_hit_ally`).
5. Puis menu `Créer ou Mettre à Jour le Catalogue` → vérifier au rapport : re-câblés heal/buff, mutés ×3, poison_apply sans clip.

## SÉQUENCE
1. Code + manifest → compiler → **commit 1** : `fix(feedback): avenants P3 — permission VIBRATE + duck emph≥3 + relais cycle + heal utile + anti-spam buff + builder tuning`.
2. Étapes banque + menu → **commit 2 (assets)** : `chore(feedback): avenants P3 — banque 1-clip (heal/buff finals), shield muet, volumes ennemis/DoT, catalogue régénéré`. Diff : catalogue EN PLACE (GUID intact), banque = remplacements listés, rien d'autre.

## CHECKLIST
1. **Device** : crit/kill/ram ennemi → vibrations par niveau ; `haptics_enabled=0` → zéro ; éditeur → aucun log d'erreur.
2. Injecteur : poison pose = silencieux propre (pas de warning) tant que le clip manque ; burn pose audible ; en fight : burn/poison **ticks audibles**.
3. Hits pendant DoT : plus de duck (tick emph 2 < 3) ; pose de gel/stun pendant un hit : duck toujours là.
4. Relais : tick au passage de cycle ET après un kill/spawn ; premier tour d'étage silencieux.
5. Heal : lifesteal/inter-étages à pleine vie = **rien** ; vraie remontée (< 98 %) = son + VFX.
6. Installation combat : plus de rafale buff ; buff temporaire posé en combat = son unique ; refresh = silencieux ; pastilles intactes.
7. Shield : gained/absorb/break **muets**, visuels + pastille intacts, tok/verre disparus (fragments boss inclus — mêmes events).
8. Volumes : wind-up/thud/relais nettement présents ; run à l'aveugle : le tour ennemi s'entend.
9. Rapport builder : re-câblés/mutés/sans-clip conformes ; zéro GUID orphelin ; `git status` propre après 2e run du menu (idempotence).
