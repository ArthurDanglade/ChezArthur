# Charte de feedback — Combat (F0, le « contrat » du chantier SFX/VFX)

**Take Five Games — Track Zero** · 2 août 2026 · **v1.1 — VALIDÉE** (Go Arthur du 02/08, amendements A1–A4 intégrés).
Compagnon de `Audit_Preparatoire_SFX_VFX.md` (vérité terrain, commit `c176092`) et de la GameFeel Bible (dont elle est le complément « couche d'état », pas le remplacement).
Règle d'arbitrage héritée d'U1 : *un joueur qui meurt doit pouvoir dire « j'ai mal joué », jamais « je n'ai pas compris »* — appliquée au feedback : **tout ce qui change l'état du combat doit être vu ET entendu à l'instant où ça change.**

---

## 0. Décisions actées (02/08/2026)

| Réf. | Décision | Statut |
|---|---|---|
| D1 | Style visuel **hybride pixel + glow** : matière en particules pixel nettes, énergie en glow additif doux. Tout procédural code/particules. | FIGÉ |
| D2 | Identité sonore **organique moderne**, banques pro libres (Sonniss GDC, Kenney, CC0), 0 €. | FIGÉ |
| D3 | **Socle FX maintenant**, en piste parallèle d'U1 — protocole de coexistence §7. | FIGÉ |
| D4 | **Langage partagé** (couleur = sens pour tout le roster) + slots d'override par perso/skill (F5). | FIGÉ |
| D5 | **AudioMixer léger** : groupes Music / Ambiance / SFX + snapshots de duck. Corrige au passage le bug slider SFX (audit §6.1). | FIGÉ |
| D6 | **Haptique dans ce chantier** (F4) : HapticManager light/medium/heavy/crit, Android d'abord, colonne haptique dans le catalogue dès F2. | FIGÉ |
| D7 | Sliders : **Tals chaîné sous le bus SFX** (le slider fin reste, il se multiplie au bus). | FIGÉ |
| D8 | Sting de victoire : validé en **F4** — sting court qui **cède à la musique Hub** ; rétrogradable F5 si le coût audio le justifie (A1). La refonte complète de la séquence de récompense reste hors périmètre. | FIGÉ |

---

## 1. Principes

Hérités de la Bible (inchangés) : réponse multi-sensorielle immédiate ; hiérarchie d'emphase (si tout crie, plus rien ne crie) ; le juice est un assaisonnement ; lisibilité totale malgré le chaos ; portrait-safe (shake subtil, feedback dans les 2/3 hauts).

Nouveaux principes, spécifiques à la couche d'état :

1. **Couleur = sens, une seule source.** `CombatFeedbackPalette` s'étend et devient l'unique définition des couleurs de cause (flottants, particules, glows, pastilles, UI). Aucune couleur de feedback codée ailleurs.
2. **Un état = quatre moments.** Application (ponctuel, visible), présence (boucle discrète qui ne fatigue pas), tick (ponctuel léger), expiration (dissipation discrète). Chaque état du §6 définit ses quatre moments — certains sont volontairement silencieux.
3. **Le corps porte l'état.** Un allié brûlé a des braises sur lui, un ennemi gelé est teinté — le chiffre flottant complète, il ne remplace pas. (Aujourd'hui : état = chiffre seul, c'est le trou n°1 de l'audit.)
4. **La forme désambiguïse, la couleur code la famille.** Deux effets d'une même famille chromatique (ex. bouclier et gel, tous deux glacés) se distinguent par forme et position (arc autour vs teinte du sprite), jamais par une nuance de couleur seule — illisible en pixel art sur mobile.
5. **Silence sur le non-joueur.** Ce qui ne change rien à une décision du joueur ne produit rien (ex. re-tri interne de file). Anti-principe du bruit.

---

## 2. Langage couleur / forme / son (le cœur de la charte)

Ancres existantes conservées telles quelles : dégâts ennemi `#FF4D4D`, dégâts allié `#FF9933`, soin `#4DFF66`, crit `#FFD933`, brûlure `#FF8C3C`, poison `#80E633`, réévaluation switch `#B48CFF`, lien `#C05A78`, rétro-soin `#7CBF7C`, chaîne `#9FB4C7` (D12, réservées G6).

Nouvelles causes :

