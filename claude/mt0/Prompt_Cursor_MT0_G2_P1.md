# PROMPT CURSOR — MT0-G2-P1 : Socle localisation FR/EN + pilotes (Accueil, Paramètres)

> Contexte projet : Chez Arthur — Unity 2022.3 LTS, C#. Respect strict de `.cursorrules`
> (commentaires FRANÇAIS, noms ANGLAIS, `_camelCase` privés, docstrings `<summary>` FR,
> aucune dépendance externe, builders éditeur idempotents + Undo + rapport `Audits/`).
> Base : `main` à `fb1a5e5` (ou HEAD courant — `SettingsPanelUI`/`PageAccueilUI` vérifiés intacts).
> Principe acté : **le FR reste la source, en place** (code, scènes, SO) ; **l'EN est une table
> d'overlay** ; aucune clé sans défaut ; `Loc` ne throw JAMAIS.

## PÉRIMÈTRE

**Nouveaux fichiers runtime** — `Assets/_Project/Scripts/Localization/`, namespace `ChezArthur.Localization` :
1. `GameLanguage.cs` · 2. `Loc.cs` · 3. `LocalizationTable.cs` · 4. `LocalizationCatalog.cs` · 5. `LocalizedText.cs`

**Nouveaux fichiers éditeur** — `Assets/_Project/Scripts/Editor/` :
6. `LocalizationPilotBuilder.cs` · 7. `LocalizationAuditor.cs` · 8. `LocalizationCsvTool.cs`

**Fichier modifié** : 9. `Assets/_Project/Scripts/UI/SettingsPanelUI.cs` (sélecteur FR/EN uniquement)

**Assets/scènes** : créés/édités PAR LES BUILDERS uniquement (catalog, table, composants pilotes, boutons sélecteur). Aucune édition manuelle de scène dans ce prompt.

**NE TOUCHE À RIEN D'AUTRE.** Interdits : `PauseMenuUI`, `HubManager`, `PageAccueilUI.cs`,
`Feedback/**`, `UI/InvocationFlow/**`, tout SO de data, tout autre écran. Aucun renommage.

---

## 1. Runtime

### 1.1 `GameLanguage.cs`
```csharp
public enum GameLanguage { French = 0, English = 1 }
```

