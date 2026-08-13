#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ChezArthur.Backend;
using ChezArthur.Meta;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Suite d'intégrité MT4-G1 + HF1. Log [G1Suite] PASS/FAIL collable.
    /// Couvre le cœur data/horloge ; quelques points restent MANUAL (boot offline réel, focus 5 min).
    /// </summary>
    public static class BackendIntegritySuite
    {
        private const string TAG = "[G1Suite]";
        private static readonly List<string> _lines = new List<string>(40);
        private static int _pass;
        private static int _fail;
        private static int _manual;
        private static long _floorBackup;
        private static bool _hadOverride;
        private static DateTime? _overrideBackup;

        /// <summary>
        /// Lance la suite (async — attendre le log final ~quelques secondes si online).
        /// </summary>
        public static async void Run()
        {
            _lines.Clear();
            _pass = 0;
            _fail = 0;
            _manual = 0;

            Log("═══════════════════════════════════════════════");
            Log("MT4-G1 + HF1 — suite d'intégrité backend / horloge");
            Log("═══════════════════════════════════════════════");

            BackupClockState();

            try
            {
                Run01_InitSafe();
                Run02_Hf1PoisonFloorHealed();
                Run03_DeviceAntiRollbackWithoutAnchor();
                Run04_DebugOverrideBeatsServer();
                await Run05_LiveSyncAsync();
                Run06_SeasonSmoke();
                Run07_ManualRemaining();
            }
            catch (Exception e)
            {
                _fail++;
                Log("ABORT — " + e.GetType().Name + " : " + e.Message);
            }
            finally
            {
                RestoreClockState();
                Log("───────────────────────────────────────────────");
                Log($"RÉSULTAT auto : {_pass} PASS · {_fail} FAIL · {_manual} MANUAL");
                Log("Colle tout le bloc [G1Suite] à Cursor.");
                Log("═══════════════════════════════════════════════");
                Flush();
            }
        }

        // ── Points ─────────────────────────────────

        private static void Run01_InitSafe()
        {
            bool threw = false;
            try
            {
                BackendService.Initialize();
                BackendService.Initialize(); // idempotent
            }
            catch
            {
                threw = true;
            }

            Assert(
                1,
                !threw,
                $"init fire-and-forget sans throw · IsInitialized={BackendService.IsInitialized} " +
                $"(peut être false encore async — OK)");
        }

        private static void Run02_Hf1PoisonFloorHealed()
        {
            GameClock.SetDebugOverride(null);
            GameClock.DebugClearServerAnchor();

            DateTime poisoned = DateTime.UtcNow.AddYears(2);
            GameClock.DebugSetFloorTicks(poisoned.Ticks);
            long floorBefore = GameClock.DebugGetFloorTicks();

            DateTime serverTruth = DateTime.UtcNow;
            GameClock.SetServerAnchor(serverTruth, Time.realtimeSinceStartupAsDouble);

            DateTime guarded = GameClock.UtcNowGuarded;
            long floorAfter = GameClock.DebugGetFloorTicks();
            double driftSec = Math.Abs((guarded - serverTruth).TotalSeconds);

            bool notStuckOnPoison = guarded.Ticks < floorBefore - TimeSpan.TicksPerDay;
            bool floorHealed = floorAfter <= serverTruth.Ticks + TimeSpan.TicksPerMinute;
            bool nearTruth = driftSec < 3.0;

            Assert(
                2,
                GameClock.HasServerTime && notStuckOnPoison && floorHealed && nearTruth,
                $"HF1 empoisonnement : drift={driftSec:0.00}s floorBefore>>now={notStuckOnPoison} " +
                $"floorHealed={floorHealed} HasServerTime={GameClock.HasServerTime}");
        }

        private static void Run03_DeviceAntiRollbackWithoutAnchor()
        {
            GameClock.SetDebugOverride(null);
            GameClock.DebugClearServerAnchor();

            DateTime now = DateTime.UtcNow;
            GameClock.DebugSetFloorTicks(now.Ticks);
            DateTime fakePast = now.AddHours(-1);
            DateTime evaluated = GameClock.DebugEvaluateAntiRollback(fakePast, out bool froze);

            Assert(
                3,
                froze && evaluated.Ticks == now.Ticks,
                $"anti-recul device (sans ancre) froze={froze} returned==floor={evaluated.Ticks == now.Ticks}");
        }

        private static void Run04_DebugOverrideBeatsServer()
        {
            GameClock.SetDebugOverride(null);
            DateTime serverTruth = DateTime.UtcNow;
            GameClock.SetServerAnchor(serverTruth, Time.realtimeSinceStartupAsDouble);

            GameClock.DebugAdvanceDays(7);
            DateTime withOverride = GameClock.UtcNowGuarded;
            bool overrideWins = GameClock.HasDebugOverride
                && withOverride > serverTruth.AddDays(6);

            GameClock.SetDebugOverride(null);
            DateTime afterClear = GameClock.UtcNowGuarded;
            double drift = Math.Abs((afterClear - DateTime.UtcNow).TotalSeconds);
            // Avec ancre : proche de serveur (= device si sync locale fraîche)
            bool backToServer = !GameClock.HasDebugOverride && drift < 5.0;

            Assert(
                4,
                overrideWins && backToServer,
                $"override>+7j={overrideWins} · Clear→serveur drift={drift:0.00}s={backToServer}");
        }

        private static async Task Run05_LiveSyncAsync()
        {
            GameClock.SetDebugOverride(null);

            // Laisse une chance à l'init async du boot
            if (!BackendService.IsInitialized || !BackendService.IsSignedIn)
            {
                BackendService.ForceReinitialize();
                await Task.Delay(2500);
            }

            bool synced = await BackendService.TrySyncServerTimeAsync();
            DateTime guarded = GameClock.UtcNowGuarded;
            DateTime device = DateTime.UtcNow;
            double delta = Math.Abs((guarded - device).TotalSeconds);

            bool ok = synced
                && BackendService.IsSignedIn
                && GameClock.HasServerTime
                && delta < 10.0;

            Assert(
                5,
                ok,
                $"sync live signed={BackendService.IsSignedIn} synced={synced} " +
                $"HasServerTime={GameClock.HasServerTime} Δdevice={delta:0.00}s " +
                $"(FAIL → réseau / script Cloud Code / projet Services)");
        }

        private static void Run06_SeasonSmoke()
        {
            string seasonId = SeasonRotationManager.CurrentSeasonId;
            TimeSpan until = SeasonRotationManager.GetTimeUntilSeasonEnd();
            bool ok = !string.IsNullOrEmpty(seasonId) && until >= TimeSpan.Zero;

            Assert(
                6,
                ok,
                $"smoke saison id={seasonId} reste={until.Days}j{until.Hours}h (horloge résolue OK)");
        }

        private static void Run07_ManualRemaining()
        {
            _manual++;
            Log("#07 MANUAL — points non auto (30–60 s si tu veux le 11/11 complet)");
            Log("  a) Offline réel : coupe réseau → Play → 1 warning [Backend] max, jeu OK.");
            Log("  b) Online, sync OK → horloge Windows +2 h SANS stop → ids saison stables.");
            Log("  c) Focus app > 5 min → retour → log re-sync.");
            Log("  d) Dashboard Cloud Code : appels GetServerTimeUtc visibles.");
            Log("  → Si #01–#06 PASS, le rail G1 data est OK ; MANUAL = confirmation terrain.");
        }

        // ── Helpers ────────────────────────────────

        private static void BackupClockState()
        {
            _floorBackup = GameClock.DebugGetFloorTicks();
            _hadOverride = GameClock.HasDebugOverride;
            _overrideBackup = null; // on clear toujours en fin pour ne pas laisser +7j
        }

        private static void RestoreClockState()
        {
            GameClock.SetDebugOverride(null);
            // Remet un plancher sain = maintenant (évite laisser un poison de test)
            GameClock.DebugSetFloorTicks(DateTime.UtcNow.Ticks);
            // Garde l'ancre si sync live a réussi (comportement désirable post-suite)
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
            StringBuilder sb = new StringBuilder(3072);
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
