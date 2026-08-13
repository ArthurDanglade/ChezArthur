#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using ChezArthur.Core;
using ChezArthur.Hub.Pages;
using ChezArthur.Localization;
using ChezArthur.Meta;
using ChezArthur.Backend;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Suite d'intégrité MT2-G6 (live) : HasTrustedTime, rollover différé offline, hint UI.
    /// Backup/restore save. APK + rotation partagée = MANUAL.
    /// </summary>
    public static class SeasonLiveIntegritySuite
    {
        private const string TAG = "[G6Suite]";
        private const string FakeSeasonId = "S_G6_FAKE";

        private static readonly List<string> _lines = new List<string>(32);
        private static int _pass;
        private static int _fail;
        private static int _manual;
        private static string _backupPath;

        /// <summary>
        /// Lance la suite depuis le Hub.
        /// </summary>
        public static void Run()
        {
            _lines.Clear();
            _pass = 0;
            _fail = 0;
            _manual = 0;
            _backupPath = null;

            Log("═══════════════════════════════════════════════");
            Log("MT2-G6 — suite live (trusted time / rollover gated)");
            Log("═══════════════════════════════════════════════");

            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
            {
                Log("ABORT — PersistentManager.Instance null (Hub en Play Mode).");
                Flush();
                return;
            }

            try
            {
                BackupSave(pm);
                GameClock.SetDebugOverride(null);
                SeasonRotationManager.SetDebugForcedWeekIndex(null);

                Run01_HasTrustedTimeSemantics();
                Run02_RolloverDeferredWithoutTrust(pm);
                Run03_RolloverWhenTrusted(pm);
                Run04_OfflineHintString();
                Run05_ManualLive();
            }
            catch (Exception e)
            {
                _fail++;
                Log("ABORT — " + e.GetType().Name + " : " + e.Message);
            }
            finally
            {
                GameClock.SetDebugOverride(null);
                if (!string.IsNullOrEmpty(_backupPath) && File.Exists(_backupPath))
                    RestoreFromPath(pm, _backupPath);
                // Ré-ancre serveur si online (la suite a ClearServerAnchor).
                BackendService.SyncServerTime();
                Log("───────────────────────────────────────────────");
                Log($"RÉSULTAT auto : {_pass} PASS · {_fail} FAIL · {_manual} MANUAL");
                Log("Colle tout le bloc [G6Suite] à Cursor.");
                Log("═══════════════════════════════════════════════");
                Flush();
            }
        }

        // ── Points ─────────────────────────────────

        private static void Run01_HasTrustedTimeSemantics()
        {
            GameClock.SetDebugOverride(null);
            GameClock.DebugClearServerAnchor();
            bool none = !GameClock.HasTrustedTime;

            GameClock.SetDebugOverride(DateTime.UtcNow);
            bool withOverride = GameClock.HasTrustedTime;

            GameClock.SetDebugOverride(null);
            GameClock.SetServerAnchor(DateTime.UtcNow, Time.realtimeSinceStartupAsDouble);
            bool withAnchor = GameClock.HasTrustedTime;

            GameClock.DebugClearServerAnchor();
            bool cleared = !GameClock.HasTrustedTime;

            Assert(
                1,
                none && withOverride && withAnchor && cleared,
                $"HasTrustedTime none={none} override={withOverride} " +
                $"anchor={withAnchor} cleared={cleared}");
        }

        private static void Run02_RolloverDeferredWithoutTrust(PersistentManager pm)
        {
            GameClock.SetDebugOverride(null);
            GameClock.DebugClearServerAnchor();

            string realCalc = SeasonRotationManager.CurrentSeasonId;
            pm.SetSeasonId(FakeSeasonId);

            SeasonProgressManager.EnsureSeasonCurrent();

            bool deferred = pm.SeasonId == FakeSeasonId
                && FakeSeasonId != realCalc
                && !GameClock.HasTrustedTime;

            Assert(
                2,
                deferred,
                $"rollover différé offline : save={pm.SeasonId} calc={realCalc} " +
                $"(attendu log [Season] Rollover différé…)");
        }

        private static void Run03_RolloverWhenTrusted(PersistentManager pm)
        {
            // Suite de #02 : save encore FakeSeasonId, pas d'ancre
            if (pm.SeasonId != FakeSeasonId)
                pm.SetSeasonId(FakeSeasonId);

            GameClock.SetDebugOverride(null);
            GameClock.DebugClearServerAnchor();

            // Override = trusted (tests MT2) — sans dépendre du réseau
            GameClock.SetDebugOverride(DateTime.UtcNow);
            string calc = SeasonRotationManager.CurrentSeasonId;

            SeasonProgressManager.EnsureSeasonCurrent();

            bool rolled = pm.SeasonId == calc
                && pm.SeasonId != FakeSeasonId
                && GameClock.HasTrustedTime;

            SeasonRecapData recap = pm.PendingSeasonRecap;
            bool recapFromFake = recap != null
                && recap.pending
                && recap.seasonId == FakeSeasonId;

            Assert(
                3,
                rolled && recapFromFake,
                $"rollover trusted (override) save={pm.SeasonId} calc={calc} " +
                $"recap.pending S={recap?.seasonId} pending={recap != null && recap.pending}");

            GameClock.SetDebugOverride(null);
        }

        private static void Run04_OfflineHintString()
        {
            GameClock.SetDebugOverride(null);
            GameClock.DebugClearServerAnchor();

            string hint = Loc.Tr(
                "ui.saison.offline",
                "Hors ligne — progression locale, synchronisation à la reconnexion");

            bool hintOk = !string.IsNullOrEmpty(hint) && hint.IndexOf("Hors ligne", StringComparison.Ordinal) >= 0;

            // UI : ouvrir page si présente et vérifier le countdown contient le hint
            bool uiOk = true;
            SeasonPageUI page = UnityEngine.Object.FindObjectOfType<SeasonPageUI>(true);
            if (page != null)
            {
                page.Open();
                // Refresh via Open ; lire TMP countdown via children
                TMPro.TextMeshProUGUI[] tmps = page.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                bool found = false;
                for (int i = 0; i < tmps.Length; i++)
                {
                    if (tmps[i] != null && tmps[i].text != null
                        && tmps[i].text.IndexOf("Hors ligne", StringComparison.Ordinal) >= 0)
                    {
                        found = true;
                        break;
                    }
                }

                uiOk = found;
                page.Close();
            }

            Assert(
                4,
                hintOk && uiOk,
                $"hint Loc OK={hintOk} · page saison contient « Hors ligne »={uiOk} " +
                $"(page={(page != null ? "trouvée" : "absente — Loc seul")})");
        }

        private static void Run05_ManualLive()
        {
            _manual++;
            Log("#05 MANUAL — checklist live device (hors auto)");
            Log("  a) Re-run suites G5 + G1 online (0 FAIL) — régression.");
            Log("  b) APK : delta≈0, countdown vrai lundi Paris, avion → temps stable, re-sync focus.");
            Log("  c) Editor + device online : même univers slot1, même semaine, même fin de saison.");
            Log("  d) Smoke : run, score, crans, claims, missions, Boss Rush.");
            Log("  → Si #01–#04 PASS, le code G6 est OK ; MANUAL = preuve live terrain.");
        }

        // ── Helpers ────────────────────────────────

        private static void BackupSave(PersistentManager pm)
        {
            pm.SaveGame();
            string source = Path.Combine(Application.persistentDataPath, "save.json");
            if (!File.Exists(source))
            {
                Log("WARN — pas de save.json avant backup.");
                return;
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            _backupPath = Path.Combine(Application.persistentDataPath, "save_g6_backup_" + stamp + ".json");
            File.Copy(source, _backupPath, overwrite: true);
            Log("Backup OK → " + _backupPath);
        }

        private static bool RestoreFromPath(PersistentManager pm, string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                if (parsed == null)
                    return false;
                SaveMigrator.MigrateToCurrent(parsed);
                SaveSystem.Save(parsed);
                pm.LoadGame();
                SeasonProgressManager.EnsureSeasonCurrent();
                Log("Restore OK → " + path);
                return true;
            }
            catch (Exception e)
            {
                Log("Restore FAIL — " + e.Message);
                return false;
            }
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

        private static void Log(string msg)
        {
            _lines.Add(TAG + " " + msg);
        }

        private static void Flush()
        {
            StringBuilder sb = new StringBuilder(2048);
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
