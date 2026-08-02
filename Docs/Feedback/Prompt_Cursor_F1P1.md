# Prompt Cursor — F1-P1 : Mixer audio, routage des bus, fix du volume SFX combat

> **Chantier SFX/VFX — gate F1, partie 1.** Réf : `Charte_Feedback_Combat_F0.md` v1.1 (D5, D7), audit `c176092` (§6.1, 6.2, 6.5).
> **PRÉALABLE MANUEL OBLIGATOIRE (Arthur, ~5 min, avant d'appliquer ce prompt)** — l'API éditeur Unity ne permet pas de créer un AudioMixer par script :
> 1. Créer le dossier `Assets/_Project/Audio/Resources/` puis clic droit → Create → **Audio Mixer**, nommé exactement `MainMixer`.
> 2. Dans la fenêtre Audio Mixer : sous `Master`, créer 3 groupes enfants nommés exactement **`Music`**, **`Ambiance`**, **`SFX`**.
> 3. Sélectionner le groupe `Music` → clic droit sur son champ **Volume** dans l'Inspector → *Expose 'Volume (of Music)' to script*. Idem pour `SFX`. Puis dans la fenêtre Audio Mixer, panneau **Exposed Parameters** (en haut à droite) : renommer les deux params exactement **`MusicVolume`** et **`SfxVolume`**.
> 4. Snapshots : renommer le snapshot par défaut **`Normal`** ; en créer un second **`AimFocus`** ; dans `AimFocus`, mettre le Volume du groupe `Music` à **−13 dB** (tout le reste identique à Normal).
> Ne rien régler d'autre (tous les groupes restent à 0 dB dans `Normal`).

---

## DEMANDE

Faire du mixer l'unique master des volumes : le slider SFX du menu pause doit ENFIN piloter les sons de combat (aujourd'hui `SfxPlayer` ignore le réglage — bug confirmé), le duck musique de la visée passe par les snapshots, le son des Tals est chaîné sous le bus SFX. **Aucun changement de ressenti à sliders = 1** (les volumes/pitchs par appel ne bougent pas, les groupes sont à 0 dB).

## PÉRIMÈTRE — fichiers

**À créer :**
- `Assets/_Project/Scripts/Audio/AudioBuses.cs`
- `Assets/_Project/Scripts/Editor/AudioRoutingAuditor.cs`
- `Assets/_Project/Scripts/Editor/AudioSceneRoutingBuilder.cs`

**À modifier (uniquement les zones décrites) :**
- `Assets/_Project/Scripts/Audio/SfxPlayer.cs`
- `Assets/_Project/Scripts/Audio/SfxManager.cs`
- `Assets/_Project/Scripts/Audio/AudioManager.cs`
- `Assets/_Project/Scripts/Gameplay/JuiceDirector.cs` (zones duck/tension SEULEMENT)
- `Assets/_Project/Scripts/Gameplay/TalsDropSystem.cs` (routage de sa/ses AudioSource(s) SEULEMENT)

**INTERDIT — ne touche à rien d'autre.** En particulier : `Scripts/Enemies/**`, `Scripts/Gameplay/Passives/**`, `CharacterBall.cs`, `TurnManager.cs`, `SettingsPanelUI.cs` (il appelle déjà `SfxManager.SetVolume`, ne pas le modifier), toute scène (`Game.unity`, `Hub.unity` — le builder sera exécuté à part), tout asset. Aucun renommage d'API publique existante. Pas de nouveau singleton MonoBehaviour.

## SPÉCIFICATION

### 1. `AudioBuses.cs` — helper statique, unique point de vérité mixer

Classe statique `ChezArthur.Audio.AudioBuses`. Charge paresseusement `Resources.Load<AudioMixer>("MainMixer")` (cache statique). **Null-safe partout** : si le mixer est introuvable → `Debug.LogWarning` UNE SEULE FOIS (« [AudioBuses] MainMixer introuvable — volumes en mode legacy ») et toutes les méthodes deviennent no-op / retournent null : le jeu doit fonctionner exactement comme avant sans l'asset.

