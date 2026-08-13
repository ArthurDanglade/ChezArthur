# PROMPT CURSOR — MT4-G3 : Remote Config (calendrier saisons + tuning pilotables — solde le constat n°5)

> Chez Arthur — Unity 2022.3, C#. Base : `main` HEAD courant. Offline-first : **les SO restent la source
> de vérité par défaut ; le remote est un OVERLAY** — absent/malformé/hors-ligne = jeu identique à aujourd'hui.
> Objectif live-ops : lancer une nouvelle saison (epoch, LR, grille) **sans update de l'app**.

## §0 — DASHBOARD ARTHUR (avant terrain)

Unity Cloud → **Remote Config** : activer, puis créer 4 clés de type JSON avec ces valeurs par défaut (copier tel quel — ce sont les valeurs actuelles du code, l'overlay est donc neutre au départ) :
- `season_calendar` : `{"epochMondayIso":"2026-07-20","seasonLengthWeeks":6,"lrBySeason":[{"seasonIndex":1,"lrCharacterId":"goat"}]}`
- `season_rewards` : `{"tiers":[...les 12 de SeasonRewardsConfig.asset : scoreRequired/talsReward/grantsLrLevel...],"prestigeStep":150,"prestigeTalsReward":50}`
- `difficulty_tiers` : `{"tiers":[{"label":"x1","multiplier":1},{"label":"x1,5","multiplier":1.5},{"label":"x2","multiplier":2},{"label":"x3","multiplier":3},{"label":"x5","multiplier":5}],"unlockStage":50}`
- `live_flags` : `{"seasonEnabled":true,"infoMessage":""}`
Publier l'environnement (production).

## PÉRIMÈTRE — 7 MODIFIÉS + 1 CRÉÉ (+ manifest)

Modifiés : `Packages/manifest.json` (+`com.unity.services.remoteconfig`) · `Backend/BackendService.cs` · `Meta/SeasonRotationManager.cs` · `Meta/SeasonRewardsConfig.cs` · `Meta/DifficultyConfig.cs` · `Meta/SeasonProgressManager.cs` · `Hub/Pages/SeasonPageUI.cs` · `Debug/DebugMenu.cs`
Créé : `Backend/RemoteTuning.cs`
**RIEN D'AUTRE.** `SaveSystem`/save/claims/logic de crédit : intouchés (le remote change des VALEURS, jamais des règles).

## 1. `Backend/RemoteTuning.cs` — fetch + overlay (`ChezArthur.Backend`, statique)

- `FetchAndApplyAsync()` : gardes (signé UGS) → `RemoteConfigService.Instance.FetchConfigsAsync(...)` (timeout 5 s, pattern WhenAny de BackendService) → pour chaque clé présente : **parse défensif** (try/catch par clé, `JsonUtility` sur DTOs `[Serializable]`) → application ; clé absente/malformée → log 1 ligne + défauts conservés (les autres clés s'appliquent quand même).
- Application par clé :
  - `season_calendar` → `SeasonRotationManager.ApplyRemoteCalendar(DateTime epochMondayParis, int seasonLengthWeeks)` + liste LR → `SeasonRewardsConfig` (clone, cf. ci-dessous).
  - `season_rewards` / `difficulty_tiers` → **clone runtime du SO** (`UnityEngine.Object.Instantiate(LoadDefault())`) → `ApplyOverride(dto)` sur le clone → **swap du cache** `LoadDefault` (les consommateurs rappellent `LoadDefault()` à chaque usage : zéro changement chez eux). Jamais de mutation de l'asset original (Editor safe).
  - `live_flags` → propriétés statiques `SeasonEnabled` (défaut true) + `InfoMessage` (défaut "").
- `public static event Action OnTuningApplied;` + état lecture (`LastFetchUtc`, `AppliedKeys`).
- Appelé par `BackendService` : après sign-in réussi + au focus (même throttle 5 min que le sync temps).

## 2. SO — appliers minimaux

- `SeasonRotationManager` : `ApplyRemoteCalendar(...)` pose `_epochMondayParis` (le setter existant `SetEpochMondayParis` trouve enfin son appelant) + `_seasonLengthWeeksOverride` (int?, utilisé par `CurrentSeasonId`/`GetCurrentSeasonEndParis` à la place de la constante quand présent). Le cycle de ROTATION reste 5 — non configurable (structurel).
- `SeasonRewardsConfig.ApplyOverride(dto)` + `DifficultyConfig.ApplyOverride(dto)` : réécrivent leurs listes/valeurs depuis le DTO (bornés, null-safe, count ≠ → log + refus de la clé). + hook interne de swap de cache (`SetRuntimeInstance`) pour `RemoteTuning`.

## 3. Gating saison (`SeasonProgressManager` + `SeasonPageUI`)

- `EnsureSeasonCurrent()` : si `!RemoteTuning.SeasonEnabled` → rollover différé (même pattern que HasTrustedTime, log dédié).
- `SeasonPageUI` : si `!SeasonEnabled` ou `InfoMessage` non vide → ligne sous le countdown (pattern offline existant) : message remote tel quel s'il existe, sinon `Loc.Tr("ui.saison.maintenance", "Saison en maintenance — revenez plus tard")`. Abonné `OnTuningApplied` pendant l'ouverture.

## 4. `DebugMenu.cs` — section « — CONFIG — »

Labels : dernier fetch, clés appliquées, epoch/length/LR courants (effectifs), seasonEnabled/message. Boutons : `Force fetch` · `Reset overrides (session)` (re-swap des caches vers les assets originaux — retour aux défauts sans redémarrer).

## GARDE-FOUS
Overlay pur : offline/échec/malformé = défauts SO exacts · jamais de mutation d'asset (clones runtime) · le remote ne crée jamais d'état en save (les claims/scores restent sur la logique existante — si la grille remote change en cours de saison, `claimedTiers` par index reste valide : **consigné** — une grille remote ne doit jamais RÉORDONNER les paliers en cours de saison, règle d'usage dashboard) · parse par clé indépendant · logs `[Tuning]` sobres.

## CHECKLIST (Arthur — Editor online, dashboard ouvert)
1. **Neutralité** : défauts dashboard = code → fetch → `AppliedKeys` complet, AUCUN comportement changé (page saison, crans, grille identiques) ; offline → défauts, zéro warning répété.
2. **Live-ops epoch** : dashboard `epochMondayIso` −1 semaine → Force fetch → `CurrentSeasonId`/countdown/semaine de rotation bougent (Dump saison avant/après) ; interplay : voyage dans le temps debug fonctionne par-dessus.
3. **Grille** : `talsReward` du palier 1 ×10 au dashboard → fetch → page saison affiche la nouvelle valeur ; claim → crédite la valeur remote ; `Reset overrides` → retour asset.
4. **Crans** : `unlockStage` 50→3 au dashboard → fetch → sous-textes du sélecteur à jour, déblocage à l'étage 3 (puis remettre 50 + fetch).
5. **Kill-switch** : `seasonEnabled:false` + message → page = bandeau maintenance, rollover différé (log) ; `true` → retour normal sans redémarrage.
6. **Malformé** : casser le JSON d'UNE clé au dashboard → fetch → log refus de cette clé seule, les autres appliquées, défauts pour la cassée (puis réparer).
7. **Suite** : étendre la suite (Editor-safe) : parse DTO valide/malformé, ApplyOverride bornes, clone-swap-reset, SeasonEnabled gating. Smoke : saisons/claims/crans/cloud.
