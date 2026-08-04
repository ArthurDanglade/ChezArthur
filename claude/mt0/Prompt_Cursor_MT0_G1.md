# PROMPT CURSOR — MT0-G1 : Save durcie + chaîne de migration + compléments debug

> Contexte projet : Chez Arthur — Unity 2022.3 LTS, C#. Respect strict de `.cursorrules`
> (commentaires FRANÇAIS, noms ANGLAIS, `_camelCase` privés, PascalCase méthodes,
> docstrings `<summary>` FR, aucune dépendance externe, pas d'initiative hors périmètre).
> Base de travail : `main` à `1151ae0` (ou HEAD courant — les fichiers du périmètre sont inchangés depuis `3700a40`).

## PÉRIMÈTRE — EXACTEMENT 3 FICHIERS

1. `Assets/_Project/Scripts/Core/SaveSystem.cs` — réécriture interne (API publique conservée à l'identique : `CURRENT_SAVE_VERSION`, `Save(SaveData)`, `Load()`, `SaveExists()`, `DeleteSave()`).
2. `Assets/_Project/Scripts/Core/SaveMigrator.cs` — **nouveau fichier**, namespace `ChezArthur.Core`.
3. `Assets/_Project/Scripts/Debug/DebugMenu.cs` — ajout d'UNE section « SAVE / GACHA » + un champ sérialisé, sans toucher aux sections existantes.

**NE TOUCHE À RIEN D'AUTRE.** Interdits explicites : `SaveData.cs`, `PersistentManager.cs`,
`CharacterManager.cs`, `GachaManager.cs`, `Feedback/**`, `UI/InvocationFlow/**`, handlers de
contenu, scènes. Aucun renommage, aucune « amélioration opportuniste ».

---

## 1. `SaveSystem.cs` — durcissement

### 1.1 Constantes et état interne (privés)

```csharp
private const string SAVE_FILE_NAME = "save.json";
private const string TMP_SUFFIX = ".tmp";
private const string BAK_SUFFIX = ".bak";
private const string CORRUPT_PREFIX = "save.json.corrupt-";
/// <summary> True si une save illisible n'a pas pu être mise en quarantaine :
/// toute écriture est alors refusée pour préserver la preuve. </summary>
private static bool _saveBlockedUntilQuarantine;
```

Chemins dérivés de `Application.persistentDataPath` comme aujourd'hui.

### 1.2 `Save(SaveData data)` — écriture atomique par chaîne de move

Ordre strict, chaque étape dans le try global existant, logs `[SaveSystem]` précis par étape :

1. **Verrou** : si `_saveBlockedUntilQuarantine` → `Debug.LogError` explicite (« écriture refusée : save corrompue non quarantinée ») et `return`. On ne touche pas le disque.
2. `data.saveVersion = CURRENT_SAVE_VERSION;` puis `JsonUtility.ToJson(data, true)`.
3. Écrire le JSON dans `save.json.tmp` (`File.WriteAllText`).
4. Rotation backup : si `save.json` existe → supprimer `save.json.bak` s'il existe, puis `File.Move(save.json → save.json.bak)`.
5. Promotion : `File.Move(save.json.tmp → save.json)`.
6. `catch` : `LogError` avec l'étape en clair ; **rollback minimal** : si `save.json` absent et `save.json.bak` présent → tenter `File.Move(bak → save.json)` (dans un try silencieux). Jamais d'exception propagée.

### 1.3 `Load()` — récupération et quarantaine

Algorithme exact :

1. **`save.json` existe** → lire + `JsonUtility.FromJson<SaveData>` dans un try :
   - **Parse OK** (résultat non null) → `_saveBlockedUntilQuarantine = false;` → `SaveMigrator.MigrateToCurrent(data);` → log chargement → `return data;`
   - **Échec** (exception ou null) → `LogError` → **quarantaine** : `File.Move(save.json → save.json.corrupt-<yyyyMMdd-HHmmss>)` (si le nom existe déjà, suffixer `-2`, `-3`…) :
     - Si le move de quarantaine **échoue** → `_saveBlockedUntilQuarantine = true;` → `LogError` (« preuve non préservée, écritures gelées ») → `return new SaveData();`
     - Si la quarantaine **réussit** → tenter `save.json.bak` : lire + parser dans un try ; parse OK → `LogWarning` (« restauré depuis backup ») → migration → `return` ; échec ou absent → `LogError` (« aucune récupération possible ») → `return new SaveData();`
2. **`save.json` absent** (crash possible en pleine écriture, ou première install) :
   - `save.json.tmp` existe et parse → `File.Move(tmp → save.json)` + `LogWarning` (« promotion du fichier temporaire ») → migration → `return`.
   - Sinon `save.json.bak` existe et parse → `LogWarning` (« restauré depuis backup ») → migration → `return` (le prochain Save réécrira `save.json`).
   - Sinon → log « aucune sauvegarde » → `return new SaveData();`
3. Un `save.json.tmp` orphelin restant après un chargement réussi de `save.json` : le supprimer silencieusement (résidu d'une écriture interrompue avant rotation).
4. Les fichiers `save.json.corrupt-*` ne sont **jamais** lus, modifiés ni supprimés par le code.

`SaveExists()` et `DeleteSave()` : comportement conservé (`DeleteSave` supprime aussi `.tmp` et `.bak`, **pas** les `.corrupt-*`).

---

## 2. `SaveMigrator.cs` — nouveau fichier

```csharp
namespace ChezArthur.Core
{
    /// <summary>
    /// Chaîne de migration du schéma de sauvegarde. Appelé par SaveSystem.Load
    /// AVANT tout usage des données. Chaque étape est idempotente.
    /// </summary>
    public static class SaveMigrator
```

- `public static bool MigrateToCurrent(SaveData data)` — retourne true si une migration a eu lieu :
  1. Garde null → false.
  2. `int from = data.saveVersion;` si `from >= SaveSystem.CURRENT_SAVE_VERSION` → normalisation des nulls seulement (voir 4) → false.
  3. Étapes en cascade : `if (from < 1) MigrateV0ToV1(data); if (from < 2) MigrateV1ToV2(data); if (from < 3) MigrateV2ToV3(data);`
  4. `NormalizeNulls(data)` : toute liste null → liste vide, toute string null → `""` (protège des saves éditées à la main).
  5. `data.saveVersion = SaveSystem.CURRENT_SAVE_VERSION;` + `Debug.Log($"[SaveMigrator] Migration v{from} → v{SaveSystem.CURRENT_SAVE_VERSION}");` → true.
- **`MigrateV0ToV1`** : si tous les `teamPreset0..4` sont vides ET `selectedTeamIds` non vide → copier `selectedTeamIds` dans `teamPreset0` et `activePresetIndex = 0`. **Ne pas vider `selectedTeamIds`** (la branche legacy de `CharacterManager.LoadFromSaveData` reste le filet de sécurité — ceinture et bretelles, comportement final identique).
- **`MigrateV1ToV2` / `MigrateV2ToV3`** : corps vides, docstring FR : « Champs additifs uniquement (records, missions, Boss Rush, hint) — défauts de type corrects par construction. Historique exact non tracé : étape conservée comme point d'ancrage. »
- En fin de classe, **gabarit commenté** pour v4 : rappel des 3 règles (incrémenter `CURRENT_SAVE_VERSION`, ajouter `MigrateV3ToV4`, documenter le changement de schéma dans la docstring).

---

## 3. `DebugMenu.cs` — section « SAVE / GACHA »

- **Un champ sérialisé ajouté** au bloc « Données (auto-remplies en Editor si vides) » :
  `[SerializeField] private List<BannerData> allBanners = new List<BannerData>();`
  auto-rempli en Editor via le même helper `AssetDatabase.FindAssets` que `allValises`/`allItems`/`allEnemies` (pattern existant, `using` conditionnel déjà en place). Ajouter le `using ChezArthur.Gacha;` nécessaire.
- **Une méthode `DrawSaveGachaSection()`**, appelée dans `DrawPanel` après `DrawCheatsSection()` ; même style GUILayout que les sections existantes, titre `"— SAVE / GACHA —"`. Tout est null-safe (`PersistentManager.Instance` peut être absent en scène de test).

Contenu :

1. **`Export save`** : `PersistentManager.Instance.SaveGame();` puis copier `save.json` vers `save_export_<yyyyMMdd-HHmmss>.json` (même dossier) ; statut + `Debug.Log` du chemin complet.
2. **`Import save_import.json`** : si `<persistentDataPath>/save_import.json` absent → statut explicite. Sinon : parser dans un try (`JsonUtility.FromJson<SaveData>`) ; échec → statut « fichier invalide, rien touché » ; succès → copier le contenu vers `save.json` **via `SaveSystem`** (écrire le fichier importé par `File.Copy(import, save.json, overwrite:true)` est interdit : passer par la promotion atomique — le plus simple : `SaveMigrator.MigrateToCurrent(parsed)` puis `SaveSystem.Save(parsed)`) → `PersistentManager.Instance.LoadGame()` → statut « importé — redémarrage conseillé pour les managers de scène » + log.
3. **`Pity → seuil-1 (toutes bannières)`** : garde `PersistentManager.Instance?.Gacha` ; pour chaque `BannerData` non null de `allBanners` : `pity[banner.Id] = banner.PityThreshold - 1` (borné ≥ 0) ; construire le dictionnaire complet depuis `Gacha.GetPityData()` puis `Gacha.LoadPityData(dict)` ; `SaveGame()` ; statut avec le nombre de bannières traitées. **Zéro modification de `GachaManager`.**
4. **`Give perso`** : deux champs texte (id, niveau — `GUILayout.TextField`, parse int défensif) + bouton : `Characters.AddCharacter(id)` (gère inconnu → statut) puis si niveau > 1 : `GetOwnedCharacter(id).level = Mathf.Clamp(niveau, 1, CharacterData.MAX_LEVEL);` + `SaveGame()` ; statut. Commentaire FR : écriture directe du niveau assumée (outil debug).

---

## GARDE-FOUS RÉCAPITULATIFS

- API publique de `SaveSystem` inchangée (aucun appelant ne doit être modifié).
- Aucun champ ajouté/retiré/renommé dans `SaveData`.
- Logs préfixés `[SaveSystem]` / `[SaveMigrator]` / `[DebugMenu]`.
- Aucune allocation en boucle chaude (tout ceci est du code froid — pas de contrainte perf, mais pas de LINQ dans `Save`/`Load` par cohérence maison).
- La section debug ne référence `AssetDatabase` que sous `#if UNITY_EDITOR` (pattern existant).
- Diff attendu : ~2 fichiers modifiés + 1 créé. Tout diff hors périmètre sera rejeté au contrôle.

## CHECKLIST DE TEST (exécutée par Arthur après push)

Dossier save Windows : `%USERPROFILE%\AppData\LocalLow\Take Five Games\Track Zero\`

1. **Migration v0** : poser `fixture_save_v0.json` renommée en `save.json` → lancer → log `[SaveMigrator] Migration v0 → v3` ; collection = Goat 12 / Ardacula 5 / Troplin 3 ; équipe preset 1 = ces 3 persos ; tals 1250, best stage 17 → jouer 30 s → rouvrir `save.json` : `saveVersion: 3`, presets remplis.
2. **Corruption** : poser `fixture_save_corrompue.json` renommée en `save.json` (sans `.bak` présent) → lancer → `save.json.corrupt-*` créé, partie neuve, log d'échec de récupération → jouer 1 min → vérifier que le `.corrupt-*` n'a pas bougé (taille/contenu).
3. **Backup** : partie saine → provoquer 2 saves (ex. régler un volume + drag équipe) → `save.json.bak` présent → corrompre `save.json` à la main (tronquer) → relancer → log « restauré depuis backup », progression intacte, ancien fichier en quarantaine.
4. **Crash d'écriture simulé** : copier `save.json` → `save.json.tmp`, supprimer `save.json` → relancer → log « promotion du fichier temporaire », progression intacte.
5. **Export/Import** : Export → fichier horodaté présent ; modifier la partie (dépenser des tals) → copier l'export en `save_import.json` → Import → état revenu, log redémarrage conseillé → redémarrer → état conforme.
6. **Pity** : `Pity → seuil-1` → un multi ×10 sur une bannière → SSR garanti présent.
7. **Give perso** : `goat` niveau `30` → visible en collection au niveau 30.
8. **Non-régression** : run complète (victoire d'étage + défaite), gacha ×10, claim de missions, Boss Rush, éveil — aucun comportement modifié.
9. **Round-trip v3 réelle** : sur ta save v3 : Export avant tests, Import après — état final identique à l'état exporté.
