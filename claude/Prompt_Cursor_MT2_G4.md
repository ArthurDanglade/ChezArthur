# PROMPT CURSOR — MT2-G4 : pages saison (page, header, récap, ensure hub)

> Chez Arthur — Unity 2022.3, C#. `.cursorrules` strict. Base : `main` à `dbd08be` (ou HEAD).
> Contrat : `Systeme_Saisons_Design_v2.docx` §9/§10 + plan D7/D8. Acquis G1–G3 : `SeasonRewards`
> (états/claims/prestige/`CreditPendingRecap`), `pendingSeasonRecap`, `SeasonRotationManager`, `Loc`.
> Textes joueur : TOUJOURS via `LocalizedText` (scène) ou `Loc.Tr/Format` (code), clés `ui.saison.*`.

## PÉRIMÈTRE — 4 MODIFIÉS + 4 CRÉÉS

Modifiés : `UI/HubHeaderUI.cs` · `Hub/HubManager.cs` · `Meta/SeasonRotationManager.cs` · `Debug/DebugMenu.cs`
Créés : `Hub/Pages/SeasonPageUI.cs` · `Hub/Pages/SeasonTierEntryUI.cs` · `UI/SeasonRecapUI.cs` · `Editor/SeasonPageBuilder.cs`
**RIEN D'AUTRE.** Interdits : `PageAccueilUI`, nav bas (`HubNavigationUI`), missions, gacha, `Feedback/**`, `UI/RevealStage/**`. Scène Hub éditée UNIQUEMENT par le builder (scène propre avant exécution).

## 1. `SeasonRotationManager.cs` — bornes de saison

- `public static DateTime GetCurrentSeasonEndParis()` : lundi d'époque + `(seasonIndex + 1) × SEASON_LENGTH_WEEKS × 7` jours (00h00 Paris — cohérent MT2-D9).
- `public static TimeSpan GetTimeUntilSeasonEnd()` : `GetCurrentSeasonEndParis() − GameClock.ParisNow` (borné ≥ 0). Rien d'autre.

## 2. `HubHeaderUI.cs` — refonte D8

- **Retrait du record** : champ `bestStageText` conservé (référence scène) mais plus jamais écrit ; le builder désactive son GameObject. `UpdateTexts` ne touche plus au record (le record reste visible ailleurs — page saison, stats).
- Nouveau : `[SerializeField] private Button seasonButton;` + `[SerializeField] private TextMeshProUGUI seasonButtonScoreText;` (score courant affiché sur le bouton, rafraîchi via `OnDataChanged` existant). Clic → `SeasonPageUI.Open()` (ref sérialisée `seasonPage`). Null-safe complet, désabonnements en `OnDestroy`.

## 3. `Hub/Pages/SeasonPageUI.cs` — la page (overlay plein écran, PAS une page de la nav)

`Open()`/`Close()`. Refresh à l'Open + abonnement `OnDataChanged` pendant l'ouverture. Ordre de lecture contrat §10.2 :
1. **Où j'en suis** : score (`bestScoreThisSeason`), dernier palier franchi (`SeasonRewards` — plus haut palier éligible), stats (meilleur étage ×cran, runs).
2. **Ce qui manque** : `Loc.Format("ui.saison.manque", "Encore {0} points → palier {1}", …)` + contenu du prochain palier (Tals / LR). Si palier 12 passé : progression vers le prochain prestige.
3. **Temps restant** : `GetTimeUntilSeasonEnd()` — `{j} j {h} h`, bascule `{h} h {m} min` sous 24 h (rafraîchi à l'Open et 1×/minute via coroutine pendant l'ouverture — pas d'Update par frame).
4. **La piste** : `ScrollRect` vertical de 12 `SeasonTierEntryUI` + ligne prestige ; **auto-centrage à l'Open sur le palier courant** (normalizedPosition calculée) ; chaque entrée : n° palier, score requis, récompense (Tals / icône LR), état (verrouillé / **bouton Réclamer** → `SeasonRewards.TryClaim(i)` / réclamé) ; prestige : compteur claimable + bouton (→ `ClaimAllPrestige`).
Bouton « Revoir le dernier bilan » : visible si un récap existe (`pending || rewardsCredited`) → `SeasonRecapUI.OpenForConsultation()` (**sans re-crédit** — garanti par `rewardsCredited`).

## 4. `UI/SeasonRecapUI.cs` — écran bloquant

- `OpenAsGate()` : appelé au chargement du Hub si `PendingSeasonRecap.pending && !rewardsCredited`. Overlay bloquant (scrim plein écran, au-dessus de tout) : saison terminée (id), score final, meilleur étage ×cran, runs, dernier palier, **liste des récompenses créditées** — et à l'affichage : `SeasonRewards.CreditPendingRecap()` (le versement EST le moment du récap, v2 §9.2). Bouton unique « Continuer » → `pm.MarkRecapShown()` (pending = false) → Close.
- `OpenForConsultation()` : mêmes données depuis le récap stocké, AUCUN crédit, bouton Fermer.
- Aucun timer, aucune fermeture par tap-hors-panel (moment de bilan, contrat §9.2).

## 5. `Hub/HubManager.cs`

Dans `Start()`, AVANT l'affichage de la page 0 : `SeasonProgressManager.EnsureSeasonCurrent();` puis si récap pending non crédité → `seasonRecapUI.OpenAsGate()` (ref sérialisée, null-safe — **c'est le rattrapage hub consigné en G1, soldé ici**).

