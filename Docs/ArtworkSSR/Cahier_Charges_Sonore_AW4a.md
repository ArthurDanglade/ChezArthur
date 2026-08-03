# Cahier des charges sonore — AW4a · Transitions d'artwork SSR

**Take Five Games — Track Zero** · 4 août 2026 · chantier AW, gate AW4a.
Objet : sourcer les clips des slots du `ArtworkTransitionConfig`. Arthur sélectionne à l'oreille ; le prompt Cursor AW4 (câblage définitif + retouche riser + cleanup) suit la sélection.

---

## 1. Sourcing & licences

- **Source primaire : Epidemic Sound** — licence commerciale détenue (décision AW4-D1). Moteur de recherche par mots-clés/mood ; termes fournis par slot ci-dessous.
- Sources secondaires (100 % libres, D2) : **Sonniss GameAudioGDC Bundles** (libres de droits commerciaux, bundles annuels ~30 Go), **Kenney** (CC0), **freesound** (filtre CC0).
- `sfx_gacha_burn.wav` (sting + ignite) : **Epidemic Sound, licence commerciale détenue — conservé**. Documenté au plan pour la release.

## 2. Règles techniques (charte F0 §5, communes à tous les slots)

Mono · punch dans les **médiums** (haut-parleurs de téléphone) · pas de silence en tête (< 5 ms) · boucles **seamless** (zéro clic au point de bouclage) · headroom ≈ −6 dBFS (le mix se fait par les volumes codés + bus SFX) · import : Vorbis ~q70, Decompress On Load (courts) / Compressed In Memory (boucles) · nommage : `sfx_aw_<slot>.wav` dans `Audio/SFX/`.
Pour juger équitablement : normaliser les candidats entre eux (~même loudness perçue) avant l'écoute.

## 3. Timelines réelles (tuning actuel du SO)

**Déchéance — 5,20 s** (hold 1,75 / burn 2,4) :

| t (s) | Événement | Slot |
|---|---|---|
| 0,00 | Apparition du prime (flash + punch) | `sting` — vol 0,9 · **SERVI** (`sfx_gacha_burn`) |
| 0,05 → 1,90 | Contemplation (liseré or, motes) | `shimmerLoop` — vol 0,35, fade-in 0,5 s, coupé à l'ignition (fade 0,25) |
| 1,90 | Ignition | `ignite` — vol 0,7 · **SERVI** (`sfx_gacha_burn`) |
| 1,90 → 4,30 | Combustion (front descend, or → cendre) | `crackleLoop` — volume **piloté par le code** (suit l'intensité, pic ≈ 3,1 s), stop fade 0,3 |
| 4,30 | Retombée | `whooshDown` — vol 0,7 |
| 5,20 | État final : déchu | — (silence, vignette) |

**Ascension — 4,55 s** (défauts SO — tuning final au gate) :

| t (s) | Événement | Slot |
|---|---|---|
| 0,02 → 2,41 | Frémissement + montée | `riser` — vol 0,6 · **retouche AW4** : lecture sur canal boucle, coupure programmée à tC − 0,07 s (fade 30 ms) → silence avant l'impact garanti quel que soit le clip |
| ≈ 0,85 / 1,43 / 1,83 | 3 pulsations (white-out par vagues) | `pulse` — vol 0,6, **pitché par le code** : ×1,00 / ×1,26 / ×1,59 (+0/+4/+8 demi-tons) |
| 2,48 | Climax (flash plein écran, burst radial) | `climax` — vol 1,0 |
| 2,50 → 3,60 | Reforge dorée montante | `reforgeLoop` — vol 0,5, stop fade en fin de reforge (release ≤ 0,8 s) |
| 3,60 → 4,55 | Apothéose (liseré or, motes) | — (silence assumé : la fin respire) |

## 4. Cible tonale

Les trois slots **tonals** (`pulse`, `climax`, `reforgeLoop`) doivent appartenir à la **même famille harmonique** — référence des esquisses AW0 : **ré majeur** (climax = accord de ré détuné). L'important est leur cohérence *entre eux* ; toute autre tonalité va si les trois y sont. La matière (`shimmer`, `crackle`, `whoosh`, `riser`) reste atonale/texturale.

## 5. Slots à sourcer (7)

### `shimmerLoop` — le précieux qu'on contemple
Boucle 2–4 s seamless. Scintillement fin, bande haute (5–7 kHz), très discret, tremolo lent acceptable. **Pas de mélodie** (rejoué à chaque nouveau SSR, ne doit jamais fatiguer sur 1,75 s). Interdit : nappes sombres, souffle large.
Recherche Epidemic : `shimmer sparkle loop soft` · `magic glitter ambience loop` · `fairy dust twinkle loop`. CC0 : `sparkle shimmer loop`.

### `crackleLoop` — les braises qui dévorent
Boucle 2–4 s seamless. Crépitement feutré, corps grave doux (~< 1 kHz) + pops fins épars. **Homogène : aucun build intégré** — c'est le code qui sculpte le volume avec le front. Interdit : feu de camp avec vent/ambiance, craquements de bois lourds.
Epidemic : `fire crackle embers close loop` · `campfire crackle intimate loop`. CC0 (Sonniss) : `fire crackle loop`.

### `whooshDown` — la chute s'achève
One-shot 0,6–0,8 s. Descente sombre, souffle qui retombe, fin mate **sans queue brillante** — c'est le moment triste (le déchu s'installe). Interdit : whoosh « cinématique » clinquant.
Epidemic : `dark whoosh down fall` · `airy descend whoosh short`. CC0 : `whoosh down dark`.

