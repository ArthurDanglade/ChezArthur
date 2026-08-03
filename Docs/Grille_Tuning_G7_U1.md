# Grille de Tuning G7 — Univers 1 « Le Château Assiégé »

**Take Five Games — Track Zero** · 03/08/2026 · extraite du code et des assets réels à HEAD `a263daa`
Compagnon de G7a/G7b (split acté). Règle d'arbitrage inchangée : *mourir doit se raconter en « j'ai mal joué »*.

## 0. Mode d'emploi

1. **Source unique** : toute valeur marquée `[DATA]` se modifie dans la **table de `EnemyRosterU1Generator.cs`** puis re-run du générateur (jamais dans l'Inspector — le générateur écraserait). Rapport + commit = itération traçable.
2. **`[PREFAB]`** : sérialisé sur `EnemyPrefab.prefab` — modifiable à l'Inspector, committer le prefab.
3. **`[CODE]`** : constante en dur — **micro-prompt Cursor obligatoire avant de toucher** (exposition en data ou modification de la constante, au choix du besoin). Ne jamais tuner à l'aveugle dans le code.
4. **Une hypothèse par session** : un levier (ou un groupe cohérent), une prédiction écrite AVANT le test, un verdict après. Le journal des itérations (§6) est la mémoire du tuning.

---

## 1. Les trois priorités actées (Arthur)

### P1 — Densité des Eaux : le Patriarche doit paraître « plus gros » qu'Alucadra
| Levier | Valeur | Source |
|---|---|---|
| N zones de base | **2** | `[DATA]` `Passive_Patriarche_Eaux.value` |
| Seuil N=3 | **60 % PV** | `[DATA]` `….specialValue2` |
| Seuil N=4 | **30 % PV** | `[DATA]` `….specialValue3` |
| Gros dégâts (présent/chute) | **90** | `[DATA]` `….specialValue1` |
| Rayon d'une zone | **1.4 u** | `[CODE]` `PatriarcheEauxHandler.ZONE_RADIUS` |
| Brûlure traversée | **3 % PV max / cycle × 2 cycles** | `[CODE]` `BURN_PERCENT` / `BURN_CYCLES` |

Pistes : N base 3 (2 se traverse trop poliment) ; rayon ↑ = lignes de drag réellement contraintes (micro-prompt si besoin) ; l'escalade 3→4→5 est une option d'ampleur.

### P2 — L'Épée Volante, hors échelle (recalibrage à intention conservée : grosse menace, tanky, prioritaire)
| Levier | Valeur | Source | Note d'audit |
|---|---|---|---|
| PV | **1000** | `[DATA]` `Enemy_EpeeVolante.baseHp` | la « priorité de cible » se joue ici |
| ATK | **500** | `[DATA]` `….baseAtk` | **VALEUR MORTE** — l'Épée n'attaque jamais par collision (kinematic) ; les dégâts du transpercement viennent du passif. Ramener à une valeur cosmétique honnête (ex. 70) pour que la carte ne mente pas |
| DEF | **150** | `[DATA]` `….baseDef` | mitigation réelle des hits reçus — le vrai « tanky » |
| Transpercement | **70** | `[DATA]` `Passive_Alucadra_Epee.specialValue1` | LA menace réelle de la phase 1 |
| Saignement | **+25 % / 2 tours** | `[DATA]` `….specialValue2 / 3` | amplificateur du dilemme |
| Réduction d'Alucadra | **50 %** | `[DATA]` `….value` | durée de la phase 1 de facto |

### P3 — Rétro-soin du Confesseur (O5 : confirmer ou réduire ICI)
| Levier | Valeur | Source |
|---|---|---|
| Fraction rétro-soin | **100 %** | `[DATA]` `Passive_Confesseur_Lien.specialValue1` |
| Transfert du lien | **20 %** | `[DATA]` `….value` |
| Sermon | **35** | `[DATA]` `….specialValue2` |

Question de session : à 100 %, soigner le connecté est-il *interdit de fait* (dilemme) ou *inutile* (frustration) ? La frontière se teste avec un SUP dans l'équipe. 75 % est le cran d'essai naturel.

---

## 2. Stats de base (contrat §6 — posées, à éprouver)

| Ennemi | PV | ATK | DEF | SPD | LF | Tals | Slot |
|---|---:|---:|---:|---:|---:|---:|---|
| Disciple Trop Motivé | 130 | 35 | 10 | 60 | 33 | 4 | Basique |
| Archère Précise | 160 | 40 | 12 | 45 | — | 3 | Basique |
| Garde aux Pieux | 260 | 25 | 30 | 30 | — | 3 | Basique |
| Le Confesseur | 900 | 45 | 35 | 40 | — | 35 | é.5 |
| La Veuve en Deuil | 2200 | 70 | 45 | 45 | 33 | 20 | é.10 |
| Alucadra | 3200 | 85 | 55 | 40 | 33 (loup) | 30 | é.15 |
| Épée Volante | 1000 | (500†) | 150 | 1 | — | 0 | compagnon |
| Patriarche Vandermont | 4500 | 100 | 60 | 25 | — | 60 | é.20 |

Tout `[DATA]` (table générateur). † = valeur morte, voir P2. `dernier_pieu` (retiré, D29) : hors jeu, Tals 100 legacy sans effet.

## 3. Passifs et patterns — leviers restants

| Levier | Valeur | Source |
|---|---|---|
| Disciple — ATK vs SUP « sur le coup » | +30 % (1 tour) | `[DATA]` `ChasseurDeSoigneurs.value` |
| Disciple — DEF sans SUP adverse | +30 % | `[DATA]` `RepliStudieux.value` |
| Disciple — force de charge | +10 % | `[DATA]` `TropMotive.value` |
| Archère — rayon de zone | 2.5 u | `[DATA]` `Branches.specialValue1` |
| Archère — pluie / perforante | 40 / 55 | `[DATA]` `….specialValue2 / 3` |
| Archère — debuff DEF perforante | −20 % (à vie) | `[DATA]` `….value` |
| Garde — Mur (équipe, non-cumul) | DEF +30 % | `[DATA]` `MurDePieux.value` |
| Garde — Colère (1/cycle, max 5) | ATK +10 %/stack | `[DATA]` `Colere.value/stackValue/maxStacks` |
| Garde — Coup de Pieu (portée / dégâts) | 2.0 u / 30 | `[DATA]` `CoupDePieu.specialValue1 / 2` |
| Veuve — amplitude des courbes | ±40 % | `[DATA]` `ChagrinRageur.value` |
| Veuve — Voile / seuil Déchirure | −50 % / 20 % PV | `[DATA]` `….specialValue1 / 2` |
| Alucadra — pas Prédateur | +10 %/rôle (max 3 rôles) | `[DATA]` `Legerete.value` |
| Alucadra — Légèreté | DEF −20 / SPD +30 / force +40 % | `[DATA]` `….specialValue1/2/3` |
| Patriarche — réduction / renvoi / plafond | 20 % / 20 % / 15 % PV max·tour | `[DATA]` `Chaine.value/sv1/sv2` |

## 4. Leviers transverses

| Levier | Valeur | Source | Effet joueur |
|---|---|---|---|
| Rétention vélocité / frame | 0.99 | `[PREFAB]` | longueur des drags ennemis (R6 « vitesse lisible ») |
| Decay mur / allié | 0.75 / 0.6 | `[PREFAB]` | nervosité des rebonds ennemis |
| Multiplicateur dégâts collision | 1.0 | `[PREFAB]` | poids d'un impact de charge |
| Force de lancer par défaut | 33 | `[CODE]` `EnemyAI.DEFAULT_LAUNCH_FORCE` | filet — les 3 mobiles ont leur valeur data |
| Wind-up avant lancer | 0.5 s | `[PREFAB]` `EnemyAI.launchDelay` | fenêtre de lecture du télégraphe |
| Plafond renvoi par défaut | 15 %/tour | `[CODE]` `REFLECT_CAP_DEFAULT` (surchargé par sv — déjà data pour la Chaîne) |
| Scaling PV / étage (U1) | +8 %/étage | `[PREFAB scène]` `StageGenerator.hpScalingPerStageByUniverse[0]` | LA pente de la courbe |
| Scaling ATK / étage (U1) | +6 %/étage | idem `[0]` de atk | idem |
| Bonus de slot é.10 / é.20 | PV ×1.15 / ×1.6 · ATK ×1.0 / ×1.4 | `[CODE]` `StageGenerator.GetBoss*Bonus` | poids des boss |
| Bonus de slot é.5 / é.15 | PV ×1.0 / ×1.7 · ATK ×1.0 / ×1.4 | `[CODE]` `GetMiniBoss*Bonus` | poids Confesseur / Alucadra |
| Nb d'ennemis par bloc d'étages | 2-4 / 3-4 / 3-4 / 4-5 | `[PREFAB scène]` `enemyCountRangeByBlock` | **le levier de rythme principal (R1)** |
| Cadence idle | 8 fps | `[DATA]` `idleFps` par ennemi | vie du sprite |
| Durées d'action (pluie, dash, eaux…) | 0.5–1.0 s | `[CODE]` consts handlers | tempo des tours Fixes |

**Micro-prompt en attente de besoin** (ne pas toucher avant) : exposition en data des bonus de slot + `ZONE_RADIUS`/`BURN_*` si les sessions P1 le réclament — je le rédige sur demande, périmètre `StageGenerator` + 2 handlers, zéro logique.

---

## 5. Protocole de session G7a

**Setup** : run neuve, équipe de départ fixe (D15), `forceArdaculaOnly`, placeholders assumés. Une session = une hypothèse écrite avant de lancer.

**Relevé par run** (une ligne par étage) : PV d'équipe en sortie (%) · morts (qui, cause, « ça se raconte ? » oui/non) · première alerte réelle (minute) · notes d'illisibilité (tout moment « je n'ai pas compris »).

**Relevé par boss** (é.5/10/15/20) : durée du combat (tours) · PV d'équipe entrée/sortie · le dilemme/mécanique a-t-il été DÉCLENCHÉ par le joueur (le lien a-t-il fait renoncer à un soin ? le voile a-t-il changé un plan ? les eaux ont-elles dévié un drag ? la chaîne a-t-elle puni ?) · ressenti d'ampleur 1–5 — **la colonne Alucadra vs Patriarche est LA mesure de P1**.

**Cibles indicatives G7a** (à affiner en séance) : première alerte ≤ étage 3 ; sortie d'étage basique 60–85 % PV ; zéro mort « pas compris » ; boss é.5 = premier « aïe » ; é.15 < é.20 en ampleur ressentie.

## 6. Journal des itérations (à remplir — la mémoire du tuning)

| # | Date | Hypothèse | Levier(s) → Avant → Après | Verdict (mesures + une phrase) |
|---|---|---|---|---|
| 1 | | | | |

## 7. Critère de sortie

- **G7a clos** quand : courbe conforme aux cibles §5 sur 3 runs consécutives, priorités P1/P2/P3 tranchées et consignées, zéro moment d'illisibilité relevé.
- **G7b** (après la passe juice/SFX du chantier feedback) : **la run du contrat** — près de la défaite dans les trois premières minutes, chaque mort racontable en « j'ai mal joué ». Verdict consigné → le contrat `Refonte_Ennemis_Design_U1_v1` passe en **VALIDÉ** (erratum §9) et le chantier refonte ennemis est clos.
