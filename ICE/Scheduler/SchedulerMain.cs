using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using static ICE.Enums.IceState;

namespace ICE.Scheduler
{
    internal static unsafe class SchedulerMain
    {
        internal static bool EnablePlugin()
        {
            State = Start;
            StartClassJob = (Job)PlayerHelper.GetClassJobId();
            return true;
        }
        internal static bool DisablePlugin()
        {
            P.TaskManager.Abort();
            StopBeforeGrab = false;
            State = Idle;
            StartClassJob = Job.ADV;
            if (P.Navmesh.IsRunning())
                P.Navmesh.Stop();
            return true;
        }

        internal static string MissionName = string.Empty;
        internal static bool inMission = false;
        internal static bool Abandon = false;
        internal static bool AnimationLockAbandonState = false;
        internal static uint PossiblyStuck = 0;
        internal static bool StopBeforeGrab = false;
        internal static uint PreviousNodeSetId = 0;
        internal static List<GatheringUtil.GathNodeInfo> CurrentNodeSet = [];
        internal static int CurrentIndex = 0;
        internal static uint NodesVisited = 0;
        internal static bool GatherNodeMissing = false;
        internal static List<uint> GathererBuffsUsed = [];
        internal static int InitialGatheringItemMultiplier = 1;
        internal static Vector3? NearestCollectionPoint = null;
#if DEBUG
        // Debug only settings
        internal static bool DebugOOMMain = false;
        internal static bool DebugOOMSub = false;
#endif

        internal static IceState State = Idle;
        internal static Job StartClassJob = Job.ADV;

        // <summary>
        // Main Scheduler. General flow is to raise flags as necessary and resolve them based on priority:
        // Idle - do nothing.
        // On start, check what state we are in and set flags as needed.
        // If Craft && Waiting - Wait for craft loop to exit. Raise ScoringMission + lower Waiting on exit.
        // If ScoringMission flag is set, run score check, reset state to Idle or Grab if turned in, otherwise unset ScoringMission flag (Returning us to Cradt/Gather/Fish)
        // If AnimationLock flag is set, attempt unstuck, unset flag after.
        // If Gamba flag is set, run gamba, reset to Idle.
        // If GrabMission && Waiting - wait for non-standard mission conditions to be true before resuming.
        // If GrabMission flag is set, get a mission. Once obtained raise Craft/Gather/Fish flags and ExecutingMission flag. Otherwise if no standards - raise Waiting. If no missions at all - set state to Idle.
        // If Manual is set on a mission - Zen. (Also Fish, for now.)
        // If Gather && ExecutingMission flag is set, run gathering. If DualClass - lower Gather flag on enough mats. Raise ScoringMission flag on completion of a loop.
        // If Craft && ExecutingMission flag is set, run crafting. If DualClass - raise Gather flag on if not enough mats. Raise ScoringMission flag on completion of a loop.
        // </summary>
        internal static void Tick()
        {
            if (Throttles.GenericThrottle && P.TaskManager.Tasks.Count == 0 && State != Idle)
            {
                switch (State)
                {
                    case var s when s.HasFlag(Start):
                        EnqueueResumeCheck();
                        break;
                    case var s when s.HasFlag(Craft) && s.HasFlag(Waiting):
                        TaskCrafting.WaitTillActuallyDone();
                        break;
                    case var s when s.HasFlag(ScoringMission) || s.HasFlag(AbortInProgress):
                        TaskScoreCheckCraft.TryCheckScore();
                        break;
                    case var s when s.HasFlag(AnimationLock):
                        TaskAnimationLock.Enqueue();
                        break;
                    case var s when s.HasFlag(Gambling):
                        TaskGamba.TryHandleGamba();
                        break;
                    case var s when s.HasFlag(GrabMission) && s.HasFlag(Waiting):
                        TaskMissionFind.WaitForNonStandard();
                        break;
                    case var s when s.HasFlag(GrabMission):
                        TaskMissionFind.Enqueue();
                        break;
                    case var s when s.HasFlag(ManualMode):
                        TaskManualMode.ZenMode();
                        break;
                    case var s when s.HasFlag(Fish) && s.HasFlag(ExecutingMission):
                        TaskFish.TryEnqueueFishing();
                        break;
                    case var s when s.HasFlag(Gather) && s.HasFlag(ExecutingMission):
                        TaskGather.TryEnqueueGathering();
                        break;
                    case var s when s.HasFlag(Craft) && s.HasFlag(ExecutingMission):
                        TaskCrafting.TryEnqueueCrafts();
                        break;
                    default:
                        if (C.StopOnAbort)
                            throw new Exception("Invalid state");
                        else
                            EnqueueResumeCheck();
                        break;
                }
            }
            /* switch (State)
            {
                case IceState.Idle:
                    break;
                case IceState.Gamba:
                    TaskGamba.TryHandleGamba();
                    break;
                case IceState.AnimationLock:
                    TaskAnimationLock.Enqueue();
                    break;
                case IceState.RepairMode:
                    TaskRepair.GatherCheck();
                    break;
                case IceState.GrabbingMission:
                    break;
                case IceState.WaitForNonStandard:
                    TaskMissionFind.WaitForNonStandard();
                    break;
                case IceState.GrabMission:
                    TaskMissionFind.Enqueue();
                    break;
                case IceState.StartCraft:
                    TaskCrafting.TryEnqueueCrafts();
                    break;
                case IceState.AbortInProgress:
                case IceState.WaitForCrafts:
                case IceState.CraftInProcess:
                case IceState.CraftCheckScoreAndTurnIn:
                    TaskScoreCheckCraft.TryCheckScore();
                    break;
                case IceState.GatherScoreandTurnIn:
                    TaskScoreCheckGather.TryCheckScore();
                    break;
                case IceState.ManualMode:
                    TaskManualMode.ZenMode();
                    break;
                case IceState.ResumeChecker:
                    TaskMissionFind.EnqueueResumeCheck();
                    break;
                case IceState.GatherNormal:
                    TaskGather.TryEnqueueGathering();
                    break;
                default:
                    throw new Exception("Invalid state");
            } */
        }
        public static void EnqueueResumeCheck()
        {
            State = Idle;
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                if (!AddonHelper.IsAddonActive("WKSMissionInfomation"))
                {
                    CosmicHelper.OpenStellarMission();
                    State = Start;
                    return;
                }
                if (MissionHandler.IsMissionTimedOut())
                    State |= AbortInProgress;
                TaskMissionFind.UpdateStateFlags();
                if (State.HasFlag(Craft) && P.Artisan.IsBusy())
                    State |= Waiting;
                State |= ScoringMission;
            }
            else if (AddonHelper.IsAddonActive("WKSLottery"))
                State = Gambling;
            else
                State = GrabMission;
            if (AnimationLockAbandonState || (!(AddonHelper.IsAddonActive("WKSRecipeNotebook") || AddonHelper.IsAddonActive("RecipeNote")) && Svc.Condition[ConditionFlag.Crafting] && Svc.Condition[ConditionFlag.PreparingToCraft]))
                State |= AnimationLock;
        }
    }
}