### `pulse` — les vagues d'anticipation
One-shot 0,4–0,6 s. Impulsion tonale simple et chaude (cloche douce / triangle), attaque nette, harmoniques **simples** — le code le pitche jusqu'à +8 demi-tons : une texture complexe « chipmunke ». Famille ré.
Epidemic : `magic chime single note short` · `tonal pulse stinger soft`. CC0 : `bell chime single hit`.

### `riser` — la montée vers le blanc
One-shot 2,4–2,8 s (la coupure programmée rend la durée non critique, mais viser ce crescendo). Tension continue, texture harmonique + souffle qui s'ouvre, **sans impact final intégré** (l'impact est le slot `climax` ; le silence pré-impact est fabriqué par la coupure). Remplace le provisoire `risersound`.
Epidemic : `riser build up tension no impact` · `shepard tone riser 3 seconds`. CC0 : `riser tension build`.

### `climax` — l'impact du chantier
One-shot 1,2–1,8 s. Boom sub + éclat brillant + queue scintillante qui s'évanouit. Référence esquisse : boom ~60 Hz + accord de **ré majeur** détuné + sparkles. C'est le son le plus fort du diptyque (vol 1,0) — hiérarchie d'emphase respectée.
Epidemic : `magical impact bloom bright tail` · `epic hit shimmer tail fantasy`. CC0 : `impact magic bloom`.

### `reforgeLoop` — l'or qui reconstruit
Boucle ou nappe 2–4 s. Chœur/nappe chaude **ascendante** (filtre qui s'ouvre), douce, portée par le visuel — jamais dominante (vol 0,5). Référence esquisse : saws détunées ré–fa#–la sous lowpass montant + shimmer. Famille ré.
Epidemic : `warm choir pad rising loop` · `ethereal golden pad swell`. CC0 : `warm pad loop rising`.

### Slots servis (référence si remplacement un jour)
`sting` : impact d'apparition < 1 s, attaque brillante + corps grave. `ignite` : whoosh d'allumage 0,3–0,5 s, souffle → craquement. Actuellement `sfx_gacha_burn` sur les deux — feel validé, licence OK.

## 6. Protocole de sélection (l'A/B est trivial grâce au SO)

1. 2–3 candidats par slot, normalisés entre eux.
2. Écoute **in-context** : glisser le candidat dans le slot du `ArtworkTransitionConfig.asset` (inspector) → rejouer le beat (harness ContextMenu ou vrai reveal/cérémonie). Aucun code à toucher.
3. Retenus → renommer `sfx_aw_<slot>` → déposer dans `Audio/SFX/`.
4. Me lister les fichiers retenus → je livre le **prompt Cursor AW4** : câblage définitif via builder (slots + import settings), retouche riser (canal boucle + coupure tC − 0,07), cleanup M-AW3-1 (coroutines v4 mortes), et la checklist de clôture (perf APK, watch-points, docs).
