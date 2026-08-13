# RUI0 — Vérité terrain & dettes (étape 1 — RAPPORT)

Date : 2026-08-13 21:57  
HEAD de travail : post-`cb2bf60` (BR2 clos) · audit : `claude/Audit_Preparatoire_Refonte_UI.md` v1.1 §7  
**Statut** : rapport uniquement — **zéro code**. En attente Go étape 2.

---

## 1. Lane Refonte Hub — doc rétroactif

Menu racine : `Chez Arthur/Refonte Hub/*` (DRY RUN / APPLIQUER).  
Helpers associés (sans MenuItem Refonte Hub) : `UiKitFactory`, `UIKitSandboxBuilder`, `RoundedRectSpriteGenerator`, `TransportIconGenerator`.

| Gate / n° | Builder | Ancre | Pose / modifie |
|---|---|---|---|
| 1.2 | `HubSceneRestructurer` | `Editor/Tools/HubSceneRestructurer.cs` | `BackgroundLayer` / `SafeRoot` / `OverlayLayer` sur Hub |
| 1.4a/c | `HubScenePurger` | idem | Purge dette scène (raycasts, fonts null, objets test) |
| 2.1 | `HubHeaderBuilder` | `HubHeaderBuilder.cs` | Header Option A sous SafeRoot + `HubHeaderSafeBleed` ; pills identité / Tals / saison |
| 2.2 | `HubNavBuilder` | `HubNavBuilder.cs` | `NavigationBar` + `HubNavSafeBleed` + `PageTransitionController` — LOCK : ne touche pas le haut |
| 3.1 | `HomeRigBuilder` | `HomeRigBuilder.cs` | `HomeIllustrationRig` + `BottomZone` overlay + framing cover (inset nav) |
| 3.2+ | `HomeActionsBuilder` | `HomeActionsBuilder.cs` | CTA Lancer / BossRush overlay ; Shop/News haut |
| 3.3 | `HomeTopBandBuilder` | `HomeTopBandBuilder.cs` | Bande `Shop · LofiPlayerBar · News` (chrome transparent, lofi outlineOnly) ; icône `Porte monnaie.png` |
| 4.a | `MissionsPageBuilder` | `MissionsPageBuilder.cs` | PageMusique → PageMissions (purge nominative + structure) |
| 4.a+ | `MissionsPagePolishBuilder` | | Bandeau Accueil-only, clearance header, cartes lisibles |
| 4.a++ | `MissionsPagePolishV2Builder` | | TabBar icônes, Tals2 cohérent, `TalsClaimFX` + SFX |
| **5.a** | `TeamPageRebuilder` | `TeamPageRebuilder.cs` | Page Équipe collection-first, dock, purge PhoneFrame |
| **5.a.1** | `TeamPageLayoutPolishBuilder` | | Double inset, scroll VLG, titre section |
| **5.a.2** | `CharacterCardPolishBuilder` | | Carte : pas de chip spé, Nv centré, ATK/DEF/SUP, icon plein cadre |
| **5.b** | `TeamDragBuilder` | | `DragLayer` + `TeamDragController` + hint |
| **5.c** | `DetailPopupRebuilder` | | Prefab popup + cleanup Hub |
| **5.c.1** | `DetailPopupPolishBuilder` | | Header allégé, **null** `typeText`/`rarityChip*`, stats colorées, panneau 270, shine OFF |
| 6.a–c | `InvocationPageRebuilder` | `InvocationPageRebuilder.cs` | Portails + showcase Personnages |

**Décisions implicites embarquées (à écrire avant reconstruction RUI)**  
- Tokens Hub déjà poussés dans `UiTheme` : `BgDeep/Panel/Elevated`, `BorderSubtle/Strong`, `AccentAmber/Rose/Teal`, `Space1–6`, `RadiusS/M/L`, `HeaderHeight=112`, `NavHeight=152`, `ButtonPrimaryH=96`, `TouchTargetMin=96`, `ButtonMaxWidth=920`.  
- Pattern DRY RUN / APPLIQUER + harnais « À FAIRE / CONFORMES / ÉCHECS ».  
- 5.c.1 a **débranché** les chips rareté du popup sans purge GO (dette §7).  
- Lane **sans doc produit** jusqu’ici → ce tableau = absorption RUI0.

**Hors menu Refonte Hub mais liés** : `SeasonPageBuilder` / `DifficultySelectorBuilder` (`Chez Arthur/Meta/…`), `GachaSummaryBuilder` (`Chez Arthur/UI/…`).

---

## 2. Inventaire des 12 écrans

Scènes : `Assets/_Project/Scenes/Hub.unity` · `Game.unity`

### Canvases (ancrage)

