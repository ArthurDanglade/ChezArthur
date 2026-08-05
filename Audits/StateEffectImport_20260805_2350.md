# StateEffect → Feedback
Date : 2026-08-05 23:50

## Boucles (présence)
- ✅ LoopBurn ← effect_state_burn
- ✅ LoopPoison ← effect_state_poisoning_2
- ✅ LoopFreeze ← effect_state_coldSnow
- ✅ LoopStun ← effect_state_stuned
- ✅ LoopShield ← effect_state_energy

## One-shots (changement de stat / heal)
- ✅ FxStateHeal ← effect_state_healGreen
- ✅ FxStateBuffUp ← effect_state_powerUp
- ✅ FxStateDebuffDown ← effect_state_slowDown

## Catalogue
- ✅ Catalogue HealReceived → FxStateHeal (FollowTarget, Tint None)
- ✅ Catalogue BuffApplied → FxStateBuffUp (FollowTarget, Tint None)
- ✅ Catalogue DebuffApplied → FxStateDebuffDown (FollowTarget, Tint None)