### 1.2 `Loc.cs` — statique, cœur du système
- Constantes : `PREF_LANGUAGE = "Loc_Language"` (PlayerPrefs — préférence device, frontière G1), `CATALOG_RESOURCE = "LocalizationCatalog"`.
- `CurrentLanguage` : lazy-init — PlayerPrefs si présent, sinon `Application.systemLanguage == SystemLanguage.French ? French : English` (persisté à la première résolution).
- `SetLanguage(GameLanguage lang)` : si différent → persiste PlayerPrefs + `PlayerPrefs.Save()` + invoque `OnLanguageChanged`.
- `public static event Action OnLanguageChanged;`
- Dictionnaire EN interne : lazy — `Resources.Load<LocalizationCatalog>(CATALOG_RESOURCE)` une seule fois ; catalog absent → **un seul** `LogWarning` `[Loc]` puis tout tombe en fallback FR (jamais de spam, jamais d'exception).
- `public static string Tr(string key, string frDefault)` : frDefault null → `""` ; si `CurrentLanguage == English` et que le dictionnaire contient `key` avec une valeur **non vide** → la retourner ; sinon frDefault. (Entrée EN vide = « à traduire » = fallback FR, silencieux.)
- `public static string Format(string key, string frDefaultPattern, params object[] args)` : `string.Format(Tr(key, frDefaultPattern), args)` sous try/catch `FormatException` → `LogError` `[Loc]` (clé en clair) + retour du pattern non formaté. Zéro throw.
- `public static string TrId(string prefix, string id, string field, string frFallback)` : clé par convention `$"{prefix}.{id}.{field}"` → `Tr(clé, frFallback)`. (Textes SO : zéro champ ajouté aux data — résolution au point d'affichage.)

### 1.3 `LocalizationTable.cs` — SO
`[CreateAssetMenu(fileName = "LocalizationTable", menuName = "Chez Arthur/Localization/Localization Table", order = 30)]`
`[Serializable] public class LocalizationEntry { public string key; [TextArea(1, 3)] public string english; }`
`List<LocalizationEntry> entries` + propriété lecture seule.

### 1.4 `LocalizationCatalog.cs` — SO
`[CreateAssetMenu(…, menuName = "Chez Arthur/Localization/Localization Catalog", order = 31)]`
`List<LocalizationTable> tables` + `BuildDictionary(Dictionary<string,string> into)` : merge de toutes les tables, clé dupliquée → `LogWarning` (dernière gagne), null-safe partout.

### 1.5 `LocalizedText.cs` — MonoBehaviour
- `[SerializeField] private string key;` + `[SerializeField] [TextArea(1, 3)] private string frDefault;`
- `Awake` : détecte la cible — `TMPro.TMP_Text` **ou** `UnityEngine.UI.Text` (`GetComponent` des deux, priorité TMP) ; aucune des deux → `LogWarning` unique et composant inerte.
- `OnEnable` : `Apply()` + abonnement `Loc.OnLanguageChanged` ; `OnDisable` : désabonnement.
- `Apply()` : `cible.text = Loc.Tr(key, frDefault);`
- `#if UNITY_EDITOR` : `public void EditorSetup(string newKey, string newFrDefault)` (utilisé par le builder ; ne re-key jamais silencieusement — c'est le builder qui décide).

---

## 2. `SettingsPanelUI.cs` — sélecteur FR/EN (seule modification)

- Nouveaux champs : `[Header("Langue")] [SerializeField] private Button frButton; [SerializeField] private Button enButton;`
- `Start()` (à la suite de l'existant) : listeners → `Loc.SetLanguage(French/English)` ; abonnement `Loc.OnLanguageChanged += RefreshLanguageButtons` ; appel initial `RefreshLanguageButtons()`.
- `OnDestroy()` (créer si absent) : désabonnement + `RemoveListener` des deux boutons.
- `RefreshLanguageButtons()` : bouton de la langue active → `Image.color` alpha 1, l'autre alpha 0,55 (null-safe ; pas d'autre feedback — le style visuel réel viendra du builder qui clone un bouton existant).
- `using ChezArthur.Localization;` ajouté. **Rien d'autre ne change dans ce fichier.**

---

## 3. Éditeur

### 3.1 `LocalizationPilotBuilder.cs` — `[MenuItem("Chez Arthur/Localization/Build Pilote (scène ouverte)")]`
Idempotent, Undo-safe (`Undo.RegisterCreatedObjectUndo` / `Undo.RecordObject`), rapport `Audits/localization_pilot_<scene>.txt`.

1. **Assets d'abord (si absents)** : crée `Assets/_Project/Data/Localization/Table_UI.asset` (LocalizationTable vide) et `Assets/_Project/Resources/LocalizationCatalog.asset` (référence Table_UI). Ne touche pas aux assets existants sinon pour ajouter les clés manquantes (étape 4).
2. **Détection de scène** : `PageAccueilUI` présent → passe « Accueil » (racine = son GameObject) ; `SettingsPanelUI` présent → passe « Paramètres » (racine = son GameObject). Aucun des deux → boîte de dialogue d'erreur, rien ne bouge.
3. **Passe labels** (commune) : pour chaque `TMP_Text`/`Text` sous la racine (enfants inactifs inclus) — ignorer si texte vide/blanc, purement numérique, ou déjà porteur d'un `LocalizedText` (dans ce cas : mettre à jour `frDefault` seulement si la clé existante correspond au même objet ; **jamais de re-key**). Sinon : ajouter `LocalizedText`, `frDefault` = texte actuel, clé = `ui.accueil.<slug>` / `ui.settings.<slug>` (slug = nom du GameObject en minuscules, non-alphanumérique → `_` ; collision → suffixe `_2`, `_3`…).
4. **Alimentation table** : chaque clé générée absente de `Table_UI` y est ajoutée avec `english = ""` (feuille de traduction prête). Jamais de suppression, jamais d'écrasement d'un `english` non vide.
5. **Passe Paramètres uniquement — sélecteur** : si `frButton`/`enButton` du `SettingsPanelUI` sont null : créer une ligne « Langue » dans le panel (parent = le transform du `SettingsPanelUI` ou son conteneur de boutons existant) ; **cloner `restartButton` comme gabarit visuel** s'il est assigné (purger ses persistent listeners via `SerializedObject` sur `m_OnClick`), labels « FR » / « EN » ; sinon fallback boutons minimaux (pattern DevMenuUI). Binder les deux refs sérialisées du `SettingsPanelUI` via `SerializedObject`. Idempotent : refs déjà assignées → ne rien recréer.
6. Rapport : composants ajoutés/mis à jour, clés générées, clés ajoutées à la table, sélecteur créé ou déjà présent. `EditorSceneManager.MarkSceneDirty`.

### 3.2 `LocalizationAuditor.cs` — `[MenuItem("Chez Arthur/Localization/Audit Couverture")]` — lecture seule
Scanne les `.cs` de `Assets/_Project/Scripts` (hors `Editor/`) par regex pour les clés littérales `Loc.Tr("…"` / `Loc.Format("…"` + collecte toutes les clés des `LocalizationTable` du projet et tous les `LocalizedText` de la scène ouverte. Rapport `Audits/localization_coverage.txt` : clés référencées, présentes en table, `english` rempli vs vide (% de couverture EN), liste des manquantes. Zéro mutation.

### 3.3 `LocalizationCsvTool.cs` — `[MenuItem("Chez Arthur/Localization/Export CSV")]` + `Import CSV`
- Export : `SaveFilePanel` → CSV **UTF-8 avec BOM**, séparateur `;` (Excel FR), colonnes `key;english`, champs entre guillemets avec échappement `""`. Toutes les tables du catalog, triées par clé.
- Import : `OpenFilePanel` → parse défensif (mêmes règles) ; met à jour `english` des clés existantes, ajoute les clés inconnues à `Table_UI`, ne supprime jamais ; rapport console : mises à jour / ajoutées / ignorées (lignes invalides).

---

## GARDE-FOUS RÉCAPITULATIFS
- `Loc` ne throw jamais, ne log qu'avec parcimonie (1 warning catalog absent, erreurs Format par clé).
- Aucun champ ajouté à un SO de data ; aucun écran hors pilotes modifié ; scène éditée uniquement par le builder.
- Diff attendu : 8 fichiers créés + 1 modifié (+ assets/scènes générés par builder, committés séparément par scène — règle de coexistence).
- Logs préfixés `[Loc]` / `[LocalizedText]` / `[LocalizationBuilder]`.

## CHECKLIST DE TEST (Arthur, après application + builders sur les 2 scènes)
1. **Builder Hub** puis **builder Game** : rapports `Audits/` présents, re-exécution = zéro nouveau changement (idempotence).
2. Remplir 4–5 entrées EN via **Export CSV → édition → Import** (ex. « Lancer la Run » → « Start Run », « Paramètres » → « Settings ») — la chaîne CSV est validée au passage.
3. **Bascule à chaud** : en jeu, panel Paramètres → EN : les labels traduits des deux pilotes basculent immédiatement, les non-traduits restent FR (fallback), zéro troncature en portrait.
4. **Persistance** : redémarrage → langue conservée ; premier lancement (PlayerPrefs vierge, `DeleteAll` en Editor) → FR si système FR.
5. **Fallback** : une clé sans EN → FR affiché, zéro log d'erreur, listée par l'**Audit Couverture** (rapport committé).
6. **Écrans non migrés** : strictement identiques (collection, missions, combat…).
7. **Non-régression smoke** : run, gacha, missions claim, Boss Rush, pause/resume.
