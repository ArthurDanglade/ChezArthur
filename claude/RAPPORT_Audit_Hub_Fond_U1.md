# RAPPORT — Audit lecture seule : Hub Fond Accueil (U1)

- **Scène** : `Assets/_Project/Scenes/Hub.unity`
- **Date audit** : 2026-08-01
- **Contrainte** : aucune modification (scène / assets / code)
- **Device de référence demandé** : Samsung A23 — 1080×2408 (20:9)
- **Référence canvas projet** : 1080×1920 portrait

---

## 1. CANVAS & ÉCHELLE

### HubCanvas — Canvas + CanvasScaler

| Champ | Valeur | Source |
|-------|--------|--------|
| GameObject | `HubCanvas` | `Hub.unity` L24189 |
| `Canvas.renderMode` | `0` = **Screen Space Overlay** | L24266 |
| `Canvas.pixelPerfect` | `0` (off) | L24269 |
| `CanvasScaler.uiScaleMode` | `1` = **Scale With Screen Size** | L24246 |
| `referenceResolution` | **(1080, 1920)** | L24249 |
| `screenMatchMode` | `0` = Match Width Or Height | L24250 |
| `matchWidthOrHeight` | **1** (= 100 % hauteur) | L24251 |
| `referencePixelsPerUnit` | **100** | L24247 |

Formule runtime (match hauteur) :

- `canvasScaleFactor = Screen.height / 1920`
- largeur canvas (unités ref) = `Screen.width × 1920 / Screen.height`
- hauteur canvas (unités ref) = `1920`

### SafeAreaFitter (sur `SafeRoot`)

| Champ | Valeur | Source |
|-------|--------|--------|
| Script | `Assets/_Project/Scripts/UI/SafeAreaFitter.cs` | guid `5c9fe1e083256f847b44903e4f5dd8a2` |
| `conformTop` | **0 (false)** | `Hub.unity` L8663 |
| `conformBottom` | **0 (false)** | L8664 |
| `conformLeft` | **1 (true)** | L8665 |
| `conformRight` | **1 (true)** | L8666 |

Comportement exact (`SafeAreaFitter.Apply`, L156–178) :

1. Lit `ScreenSafeArea.SafeArea` / Width / Height (`ScreenSafeArea.cs` — Device.Screen en Editor).
2. Si `conformTop == false` → `yMax = screenH` (SafeRoot jusqu’au **bord haut physique**).
3. Si `conformBottom == false` → `yMin = 0` (SafeRoot jusqu’au **bord bas physique**).
4. Gauche/droite : `xMin/xMax` = safe area (sauf si conform désactivé).
5. Pose `anchorMin/Max` normalisés + `offsetMin/Max = 0`.

**Conséquence Accueil** : le fond illustration n’est **pas** inset verticalement par la safe area. Les insets haut/bas sont gérés ailleurs :

- Haut : `HubHeaderSafeBleed` (Header edge-to-edge, pills dans zone safe).
- Bas : `HubNavSafeBleed` — hauteur nav = `UiTheme.NavHeight (152) + bleed`, avec `bleed = safe.yMin / canvas.scaleFactor` (`HubNavSafeBleed.cs` L235–246).

#### Valeurs safe area A23 (1080×2408, 20:9)

| Donnée | Valeur |
|--------|--------|
| Résolution écran | 1080×2408 (donnée mission) |
| `safeArea` exacte A23 dans le dépôt | **INTROUVABLE** (aucun Device Simulator / doc chiffrée A23 dans le repo ; cherché : `*A23*`, `2408`, audits Hub, scripts SafeArea) |
| Largeur canvas ref @ 1080×2408, match=1 | `1080 × 1920 / 2408 = **861.1296** u` |
| Hauteur canvas ref | **1920** u |
| `scaleFactor` | `2408 / 1920 = **1.254166…**` |

