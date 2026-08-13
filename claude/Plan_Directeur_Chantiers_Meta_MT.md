# Plan directeur — Chantiers Méta (MT) : « le jeu autour du jeu »

**Take Five Games — Track Zero** · 13 août 2026 · **v1.4** (màj 13/08 — **MT2 rail local CLOS** · **MT4-0 Go UGS** · suite d'intégrité = graine MT7)
Réponse au constat du 04/08 : les chantiers visuels attendent les sprites de l'artiste ; G7 (calibrage) et les passes de tests profonds sont différés à l'arrivée des assets (acté par Arthur). Ce document cartographie tout ce qui peut avancer **sans un seul asset**, et dans quel ordre.
Vérité terrain : repo git + plans d'exécution par chantier (`Plan_Execution_MT*.md`).

---

## 1. État des lieux — le jeu dans son ensemble (13/08)

| Pilier | État constaté | Verdict |
|---|---|---|
| **Combat & contenu U1** | Socle refondu (G1–G6c). G7 calibrage différé — assets + critère 3 minutes. | ✅ Sain, pause volontaire |
| **SFX/VFX (F)** | F1–F3 actifs / clôtures en cours · F4–F5 à venir. | 🟢 Actif (lane parallèle) |
| **Gacha & reveal** | AW / INV en lanes parallèles — **ne pas croiser** avec MT. | 🟢 Actif |
| **Méta / hub — saisons** | **MT2 rail local CLOS (G1–G5)** : score, crans, piste 12+prestige, LR/portail, pages + récap gate, rollover validé par `SeasonIntegritySuite` (13 PASS / 0 FAIL + MANUAL). **G6 live** attend temps serveur (MT4-G1). UI saison brute (polish design hors rail). | ✅ Rail local · ⏳ live |
| **Transverse / backend** | Save durcie + migrations (MT0). `GameClock` couture anti-recul locale. **MT4-0 CLOS** : cahier + comparatif web → **Go UGS seul v1**. | 🟢 MT4 en cours |

**Lecture de manager** : le rail saisons local est soldé ; le prochain levier produit bloquant pour le live est **MT4-G1** (Auth + temps serveur → débloque MT2-G6).

---

## 2. Décisions actées

| Réf. | Question | Décision |
|---|---|---|
| **MT-D1** | Saison v1 | Portails + seuils + reset — contrat v2 acté (MT2-0). Exécution rail local **CLOS** 13/08. |
| **MT-D2** | Backend | Solution managée, pas de serveur maison. **Go 13/08 : UGS seul en v1** (Auth, Cloud Save, Remote Config, Cloud Code temps serveur, Leaderboards prêts phase 2). PlayFab éliminé (free tier 100k→1k MAU + Xbox). Firebase écarté pour B6 classement (à construire = contraire MT-D2). Crash = Android Vitals v1 ; Crashlytics option différée. Offline-first + save souveraine = anti-vendor. Cahier : `claude/Cahier_Charges_Backend_MT4.md`. |
| **MT-D3** | Langues | FR + EN dès la v1 (socle MT0-G2). |
| **MT-D4** | Premier chantier | MT0 Fondations — GO historique 04/08. |
| **MT-D5** | Matière première | `claude/raw_meta/` — scellée à l'ouverture des contrats. |

---

## 3. Backlog des chantiers MT

Méthode : audit → interview → gates → Go → prompt Cursor → contrôle diff → checklist → commit. **Graine QA** : `SeasonIntegritySuite` (MT2-G5) = gabarit des futures suites → consignées sous **MT7**.

### MT0 — Fondations

Voir `Plan_Execution_MT0_Fondations.md`. Save / loc / états — selon journal à HEAD.

### MT1 — FTUE / Tuto

Moteur data-driven + script v1. Dépend MT0. Pas ouvert en parallèle du socle UGS sauf Go explicite.

### MT2 — Saisons *(rail local CLOS)*

Voir `Plan_Execution_MT2_Saisons.md` v1.7. **G1–G5 CLOS.** **G6** = live bornes serveur = **MT4-G5** / déclenché par MT4-G1.

### MT3 — Missions / quotidien

Compléter catalogue / équilibrage après population live. Après MT1/MT2 live.

### MT4 — Backend & comptes *(EN COURS — Go UGS 13/08)*

Cahier : `claude/Cahier_Charges_Backend_MT4.md`. Gates figés :

| Gate | Périmètre | État |
|---|---|---|
| **MT4-0** | Cahier + comparatif web + reco | ✅ **CLOS** · Go UGS |
| **MT4-G1** | Packages UGS + init + Auth anonyme + temps serveur Cloud Code → couture `GameClock` (offline = garde locale) | ⏳ **prompt à rédiger** — débloque MT2-G6 |
| **MT4-G2** | Cloud Save v5 + liaison Google + UI « liez votre compte » | après G1 |
| **MT4-G3** | Remote Config calendrier / rotation / kill-switch | après G2 |
| **MT4-G4** | Analytics + RGPD | après G3 |
| **MT4-G5** | = **MT2-G6 live** (bornes + rotation sync) · classement = phase 2 | après G1+ |

### MT5 — Settings / hub

Paramètres complets, Magasin/News = décision monétisation. Coordination F4/F5.

### MT6 — Reprise de run

Absent à HEAD. Après base mobile stable.

### MT7 — Pipeline & QA

CI, build Android, piste interne, **plan de tests** — **réutiliser le gabarit `SeasonIntegritySuite`**.

---

## 4. Séquence (13/08)

```
CLOS        : MT2 rail local (G1–G5) · MT4-0 (cahier + Go UGS)
EN COURS    : MT4-G1 (Auth + temps serveur)  ← prochain prompt
DÉBLOQUÉ    : MT2-G6 / MT4-G5 live  ← après MT4-G1
Ensuite     : MT4-G2 Cloud Save → G3 Remote Config → G4 Analytics/RGPD
Parallèle   : F / INV (lanes isolées) · polish UI saison (design, hors rail)
Plus tard   : MT1 FTUE · MT7 CI/QA (graine suite) · MT3 · MT5 · MT6
Assets      : G7 + contenu U2+
```

Coexistence : jamais `Feedback/**`, handlers de contenu, `UI/RevealStage/**` / `UI/InvocationFlow/**` depuis la lane MT ; scènes/data en commits séparés.

---

## 5. Ce qu'on ne fait PAS maintenant

G7 / tuning global · monétisation non tranchée · classement saisonnier peuplé (infra prête, activation différée) · Crashlytics tant que Vitals suffisent · serveur maison.

---

## 6. Points ouverts

1. Estimateur UGS officiel avant launch public (plafonds free durs).
2. Politique conflits Cloud Save (trancher au MT4-G2).
3. Crashlytics oui/non après premiers Vitals.
4. Seuil population classement (v2 §12).
5. Monétisation v1 (IAP / pub / rien).
6. Licence Epidemic Sound avant release.

---

*Prochaine étape : Claude livre `Prompt_Cursor_MT4_G1.md` → Cursor colle → push → contrôle diff → checklist Auth + horloge serveur.*
