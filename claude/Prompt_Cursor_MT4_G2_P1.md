# PROMPT CURSOR — MT4-G2-P1 : cloud save (plomberie + politique de conflits MT4-D2)

> Chez Arthur — Unity 2022.3, C#. Base : `main` à `68bf31f` (ou HEAD). Offline-first non négociable.
> **Scoping honnête** : l'identité anonyme UGS est PAR INSTALLATION — le vrai « changer de téléphone »
> arrive en P2 (liaison Google). P1 livre : la plomberie complète, le backup continu, la politique de
> conflits opérationnelle (testable via debug), sur laquelle P2 n'aura qu'à brancher la liaison.
> Politique MT4-D2 : **auto si évident, dialogue joueur si ambigu, jamais d'écrasement silencieux du côté riche.**

## PÉRIMÈTRE — 3 MODIFIÉS + 2 CRÉÉS (+ manifest)

Modifiés : `Packages/manifest.json` (+`com.unity.services.cloudsave`) · `Backend/BackendService.cs` · `Core/PersistentManager.cs` (2 points d'accroche) · `Debug/DebugMenu.cs`
Créés : `Backend/CloudSaveSync.cs` · `Backend/SaveConflictDialog.cs`
**RIEN D'AUTRE.** Interdits : `SaveSystem`/`SaveMigrator` (INTOUCHÉS — la save locale reste souveraine), saisons, UI hub (le dialogue est construit runtime), scènes.

(Voir fichier Downloads pour checklist 7 pts complète.)