| Cause | Couleur | Forme visuelle (pixel + glow, D1) | Identité sonore (D2) |
|---|---|---|---|
| **Buff de stat** (toutes stats) | `#66B8FF` bleu clair | Flèches/chevrons pixel **montants** le long de l'unité + micro-glow ; pastille d'icône de stat (épée/bouclier/chrono/fronde) tant que le buff dure | Whoosh **montant** court, brillant |
| **Debuff de stat** | `#B44DE6` violet saturé | Chevrons pixel **descendants** + pastille assombrie | Whoosh **descendant** sourd |
| **Bouclier — gain** | `#7DE0FF` cyan glacé | Arc/bulle hexagonale pixel qui se referme autour de l'unité, reste en présence à alpha faible | « Clink » cristallin doux |
| **Bouclier — encaisse** | idem | Pulse de l'arc au point d'impact | « Tok » mat (jamais le son de dégâts) |
| **Bouclier — casse** | idem | Éclats pixel de l'arc + flash bref | Verre étouffé + corps grave léger |
| **Brûlure — pose/tick/fin** | `#FF8C3C` | Braises pixel montantes sur le corps (présence), flare bref à la pose, bouffée au tick, dissipation en fumée 3–4 px à la fin | Flare bref à la pose ; crépitement très court au tick |
| **Poison — pose/tick/fin** | `#80E633` | Gouttes/bulles pixel **tombantes** (opposé de la brûlure) | « Blub » discret au tick |
| **Stun** | `#FFE066` | Étoiles/spirale pixel tournoyant au-dessus de la tête (présence forte : info tactique) | Impact mat + ring léger à la pose |
| **Gel** | `#AEE9FF` | **Teinte du sprite entier** + cristaux pixel aux angles ; brisure de glace à la fin | Cristallisation à la pose, bris à la fin |
| **Soin (renforcé)** | `#4DFF66` | Motes/croix pixel montantes + glow doux — à la **réception** du soin (l'existant n'a que le chiffre) | Shimmer chaud court |
| **Invocation/spawn** | blanc→teinte ennemi | Matérialisation : dissolve inversé (gabarit `AwakeningDissolve`) + poussière au sol | Souffle grave court |
| **Wind-up ennemi** | existant (flash blanc) | Inchangé (EnemyHitReaction) | **Inspiration grave courte** calée sur la durée du wind-up (gabarit : pitch synchronisé comme le riser du Super Lancer) |
| **Impact ennemi→allié** | — | Flash blanc + squash de l'**allié** (portage EnemyHitReaction), micro-hitstop, shake léger | Thud **sourd** — signature grave opposée au « thwack » brillant allié→ennemi : on distingue à l'oreille qui frappe qui |
| **Relais de tour** | halo bille active (existant) | Transition de focus déjà en place | Tick feutré très discret |
| **Victoire** | — | Slow-mo finisher existant | Sting chaleureux court (~2 s), pas de fanfare |

Vigilances de palette actées (A3) : **gate visuel obligatoire** sur capture d'arène réelle. Debuff `#B44DE6` vs réévaluation `#B48CFF` : désambiguïsés par forme (chevrons sur corps vs label texte) — **en cas de doute au gate visuel, le debuff bascule sur indigo (≈ `#5A46E0`) et la réévaluation reste lavande `#B48CFF`**. Famille glacée bouclier/gel : **la forme prime** (arc autour de l'unité vs teinte du sprite entier), la couleur ne sert qu'à coder la famille.

---

## 3. Hiérarchie d'emphase et budgets

