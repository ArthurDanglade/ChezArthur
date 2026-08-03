# Prompt Cursor — F3-P2b : UnitStatusFx — boucles de présence, teinte gel, pastilles

> **Chantier SFX/VFX — gate F3, partie 2b.** Réf : charte v1.1 (§1.2–1.3 « un état = 4 moments » / « le corps porte l'état », §3 budgets), Go du 04/08 (2ᵉ menu builder · rail dans les barres · priorité `Freeze > Stun > Burn > Poison > Shield`). HEAD de référence : `bd45e10`.
> **Principe** : le driver ÉCOUTE (events P1 + 2 petits events d'état ajoutés + sync initiale) et ne modifie JAMAIS le gameplay. Les boucles vivent dans un pool dédié à Release explicite — jamais le FxPool one-shot. Zéro scène : attach 100 % runtime.
> **Simplification actée dans le périmètre du Go Q3 (pastilles losanges)** : les losanges sont des `Image` UI blanches tournées à 45° et teintées par cause — même rendu, **zéro texture nouvelle**, zéro câblage de sprite. Si le gate P3 juge le rendu insuffisant, un sprite dédié se génèrera en 10 min de builder.

---

## DEMANDE

Le driver d'état par unité (boucle unique priorisée + teinte gel + publication des pastilles), son pool de boucles, le rail de pastilles dans les barres PV, les 2 events d'état manquants, et le 2ᵉ menu du builder qui génère les 5 prefabs de boucle en réutilisant les assets P2a.

## PÉRIMÈTRE — fichiers

**À créer :**
- `Assets/_Project/Scripts/Gameplay/Feedback/UnitStatusFx.cs`
- `Assets/_Project/Scripts/Gameplay/Feedback/StatusLoopPool.cs`
- `Assets/_Project/Scripts/UI/StatusPipsRail.cs`

**À modifier (zones décrites uniquement) :**
- `Assets/_Project/Scripts/Editor/FeedbackVfxBuilder.cs` (menu 2 + helpers boucle — les helpers P2a existants ne changent pas)
- `Assets/_Project/Scripts/Gameplay/CharacterBallFactory.cs` (1 ligne, après l'AllyHitReaction L87)
- `Assets/_Project/Scripts/Gameplay/StageGenerator.cs` (1 ligne en fin de `SpawnEnemy`, après l'init `EnemyShieldSystem` — avenant acté)
- `Assets/_Project/Scripts/Gameplay/Passives/Handlers/AllyDotSystem.cs` (+ `HasBurn` + event)
- `Assets/_Project/Scripts/Enemies/EnemyShieldSystem.cs` (+ event de présence)
- `Assets/_Project/Scripts/UI/AllyHPBar.cs`, `Assets/_Project/Scripts/UI/EnemyHPBar.cs` (hôtes du rail)
- `Assets/_Project/Scripts/UI/HPBarManager.cs` (2 lignes : bind/unbind aux `Attach`/`Detach`)

**INTERDIT** : `Enemy.cs`, `CharacterBall.cs`, `BuffReceiver.cs` (les events P1 suffisent — n'y retouche pas), `FeedbackCauses` (réutilisé tel quel), `CombatFeedbackService`/`FxPool`/catalogue, handlers de contenu, **tout prefab existant** (barres incluses — le rail se construit par code), toute scène.

## SPÉCIFICATION

### 1. Events d'état manquants

**`AllyDotSystem`** : `public event System.Action<CharacterBall, bool> OnBurnStateChanged;` + `public static bool HasBurn(CharacterBall target)` (scan `_dots`, zéro alloc). Feux : `ApplyBurnInternal` (nouvelle entrée uniquement — un refresh ne change pas la présence) → `(target, true)` ; chaque `RemoveAt` (expiration, mort de la cible, `ClearAllDots`) → `(target, false)` si la cible n'a plus d'entrée. Les émissions feedback P1 ne bougent pas.

**`EnemyShieldSystem`** : `public event System.Action<bool> OnShieldPresenceChanged;` + `public bool HasShieldPresence => ShieldActive || (_hasFragments && AliveFragmentCount > 0);`. Un privé `NotifyPresenceIfChanged()` (cache du dernier état émis) appelé après : `ActivateShield`, la casse dans `AbsorbDamage`, `SetupFragments`, le décrément dans `DamageFragment`. Régén : présence inchangée → silence.

### 2. `StatusLoopPool` (classe C#, même famille que FxPool mais **Release explicite**)

`Get(ParticleSystem prefab, Transform parent)` : dépile ou instancie, parente (`SetParent(parent, false)`, position locale zéro — l'offset vit dans le prefab), `Play(true)`. `Release(instance)` : `Stop(true, StopEmittingAndClear)`, reparente au root du pool, désactive, empile. `ActiveLoopCount` public (diagnostic). Root créé paresseusement (`StatusLoopPoolRoot`, DontDestroy NON — vie de scène). Aucun retour automatique : les boucles ne s'arrêtent jamais seules.

### 3. `UnitStatusFx` (MonoBehaviour, namespace `ChezArthur.Gameplay.Feedback`)

**Attach** : `CharacterBallFactory` → `ball.gameObject.AddComponent<UnitStatusFx>().Initialize();` · `StageGenerator.SpawnEnemy` → idem sur l'ennemi (le `BuffReceiver` existe — créé dans `Enemy.Awake`).

**`Initialize()`** — auto-résolution null-safe : `_ball = GetComponent<CharacterBall>()`, `_enemy = GetComponent<Enemy>()`, `_buffReceiver = GetComponent<BuffReceiver>()`, renderer paresseux (`_ball.VisualRenderer` / `GetComponentInChildren<SpriteRenderer>` ennemi), abonnements (`OnBuffAdded`/`OnBuffRemoved`, `AllyDotSystem.OnBurnStateChanged` si `_ball`, `EnemyShieldSystem.OnShieldPresenceChanged` si présent), **sync initiale** : scan `ActiveBuffs` (classification ci-dessous), `StunSystem.Instance?.IsStunned`, `FreezeSystem.Instance?.HasFreezeBuff`, `AllyDotSystem.HasBurn`, `HasShieldPresence`. Désabonnement complet en `OnDestroy` + `ReleaseAll` + restauration teinte.

**Modèle d'état** : compteurs par cause (tableau indexé sur un petit enum interne `{Freeze, Stun, Burn, Poison, Shield, BuffUp, DebuffDown}`). Classification d'un `BuffData` : `FeedbackCauses.Classify(b)` (Buff→BuffUp, Debuff→DebuffDown, Shield/Burn/Poison directs) ; si `None` → regarder le BuffId : `StunSystem.StunBuffId` → Stun, `FreezeSystem.FreezeBuffId` → Freeze, sinon ignorer (carrier). Incrément sur `OnBuffAdded`, décrément sur `OnBuffRemoved` **quelle que soit la raison** (Replaced compris — le Add du remplaçant ré-incrémente juste après). Jamais négatif (clamp + warning éditeur).

**Boucle unique** : à chaque changement, cible = première cause active dans l'ordre **`Freeze > Stun > Burn > Poison > Shield`** (BuffUp/DebuffDown : jamais de boucle). Si cible ≠ boucle courante : `Release` puis `Get` du prefab mappé. **Chargement des prefabs** : le composant étant ajouté par code (aucun champ sérialisé possible), les 5 prefabs vivent dans `Assets/_Project/Resources/VFX/Feedback/Loops/` (dossier Resources dédié — précédent MainMixer, déviation `.cursorrules` documentée en commentaire) et se chargent par `Resources.Load<GameObject>` **paresseux + cache statique**, null-safe : prefab absent = warning unique + pas de boucle, jamais de crash. Le menu 2 du builder les génère à cet emplacement exact.
**Parent des boucles** : le transform du visuel (renderer résolu) — les boucles sont en simulation Local et suivent l'unité.

**Teinte gel (écrivain unique)** : à l'activation Freeze : capture `renderer.color` puis `renderer.color = CombatFeedbackPalette.GetColor(FeedbackCause.Freeze)` ; à la désactivation / mort / OnDestroy : restauration de la couleur capturée. Un seul point d'écriture, commentaire « écrivain unique teinte d'état — vigilance Veuve P3 ».

**Publication pastilles** : `public event System.Action OnPipsChanged;` + `public int GetActivePips(FeedbackCause[] buffer)` (remplit un buffer fourni, retourne le compte — zéro alloc) : toutes les causes actives SAUF celle portée par la boucle, ordre fixe `Stun, Freeze, Burn, Poison, Shield, BuffUp, DebuffDown`. Événement tiré à chaque changement d'état.

### 4. `StatusPipsRail` (UI, namespace `ChezArthur.UI`)

Composant ajouté PAR CODE par les barres : construit ses pips à la demande — jusqu'à 4 `Image` blanches (sprite null), 10×10, **rotation Z = 45°**, teintées `CombatFeedbackPalette.GetColor(cause)`, espacées horizontalement au-dessus de la barre (offset y = +8), + un `TextMeshProUGUI` « +n » (taille 10) si débordement. `Bind(UnitStatusFx source)` / `Unbind()` : abonnement `OnPipsChanged`, rebuild par réutilisation des Images existantes (activation/désactivation — **zéro Instantiate en régime stable** après la première construction). Buffer `FeedbackCause[8]` réutilisé.

### 5. Barres — hôtes du rail

- **`AllyHPBar`** : dans `Initialize(CharacterBall character)` : crée/récupère son `StatusPipsRail` (enfant par code) et `Bind(character.GetComponent<UnitStatusFx>())` (null-safe).
- **`EnemyHPBar`** : `public void BindStatus(Enemy enemy)` (crée/récupère le rail, bind sur le `UnitStatusFx` de l'ennemi, null-safe) + `public void UnbindStatus()`. **Les barres ennemies sont poolées** (`GetOrCreateBar`) : le rebind doit nettoyer l'ancien abonnement et vider les pips.
- **`HPBarManager`** : dans `Attach(enemy)` après le câblage existant → `bar.BindStatus(enemy);` ; dans `Detach` → `bar.UnbindStatus();`. Rien d'autre.

### 6. Builder — menu 2 (`FeedbackVfxBuilder`, helpers P2a réutilisés)

`[MenuItem("Chez Arthur/Feedback/Générer Boucles d'État (P2b)")]` — génère 5 prefabs dans `Assets/_Project/Resources/VFX/Feedback/Loops/` (EnsureFolder ; **SaveAsPrefabAsset sans DeleteAsset — GUID conservés**), réutilise `EnsureSpriteMat`/`EnsureGlowMat` (matériaux P2a, aucun nouveau) + un nouveau `CreateLoopRoot` (loop = **true**, `playOnAwake = false`, simulation **Local**, sorting 12/13, `maxParticles ≤ 12`) :

| Prefab | Matériau | Recette (présence discrète — charte : « ne fatigue pas ») |
|---|---|---|
| `LoopBurn` | eclat | braises : rate 4/s, montée 0.4–0.8, vie 0.5, taille 0.05–0.08, shape box sur le corps, glow chaud faible |
| `LoopPoison` | goutte | gouttes : rate 3/s, chute (gravité 0.8), émission haut du corps, vie 0.5, pas de glow |
| `LoopShield` | arc | arc : 1 particule permanente (vie 1, rate 1/s), alpha 0.35, pulse doux (size 1→1.06), au-dessus du centre |
| `LoopStun` | etoile | 3 étoiles : rate 3/s, orbital Z lent, offset +0.35 y (au-dessus de la tête), vie 1, glow léger |
| `LoopFreeze` | cristal | 4 cristaux quasi statiques : rate 2/s, vie 1.5, scintillement (color over lifetime alpha 0.6→1→0), aux angles (shape box), pas de glow |

Rapport `Audits/FeedbackLoops_<stamp>.md` (CRÉÉ/MIS À JOUR par prefab). Le `FeedbackCatalogAuditor` ne scanne pas ce dossier (les boucles ne sont pas au catalogue — c'est le design).

## CONVENTIONS

`.cursorrules` : commentaires FRANÇAIS, noms ANGLAIS, bandeaux, zéro alloc en hot path (buffers réutilisés, compteurs en tableaux, aucun LINQ), pas de `Find*` dans Update (le driver n'a **aucun Update**), UI sobre. Compile sans warning.

## SÉQUENCE

1. Appliquer → compiler → **commit 1 (code)** : `feat(feedback): F3-P2b UnitStatusFx — boucles d'état + teinte gel + pastilles`. (Sans les prefabs, le driver warn une fois et ne pose pas de boucle — pastilles et teinte fonctionnent déjà.)
2. Menu 2 → rapport → **commit 2 (assets)** : `feat(feedback): F3-P2b prefabs boucles d'état (Resources)`. Aucune scène dans aucun commit.

## CHECKLIST DE TEST

1. Burn allié (spec Kram sur allié / AllyDot debug) → **braises sur le corps** tant que ça brûle, disparition à la fin ; le flinch DoT (pt n°7) : réécouter avec le visuel — noter le verdict pour P3.
2. Burn ennemi (Kram) → braises sur l'ennemi ; poison PusamAir → gouttes ; bouclier (Bouclar/boss) → arc discret permanent ; stun → spirale d'étoiles au-dessus de la tête ; gel Frigor → **sprite teinté #AEE9FF + cristaux**, restauration exacte au dégel ET à la mort.
3. **Priorité** : ennemi gelé + brûlant → boucle gel, pastille burn ; stun + poison → boucle stun, pastille poison ; l'ordre `Freeze > Stun > Burn > Poison > Shield` vérifié sur 2 combos.
4. **Pastilles** : buff ATK → losange bleu au-dessus de la barre ; debuff → violet ; 5 états simultanés → 4 losanges + « +n » ; expiration → le losange disparaît.
5. **Barres poolées** : tuer un ennemi à états → sa barre réattribuée à un autre ennemi n'affiche PAS de pips fantômes (rebind propre).
6. Kill/étage suivant : `ActiveLoopCount` retombe à 0, aucune boucle orpheline, aucune teinte résiduelle, zéro erreur console.
7. Drag 30 rebonds + états actifs : boucles stables (jamais 2 par unité), one-shots P2a et groupe A inchangés, slider SFX respecté.
8. Profiler : 0 alloc GC par changement d'état en régime stable (après première construction du rail).
9. Prefabs absents (test robustesse : renommer temporairement le dossier Loops) → warning unique, pas de boucle, zéro crash.
10. `git status` : commit 1 = code seul ; commit 2 = `Resources/VFX/Feedback/Loops/**` + rapport + metas seulement.
