# Cahier des charges — MT4 Backend & comptes (MT4-0)

**Take Five Games — Track Zero** · 5 août 2026 · v1.1 — **Go acté 13/08/2026 : reco UGS + 5 gates** (comparatif vérifié web ; PlayFab free tier −99 % au 11/03/2026)
Rappel MT-D2 : solution managée, pas de serveur maison. Ambition « type Dokkan » à terme : comptes, données joueur, saisons pilotées.

---

## 1. Besoins (issus des chantiers, par ordre de priorité)

| # | Besoin | Consommateur | Urgence |
|---|---|---|---|
| B1 | **Temps serveur de confiance** | MT2-G6 (bornes de saison, rotation sync — remplace la garde locale) | **Bloquant G6** |
| B2 | **Remote config** : calendrier saisons (epoch, durée, LR/saison, grille), table de rotation | Constat n°5 (epoch en dur), live-ops sans update | Haute |
| B3 | **Comptes** : anonyme par défaut + liaison (Google) — « liez votre compte » (MT1-D3) | Cloud save, continuité multi-device | Haute |
| B4 | **Cloud save** : sync `save.json` v5, résolution de conflits | Changement de téléphone = zéro perte | Haute |
| B5 | Analytics (funnel, rétention) + crash reporting | Pilotage tuning/FTUE | Moyenne |
| B6 | **Classement saisonnier** à reset + archives (tableau d'honneur) | v2 §12 — phase 2 (population suffisante) | Différée (infra à choisir compatible) |
| B7 | RGPD : consentement, privacy policy, suppression de compte | Obligation release | Release |
| Contraintes | Offline-first (jeu 100 % jouable hors ligne, sync opportuniste) · coût ≈ 0 au lancement solo · Unity 2022.3 · évolutif vers du server-authoritative (gacha) sans refonte | | |

## 2. Comparatif (état vérifié au 05/08/2026)

| Critère | **Unity Gaming Services** | **PlayFab (Microsoft)** | **Firebase (Google)** |
|---|---|---|---|
| Free tier | Par service, plafonds réels (« real ceilings ») — ≈ 0 € à échelle de lancement solo ; estimateur à vérifier avant launch public | **Coupé de 100 000 → 1 000 MAU le 11/03/2026** ; « Foundation Mode » illimité **conditionné à un engagement de sortie Xbox** ; sinon Live Mode payant | Spark généreux à petite échelle (Auth/Firestore/Remote Config/Analytics/Crashlytics) |
| Comptes anonyme + liaison | ✅ Authentication (natif Unity) | ✅ | ✅ Auth |
| Cloud save | ✅ Cloud Save (clé-valeur) | ✅ | Firestore (à modeler soi-même) |
| **Classement saisonnier** | ✅ **Leaderboards natifs : resets programmés + versions + archives** (tableau d'honneur = archives consultables) + use-case Cloud Code « reward top players fin de saison » documenté | ✅ (statistiques versionnées) | ❌ à construire (Firestore + Functions = serveur maison light, contraire à MT-D2) |
| Temps serveur | ✅ trivial via Cloud Code (fonction serverless) | ✅ | ✅ (serverTimestamp / Function) |
| Remote config | ✅ Remote Config + Triggers | ✅ Title Data | ✅ Remote Config |
| Analytics / crash | Analytics UGS ; crash = Android Vitals (Play Console, gratuit) ou Crashlytics en complément | Télémétrie custom **exclue** du Foundation Mode | ✅ **Crashlytics/Analytics best-in-class** |
| Intégration Unity | ✅✅ SDK natifs, même éditeur, un seul vendor | SDK correct | SDK Unity maintenu (release notes actives) mais généraliste |
| Évolution server-authoritative | Cloud Code (JS serverless, même stack) | Foundation exclut serveurs dédiés/segmentation | Cloud Functions (hors free à l'usage) |

## 3. Recommandation manager — **ACTÉE (Go 13/08/2026)**

**Socle = Unity Gaming Services**, seul en v1. Motifs : PlayFab est **éliminé** par le changement du 11/03/2026 (1 000 MAU gratuits, illimité seulement contre engagement Xbox — hors sujet pour un launch Android solo) ; Firebase couvrirait B1–B5 mais **B6 exigerait de construire le classement saisonnier à la main** (Firestore + Functions = précisément le serveur maison que MT-D2 refuse), là où UGS l'a **en natif avec resets + archives** — c'est le match exact avec la v2 §12 (même la cérémonie « reward top players fin de saison » est un use-case documenté chez eux). Un seul vendor, un seul SDK, même écosystème que le moteur.
Crash reporting v1 : **Android Vitals** (gratuit, zéro SDK) ; **Firebase Crashlytics en option** si les Vitals se révèlent insuffisants — décision différée, non structurante (SDK additif).
**Risque consigné** : plafonds free UGS = durs (« une limite atteinte peut gater l'API ») → passage par l'estimateur officiel + alerte de facturation AVANT tout launch public ; et l'histoire PlayFab rappelle qu'un free tier peut changer — l'abstraction déjà en place (`GameClock` couture temps, save locale souveraine, offline-first) est notre assurance : le jeu reste 100 % fonctionnel sans backend.

## 4. Gates MT4 — **FIGÉS au Go**

| Gate | Périmètre |
|---|---|
| **MT4-G1** | Socle UGS : packages + init + **Auth anonyme** + **temps serveur** (Cloud Code `GetServerTimeUtc` → `GameClock` couture posée en MT2-G1, offline → garde locale actuelle). **Débloque MT2-G6.** |
| **MT4-G2** | Cloud Save : sync save v5 (horodatage serveur, résolution de conflits à spécifier au gate, garde anti-régression), liaison Google + UI paramètres (« liez votre compte » — MT1-D3 s'active) |
| **MT4-G3** | Remote Config : calendrier saisons + rotation + kill-switch (constat n°5 soldé) |
| **MT4-G4** | Analytics + RGPD (consentement, privacy policy, suppression compte/cloud) |
| **MT4-G5** | = **MT2-G6 live** : bornes serveur, rotation synchronisée · (classement = phase 2, infra prête, activation différée population) |

## 5. Points ouverts
Estimation chiffrée UGS à l'échelle cible (estimateur officiel, avant launch) · politique de résolution de conflits cloud save (à trancher MT4-G2) · Crashlytics oui/non (après premiers Vitals) · seuil de population classement (v2 §12, inchangé).

## Sources (vérifiées 05/08/2026)
[Unity — Pricing and billing](https://docs.unity.com/en-us/services/pricing-and-billing) · [UGS 2026 : stack, pricing, alternatives](https://crux.supercraft.host/blog/unity-gaming-services-alternatives/) · [AccelByte — PlayFab free tier cut 99%](https://accelbyte.io/blog/playfab-just-cut-its-free-tier-by-99.-heres-what-that-means-for-your-game) · [KeyStone — PlayFab 100K→1K (2026)](https://keystone-studios.com/blog/playfab-pricing-change-keystone-alternative) · [Microsoft — Updating an Account to Modern Pricing](https://learn.microsoft.com/it-it/gaming/playfab/features/pricing/transition) · [Unity Docs — Leaderboards Resets](https://docs.unity.com/en-us/leaderboards/concepts/resets) · [Unity Docs — Leaderboard archives and versions](https://docs.unity.com/ugs/en-us/manual/leaderboards/manual/concepts/archives) · [Unity Docs — Reward top players (fin de saison, Cloud Code)](https://docs.unity.com/ugs/en-us/manual/cloud-code/manual/triggers/tutorials/use-cases/reward-top-players) · [Firebase — pricing plans](https://firebase.google.com/docs/projects/billing/firebase-pricing-plans) · [Firebase Unity SDK — release notes](https://firebase.google.com/support/release-notes/unity)