État **sérialisé** dans la scène (dernier layout éditeur, cohérent avec un Game view haut 20:9) :

| Nœud | Champ | Valeur sérialisée | Source |
|------|-------|-------------------|--------|
| `NavigationBar` | `sizeDelta.y` | **229.34598** | L24455 |
| `BottomZone` | `anchoredPosition.y` | **231.65784** | L28653 |
| `HomeIllustrationRig` | `sizeDelta` | **(1005.0912, 1688.3422)** | L16523 |
| `HomeIllustrationRig` | `anchoredPosition` | **(0, 115.82892)** | L16522 |

Interprétation bleed bas (si NavHeight token 152) :

- bleed implicite nav ≈ `229.34598 − 152 = **77.34598** u`
- inset framing (BottomZone.y) ≈ **231.65784** u → `1920 − 231.65784 = **1688.34216** u` (= `sizeDelta.y` du rig)

Conversion bleed → pixels device (si `scaleFactor = 1.254167`) :

- `77.34598 × 1.254167 ≈ **97.0** px` (nav)
- `79.65784 × 1.254167 ≈ **99.9** px` (BottomZone)  

→ safe.yMin A23 **non mesurée ici** ; les rects scène impliquent ~**98–100 px** de bande unsafe bas au moment de la dernière sauvegarde.

### Conventions PPU du projet

| Domaine | PPU | Source |
|---------|-----|--------|
| Canvas Hub `referencePixelsPerUnit` | **100** | `Hub.unity` L24247 |
| Sprites Hub Accueil (`Sprites/Hub/**`) | **100** (`spritePixelsToUnits`) | metas `base/char/window/sky/mountain…` |
| Sprites UI fonctionnels (`Sprites/UI/`) | preset Point + RGBA32 ; PPU **non forcé** par `UIImportPostprocessor` (reste défaut Unity / asset) | `UIImportPostprocessor.cs` |
| Icônes personnage | **100** | `CharacterIconImportPostprocessor.cs` L15–32 |
| Sprites combat | **256** | `CombatSpriteImportPostprocessor.cs` L23–106 |
| Auras combat | **256** | `AuraSpriteImportPostprocessor.cs` L16–34 |

**Note** : les couches paysage Accueil sont des **RawImage** (UV scroll) — le PPU n’affecte pas leur taille à l’écran (rect stretch). Le PPU 100 s’applique aux **Image** (Wagon / Window / Character).

---

## 2. ZONE ILLUSTRATION DE L’ACCUEIL

### Emplacement music player vs Accueil

Le music player **n’est pas** enfant de `PageAccueil`. Il vit sous `SafeRoot/TopUtilityRow/LofiPlayerBar` (sibling de `PageContainer`, dessiné **par-dessus** les pages).

Ordre siblings `SafeRoot` (bas → haut rendu UI) :

1. `PageContainer` → `PageAccueil` …
2. `Header`
3. `TopUtilityRow` (BandBackdrop, BandHairline, ShopCluster, **LofiPlayerBar**, NewsCluster)
4. `NavigationBar`

`TopUtilityRow` : anchors haut stretch, `anchoredPosition.y = -176`, `sizeDelta.y = 144` (`Hub.unity` L48077–48080) — sous le Header (`UiTheme.HeaderHeight = 176`).

### Hiérarchie `PageAccueil` (music player → Lancer une run)