**Échelle** (chaque palier monte sur tous les canaux, jamais un petit aussi fort qu'un gros) :
tick DoT < rebond < application d'état < coup < crit ≈ casse de bouclier < kill < transformation / stage clear.

**Budget par unité** : au plus **une boucle d'état pleine** (braises, bulles, arc…) visible par unité — s'il y a plusieurs états, priorité contrôles (stun/gel) > DoT > bouclier > stats, le reste en **pastilles d'icônes** empilées au-dessus de la barre de PV. Le détail complet vit sur la carte ennemie (G2) et l'UI d'équipe.

**Budget écran** (points de départ, tunables) : ≤ 12 systèmes de particules actifs / ~400 particules vivantes (le pool refuse au-delà, silencieusement, priorité aux emphases hautes) ; popups déjà plafonnés à 5 ; **plafonds de voix audio par famille** : impacts 4, statuts 2, UI 2, stings 1 — une famille pleine vole sa voix la plus ancienne, jamais celle d'une autre famille.

**Anti-spam** (générique dans le socle, jamais par effet) : cooldown par événement (impacts ~70 ms, statuts ~120 ms), pitch jitter systématique ±4–6 %, 2–3 variations de clip par événement fréquent. Un drag à 30 rebonds = l'escalade existante + jamais plus que les plafonds ci-dessus.

*(A2) Tous les chiffres de ce §3 sont des points de départ : retune obligatoire au **premier gate jouable**, formalisé par avenant à la charte.*

---

## 4. Catalogue d'événements V1 (liste fermée)

Colonnes du catalogue (ScriptableObject, F2) : `eventId` → bundle { VFX (prefab poolé + teinte), SFX (clips[] + volume + pitch range + cooldown + famille de voix), shake (trauma), hitstop (ms), haptique (none/light/medium/heavy), emphase (1–6), overrides par characterId/skillId (F5) }.

**Groupe A — cœur existant, re-câblé sur le catalogue (F2, iso-ressenti)** : `ally_launch`, `super_launch` (2 temps : charge + détonation), `aim_tension` (boucle de visée + tick d'entrée en zone + duck), `wall_bounce`, `hit_enemy`, `crit`, `kill`, `stage_finisher` (= victoire d'étage actuelle, slow-mo + zoom), `defeat_beat`. Zéro changement de ressenti — critère de non-régression.
*(A4) Couverture vérifiée contre `JuiceDirector` à `c176092` : 8 bundles câblés, tous listés ci-dessus. Les ticks de `StageTransitionUI` (SfxPlayer direct) restent hors catalogue, conservés tels quels — ils héritent du mixer via SfxPlayer. `ResetForNewStage` = plomberie, pas un événement.*

**Groupe B — langage d'état (F3, le cœur joueur du chantier)** : `heal_received`, `buff_applied` / `buff_expired`, `debuff_applied` / `debuff_expired`, `shield_gained` / `shield_absorbed` / `shield_broken` (alliés **et** ennemis — `EnemyShieldSystem` inclus), `burn_applied` / `burn_tick` / `burn_ended` (allié via `AllyDotSystem`, ennemi via `BurnTickSystem`), `poison_applied` / `poison_tick` / `poison_ended`, `stun_applied` / `stun_ended`, `freeze_applied` / `freeze_ended`.

**Groupe C — axe ennemi & moments (F4)** : `enemy_windup` (son), `enemy_launch`, `enemy_hit_ally` (le bundle défense complet), `enemy_wall_bounce` (léger), `summon_spawned`, `turn_relay`, `victory_sting`, `boss_defeated` (accent sur le kill existant), `spec_switch` (son sur la bannière existante), `zone_placed` / `zone_crossed` (hooks G3), `revive`, `extra_turn` (léger).

**Groupe D — réservés U1/G6 (consommés par la piste d'Arthur, jamais créés ici)** : beam du lien, voile de la Veuve, transformation d'Alucadra, Eaux Bénites — ils utiliseront les événements B/C + leurs handlers ; les briques signatures spécifiques arrivent en F5, après G6b/c jouables (D3).

Règle D4 : tout événement résout d'abord l'override (perso/skill) puis retombe sur le générique. V1 ne livre **que** du générique ; l'architecture du catalogue expose le slot dès F2 pour que F5 et les futurs persos gacha n'exigent aucun refactor.

---

## 5. Règles techniques non négociables

1. `Time.timeScale` interdit hors pics existants (finisher, défaite). Un futur ralenti de crit passera par un contrôleur dédié en temps non-scalé (F4) — leçon StageAnnouncerUI / Bible §2.1.
2. **Tout FX spawné est poolé** (pool générique F2, gabarit `GroundZoneSystem`). Zéro alloc en hot path, MPB pour les flashs, pas de `Find*` dans Update (.cursorrules).
3. **Câblage scène uniquement par builders éditeur idempotents** (pattern `PixelDeathBurstBuilder`), rapport dans `Audits/`, jamais de câblage manuel.
4. Sorting (layer `Default` unique) : zones au sol < unités < FX de corps < glows < popups (canvas overlay). Les ordres exacts s'alignent sur `GroundZoneSystem` au moment de F2 et sont documentés dans le catalogue.
5. Audio : clips mono, courts (< 400 ms hors stings), Vorbis ~q70, Decompress On Load pour les petits clips ; punch dans les médiums (HP de téléphone, Bible §4). Tous les volumes passent par le mixer (D5) : sliders = paramètres dB du mixer, duck = snapshots. `SfxPlayer`/`SfxManager` gardent leurs API, routées vers le groupe SFX.
6. API existantes préservées : `JuiceDirector.PlayX` continue d'exister pendant la migration (il devient consommateur du catalogue, ses appelants ne bougent pas) ; `FloatingNumberSpawner` inchangé.
7. Accessibilité (F5) : toggle « réduire les mouvements » (shake, slow-mo, flashs plein écran) respecté par le catalogue — champ `respectsReduceMotion` par bundle.

---

## 6. Découpage en gates (périmètres fermés)

| Gate | Périmètre | Critère de test |
|---|---|---|
| **F1 — Socle audio** | Mixer (3 groupes + snapshots), routage SfxPlayer/SfxManager/Tals (D7), **fix slider SFX**, banque D2 v1 importée (settings §5.5), remplacement du clip Epidemic Sound | Slider SFX baisse TOUT (combat inclus) ; duck visée identique au ressenti actuel ; A/B silence total via mixer |
| **F2 — Socle VFX + catalogue** | `FeedbackCatalog` SO + service poolé + garde-fous (§3), groupe A re-câblé iso-ressenti, hit-react allié (flash+squash), conventions sorting | Checklist non-régression du feel actuel ; drag 30 rebonds = plafonds respectés (log de comptage) |
| **F3 — Langage d'état** | Groupe B complet : 4 moments par état, pastilles d'icônes, briques d'événements manquantes (`OnBuffAdded/Removed` sur BuffReceiver, events shield, hooks Stun/Freeze/AllyDot) | Scène dev : chaque état posé/tické/expiré est visible sur le corps + audible + pastille ; 2 états simultanés = 1 boucle + pastilles ; **rendez-vous G6** : Arthur branche ses handlers dessus |
| **F4 — Axe ennemi & moments** | Groupe C + HapticManager (D6) + crit dramatique complet (ralenti unscaled + zoom-punch) + sting victoire (D8) | À l'aveugle (écran caché) : on distingue qui frappe qui à l'oreille ; wind-up audible avant chaque lancer ennemi |
| **F5 — Signatures & couche pro** | Slots d'override actifs (D4), moments U1 (avec Arthur, post-G6b/c), accessibilité, passe perf/profiling APK, tuning inspecteur | Toggle réduire-mouvements OK ; profiling : 0 alloc/frame en combat stable ; nouveaux persos = 0 travail FX vérifié sur un perso test |

Boucle par gate inchangée (méthode U1) : audit ciblé → proposition + fichiers touchés → **Go d'Arthur** → prompt Cursor → push → contrôle du diff ligne par ligne → checklist in-game → commit.

---

## 7. Protocole de coexistence avec U1 (acté par Arthur, 02/08)

- **Point d'insertion** : G6a-P3 posé sur `main` (`c176092`), rien de bloquant pour F0→F2. On ne termine pas tout G6 d'abord.
- **Pistes** : Claude = F0 → F1 → F2 (socle + catalogue), **sans brancher de signatures U1**. Arthur = G6 en parallèle (handlers différés, G6b–G6c).
- **Rendez-vous** : F3 sur les événements génériques dès le socle validé ; F5 seulement après G6b/G6c jouables.
- **Zones gelées croisées** : le chantier FX n'édite pas les handlers ennemis ; le chantier G6 n'édite pas `JuiceDirector`, le pool SFX, ni le catalogue FX.
- **`Game.unity`** : commits séparés, jamais les deux chantiers dans le même commit de scène.

---

## 8. Critères de validation de la charte (F0)

1. Chaque événement du §4 a couleur, forme, son, emphase, garde-fou définis — aucun point d'émission de l'audit §4 laissé orphelin sans décision (feedback ou silence volontaire).
2. La palette §2 est validée **sur capture d'arène réelle** (fond sombre du bar) au premier gate visuel — les deux vigilances (violets, famille glacée) tranchées à ce moment-là.
3. ~~Arthur valide ou amende~~ — **fait le 02/08** : D8 figé (A1), budgets retunables au 1er gate jouable par avenant (A2), palette sous gate visuel obligatoire (A3), liste §4 fermée avec groupe A complété (A4).

---

*Charte v1.1 — validée le 02/08/2026. F1 en cours : voir `Plan_Execution_SFX_VFX.md` (proposition technique) et le prompt Cursor F1-P1.*