| Scène | Canvas | sortingOrder |
|---|---|---|
| Hub | `HubCanvas` | 0 |
| Hub | `SeasonRecapOverlay` | 500 |
| Game | `Canvas` (HUD) | −1 |
| Game | `SacrificePanel` (override) | 50 |
| Game | `SynergyBannerCanvas` | 110 |
| Game | `RuptureBannerCanvas` | 120 |
| Game | `EnemyHPBarsCanvas` | 200 |
| Game | `BattleTextCanvas` | 250 |

### Hub

| ID | Forme | Canvas / chemin | Scripts | Builder |
|---|---|---|---|---|
| **H1** CTA | **page** Accueil | `HubCanvas` → `PageAccueil` → `BottomZone` → `BtnLancerRun` / `BtnBossRush` | `PageAccueilUI`, `HubButtonUI` → ouvre `DifficultySelectorUI` | `HomeActionsBuilder`, `HomeRigBuilder` |
| **H2** Header | **chrome** | `SafeRoot` → `Header` (`PillIdentity`, `PillStage`, `PillTals`, `BtnSaison`) | `HubHeaderUI`, `HubHeaderSafeBleed` | `HubHeaderBuilder` (+ bind saison) |
| **H3** Bande | **bande page** | `TopUtilityRow` → Shop / `LofiPlayerBar` / News | `LofiPlayerBarUI`, `PageAccueilUI` | `HomeTopBandBuilder` |
| **H4** Difficulté | **popup/overlay** | `DifficultySelectorOverlay` (`Scrim`+`Card`) | `DifficultySelectorUI` | `Chez Arthur/Meta/Build Difficulty Selector (Hub)` |
| **H5** Saison | **popup/overlay** | `SeasonPageOverlay` ; satellite `SeasonRecapOverlay` so=500 | `SeasonPageUI`, `SeasonTierEntryUI`, `SeasonRecapUI` | `Chez Arthur/Meta/Build Season Page (Hub)` |
| **H6** Récap invoc. | **overlay flow** (pas nav) | `OverlayLayer` → `GachaAnimationUI` → **`SummaryScene`** (live) ; legacy `PullResultPopup` OBSOLÈTE | `GachaAnimationController`, `PullResultEntryUI`, `GachaSummaryGridFitter` · prefabs `PullResultEntry` / `PullResultSingleCard` | `GachaSummaryBuilder` + BR2 wiring |

### Run

