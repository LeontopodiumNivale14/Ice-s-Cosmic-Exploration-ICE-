using ICE.Scheduler.Tasks;

namespace ICE.Scheduler
{
    internal static unsafe class SchedulerMain
    {
        internal static bool EnablePlugin()
        {
            State = IceState.ResumeChecker;
            return true;
        }
        internal static bool DisablePlugin()
        {
            P.TaskManager.Abort();
            StopBeforeGrab = false;
            State = IceState.Idle;
            return true;
        }

        internal static string MissionName = string.Empty;
        internal static bool inMission = false;
        internal static bool Abandon = false;
        internal static bool StopBeforeGrab = false;
        internal static bool StopOnceHitCredits = false;


        internal static IceState State = IceState.Idle;

        internal static void Tick()
        {
            if (GenericThrottle && P.TaskManager.Tasks.Count == 0)
            {
                switch (State)
                {
                    case IceState.Idle:
                    case IceState.WaitForCrafts:
                    case IceState.CraftInProcess:
                    case IceState.GrabbingMission:
                        break;
                    case IceState.GrabMission:
                        TaskMissionFind.Enqueue();
                        break;
                    case IceState.StartCraft:
                        TaskCrafting.TryEnqueueCrafts();
                        break;
                    case IceState.CheckScoreAndTurnIn:
                        TaskScoreCheck.TryCheckScore();
                        break;
                    case IceState.ManualMode:
                        TaskManualMode.ZenMode();
                        break;
                    case IceState.ResumeChecker:
                        TaskMissionFind.EnqueueResumeCheck();
                        break;
                    default:
                        throw new Exception("Invalid state");
                }
            }
        }
    }

    internal enum IceState
    {
        Idle,
        GrabMission,
        GrabbingMission,
        StartCraft,
        CraftInProcess,
        CheckScoreAndTurnIn,
        WaitForCrafts,
        ManualMode,
        ResumeChecker
    }
}
