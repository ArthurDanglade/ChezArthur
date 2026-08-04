# Plan d'exécution — MT0 Fondations

**Take Five Games — Track Zero** · 4 août 2026 · v1.2 (**G1 : CLOS 9/9** · **G2 : proposition soumise au Go**)
Compagnon de `Audit_Preparatoire_MT0_Fondations.md` et du `Plan_Directeur_Chantiers_Meta_MT.md` v1.2.
Boucle par gate : audit ciblé à HEAD → proposition → **Go** → prompt Cursor → push → contrôle du diff ligne à ligne → checklist in-game → commit.
Coexistence : MT0 ne touche jamais `Feedback/**`, les handlers de contenu, ni `UI/InvocationFlow/**`. Scènes : commits séparés par chantier. Parallélisme vérifié en continu (INV2, F4-P1, revert `a052507` : zéro intersection avec MT0).

---

## 1. Gates

| Gate | Périmètre | État |
|---|---|---|
| **MT0-G1** | Save durcie + chaîne de migration + compléments debug | ✅ **CLOS 04/08** — code `4d024a5`, checklist **9/9**, docs `0226173` |
| **MT0-G2** | Socle localisation FR/EN + écrans pilotes (Accueil, Paramètres) | **Proposition soumise au Go** (§3) |
| **MT0-G3** | Sémantique d'états assainie (les deux « Temporaire ») | après G2 |

---

## 2. G1 — CLOS (archive)

Architecture livrée à `4d024a5` : écriture atomique par chaîne de move (tmp → bak → promotion, étape en clair dans les erreurs), quarantaine `corrupt-*` avec collision gérée, verrou anti-écrasement, promotion tmp/bak au chargement, `SaveMigrator` (cascade v0→v3, `NormalizeNulls` exhaustif, gabarit v4), section debug SAVE/GACHA (export/import, pity seuil−1, give perso). API publiques intactes, zéro diff hors périmètre.
**Checklist 9/9 OK** (migration v0, corruption sans bak, restauration .bak, promotion .tmp, export/import, pity, give, smoke, round-trip v3 réelle).
Comportements consignés : delta N−1 sur double-échec d'écriture · `DeleteSave` = porte de sortie du verrou (preuve supprimée, assumé) · give sur possédé = +1 (sémantique doublon).
**À surveiller (hors G1, non reproduit)** : un gel du Hub constaté une fois en Editor après le test 3 (restauration .bak), disparu au Stop/Play suivant. Si ça revient : suspecter l'enchaînement LoadGame → events `OnDataChanged` pendant l'init de scène.

---

## 3. G2 — Proposition (audit ciblé du 04/08 au soir, HEAD `0226173`)

### 3.1 Vérité terrain texte

Stack **mixte** : TMP 3.0.7 (115 fichiers) **et** `UnityEngine.UI.Text` legacy (65 fichiers). Fonts latines (LiberationSans SDF + Alphapix pixel) → EN sans risque de glyphes. **Aucun package de localisation** installé. Les textes joueur vivent à **trois endroits** : ~292 sites d'assignation runtime dans le code (dont templates interpolés, ex. missions `$"Terminer l'univers de {…}"`), ~550 labels posés dans les scènes (Hub 260, Game 294), et les **SO** (CharacterData : nom + 4 TextArea ; PassiveData ; MissionData displayName/description ; valises, items, bannières, ennemis…). Accueil : labels 100 % scène. Panel Paramètres : menu pause, scène Game (le Hub n'a pas d'accès settings — noté pour MT5). `Resources/` déjà utilisé par le projet (3 dossiers).

### 3.2 Choix d'outillage — reco : **système maison léger** (pas com.unity.localization)

