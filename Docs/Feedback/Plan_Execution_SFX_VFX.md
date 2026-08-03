# Plan d'exécution — Chantier SFX/VFX de combat

**Take Five Games — Track Zero** · 2 août 2026 · v1
Compagnon de `Charte_Feedback_Combat_F0.md` **v1.1 validée** (le contrat) et de `Audit_Preparatoire_SFX_VFX.md` (vérité terrain, commit `c176092`).
Règle d'arbitrage : *tout ce qui change l'état du combat doit être vu ET entendu à l'instant où ça change* — sans jamais dépasser les budgets de lisibilité (charte §3).

---

## 0. Cadre acté

Charte v1.1 validée le 02/08 avec amendements A1–A4 (D8 figé, budgets retunables au 1er gate jouable par avenant, gate visuel palette obligatoire avec repli indigo/lavande, groupe A complété `aim_tension` + couverture JuiceDirector vérifiée).

**Protocole de coexistence U1 (rappel, non négociable)** : Claude = F0→F1→F2 sans signatures U1 ; Arthur = G6 en parallèle. Zones gelées croisées : le chantier FX n'édite **jamais** les handlers ennemis (`Scripts/Enemies/**`, `Scripts/Gameplay/Passives/**` côté contenu) ; G6 n'édite jamais `JuiceDirector`, le pool SFX, le mixer, le catalogue FX. `Game.unity` : commits séparés, annoncés, jamais les deux chantiers dans le même commit de scène. Rendez-vous F3 (events génériques) puis F5 (après G6b/c jouables).

---

## 1. Boucle de travail par gate (méthode U1, inchangée)

1. **Audit ciblé** du gate sur clone à HEAD (`git pull` avant chaque rédaction de prompt).
2. **Proposition** : approche + fichiers touchés + critères de test → **Go explicite d'Arthur**.
3. **Prompt Cursor** rédigé contre le code réel : périmètre exact, garde-fous « ne touche pas à… », conventions `.cursorrules`.
4. **Push de contrôle** : Arthur push → je pull et contrôle le **diff ligne par ligne**. Diff hors gate = rejeté.
5. **Test in-game** : checklist fournie avec le prompt (machine/APK).
6. **Commit final** (`feat:`/`fix:`) une fois diff validé et tests passés.

Scripts éditeur idempotents (`[MenuItem "Chez Arthur/…"]`, rapport `Audits/`) pour tout câblage. **Un gate à la fois** : F2+ ne sont détaillés qu'à leur tour — le code aura bougé.

---

## 2. Ordre et dépendances

```
F0 (charte)                                      ✅ VALIDÉE 02/08
 └─ F1-P1  Mixer + routage + fix slider SFX      [pur code, aucune dépendance]
     ├─ F1-P2  Banque sonore v1 + outillage import   [parallèle possible dès P1 validé]
     └─ F2-P1  FeedbackCatalog SO + service poolé + garde-fous
         └─ F2-P2  Re-câblage groupe A (iso-ressenti) + hit-react allié
             └─ F3  Langage d'état (groupe B)     ← RENDEZ-VOUS G6 : les handlers consomment
                 └─ F4  Axe ennemi & moments (groupe C) + haptique + crit complet + sting victoire
                     └─ F5  Signatures & couche pro   ← après G6b/G6c jouables
```

F1-P2 (banque) ne bloque rien : F2 et F3 fonctionnent avec les clips existants + placeholders, remplacés à mesure que la banque se remplit.

---

## 3. F1 — Socle audio

### F1-P1 — Mixer, routage, fix de la chaîne de volume (proposition technique — détail ci-dessous, prompt livré)

**DEMANDE** : un seul point de vérité pour les volumes, le slider SFX qui pilote enfin le combat, le duck de visée par snapshots, Tals chaîné sous le bus SFX (D5, D7, audit §6.1–6.2, §6.5).

