# PROMPT CURSOR — MT2-G6 / MT4-G5 : saisons « live » (bornes serveur + rotation sync)

> Base : `main` à `82a8c3a` (ou HEAD). Acquis G1 MT4 : `UtcNowGuarded` résout déjà l'heure serveur
> quand synchronisé (ancre autoritaire) — **la rotation et les bornes suivent DÉJÀ le serveur en ligne**.
> G6 = un durcissement ciblé + la validation live. Heure officielle : lundi 00h00 Europe/Paris (MT2-D9), déjà l'ancrage des ids.

## PÉRIMÈTRE — 3 FICHIERS

1. **`Meta/GameClock.cs`** : `public static bool HasTrustedTime => _debugOverrideUtc.HasValue || _hasServerAnchor;` (l'override debug = temps de confiance pour les tests — documenté).
2. **`Meta/SeasonProgressManager.cs`** — `EnsureSeasonCurrent()` : la branche **rollover** (id différent) ne s'exécute que si `GameClock.HasTrustedTime` ; sinon `Log [Season] « rollover différé — temps de confiance indisponible (offline) »` et return (la saison affichée reste l'ancienne ; le score continue de s'y accumuler ; le rollover s'appliquera à la prochaine session synchronisée — les entitlements seront calculés à ce moment, rien n'est perdu). La branche « première init » reste sans condition.
3. **`Hub/Pages/SeasonPageUI.cs`** : indicateur discret si `!GameClock.HasTrustedTime` — une ligne sous le compte à rebours : `Loc.Tr("ui.saison.offline", "Hors ligne — progression locale, synchronisation à la reconnexion")`. Masqué sinon. (+ clé ajoutée à `Table_UI` à la main par toi ou au prochain builder run — pas de builder dans ce gate.)

**RIEN D'AUTRE.** (Le DebugMenu affiche déjà l'état backend/ancre — suffisant.)

## CHECKLIST LIVE (Arthur — Editor + APK device réel)

1. **Editor online** : boot → sync → `HasTrustedTime` vrai → rollover debug (+42 j, override = trusted) fonctionne comme avant — zéro régression MT2 (suites re-run : SeasonIntegritySuite + G1Suite).
2. **Editor offline** (réseau coupé, pas d'override) : saison affichée intacte, indicateur « hors ligne » sur la page, `+42 j` d'horloge **Windows** (pas l'override) → **rollover différé** (log), rien ne bouge → réseau rétabli → sync → `Check rollover` → rollover s'applique, récap correct, entitlements complets.
3. **APK device réel** : boot online → heure serveur (delta au DebugMenu ≈ 0 vs vraie heure) → page saison : compte à rebours vers le vrai lundi 00h00 Paris → mode avion en session → temps stable (ancre realtime) → retour réseau → re-sync au focus.
4. **Rotation partagée** : Editor + device online simultanés → même univers en position 1, même semaine, même fin de saison (bornes identiques à la seconde près).
5. Smoke : runs, score, crans, claims, missions, Boss Rush — inchangés.

**5/5 = MT2 rail LIVE clos** (G6/MT4-G5 soldés — le classement phase 2 reste volontairement dormant jusqu'au seuil de population, v2 §12).