Motifs : 2 langues seulement ; culture projet « zéro dépendance, builders idempotents » ; l'essentiel du texte vit dans des SO custom qu'Unity Localization ne couvre pas sans refonte de schéma ; le package impose son infra (locales, tables, workflow) pour un gain nul à cette échelle. Le système maison tient en ~5 scripts et reste remplaçable (l'API `Loc.Tr` isole tout).

### 3.3 Architecture proposée (G2-P1 = socle + pilotes)

**Principe : le FR reste la source, en place** (code, scènes, SO) ; **l'EN est une table d'overlay** ; aucune clé sans défaut.

Nouveau `Scripts/Localization/` (namespace `ChezArthur.Localization`) :
- `GameLanguage` (French/English) ; défaut premier lancement : langue système = FR → FR, sinon EN.
- `Loc` (statique) : `CurrentLanguage` persistée **PlayerPrefs** (préférence device — frontière G1 respectée), event `OnLanguageChanged`, `Tr(key, frDefault)` (EN via table, sinon frDefault), `Format(key, frDefaultPattern, args)` pour les templates, `TrId(prefix, id, field, frFallback)` pour les textes SO — **clé par convention** `"passive.{id}.name"` résolue au point d'affichage : **zéro champ ajouté aux SO, zéro migration de data**.
- `LocalizationTable` (SO, entrées key→english) + `LocalizationCatalog` (SO unique référençant les tables, chargé par `Resources.Load` — pratique déjà présente dans le projet).
- `LocalizedText` (MonoBehaviour) : clé + frDefault capturé du texte de scène, gère TMP **et** Text legacy (adapter interne), réagit au changement de langue.

Éditeur (3 outils, pattern maison) : builder pilote idempotent (pose `LocalizedText` sur les labels des panels Accueil + Paramètres, capture les frDefault, clés `ui.accueil.*` / `ui.settings.*`, rapport `Audits/`) · `LocalizationAuditor` lecture seule (clés code vs table EN : manquantes listées, couverture chiffrée) · import/export **CSV** de la table (pour traduire confortablement, et plus tard sous-traiter).

**Pilotes** : page Accueil (Hub.unity) + panel Paramètres du menu pause (Game.unity) + **sélecteur FR/EN ajouté au panel Paramètres** (`SettingsPanelUI.cs`, style existant). Bascule à chaud.

**Hors G2-P1 (dette suivie, lots P2 étalés)** : les ~292 sites code, ~550 labels scène, textes SO — chaque lot avec checklist visuelle. L'auditeur mesure la couverture à chaque lot.

### 3.4 Critères de test G2-P1

1. Bascule FR↔EN à chaud sur les deux pilotes : tous les labels changent, zéro troncature en portrait.
2. Redémarrage → langue conservée.
3. Clé EN manquante → FR affiché (fallback), listée par l'auditeur, zéro crash.
4. Écrans non migrés : strictement intacts.
5. Rapport auditeur committé dans `Audits/`.
6. Non-régression : run, gacha, missions (textes dynamiques FR intacts), Boss Rush.

**Périmètre fichiers pressenti** : `Scripts/Localization/` (5 nouveaux) + 3 scripts éditeur + `SettingsPanelUI.cs` (sélecteur) + assets catalog/table + `Hub.unity`/`Game.unity` via builder (commits scène séparés). Rien d'autre.

---

## 4. Journal

| Date | Gate | Verdict |
|---|---|---|
| 04/08 | Audit préparatoire MT0 (`3700a40`) | ✅ Acté — 3 gates recadrés |
| 04/08 | G1 — Go | Prompt + 2 fixtures livrés |
| 04/08 | G1 — push `4d024a5` | Diff contrôlé ligne à ligne : VALIDÉ, zéro rejet ; 3 comportements consignés |
| 04/08 | **G1 — CLOS** | **Checklist 9/9 OK** (Arthur). Docs plans committés `0226173`. Note « Hub figé ×1 en Editor » consignée à surveiller (§2). |
| 04/08 | G2 — audit ciblé + proposition | §3 — **en attente du Go d'Arthur** (reco : système maison, FR source + table EN overlay, pilotes Accueil/Paramètres + sélecteur) |
