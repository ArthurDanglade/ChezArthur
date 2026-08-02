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

- **F2-P1** : `FeedbackCatalog` (SO : eventId → bundle VFX/SFX/shake/hitstop/haptique/emphase/overrides) + `CombatFeedbackService` (pool générique gabarit GroundZoneSystem, plafonds §3, cooldowns, familles de voix) — nouveau code pur, sans re-câblage.
- **F2-P2** : groupe A re-câblé sur le catalogue (JuiceDirector devient consommateur, API publique conservée) + hit-react allié (flash MPB + squash, portage EnemyHitReaction) + conventions sorting. Critère : checklist de non-régression du feel à l'aveugle.
- **F3** : briques d'événements manquantes (`OnBuffAdded/Removed` sur BuffReceiver, events shield allié/ennemi, hooks AllyDot/Stun/Freeze) puis les 4 moments de chaque état du groupe B, pastilles d'icônes. **Rendez-vous G6.**
- **F4** : groupe C (wind-up sonore, impact ennemi→allié…), `HapticManager` (D6), crit dramatique complet (ralenti unscaled dédié + zoom-punch), sting victoire (D8 : court, cède à la musique Hub, rétrogradable F5).
- **F5** : overrides actifs (D4), moments U1 avec Arthur, accessibilité réduire-mouvements, FeelProfile/tuning, passe perf APK. + Avenant budgets (A2) au premier gate jouable — probablement dès F2-P2.

---

## 5. Points ouverts

1. Valeur scène actuelle de `SfxPlayer._masterVolume` : si ≠ 1, décision au contrôle du diff (compensation ou non).
2. Emplacement de versionnage des docs du chantier dans le repo (le dossier `claude/` est exclu des commits — racine comme la Bible, ou `Docs/`).
3. F1-P2 : Arthur veut-il le zip de première sélection CC0 préparé par Claude, ou sourcer lui-même sur la liste de courses ?

---

## 6. Journal d'exécution

| Date | Gate | Commit | Verdict |
|---|---|---|---|
| 02/08 | F0 — charte de feedback | — (doc) | **VALIDÉE v1.1** (amendements A1–A4) |
