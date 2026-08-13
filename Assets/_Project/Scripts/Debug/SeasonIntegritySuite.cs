#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ChezArthur.Characters;
using ChezArthur.Core;
using ChezArthur.Gacha;
using ChezArthur.Meta;
using ChezArthur.Missions;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Suite d'intégrité MT2-G5 : enchaîne S1→S2→S3 + robustesse, log [G5Suite] PASS/FAIL.
    /// Backup auto de la save au début, restauration à la fin (#14).
    /// </summary>
    public static class SeasonIntegritySuite
    {
        // ═══════════════════════════════════════════
        // CONSTANTES
        // ═══════════════════════════════════════════
        private const string TAG = "[G5Suite]";
        private const string FixtureRelative = "../claude/mt0/fixture_save_v0.json";

        // ═══════════════════════════════════════════
        // ÉTAT DE RUN
        // ═══════════════════════════════════════════
        private static readonly List<string> _lines = new List<string>(48);
        private static int _pass;
        private static int _fail;
        private static int _manual;
        private static string _backupPath;

        // ═══════════════════════════════════════════
        // API PUBLIQUE
        // ═══════════════════════════════════════════

        /// <summary>
        /// Lance toute la suite. Appeler depuis le Hub (PersistentManager présent).
        /// </summary>
        public static void Run()
        {
            _lines.Clear();
            _pass = 0;
            _fail = 0;
            _manual = 0;
            _backupPath = null;

            Log("═══════════════════════════════════════════════");
            Log("MT2-G5 — suite d'intégrité (rail local)");
            Log("═══════════════════════════════════════════════");

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
            {
                Log("ABORT — PersistentManager.Instance null (lance depuis Hub en Play Mode).");
                Flush();
                return;
            }

            try
            {
                BackupSave(pm);
                ResetClockAndWeek();

                Run01_ResetVirgin(pm);
                string lrS1 = Run02_LiveSeason(pm);
                Run03_IntraSeasonRotation();
                Run04_DumpBeforeRollover(pm);
                Run05_RolloverGate(pm, lrS1);
                Run06_PostRolloverInvariants(pm, lrS1);
                Run07_PortalData(lrS1);
                Run08_NoDoubleCredit(pm);
                string s2Id = Run09_OverwriteRecap(pm);
                Run10_PersistReload(pm, s2Id);
                Run11_AntiRollback();
                Run12_DeepMigration(pm);
                Run13_ManualLocale();
                Run14_RestoreBackup(pm);
            }
            catch (Exception e)
            {
                Log($"ABORT exception : {e.GetType().Name} — {e.Message}");
                _fail++;
                try
                {
                    if (!string.IsNullOrEmpty(_backupPath) && File.Exists(_backupPath))
                        RestoreFromPath(pm, _backupPath);
                }
                catch (Exception restoreEx)
                {
                    Log($"Restore d'urgence échouée : {restoreEx.Message}");
                }
            }
            finally
            {
                ResetClockAndWeek();
                Log("───────────────────────────────────────────────");
                Log($"RÉSULTAT auto : {_pass} PASS · {_fail} FAIL · {_manual} MANUAL");
                Log("Colle tout le bloc [G5Suite] à Cursor / Claude.");
                if (_manual > 0)
                    Log("Puis enchaîne les MANUAL détaillés en bas du log.");
                Log("═══════════════════════════════════════════════");
                Flush();
            }
        }

        // ═══════════════════════════════════════════
        // POINTS 01–14
        // ═══════════════════════════════════════════

        private static void Run01_ResetVirgin(PersistentManager pm)
        {
            SaveSystem.DeleteSave();
            pm.LoadGame();
            SeasonProgressManager.EnsureSeasonCurrent();
            pm.ResetUnlockedDifficulties();

            bool ok =
                pm.BestScoreThisSeason == 0
                && pm.RunsThisSeason == 0
                && (pm.ClaimedTiers == null || pm.ClaimedTiers.Count == 0)
                && !pm.IsDifficultyUnlocked(1)
                && (pm.PastSeasonLrIds == null || pm.PastSeasonLrIds.Count == 0)
                && (pm.PendingSeasonRecap == null
                    || (!pm.PendingSeasonRecap.pending && !pm.PendingSeasonRecap.rewardsCredited)
                    || string.IsNullOrEmpty(pm.PendingSeasonRecap.seasonId));

            // Recap fraîche après delete peut être objet vide — toléré si pas pending.
            if (pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.pending)
                ok = false;

            Assert(
                1,
                ok,
                $"saison vierge score={pm.BestScoreThisSeason} cransUnlocked1={pm.IsDifficultyUnlocked(1)} " +
                $"portal={Count(pm.PastSeasonLrIds)} season={pm.SeasonId}/{SeasonRotationManager.CurrentSeasonId}");
            DumpInline(pm, "après reset");
        }

        private static string Run02_LiveSeason(PersistentManager pm)
        {
            // Run x1 abandon ét.4 → score 4
            pm.TryImproveSeasonScore(4, 4, 1f);
            pm.IncrementSeasonRuns();

            // +50 ×2 → 104
            pm.TryImproveSeasonScore(pm.BestScoreThisSeason + 50, 0, 1f);
            pm.TryImproveSeasonScore(pm.BestScoreThisSeason + 50, 0, 1f);

            // Claims paliers 1–4 (index 0–3), PAS le 5
            bool claimsOk = true;
            for (int i = 0; i < 4; i++)
            {
                if (!SeasonRewards.TryClaim(i))
                    claimsOk = false;
            }

            // Cran x1,5 (index 1) — équivalent unlockStage atteint
            pm.UnlockDifficulty(1);
            // Run x1,5 ét.2 → score 3 : ne doit PAS battre 104
            bool improvedBad = pm.TryImproveSeasonScore(3, 2, 1.5f);

            int claimed = pm.ClaimedTiers != null ? pm.ClaimedTiers.Count : 0;
            bool ok =
                pm.BestScoreThisSeason == 104
                && claimsOk
                && claimed == 4
                && pm.IsDifficultyUnlocked(1)
                && !improvedBad
                && SeasonRewards.GetTierState(4) == SeasonTierState.Claimable;

            // Mission daily : soft (si une Completed existe)
            string missionNote = ClaimOneDailyIfPossible();

            SeasonRewardsConfig config = SeasonRewardsConfig.LoadDefault();
            string lrId = config != null ? config.GetLrIdForSeason(pm.SeasonId) : "goat";

            Assert(
                2,
                ok,
                $"score={pm.BestScoreThisSeason} claims={claimed}/4 tier5Claimable=" +
                $"{SeasonRewards.GetTierState(4) == SeasonTierState.Claimable} " +
                $"cran1.5={pm.IsDifficultyUnlocked(1)} score3rejected={!improvedBad} · {missionNote}");

            return lrId ?? "goat";
        }

        private static void Run03_IntraSeasonRotation()
        {
            int uBefore = SeasonRotationManager.GetCurrentUniverseAtSlot(0);
            int w = SeasonRotationManager.CurrentWeekIndex;
            SeasonRotationManager.SetDebugForcedWeekIndex((w + 1) % 5);
            int uAfter = SeasonRotationManager.GetCurrentUniverseAtSlot(0);
            bool ok = uAfter != uBefore;
            Assert(
                3,
                ok,
                $"rotation intra-saison slot0 {uBefore} → {uAfter} (semaine forcée)");
            SeasonRotationManager.SetDebugForcedWeekIndex(null);
        }

        private static void Run04_DumpBeforeRollover(PersistentManager pm)
        {
            DumpInline(pm, "photo avant rollover (#04)");
            bool ok =
                pm.BestScoreThisSeason == 104
                && pm.IsDifficultyUnlocked(1)
                && pm.ClaimedTiers != null
                && pm.ClaimedTiers.Count == 4;
            Assert(4, ok, "Dump n°2 cohérent (score 104, claims 4, cran 1.5)");
        }

        private static void Run05_RolloverGate(PersistentManager pm, string lrS1)
        {
            SeasonRotationManager.SetDebugForcedWeekIndex(null);
            string seasonBefore = pm.SeasonId;
            int talsBefore = pm.Tals;
            int goatBefore = GetCharLevel(pm, lrS1);

            // Forcer un ancrage d'override puis +42 j (6 semaines)
            GameClock.SetDebugOverride(DateTime.UtcNow);
            GameClock.DebugAdvanceDays(42);

            string calcAfter = SeasonRotationManager.CurrentSeasonId;
            SeasonProgressManager.EnsureSeasonCurrent();

            SeasonRecapData recap = pm.PendingSeasonRecap;
            bool pendingOk =
                recap != null
                && recap.pending
                && !recap.rewardsCredited
                && recap.finalScore == 104
                && recap.pendingLrLevels >= 1
                && recap.pendingTals > 0
                && calcAfter != seasonBefore
                && pm.SeasonId == calcAfter;

            // Crédit gate (comme OpenAsGate)
            SeasonRewards.CreditPendingRecap();
            if (pm.PendingSeasonRecap != null)
                pm.MarkRecapShown();

            int goatAfter = GetCharLevel(pm, lrS1);
            bool creditOk =
                pm.Tals > talsBefore
                && (goatAfter > goatBefore || pm.Characters != null && pm.Characters.OwnsCharacter(lrS1))
                && pm.PendingSeasonRecap != null
                && pm.PendingSeasonRecap.rewardsCredited;

            Assert(
                5,
                pendingOk && creditOk,
                $"rollover {seasonBefore}→{pm.SeasonId} finalScore={recap?.finalScore} " +
                $"pendingTals={recap?.pendingTals} lrLvl={recap?.pendingLrLevels} " +
                $"Tals {talsBefore}→{pm.Tals} goatLvl {goatBefore}→{goatAfter}");
        }

        private static void Run06_PostRolloverInvariants(PersistentManager pm, string lrS1)
        {
            DumpInline(pm, "après rollover S1→S2 (#06)");
            bool seasonBlockZero =
                pm.BestScoreThisSeason == 0
                && pm.BestStageThisSeason == 0
                && pm.RunsThisSeason == 0
                && (pm.ClaimedTiers == null || pm.ClaimedTiers.Count == 0)
                && pm.PrestigeTiersClaimed == 0;
            bool accountKept =
                pm.IsDifficultyUnlocked(1)
                && ContainsId(pm.PastSeasonLrIds, lrS1);
            bool recapCredited =
                pm.PendingSeasonRecap != null
                && pm.PendingSeasonRecap.rewardsCredited;

            string missionNote = "missions=n/a";
            MissionManager mm = MissionManager.Instance;
            if (mm != null && mm.IsInitialized)
            {
                mm.DebugForceApplyResets();
                missionNote = "ForceApplyResets OK (seasonal réévalué)";
            }

            Assert(
                6,
                seasonBlockZero && accountKept && recapCredited,
                $"blocSaisonZero={seasonBlockZero} crans+portal={accountKept} credited={recapCredited} · {missionNote}");
        }

        private static void Run07_PortalData(string lrS1)
        {
            bool unlocked = SeasonRewards.IsLrUnlockedForPortal(lrS1);
            bool bannerFound = false;
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:BannerData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                BannerData b = UnityEditor.AssetDatabase.LoadAssetAtPath<BannerData>(path);
                if (b != null && b.IsLrPortal)
                {
                    bannerFound = true;
                    break;
                }
            }
#else
            bannerFound = true; // hors Editor : assert data portail uniquement
#endif
            Assert(
                7,
                unlocked && bannerFound,
                $"LR '{lrS1}' dans pastSeasonLrIds={unlocked} · bannière isLrPortal trouvée={bannerFound}");
            Log("  → MANUAL soft : ouvre la bannière portail 5 s pour confirmer le LR visible (UI).");
        }

        private static void Run08_NoDoubleCredit(PersistentManager pm)
        {
            int talsBefore = pm.Tals;
            SeasonRewards.CreditPendingRecap();
            bool ok = pm.Tals == talsBefore;
            Assert(8, ok, $"re-crédit bloqué Tals stables={pm.Tals}");
        }

        private static string Run09_OverwriteRecap(PersistentManager pm)
        {
            string s1RecapId = pm.PendingSeasonRecap != null ? pm.PendingSeasonRecap.seasonId : "";
            pm.TryImproveSeasonScore(50, 0, 1f);
            // Ne rien claim
            string s2Before = pm.SeasonId;

            GameClock.DebugAdvanceDays(42);
            SeasonProgressManager.EnsureSeasonCurrent();

            SeasonRecapData recap = pm.PendingSeasonRecap;
            bool ok =
                recap != null
                && recap.pending
                && !recap.rewardsCredited
                && recap.finalScore == 50
                && recap.seasonId == s2Before
                && recap.seasonId != s1RecapId
                && recap.pendingLrLevels == 0
                && recap.pendingTals > 0
                && recap.lastTierReached >= 2;

            SeasonRewards.CreditPendingRecap();
            pm.MarkRecapShown();

            Assert(
                9,
                ok,
                $"écrasement récap S2 id={recap?.seasonId} (ancien S1={s1RecapId}) " +
                $"score={recap?.finalScore} lastTier={recap?.lastTierReached} " +
                $"pendingTals={recap?.pendingTals} lrLvl={recap?.pendingLrLevels}");

            return pm.SeasonId;
        }

        private static void Run10_PersistReload(PersistentManager pm, string expectedSeasonId)
        {
            int score = pm.BestScoreThisSeason;
            int tals = pm.Tals;
            bool pending = pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.pending;
            bool credited = pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.rewardsCredited;

            pm.SaveGame();
            pm.LoadGame();
            SeasonProgressManager.EnsureSeasonCurrent();

            bool ok =
                pm.SeasonId == expectedSeasonId
                && pm.BestScoreThisSeason == score
                && pm.Tals == tals
                && (pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.pending) == pending
                && (pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.rewardsCredited) == credited
                && !(pm.PendingSeasonRecap != null && pm.PendingSeasonRecap.pending && !pm.PendingSeasonRecap.rewardsCredited);

            Assert(
                10,
                ok,
                $"Save+Load (kill-app simulé) season={pm.SeasonId} score={pm.BestScoreThisSeason} " +
                $"pending={pending} credited={credited} — pas de double gate");
            Log("  → MANUAL soft : Stop Play → Play une fois pour valider le vrai kill Editor.");
        }

        private static void Run11_AntiRollback()
        {
            GameClock.SetDebugOverride(null);
            // Établit / rafraîchit le plancher prefs
            DateTime _ = GameClock.UtcNowGuarded;
            string raw = PlayerPrefs.GetString("GameClock_LastSeenUtcTicks", "");
            if (!long.TryParse(raw, out long floorTicks) || floorTicks <= 0)
            {
                Assert(11, false, "plancher PlayerPrefs absent après UtcNowGuarded");
                return;
            }

            DateTime fakePast = new DateTime(floorTicks, DateTimeKind.Utc).AddHours(-1);
            DateTime evaluated = GameClock.DebugEvaluateAntiRollback(fakePast, out bool froze);
            bool ok = froze && evaluated.Ticks == floorTicks;
            Assert(
                11,
                ok,
                $"anti-recul simulé (−1 h) froze={froze} returnedTicks==floor={evaluated.Ticks == floorTicks}");
            Log("  → MANUAL soft : optionnel horloge Windows −1 h hors override (même garde).");
        }

        private static void Run12_DeepMigration(PersistentManager pm)
        {
            string fixturePath = Path.GetFullPath(Path.Combine(Application.dataPath, FixtureRelative));
            if (!File.Exists(fixturePath))
            {
                Assert(12, false, $"fixture introuvable : {fixturePath}");
                return;
            }

            SaveData parsed;
            try
            {
                string json = File.ReadAllText(fixturePath);
                parsed = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Assert(12, false, $"parse fixture : {e.Message}");
                return;
            }

            if (parsed == null)
            {
                Assert(12, false, "fixture null après FromJson");
                return;
            }

            int from = parsed.saveVersion;
            bool migrated = SaveMigrator.MigrateToCurrent(parsed);
            bool versionOk = parsed.saveVersion == SaveSystem.CURRENT_SAVE_VERSION;

            SaveSystem.Save(parsed);
            pm.LoadGame();
            SeasonProgressManager.EnsureSeasonCurrent();

            OwnedCharacter goat = pm.Characters != null ? pm.Characters.GetOwnedCharacter("goat") : null;
            bool dataOk =
                pm.Tals == 1250
                && pm.BestStage == 17
                && goat != null
                && goat.level == 12
                && pm.BestScoreThisSeason == 0;

            Assert(
                12,
                migrated && versionOk && dataOk,
                $"migration v{from}→v{parsed.saveVersion} migrated={migrated} " +
                $"Tals={pm.Tals} bestStage={pm.BestStage} goatLvl={goat?.level} " +
                $"scoreSaison={pm.BestScoreThisSeason}");
        }

        private static void Run13_ManualLocale()
        {
            _manual++;
            Log("#13 MANUAL — FR/EN page saison + récap (après restore #14, sur ta vraie save OK)");
            Log("  1. Hub → Paramètres → bascule EN puis FR.");
            Log("  2. Bouton Saison (header) → parcours labels (titre, paliers, countdown).");
            Log("  3. « Revoir le dernier bilan » si dispo — ou DBG « Ouvrir récap (gate) » si pending.");
            Log("  4. OK = zéro clé brute type ui.saison.xxx, zéro texte coupé illisible.");
            Log("  → Coche mentalement #13 après restore ; ne bloque pas le score auto.");
        }

        private static void Run14_RestoreBackup(PersistentManager pm)
        {
            if (string.IsNullOrEmpty(_backupPath) || !File.Exists(_backupPath))
            {
                Assert(14, false, "backup introuvable — ta save n'a PAS été restaurée auto");
                return;
            }

            bool ok = RestoreFromPath(pm, _backupPath);
            Assert(14, ok, $"restore depuis {_backupPath}");
            DumpInline(pm, "après restore vraie save");
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════

        private static void BackupSave(PersistentManager pm)
        {
            pm.SaveGame();
            string source = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(source))
            {
                Log("WARN — pas de save.json avant backup (compte neuf ?) — #14 pourra échouer.");
                return;
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            _backupPath = Path.Combine(Application.persistentDataPath, "save_g5_backup_" + stamp + ".json");
            File.Copy(source, _backupPath, overwrite: true);
            Log($"Backup OK → {_backupPath}");
        }

        private static bool RestoreFromPath(PersistentManager pm, string path)
        {
            string json = File.ReadAllText(path);
            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            if (parsed == null)
                return false;

            SaveMigrator.MigrateToCurrent(parsed);
            SaveSystem.Save(parsed);
            pm.LoadGame();
            SeasonProgressManager.EnsureSeasonCurrent();
            return true;
        }

        private static void ResetClockAndWeek()
        {
            GameClock.SetDebugOverride(null);
            SeasonRotationManager.SetDebugForcedWeekIndex(null);
        }

        private static string ClaimOneDailyIfPossible()
        {
            MissionManager mm = MissionManager.Instance;
            if (mm == null || !mm.IsInitialized)
                return "missions absentes (soft skip)";

            List<MissionRuntimeEntry> buf = new List<MissionRuntimeEntry>(16);
            mm.GetEntriesForLayer(MissionLayer.Daily, buf);
            for (int i = 0; i < buf.Count; i++)
            {
                MissionRuntimeEntry e = buf[i];
                if (e == null || e.Data == null)
                    continue;
                if (e.State == MissionClaimState.Completed || e.IsClaimable)
                {
                    bool claimed = mm.TryClaim(e.Data.MissionId);
                    return claimed
                        ? $"daily claim OK ({e.Data.MissionId})"
                        : $"daily claim FAIL ({e.Data.MissionId})";
                }
            }

            return "aucune daily Completed (soft skip — OK pour le rail data)";
        }

        private static int GetCharLevel(PersistentManager pm, string id)
        {
            if (pm?.Characters == null || string.IsNullOrEmpty(id))
                return 0;
            OwnedCharacter o = pm.Characters.GetOwnedCharacter(id);
            return o != null ? o.level : 0;
        }

        private static bool ContainsId(IReadOnlyList<string> list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                    return true;
            }
            return false;
        }

        private static int Count(IReadOnlyList<string> list)
        {
            return list != null ? list.Count : 0;
        }

        private static void Assert(int num, bool ok, string detail)
        {
            if (ok)
            {
                _pass++;
                Log($"#{num:00} PASS — {detail}");
            }
            else
            {
                _fail++;
                Log($"#{num:00} FAIL — {detail}");
            }
        }

        private static void DumpInline(PersistentManager pm, string label)
        {
            if (pm == null)
                return;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            Log(
                $"  [Dump {label}] season={pm.SeasonId}/{SeasonRotationManager.CurrentSeasonId} " +
                $"score={pm.BestScoreThisSeason} claims={CountInts(pm.ClaimedTiers)} " +
                $"cran1.5={pm.IsDifficultyUnlocked(1)} portal={Count(pm.PastSeasonLrIds)} " +
                $"recap pending={recap != null && recap.pending} credited={recap != null && recap.rewardsCredited} " +
                $"Tals={pm.Tals}");
        }

        private static int CountInts(IReadOnlyList<int> list)
        {
            return list != null ? list.Count : 0;
        }

        private static void Log(string msg)
        {
            _lines.Add(TAG + " " + msg);
        }

        private static void Flush()
        {
            StringBuilder sb = new StringBuilder(4096);
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0)
                    sb.Append('\n');
                sb.Append(_lines[i]);
            }

            Debug.Log(sb.ToString());
        }
    }
}
#endif
