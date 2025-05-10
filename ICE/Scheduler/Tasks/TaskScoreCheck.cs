using Dalamud.Game.ClientState.Conditions;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheck
    {
        public static void TryCheckScore()
        {
            if (SchedulerMain.AnimationLockAbandonState)
            {
                SchedulerMain.State = IceState.AnimationLock;
                return;
            }

            if (CosmicHelper.CurrentLunarMission == 0)
            {
                // this in theory shouldn't happen but going to add it just in case
                IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                SchedulerMain.State = IceState.GrabMission;
                return;
            }

            IceLogging.Debug($"[Score Checker] Current Scoring Mission Id: {CosmicHelper.CurrentLunarMission}", true);
            var currentMission = C.Missions.Single(x => x.Id == CosmicHelper.CurrentLunarMission);

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                if (SchedulerMain.State == IceState.AbortInProgress || (SchedulerMain.AnimationLockAbandonState && (!AddonHelper.IsAddonActive("WKSRecipeNotebook") || !AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft]))
                {
                    IceLogging.Error("[Score Checker] Aborting mission");
                    TurnIn(z, true);
                    return;
                }

                var (currentScore, silverScore, goldScore) = TaskCrafting.GetCurrentScores();

                if (currentScore != 0)
                {
                    if (IceLogging.ShouldLog())
                    {
                        IceLogging.Debug("[Score Checker] Score != 0", true);
                        IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal : {silverScore} | Gold Goal: {goldScore}", true);
                        IceLogging.Debug($"[Score Checker] Is Turnin Asap Enabled?: {currentMission.TurnInASAP}", true);
                    }

                    if (currentMission.TurnInASAP)
                    {
                        IceLogging.Info("$[Score Checker] Turnin Asap was enabled, and true. Firing off", true);
                        TurnIn(z);
                        return;
                    }

                    var enoughMain = TaskCrafting.HaveEnoughMain();
                    if (enoughMain == null)
                    {
                        IceLogging.Error("[Score Checker] Current mission is 0, aborting");
                        SchedulerMain.State = IceState.GrabMission;
                        return;
                    }

                    IceLogging.Debug($"[Score Checker] Checking current score:  {currentScore} is >= Silver Score: {silverScore} && {enoughMain.Value} && if TurninSilver is true: {currentMission.TurnInSilver}", true);

                    if (currentScore >= silverScore && enoughMain.Value && currentMission.TurnInSilver)
                    {
                        IceLogging.Info($"[Score Checker] Silver was enabled, and you also meet silver threshold.", true);
                        TurnIn(z);
                        return;
                    }

                    IceLogging.Debug($"[Score Checker] Seeing if Player not busy: {PlayerHelper.IsPlayerNotBusy()} && is not Preparing to craft: {Svc.Condition[ConditionFlag.PreparingToCraft]}", true);
                    if (currentScore >= goldScore)
                    {
                        IceLogging.Info($"[Score Checker] Conditions for gold was met. Turning in", true);
                        TurnIn(z);
                        return;
                    }
                }

                IceLogging.Debug($"[Score Checker] Player is in state: {string.Join(',', Svc.Condition.AsReadOnlySet().Select(x => x.ToString()))}", true);
                IceLogging.Debug($"[Score Checker] Artisan is busy?: {P.Artisan.IsBusy()}", true);
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions]) && !P.Artisan.GetEnduranceStatus())
                {
                    IceLogging.Debug($"[Score Checker] Current Score: {currentScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore}", true);
                    IceLogging.Debug("[Score Checker] Player is not busy but hasnt hit score, resetting state to try craft", true);
                    SchedulerMain.State = IceState.StartCraft;
                }
            }
            else
            {
                IceLogging.Debug("[Score Checker] Addon not ready or player is busy");
                CosmicHelper.OpenStellaMission();
            }
        }

        private unsafe static void TurnIn(WKSMissionInfomation z, bool abortIfNoReport = false)
        {
            if (IceLogging.ShouldLog("Turning in item", 250))
            {
                P.Artisan.SetEnduranceStatus(false);
                var (currentScore, silverScore, goldScore) = TaskCrafting.GetCurrentScores();
                
                if (!(AddonHelper.IsAddonActive("WKSRecipeNotebook") || AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft])
                {
                    IceLogging.Error("[TurnIn] Unexpected error. Potential Crafting Animation Lock.");
#if DEBUG
                    IceLogging.Error($"[TurnIn] PossiblyStuck: {SchedulerMain.PossiblyStuck} | AnimationLockToggle {C.AnimationLockAbandon} | AnimationLockState {SchedulerMain.AnimationLockAbandonState}");
#endif
                    if (SchedulerMain.PossiblyStuck < 2 && C.AnimationLockAbandon)
                    {
                        SchedulerMain.PossiblyStuck += 1;
                    }
                    else if (SchedulerMain.PossiblyStuck >= 2 && C.AnimationLockAbandon)
                    {
                        SchedulerMain.AnimationLockAbandonState = true;
                        Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                        {
                            Message = "[ICE] Unexpected error. I might be Animation Locked. Trigger count: " + SchedulerMain.PossiblyStuck + " " +
                            (C.AnimationLockAbandon ? "Attempting experimental unstuck." : "Please enable Experimental unstuck to attempt unstuck."),
                            Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                        });
                    }
                }
                if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var cr) && cr.IsAddonReady && currentScore < goldScore)
                {
                    IceLogging.Info("[Score Checker] Player is preparing to craft, trying to fix", true);
                    P.Artisan.SetStopRequest(true);
                    // cr.Addon->FireCallbackInt(-1);
                }
            }
            if (!SchedulerMain.AnimationLockAbandonState)
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.NormalConditions] == true, new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 1000 });

            var config = abortIfNoReport ? new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000, AbortOnTimeout = false } : new();
            IceLogging.Info("[TurnIn] Attempting turnin", true);
            P.TaskManager.Enqueue(TurnInInternals, "Changing to grab mission", config);

            if (abortIfNoReport && C.StopOnAbort && !SchedulerMain.AnimationLockAbandonState)
            {
                SchedulerMain.StopBeforeGrab = true;
                Svc.Chat.Print(new Dalamud.Game.Text.XivChatEntry()
                {
                    Message = "[ICE] Unexpected error. Insufficient materials. Stopping. You failed to reach your Score Target.\n" +
                    $"If you expect Mission ID {CosmicHelper.CurrentLunarMission} to not reach " + (C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).TurnInSilver ? "Silver" : "Gold") +
                    " - please mark it as Silver/ASAP accordingly.\n" +
                    "If you were expecting it to reach the target, check your Artisan settings/gear.",
                    Type = Dalamud.Game.Text.XivChatType.ErrorMessage,
                });
            }
            if ((abortIfNoReport || SchedulerMain.AnimationLockAbandonState) && CosmicHelper.CurrentLunarMission != 0)
            {
                SchedulerMain.Abandon = true;
                if (SchedulerMain.AnimationLockAbandonState)
                    P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.AnimationLock, "Animation Lock", config);
                P.TaskManager.Enqueue(TaskMissionFind.AbandonMission, "Aborting mission", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000 });
            }
        }

        private static bool? TurnInInternals()
        {
            if (CosmicHelper.CurrentLunarMission == 0)
            {
                TaskMissionFind.BlacklistedMission.Clear();
                SchedulerMain.State = IceState.GrabMission;
                return true;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                //IceLogging.Debug("[Score Checker] REPORTING", true);
                z.Report();
            }
            else 
            {
                CosmicHelper.OpenStellaMission();
            }

            return false;
        }
    }
}