API (commentaires en français, structure `.cursorrules`) :
- `public static bool IsAvailable { get; }`
- `public static AudioMixerGroup MusicGroup { get; }` / `AmbianceGroup` / `SfxGroup` — via `FindMatchingGroups`, cachés.
- `public static void SetMusicVolume01(float v)` / `SetSfxVolume01(float v)` — conversion linéaire→dB : `v <= 0.0001f ? -80f : 20f * Mathf.Log10(v)`, appliquée via `SetFloat("MusicVolume"/"SfxVolume", db)`.
- `public static void TransitionToAim(float seconds)` / `TransitionToNormal(float seconds)` — `FindSnapshot("AimFocus"/"Normal").TransitionTo(seconds)`, snapshots cachés.
- Aucune allocation hors du premier accès. Aucun `Update`.

### 2. `SfxPlayer.cs` — le fix du bug

- Dans `Awake`, pour chaque source du pool : `src.outputAudioMixerGroup = AudioBuses.SfxGroup;` (si non null).
- **Supprimer le champ `_poolSize`… non — supprimer UNIQUEMENT `_masterVolume`** et son usage : `Play` devient `src.volume = Mathf.Clamp01(volume);` — le master, c'est le bus.
- Dans `Awake`, appliquer le pref au bus (idempotent, couvre un boot direct scène Game) : `AudioBuses.SetSfxVolume01(PlayerPrefs.GetFloat("AudioManager_SfxVolume", 1f));` — utiliser la constante de clé existante de `SfxManager` si accessible, sinon la déclarer en `const` locale identique.
- API publique `Play` / `PlayPitched` : signatures inchangées.

### 3. `SfxManager.cs`

