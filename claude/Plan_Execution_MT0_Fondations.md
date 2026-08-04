# Plan d'exécution — MT0 Fondations

**Take Five Games — Track Zero** · 4 août 2026 · v1.1 (**G1 : diff contrôlé ✅ — checklist chez Arthur**)
Compagnon de `Audit_Preparatoire_MT0_Fondations.md` (vérité terrain `3700a40`, périmètre G1 revérifié intact à **`1151ae0`**) et du `Plan_Directeur_Chantiers_Meta_MT.md` v1.2.
Boucle par gate (méthode standard, non négociable) : audit ciblé à HEAD → proposition → **Go** → prompt Cursor → push → contrôle du diff ligne à ligne → checklist in-game → commit.
Coexistence : MT0 ne touche jamais `Feedback/**`, les handlers de contenu, ni `UI/InvocationFlow/**` (INV en cours). `Game.unity`/`Hub.unity` : commits séparés par chantier. **Vérifié le 04/08 au soir** : INV2 (`4cefaa5`, `a4bc455`, `c7edda8`) et F4-P1 (`faf733f`) ont avancé en parallèle — zéro intersection avec le périmètre G1 (`Core/Save*`, `Debug/DebugMenu`).

---

## 1. Gates

| Gate | Périmètre | État |
|---|---|---|
| **MT0-G1** | Save durcie (écriture atomique, backup, quarantaine, garde anti-écrasement) + chaîne de migration réelle + compléments debug (pity, give perso, export/import) | ✅ Go 04/08 · **push `4d024a5` · diff contrôlé ligne à ligne : VALIDÉ, zéro rejet** · checklist 9 points en cours côté Arthur |
| **MT0-G2** | Socle localisation FR/EN + écrans pilotes (Paramètres, Accueil) — migration de masse hors gate, étalée | après G1 |
| **MT0-G3** | Sémantique d'états assainie (solder les deux « Temporaire », doc d'architecture d'états) | après G2 |

---

## 2. Architecture G1 (implémentée à `4d024a5`)

**Fichiers touchés (conformes au périmètre fermé)** : `Core/SaveSystem.cs` (réécriture interne, API publique intacte) · `Core/SaveMigrator.cs` (nouveau) · `Debug/DebugMenu.cs` (section « SAVE / GACHA ») · `claude/mt0/` (prompt + fixtures, docs). **Zéro diff** sur `PersistentManager`, `CharacterManager`, `GachaManager`, `SaveData`, `Feedback/**`, `UI/InvocationFlow/**` — vérifié par `git show --stat`.

**Écriture atomique (chaîne de move)** : verrou → sérialisation → `save.json.tmp` → rotation `save.json` → `save.json.bak` → promotion. Try/catch par étape (variable `step` dans le message d'erreur), rollback minimal depuis `.bak`, jamais d'exception propagée.

**Chargement avec récupération** : parse OK → clear verrou → migration → nettoyage `tmp` orphelin. Parse KO → quarantaine `save.json.corrupt-<horodatage>` (collision gérée `-2`, `-3`…, fichiers jamais lus/supprimés par le code) → échec de quarantaine = **verrou anti-écrasement** (`Save()` refuse) → sinon tentative `.bak` → sinon save neuve. `save.json` absent → promotion du `tmp` s'il parse, sinon `.bak`, sinon première install. Le constat critique n°1 de l'audit (corruption → écrasement définitif) est **structurellement clos**.

**Chaîne de migration** : `saveVersion` lu avant tout usage ; cascade v0→v1 (copie `selectedTeamIds` → `teamPreset0` si presets vides, sans vider le legacy — filet `CharacterManager` conservé), v1→v2/v2→v3 points d'ancrage documentés, `NormalizeNulls` exhaustif (contrôlé champ à champ contre `SaveData`), stamp + log, gabarit v4 commenté. Idempotente.

**Debug « SAVE / GACHA »** : Export (SaveGame + copie horodatée + chemin loggé) · Import `save_import.json` (parse défensif → migration → `SaveSystem.Save` — passe par la promotion atomique, l'ancienne save part en `.bak` — → `LoadGame`, redémarrage conseillé) · Pity → seuil−1 (toutes bannières, borné ≥ 0, via `GetPityData`/`LoadPityData` publics) · Give perso (id + niveau clampé 1–99, parse défensif). Tout null-safe, `allBanners` auto-fill pattern existant.

**Comportements consignés au contrôle (non bloquants, acceptés)** :
1. Double-échec d'écriture (rotation OK puis promotion KO) → retour à l'état N−1 : perte du **delta d'une save**, jamais de tout (compromis assumé de la chaîne de move ; l'ancien code perdait tout).
2. `DeleteSave` en état verrouillé supprime aussi la preuve — assumé : c'est l'action de reset explicite de l'utilisateur, et l'unique porte de sortie propre du verrou.
3. « Give perso » sur un perso déjà possédé avec nv ≤ 1 → +1 niveau (sémantique doublon d'`AddCharacter`) — outil debug, statut loggé.

**Fixtures** (repo : `claude/mt0/`) : `fixture_save_v0.json` (pré-versioning réaliste, ids réels) · `fixture_save_corrompue.json` (JSON tronqué). Dossier device : `%LocalLow%\Take Five Games\Track Zero\`.

---

## 3. Journal

| Date | Gate | Verdict |
|---|---|---|
| 04/08 | Audit préparatoire MT0 (`3700a40`) | ✅ Acté par Arthur — 3 gates recadrés |
| 04/08 | G1 — Go | Proposition + prompt `Prompt_Cursor_MT0_G1.md` + 2 fixtures livrés. |
| 04/08 | G1 — push `4d024a5` | **Diff contrôlé ligne à ligne : VALIDÉ, zéro rejet.** Périmètre exact (3 fichiers code + docs mt0), spec respectée point par point, 3 comportements consignés (§2). **OK pour dérouler la checklist 9 points** (Export de la v3 réelle d'abord — point 9). Clôture du gate au verdict des tests. |
