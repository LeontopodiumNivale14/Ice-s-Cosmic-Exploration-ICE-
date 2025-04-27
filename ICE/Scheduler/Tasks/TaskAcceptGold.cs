using ECommons.Logging;
using ECommons.Throttlers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskAcceptGold
    {
        public static void Enqueue()
        {
            P.taskManager.Enqueue(() => TurninGold(), "Waiting for valid turnin", DConfig);
            P.taskManager.Enqueue(() => PluginLog.Information("Turnin Complete"));
        }

        internal unsafe static bool? TurninGold()
        {
            uint currentScore = 0;
            uint goldScore = 0;

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                currentScore = x.CurrentScore;
                goldScore = x.GoldScore;
                bool scorecheck = currentScore != 0 && goldScore != 0;

                if (goldScore <= currentScore && PlayerNotBusy() && scorecheck)
                {
                    if (EzThrottler.Throttle("Turning in item"))
                    {
                        if (C.Once && currentScore>=goldScore)
                        {
                            PluginLog.Debug($"Turning in gold: {SchedulerMain.MissionName}");
                            C.EnabledMission.RemoveAll(e => e.Name == SchedulerMain.MissionName);
                            C.Save();
                        }
                        x.Report();
                        return true;
                    }
                }
            }

            return false;
        }

        internal unsafe static bool? LeaveTurnin()
        {
            if (!IsAddonActive("WKSMissionInfomation"))
            {
                return true;
            }

            return false; //
        }
    }
}
