using Dalamud.Game.ClientState.Conditions;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskAnimationLock
    {
        public static void Enqueue()
        {   
            if (Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.ExecutingCraftingAction] || AddonHelper.IsAddonActive("RecipeNote") || AddonHelper.IsAddonActive("WKSRecipeNotebook"))
            {
                IceLogging.Info("We were in Animation Lock fix state and seem to be fixed. Reseting.");
                SchedulerMain.State = IceState.GrabMission;
                SchedulerMain.PossiblyStuck = 0;
                SchedulerMain.AnimationLockAbandonState = false;
            }
            else
            {
                if (EzThrottler.Throttle("Open Recipe", 1000))
                    AddonHelper.OpenRecipeNote();
            }
        }
    }
}