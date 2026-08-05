# Grille de Tuning INVR4 — Reveal « Entrée en scène » (freeze & clôture INV-R)

**Take Five Games — Track Zero** · 06/08/2026 · extraite du code et des assets réels à HEAD `84d7d1c`
Compagnon de `Audit_Reboot_Invocation_INVR.md`. Règle d'arbitrage du chantier : *« long ne veut pas dire mauvais — le rythme »* (R1) · *« des moments délicieux »* (R2).

## 0. Mode d'emploi

1. **`[SO]`** : champ de `RevealStageConfig.asset` — tune libre à l'Inspector sur device, **aucun commit pendant l'exploration**. Les valeurs finales seront figées par le prompt de freeze (asset + défauts `.cs` + builder + auditor alignés — le pattern de ton `84d7d1c`, désormais canal officiel : **R-D4**).
2. **`[CODE]`** : constante en dur (`Director` / `InfoPanel` / contrôleur) — **jamais tunée à l'aveugle** : noter le souhait dans le retour §5, le freeze l'appliquera (ou l'exposera en SO si ça doit rester tunable).
3. **`[DATA]`** : par perso (`CharacterData`) — Inspector, commit data.
4. **Une hypothèse par session** : un levier (ou groupe cohérent), une prédiction écrite avant, un verdict après. Journal §6.
5. La **preview** (`invr0-entree-en-scene-preview`) reste l'outil comparatif côte à côte (sliders identiques) ; le juge final est le device.

---

## 1. P1 — L'entrée (le chantier : « trop lisible, trop rapide », SR surtout)

| Levier | Valeur | Source | Piste | Signe de trop loin |
|---|---|---|---|---|
| `shadowLevel` | **0,62** | `[SO]` | **0,42–0,52** | on ne devine plus AUCUNE forme → la pénombre devient un simple halo, le jeu de reconnaissance meurt |
| `lightMaxSR` | **0,34** | `[SO]` | 0,26–0,30 | idem — et l'écart SR→SSR doit rester sensible |
| `lightMaxSSR` | **0,42** | `[SO]` | 0,36–0,40 | — |
| `lightMaxLR` | **0,46** | `[SO]` | 0,40–0,44 | — |
| `ditherCell` | **3** | `[SO]` | **4** (pénombre plus grossière = moins lisible, très pixel) | 5+ : bouillie, les motes se noient |
| `entrySR` | **0,95** | `[SO]` | **1,15–1,35** | > 1,5 : le SR n'est plus « rapide », le ×10 s'alourdit (8 SR/multi) |
| `entrySSR` | **1,60** | `[SO]` | 1,90–2,20 | ⚠ contrainte riser ci-dessous |
| `entryLR` | **1,95** | `[SO]` | 2,20–2,50 | ⚠ contrainte riser ci-dessous |
| `pulsesSR/SSR/LR` | 2 / 3 / 4 | `[SO]` | ne toucher qu'après les durées (la pulsation s'étire avec l'entrée) | pulses+durée courte = clignotement |

**⚠ CONTRAINTE RISER — à ne pas franchir sans me le dire** : le clip `sfx_inv_entry_riser.wav` dure **2,55 s** et doit être **coupé par le noir** (le silence est le beat). Il faut donc **`entry + hold ≤ ~2,45 s`** par rareté. Aux pistes hautes : SSR 2,20 + 0,28 = 2,48 (limite) · LR 2,50 + 0,38 = 2,88 (**dépassé** — le riser s'éteindrait tout seul 0,3 s avant le noir, le beat s'effondre). Si le feel réclame des entrées plus longues que la contrainte : **je produis une version longue du riser** (rebuild ~3,2 s), ne pas rogner le `hold` pour compenser.

## 2. P2 — Respiration post-snap (R-D4, posée à `84d7d1c` — à valider à l'oreille)

| Levier | Valeur | Source | Note |
|---|---|---|---|
| Hold post-snap | **0,2 s** | `[CODE]` (`WaitForSecondsRealtime(0.2f)`, contrôleur) | piste 0,15–0,30. Verdict §5 : garder en constante ou **exposer en SO au freeze** (reco : exposer — c'est du rythme) |
| `statusDelay` | **1,0** | `[SO]` | queues des snaps : SR s'éteint ~0,4 s après la fin du snap visuel · SSR ~0,8 s · **LR ~1,16 s** → à 1,0 le stamp marche encore légèrement sur la queue LR. Options : global 1,0–1,2, **ou** `statusDelay` par rareté (champs ajoutés au freeze si le feel l'exige — à trancher sur un LR nouveau) |
| `nameDelay` / `nameDur` | 0,10 / 0,25 | `[SO]` | RAS sauf oreille |
| `chipFill` / `tickStagger` | 0,45 / 0,12 | `[SO]` | juger la cascade complète sur doublon 4 deltas |

## 3. P3 — Verdicts checklist INVR3 encore ouverts

| Verdict attendu | Options |
|---|---|
| **`snapLr`** : l'attaque douce (~120 ms) épouse le front 0,34 s ou paraît molle ? | OK tel quel · redécoupe 2ᵉ vague du source (je m'en charge) · remplacement clip |
| **Hiérarchie** SR < SSR < LR à l'oreille | OK · ajuster crêtes (je re-masterise) |
| **`VOL_*`** `[CODE]` : riser 0,6 · snap 1,0 · stamp 0,8 · tick 0,5 · dim 0,7 | noter les souhaits (ex. « stamp trop fort face au snapSr ») |
| **Skip-all** : cadence sonore propre, zéro blip | OK · retours |