## 6. `Editor/SeasonPageBuilder.cs`

`[MenuItem("Chez Arthur/Meta/Build Season Page (Hub)")]`, idempotent, Undo, rapport `Audits/season_page_build.txt` :
1. **Header** : désactive le GameObject du record ; crée le bouton Saison **au centre** (entre pseudo et Tals — gabarit cloné d'un bouton existant du header, **PURGE listeners + LocalizedText clonés, leçon HF1 non négociable**) ; bind `seasonButton`/`seasonButtonScoreText`/`seasonPage` par `SerializedObject`.
2. **Page saison** : overlay sous le canvas hub (scrim + panneau + ScrollRect + 12 entrées instanciées depuis un gabarit d'entrée construit par le builder + ligne prestige + bouton bilan + bouton fermer). Bind complet de `SeasonPageUI` et des 12 `SeasonTierEntryUI`.
3. **Récap** : overlay dédié au-dessus de tout (sorting), bind `SeasonRecapUI` + ref dans `HubManager`.
4. `LocalizedText` sur tous les labels statiques (clés `ui.saison.*`, frDefault FR) + alimentation `Table_UI` (`english=""`).
5. Ré-exécution = zéro changement.

## 7. `DebugMenu.cs`

Boutons : `Ouvrir page saison` · `Ouvrir récap (gate)` (force `OpenAsGate` si pending) · label temps restant saison. Null-safe.

## GARDE-FOUS
Crédit récap UNIQUEMENT via `CreditPendingRecap` (déjà à crédit unique) · consultation ≠ crédit · le récap-gate ne s'affiche qu'au Hub (jamais en run) · page = overlay (nav bas intacte, `HubManager.ShowPage` non modifié hors Start) · budget perf : zéro Update/frame (coroutine 1 min pour le compte à rebours) · logs `[Season]`.

## CHECKLIST (Arthur)
1. **Builder Hub** (scène propre) : rapport, re-run = zéro diff ; header : record masqué, bouton Saison centré avec score.
2. **Page** : ouverture depuis le header ; les 4 blocs dans l'ordre contrat ; piste auto-centrée sur le palier courant ; `+50 score` (debug) → refresh live ; Réclamer un palier → Tals versés, état passe à Réclamé ; palier LR → Goat obtenu/niveau ; prestige après palier 12.
3. **Temps restant** : cohérent avec `+7 jours` debug (diminue) ; format < 24 h correct.
4. **Récap gate** : +42 j → retour Hub (ou `Check rollover` + re-entrée Hub) → récap bloquant s'affiche AVANT toute interaction, récompenses listées ET créditées à cet instant (Tals avant/après), « Continuer » → hub normal, gate ne réapparaît pas.
5. **Consultation** : « Revoir le dernier bilan » → mêmes chiffres, AUCUN re-crédit (Tals inchangés).
6. **FR/EN** : bascule → page + récap traduits (fallback FR pour les clés vides), zéro troncature portrait.
7. **Smoke** : accueil/lancer/crans/missions/gacha/Boss Rush intacts ; aucune régression nav bas.
