# PROMPT CURSOR — MT4-G1 : socle UGS (Auth anonyme + temps serveur → couture GameClock)

> Chez Arthur — Unity 2022.3, C#. `.cursorrules` strict. Base : `main` à `107e902` (ou HEAD).
> Contrat : `Cahier_Charges_Backend_MT4.md` v1.1 (Go UGS). **Offline-first non négociable** :
> le jeu démarre et tourne à l'identique sans réseau ; le backend est une amélioration, jamais une dépendance.
> Ce gate débloque MT2-G6.

## PÉRIMÈTRE — 4 MODIFIÉS + 1 CRÉÉ (+ manifest)

Modifiés : `Packages/manifest.json` · `Core/AppBootstrap.cs` · `Meta/GameClock.cs` · `Debug/DebugMenu.cs`
Créé : `Scripts/Backend/BackendService.cs` (nouveau module, namespace `ChezArthur.Backend`)
**RIEN D'AUTRE.** Interdits : save, saisons, missions, UI hub, `Feedback/**`, `UI/RevealStage/**`.

## 0. ÉTAPES MANUELLES ARTHUR (dashboard — AVANT de tester, APRÈS avoir collé le code)

1. Unity Editor → Project Settings → Services : créer/lier le projet Unity Cloud (organisation perso).
2. Dashboard Unity Cloud : activer **Authentication** et **Cloud Code**.
3. Cloud Code → Scripts → créer `GetServerTimeUtc` (JS), **Publish** :
```js
module.exports = async () => {
    return { utcNowMs: Date.now() };
};
```

## 1. `Packages/manifest.json`

Ajouter : `com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.cloudcode` — versions résolues par Unity (vérifiées compatibles 2022.3). Aucun autre package.

## 2. `Scripts/Backend/BackendService.cs`

Statique + hôte MonoBehaviour interne (GO caché `[BackendServiceHost]`, DontDestroyOnLoad, créé par `Initialize()`) pour le refresh au focus. Tout en **try/catch, jamais d'exception propagée, jamais bloquant**.

- `public static void Initialize()` — idempotent, appelé par AppBootstrap. Lance en async (fire-and-forget) : `UnityServices.InitializeAsync()` → `AuthenticationService.Instance.SignInAnonymouslyAsync()` (si pas déjà signé) → `SyncServerTime()`. Chaque étape logguée `[Backend]` (succès une ligne ; échec **un seul** LogWarning par session — pas de spam offline).
- État public lecture : `IsInitialized`, `IsSignedIn`, `PlayerId` (vide sinon), `HasServerTime`, `LastSyncUtc`.
- `public static async void SyncServerTime()` : appel Cloud Code `GetServerTimeUtc` (`CloudCodeService.Instance.CallEndpointAsync`) avec **timeout court (5 s)** ; succès → `GameClock.SetServerAnchor(serverUtc, Time.realtimeSinceStartupAsDouble)` + event `OnServerTimeSynced` ; échec → silencieux après le premier warning.
- Hôte : `OnApplicationFocus(true)` → si initialisé et > 5 min depuis le dernier sync → `SyncServerTime()`.
- **Aucune API du reste du jeu appelée** (pas de save, pas de saison) — G1 = plomberie pure.

## 3. `Meta/GameClock.cs` — ancre serveur (la couture MT2-D4 se ferme)

- Champs : `static DateTime _serverAnchorUtc; static double _serverAnchorRealtime; static bool _hasServerAnchor;`
- `public static void SetServerAnchor(DateTime serverUtc, double realtimeAtFetch)` + `public static bool HasServerTime => _hasServerAnchor;`
- **Ordre de résolution de `UtcNowGuarded`** (documenté en tête) :
  1. Override debug (voyage dans le temps) — inchangé, toujours prioritaire.
  2. **Ancre serveur** : `_serverAnchorUtc + TimeSpan.FromSeconds(Time.realtimeSinceStartupAsDouble − _serverAnchorRealtime)` — **immunisé contre les changements d'horloge device en cours de session** (ancré realtime, pas sur DateTime.UtcNow).
  3. Garde locale actuelle (plancher PlayerPrefs) — comportement existant intact.
- Le plancher PlayerPrefs continue d'être nourri par le temps résolu (une heure serveur vue = plancher pour les sessions offline suivantes).
- `UtcNow` brut : inchangé.

## 4. `Core/AppBootstrap.cs`

Dans `Initialize()` : `ChezArthur.Backend.BackendService.Initialize();` sous try/catch silencieux. Une ligne + commentaire (« jamais bloquant — offline-first »). Rien d'autre.

## 5. `Debug/DebugMenu.cs` — section « — BACKEND — »

Labels : init/signed (+ 8 premiers chars du PlayerId), server time synced O/N + heure serveur résolue vs heure device (delta en s), dernier sync. Boutons : `Re-sync temps serveur` · `Ré-init backend`. Null-safe (menu fonctionnel offline).

## GARDE-FOUS
Zéro appel réseau bloquant au boot (froid identique) · zéro throw (device sans réseau = expérience actuelle exacte) · l'override debug reste au-dessus de l'ancre serveur (tests MT2 intacts) · logs `[Backend]`/`[GameClock]` sobres (1 warning offline max) · aucun identifiant/donnée joueur envoyé hors Auth anonyme UGS.

## CHECKLIST (Arthur)
1. **Offline d'abord** (réseau coupé) : boot normal, temps de lancement inchangé, 1 seul warning `[Backend]`, garde locale active, run/saison/missions intacts.
2. **Online** : boot → logs init + sign-in (PlayerId au DebugMenu) + sync serveur (delta device affiché).
3. **Anti-triche réelle** : horloge Windows +2 h EN COURS de session → `UtcNowGuarded`/ids saison **stables** (ancre realtime) ; nouvelle session offline avec horloge +2 h → plancher cohérent.
4. **Interplay debug** : `+7 jours` (override) fonctionne toujours par-dessus l'ancre ; Clear → retour au temps serveur.
5. **Focus** : app en arrière-plan > 5 min → re-sync au retour (log).
6. **Dashboard** : script publié, appels visibles dans le dashboard Cloud Code.
7. **Smoke MT2** : page saison (compte à rebours cohérent avec l'heure serveur), rollover debug, crans, claims — zéro régression.