```
SafeRoot
└─ PageContainer
   └─ PageAccueil                          [CanvasGroup, PageAccueilUI, RectMask2D]
      ├─ HomeIllustrationRig               [HomeIllustrationFraming]
      │  ├─ LandscapeLayer                 [ParallaxManager + CanvasRenderer]
      │  │  ├─ 'Sky '                      [RawImage → sky.png]
      │  │  ├─ 'Clouds '                   [RawImage → cloud1.png]
      │  │  ├─ Clouds  (1)                 [RawImage → cloud.png]
      │  │  ├─ 'Mountain '                 [RawImage → mountain.png]
      │  │  ├─ 'Ground_rock '              [RawImage → tree 3.png]
      │  │  ├─ 'Hills_mid '                [RawImage → tree 2.png]
      │  │  └─ 'Hills_far '                [RawImage → tree1.png]
      │  ├─ WagonLayer                     [Image → base.png]
      │  ├─ Window                         [Image → window reflection.png]
      │  ├─ CharacterLayer                 [Image → char.png]
      │  └─ 'LightOverlay '                [RawImage → vfx.png, a=0.5686]
      └─ BottomZone                        [VerticalLayoutGroup, ContentSizeFitter, BottomZoneNavClearance]
         ├─ BtnLancerRun                   [Image, Button, HubButtonUI, …]
         │  ├─ Label
         │  └─ SubLabel (inactive)
         └─ BtnBossRush
            ├─ Fill
            ├─ Label
            └─ SubLabel (inactive)
```

Composants clés `PageAccueil` (`Hub.unity` L7491–7574) :

| Composant | Rôle |
|-----------|------|
| RectTransform | stretch parent `anchorMin(0,0) anchorMax(1,1)` sizeDelta(0,0) |
| `PageAccueilUI` | câble Lancer / BossRush / Magasin / News |
| CanvasGroup | alpha transitions |
| RectMask2D | padding (0,0,0,0) — clip le cover qui déborde |

`BottomZone` layout (`L28656–28695`) : VerticalLayoutGroup padding L16/R16/T12/B12, spacing 12, childAlignment LowerCenter (7), ContentSizeFitter vertical Preferred Size.

### `HomeIllustrationFraming` — comportement précis

Fichier : `Assets/_Project/Scripts/UI/HomeIllustrationFraming.cs`

| Paramètre | Valeur scène | Source |
|-----------|--------------|--------|
| `NativeWidth` | **1143** (const) | L19 |
| `NativeHeight` | **1920** (const) | L22 |
| `focusX` | **0.5** | `Hub.unity` L16537 |
| `focusY` | **0.38** (depuis le **haut**) | L16538 |
| `bottomZone` | ref `BottomZone` | L16539 |

Algorithme (pas letterbox / pas pillarbox) — **cover strict** :

```176:178:Assets/_Project/Scripts/UI/HomeIllustrationFraming.cs
            // Cover strict : remplit TOUTE la zone (aucun pillarbox / letterbox).
            float scale = Mathf.Max(zoneW / NativeWidth, zoneH / NativeHeight);
            float rigW = NativeWidth * scale;
```

- Zone = largeur page entière × (`parentH − bottomInset`).
- `bottomInset` = `bottomZone.anchoredPosition.y` (hauteur nav réelle, bleed inclus) — **pas** la hauteur des boutons (`ComputeBottomInset`, L245–252).
- Positionne le rig pour garder le focus dans la zone, puis **clamp** pour que le rig couvre toujours la zone (crop latéral/vertical possible).
- Force anchors/pivot centre, `localScale = 1`, écrit `sizeDelta` + `anchoredPosition`.
- Recalcule : `OnEnable`, `LateUpdate`, `OnRectTransformDimensionsChange`, retry 2 frames.

### RectTransform zone illustration

#### Rig (cadre art 1143×1920)

| | Unités ref canvas | Source |
|--|-------------------|--------|
| Ancres | min(0.5,0.5) max(0.5,0.5) pivot(0.5,0.5) | L16520–16524 |
| Taille sérialisée | **1005.0912 × 1688.3422** | L16523 |
| Scale cover implicite | `1688.3422 / 1920 = **0.8793449**` (= `1005.0912 / 1143`) | calcul |

#### Zone visible (page − nav) — calculs

**A) Réf 1080×1920, bleed bas = 0** (hypothèse safe.yMin=0) :

