# PROMPT CURSOR — MT4-G2-P2 : liaison Google Play Games (« liez votre compte », MT1-D3)

> Chez Arthur — Unity 2022.3, C#. Base : `main` à `5655e91` (ou HEAD). Vérifié web 15/08/2026 (docs Unity Auth).
> **Android uniquement** (Editor : UI inerte proprement). La promesse livrée ici : lier le compte anonyme →
> réinstallation / nouveau téléphone → se reconnecter via Google → **même PlayerId, cloud restauré** (P1 fait le reste).
> Prérequis techniques : GPGS **plugin v11.01+** (PGS v2), Unity Auth SDK ≥ 2.1.0 (déjà 3.6.1 ✓).

## §0 — ÉTAPES MANUELLES ARTHUR (avant terrain ; le code peut être collé avant)

1. **Play Console** : créer l'app (package id définitif !) → **Play Games Services v2** : configuration → **credentials** :
   - credential **Android** : SHA-1 de la **clé d'upload** ET (si Play App Signing actif) SHA-1 de la **clé de signature Google** — les deux ;
   - credential **Web App** (OAuth) : récupérer **Client ID + Client Secret** ;
   - ajouter ton compte Google en **testeur** PGS ; **publier** la config PGS (draft = NONAUTHENTICATED classique).
2. **Unity Dashboard** : Authentication → ID Providers → **Google Play Games** → coller Web Client ID + Secret.
3. **Unity Editor** : importer le **GPGS plugin v11+** (.unitypackage release officielle GitHub) → External Dependency Manager : Resolve des dépendances Android.
4. Build signé avec la clé d'upload enregistrée (un APK debug non enregistré = échec d'auth silencieux — piège n°1).

## PÉRIMÈTRE — 4 MODIFIÉS + 1 CRÉÉ

Modifiés : `Backend/BackendService.cs` · `Backend/CloudSaveSync.cs` (fix ciblé §3) · `UI/SettingsPanelUI.cs` · `Debug/DebugMenu.cs`
Créé : `Editor/AccountRowBuilder.cs` (ligne « Compte » du panel Paramètres)
(+ le dossier du plugin GPGS importé — hors revue, vendor.)
**RIEN D'AUTRE.** `SaveSystem`/`SaveMigrator`/saisons/missions : intouchés.

## 1. `BackendService.cs` — flux de liaison (`#if UNITY_ANDROID` autour du GPGS ; API toujours présente, no-op ailleurs)

- État : `IsGoogleLinked` (depuis `AuthenticationService.Instance.PlayerInfo` identities, rafraîchi après sign-in/link) · event `OnAccountStateChanged`.
- `LinkWithGoogleAsync()` : `PlayGamesPlatform.Instance.Authenticate` → succès → `RequestServerSideAccess(true, code)` (**code à usage unique — jamais mis en cache**) → `AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(code)` → log + event. Échecs : log sobre + état `LinkError` lisible par l'UI.
- **`AccountAlreadyLinked`** (ce Google est déjà lié à un AUTRE PlayerId — cas réinstallation/2e device) : proposer la bascule — `SignOut` (sans clear session token ? NON : sign-out propre) → nouveau `Authenticate`+code → `SignInWithGooglePlayGamesAsync(code)` → **PlayerId du compte lié récupéré** → enchaîner `CloudSaveSync.CompareAndResolveAsync()` : la politique MT4-D2 et le dialogue de conflit P1 gèrent la suite tels quels (réutilisation, zéro code nouveau de résolution). La bascule est confirmée par l'UI AVANT le sign-out (dialogue : « Ce compte Google possède déjà une sauvegarde — s'y connecter ? Votre partie anonyme actuelle restera sur cet appareil »).
- `UnlinkGoogleAsync()` : **debug uniquement** (pas d'UI joueur v1 — consigné).

## 2. `SettingsPanelUI.cs` + `AccountRowBuilder.cs`

- Refs : `accountStatusText` (TMP) + `linkButton`. `RefreshAccount()` : non lié → statut `Loc.Tr("ui.compte.non_lie", "Compte non lié — progression sur cet appareil uniquement")` + bouton « Lier à Google » actif (Android) / grisé avec `(appareil uniquement)` (Editor) ; lié → `Loc.Format("ui.compte.lie", "Lié à Google ({0})", nom joueur PGS si dispo sinon "compte lié")` + bouton masqué. Abonné `OnAccountStateChanged`, désabonnements OnDestroy. Clic → confirmation légère (2e tap) → `LinkWithGoogleAsync`.
- Builder : ligne « Compte » ajoutée au SettingsPanel (Game.unity) sous la ligne Langue — **gabarit cloné, purge listeners + LocalizedText (leçon HF1)**, refs bindées `SerializedObject`, clés Loc ajoutées à `Table_UI` (`english=""`), idempotent, rapport `Audits/account_row_build.txt`. **Scène propre avant exécution ; commit scène séparé.**

## 3. `CloudSaveSync.cs` — fix ciblé (note terrain P1, consignée)

Dans la comparaison : si `fingerprint` diffèrent mais **résumés équivalents** (mêmes ownedCount/tals/bestStage/bestScoreThisSeason) → **auto, pas de dialogue** : le plus récent gagne (`serverUploadUtcTicks` vs `lastPlayedUtcTicks` local), log `[Cloud] Divergence mineure — dernier écrivain retenu`. Le dialogue reste réservé aux VRAIES ambiguïtés. Rien d'autre ne change.

## 4. `DebugMenu.cs` — CLOUD/COMPTE

Labels : lié O/N + identities. Boutons : `Lier (device)` · `Basculer vers compte lié` (force le chemin AccountAlreadyLinked) · `Unlink (QA)` (double confirmation).

## GARDE-FOUS
Auth code jamais loggé ni stocké · sign-out uniquement dans le flux de bascule confirmé par le joueur · Editor = zéro appel GPGS (compilation `#if UNITY_ANDROID`, UI inerte propre) · offline : bouton Lier grisé si non signé UGS · tout no-throw.

## CHECKLIST (Arthur — APK signé, compte testeur PGS)
1. **Editor** : compile, panel Paramètres montre la ligne Compte grisée « (appareil uniquement) », zéro erreur GPGS.
2. **Device — liaison** : Paramètres → Lier → UI Google → statut « Lié » ; DebugMenu : même PlayerId qu'avant la liaison (le lien N'A PAS changé d'identité).
3. **La promesse** : Force upload → désinstaller/réinstaller (ou Clear data) → boot (nouveau PlayerId anonyme) → Paramètres → Lier → `AccountAlreadyLinked` → dialogue de bascule → confirmation → sign-in Google → **PlayerId d'origine récupéré** → compare cloud → pull (ou dialogue MT4-D2 si local non vierge) → progression restaurée.
4. **Fix résumés équivalents** : deux saves aux mêmes chiffres, fingerprints différents → pas de dialogue, dernier écrivain retenu (log).
5. Unlink QA → re-lier → OK. Offline : bouton grisé. Smoke : cloud P1 (5 pts rapides), saisons, gacha.
6. Suite : ajouter à `G2Suite` les cas **Editor-safe** seulement (résumés équivalents → auto ; état UI inerte) — le reste = MANUAL device, consigné dans la suite.
