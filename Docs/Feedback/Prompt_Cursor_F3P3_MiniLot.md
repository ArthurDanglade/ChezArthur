# Prompt Cursor — F3-P3 mini-lot pré-gate : A-P3-1 (teinte boucles) · A-P3-2 (couleur labels) · A-P3-3 (mat kill)

> **Chantier SFX/VFX — gate F3-P3, avenants pré-gate.** Go du 04/08 (A-P3-1/2/3). HEAD de référence : `8aa8b66`.
> Trois corrections chirurgicales avant les captures du gate palette. **Aucune scène, aucun preset d'asset modifié** (les teintes de labels sont des clones runtime). 1 commit code + 1 commit asset.

---

## PÉRIMÈTRE — fichiers

**À modifier :**
- `Assets/_Project/Scripts/Gameplay/Feedback/UnitStatusFx.cs` (A-P3-1 — zone `SwitchLoop` uniquement)
- `Assets/_Project/Scripts/UI/FloatingNumberSpawner.cs` (A-P3-2 — chemin `ShowLabel` uniquement)
- `Assets/_Project/Scripts/Editor/PixelDeathBurstBuilder.cs` (A-P3-3 — durcissement)

**Asset régénéré au menu (commit 2)** : `Assets/_Project/Prefabs/VFX/PixelDeathBurst.prefab` — **GUID conservé impératif** (le catalogue Kill le référence).

**INTERDIT** : les presets `TextAnimation` existants (assets intacts — clones runtime seulement), `FeedbackVfxBuilder`, le catalogue, les autres méthodes `Show*` de FloatingNumberSpawner, toute scène, zones gelées habituelles.

## SPÉCIFICATION

### A-P3-1 — Boucles teintées par cause (`UnitStatusFx.SwitchLoop`)

Après `_loopInstance = StatusLoopPool.Shared.Get(prefab, parent);` (L333) : teinter l'instance par la cause — mapping `StatusSlot` → `FeedbackCause` (Freeze→Freeze, Stun→Stun, Burn→Burn, Poison→Poison, Shield→Shield), puis pour **chaque** `ParticleSystem` de l'instance (racine + enfant Glow, `GetComponentsInChildren`) :
```csharp
Color c = CombatFeedbackPalette.GetColor(cause);
var main = ps.main;
Color prev = main.startColor.color;
c.a = prev.a;                    // préserver l'alpha du prefab (ex. arc à 0.35)
main.startColor = c;
```
Alloc du `GetComponentsInChildren` tolérée : le Get est événementiel (changement d'état), pas du hot path — commentaire le disant. Aucun autre changement dans le fichier.

### A-P3-2 — Respect de `color` sur le chemin Pixel (`FloatingNumberSpawner.ShowLabel`)

Le plugin porte ses couleurs en **Gradients** (`TextAnimation.fillColorInTime`, `borderColorInTime`). Approche : **clone runtime teinté, avec cache borné**.

1. Privés : `Dictionary<(TextAnimation, Color32), TextAnimation> _tintedAnimCache` (lazy) + 
```csharp
private TextAnimation GetTintedAnim(TextAnimation baseAnim, Color color)
```
— retourne le clone en cache sinon : `Object.Instantiate(baseAnim)` ; **remplace le RGB** de `fillColorInTime` : nouvelles `colorKeys` = mêmes temps que l'original, toutes à `color` ; **`alphaKeys` de l'original conservées** (l'enveloppe de fondu est l'animation) ; `borderColorInTime` **intact** (le contour sombre porte la lisibilité) ; met en cache. Cache borné par nature (couleurs = palette, ~12 entrées max) — commentaire le disant.
2. `ShowLabel` : `TryDisplayPixel(text, anim, pos)` → `TryDisplayPixel(text, GetTintedAnim(anim, color), pos)`. **Rien d'autre ne change** — ni les autres `Show*` (leurs presets typés sont corrects), ni les presets assets, ni `FallbackSpawn`.
3. Commentaire d'en-tête de méthode : « Amendement charte §5.6 — API intacte, bugfix : le paramètre couleur était ignoré sur le chemin PixelBattleText (labels D12 écrasés en orange crit). »

### A-P3-3 — Mat kill + durcissement du vieux builder (`PixelDeathBurstBuilder`)

1. **Supprimer le bloc `DeleteAsset`** (`existing != null → DeleteAsset`) : `SaveAsPrefabAsset` directement sur le chemin — **GUID conservé** (même verrou que FeedbackVfxBuilder, commentaire identique « Pas de DeleteAsset — GUID conservé »).
2. Remplacer `renderer.material = new Material(Shader.Find("Sprites/Default"));` par :
```csharp
// Matériau BUILTIN persistant — un new Material() n'est pas sérialisé dans le prefab (bug magenta historique).
renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
```
3. **Menu « Créer + Assigner … à JuiceDirector » : obsolète** — `_deathBurstPrefab` a été purgé en F2-P2b (le catalogue Kill porte la réf). Supprimer ce `[MenuItem]` et le chemin d'assignation scène ; conserver uniquement « Créer PixelDeathBurst (prefab) ». Commentaire : « Assignation scène supprimée — la réf vit dans FeedbackCatalog (Kill) depuis F2-P2b. »

## SÉQUENCE

1. Appliquer → compiler → **commit 1 (code)** : `fix(feedback): F3-P3 pré-gate — teinte boucles + couleur labels + durcissement builder kill`.
2. Menu `Chez Arthur/VFX/Créer PixelDeathBurst (prefab)` → **commit 2 (asset)** : `fix(feedback): F3-P3 pré-gate — PixelDeathBurst matériau builtin (magenta soldé)`. **Vérifier au diff : prefab modifié EN PLACE (pas de suppression/recréation, .meta intact).**

## CHECKLIST DE TEST

1. Injecteur ÉTATS : burn → **braises orange** `#FF8C3C` ; poison → gouttes acide ; bouclier → arc cyan (alpha 0.35 préservé) ; stun → étoiles jaunes ; gel → cristaux glacés + teinte sprite. Le glow suit la couleur de sa cause.
2. Priorité inchangée (gel + burn → boucle gel colorée + pastille burn) ; Release/rebind : aucune couleur résiduelle d'une cause précédente sur une boucle réutilisée (la teinte au Get écrase la précédente — vérifier en enchaînant burn → release → poison sur la même unité).
3. Labels : MÉGACRIT → **or** ; switch de spé → rééval **lavande** `#B48CFF` ; lien Confesseur / chaîne Patriarche → leurs couleurs D12 ; le **crit garde son preset orange d'origine** (chemin non touché).
4. Kill : burst **blanc net**, plus aucun magenta — machine ET après restart de l'éditeur (le bug ne vivait qu'en session).
5. `git diff` commit 2 : `PixelDeathBurst.prefab` seul, modification en place, guid inchangé dans le .meta ; le catalogue Kill joue toujours le burst (référence intacte).
6. Presets `TextAnimation` : `git status` — **aucun asset preset modifié** (clones runtime uniquement).
7. Profiler : pas d'alloc récurrente sur les labels répétés (cache) ; boucles : alloc ponctuelle au changement d'état seulement.
8. Non-régression : run normale — groupe A, one-shots B, pastilles, sons : identiques hors les 3 corrections.