| Grandeur | Valeur |
|----------|--------|
| Page | 1080 × 1920 u |
| bottomInset | 152 u (`UiTheme.NavHeight`) |
| zone | **1080 × 1768** u |
| cover scale | `max(1080/1143, 1768/1920) = max(0.944881, 0.920833) = **0.944881**` |
| rig | **1080 × 1814.17** u |
| pixels device (= u, scaleFactor=1) | zone **1080 × 1768** px |

**B) A23 1080×2408, match=1, en utilisant l’inset sérialisé scène (BottomZone.y = 231.65784)** :

| Grandeur | Unités ref | Pixels device (× 2408/1920) |
|----------|------------|------------------------------|
| Canvas | 861.1296 × 1920 | 1080 × 2408 |
| zone W×H | **861.1296 × 1688.3422** | **1080 × 2117.50** |
| cover scale | **0.8793449** | — |
| rig | **1005.0912 × 1688.3422** | **1260.53 × 2117.50** |
| crop horizontal | rig plus large que zone (cover) | ~180 px art croppés côté device |

**C) A23, bleed bas = 0** (safe.yMin=0, pour borne) :

| Grandeur | Valeur |
|----------|--------|
| zone | 861.1296 × 1768 u → **1080 × 2217.17** px device |
| cover scale | `max(0.7534, 0.920833) = **0.920833**` |
| rig | 1052.51 × 1768 u |

### Vitre / cadre wagon — élément séparé ?

| Élément | Séparé ? | Détail |
|---------|----------|--------|
| Cadre wagon + banquette + **trou vitre** | **Oui (layer)** : `WagonLayer` / `base.png` | Rect stretch plein rig `anchorMin(0,0) max(1,1)` sizeDelta(0,0) — L39399–39403. Le **cadre est cuit dans l’art** ; le trou est **transparent** (alpha &lt; 16). |
| Reflets vitre | **Oui** : `Window` / `window reflection.png` | Même rect stretch plein rig — L18622–18626. |
| Rect « trou vitre » UI dédié | **Non** | Pas de masque/hole RectTransform séparé ; le paysage est révélé par l’alpha de `base.png`. |

Mesure alpha `base.png` (lecture pixels, pas Inspector) :

| Mesure | Valeur (px art natifs) |
|--------|-------------------------|
| Colonne centrale transparente | y **358 → 1086** (hauteur **729**) |
| Ligne médiane du trou (y=722) | x **0 → 1112** (largeur **1113**) |
| Bbox transparente échantillonnée (pas 2) | **(0,358)–(1112,1106)** ≈ **1113 × 749** |

---

## 3. TEXTURES DE L’ACCUEIL

Toutes les couches paysage / wagon / char listées ci-dessous sont stretchées plein `HomeIllustrationRig` (ancres 0–1, sizeDelta 0). Rect rendu sérialisé du rig = **1005.0912 × 1688.3422** u (`Hub.unity` L16523).

**Densité de grain (unités ref)** = `rig / native` :

- `1005.0912 / 1143 = **0.8793449**`
- `1688.3422 / 1920 = **0.8793449**` (identique)

**Densité device A23** (× `2408/1920 = 1.2541667`) = **1.10287** px device / px art.

GPU : `1143 × 1920 × 4` (RGBA32) = **8 770 560 B** ≈ **8.366 MiB** par texture.

Légende import (tous les `.meta` listés, Android/Default) :

| Champ meta | Valeur | Signification |
|------------|--------|----------------|
| `filterMode` | `0` | Point |
| `enableMipMap` | `0` | off |
| Default/Android `textureFormat` | `4` | RGBA32 |
| Default/Android `textureCompression` | `0` | Uncompressed |
| Standalone/iPhone `textureCompression` | `1` | Compressed (format `-1`) |
| `maxTextureSize` | `2048` | |
| `spritePixelsToUnits` | `100` | |
| `wrapU/V` | `0` = Repeat · `1` = Clamp | |

### Table par fichier