- `Awake` : router `sfxSource` et `managedSfxSource` vers `AudioBuses.SfxGroup` ; appliquer le pref au bus (comme SfxPlayer).
- `PlaySfx(clip, volumeScale)` : `PlayOneShot(clip, clampedScale)` — **ne plus multiplier par `_volume`** (le bus s'en charge). Idem `PlayManagedSfx` : `volume = Mathf.Clamp01(volumeScale)`.
- `SetVolume(normalized)` : garde le clamp + la persistance PlayerPrefs (clé `AudioManager_SfxVolume` INCHANGÉE), et appelle en plus `AudioBuses.SetSfxVolume01(_volume)`. `CurrentVolume` inchangé (le slider s'initialise dessus).
- `ApplyVolumeToSources()` : simplifier — les sources restent à volume 1 (supprimer la logique de volume sur `managedSfxSource`).

### 4. `AudioManager.cs`

- `Awake` : router `_musicSource` → `MusicGroup`, `_trainSource` et `_vinylSource` → `AmbianceGroup` ; appliquer le pref musique au bus : `AudioBuses.SetMusicVolume01(savedMusic)`.
- `SetMusicVolume(volume)` : persiste le pref (clé inchangée) + `AudioBuses.SetMusicVolume01(v)` ; **la source ne porte plus le master** : hors fade, `_musicSource.volume = 1f`.
- Fades internes (`PlayMusic` premier démarrage, `FadeInMusic`, `FadeOutMusic`, `Update`) : normaliser sur 0 ↔ **1f** au lieu de 0 ↔ `musicVolume` (le champ `musicVolume` ne sert plus qu'à initialiser le pref legacy — le conserver sérialisé pour compat, commenter son rôle résiduel).
- `MusicVolume` (propriété lue par SettingsPanelUI) : doit retourner la valeur 0–1 courante du réglage (depuis le pref/valeur cachée, PAS depuis `_musicSource.volume`).
- **Supprimer `DuckMusicForAim` et `RestoreMusicAfterAim`** (leur seul appelant est JuiceDirector, rerouté ci-dessous — vérifier 0 appelant résiduel avant suppression).
- Volumes train/vinyle : INCHANGÉS (réglages fins par source, sliders du Hub intacts).

### 5. `JuiceDirector.cs` — zones duck/tension uniquement

- `BeginMusicDuck()` → corps remplacé par `AudioBuses.TransitionToAim(_aimMusicDuckFadeSeconds);`
- `EndMusicDuck()` → `AudioBuses.TransitionToNormal(_aimMusicDuckFadeSeconds);`
- **Supprimer** : le champ sérialisé `_combatMusicSource`, `_combatMusicDuckRoutine`, `_combatMusicVolumeBeforeDuck`, la coroutine `DuckAudioSourceRoutine` (0 appelant après reroutage). Conserver `_aimMusicDuckMultiplier` sérialisé avec un commentaire « obsolète — porté par le snapshot AimFocus (−13 dB) ; conservé pour référence de tuning » OU le supprimer aussi : choisis la suppression propre.
- `GetTensionSource()` : ajouter `_tensionSource.outputAudioMixerGroup = AudioBuses.SfxGroup;` à la création.
- **NE TOUCHE À RIEN D'AUTRE dans ce fichier** (bundles, hitstop, finisher, defeat, escalade : zone gelée).

### 6. `TalsDropSystem.cs`

- Au(x) point(s) de création/configuration de sa/ses `AudioSource` : assigner `AudioBuses.SfxGroup`. `PickupVolume`/slider fin : INCHANGÉS (ils s'empilent sur le bus — décision D7).

### 7. `AudioRoutingAuditor.cs` (éditeur, lecture seule)

`[MenuItem("Chez Arthur/Audio/Audit Routage Audio")]` — génère `Audits/AudioRouting_<yyyyMMdd_HHmm>.md` :
- Conformité mixer : asset présent dans Resources, groupes `Music/Ambiance/SFX`, params `MusicVolume`/`SfxVolume`, snapshots `Normal`/`AimFocus` (avec delta dB du groupe Music). Chaque manquant = ligne ❌ explicite.
- Scan des scènes ouvertes : liste toute `AudioSource` (objet, chemin hiérarchie) avec son `outputAudioMixerGroup` — celles à `null` en section « À router ».
- Zéro modification d'asset (idempotent, vérifiable au diff git).

### 8. `AudioSceneRoutingBuilder.cs` (éditeur, Undo-safe, idempotent)

`[MenuItem("Chez Arthur/Audio/Câbler Audio Scène Combat")]` — dans la scène ouverte : trouve le GameObject `CombatMusic`, assigne son `AudioSource.outputAudioMixerGroup = Music` (via le mixer chargé depuis Resources), `Undo.RecordObject` + `EditorSceneManager.MarkSceneDirty`. Re-exécution = aucun changement (log « déjà câblé »). Ne touche à rien d'autre dans la scène.
⚠️ **Ne PAS exécuter ce menu dans cette session Cursor** — l'exécution et le commit de `Game.unity` se font séparément (protocole de coexistence G6).

## CONVENTIONS (rappel .cursorrules)

Commentaires FRANÇAIS, noms ANGLAIS, structure de script standard (bandeaux CONSTANTES / SERIALIZED FIELDS / …), namespaces `ChezArthur.Audio` / éditeur sans namespace imposé mais sous `Scripts/Editor/`. Zéro alloc en hot path (AudioBuses tout en cache statique). Pas de `FindObjectOfType` dans Update. Pas de LINQ runtime.

## CHECKLIST DE TEST (après application + création manuelle du mixer)

1. Menu pause → baisser **SFX** : les hits de combat, rebonds, kill baissent (LE bug d'avant — c'était le test rouge).
2. Sliders à 1 : loudness identique à avant l'application (A/B de mémoire — groupes à 0 dB).
3. Visée Super Lancer : la musique (Hub ET CombatMusic une fois le builder passé) se duck comme avant (~×0.22, fondu 0.35 s), remonte au relâché/cancel.
4. Slider Tals : agit toujours, ET suit le slider SFX (multiplication).
5. Restart de l'app : tous les volumes persistent (mêmes clés prefs).
6. Test de robustesse : renommer temporairement `MainMixer` → un seul warning `[AudioBuses]`, aucun crash, sons audibles (mode legacy).
7. `Chez Arthur/Audio/Audit Routage Audio` : rapport vert sur le mixer ; seule `CombatMusic` apparaît « à router » tant que le builder n'est pas passé.
8. `git status` : AUCUN fichier de scène modifié par cette application.