## 4. Autres leviers à figer

| Levier | Valeur | Source | Question de session |
|---|---|---|---|
| `entryOverlap` | **0,15** | `[SO]` | le fondu sortant coupé à ~50 % « pop »-t-il ? Sinon garder ; si oui : **0** (séquentiel propre) ou 0,08 |
| `fakeHold` / `fakeCutBonus` | 0,18 / 0,14 | `[SO]` | re-feel du fakeout avec les vrais sons (le silence étiré est-il délicieux ou trop long ?) |
| Focals par perso | (0,5 · 0,65) défaut | `[DATA]` `portraitFocalPoint` | passe roster : preview clic droit → reporter. Priorité aux SSR/LR animés (Ardacula…) et aux persos dont le visage est excentré |
| `vignette` | 0,34 | `[SO]` | **morte depuis R-D3** — proposition : **purge au freeze** (champ + asset + shader garde `_Vignette` pour le harness ? non : purge complète propre). Ton verdict §5 |
| `frontSoft` / `cutDuration` / `frontSoft` | 0,10 / 0,08 | `[SO]` | ne toucher qu'en dernier recours |

## 5. Le retour attendu (à me renvoyer tel quel, rempli)

```
ENTRÉE   : shadowLevel=…  lightMax SR/SSR/LR=…/…/…  ditherCell=…  entry SR/SSR/LR=…/…/…  pulses=…/…/…
RISER    : contrainte 2,45 s respectée ? OUI / NON → version longue demandée
POST-SNAP: hold=… (garder CODE / exposer SO)   statusDelay=… (global / par rareté : SR=… SSR=… LR=…)
SONS     : snapLr OK/mou→action…   hiérarchie OK/…   VOL_* souhaités : …   skip-all OK/…
DIVERS   : entryOverlap=…   fakeout OK/…   vignette : purge OUI/NON   focals : faits (nb persos ajustés : …)
```

→ Je transforme ce retour en **prompt de freeze INVR4** : défauts `.cs` + asset + builder + auditor alignés (pattern R-D4), purge `vignette` (si acté), exposition/valeur du hold, `VOL_*`, éventuels `statusDelay` par rareté, re-masterisation éventuelle (snapLr, crêtes, riser long), **rapport auditor re-généré vert** (solde M-INVR3-1), sync des défauts de la preview, puis **clôture INV-R** : bilan chantier au doc, gates fermés.

## 6. Journal des itérations

| Date | Levier(s) | Prédiction | Verdict |
|---|---|---|---|
| 05/08 | hold 0,2 + statusDelay 1,0 (R-D4) | le stamp ne marche plus sur la queue du bam | posé — à confirmer, LR à vérifier |
| — | | | |