| ID | Forme | Canvas / chemin | Scripts | Builder |
|---|---|---|---|---|
| **R1** Header combat | **HUD** composite | `Canvas` → `HeaderBar` + `SynergyHud` ; `BossHPBarPanel` ; `InitiativeBannerPanel` ; toast `SynergyBannerCanvas` | `GameUI`, `BossHPBarUI`, `SynergyHudUI`, `SynergyBannerUI`, `InitiativeBannerUI` | plusieurs `Chez Arthur/UI/Monter|Build …` |
| **R2** Caisses HP | **HUD** | `SafeArea` → `AllyHPBar_1..4` | `AllyHPBar`, `GameUI` | **non** dédié |
| **R3** Fiche ennemi | **overlay** hold | `EnemyCard` → `EnemyCardPanel_v2` | `EnemyCardUI`, `EnemyCardStyle` | `Chez Arthur/UI/Générer|Réparer Fiche Ennemi v2` |
| **R4** Bonus | **panel** plein écran | `BonusSelectionPanel` + `BonusCard_1..3` | `BonusSelectionUI`, `BonusCard` | `BonusCardRebuilder` / `BonusCardStyler` (`Take Five Games/UI/…`) |
| **R5** Sacrifice | **panel** (modale centrée) | **`SacrificePanel` Canvas so=50** | `SacrificeUI`, `SacrificeSlotUI`, `SacrificeUIBridge`, `StatLineUI` | nombreux `Take Five Games/UI/…` (bottom-sheet, comparaison, slots…) |
| **R6** Shop gare | **panel** | `GarePanel` | `GareUI`, `GareSlotCard` ; prefabs `GareSlotCard` / `GareOfferCard` | **non** dédié |
| **R7** Pause état | **popup** onglet 0 | `PauseMenuRoot` → `TeamPanel` | `PauseMenuUI`, `TeamPanelUI`, `ValiseSectionUI`, `SynergySectionUI` · **`BonusPanelUI` absent de Game.unity`** | partiel (synergies / generators) |
| **R8** Pause settings | **popup** onglet 1 | même root → `SettingsPanel` | `SettingsPanelUI`, `PauseMenuUI` | partiel (`LocalizationPilotBuilder`, etc.) |

---

## 3. UiTheme — tokens actuels + hors-thème

**Ancre** : `Assets/_Project/Scripts/UI/UiTheme.cs` — classe `static` (pas SO).

| Famille | Tokens |
|---|---|
| Sprites | `SpriteCard=card_rounded`, `SpriteCoin=Tals2`, `SpriteMenu` |
| Surfaces | `Surface`, `SurfaceBar`, `SurfaceGlobal`, `Frame` |
| Fonds Hub | `BgDeep`, `BgPanel`, `BgElevated` |
| Bordures | `BorderSubtle`, `BorderStrong` |
| Textes | `TextPrimary/Secondary/Muted`, `AccentSection`, `TextDisabled` |
| Onglets | `TabActive`, `TabInactive` |
| Accents | `Gold`, `Positive`, `Negative`, `SynergyBroken`, `AccentAmber/Rose/Teal` |
| États | `Success`, `Danger`, `ScrimOverlay` |
| Super Lancer | `SuperLancerZone/Track/Indicator` |
| **Rareté perso** | `RaritySR/SSR/LR` (+ accès `CharacterRarityPalette`) |
| Fiche perso | `CardPanel*`, `CeremonyLight`, fonts `CardFont*` |
| Gacha | `GachaStageCharcoal` |
| Rôles / stats | `RoleAttacker/Defender/Support`, `StatHp/Atk/Def/Speed` |
| **Rareté valise** | `ValiseCommune/Rare/Epique/Legendaire`, `AccentGold` |
| **Rareté bonus** | `BonusCommon/Uncommon/Rare/Epic/Special` |
| Badges | `BadgeNew/Upgrade/Item/Downside` |
| Type ennemi | `EnemyTypeNormal/MiniBoss/Boss` |
| Typo | `FontTitle`…`FontCelebration` |
| Espacements | `PadCard/Compact`, `SpacingRow`, `Space1–6`, `Radius*`, `Border*`, dims Hub |

**Déjà deux palettes rareté dans UiTheme** (perso vs valise vs bonus) — conforme à l’intention RUI ; le problème audit = **usage confus sur cartes shop/bonus**, pas l’absence de tokens.

**Candidats hors-centralisation (échantillon prouvé)**  
- `CombatFeedbackPalette.cs` — couleurs feedback combat  
- `FloatingNumberSpawner` — couleurs dégâts/soin SerializeField  
- `PressureGaugeUI` — gradient + `trackColor` locaux (partiel `UiTheme.Gold`)  
- `RuptureBannerUI` — couleurs SerializeField  
- `ArtworkTransitionMath` / harness AW — palette éveil dédiée  
- Builders Sacrifice / Bonus qui reposent parfois sur hex locaux via stylers  

→ RUI1 : étendre tokens (grille 8, styles TMP nommés, locked) **sans** fusionner feedback combat / AW dans le même sac.

---

## 4. SacrificeUI / jauge de pression — état réel

### SacrificeUI — **vivant, refonte UI partielle, pas abandonné**

- Script runtime : `Scripts/UI/SacrificeUI.cs` — API complète (incoming + comparaison colonnes + slots + confirm).  
- Bridge : `SacrificeUIBridge` branché sur `ValiseManager` / `ItemManager` `OnSacrificeRequired`.  
- Scène : `SacrificePanel` Canvas sortingOrder **50**.  
- Prefab slot : `Prefabs/UI/SacrificeSlot_0.prefab`.  
- Outils éditeur actifs (`Take Five Games/UI/…`) :  
  - `SacrificeBottomSheetRebuilder` — **Gate 6b.1** : modale flottante centrée (scrim 0.60, marges 48) — **remplace** l’ancien bottom-sheet plein écran  
  - `SacrificeVerticalRebuilder1`, `SacrificeComparisonRebuilder`, `SacrificeComparisonContentStyler`  
  - slots : pastilles / tuiles / carrés / scroll  
  - incoming : bande / ligne / bannière  
  - `SacrificeLayoutAuditor`  
- **Verdict** : gameplay OK ; UI en **série de gates Take Five** (6b/6c…) — état « refonte en cours / itérative », **pas** une coquille morte. RUI3 devra **écraser au builder RUI (RUI-D5)** plutôt que continuer la pile de stylers.

### Jauge de pression — **système gameplay LIVE**

- `PressureGaugeSystem` (`Scripts/Gameplay/`) — singleton de scène, montée/descente, rupture Gates 2–4.  
- Scène Game : GO `PressureGaugeSystems` + HUD `PressurePerimeter_v1` (actif).  
- `PressureGaugeUI.cs` + generators éditeur (`PressureGaugeUIGeneratorTool`, `PressureGaugeSceneBuilder`) existent.  
- **Verdict** : pas une dette UI RUI0 à isoler ; HUD périphérique distinct du header combat (R1). RUI2 décidera s’il entre dans le langage header 3 zones ou reste liseré bord.

---

## 5. Monnaies

| Devise | Data | Icône / token | Surfaces |
|---|---|---|---|
| **Tals (méta)** | `PersistentManager.Tals` / `AddTals` / `SpendTals` | `UiTheme.SpriteCoin` = `Tals2` (+ Tals1/3 pour FX) | Header Hub `PillTals`, Missions claim FX, Gacha coûts portail, Saison rewards |
| **Tals (run)** | `RunManager.TalsEarned` + `OnTalsChanged` | même famille visuelle | `GameUI` (compteur run), `GareUI.UpdateTals` (shop) |
| **Or** | **pas** de devise runtime séparée trouvée | `UiTheme.Gold` = **couleur accent** (Tals / valeur), pas une wallet | Confusion label « or » visuelle ≠ data |
| Shop « carré blanc » | — | Bande Accueil : sprite `Porte monnaie.png` (`HomeTopBandBuilder`) ; icône shop peut manquer / placeholder blanc | Ambiguïté visuelle H3 / R6 — **pas une 3ᵉ devise** |

Pas d’autre soft/hard currency inventoriée (pas de gemmes séparées dans les scripts scannés).

---

## 6. Debug dans l’UI de prod

| Élément | Qui pose | Où | Notes |
|---|---|---|---|
| **« Preview éveil »** | `AwakeningCeremonyDebugButton` | Hub : GO `AwakeningCeremonyDebugPreview` (scène) ; runtime peut créer `BtnPreviewEveil` sous Canvas | Label hardcodé `"Preview éveil"` ; outil calibrage AW — **à garder en mode dev** |
| Masquage flow gacha | `GachaAnimationController.debugPreviewRoot` | cherche `AwakeningCeremonyDebugPreview` / `BtnPreviewEveil` | Cache pendant cérémonie invoc. — prouve que le bouton est traité comme chrome parasite |
| **« DBG »** | `DebugMenu` IMGUI | coin écran, bouton 56×32 | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` ; **auto-Destroy** hors editor/dev build |
| Étages | `GameUI.stageText` → `$"Étage {CurrentStage}"` | Header combat | **Prod légitime** (contexte run), pas debug — à garder dans zone R1 |
| Autres | menus pression / season integrity dans `DebugMenu` | IMGUI only | Déjà gated compile |

