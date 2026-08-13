#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ChezArthur.Backend;
using ChezArthur.Core;
using ChezArthur.Meta;

namespace ChezArthur.Debugging
{
    /// <summary>
    /// Suite MT4-G3 Editor-safe : parse DTO, ApplyOverride, clone-swap-reset, gating.
    /// Fetch live dashboard = MANUAL.
    /// </summary>
    public static class RemoteTuningIntegritySuite
    {
        private const string TAG = "[G3Suite]";
        private static readonly List<string> _lines = new List<string>(32);
        private static int _pass;
        private static int _fail;
        private static int _manual;

        public static void Run()
        {
            _lines.Clear();
            _pass = 0;
            _fail = 0;
            _manual = 0;

            Log("═══════════════════════════════════════════════");
            Log("MT4-G3 — suite remote tuning (Editor-safe)");
            Log("═══════════════════════════════════════════════");

            try
            {
                Run01_ParseCalendarValid();
                Run02_ParseCalendarMalformed();
                Run03_ApplyOverrideBounds();
                Run04_CloneSwapReset();
                Run05_SeasonEnabledGating();
                Run06_Manual();
            }
            catch (Exception e)
            {
                _fail++;
                Log("ABORT — " + e.GetType().Name + " : " + e.Message);
            }
            finally
            {
                RemoteTuning.ResetOverrides();
                RemoteTuning.DebugSetLiveFlags(true, "");
                Log("───────────────────────────────────────────────");
                Log($"RÉSULTAT auto : {_pass} PASS · {_fail} FAIL · {_manual} MANUAL");
                Log("Colle tout le bloc [G3Suite] à Cursor.");
                Log("═══════════════════════════════════════════════");
                Flush();
            }
        }

        private static void Run01_ParseCalendarValid()
        {
            const string json =
                "{\"epochMondayIso\":\"2026-07-20\",\"seasonLengthWeeks\":6," +
                "\"lrBySeason\":[{\"seasonIndex\":1,\"lrCharacterId\":\"goat\"}]}";
            bool ok = RemoteTuning.DebugTryParseCalendar(json, out RemoteTuning.SeasonCalendarDto dto)
                && dto.seasonLengthWeeks == 6
                && dto.epochMondayIso == "2026-07-20";
            Assert(1, ok, "parse calendar valide");
        }

        private static void Run02_ParseCalendarMalformed()
        {
            bool rejected = !RemoteTuning.DebugTryParseCalendar("{not json", out _)
                && !RemoteTuning.DebugTryParseCalendar("", out _)
                && !RemoteTuning.DebugTryParseCalendar("{\"seasonLengthWeeks\":6}", out _);
            Assert(2, rejected, "parse calendar malformé / incomplet → refus");
        }

        private static void Run03_ApplyOverrideBounds()
        {
            SeasonRewardsConfig baseCfg = SeasonRewardsConfig.LoadDefault();
            SeasonRewardsConfig clone = UnityEngine.Object.Instantiate(baseCfg);

            var bad = new RemoteTuning.SeasonRewardsDto
            {
                tiers = new RemoteTuning.SeasonTierDto[3],
                prestigeStep = 150,
                prestigeTalsReward = 50
            };
            bool refused = !clone.ApplyOverride(bad);

            var goodTiers = new RemoteTuning.SeasonTierDto[baseCfg.TierCount];
            for (int i = 0; i < goodTiers.Length; i++)
            {
                SeasonTier t = baseCfg.GetTier(i);
                goodTiers[i] = new RemoteTuning.SeasonTierDto
                {
                    scoreRequired = t.scoreRequired,
                    talsReward = t.talsReward * 10,
                    grantsLrLevel = t.grantsLrLevel
                };
            }

            var good = new RemoteTuning.SeasonRewardsDto
            {
                tiers = goodTiers,
                prestigeStep = 150,
                prestigeTalsReward = 50
            };
            bool accepted = clone.ApplyOverride(good)
                && clone.GetTier(0) != null
                && clone.GetTier(0).talsReward == baseCfg.GetTier(0).talsReward * 10;

            UnityEngine.Object.Destroy(clone);
            Assert(3, refused && accepted, $"ApplyOverride bornes refuse={refused} accept×10={accepted}");
        }

        private static void Run04_CloneSwapReset()
        {
            SeasonRewardsConfig original = SeasonRewardsConfig.LoadDefault();
            int baseTals = original.GetTier(0).talsReward;

            SeasonRewardsConfig clone = UnityEngine.Object.Instantiate(original);
            var dto = new RemoteTuning.SeasonRewardsDto
            {
                tiers = new RemoteTuning.SeasonTierDto[original.TierCount],
                prestigeStep = original.PrestigeStep,
                prestigeTalsReward = original.PrestigeTalsReward
            };
            for (int i = 0; i < dto.tiers.Length; i++)
            {
                SeasonTier t = original.GetTier(i);
                dto.tiers[i] = new RemoteTuning.SeasonTierDto
                {
                    scoreRequired = t.scoreRequired,
                    talsReward = t.talsReward + 1,
                    grantsLrLevel = t.grantsLrLevel
                };
            }

            clone.ApplyOverride(dto);
            SeasonRewardsConfig.SetRuntimeInstance(clone);
            bool swapped = SeasonRewardsConfig.LoadDefault().GetTier(0).talsReward == baseTals + 1;

            SeasonRewardsConfig.ClearRuntimeOverride();
            bool reset = SeasonRewardsConfig.LoadDefault().GetTier(0).talsReward == baseTals;

            Assert(4, swapped && reset, $"clone-swap-reset swapped={swapped} reset={reset}");
        }

        private static void Run05_SeasonEnabledGating()
        {
            PersistentManager pm = PersistentManager.Instance;
            if (pm == null)
            {
                Assert(5, false, "PersistentManager absent");
                return;
            }

            string beforeId = pm.SeasonId ?? "";
            GameClock.SetDebugOverride(DateTime.UtcNow);
            RemoteTuning.DebugSetLiveFlags(false, "test maintenance");

            // Force un id save différent pour tenter un rollover
            if (!string.IsNullOrEmpty(beforeId))
                pm.SetSeasonId(beforeId + "_G3TEST");

            SeasonProgressManager.EnsureSeasonCurrent();
            bool deferred = (pm.SeasonId ?? "").EndsWith("_G3TEST", StringComparison.Ordinal)
                && !RemoteTuning.SeasonEnabled;

            // Restore
            if (!string.IsNullOrEmpty(beforeId))
                pm.SetSeasonId(beforeId);
            else
                pm.SetSeasonId(SeasonRotationManager.CurrentSeasonId);

            RemoteTuning.DebugSetLiveFlags(true, "");
            GameClock.SetDebugOverride(null);

            Assert(5, deferred, $"SeasonEnabled gating rollover différé={deferred}");
        }

        private static void Run06_Manual()
        {
            _manual++;
            Log("#06 MANUAL — dashboard live");
            Log("  a) Neutralité : JSON §0 → Force fetch → aucun changement visible.");
            Log("  b) epoch −1 semaine → CurrentSeasonId / countdown bougent.");
            Log("  c) talsReward×10 / unlockStage 3 / kill-switch / JSON cassé.");
            Log("  d) Reset overrides → retour asset.");
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

        private static void Log(string msg) => _lines.Add(TAG + " " + msg);

        private static void Flush()
        {
            var sb = new StringBuilder(2048);
            for (int i = 0; i < _lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_lines[i]);
            }
            Debug.Log(sb.ToString());
        }
    }
}
#endif
