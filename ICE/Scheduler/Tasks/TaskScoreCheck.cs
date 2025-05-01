using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using ECommons.Throttlers;
using ICE.Utilities;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheck
    {
        public static void TryCheckScore()
        {
            PluginDebug($"Current Scoring Mission Id: {CurrentLunarMission}");

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var (currentScore, silverScore, goldScore) = TaskCrafting.GetCurrentScores();

                if (currentScore != 0)
                {
                    if (LogThrottle)
                    {
                        PluginDebug("[Score Checker] Score != 0");

                        PluginDebug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}");

                        PluginDebug($"[Score Checker] Is Turnin Asap Enabled?: {C.TurninASAP}");
                    }

                    if (C.TurninASAP)
                    {
                        PluginDebug("$[Score Checker] Turnin Asap was enabled, and true. Firing off");
                        TurnIn(z);
                        return;
                    }

                    if (LogThrottle)
                        PluginDebug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && {TaskCrafting.HaveEnoughMain()} && if TurninSilver is true: {C.TurninOnSilver}");
                    if (currentScore >= silverScore && TaskCrafting.HaveEnoughMain() && C.TurninOnSilver)
                    {
                        PluginDebug($"Silver was enabled, and you also meet silver threshold. ");
                        TurnIn(z);
                        return;
                    }

                    if (LogThrottle)
                        PluginDebug($"[Score Checker] Seeing if Player not busy: {PlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}");
                    if (PlayerNotBusy() && currentScore >= goldScore)
                    {
                        PluginDebug($"[Score Checker] Conditions for gold was met. Turning in");
                        TurnIn(z);
                        return;
                    }
                }

                PluginDebug($"[Score Checker] Player is in state: {string.Join(',', Svc.Condition.AsReadOnlySet().Select(x => x.ToString()))}");
                PluginDebug($"[Score Checker] Artisan is busy?: {P.Artisan.IsBusy()}");
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
                {
                    PluginDebug("[Score Checker] Player is not busy but hasnt hit score, resetting state to try craft");
                    SchedulerMain.State = IceState.StartCraft;
                }
            }
            else
            {
                PluginDebug("[Score Checker] Addon not ready or player is busy");
                OpenStellaMission();
            }
        }

        private unsafe static void TurnIn(WKSMissionInfomation z)
        {
            if (EzThrottler.Throttle("Turning in item", 100))
            {
                P.Artisan.SetEnduranceStatus(false);
                var scores = TaskCrafting.GetCurrentScores();
                if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var cr) && cr.IsAddonReady && scores.currentScore < scores.goldScore)
                {
                    PluginDebug("[Score Checker] Player is preparing to craft, trying to fix");
                    cr.Addon->FireCallbackInt(-1);
                }
                P.TaskManager.EnqueueDelay(1500);

                P.TaskManager.Enqueue(TurnInInternals, "Changing to grab mission");
            }
        }

        private static bool? TurnInInternals()
        {
            if (CurrentLunarMission == 0)
            {
                SchedulerMain.State = IceState.GrabMission;
                return true;
            }

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                z.Report();
                if (C.StopNextLoop)
                {
                    SchedulerMain.DisablePlugin();
                    C.StopNextLoop = false;
                    C.Save();
                } 
                return false;
            }

            return false;
        }
    }
}
