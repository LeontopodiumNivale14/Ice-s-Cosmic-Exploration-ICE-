using Dalamud.Game.ClientState.Conditions;
using Dalamud.Memory;
using ECommons.GameHelpers;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

internal static class MissionHandler
{
    internal static unsafe bool? HaveEnoughMain()
    {
        if (CosmicHelper.CurrentLunarMission == 0)
            return null;

        if (AddonHelper.GetNodeText("WKSMissionInfomation", 23).Contains("00:00"))
        {
            SchedulerMain.State |= IceState.AbortInProgress;
            return true;
        }
        else if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Critical))
        {
            var (currentScore, _, _, _) = GetCurrentScores();
            if (currentScore == 0)
                return false;
        }
        else if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Craft))
        {
            foreach (var main in CosmicHelper.CurrentMoonRecipe.MainCraftsDict)
            {
                var itemId = ExcelHelper.RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                var mainNeed = main.Value;
                PlayerHelper.GetItemCount((int)itemId, out var currentAmount);

                if (currentAmount < mainNeed)
                {
                    return false;
                }
            }
        }
        else if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Gather))
        {
            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Limited)
            && SchedulerMain.NodesVisited >= SchedulerMain.CurrentNodeSet.Count)
            {
                SchedulerMain.State |= IceState.AbortInProgress;
                return true;
            }
            else
            {
                foreach (var item in CosmicHelper.GatheringItemDict[CosmicHelper.CurrentLunarMission].MinGatherItems)
                {
                    if (PlayerHelper.GetItemCount((int)item.Key, out int count) && count < item.Value)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }
    internal static (bool craft, bool gather) HaveEnoughMainDual()
    {
        bool craft = false;
        bool gather = false;
        foreach (var main in CosmicHelper.CurrentMoonRecipe.MainCraftsDict)
        {
            var itemId = ExcelHelper.RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
            var mainNeed = main.Value;
            PlayerHelper.GetItemCount((int)itemId, out var currentAmount);
            craft = currentAmount >= mainNeed;
        }
        foreach (var item in CosmicHelper.GatheringItemDict[CosmicHelper.CurrentLunarMission].MinGatherItems)
            if (PlayerHelper.GetItemCount((int)item.Key, out int count))
                gather = craft ? count  >= item.Value : count >= item.Value * SchedulerMain.InitialGatheringItemMultiplier;
        return (craft, gather);
    }
    internal unsafe static (uint currentScore, uint bronzeScore, uint silverScore, uint goldScore) GetCurrentScores()
    {
        uint currentScore = 0, bronzeScore = 0, silverScore = 0, goldScore = 0;
        if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
        {
            if (AddonHelper.GetNodeText("WKSMissionInfomation", 23).Contains("00:00"))
                SchedulerMain.State |= IceState.AbortInProgress;

            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Critical))
            {
                string _stages = MemoryHelper.ReadSeStringNullTerminated((nint)z.Addon->AtkValues[5].String.Value).GetText(); // Returns Current/Max - "0/2"
                string[] _stagesSplit = _stages.Split('/');
                currentScore = uint.Parse(_stagesSplit[0]);
                silverScore = 1;
                goldScore = 2;
            }
            else if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.ScoreTimeRemaining))
            {
                currentScore = (uint)(HaveEnoughMain() == true ? 1 : 0);
                silverScore = 1;
                goldScore = 1;
            }
            else
            {
                string _currentScore = MemoryHelper.ReadSeStringNullTerminated((nint)z.Addon->AtkValues[2].String.Value).GetText();
                bronzeScore = CosmicHelper.CurrentMissionInfo.BronzeRequirement;
                silverScore = CosmicHelper.CurrentMissionInfo.SilverRequirement;
                goldScore = CosmicHelper.CurrentMissionInfo.GoldRequirement;

                // Remove all non-digit characters before parsing
                _currentScore = new string(_currentScore.Where(char.IsDigit).ToArray());

                uint.TryParse(_currentScore, out currentScore);
            }
        }
        else
        {
            CosmicHelper.OpenStellarMission();
        }

        return (currentScore, bronzeScore, silverScore, goldScore);
    }
    internal static void TurnIn(WKSMissionInfomation z, bool abortIfNoReport = false)
    {
        if (EzThrottler.Throttle("Turning in item", 250))
        {
            P.Artisan.SetEnduranceStatus(false);
            var (currentScore, bronzeScore, silverScore, goldScore) = GetCurrentScores();

            if (!(AddonHelper.IsAddonActive("WKSRecipeNotebook") || AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft])
            {
                if (SchedulerMain.PossiblyStuck > 0)
                {
                    IceLogging.Error("[TurnIn] Unexpected error. Potential Crafting Animation Lock.");
#if DEBUG
                    IceLogging.Error($"[TurnIn] PossiblyStuck: {SchedulerMain.PossiblyStuck} | AnimationLockToggle {C.AnimationLockAbandon} | AnimationLockState {SchedulerMain.AnimationLockAbandonState}");
#endif
                }
                if (SchedulerMain.PossiblyStuck < 2 && C.AnimationLockAbandon)
                {
                    SchedulerMain.PossiblyStuck += 1;
                }
                else if (SchedulerMain.PossiblyStuck >= 2 && C.AnimationLockAbandon)
                {
                    SchedulerMain.AnimationLockAbandonState = true;
                    DuoLog.Error($"Unexpected error. I might be Animation Locked. " +
                        (C.AnimationLockAbandon ? "Attempting unstuck." : "Please enable Experimental unstuck to attempt unstuck."));
                }
            }
        }
        var config = abortIfNoReport ? new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000, AbortOnTimeout = false } : new();
        IceLogging.Info("Attempting turnin");
        P.TaskManager.Enqueue(TurnInInternals, "Turning in", config);

        if (abortIfNoReport && C.StopOnAbort && !SchedulerMain.AnimationLockAbandonState)
        {
            SchedulerMain.StopBeforeGrab = true;
            DuoLog.Error("Unexpected error. Stopping. You failed to reach your Score Target.\n" +
                $"If you expect Mission ID {CosmicHelper.CurrentLunarMission} to not reach " + (C.Missions.SingleOrDefault(x => x.Id == CosmicHelper.CurrentLunarMission).TurnInSilver ? "Silver" : "Gold") +
                " - please mark it as Silver/ASAP accordingly.\n" +
                "If you were expecting it to reach the target, check your settings/gear.");
        }
        if ((abortIfNoReport || SchedulerMain.AnimationLockAbandonState) && CosmicHelper.CurrentLunarMission != 0)
        {
            SchedulerMain.Abandon = true;
            if (SchedulerMain.AnimationLockAbandonState)
                P.TaskManager.Enqueue(() => SchedulerMain.State |= IceState.AnimationLock, "Animation Lock", config);
            P.TaskManager.Enqueue(TaskMissionFind.AbandonMission, "Aborting mission", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration() { TimeLimitMS = 5000 });
        }
    }

    private static unsafe bool? TurnInInternals()
    {
        if (EzThrottler.Throttle("UI", 250))
        {
            if (CosmicHelper.CurrentLunarMission == 0)
            {
                TaskMissionFind.BlacklistedMission.Clear();
                SchedulerMain.State = IceState.GrabMission;
                return true;
            }

            if (!ExitCraftGatherUI())
                return false;

            if ((Job)PlayerHelper.GetClassJobId() != SchedulerMain.StartClassJob)
            {
                GearsetHandler.TaskClassChange(SchedulerMain.StartClassJob);
                return false;
            }

            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Critical) && !SchedulerMain.State.HasFlag(IceState.AbortInProgress))
            {
                if (EzThrottler.Throttle("Interacting with checkpoint", 250) && !Player.IsBusy)
                {
                    var gameObject = Utils.TryGetObjectCollectionPoint();
                    float gameObjectDistance = 999;
                    if (gameObject is not null)
                        gameObjectDistance = PlayerHelper.GetDistanceToPlayer(gameObject);
                    if (gameObjectDistance < 5)
                    {
                        P.Navmesh.Stop();
                        Utils.InteractWithObject(gameObject);
                    }
                    else if (gameObjectDistance < 999)
                        TaskGather.PathToNode(gameObject.Position);
                    else if (SchedulerMain.NearestCollectionPoint is not null)
                        TaskGather.PathToNode((Vector3)SchedulerMain.NearestCollectionPoint);
                }
                return false;
            }

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                z.Report();
                return false;
            }
            else
            {
                CosmicHelper.OpenStellarMission();
                return false;
            }
        }
        else
            return false;
    }

    public unsafe static bool ExitCraftGatherUI()
    {
        if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var cr) && cr.IsAddonReady)
        {
            IceLogging.Info("Player is preparing to craft, trying to fix");
            cr.Addon->FireCallbackInt(-1);
            return false;
        }
        else if (GenericHelpers.TryGetAddonMaster<GatheringMasterpiece>("GatheringMasterpiece", out var gm) && gm.IsAddonReady)
        {
            IceLogging.Info("Player is gathering collectibles, trying to fix");
            gm.Addon->FireCallbackInt(-1);
            return false;
        }
        else if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var g) && g.IsAddonReady)
        {
            IceLogging.Info("Player is gathering, trying to fix");
            g.Addon->FireCallbackInt(-1);
            return false;
        }
        return true;
    }
}