| Fichier | Chemin | Natif | filter | mip | format (Def/And) | compression Def/And | wrap U/V | maxSize | GPU | Grain ref (rig/natif) |
|---------|--------|-------|--------|-----|------------------|---------------------|----------|---------|-----|------------------------|
| sky.png | `Assets/_Project/Sprites/Hub/Parallax/sky.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| cloud.png | `…/Parallax/cloud.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| cloud1.png | `…/Parallax/cloud1.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| mountain.png | `…/Parallax/mountain.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| tree1.png | `…/Parallax/tree1.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| tree 2.png | `…/Parallax/tree 2.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| tree 3.png | `…/Parallax/tree 3.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Repeat** | 2048 | 8.366 MiB | **0.879345** |
| base.png | `Assets/_Project/Sprites/Hub/base.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Clamp** | 2048 | 8.366 MiB | **0.879345** |
| char.png | `Assets/_Project/Sprites/Hub/char.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Clamp** | 2048 | 8.366 MiB | **0.879345** |
| window reflection.png | `Assets/_Project/Sprites/Hub/window reflection.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Clamp** | 2048 | 8.366 MiB | **0.879345** |
| vfx.png | `Assets/_Project/Sprites/Hub/vfx.png` | **1143×1920** | Point | off | RGBA32 | Uncompressed | **Clamp** | 2048 | 8.366 MiB | **0.879345** |

Sources metas : lignes `filterMode` / `wrapU` / platformSettings de chaque `*.meta` (ex. `sky.png.meta` L37–41, L71–75, L109–114).

**Total GPU page Accueil (11 textures)** : `11 × 8 770 560 = **96 476 160 B** ≈ **92.03 MiB**`.

Ratio d’affichage paysage (identique pour les 7 RawImage) :

| | Unités ref | Pixels device A23 (×1.254167) |
|--|------------|-------------------------------|
| Rect rendu couche | **1005.0912 × 1688.3422** | **1260.53 × 2117.50** |
| Natif | 1143 × 1920 | — |
| Grain effectif | **0.879345** u/px art | **1.10287** px device/px art |

---

## 4. PARALLAXMANAGER

Script : `Assets/_Project/Scripts/Hub/ParallaxManager.cs`  
Instance Accueil : `LandscapeLayer` (`Hub.unity` L26410–26483), `m_Enabled: 1`.

### Couches animées + vitesses sérialisées

Ordre tableau `layers` = arrière → avant. Mapping image fileID → nœud / texture (audit §2) :

| # | RawImage fileID | Nœud | Texture | `scrollSpeed` | `uvRect` sérialisé |
|---|-----------------|------|---------|---------------|-------------------|
| 0 | 567356713 | `'Sky '` | sky.png | **0.05** | x=0 y=0 **width=0 height=0** |
| 1 | 774858019 | `'Clouds '` | cloud1.png | **0.06** | idem 0×0 |
| 2 | 1297058737 | `Clouds  (1)` | cloud.png | **0.08** | idem 0×0 |
| 3 | 810060587 | `'Mountain '` | mountain.png | **0.1** | idem 0×0 |
| 4 | 846561666 | `'Ground_rock '` | tree 3.png | **0.15** | idem 0×0 |
| 5 | 1849819270 | `'Hills_mid '` | tree 2.png | **0.2** | idem 0×0 |
| 6 | 1900027637 | `'Hills_far '` | tree1.png | **0.3** | idem 0×0 |

Autres champs sérialisés : `wagonTransform` → `WagonLayer` (1651044684), `shakeIntensity: 1.5`, `shakeSpeed: 40`, `isScrolling: 1`, `isShaking: 1`.

### Mécanisme UV / wrap (code)

Initialisation si `uvRect` largeur/hauteur ≤ 0 → copie depuis `RawImage.uvRect` runtime :

