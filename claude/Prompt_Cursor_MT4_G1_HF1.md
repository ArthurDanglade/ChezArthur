# PROMPT CURSOR — MT4-G1-HF1 : le temps serveur doit dominer un plancher empoisonné

> Défaut attrapé au contrôle de diff `ebac2f3`. Scénario : session offline avec horloge device avancée
> (+2 ans) → plancher PlayerPrefs empoisonné → sync serveur ultérieure pose l'ancre VRAIE →
> `UtcNowGuarded` compare l'heure serveur au plancher empoisonné et **retourne le plancher** :
> la vérité serveur perd contre une triche locale antérieure. Les saisons peuvent défiler avec un backend actif.

## PÉRIMÈTRE — 1 SEUL FICHIER : `Assets/_Project/Scripts/Meta/GameClock.cs`

1. **`UtcNowGuarded`** : quand `_hasServerAnchor` est vrai, **ne jamais appliquer le clamp plancher**
   (l'ancre serveur est autoritaire ; le bloc `floorTicks` de recul ne s'applique qu'au chemin device).
   L'alimentation du plancher (écriture) reste active dans les deux cas (sessions offline suivantes).
2. **`SetServerAnchor`** : après avoir posé l'ancre, **écraser le plancher avec `serverUtc.Ticks`**
   (`PlayerPrefs` + `Save()`) — une ancre serveur guérit un plancher empoisonné. Log une ligne si
   l'ancien plancher était supérieur (« plancher corrigé par le temps serveur »).
3. Commentaire de tête mis à jour : « ordre : override debug > ancre serveur (autoritaire, ignore le
   plancher en lecture, le réécrit) > device + plancher ».

**RIEN D'AUTRE.**

## CHECKLIST
1. Repro du défaut d'abord SANS le fix impossible post-fix — donc : offline, horloge +2 ans, lancer (plancher pollué, Dump/labels debug le montrent), remettre l'heure, online → sync → **heure résolue = heure serveur** (delta ≈ 0 au DebugMenu), plancher réécrit à la valeur serveur (relancer offline : plancher sain).
2. Anti-recul device toujours actif SANS ancre (offline, horloge −1 h → warning + gel plancher).
3. Override debug toujours prioritaire (+7 jours par-dessus l'ancre).
4. Smoke : boot online/offline, page saison, rollover debug.
