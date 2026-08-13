# Feedback — burn/poison clips + pastilles/alterne + buff scale

Date : 2026-08-12 23:40

## Décisions Arthur
- Multi-états : **pastilles toutes visibles + boucle FX qui alterne** (0,8 s)
- SFX : **apply + tick même volume** (clips Arthur)
- Buff/debuff : scale **×1,8**
- Source passif/valise/item : hors lot

## Assets
- `etats/sfx_burn_apply_1.wav` + `sfx_burn_tick_1.wav` ← `Downloads/burnclip.wav`
- `etats/sfx_poison_apply_1.mp3` + `sfx_poison_tick_1.mp3` ← `Downloads/poison.mp3`
- `burn_tick` / `poison_tick` retirés de `MutedSlots`

## Code
- `UnitStatusFx` : rotation candidats Freeze→Stun→Burn→Poison→Shield ; `GetActivePips` n'exclut plus la boucle active
- `FeedbackCatalog` : clips burn/poison + volumes tick = apply ; buff/debuff `vfxScale` 1.8
- Builders alignés (catalogue + VFX scale forcée)

## Chez toi
1. Unity refresh / import des nouveaux audio
2. Optionnel : `Chez Arthur → Feedback → Créer ou Mettre à Jour le Catalogue`
3. Play : debug Burn/Poison (apply + ticks sonnent) ; multi-états = pastilles + boucle qui tourne ; buff/debuff plus gros
