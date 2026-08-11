# PROMPT CURSOR — MT2-G3 : piste de saison (grille, claims, LR, portail cumulatif)

> Chez Arthur — Unity 2022.3, C#. `.cursorrules` strict. Base : `main` à `7c060ab` (ou HEAD).
> Contrat : `Systeme_Saisons_Design_v2.docx` §7/§9 + plan D3/D7/D10. **La PAGE saison et l'écran récap = G4** —
> ici on livre la DATA et les API, testables au DebugMenu. Valeurs Tals = placeholders assumés.

## PÉRIMÈTRE — 8 MODIFIÉS + 3 CRÉÉS

Modifiés : `Core/SaveData.cs` · `Core/SaveSystem.cs` (v5) · `Core/SaveMigrator.cs` · `Core/PersistentManager.cs` · `Meta/SeasonProgressManager.cs` · `Gacha/BannerData.cs` · `Gacha/GachaManager.cs` (hook borné) · `Debug/DebugMenu.cs`
Créés : `Meta/SeasonRewardsConfig.cs` (SO) · `Meta/SeasonRewards.cs` · `Editor/SeasonRewardsAssetsBuilder.cs`
**RIEN D'AUTRE.** Interdits : pages/UI hub (G4), scènes, MissionManager, `Feedback/**`, `UI/RevealStage/**`, flux d'invocation UI.

## 1. `SeasonRewardsConfig.cs` — SO (`Resources/SeasonRewardsConfig`, pattern LoadDefault caché null-safe)

- `[Serializable] SeasonTier { int scoreRequired; int talsReward; bool grantsLrLevel; }` · `List<SeasonTier> tiers` — défauts contrat §7.1 : scores 20/40/60/80/100/130/160/200/250/320/400/500 ; `grantsLrLevel` = true aux index 4/7/9/11 (paliers 5/8/10/12) ; Tals placeholder (ex. 100×(index+1), grandissant).
- Prestige : `int prestigeStep = 150;` `int prestigeTalsReward = 50;`
- LR par saison : `[Serializable] SeasonLrEntry { int seasonIndex; string lrCharacterId; }` · `List<SeasonLrEntry>` (défaut : `{ 1, "goat" }` — placeholder MT2-D10) · `string GetLrIdForSeason(string seasonId)` (parse "S{n}", fallback dernier connu, null-safe → "").

## 2. Save v5 (`SaveData`/`SaveSystem`/`SaveMigrator`)

- `SeasonRecapData` += `int pendingTals;` `int pendingLrLevels;` `string lrCharacterId = "";` `bool rewardsCredited;` (additifs).
- Bloc compte += `List<string> pastSeasonLrIds` (LR entrés au portail cumulatif — jamais reset).
- `CURRENT_SAVE_VERSION = 5` · `MigrateV4ToV5` (ancre documentée, additif) · `NormalizeNulls` += `pastSeasonLrIds`, `pendingSeasonRecap.lrCharacterId`.

## 3. `PersistentManager.cs`

