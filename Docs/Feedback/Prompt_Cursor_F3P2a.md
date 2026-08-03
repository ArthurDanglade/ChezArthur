# Prompt Cursor — F3-P2a : VFX one-shot procéduraux du groupe B + câblage catalogue

> **Chantier SFX/VFX — gate F3, partie 2a.** Réf : charte v1.1 (§2 formes/couleurs, §5.4 sorting, D1 pixel+glow, R12 zéro séquence dessinée), Go du 03/08 (P2a→P2b · avenant StageGenerator réservé P2b · pastilles losanges). HEAD de référence : `2591e66`.
> **Data pure, zéro code runtime.** Un builder éditeur génère textures pixel + matériaux + 12 prefabs ParticleSystem one-shot, et les câble dans les entrées B du catalogue. Le service joue déjà ces bundles depuis F3-P1 : à l'exécution du menu, **tous les moments d'état deviennent visibles** sans toucher une ligne de runtime.
> **Gabarit technique = la recette `PixelDeathBurstBuilder`** (PS configuré par code, EnsureFolder, SaveAsPrefabAsset) avec une différence CRITIQUE : **jamais de `DeleteAsset` avant sauvegarde** — `SaveAsPrefabAsset` sur un chemin existant conserve le GUID, les références du catalogue survivent aux régénérations.

---

## DEMANDE

Un builder idempotent qui crée : 8 textures pixel + 1 glow (PNG procéduraux), 9 matériaux, 12 prefabs one-shot pixel+glow, et remplit les 18 entrées B du catalogue (`vfxPrefab` vide OU pointant `FxPlaceholder` uniquement). Rapport `Audits/`.

## PÉRIMÈTRE — fichiers

**À créer** : `Assets/_Project/Scripts/Editor/FeedbackVfxBuilder.cs` — c'est le SEUL fichier de code du gate.
**Assets générés à l'exécution du menu (commit 2)** : `Assets/_Project/Art/FX/Feedback/` (textures + matériaux) · `Assets/_Project/Prefabs/VFX/Feedback/` (prefabs, à côté de FxPlaceholder).

