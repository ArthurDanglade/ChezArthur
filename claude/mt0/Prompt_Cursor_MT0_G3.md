# PROMPT CURSOR — MT0-G3 : sémantique d'états assainie (décision A)

> Chez Arthur — Unity 2022.3, C#. `.cursorrules` strict (commentaires FR, noms EN).
> Base : `main` à `d444334` (ou HEAD). **PÉRIMÈTRE : 2 FICHIERS, RIEN D'AUTRE.**
> Interdits : tout autre fichier, toute scène, tout renommage, toute « amélioration » opportuniste.

## 1. `Assets/_Project/Scripts/Core/GameManager.cs`

1. Remplacer la ligne commentaire « Temporaire : démarrage en Playing… » et le défaut :
   `private GameState _currentState = GameState.Menu;`
2. Ajouter en tête de classe (dans le `<summary>` existant, à la suite) la doc d'architecture :
   *« Machine d'états de COMBAT (scène Game). Au Hub, l'état reste Menu — aucun système du
   Hub ne la lit. Playing est posé par RunManager.StartRun à l'entrée de run ; Paused/Victory/
   Defeat par les UI et le flux de run. »*

## 2. `Assets/_Project/Scripts/Core/RunManager.cs`

1. Remplacer le commentaire « Temporaire : démarre automatiquement la run pour le prototype »
   par la règle actée : *« Règle actée (MT0-G3) : la scène Game EST une run — le chargement de
   la scène démarre la run. Le mode (Normal/BossRush) vient de PendingRunMode. »*
   L'appel `StartRun()` dans `Start()` reste tel quel.
2. **Garde anti double-start** en tête de `StartRun()` :
   ```csharp
   // Garde : une run déjà en cours ne se redémarre pas par accident.
   if (_currentState == RunState.InProgress)
   {
       Debug.LogWarning("[RunManager] StartRun ignoré : run déjà en cours.");
       return;
   }
   ```
   ATTENTION : le « Restart run » du DebugMenu et `RestartAtStage` doivent continuer de
   fonctionner — vérifier leur chemin : s'ils repassent par `StartRun()` après avoir remis
   l'état à `NotStarted`/`Defeat`, rien à faire ; s'ils appellent `StartRun()` sur une run
   `InProgress`, remettre `_currentState = RunState.NotStarted` juste avant leur appel
   (dans RunManager uniquement — ne pas toucher DebugMenu.cs).

## CHECKLIST (Arthur)
1. `grep -rn "Temporaire" Assets/_Project/Scripts/Core/` = 0 résultat.
2. Hub → Lancer Run : drag fonctionnel dès l'étage 1 (log `[GameManager] État : Menu → Playing`).
3. Boss Rush : idem, mode consommé.
4. Pause/resume, bonus, Gare, sacrifice, défaite → retour Hub → re-run : zéro régression.
5. DebugMenu « Restart run » et « Restart à l'étage N » : fonctionnels (un seul démarrage, log de garde si double appel).