- Wiring save/load + accès `PastSeasonLrIds` · `AddPastSeasonLr(string id)` (idempotent, Save).
- `bool TryClaimSeasonTier(int tierIndex)` → délégué de données pur : garde `!claimedTiers.Contains` → add + Save + OnDataChanged (la logique d'éligibilité vit dans `SeasonRewards`).
- `void IncrementPrestigeClaimed(int count)` (+= , Save).
- `void MarkRecapRewardsCredited()` (`pendingSeasonRecap.rewardsCredited = true`, Save).

## 4. `SeasonRewards.cs` — statique (`ChezArthur.Meta`), cerveau de la piste

- `TierState GetTierState(int i)` : `Locked` (score < requis) / `Claimable` / `Claimed`.
- `bool TryClaim(int i)` : éligible + non réclamé → crédit (**Tals via `AddTals` ; si `grantsLrLevel` : `Characters.AddCharacter(lrId)`** — la sémantique doublon existante fait la montée de niveau, log `[Season]`) → `TryClaimSeasonTier(i)`. Jamais de double crédit (claim d'abord refusé si déjà pris).
- Prestige : `int GetPrestigeClaimableCount()` = `max(0, (score − tier12.scoreRequired) / prestigeStep) − prestigeTiersClaimed` (0 si palier 12 non atteint) · `int ClaimAllPrestige()` (crédite N × prestigeTalsReward, incrémente, retourne N).
- `void ComputeRolloverEntitlements(SeasonRecapData recap, ...)` : appelé PENDANT le rollover — pour la saison finie : somme des Tals des paliers **éligibles non réclamés** (+ prestige restant) → `recap.pendingTals` ; nombre de montées LR non réclamées → `recap.pendingLrLevels` + `recap.lrCharacterId` ; `recap.lastTierReached` = plus haut palier éligible (1-based, 0 si aucun) ; `rewardsCredited = false`.
- `void CreditPendingRecap()` : si `pending && !rewardsCredited` → verse `pendingTals` + `pendingLrLevels × AddCharacter(lrCharacterId)` → `MarkRecapRewardsCredited()`. (**G4 l'appellera à l'affichage du récap** — v2 §9 : acquis à la fin, crédité à l'affichage. Testable au DebugMenu dès G3.)
- `bool IsLrUnlockedForPortal(string characterId)` : `pastSeasonLrIds.Contains(id)`.

## 5. `SeasonProgressManager.cs` — rollover enrichi

Dans `EnsureSeasonCurrent`, avant `ApplySeasonRollover` : `SeasonRewards.ComputeRolloverEntitlements(recap, …)` ; puis **le LR de la saison finie entre au portail** : `AddPastSeasonLr(config.GetLrIdForSeason(savedId))` (si non vide). Log complet.

## 6. Portail cumulatif — `BannerData` + `GachaManager` (hook borné)

- `BannerData` : `+ [SerializeField] private bool isLrPortal;` + propriété. RIEN d'autre.
- `GachaManager.RollCharacterFromPool` : si `banner.IsLrPortal`, filtrer les candidats **LR** par `SeasonRewards.IsLrUnlockedForPortal(id)` (les non-LR du pool passent tels quels ; liste filtrée vide → fallback comportement actuel + LogWarning). **Aucun autre changement dans GachaManager** — pity/taux/coûts intacts.

## 7. `SeasonRewardsAssetsBuilder.cs` — éditeur

`[MenuItem("Chez Arthur/Meta/Build Season Rewards Assets")]`, idempotent, rapport `Audits/season_rewards_build.txt` : crée `Resources/SeasonRewardsConfig.asset` (défauts §1) + `ScriptableObjects/Banners/Banniere_Portail_LR.asset` (BannerData : `isLrPortal = true`, pool = tous les `CharacterData` de rareté LR trouvés au projet, coûts par défaut, `hasDuration = false`). Ne modifie jamais un asset existant (hors ajout des LR manquants au pool du portail).

## 8. `DebugMenu.cs` — META/SAISON

Labels : palier courant/éligible, claims (`claimedTiers.Count`/12), prestige claimable, recap pending (+ pendingTals/LR). Boutons : `+50 score` (via `TryImproveSeasonScore(score+50, stage fictif 0, tier 1f)` — commenter « debug ») · `Claim palier suivant` · `Claim prestige` · `Créditer récap pending` (→ `CreditPendingRecap`). Null-safe.

## GARDE-FOUS
Jamais de double crédit (claim refusé si déjà pris ; récap crédité une seule fois via `rewardsCredited`) · le rollover G1 reste scopé (les nouveaux champs compte survivent, le recap est écrasé par le nouveau — c'est voulu : un seul récap pending à la fois, l'ancien non affiché est perdu, **consigné** v2 « une saison à la fois ») · LR crédité via `AddCharacter` uniquement (aucun code de niveau custom) · logs `[Season]`.

## CHECKLIST (Arthur)
1. **Builder** : 2 assets créés, re-run = zéro diff ; pool du portail = tous les LR (Goat inclus), `isLrPortal` coché.
2. **Migration v4→v5** : save v4 → log migration, jeu intact.
3. **Claims** : `+50 score` ×2 (score 100) → paliers 1–5 éligibles → `Claim palier suivant` ×5 : Tals crédités croissants, **palier 5 = Goat obtenu** (ou +1 niveau si possédé) ; re-claim refusé.
4. **Prestige** : score ≥ 500 (+50 ×10) → claims 6–12 → prestige claimable = (score−500)/150 ; `Claim prestige` → Tals versés, compteur à jour.
5. **Rollover entitlements** : nouvelle saison de test, +50 score ×3 (150), **ne rien réclamer** → +42 j → `Check rollover` → recap : pendingTals = somme paliers 1–6 non réclamés, pendingLrLevels = 1 (palier 5), lastTierReached = 6 → `Créditer récap` → Tals + Goat versés, re-crédit refusé · **pastSeasonLrIds contient le LR** → portail : Goat tirable (pool non vide).
6. **Gacha non-régression** : bannière normale (non-portail) : tirages/pity/coûts inchangés ; portail avec `pastSeasonLrIds` vide → fallback + warning, pas de crash.
7. **Smoke** : run/score/crans/missions/Boss Rush inchangés.