**Approche.** Un `AudioMixer` **créé manuellement une fois** par Arthur (l'API éditeur Unity ne permet pas de créer un .mixer par script — déviation assumée, compensée par un outil d'audit qui valide sa conformité) : `Master → Music / Ambiance / SFX`, params exposés `MusicVolume` / `SfxVolume` (dB), snapshots `Normal` / `AimFocus` (Music à −13 dB ≈ le ×0.22 actuel). Placé dans `Assets/_Project/Audio/Resources/MainMixer.mixer`.

Côté code, un helper statique unique `ChezArthur.Audio.AudioBuses` (lazy `Resources.Load`, null-safe : mixer absent = warning unique + comportement actuel préservé) expose les groupes, `SetMusicVolume01` / `SetSfxVolume01` (conversion dB, plancher −80), `TransitionToAim(s)` / `TransitionToNormal(s)`. **Déviation `.cursorrules` documentée** : helper statique + Resources.Load plutôt qu'injection SerializeField — les managers créent leurs AudioSources par `AddComponent` au runtime dans deux scènes ; injecter le mixer dans 6 composants × 2 scènes multiplierait les points de câblage pour zéro bénéfice.

Routage à la création des sources (zéro édition de scène sauf une) : `AudioManager` (music/train/vinyl → Music/Ambiance/Ambiance), `SfxManager` (2 sources → SFX), `SfxPlayer` (pool 10 → SFX), source de tension `JuiceDirector` (→ SFX), `TalsDropSystem` (→ SFX, son slider fin conservé en multiplicateur local, D7). Seule édition de scène : `CombatMusic` (source sérialisée dans `Game.unity`) → groupe Music, via builder `Chez Arthur/Audio/Câbler Audio Scène Combat` — **exécution et commit de scène séparés et annoncés** (protocole §0).

Unification des volumes : le bus devient l'unique master. `SfxManager.SetVolume` → bus + pref (clé `AudioManager_SfxVolume` inchangée) et ses `PlayOneShot` cessent de multiplier par `_volume` local ; `SfxPlayer._masterVolume` supprimé (le pool joue à volume relatif, le bus fait le reste) ; `AudioManager.SetMusicVolume` → bus + pref, ses fades internes (fade in/out, pause) se normalisent sur 0↔1 ; sliders train/vinyle inchangés (réglages fins par source, Ambiance sans master exposé). Duck : `JuiceDirector.BeginMusicDuck`/`EndMusicDuck` → `AudioBuses.TransitionToAim/Normal(_aimMusicDuckFadeSeconds)` ; suppression de la coroutine duck locale, du champ `_combatMusicSource` et de `AudioManager.DuckMusicForAim`/`RestoreMusicAfterAim` (zéro appelant résiduel après reroutage — précédent G4-P1 LaunchForce). `SfxPlayer.Awake` applique aussi le pref au bus (idempotent — couvre un lancement de scène combat sans SfxManager).

**Fichiers à créer** : `Scripts/Audio/AudioBuses.cs` · `Scripts/Editor/AudioRoutingAuditor.cs` (menu audit : conformité mixer + liste des AudioSources sans groupe + rapport `Audits/`) · `Scripts/Editor/AudioSceneRoutingBuilder.cs` (câblage CombatMusic, idempotent).
**Fichiers à modifier** : `SfxPlayer.cs`, `SfxManager.cs`, `AudioManager.cs`, `JuiceDirector.cs` (zones duck/tension uniquement), `TalsDropSystem.cs` (routage source uniquement).
**Interdits** : tout le reste — notamment `Scripts/Enemies/**`, `Scripts/Gameplay/Passives/**`, `CharacterBall`, `TurnManager`, `Game.unity` (dans ce prompt).

**Tests F1-P1** : (1) slider SFX baisse TOUT — hits de combat inclus (le bug) ; (2) à sliders = 1, loudness identique à avant (groupes à 0 dB, volumes par appel inchangés) ; (3) duck de visée : ressenti identique à l'A/B (−13 dB / 0.35 s) ; (4) Tals suit le bus SFX × son slider ; (5) prefs persistent après restart ; (6) mixer absent (test de robustesse éditeur) = aucun crash, sons au comportement actuel ; (7) `AudioRoutingAuditor` : zéro source non routée en scène Game après builder.

