# Prompt Cursor — F5-L1 : canal labels d'état (lisibilité muette) + fix chiffres doublés

> **Chantier SFX/VFX — F5-L, phase L1.** Go du 05/08 (wording validé — Stun = « Étourdissement » ; plafonds 3 global / 1+1 par unité / 0,8 s ; « Bouclier »/« Brisé » actés). HEAD : `80a26e8` (pull d'abord).
> L1 = canal label dans le pipeline + 6 mots d'état en data + dedup anti-doublons des chiffres. **Stats/origine = L2 (hors périmètre), `BuffReceiver` INTERDIT ici.**
> **2 commits** : code / catalogue régénéré. Staging sélectif strict.

---

## PÉRIMÈTRE

**À modifier** : `FeedbackBundle.cs` (+2 champs) · `CombatFeedbackService.cs` (dispatch ~12 l.) · `UI/FloatingNumberSpawner.cs` (système de lanes) · `UI/FloatingNumberHook.cs` (dedup + diagnostic) · `Editor/FeedbackCatalogBuilder.cs` (seeds labels)
**Asset (commit 2)** : `FeedbackCatalog.asset` EN PLACE (GUID intact).
**INTERDIT** : `BuffReceiver` (L2), `FeedbackContext` (L2), handlers, `UnitStatusFx`/pips, `JuiceDirector`, `SettingsPanelUI`, scènes, les méthodes `Show*` existantes du spawner (chiffres et `ShowLabel` D12 intacts).

## SPÉCIFICATION

### 1 — Bundle : le label est de la data

`FeedbackBundle` : `public string labelTextFr = "";` + `public Color labelColor = Color.white;` (tooltip : « Mot affiché à la pose — vide = pas de label. Couleur = palette par cause, écrite par le builder »).

### 2 — Builder : 6 mots, couleurs = palette vivante

Struct `Seed` : + `public string LabelFr;` + `public FeedbackCause LabelCause;` (None par défaut). Sync-avant-continue (pattern existant) : si `LabelFr` non vide → `b.labelTextFr = seed.LabelFr; b.labelColor = CombatFeedbackPalette.GetColor(seed.LabelCause);` (le builder relit la palette à chaque run — source unique §1.1 préservée).
Seeds mis à jour : `burn_apply` → « Brûlure »/Burn · `poison_apply` → « Poison »/Poison · `freeze_apply` → « Gel »/Freeze · `stun_apply` → « Étourdissement »/Stun · `shield_gain` → « Bouclier »/Shield · `shield_break` → « Brisé »/Shield. (Slots mutés audio : le label joue quand même — canaux indépendants.)

### 3 — Service : dispatch du label (après les garde-fous existants)

Dans `Play`, après le bloc haptic (le cooldown d'événement a déjà throttlé — gratuit) :
```csharp
// 6) Label d'état (F5-L1) — le mot annonce, le corps porte, le chiffre mesure.
if (!string.IsNullOrEmpty(bundle.labelTextFr) && FloatingNumberSpawner.Instance != null)
{
    Transform anchor = ctx.Target != null ? ctx.Target
        : (ctx.TargetBall != null ? ctx.TargetBall.transform : null);
    if (anchor != null)
        FloatingNumberSpawner.Instance.ShowStateLabel(
            anchor.GetInstanceID(), bundle.labelTextFr, bundle.labelColor,
            anchor.position);
}
```
Pas de label sans cible connue (un mot orphelin au sol = bruit).

### 4 — Spawner : lanes d'état (nouveau, à côté de l'existant — rien d'existant ne change)

`public void ShowStateLabel(int unitId, string text, Color color, Vector3 unitPos)` :
- **Budget global labels : 3 actifs** (compteur dédié — ne consomme JAMAIS le budget popups des chiffres ; retirer le `priority: true` nulle part : `ShowLabel` D12 existant reste tel quel).
- **Lane par unité** (`Dictionary<int, Lane>` borné, purge des lanes inactives > 5 s) : 1 label affiché + 1 en file — un 3e remplace la file (le plus récent gagne). Fin d'affichage sur timer **0,8 s** (unscaled), la file part alors.
- **Dedup** : même texte + même unité < 0,6 s → skip.
- **Ancre** : `unitPos + Vector3.up * stateLabelOffsetY` (serialized, défaut 1.1f — au-dessus de la barre PV) ; si 2 labels co-affichés même zone, `ResolveFreePosition` existant fait le stagger.
- Rendu : même chemin que `ShowLabel` (preset `labelAnim` + `GetTintedAnim(anim, color)` — cache existant). Mots longs (« Étourdissement ») : scale 0,9f appliqué si `text.Length > 10` (const commentée, tuning L3).
- Coroutines du spawner (singleton MonoBehaviour) pour les timers — `WaitForSecondsRealtime`.

### 5 — Chiffres doublés : dedup au puits + diagnostic (cause exacte inconnue — « parfois »)

`FloatingNumberHook` (les 2 chemins L74/L80) :
1. **Dedup-frame** : même unité + même montant + même `Time.frameCount` → skip le 2e popup.
2. Quand le dedup attrape un doublon, log dev-only : `[Popup] doublon {name} {amount} f{frame}` — on identifiera la source racine en play (fix racine = L3 si besoin).
3. **Vérifier** qu'aucun chemin n'attache `FloatingNumberHook` deux fois (prefab sérialisé + AddComponent runtime dans CharacterBallFactory / StageGenerator / MidCombatSpawner) — si double attache trouvée : la retirer, c'est LA cause racine, le dire en commentaire de commit.
La grille actuelle (vérifiée au clone) : PoisonTick L80 ✓, BurnTick L73+L86 ✓, AllyDot L257 ✓ suppriment avant leur chiffre coloré — ne pas y toucher.

## SÉQUENCE
1. Code → **commit 1** : `feat(feedback): F5-L1 — labels d'état (lanes 3/1+1/0,8s, palette) + dedup popups doublons`.
2. Menu catalogue → **commit 2 (asset seul)** : `chore(feedback): F5-L1 — 6 labels d'état seedés`. Diff : catalogue en place, 6 entrées seules (labelTextFr/labelColor + le bruit sérialisation one-shot des 2 nouveaux champs, attendu).

## CHECKLIST (run muette — le critère du lot)
1. Injecteur, son coupé : pose burn → « Brûlure » orange au-dessus de l'unité · poison → « Poison » vert · gel → « Gel » glacé · stun → « Étourdissement » jaune (lisible, scale 0,9) · bouclier → « Bouclier » cyan · casse → « Brisé » cyan. Les slots audio-muets (bouclier, brisé) affichent bien leur mot.
2. AUCUN label sur : ticks, expirations, refresh d'état, heal, relais. (L'anti-spam buff existant gate déjà les événements en amont.)
3. Spam injecteur : jamais > 3 labels à l'écran ; par unité 1+1, le plus récent remplace la file ; re-pose < 0,6 s = dedup.
4. Les chiffres ne perdent JAMAIS un slot à cause d'un label (budgets séparés) ; MÉGACRIT/rééval/lien (D12) inchangés.
5. Doublons : burn/poison fight — un seul chiffre par tick (coloré) ; si le log `[Popup] doublon` sort, noter quelle situation l'a produit.
6. Pastilles, boucles, priorité de boucle : strictement inchangées.
7. `git status` propre après 2e run du menu (idempotence) ; zéro GUID orphelin.
