using Dalamud.Game.ClientState.Conditions;
using ECommons.GameHelpers;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskFish
    {
        /* Ah shit, here we go again. */

        public static void TryEnqueueFishing()
        {
            if (CosmicHelper.CurrentLunarMission != 0)
            {
                Job targetClass;
                if (((Job)CosmicHelper.CurrentMissionInfo.JobId) == Job.FSH)
                    targetClass = Job.FSH;
                else if (((Job)CosmicHelper.CurrentMissionInfo.JobId2) == Job.FSH)
                    targetClass = Job.FSH;
                else
                    return;
                if ((Job)PlayerHelper.GetClassJobId() != targetClass)
                    GearsetHandler.TaskClassChange(targetClass);
                else
                    MakeFishingTask();
            }
        }

        internal static void MakeFishingTask()
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
                IceLogging.Error("We shouldn't be here, stopping and progressing");
                SchedulerMain.State |= IceState.ScoringMission;
                return;
            }

            if (!P.TaskManager.IsBusy)
            {
                int currentIndex = SchedulerMain.CurrentIndex;

                CosmicHelper.OpenStellarMission();
                var currentMission = CosmicHelper.CurrentLunarMission;

                //List<Vector4> FishNodes = []; // Grab from config?
                //Vector4 node = FishNodes.OrderBy(x => PlayerHelper.GetDistanceToPlayer(x.AsVector3())).First();
                Vector4 node = C.FishingSpots[CosmicHelper.CurrentMissionInfo.MarkerId];

                if (!Svc.Condition[ConditionFlag.Gathering])
                    {

                        Vector3 nodeLoc = node.AsVector3();
                        float nodeRot = node.W;
                        if (PlayerHelper.GetDistanceToPlayer(nodeLoc) > 1.5f)
                        {
                            // Seen that the distance between you and the node is greater than 2, pathfinding
                            P.TaskManager.Enqueue(() => PathToNode(nodeLoc), "Pathing to node");
                            return;
                        }
                        else
                        {
                            P.TaskManager.Enqueue(() => StartFishing(nodeRot), "Fish");
                            return;
                        }
                    }
                P.TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Fishing]); // Wait till we start hooking
                P.TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Fishing]); // We are no longer hooking
                P.TaskManager.Enqueue(() => SchedulerMain.State |= IceState.ScoringMission, "Checking score");
            }
        }

        private static bool? StartFishing(float nodeRot)
        {
            if (!Svc.Condition[ConditionFlag.Gathering])
            {
                if (Player.Rotation != nodeRot)
                    PlayerHelper.SetRotation(nodeRot);

                if (PlayerHelper.IsCastAvailable())
                    P.AutoHook.StartFishing();

                return false;
            }
            else
            {
                return true;
            }
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
    }
}