### F1-P2 — Banque sonore v1 + outillage d'import (détaillé après validation P1)

Périmètre : `AudioImportPostprocessor` (mono forcé, Vorbis ~q70, Decompress On Load < 200 Ko, dossiers `Audio/SFX/Combat/<famille>/`), convention `sfx_<event>_<n>.wav`, liste de courses par famille (charte §2) sourcée Sonniss GDC / Kenney / CC0, remplacement du clip Epidemic Sound (lever gacha). Je préparerai une première sélection CC0 en zip ; Arthur complète/tranche à l'oreille. Les slots du futur catalogue pointent des clips — la banque se remplit sans re-code.

---

## 4. Gates suivants (périmètres charte §6 — détail à leur tour, jamais en avance)

### F2-P1 — FeedbackCatalog + CombatFeedbackService (VALIDÉE — Go du 02/08, 3 questions : oui / oui / oui ; prompt Cursor livré)

**Demande.** Le socle data-driven du chantier : un catalogue d'événements (SO) et un service runtime poolé avec les garde-fous génériques de la charte §3. **Nouveau code pur, dormant** — aucun re-câblage de l'existant (c'est F2-P2), aucun changement de comportement en jeu.

**Approche.**
- `FeedbackEventId` (enum, valeurs explicites) : la liste fermée charte §4, groupes A+B+C (~35 entrées). Tout ajout futur = avenant charte + entrée d'enum.
- `FeedbackBundle` [Serializable] : VFX (prefab ParticleSystem, `tintMode` None/Cause/Custom, attache Monde/SuitLaCible, échelle) · SFX (clips[], volumeScale, pitch min/max, **famille de voix** {Impacts, Statuts, Moments, UI}, cooldown ms) · shakeTrauma · hitstopMs · `hapticLevel` (réservé, consommé F4) · emphase 1–6 · `respectsReduceMotion` (consommé F5).
- `FeedbackCatalog` (ScriptableObject, `Data/Feedback/FeedbackCatalog.asset`) : entrées par défaut + **overrides par characterId** (D4). Index runtime construit une fois (tableaux par (int)eventId + dictionnaire d'overrides) — zéro LINQ, zéro alloc par appel. Event sans bundle = no-op silencieux (warning unique en éditeur).
- `CombatFeedbackService` (singleton de scène, instancié par builder en F2-P2) : `Play(FeedbackEventId, in FeedbackContext)` avec `FeedbackContext` struct {Position, Direction, Intensity01, Target, TargetBall, CharacterId}. Garde-fous dans l'ordre : cooldown par event → budget FX global (12 systèmes actifs, au-delà skip si emphase < 5) → familles de voix (Impacts 4 / Statuts 2 / Moments 2 / UI 1 ; famille pleine : **skip si emphase < 5, vol de la voix la plus ancienne de la même famille si ≥ 5**) → tirage clip + jitter pitch ±5 %. Shake → `CameraShake.AddTrauma` ; hitstop → `ctx.TargetBall?.ApplyHitStop` (seul porteur actuel). Compteurs de diagnostic (FX actifs, skips) exposés en éditeur.
- `FxPool` + `PooledFxReturner` : pool par prefab (gabarit GroundZoneSystem), retour via `OnParticleSystemStopped` (stopAction = Callback forcé au spawn), préchauffe au premier usage. Zéro alloc en régime stable.
- `CombatFeedbackPalette` : **seule modification d'existant** — ajout des couleurs charte §2 (BuffUp `#66B8FF`, DebuffDown `#B44DE6`, Shield `#7DE0FF`, Stun `#FFE066`, Freeze `#AEE9FF`, Heal `#4DFF66` centralisé).
- Éditeur : `FeedbackCatalogBuilder` (crée l'asset idempotent, pré-remplit une entrée par event, **branche d'office les 36 clips banque v0 sur les slots B/C par convention de nom**, crée `Prefabs/VFX/Feedback/FxPlaceholder.prefab` teintable branché sur 3–4 events pour les tests de pool, rapport `Audits/`) + `FeedbackCatalogAuditor` (events sans bundle, clips null, bornes, overrides orphelins — lecture seule).
- Dev harness : `[ContextMenu]` sur le service (« Jouer tous les events », « Spam hit ×20 ») pour valider caps et pool sans toucher au gameplay.

**Fichiers à créer** : `Scripts/Gameplay/Feedback/{FeedbackEventId, FeedbackContext, FeedbackBundle, FeedbackCatalog, CombatFeedbackService, FxPool, PooledFxReturner}.cs` · `Scripts/Editor/{FeedbackCatalogBuilder, FeedbackCatalogAuditor}.cs`. **À modifier** : `CombatFeedbackPalette.cs` uniquement. **Interdits** : JuiceDirector, SfxPlayer/SfxManager, CharacterBall, Enemy, toute scène, zones gelées G6.

**Dépendances** : F1 (le service joue via `SfxPlayer.Play` → bus SFX). Impact runtime : aucun tant que rien n'appelle `Play` (P1 dormant).

**Tests** : ContextMenu « tous les events » → 36 sons v0 audibles avec jitter ; 2ᵉ vague de FX sans `Instantiate` (log pool) ; spam 20 hits/200 ms → plafond Impacts=4 + cooldown respectés ; saturation Statuts → 3ᵉ son skip (emphase 2) vs steal (emphase forcée 5) ; 13 FX demandés → 12 actifs + skips comptés ; event vide → no-op + warning unique ; Profiler : 0 alloc/frame en spam stable.

**Questions au Go** : (1) enum V1 figé ~35 events, OK ? (2) politique famille pleine skip/steal ci-dessus, OK ? (3) pré-branchement des sons v0 sur les slots B/C dès le builder (F3 n'aura que le visuel à poser), OK ?
### F2-P2 — Re-câblage groupe A + hit-react allié (PROPOSITION du 03/08 — en attente de Go)

**Demande.** Le juice existant passe par les garde-fous du service (iso-ressenti), les bursts deviennent poolés, l'allié réagit corporellement quand il encaisse, le service entre en scène. Découpage en **deux passes** pour isoler le risque :

**F2-P2a — routage d'exécution (data inchangée, diff minimal).**
- `CombatFeedbackService` : deux API guidées basse-niveau pour les bundles **dynamiques** du JuiceDirector — `TryPlayGuardedSfx(famille, clip, volume, pitch, emphase)` (false si voix refusée) et `SpawnGuardedFx(prefab, pos, rot, scaleMul, emphase)` (null si budget refusé). Les **courbes restent code** (pitch/volume/hitstop ∝ dégâts, escalade, trauma) : le service n'applique que les plafonds. Au passage : correction du scale des FX (`restScale × scaleMul` au lieu de `one × scale` — note du contrôle F2-P1) + garde anti-doublon d'instance sur le singleton (pattern maison).
- `JuiceDirector` : ses appels `SfxPlayer.Play` → `TryPlayGuardedSfx` (Impacts : hits/rebonds/kill · Moments : super/finisher/defeat · UI : tick de zone) ; ses 3 `Instantiate` de bursts (impact/launch/death) → `SpawnGuardedFx` (pooling gagné, dette .cursorrules soldée). **Aucune valeur modifiée, champs sérialisés intacts.** La tension de visée (boucle) reste locale.
- `AllyHitReaction` (nouveau, portage EnemyHitReaction sans wind-up) : flash MPB + squash **neutre** (directionnel en F4 avec l'émetteur `EnemyHitAlly`). Matériau : statique partagé créé du shader `SpriteFlash` via `Shader.Find` (inclus au build par les ennemis) — zéro asset, zéro scène ; MPB par renderer. Anti-bruit aligné popups : seuil 5 dégâts + cooldown 100 ms. Attaché par `CharacterBallFactory` à l'instanciation (1 point de code, aucun prefab touché), câblé sur `VisualRenderer` existant.
- Builder scène `Câbler Feedback Scène Combat` : GO `CombatFeedbackService` + catalogue + CameraShake. **Commit scène séparé et annoncé.**
- Tests : A/B jeu normal **indistinguable** (lancer/super/rebonds/hit/crit/kill/finisher/défaite) ; kills en série → zéro `Instantiate` après préchauffe ; slider SFX toujours respecté ; allié touché → flash + squash, silence sur les ticks ≤ 5 PV ; spam extrême → plafonds charte (voulu, documenté).

**F2-P2b — migration data groupe A → catalogue (après validation P2a au ressenti).**
- Builder `Migrer JuiceDirector vers Catalogue` : lit les valeurs sérialisées de la scène (hit/crit/launch/super/bounce/kill/defeat clips, prefabs bursts, volumes/pitchs de base) → remplit les entrées A si vides. `JuiceDirector` lit ensuite le catalogue (réf sérialisée) pour clips/prefabs/bases — courbes toujours code. Purge des champs migrés au commit scène. A/B final. Bénéfice : la banque pro (F1-P2) alimentera aussi le combat via le catalogue, tuning en un seul asset.

**Fichiers (P2a)** : modifier `CombatFeedbackService.cs`, `JuiceDirector.cs` (zones d'exécution son/burst uniquement), `CharacterBallFactory.cs` (1 ajout) · créer `AllyHitReaction.cs`, `FeedbackSceneBuilder.cs` (éditeur). **Interdits** : `CharacterBall.cs`, `Enemy.cs` (G6c actif dessus), zones gelées habituelles.

**Questions au Go** : (1) découpage P2a/P2b (reco) ou fusion en une passe ? (2) plafonds de voix appliqués au groupe A dès P2a (reco : oui — c'est la charte, l'A/B se joue sur le jeu normal) ? (3) hit-react allié : seuil 5 + cooldown 100 ms + squash neutre (reco : oui) ?
- **F3** : briques d'événements manquantes (`OnBuffAdded/Removed` sur BuffReceiver, events shield allié/ennemi, hooks AllyDot/Stun/Freeze) puis les 4 moments de chaque état du groupe B, pastilles d'icônes. **Rendez-vous G6.**
- **F4** : groupe C (wind-up sonore, impact ennemi→allié…), `HapticManager` (D6), crit dramatique complet (ralenti unscaled dédié + zoom-punch), sting victoire (D8 : court, cède à la musique Hub, rétrogradable F5).
- **F5** : overrides actifs (D4), moments U1 avec Arthur, accessibilité réduire-mouvements, FeelProfile/tuning, passe perf APK. + Avenant budgets (A2) au premier gate jouable — probablement dès F2-P2.

---

## 5. Points ouverts

1. ~~`SfxPlayer._masterVolume`~~ — résolu 02/08 : valeur scène = 1, aucune compensation.
2. ~~Versionnage des docs~~ — résolu 02/08 : `Docs/Feedback/` (commit `e29a60a`).
3. ~~Zip CC0~~ — résolu 02/08 : banque v0 livrée (36 placeholders synthétisés calibrés + manifeste + liste de courses). La banque pro remplace fichier par fichier.
4. **Réserve F1-P1** : le groupe mixer est nommé « `Music ` » avec un **espace final** (YAML `m_Name: 'Music '`). Fonctionne aujourd'hui (`FindMatchingGroups` = recherche par sous-chaîne) mais piège latent pour tout lookup exact futur → renommer `Music` dans la fenêtre Audio Mixer, à glisser dans le prochain commit touchant le mixer.
5. Checklist in-game F1-P1 (points 1–6 et 8) : à dérouler par Arthur en Play Mode — condition de clôture pleine de F1.
6. **Metas SFX hors Combat retouchés par le postprocessor** (réimport, non commités) : reco = **commit hygiène dédié** (`chore(audio): normalisation import SFX hub`), APRÈS un A/B rapide des sons hub où la stéréo pouvait compter (`revealsound`, `risersound`, sons de porte) — `forceToMono` est la règle projet (charte §5.5) et économise la mémoire, mais on écoute avant de figer ; si un son perd vraiment, on actera une liste d'exceptions dans le postprocessor par avenant.

---

## 6. Journal d'exécution

| Date | Gate | Commit | Verdict |
|---|---|---|---|
| 02/08 | F0 — charte de feedback | — (doc) | **VALIDÉE v1.1** (amendements A1–A4) — versionnée `Docs/Feedback/` (`e29a60a`) |
| 02/08 | F1-P1 — mixer, AudioBuses, routage, fix slider | `ba7768f` | **VALIDÉ** — périmètre exact (14 fichiers, zéro scène), AudioBuses conforme (null-safe, warning unique, dB plancher −80), aucun double-master (PlayOneShot/managed/fades vérifiés ligne à ligne, fades normalisés sur `GetDuckedMusicVolume`), API duck legacy supprimées avec **0 appelant résiduel**, JuiceDirector touché uniquement sur les zones duck/tension. Zones gelées croisées respectées (G6a-P4, G6b-P1, G6b-P2 vérifiés clean). Note actée : en mode legacy (mixer absent) le master SFX est neutre — acceptable, asset committé. |
| 02/08 | F1-P1 — câblage scène | `ca8ceb5` | **VALIDÉ** — diff scène minimal (purge `_masterVolume`/`_aimMusicDuckMultiplier`/`_combatMusicSource` + `CombatMusic → Music` seuls), AimFocus **−12,72 dB** acté (tolérance vs −13 contractuel, inaudible), audit 1640 vert, `SfxManager` non routé en scène = attendu (routage Awake runtime). **Réserve mineure** : espace final dans le nom du groupe « Music » (point ouvert n°4). Checklist in-game à dérouler (point n°5). |
| 02/08 | F1-P2 — outillage import | `8cc1bcc` | **VALIDÉ** — périmètre exact (2 scripts éditeur + docs versionnés), postprocessor conforme (early-out hors `_Project/Audio`, SFX mono/Vorbis 0.7/seuil 200 Ko avec repli premier-import documenté, Music/Ambiance streaming stéréo), auditeur 20 slots/regex/hygiène en lecture seule. |
| 02/08 | F1-P2 — banque v0 | `46b2a7c` + `3d1681a` | **VALIDÉ** — 36 wav aux bons emplacements, metas vérifiés (mono, Vorbis 0.7, DecompressOnLoad), rapport 1658 : 20/20 slots, 0 violation, 0 stéréo. Hotfix `Combat.meta` conforme. **F1 CLOS** sous réserve de la checklist in-game F1-P1 (point ouvert n°5). |
| 02/08 | F2-P1 — Go + prompt | — (HEAD `6706de7`) | Go acté (enum figé · skip < 5 / steal ≥ 5 · pré-branchement v0). Signatures référencées revérifiées à HEAD (`ApplyHitStop`, `AddTrauma`, `SfxPlayer.Play`, `ImpactBurst.prefab`), zone gelée FX vérifiée sur `c382c66` (G6c-P1, clean). Prompt Cursor livré — attendu : 2 commits (code, puis assets générés + rapports), aucune scène. |
| 03/08 | F2-P1 — socle catalogue + service | `2d16892` + `5eca4a3` + `f7e62c7` | **VALIDÉ — F2-P1 CLOS.** Ordre des garde-fous conforme (cooldown → budget FX → familles skip/steal → shake/hitstop), hygiène Release complète (Stop+Clear, reparentage pool, restauration échelle, compteur protégé par OnDestroy), `Resolve` zéro-alloc (index tableau + ValueTuple), `PickClip` réservoir sans alloc, palette strictement additive (0 suppression), dormance vérifiée (0 appelant hors Feedback/Editor), audit 1909 conforme (40/40, 36 clips, 0 défaut), zone gelée OK sur `b59c026` (G6c fix). **2 notes à corriger en P2a** : scale VFX `one × mul` → `restScale × mul` ; garde anti-doublon manquante sur le singleton du service. |
