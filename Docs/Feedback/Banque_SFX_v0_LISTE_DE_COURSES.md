# Liste de courses — banque sonore pro (D2 : organique moderne, 0 €)

Objectif : remplacer les placeholders v0 fichier par fichier (mêmes noms), famille par famille. Priorité aux **médiums** (haut-parleurs de téléphone) ; 2–3 variations par événement fréquent.

## Sources (licences vérifiées, gratuites)

1. **Sonniss GDC** — `sonniss.com/gameaudiogdc` : bundles annuels (2015→2024, ~30 Go/an), licence royalty-free commerciale sans attribution. LA source « organique premium ». Télécharger 1–2 années récentes suffit largement.
2. **Kenney** — `kenney.nl/assets` (CC0) : *Impact Sounds* (impacts bois/métal/verre — parfait `shield_hit/break`), *RPG Audio* (potions/soins/pièces), *Interface Sounds* (`turn_relay`, UI), *Music Jingles* (base de stings victoire).
3. **freesound.org** — filtre licence **CC0 uniquement** (Creative Commons 0). Vérifier le sample rate (≥ 44,1 kHz) et l'absence de reverb baveuse.

## Requêtes par famille (mots-clés qui marchent)

| Slot | Chercher | Éviter |
|---|---|---|
| `heal` | "magic heal shimmer", "soft chime bloom", "fairy sparkle short" | nappes longues, église/choeur |
| `buff_up` / `debuff_down` | "whoosh riser short", "power up organic", "whoosh down dark" | lasers sci-fi, 8-bit |
| `shield_*` | "glass clink", "ice hit", "glass debris small", "metal tink muted" | verre brisé long (bouteille) |
| `burn_*` | "fire ignite small", "ember crackle close", "torch whoosh" | feu de camp en boucle |
| `poison_tick` | "bubble pop small", "liquid blub", "goo squish short" | cartoon boing |
| `stun_apply` | "dull thud ring", "bell muted hit" | gong long |
| `freeze_*` | "ice crack", "frost crystallize", "icicle break" | blizzard/vent |
| `enemy_windup` | "low riser short", "tension swell 0.5s", "sub whoosh rise" | risers EDM 4 s |
| `enemy_hit_ally` | "body thud low", "heavy punch sub", "impact dull" | claps/snares |
| `victory_sting` | "success jingle warm", "sting achievement short" | fanfares 8-bit |
| levier gacha (rempl. Epidemic) | "mechanical lever pull", "ratchet crank short" | — |
| `super_detonation` (couche sub) | "sub drop impact", "cinematic boom short" | booms 3 s |
| `crit` | "sword shing", "metal slice bright" | anime voice |

## Rappels d'intégration

Déposer dans `Assets/_Project/Audio/SFX/Combat/<famille>/` sous le nom exact du slot (`sfx_<event>_<n>.wav`) — l'`AudioImportPostprocessor` (F1-P2) applique mono/Vorbis/DecompressOnLoad automatiquement, et `Chez Arthur/Audio/Audit Banque SFX` vérifie nommage, familles manquantes et variations. Recadrer les silences de tête (< 5 ms avant le transitoire), pas de reverb longue (l'arène est petite), −3 dBFS peak.
