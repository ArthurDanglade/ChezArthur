# PROMPT CURSOR — MT2-G2 : crans de difficulté (sélecteur, déblocage, scaling v1)

> Chez Arthur — Unity 2022.3, C#. `.cursorrules` strict. Base : `main` à `53e7c1d` (ou HEAD).
> Contrat : `Systeme_Saisons_Design_v2.docx` §5/§10.3 + `Plan_Execution_MT2_Saisons.md` D6/D8.
> Acquis G1 à réutiliser tels quels : `_currentDifficultyMultiplier` (RunManager), `unlockedDifficulties` (save v4),
> `SeasonProgressManager.ReportStageReached`, pattern Pending (`PendingRunMode`), `IsSeasonTainted()`.

## PÉRIMÈTRE — 6 MODIFIÉS + 3 CRÉÉS

Modifiés : `Core/PersistentManager.cs` · `Core/RunManager.cs` · `Gameplay/StageGenerator.cs` · `Hub/Pages/PageAccueilUI.cs` · `Debug/DebugMenu.cs` · (audit borné : `Enemies/MidCombatSpawner.cs`/`EnemySummonSystem.cs` — voir §4, **modification uniquement si héritage du cran non transitif**)
Créés : `Meta/DifficultyConfig.cs` (SO) · `Hub/Pages/DifficultySelectorUI.cs` · `Editor/DifficultySelectorBuilder.cs`
**RIEN D'AUTRE.** Interdits : page saison (G4), MissionManager, gacha, `Feedback/**`, `UI/RevealStage/**`, Boss Rush flow.

## 1. `Meta/DifficultyConfig.cs` — SO (`ChezArthur.Meta`)

`[CreateAssetMenu(..., "Chez Arthur/Meta/Difficulty Config")]`. Champs : `[Serializable] DifficultyTier { string label; float multiplier; }` · `List<DifficultyTier> tiers` (défauts : x1/1f, x1,5/1.5f, x2/2f, x3/3f, x5/5f) · `int unlockStage = 50` (étage requis dans le cran N pour débloquer N+1). Accès statique `DifficultyConfig.LoadDefault()` : `Resources.Load<DifficultyConfig>("DifficultyConfig")`, caché, null-safe (absent → 1 warning + défauts codés en dur via instance runtime). Propriétés : `TierCount`, `GetLabel(i)`, `GetMultiplier(i)` (bornés).

## 2. `PersistentManager.cs`

- Pending (non sauvegardé, pattern `PendingRunMode`) : `SetPendingDifficulty(int index)` + `(int index, float multiplier) ConsumePendingDifficulty()` (défaut 0/1f ; multiplier résolu via `DifficultyConfig.LoadDefault()` à la consommation).
- `bool IsDifficultyUnlocked(int index)` : index ≤ 0 → true ; sinon `unlockedDifficulties.Contains(index)`.
- `void UnlockDifficulty(int index)` : garde index valide + pas déjà présent → add + `SaveGame()` + `OnDataChanged` + log `[Season]`.

## 3. `RunManager.cs`

- Champ `private int _currentDifficultyIndex;` (+ propriété lecture).
- `StartRun()` (après `EnsureSeasonCurrent`) : `var (idx, mult) = ConsumePendingDifficulty();` — **Boss Rush force idx 0 / mult 1f** (crans hors Boss Rush v1). Remplace le `_currentDifficultyMultiplier = 1f` posé en G1 par ces valeurs. Log `[RunManager] Run x{label} (index {idx})`.
- `RegisterStageReachedAsBest()` : après le report existant, déblocage :
  ```csharp
  // Déblocage du cran suivant : étage requis atteint dans le cran courant, run légitime.
  if (_currentRunMode == GameRunMode.Normal && !IsSeasonTainted()
      && _currentStage >= DifficultyConfig.LoadDefault().UnlockStage)
      PersistentManager.Instance?.UnlockDifficulty(_currentDifficultyIndex + 1);
  ```
  (`UnlockDifficulty` gère lui-même « dernier cran » et « déjà débloqué » sans bruit.)

## 4. `StageGenerator.cs` — scaling v1

`GetHpMultiplier` et `GetAtkMultiplier` : multiplier le résultat par le cran — `* (RunManager.Instance != null ? RunManager.Instance.CurrentDifficultyMultiplier : 1f)`. **Un seul point d'application chacun.** DEF/SPD non touchés (axes riches = post-refonte ennemis, contrat §5.4). Le spawn Boss Rush « stats de base » (l.148) reste hors scaling ✓.
**Audit borné (rapport en commentaire de PR/commit)** : vérifier que `MidCombatSpawner` et `EnemySummonSystem` héritent du cran — soit leurs `hpMult/atkMult` proviennent des getters de StageGenerator, soit (invocations) des stats du sommonneur déjà scalées (héritage transitif). Si un chemin passe à côté : appliquer le même facteur cran à ce chemin, et RIEN d'autre.

