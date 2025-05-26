using ECommons.EzSharedDataManager;
using System.Collections.Generic;

namespace ICE.Scheduler.Handlers
{
    internal static class YesAlreadyManager
    {
        private static bool WasChanged = false;
        internal static void Tick()
        {
            if (WasChanged)
            {
                if (!SchedulerMain.State.HasFlag(IceState.GrabMission))
                {
                    WasChanged = false;
                    Unlock();
                    IceLogging.Debug($"YesAlready unlocked");
                }
            }
            else
            {
                if (SchedulerMain.State.HasFlag(IceState.GrabMission))
                {
                    WasChanged = true;
                    Lock();
                    IceLogging.Debug($"YesAlready locked");
                }
            }
        }
        internal static void Lock()
        {
            if (EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                data.Add(Name);
            }
        }

        internal static void Unlock()
        {
            if (EzSharedData.TryGet<HashSet<string>>("YesAlready.StopRequests", out var data))
            {
                data.Remove(Name);
            }
        }
    }
}
