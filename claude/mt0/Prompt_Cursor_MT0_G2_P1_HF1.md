# PROMPT CURSOR — MT0-G2-P1-HF1 : boutons FR/EN — purge des LocalizedText clonés

> Hotfix du contrôle de diff `8632725`. Base : `main` à `9a3497e` (ou HEAD courant).
> Bug confirmé : le clonage de `restartButton` comme gabarit du sélecteur a **copié le
> `LocalizedText` de son label** (clé `ui.settings.text_tmp`, frDefault « RECOMMENCER »)
> sur les labels de `BtnLangFR` et `BtnLangEN`. À l'exécution, `OnEnable → Apply()`
> écrasera « FR » / « EN » par « RECOMMENCER ». 7 LocalizedText dans la scène au lieu de 5.

## PÉRIMÈTRE — UN SEUL FICHIER

`Assets/_Project/Scripts/Editor/LocalizationPilotBuilder.cs`

**NE TOUCHE À RIEN D'AUTRE.** Ni runtime, ni scènes à la main (la correction de scène passe
par la ré-exécution du builder), ni `SettingsPanelUI`, ni quoi que ce soit hors MT0.

## MODIFICATIONS

1. **Branche création (clone du gabarit)** : juste après le clonage et la purge des
   persistent listeners, **supprimer tout `LocalizedText`** présent sur le clone et ses
   enfants (`GetComponentsInChildren<LocalizedText>(true)` → `Undo.DestroyObjectImmediate`),
   AVANT d'écrire le texte du label (« FR » / « EN »). Compter et reporter.
2. **Branche « sélecteur déjà présent » (self-heal idempotent)** : si `frButton`/`enButton`
   sont déjà bindés, exécuter quand même une passe de réparation : supprimer tout
   `LocalizedText` trouvé sous leurs transforms (même mécanique Undo), ré-asserter les
   textes de labels « FR » / « EN ». Zéro trouvé → zéro changement (idempotence).
3. **Rapport** : nouvelles lignes « LocalizedText parasites supprimés : N (BtnLangFR/EN) »
   dans `Audits/localization_pilot_game.txt`. `MarkSceneDirty` uniquement si N > 0 ou création.

## GARDE-FOUS
- Les 5 `LocalizedText` légitimes du pilote Paramètres ne sont jamais touchés (la passe de
  réparation est strictement scoping `frButton`/`enButton` et leurs enfants).
- Conventions `.cursorrules` (commentaires FR, Undo-safe, idempotent).

## CHECKLIST (Arthur)
1. Appliquer le prompt → **ré-exécuter le builder sur la scène Game** → rapport : « supprimés : 2 ».
2. Diff de scène attendu : **uniquement** le retrait des 2 blocs `LocalizedText` (+ leurs
   entrées `m_Component`) sous BtnLangFR/BtnLangEN. Commit scène séparé (lane MT0).
3. Play : les boutons affichent « FR » / « EN », la bascule fonctionne, le label
   RECOMMENCER du vrai bouton Restart se localise normalement.
4. Ré-exécuter le builder une 3e fois → rapport « supprimés : 0 », zéro diff (idempotence).
5. Enchaîner ensuite CSV + checklist 7 points du gate, inchangés.
