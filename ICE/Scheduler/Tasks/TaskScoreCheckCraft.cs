using Dalamud.Game.ClientState.Conditions;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskScoreCheckCraft
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

            IceLogging.Debug($"Current Scoring Mission Id: {CosmicHelper.CurrentLunarMission}");
            var currentMission = C.Missions.Single(x => x.Id == CosmicHelper.CurrentLunarMission);
            var (currentScore, bronzeScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();
            bool enoughMain = MissionHandler.HaveEnoughMain().Value;
            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                if (SchedulerMain.AnimationLockAbandonState && (!AddonHelper.IsAddonActive("WKSRecipeNotebook") || !AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft])
                {
                    IceLogging.Error("[Score Checker] Aborting mission");
                    MissionHandler.TurnIn(z, true);
                    return;
                }

                if (enoughMain || SchedulerMain.State.HasFlag(IceState.AbortInProgress) || (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Craft) && CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Gather)))
                {
                    uint targetLevel = 0;
                    if (currentMission.TurnInGold)
                        targetLevel = 3;
                    else if (currentMission.TurnInSilver)
                        targetLevel = 2;
                    else if (currentMission.TurnInASAP)
                        targetLevel = 1;
                    IceLogging.Debug($"Current Score: {currentScore} | Bronze Goal: {bronzeScore} | Silver Goal: {silverScore} | Gold Goal: {goldScore} | Target Level: {targetLevel} | Abort State: {SchedulerMain.State.HasFlag(IceState.AbortInProgress)}");

                    if (targetLevel == 3)
                    {
                        if (currentScore >= goldScore ||
                        (SchedulerMain.State.HasFlag(IceState.AbortInProgress) && currentMission.TurnInSilver && currentScore > silverScore) ||
                        (SchedulerMain.State.HasFlag(IceState.AbortInProgress) && currentMission.TurnInASAP))
                        {
                            IceLogging.Debug("Gold was enabled, and you also meet gold threshold.");
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    else if (targetLevel == 2)
                    {
                        if (currentScore >= silverScore ||
                        (SchedulerMain.State.HasFlag(IceState.AbortInProgress) && currentMission.TurnInASAP))
                        {
                            IceLogging.Debug("Silver was enabled, and you also meet silver threshold.");
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    else if (targetLevel <= 1)
                    {
                        if (currentScore >= bronzeScore || bronzeScore == 0)
                        {
                            IceLogging.Debug("Turnin Asap was enabled, and true. Firing off");
                            MissionHandler.TurnIn(z);
                            return;
                        }
                    }
                    if (SchedulerMain.State.HasFlag(IceState.AbortInProgress))
                    {
                        IceLogging.Error("[Score Checker] Aborting mission");
                        MissionHandler.TurnIn(z, true);
                        return;
                    }
                    if (EzThrottler.Throttle("UI", 250) && CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Craft) && CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Gather))
                    {
                        var (craft, gather) = MissionHandler.HaveEnoughMainDual();
                        PlayerHelper.GetItemCount(48233, out int count);
                        IceLogging.Debug($"[Dual class] Craft Enough: {craft} | Gather Enough: {gather} | Cosmics: {count}");
                        if (gather) // Gathering complete
                        {
                            SchedulerMain.State &= ~IceState.Gather;
                            SchedulerMain.State &= ~IceState.ScoringMission;
                            MissionHandler.ExitCraftGatherUI();
                            return;
                        }
                        else if (count > 0 && !gather && !SchedulerMain.State.HasFlag(IceState.Gather)) // Neither Gathering nor Crafting complete but Cosmic still available.
                        {
                            SchedulerMain.State |= IceState.Gather;
                            SchedulerMain.State &= ~IceState.ScoringMission;
                            MissionHandler.ExitCraftGatherUI();
                            return;
                        }
                        else if (count == 0) // No cosmics
                        {
                            SchedulerMain.State |= IceState.AbortInProgress;
                        }
                    }
                }
                if ((Svc.Condition[ConditionFlag.PreparingToCraft] || Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.Gathering]) && !P.Artisan.GetEnduranceStatus())
                {
                    SchedulerMain.State &= ~IceState.ScoringMission;
                }
            }
            else
            {
                IceLogging.Debug("[Score Checker] Addon not ready or player is busy");
                CosmicHelper.OpenStellarMission();
            }
        }
    }
}