**INTERDIT** : tout fichier runtime, `FeedbackCatalogBuilder`/`FeedbackCatalogAuditor` (intacts — le nouveau builder est autonome), les prefabs existants (`ImpactBurst`, `LaunchBurst`, `PixelDeathBurst`, `FxPlaceholder` — ce dernier cesse d'être référencé mais n'est PAS supprimé ni modifié), `AwakeningGlow.mat` (on réutilise son shader, pas l'asset), toute scène, zones gelées habituelles. Dans le catalogue : ne toucher QUE `vfxPrefab`, `tintMode`, `tintCause`, `vfxScale` des 18 entrées listées — clips/familles/emphases/cooldowns/volumes strictement intacts.

## SPÉCIFICATION

### 1. Menu et robustesse

`[MenuItem("Chez Arthur/Feedback/Générer VFX Groupe B (textures + prefabs + câblage)")]` — séquence : dossiers → textures → matériaux → prefabs → câblage catalogue → rapport. Idempotence par écrasement à GUID constant : PNG via `File.WriteAllBytes` + `ImportAsset` (réimport, même guid) ; matériaux et prefabs : charger s'il existe et mettre à jour, sinon créer (`SaveAsPrefabAsset` sans delete). Re-exécution complète = diff git vide.

### 2. Textures (`Art/FX/Feedback/tex_fx_<nom>.png` — formes blanches sur alpha 0, dessinées par masques de pixels)

| Nom | Taille | Forme |
|---|---|---|
| `chevron` | 8×8 | chevron « ^ » plein, 2 px d'épaisseur |
| `croix` | 8×8 | croix « + » 2 px |
| `arc` | 24×24 | arc d'anneau ~120° (haut), 2 px |
| `eclat` | 8×8 | triangle effilé |
| `goutte` | 8×8 | goutte (rond 4 px + pointe haute) |
| `etoile` | 8×8 | étoile 4 branches |
| `cristal` | 8×8 | losange allongé vertical |
| `glow` | 32×32 | dégradé radial doux (alpha 1 → 0) |

Import forcé par le builder (`TextureImporter`) : **Point (no filter)**, no mipmaps, no compression, wrap Clamp, type Default. Le look pixel vient de là.

### 3. Matériaux (`Art/FX/Feedback/`)

- `mat_fx_<forme>.mat` × 8 : `Sprites/Default` + `_MainTex` = la texture (gabarit PixelDeathBurst : le blanc se teinte par vertex color / startColor).
- `mat_fx_glow.mat` : `Shader.Find("ChezArthur/UI/AwakeningGlowAdditive")` + texture glow ; si introuvable → repli `Legacy Shaders/Particles/Additive` + warning au rapport.

### 4. Gabarit commun des 12 prefabs (recette PixelDeathBurst adaptée)

Racine = PS « matière pixel » : `loop = false` (OBLIGATOIRE — l'auditeur rejette les loops au catalogue), durée ≤ 0.6 s, simulation World, scalingMode Local, `maxParticles ≤ 64`, colorOverLifetime **blanc → alpha 0** (la teinte vient du `startColor` posé par le service via TintMode.Cause), renderer Billboard / SortMode Distance / ombres off / motion vectors off, **`sortingOrder = 12`** (unités = 10, charte §5.4 : unités < FX de corps < glows < popups). Enfant `Glow` = second PS : `mat_fx_glow`, 1–3 particules larges (0.3–0.6), **durée ≤ racine** (le retour pool est déclenché par la racine — F2-P1 force `stopAction = Callback` au spawn), `sortingOrder = 13`, playOnAwake true.

### 5. Les 12 prefabs (`Prefabs/VFX/Feedback/Fx<Nom>.prefab`)

| Prefab | Texture | Recette (l'esprit charte §2 — valeurs de départ, tuning P3) |
|---|---|---|
| `FxChevronsUp` | chevron | burst 5–7, vitesse 1.2–2 vers le HAUT (cone étroit up), gravité 0, taille 0.08–0.12, vie 0.3–0.45, glow discret |
| `FxChevronsDown` | chevron | idem, rotation 180° (chevrons descendants), vitesse vers le BAS |
| `FxHealMotes` | croix | burst 6–8, montée douce 0.6–1.2 + noise léger, vie 0.4–0.6, glow doux |
| `FxShieldGain` | arc | 1 particule arc, scale-in (sizeOverLifetime 0.6→1), vie 0.35, + glow bref — l'arc « se referme » |
| `FxShieldPulse` | arc | 1 particule arc, pulse court (1→1.15→fade), vie 0.25, pas de glow |
| `FxShieldShatter` | eclat | burst radial 10–14, vitesse 2–5, dampen 0.3, vie 0.3–0.5, glow flash bref |
| `FxBurnFlare` | eclat | bouffée 6–9 vers le haut, vitesse 1.5–3, vie 0.25–0.4, glow chaud net |
| `FxPoisonSplash` | goutte | burst 5–7 TOMBANTES (gravityModifier 1.2, vitesse initiale faible), vie 0.35–0.5, glow minimal |
| `FxStunRing` | etoile | 3–4 étoiles en orbite brève (velocityOverLifetime orbital Z), au-DESSUS de la position (offset shape +0.25 y), vie 0.5, glow léger |
| `FxFreezeCrystals` | cristal | burst 5–6 apparition quasi statique (vitesse 0.2–0.5), scale-in, vie 0.5, glow froid |
| `FxFreezeShatter` | cristal | burst radial 10–12, vitesse 2.5–5, gravité 0.8, vie 0.3–0.5, glow flash |
| `FxDissipate` | croix | 4–6 motes DESCENDANTES lentes, fade rapide, vie 0.3–0.4, pas de glow — la dissipation discrète générique |

### 6. Câblage catalogue (18 entrées — `tintMode = Cause` partout)

**Règle d'écriture** : ne modifier `vfxPrefab` que s'il est **null OU égal au FxPlaceholder** (comparaison par référence à l'asset chargé depuis `Assets/_Project/Prefabs/VFX/Feedback/FxPlaceholder.prefab` — jamais un guid en dur). Un autre prefab déjà présent = INTACTE au rapport. `Undo.RecordObject` + `SetDirty` + `SaveAssets`.

| Event (id) | Prefab | tintCause | vfxScale |
|---|---|---|---|
| HealReceived (9) * | FxHealMotes | Heal | 1 |
| BuffApplied (10) * | FxChevronsUp | BuffUp | 1 |
| BuffExpired (11) | FxDissipate | BuffUp | 0.8 |
| DebuffApplied (12) * | FxChevronsDown | DebuffDown | 1 |
| DebuffExpired (13) | FxDissipate | DebuffDown | 0.8 |
| ShieldGained (14) | FxShieldGain | Shield | 1 |
| ShieldAbsorbed (15) | FxShieldPulse | Shield | 0.8 |
| ShieldBroken (16) * | FxShieldShatter | Shield | 1 |
| BurnApplied (17) | FxBurnFlare | Burn | 1 |
| BurnTick (18) | FxBurnFlare | Burn | 0.6 |
| BurnEnded (19) | FxDissipate | Burn | 0.7 |
| PoisonApplied (20) | FxPoisonSplash | Poison | 1 |
| PoisonTick (21) | FxPoisonSplash | Poison | 0.6 |
| PoisonEnded (22) | FxDissipate | Poison | 0.7 |
| StunApplied (23) | FxStunRing | Stun | 1 |
| StunEnded (24) | FxDissipate | Stun | 0.7 |
| FreezeApplied (25) | FxFreezeCrystals | Freeze | 1 |
| FreezeEnded (26) | FxFreezeShatter | Freeze | 1 |

\* = porte aujourd'hui FxPlaceholder (posé en F2-P1) → REMPLACÉE au rapport. `attachMode` reste World partout (P2a — le suivi d'unité est l'affaire des boucles P2b). Aucun changement de `shakeTrauma`/`hitstopMs` (restent 0 — tuning éventuel en P3 par avenant).

### 7. Rapport `Audits/FeedbackVfx_<yyyyMMdd_HHmm>.md`

Textures/matériaux/prefabs : CRÉÉ / MIS À JOUR par asset · câblage : CÂBLÉE / REMPLACÉE (placeholder) / INTACTE par entrée · warning repli shader glow le cas échéant · récap sorting (12/13 vs unités 10).

## CONVENTIONS

`.cursorrules` : commentaires FRANÇAIS, noms ANGLAIS, bandeaux, `#if UNITY_EDITOR`, éditeur sous `Scripts/Editor/`, namespace `ChezArthur.EditorTools`. Masques de textures en tableaux de coordonnées lisibles (pas de bitmaps encodés en dur illisibles). Compile sans warning.

## SÉQUENCE

1. Appliquer → compiler → **commit 1 (code)** : `feat(feedback): F3-P2a builder VFX one-shot groupe B`. Rien ne change en jeu.
2. Exécuter le menu → lire le rapport → `Chez Arthur/Feedback/Audit Catalogue Feedback` → **commit 2 (assets)** : `feat(feedback): F3-P2a textures + prefabs VFX groupe B + câblage catalogue`. Aucune scène dans aucun commit.

## CHECKLIST DE TEST

1. Après commit 1 : une run normale strictement identique (le builder n'a pas tourné).
2. Après menu : rapport complet (9 textures+glow, 9 matériaux, 12 prefabs, 15 CÂBLÉES + 3–4 REMPLACÉES placeholder, 0 INTACTE inattendue) ; `FeedbackCatalogAuditor` vert — **0 prefab `loop = true`**.
3. Harness « Jouer tous les events » : les 18 moments B visibles ET teintés juste — buff bleu montant, debuff violet descendant, soin vert, bouclier cyan (fermeture/pulse/éclats), braise orange, poison acide tombant, stun jaune au-dessus de la tête, gel glacé (cristallisation puis brisure).
4. Run réelle : Kram → flare de pose + bouffées de tick ; Bouclar → arc au gain, pulse à l'encaisse, éclats à la casse ; stun → ring d'étoiles ; gel Frigor → cristaux à la pose, brisure au bris par un allié.
5. Pool : 2ᵉ vague sans `Instantiate` ; > 12 FX demandés → budget respecté (`SkippedFx` > 0), emphases ≥ 5 passent.
6. **Idempotence** : re-exécuter le menu → rapport « MIS À JOUR / INTACTE », `git status` **vide** (GUIDs conservés — le test qui prouve le SaveAsPrefabAsset sans delete).
7. Sorting : FX au-dessus des sprites d'unités (12/13 vs 10), sous les popups ; zones au sol toujours dessous.
8. `git status` commit 2 : uniquement `Art/FX/Feedback/**`, `Prefabs/VFX/Feedback/**`, `FeedbackCatalog.asset`, `Audits/` (+ metas).
