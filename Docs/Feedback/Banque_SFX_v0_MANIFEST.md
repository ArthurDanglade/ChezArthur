# Banque SFX v0 — manifeste

**Provenance : 100 % synthèse procédurale** (générée le 02/08/2026, aucune licence tierce — libre d'usage total).
**Rôle : placeholders calibrés**, pas la banque finale. Chaque fichier respecte le slot exact de la Charte Feedback Combat v1.1 (nom, famille de voix, durée, enveloppe, mono 44,1 kHz, peak −3 dBFS, fades 5 ms) : le câblage F2/F3 se fait dessus, et la banque pro (voir `LISTE_DE_COURSES.md`) les remplace **fichier par fichier, sans re-code** (même nom = swap).

Arborescence cible dans Unity : `Assets/_Project/Audio/SFX/Combat/<famille>/` — le dossier `SFX/` de ce zip se dépose tel quel dans `Assets/_Project/Audio/`.

## etats/ (famille de voix « statuts » — plafond 2 voix, cooldown ~120 ms)

| Événement (charte §2/§4) | Fichiers | Durée | Intention sonore |
|---|---|---|---|
| `heal_received` | `sfx_heal_1..3` | ~0,55 s | Shimmer chaud, accord majeur doux montant |
| `buff_applied` | `sfx_buff_up_1..2` | ~0,3 s | Whoosh montant + micro-chime |
| `debuff_applied` | `sfx_debuff_down_1..2` | ~0,3 s | Whoosh descendant sombre |
| `shield_gained` | `sfx_shield_gain_1..2` | ~0,26 s | Clink cristallin (cloche FM) |
| `shield_absorbed` | `sfx_shield_hit_1..2` | ~0,12 s | « Tok » mat — jamais le son de dégâts |
| `shield_broken` | `sfx_shield_break_1..2` | ~0,38 s | Verre granulaire + corps grave |
| `burn_applied` | `sfx_burn_apply_1..2` | ~0,3 s | Flare soufflé + amorce de crépitement |
| `burn_tick` | `sfx_burn_tick_1..2` | ~0,16 s | Crépitement bref |
| `poison_tick` | `sfx_poison_tick_1..2` | ~0,16 s | Blub descendant |
| `stun_applied` | `sfx_stun_apply_1..2` | ~0,3 s | Impact mat + ring désaccordé |
| `freeze_applied` | `sfx_freeze_apply_1..2` | ~0,34 s | Arpège cristallisation montant |
| `freeze_ended` | `sfx_freeze_end_1` | ~0,2 s | Bris de glace clair |

## ennemis/ (famille « impacts » côté défense)

| Événement | Fichiers | Durée | Intention |
|---|---|---|---|
| `enemy_windup` | `sfx_enemy_windup_1..2` | ~0,44 s | Inspiration grave crescendo (à caler sur la durée du wind-up, gabarit riser Super Lancer) |
| `enemy_hit_ally` | `sfx_enemy_hit_ally_1..3` | ~0,19 s | Thud sourd sub — signature grave opposée au « thwack » allié |

## moments/

| Événement | Fichiers | Durée | Intention |
|---|---|---|---|
| `turn_relay` | `sfx_turn_relay_1` | 0,05 s | Tick feutré très discret |
| `victory_sting` | `sfx_victory_sting_1` | 1,9 s | Bloom d'accord chaleureux, décroissance douce (D8 : cède à la musique) |
| `spec_switch` | `sfx_spec_switch_1..2` | ~0,24 s | Flip aérien haut-bas |
| `summon_spawned` | `sfx_summon_spawn_1` | ~0,46 s | Souffle grave inversé + éclosion |
| `zone_placed` | `sfx_zone_place_1` | 0,2 s | Pulse bas doux |
| `zone_crossed` | `sfx_zone_cross_1` | 0,13 s | Grésil bref |

**Non couverts volontairement** (existants sains à conserver : `hit1–5`, `killsound`, `gameoversound` ; ou à sourcer en pro dès la vraie banque : remplaçant du levier Epidemic Sound, couche sub `super_detonation`, `crit` brillant « shing », `boss_defeated`).
