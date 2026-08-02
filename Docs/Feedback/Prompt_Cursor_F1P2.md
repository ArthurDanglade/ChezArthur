# Prompt Cursor — F1-P2 : Outillage d'import de la banque sonore

> **Chantier SFX/VFX — gate F1, partie 2.** Réf : `Docs/Feedback/Charte_Feedback_Combat_F0.md` v1.1 (§3 familles de voix, §5.5 règles audio), plan §3.
> Contexte : la banque v0 (36 placeholders calibrés, zip fourni par Claude) sera déposée dans `Assets/_Project/Audio/SFX/Combat/{etats,ennemis,moments}/`, puis remplacée fichier par fichier par la banque pro (Sonniss/Kenney/CC0). Ce prompt livre l'outillage qui rend ce flux **zéro-friction et auto-contrôlé**. Aucun code runtime.

---

## DEMANDE

Deux scripts éditeur : un `AssetPostprocessor` qui impose les réglages d'import audio du projet (mono, Vorbis, chargement adapté à la taille), et un auditeur de banque qui vérifie nommage, familles et variations contre la charte.

## PÉRIMÈTRE — fichiers

**À créer :**
- `Assets/_Project/Scripts/Editor/AudioImportPostprocessor.cs`
- `Assets/_Project/Scripts/Editor/AudioBankAuditor.cs`

**INTERDIT** : tout le reste. Aucun fichier runtime, aucune scène, aucun asset audio existant retouché à la main (le postprocessor s'appliquera naturellement aux prochains réimports). Ne pas toucher `Scripts/Enemies/**`, `Scripts/Gameplay/**`, `Scripts/Audio/**`.

## SPÉCIFICATION

### 1. `AudioImportPostprocessor.cs` — `AssetPostprocessor`, `OnPreprocessAudio`

S'applique **uniquement** aux assets sous `Assets/_Project/Audio/` (early-out sinon). Règles par sous-chemin :

**`/SFX/`** :
- `forceToMono = true`, `loadInBackground = false`, `ambisonic = false`.
- `AudioImporterSampleSettings` (défaut) : `compressionFormat = Vorbis`, `quality = 0.7f`, `sampleRateSetting = PreserveSampleRate`.
- `loadType` : `DecompressOnLoad` si le fichier source fait **moins de 200 Ko** (`FileInfo` sur `assetPath`), sinon `CompressedInMemory`.

**`/Music/` et `/Ambiance/`** :
- `forceToMono = false`, `loadType = Streaming`, `Vorbis quality = 0.65f`, `PreserveSampleRate`.

Implémentation : dans `OnPreprocessAudio`, caster `assetImporter` en `AudioImporter`, appliquer `defaultSampleSettings` (struct : lire, modifier, réassigner). Pas de `SetOverrideSampleSettings` par plateforme en V1 — le défaut suffit (Android prioritaire). Commentaire d'en-tête : « Le postprocessor fait foi sur les réglages d'import audio du projet — ne pas régler à la main dans l'Inspector. »

### 2. `AudioBankAuditor.cs` — `[MenuItem("Chez Arthur/Audio/Audit Banque SFX")]`, lecture seule

Rapport Markdown `Audits/AudioBank_<yyyyMMdd_HHmm>.md` (même pattern que `AudioRoutingAuditor`) :

1. **Inventaire** par sous-dossier de `Audio/SFX/Combat/` : nom, taille (Ko), mono effectif (via `AudioImporter.defaultSampleSettings` + `forceToMono`), loadType. Utiliser `AssetDatabase.FindAssets("t:AudioClip", …)`.
2. **Nommage** : chaque fichier doit matcher `^sfx_[a-z0-9]+(_[a-z0-9]+)*_[0-9]+$` — violations listées ❌.
3. **Couverture charte** : table des slots attendus avec compteur de variations trouvées. Liste des slots (constante dans le script) : `heal`, `buff_up`, `debuff_down`, `shield_gain`, `shield_hit`, `shield_break`, `burn_apply`, `burn_tick`, `poison_tick`, `stun_apply`, `freeze_apply`, `freeze_end`, `enemy_windup`, `enemy_hit_ally`, `turn_relay`, `victory_sting`, `spec_switch`, `summon_spawn`, `zone_place`, `zone_cross`. Slot absent = ❌ ; 1 seule variation sur un slot fréquent (`heal`, `buff_up`, `debuff_down`, `burn_tick`, `poison_tick`, `enemy_hit_ally`) = ⚠️.
4. **Hygiène** : stéréo résiduelle en SFX ❌, clip > 1 Mo hors `victory_sting` ⚠️, extension ≠ `.wav`/`.ogg` ⚠️.
5. Zéro écriture d'asset (vérifiable au diff git après exécution).

## CONVENTIONS

`.cursorrules` : commentaires FRANÇAIS, noms ANGLAIS, bandeaux de structure, `#if UNITY_EDITOR` + dossier `Scripts/Editor/`, zéro LINQ runtime (LINQ toléré en éditeur mais rester sobre).

## SÉQUENCE D'INTÉGRATION (après application du prompt)

1. Appliquer ce prompt → compiler → commit code (`feat(audio): F1-P2 outillage import + audit banque`).
2. Dézipper `sfx_banque_v0.zip` : déposer son dossier `SFX/` dans `Assets/_Project/Audio/` (fusion avec l'existant — les nouveaux sous-dossiers `Combat/etats|ennemis|moments` se créent).
3. Laisser Unity importer (le postprocessor s'applique), puis `Chez Arthur/Audio/Audit Banque SFX` → rapport attendu : 36 clips, 20/20 slots couverts, 0 violation de nommage, 0 stéréo.
4. Commit contenu séparé (`feat(audio): F1-P2 banque SFX v0 placeholders + rapport`).

## CHECKLIST DE TEST

1. Réimporter un clip SFX existant (clic droit → Reimport) : réglages forcés (mono, Vorbis 0.7, DecompressOnLoad si < 200 Ko) visibles dans l'Inspector.
2. Les clips `Music/` restent stéréo + Streaming après Reimport.
3. Rapport d'audit : sections inventaire/nommage/couverture/hygiène présentes, verdict conforme au point 3 ci-dessus.
4. Double exécution de l'audit = deux rapports identiques (hors horodatage) ; `git status` propre côté assets.