```77:89:Assets/_Project/Scripts/Hub/ParallaxManager.cs
        private void EnsureUvInitialized()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image != null
                    && (layers[i].uvRect.width <= 0f || layers[i].uvRect.height <= 0f))
                {
                    layers[i].uvRect = layers[i].image.uvRect;
                }
            }
        }
```

Scroll horizontal uniquement sur `uvRect.x` (pas de déplacement RectTransform des couches) :

```92:111:Assets/_Project/Scripts/Hub/ParallaxManager.cs
        private void UpdateParallax()
        {
            if (layers == null)
                return;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].image == null)
                    continue;

                float speed = layers[i].scrollSpeed * _speedMultiplier;
                layers[i].uvRect.x += speed * Time.unscaledDeltaTime;

                if (layers[i].uvRect.x > 1f)
                    layers[i].uvRect.x -= 1f;
                if (layers[i].uvRect.x < 0f)
                    layers[i].uvRect.x += 1f;

                layers[i].image.uvRect = layers[i].uvRect;
            }
        }
```

Wrap logiciel : modulo manuel sur `[0,1]` de `uvRect.x`.  
**Repeat requis sur les textures paysage** : oui — les 7 PNG parallaxe ont `wrapU/V: 0` (Repeat). Sans Repeat GPU, le scroll UV hors [0,1] (ou aux bords) afficherait du clamp/artefacts ; le code suppose une texture tileable.

Shake wagon (position, pas UV) :

```114:121:Assets/_Project/Scripts/Hub/ParallaxManager.cs
        private void UpdateShake()
        {
            float offsetY = Mathf.Sin(Time.unscaledTime * shakeSpeed) * shakeIntensity;
            float offsetX = Mathf.Sin(Time.unscaledTime * shakeSpeed * 0.7f)
                * (shakeIntensity * 0.3f);

            wagonTransform.anchoredPosition =
                _wagonOriginalPosition + new Vector2(offsetX, offsetY);
        }
```

### Allocations par frame

| Chemin | Alloc ? |
|--------|---------|
| `Update` → `UpdateParallax` | **Non** (mutation `Rect` + assignation `uvRect`) |
| `Update` → `UpdateShake` | `new Vector2(...)` struct stack — **pas d’alloc heap** |
| Strings / LINQ / `new` classe | **Aucun** dans Update |

---

## 5. RECT FINAL À COUVRIR (fin calcul B + vitre)

### B — A23 1080×2408, inset sérialisé (rappel complété)

| Grandeur | Unités ref | Pixels device (× 2408/1920) |
|----------|------------|------------------------------|
| Canvas (match=1) | 861.1296 × 1920 | 1080 × 2408 |
| Zone page − BottomZone | 861.1296 × 1688.3422 | **1080 × 2117.50** |
| **Rig HomeIllustration** | **1005.0912 × 1688.3422** | **1260.53 × 2117.50** |
| Cover scale | 0.8793449 | art→device **1.10287** |

### Vitre actuelle vs vitre jusqu’au haut du rig

**Important** : le RectTransform `Window` est **plein rig** (`anchorMin(0,0) max(1,1)` sizeDelta(0,0) — L18623–18626). Il n’ouvre **pas** une fenêtre géométrique UI ; il pose les reflets. L’ouverture réelle = **alpha de `base.png`** (`WagonLayer`), même rect plein rig.

| | Coordonnées art natif (1143×1920) | Fraction du canvas art / du rig | Sur rig sérialisé (u) | Sur A23 device (px), échelle 1.10287 |
|--|-----------------------------------|--------------------------------|------------------------|--------------------------------------|
| Ouverture actuelle (col. centrale alpha) | y **358 → 1086**, h **729** ; x ≈ **0 → 1112**, w **1113** | h/1920 = **37.97 %** ; marge haute 358/1920 = **18.65 %** | h = `729/1920 × 1688.3422 = **641.0**` ; top = `358/1920 × 1688.3422 = **314.8**` | h ≈ **707.0** ; top ≈ **347.1** |
| Si trou monte au **haut du canvas art** (yTop=0, bas inchangé y=1086) | h **1086** | **56.56 %** du rig | h = `1086/1920 × 1688.3422 = **955.0**` ; top = **0** | h ≈ **1053.3** |
| Gain vertical | +357 px art | +18.59 pts | +314.0 u | +346.3 px |

