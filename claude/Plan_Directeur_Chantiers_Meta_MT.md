# Plan directeur — Chantiers Méta (MT) : « le jeu autour du jeu »

**Take Five Games — Track Zero** · 13 août 2026 · **v1.5** (màj 13/08 soir — **MT2 CLOS (local+live)** · **MT4-G1 CLOS** · prochain **MT4-G2**)
Réponse au constat du 04/08 : les chantiers visuels attendent les sprites de l'artiste ; G7 (calibrage) et les passes de tests profonds sont différés à l'arrivée des assets (acté par Arthur). Ce document cartographie tout ce qui peut avancer **sans un seul asset**, et dans quel ordre.
Vérité terrain : repo git + plans d'exécution par chantier (`Plan_Execution_MT*.md`).

---

## 1. État des lieux — le jeu dans son ensemble (13/08)

| Pilier | État constaté | Verdict |
|---|---|---|
| **Combat & contenu U1** | Socle refondu (G1–G6c). G7 calibrage différé — assets + critère 3 minutes. | ✅ Sain, pause volontaire |
| **SFX/VFX (F)** | F1–F3 actifs / clôtures en cours · F4–F5 à venir. | 🟢 Actif (lane parallèle) |
| **Gacha & reveal** | AW / INV en lanes parallèles — **ne pas croiser** avec MT. | 🟢 Actif |
| **Méta / hub — saisons** | **MT2 CLOS (G1–G6)** : rail local + live (temps serveur, rollover gated offline, rotation sync). Classement phase 2 dormant. UI saison brute (polish hors rail). | ✅ **CLOS** |
| **Transverse / backend** | Save durcie (MT0). **MT4-G1 CLOS** (Auth + Cloud Code + ancre). Prochain : **MT4-G2** Cloud Save + liaison compte. | 🟢 MT4 en cours |

**Lecture de manager** : saisons live actées. Prochain levier comptes = **MT4-G2** (politique de conflits à trancher à l'ouverture).

---

## 2. Décisions actées

| Réf. | Question | Décision |
|---|---|---|
| **MT-D1** | Saison v1 | Portails + seuils + reset — contrat v2. **Exécution G1–G6 CLOS** 13/08. |
| **MT-D2** | Backend | **Go UGS** 13/08. G1 temps serveur CLOS. G2 Cloud Save = suivant. |
| **MT-D3** | Langues | FR + EN dès la v1 (socle MT0-G2). |
| **MT-D4** | Premier chantier | MT0 Fondations — GO historique 04/08. |
| **MT-D5** | Matière première | `claude/raw_meta/` — scellée à l'ouverture des contrats. |

---

## 3. Backlog des chantiers MT

Méthode : audit → interview → gates → Go → prompt Cursor → contrôle diff → checklist → commit. **Graine QA** : suites `SeasonIntegritySuite` / `BackendIntegritySuite` / `SeasonLiveIntegritySuite` → **MT7**.

### MT0 — Fondations
Voir `Plan_Execution_MT0_Fondations.md`.

### MT1 — FTUE / Tuto
Pas ouvert en parallèle du socle UGS sauf Go explicite.

### MT2 — Saisons ✅ **CLOS**
Voir `Plan_Execution_MT2_Saisons.md` **v1.9 finale**. G1–G6 CLOS. Classement = phase 2.

### MT3 — Missions / quotidien
Après population live / MT1.

### MT4 — Backend & comptes *(EN COURS)*

| Gate | État |
|---|---|
| **MT4-0** | ✅ CLOS — Go UGS |
| **MT4-G1** | ✅ CLOS — Auth + temps serveur + HF1 |
| **MT4-G2** | ⏳ **prochain** — Cloud Save + liaison Google · politique conflits à trancher |
| **MT4-G3** | Remote Config |
| **MT4-G4** | Analytics + RGPD |
| **MT4-G5** | ✅ = MT2-G6 CLOS |

### MT5 / MT6 / MT7
Settings · reprise de run · CI/QA (gabarits suites).

---

## 4. Séquence (13/08)

```
CLOS        : MT2 (G1–G6 local+live) · MT4-0 · MT4-G1 · MT4-G5/G6
EN COURS    : MT4-G2 (Cloud Save)  ← prochain prompt + arbit conflits
Ensuite     : MT4-G3 Remote Config → G4 Analytics/RGPD
Parallèle   : F / INV · polish UI saison (design)
Plus tard   : MT1 · MT7 · MT3 · MT5 · MT6
Assets      : G7 + U2+
```

Coexistence : jamais `Feedback/**`, handlers de contenu, `UI/RevealStage/**` / `UI/InvocationFlow/**` depuis la lane MT.

---

## 5. Ce qu'on ne fait PAS maintenant

G7 / tuning global · classement peuplé · Crashlytics tant que Vitals suffisent · serveur maison.

---

## 6. Points ouverts

1. **Politique conflits Cloud Save** (trancher à l'ouverture MT4-G2).
2. Estimateur UGS avant launch public.
3. Crashlytics oui/non après Vitals.
4. Seuil population classement (v2 §12).
5. Monétisation v1.
6. Licence Epidemic Sound.

---

*Prochaine étape : Claude propose politique conflits + prompt **MT4-G2** → Cursor.*