## 5. `Hub/Pages/DifficultySelectorUI.cs`

Panel overlay (construit par le builder §6). API : `Open()` / `Close()`. Contenu : titre, **label rotation** (« Pos. 1 cette semaine : {UniverseIds.GetDisplayName(SeasonRotationManager.GetCurrentUniverseAtSlot(0))} ») rafraîchi à l'Open, 5 lignes de cran : bouton label (`GetLabel`) — débloqué : cliquable ; verrouillé : non-interactable, alpha réduit, sous-texte condition (« Étage {unlockStage} en {label du cran précédent} »). Clic cran : `SetPendingRunMode(Normal)` + `SetPendingDifficulty(i)` + `SceneLoader.LoadGame()` — **2 touches respectées : Lancer → cran → run**. Bouton fermer. Null-safe complet ; textes joueur via `LocalizedText` posés par le builder (clés `ui.accueil.diff_*`, frDefault FR).

## 6. `Editor/DifficultySelectorBuilder.cs`

`[MenuItem("Chez Arthur/Meta/Build Difficulty Selector (Hub)")]` — idempotent, Undo, rapport `Audits/difficulty_selector_build.txt` :
1. Crée `Assets/_Project/Data/Meta/DifficultyConfig.asset` (défauts §1) + copie/référence dans `Assets/_Project/Resources/` si absent (asset unique : le créer directement dans Resources).
2. Construit le panel dans `Hub.unity` sous le canvas de `PageAccueilUI` (racine trouvée par composant) : fond scrim + colonne 5 boutons + labels, **gabarit cloné depuis le bouton Lancer existant** — **PURGER les persistent listeners ET tout `LocalizedText`/composant hérité du clone avant réétiquetage (leçon G2-P1/HF1, non négociable)**.
3. Bind par `SerializedObject` : refs du `DifficultySelectorUI` + nouvelle ref `difficultySelector` de `PageAccueilUI`.
4. Pose les `LocalizedText` (clés `ui.accueil.diff_*`) + alimente `Table_UI` (`english=""`), pattern LocalizationPilotBuilder.
5. Ré-exécution = zéro changement (rapport « déjà présent »). **Scène propre avant exécution** (règle §3.5 MT0).

## 7. `PageAccueilUI.cs`

Ref sérialisée `[SerializeField] private DifficultySelectorUI difficultySelector;`. `OnLancerRunClicked` : si ref non nulle → `difficultySelector.Open()` (le lancement part du sélecteur) ; **fallback si null : comportement actuel inchangé** (SetPendingRunMode + LoadGame). Boss Rush : intact.

## 8. `DebugMenu.cs` — META/SAISON

Labels : `Cran run : x{mult} (idx {i})` · `Débloqués : {liste}`. Boutons : `Unlock all crans` · `Reset crans` (Clear + Save). Null-safe.

## GARDE-FOUS
Boss Rush : ni sélecteur, ni scaling, ni déblocage (forcé x1) · le déblocage exige une run **non tainted** (god mode / restart debug ne débloquent jamais) · aucun changement de cran en cours de run (aucune API ne le permet — verrouillage structurel) · logs `[Season]`/`[RunManager]`.

## CHECKLIST (Arthur)
1. **Builder Hub** (scène propre !) : rapport, re-run = zéro diff, config asset dans Resources.
2. **Sélecteur** : Lancer → panel (rotation pos. 1 affichée juste) → x1 cliquable, x1,5→x5 grisés avec condition ; tap x1 → run. **2 touches.**
3. **Scaling** : baisser `unlockStage` à 3 dans l'asset (test data) → run x1 jusqu'à l'étage 3 → log unlock → relancer → x1,5 dispo → étage 1 en x1,5 : PV/ATK ennemis ×1,5 vs x1 (carte ennemie) ; invocation mid-combat scalée aussi.
4. **Score** : étage 4 en x1,5 → DebugMenu : score 6 (4×1,5).
5. **Gardes** : god mode → étage 3 ne débloque PAS x2 · Boss Rush → aucun scaling, cran x1, pas d'unlock.
6. **Persistance** : redémarrage → crans débloqués conservés · rollover +42 j → **crans conservés** (bloc compte).
7. Remettre `unlockStage = 50` + non-régression smoke (run, pause, gacha, missions, Boss Rush).