Avec **+10 %** sur hauteur d’ouverture actuelle (spec agrandissement) : `729 × 1.10 = **801.9**` px art → sur rig `801.9/1920 × 1688.3422 = **705.1**` u → device ≈ **777.7** px.

Rig **inchangé** dans tous ces cas (framing inchangé) : seul le masque art (`base.png` / future vitre) change la portion visible du paysage.

---

## 6. SYNTHÈSE

### Grain effectif par couche

| Couche | Grain ref (rig/natif) | Grain A23 device | Entier ? |
|--------|----------------------|------------------|----------|
| sky, cloud, cloud1, mountain, tree1, tree 2, tree 3 | **0.879345** | **1.10287** | **Non** |
| base, char, window reflection, vfx (même rect) | **0.879345** | **1.10287** | **Non** |

### Canvas recommandé — grain **identique** à l’existant

Pour conserver le même facteur 0.879345 sous le framing actuel (`NativeWidth/Height` 1143×1920) :

| | Pixels art |
|--|------------|
| Canvas couche (match framing) | **1143 × 1920** |
| Période de boucle horizontale (Repeat / `uvRect.x`) | **1143** (= largeur canvas ; convention « marge boucle » projet = **INTROUVABLE** au-delà de cette tuile) |
| Bande contenu verticale utile (trou actuel +10 %) | hauteur ≥ **802** dans la zone fenêtre |
| Bande si vitre jusqu’en haut du canvas | contenu dès **y = 0**, hauteur utile jusqu’au bas du trou (ex. **1086** si bas fixe) |
| Largeur utile derrière trou actuel | ≥ **1113** (trou mesuré) ; canvas reste **1143** pour le tile |

### ÉCHECS (écarts règles projet)

| # | Écart | Preuve |
|---|-------|--------|
| E1 | Grain **non entier** (0.879 / 1.103) | §3 ratio rig/natif |
| E2 | Standalone + iPhone : `textureCompression: 1` (≠ RGBA32 uncompressed maison) | tous les `*.meta` Hub listés, platformSettings Standalone/iPhone |
| E3 | filterMode / compression Android : **conformes** Point + RGBA32 Uncompressed | metas — **pas un échec** sur cible Android |
| E4 | `Sprites/Hub/` hors postprocessor UI/Combat (pas de stamper auto) | `UIImportPostprocessor` → `Sprites/UI/` seulement |
| E5 | BottomZone.y **231.66** ≠ NavigationBar.h **229.35** | `Hub.unity` L28653 vs L24455 |

### Rappel non-échecs Android

- Point, mipmaps off, Android RGBA32 uncompressed, maxSize 2048 ≥ 1143.
- 7 couches paysage déjà en **Repeat** (requis `ParallaxManager`).
- `ParallaxManager` : zéro alloc heap en Update.

---

### Annexe — personnage / overlays (conservée)

| Élément | Séparé ? |
|---------|----------|
| Perso + valise | `char.png` (valise cuite dedans) |
| Banquette / cadre vitre | `base.png` (trou alpha) |
| Reflets | `Window` / `window reflection.png` |

Ordre rendu : Landscape → Wagon → Window → Character → LightOverlay → BottomZone → Header / TopUtilityRow / Nav.

`PageTransitionController` : fade 0,15 s sur CanvasGroup ; parallaxe **continue** pendant le fade (GO actif) ; pas de freeze dédié.

---

*Fin du rapport (sections 3–6 complétées) — audit lecture seule, aucune modification scène/assets/code.*