**Plan isolation (étape 2b — pas encore)** : racine/canvas `Debug` gated (toggle dev) ; retirer Preview éveil du header/prod Hub ; DBG déjà conditionnel mais visible en editor Play Mode → même canvas debug.

---

## 7. Champs morts popup (dette BR §9)

**Ancre script** : `CharacterDetailPopup.cs`

```csharp
[SerializeField] private TextMeshProUGUI typeText;        // L37
[SerializeField] private TextMeshProUGUI rarityChipText;  // L92
[SerializeField] private Image rarityChipFrame;          // L93
```

- **Aucun usage runtime** (`typeText.` / `rarityChip*` = 0 match hors déclaration).  
- Commentaire L1056 : `// typeText / badge spé retirés (Gate 5.c.1).`  
- **Prefab** `CharacterDetailPopup.prefab` : SerializeField refs = `{fileID: 0}` **MAIS GOs toujours présents** : `TypeText`, `RarityChip`, `RarityChipText`.  
- **Builders** :  
  - `DetailPopupPolishBuilder` (5.c.1) → `SetObj(so, "typeText"|"rarityChip*", null)`  
  - `CharacterDetailPopupBuilder` (legacy) → **recrée encore** typeText + chips  
  - `RarityBadgeWiringTool` consigne la dette  

**Plan purge A1 (étape 2a — après Go)**  
1. Retirer les 3 champs du script.  
2. Détruire GOs `TypeText` / `RarityChip*` sur le prefab (builder idempotent).  
3. Aligner : `DetailPopupPolishBuilder` + **`CharacterDetailPopupBuilder`** (ne plus créer) + tout autre SetObj.  
4. Commit séparé `fix:` / `chore:` dette popup.

---

## Synthèse pour contrôle Claude

| # | Verdict court |
|---|---|
| 1 | Lane Refonte Hub = ~17 MenuItems documentés 1.2→6.c ; 5.x équipe/popup absorbables |
| 2 | 12 écrans ancrés ; H4/H5/H6 encore overlays ; H6 live = SummaryScene |
| 3 | UiTheme déjà riche (dont 3 échelles rareté) ; hors-thème = feedback/AW/Sacrifice stylers |
| 4 | Sacrifice **vivant + UI itérative** ; pression **LIVE** (Perimeter) |
| 5 | Une devise : **Tals** (méta + run) ; Gold = couleur ; carré blanc = icône manquante |
| 6 | Preview éveil + DBG = cibles isolation ; « Étage » = prod |
| 7 | Champs morts + GOs orphelins popup — purge A1 claire |

**Interdits respectés** : zéro changement de code / scène / prefab dans cette étape.

**En attente** : Go étape 2 → (a) purge champs morts popup · (b) isolation debug — commits séparés.
