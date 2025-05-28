using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Collections.Generic;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskGather
    {
        /* Moreso laying the floorplans for all of this, because this is going to get messy w/o any pre-planning
         * First things first, there's several types of missions for gathering
         * -> Quantity Limited (Gather x amount on limited amount of nodes)
         * -> Quantity (Gather x amount, gather more for increased score)
         * -> Timed (Gather x amount in the time limit)
         * -> Chain (Increase score based on chain)
         * -> Gatherer's Boon (Increase score by hitting boon % chance)
         * -> Chain + Boon (Get score from chain nodes + boon % chance)
         * -> Collectables (This is going to be annoying)
         * -> Time Steller Reduction (???) (Assuming Collectables -> Reducing for score... fuck)
         * 
         * Which gives us... 8 different kinds to account for. gdi.
         * 
        */

        public static void TryEnqueueGathering()
        {
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                Job targetClass;
                if (((Job)CosmicHelper.CurrentMissionInfo.JobId).IsDol())
                    targetClass = (Job)CosmicHelper.CurrentMissionInfo.JobId;
                else if (((Job)CosmicHelper.CurrentMissionInfo.JobId2).IsDol())
                    targetClass = (Job)CosmicHelper.CurrentMissionInfo.JobId2;
                else
                    return;
                if ((Job)PlayerHelper.GetClassJobId() != targetClass)
                    GearsetHandler.TaskClassChange(targetClass);
                else
                    MakeGatheringTask();
            }
        }

        // Version 2 of the gathering task. Trying to improve on it all...
        internal static void MakeGatheringTask()
        {
            var (currentScore, bronzeScore, silverScore, goldScore) = MissionHandler.GetCurrentScores();

            if (currentScore == 0 && silverScore == 0 && goldScore == 0)
            {
                IceLogging.Debug("Failed to get scores, aborting");
                return;
            }

            if (MissionHandler.IsMissionTimedOut())
            {
                SchedulerMain.State |= IceState.AbortInProgress;
                return;
            }

            if (currentScore >= goldScore)
            {
                IceLogging.Error("[TaskGathering | Current Score] We shouldn't be here, stopping and progressing");
                SchedulerMain.State |= IceState.ScoringMission;
                return;
            }

            if (!P.TaskManager.IsBusy)
            {
                int currentIndex = SchedulerMain.CurrentIndex;

                CosmicHelper.OpenStellarMission();
                var currentMission = CosmicHelper.CurrentLunarMission;

                List<uint> MissionNodes = new List<uint>();

                foreach (var entry in SchedulerMain.CurrentNodeSet)
                {
                    if (CosmicHelper.MissionInfoDict[currentMission].NodeSet == entry.NodeSet)
                    {
                        MissionNodes.Add(entry.NodeId);
                    }
                }
                uint nodeId = MissionNodes[currentIndex];

                // Checking to make sure that you're not currently gathering
                if (!Svc.Condition[ConditionFlag.Gathering])
                {

                    Vector3 nodeLoc = GatheringUtil.MoonNodeInfoList.Where(x => x.NodeId == nodeId).FirstOrDefault().LandZone;
                    if (PlayerHelper.GetDistanceToPlayer(nodeLoc) > 1.5f)
                    {
                        // Seen that the distance between you and the node is greater than 2, pathfinding
                        P.TaskManager.Enqueue(() => PathToNode(nodeLoc), "Pathing to node");
                        return;
                    }
                    else
                    {
                        IGameObject? gameObject = null;
                        Utils.TryGetObjectByDataId(nodeId, out gameObject);
                        if (gameObject != null)
                        {
                            // Game object has been found. Now time to check stuff on it.
                            if (gameObject.IsTargetable)
                            {
                                P.TaskManager.Enqueue(() => Utils.TargetgameObject(gameObject), "Targeting gameObject");
                                P.TaskManager.Enqueue(() => InteractGather(gameObject), "Interacting with Object");
                                P.TaskManager.Enqueue(() => GatheringAddonReady(), "Making sure gathering addon is ready");
                                return;
                            }
                            else
                            {
                                P.TaskManager.Enqueue(() => UpdateIndex(MissionNodes), "Increasing the index by 1");
                                return;
                            }
                        }
                    }
                }
                else if (Svc.Condition[ConditionFlag.Gathering])
                {
                    // Probably not necessary to check, but also helps me keep track of what this should be

                    // Check for addon to make sure it's visible (master)
                    // Check to see if buffs need applied
                    //   -> If yes, then run a task to apply said buff (might be best to just make this a generic task to save some sanity...
                    //      ->  Make sure to return post this so it doesn't try and continue to run 
                    //   -> If no, continue on
                    // Make a task to gather an item, make it return true when you enter the gatheringaction state
                    // Wait for you to exit gatherActionState
                    if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var x) && x.IsAddonReady)
                    {
                        uint Boon1 = 0;
                        uint Boon2 = 0;
                        uint Tidings = 0;
                        uint Yield1 = 0;
                        uint Yield2 = 0;
                        uint IntegInc = 0;
                        uint BonusInteg = 0;
                        uint BYieldII = 0;

                        if (PlayerHelper.GetClassJobId() == 17)
                        {
                            Boon1 = GatheringUtil.GathActionDict["BoonIncrease1"].BtnActionId;
                            Boon2 = GatheringUtil.GathActionDict["BoonIncrease2"].BtnActionId;
                            Tidings = GatheringUtil.GathActionDict["Tidings"].BtnActionId;
                            Yield1 = GatheringUtil.GathActionDict["YieldI"].BtnActionId;
                            Yield2 = GatheringUtil.GathActionDict["YieldII"].BtnActionId;
                            IntegInc = GatheringUtil.GathActionDict["IntegrityIncrease"].BtnActionId;
                            BonusInteg = GatheringUtil.GathActionDict["BonusIntegrityChance"].BtnActionId;
                            BYieldII = GatheringUtil.GathActionDict["BountifulYieldII"].BtnActionId;
                        }
                        else if (PlayerHelper.GetClassJobId() == 16)
                        {
                            Boon1 = GatheringUtil.GathActionDict["BoonIncrease1"].MinActionId;
                            Boon2 = GatheringUtil.GathActionDict["BoonIncrease2"].MinActionId;
                            Tidings = GatheringUtil.GathActionDict["Tidings"].MinActionId;
                            Yield1 = GatheringUtil.GathActionDict["YieldI"].MinActionId;
                            Yield2 = GatheringUtil.GathActionDict["YieldII"].MinActionId;
                            IntegInc = GatheringUtil.GathActionDict["IntegrityIncrease"].MinActionId;
                            BonusInteg = GatheringUtil.GathActionDict["BonusIntegrityChance"].MinActionId;
                            BYieldII = GatheringUtil.GathActionDict["BountifulYieldII"].MinActionId;
                        }

                        var mission = CosmicHelper.MissionInfoDict[currentMission];

                        bool Collectable = mission.Attributes.HasFlag(MissionAttributes.Collectables);
                        bool Reducable = mission.Attributes.HasFlag(MissionAttributes.ReducedItems);
                        bool DualClass = mission.Attributes.HasFlag(MissionAttributes.Gather) && mission.Attributes.HasFlag(MissionAttributes.Craft);

                        if (!(Collectable || Reducable) && x.TotalIntegrity != 0)
                        {
                            var DictEntry = CosmicHelper.GatheringItemDict[currentMission].MinGatherItems;
                            var profileId = C.Missions.Where(x => x.Id == currentMission).FirstOrDefault().GatherSettingId;
                            var gBuffs = C.GatherSettings.Where(g => g.Id == profileId).First();

                            bool hasAllItems = true;
                            uint itemToGather = 0;
                            bool gather1More = true;

                            foreach (var item in DictEntry)
                            {
                                if (PlayerHelper.GetItemCount((int)item.Key, out int count) && count < item.Value)
                                {
                                    hasAllItems = false;
                                    itemToGather = item.Key;
                                    if (DualClass)
                                    {
                                        // Checking to see if the amount you need is < Minimum amount have bountiful set to
                                        // Ex. if (need) 1 < 2
                                        if (item.Value - count < gBuffs.Buffs.BountifulMinItem)
                                            gather1More = false;
                                    }
                                    break;
                                }
                            }

                            if (!Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                            {
                                bool missingDur = x.CurrentIntegrity < x.TotalIntegrity;

                                foreach (var item in x.GatheredItems)
                                {
                                    if ((!hasAllItems && item.ItemID == itemToGather) || (hasAllItems && item.ItemID != 0))
                                    {
                                        IceLogging.Debug($"[Condition F] Mission is aiming to gather: {itemToGather}", true);
                                        if (ApplyGatheringBuffs(item, gBuffs, missingDur, Boon1, Boon2, Tidings, Yield1, Yield2, IntegInc, BonusInteg, BYieldII, gather1More))
                                        {
                                            return;
                                        }
                                        else
                                        {
                                            IceLogging.Info($"HasAllItems: {hasAllItems} \n" +
                                                $"Found Item: {item.ItemID} | {item.ItemName}", true);
                                            if (EzThrottler.Throttle($"Gathering: {item.ItemName}"))
                                            {
                                                IceLogging.Info($"Telling it to item: {item.ItemName}");
                                                GatherItem(item);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (x.CurrentIntegrity == 0)
                        {
                            P.TaskManager.Enqueue(() => IntegrityCheck(x));
                        }
                    }
                }
                // Check the score
                P.TaskManager.Enqueue(() => SchedulerMain.State |= IceState.ScoringMission, "Checking score");
            }
        }

        private static bool CanUseGatheringAction(string actionName, GatherBuffProfile gatherBuffs, bool missingDur, int? boonChance = null, bool gather1More = true)
        {
            var actionInfo = GatheringUtil.GathActionDict[actionName];
            uint actionId = PlayerHelper.GetClassJobId() == 16 ? actionInfo.MinActionId : actionInfo.BtnActionId;
            uint used = (uint)SchedulerMain.GathererBuffsUsed.Count(x => x == actionId);
            bool hasStatus = PlayerHelper.HasStatusId(actionInfo.StatusId);
            bool hasGp = PlayerHelper.GetGp() >= actionInfo.RequiredGp;

            return actionName switch
            {
                "BoonIncrease1" => gatherBuffs.Buffs.BoonIncrease1 && boonChance < 100 && !hasStatus && !missingDur && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BoonIncrease1Gp && (gatherBuffs.Buffs.BoonIncrease1MaxUse == -1 || gatherBuffs.Buffs.BoonIncrease1MaxUse > used),
                "BoonIncrease2" => gatherBuffs.Buffs.BoonIncrease2 && boonChance < 100 && !hasStatus && !missingDur && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BoonIncrease2Gp && (gatherBuffs.Buffs.BoonIncrease2MaxUse == -1 || gatherBuffs.Buffs.BoonIncrease2MaxUse > used),
                "Tidings" => gatherBuffs.Buffs.TidingsBool && !hasStatus && !missingDur && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.TidingsGp && (gatherBuffs.Buffs.TidingsMaxUse == -1 || gatherBuffs.Buffs.TidingsMaxUse > used),
                "YieldI" => gatherBuffs.Buffs.YieldI && !hasStatus && !missingDur && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.YieldIGp && (gatherBuffs.Buffs.YieldIMaxUse == -1 || gatherBuffs.Buffs.YieldIMaxUse > used),
                "YieldII" => gatherBuffs.Buffs.YieldII && !hasStatus && !missingDur && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.YieldIIGp && (gatherBuffs.Buffs.YieldIIMaxUse == -1 || gatherBuffs.Buffs.YieldIIMaxUse > used),
                "IntegrityIncrease" => gatherBuffs.Buffs.BonusIntegrity && missingDur && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BonusIntegrityGp && (gatherBuffs.Buffs.BonusIntegrityMaxUse == -1 || gatherBuffs.Buffs.BonusIntegrityMaxUse > used),
                "BonusIntegrityChance" => hasStatus && missingDur,
                "BountifulYieldII" => gatherBuffs.Buffs.BountifulYieldII && !hasStatus && hasGp && PlayerHelper.GetGp() >= gatherBuffs.Buffs.BountifulYieldIIGp && (gatherBuffs.Buffs.BountifulYieldIIMaxUse == -1 || gatherBuffs.Buffs.BountifulYieldIIMaxUse > used) && gather1More == true,
                _ => false,
            };

        }

        private static bool ApplyGatheringBuffs(Gathering.GatheredItem item, GatherBuffProfile gBuffs, bool missingDur, uint Boon1, uint Boon2, uint Tidings, uint Yield1, uint Yield2, uint IntegInc, uint BonusInteg, uint BYieldII, bool gather1More)
        {
            int boonChance = item.BoonChance;

            var buffsToApply = new (uint actionId, System.Func<bool> condition, string debugMessage)[]
            {
                (Boon2, () => CanUseGatheringAction("BoonIncrease2", gBuffs, missingDur, boonChance), "Boon 2"),
                (Boon1, () => CanUseGatheringAction("BoonIncrease1", gBuffs, missingDur, boonChance), "Boon 1"),
                (Tidings, () => CanUseGatheringAction("Tidings", gBuffs, missingDur), "Tidings"),
                (Yield2, () => CanUseGatheringAction("YieldII", gBuffs, missingDur), "Kings Yield 2"),
                (Yield1, () => CanUseGatheringAction("YieldI", gBuffs, missingDur), "Kings Yield 1"),
                (BYieldII, () => CanUseGatheringAction("BountifulYieldII", gBuffs, missingDur, gather1More: gather1More), "Bountiful Yield/Harvest 2"),
                (BonusInteg, () => CanUseGatheringAction("BonusIntegrityChance", gBuffs, missingDur), "Wise of the World"),
                (IntegInc, () => CanUseGatheringAction("IntegrityIncrease", gBuffs, missingDur), "Ageless Words")
            };

            foreach (var (actionId, condition, debugMessage) in buffsToApply)
            {
                if (condition())
                {
                    IceLogging.Debug("[ICE] Applying Buff: " + debugMessage, true);
                    if (EzThrottler.Throttle("Gather Buffs"))
                    {
                        GatherBuffs(actionId);
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see distance to the node. If you're to far away, will pathfind to it.
        /// </summary>
        /// <param name="id"></param>
        internal static bool? PathToNode(Vector3 nodeLoc)
        {
            if (PlayerHelper.GetDistanceToPlayer(nodeLoc) > 1.5f && !P.Navmesh.IsRunning())
            {
                if (EzThrottler.Throttle("Throttling pathfind"))
                {
                    P.Navmesh.PathfindAndMoveTo(nodeLoc, false);
                }
            }
            else if (PlayerHelper.GetDistanceToPlayer(nodeLoc) < 1.5f)
            {
                if (P.Navmesh.IsRunning())
                {
                    P.Navmesh.Stop();
                }

                return true;
            }

            return false;
        }

        internal unsafe static bool? InteractGather(IGameObject? gameObject)
        {
            if (Svc.Condition[ConditionFlag.Gathering])
            {
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Trying to interact w/ node"))
                {
                    try
                    {
                        var gameObjectPointer = (GameObject*)gameObject.Address;
                        TargetSystem.Instance()->InteractWithObject(gameObjectPointer, false);
                    }
                    catch (Exception ex)
                    {
                        Svc.Log.Info($"InteractWithObject: Exception: {ex}");
                    }
                }
            }

            return false;
        }

        internal unsafe static bool? GatheringAddonReady()
        {
            if (GenericHelpers.TryGetAddonMaster<Gathering>("Gathering", out var m) && m.IsAddonReady && !Svc.Condition[ConditionFlag.ExecutingGatheringAction])
            {
                return true;
            }

            return false;
        }

        private unsafe static void GatherBuffs(uint actionId)
        {
            bool? UseBuffs()
            {
                if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                    return true;

                if (EzThrottler.Throttle($"Trying to activate buff: {actionId}"))
                {
                    ActionManager.Instance()->UseAction(ActionType.Action, actionId);
                }

                return false;
            }

            P.TaskManager.Enqueue(UseBuffs, "Applying buffs to character");
            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction], "Waiting for gather buffs");
            P.TaskManager.Enqueue(() => SchedulerMain.GathererBuffsUsed.Add(actionId));
        }

        private unsafe static void GatherItem(Gathering.GatheredItem item)
        {
            bool? HasAttemptedGathering()
            {
                if (Svc.Condition[ConditionFlag.ExecutingGatheringAction])
                    return true;

                if (EzThrottler.Throttle($"Attempting to item item: {item.ItemName}"))
                {
                    item.Gather();
                }

                return false;
            }

            P.TaskManager.Enqueue(() => HasAttemptedGathering(), "Gathering Item Task");
            P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.ExecutingGatheringAction], "Waiting for gathering attempt");
        }


        internal unsafe static bool? UpdateIndex(List<uint> MissionNodes)
        {
            SchedulerMain.NodesVisited++;
            if (SchedulerMain.CurrentIndex < MissionNodes.Count - 1)
                SchedulerMain.CurrentIndex++;
            else
                SchedulerMain.CurrentIndex = 0;

            if (CosmicHelper.CurrentMissionInfo.Attributes.HasFlag(MissionAttributes.Limited) && SchedulerMain.NodesVisited >= SchedulerMain.CurrentNodeSet.Count)
                SchedulerMain.State |= IceState.AbortInProgress;
            SchedulerMain.State |= IceState.ScoringMission;
            return true;
        }

        internal static bool? IntegrityCheck(Gathering x)
        {
            if (Svc.Condition[ConditionFlag.Gathering])
            {
                if (x.IsAddonReady && x.CurrentIntegrity != 0)
                {
                    return true;
                }
            }
            else if (PlayerHelper.IsPlayerNotBusy())
            {
                return true;
            }

            return false;
        }
    }
}
