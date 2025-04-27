using ECommons.Automation;
using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Logging;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskMissionFind
    {
        private static uint MissionId = 0;
        private static uint MissionScore = 0;

        public static void Enqueue()
        {
            P.taskManager.Enqueue(() => UpdateValues());
            P.taskManager.Enqueue(() => OpenMissionFinder(), "Opening the Mission finder");
            P.taskManager.Enqueue(() => WeatherButton(), "Selecting Weather");
            P.taskManager.EnqueueDelay(100);
            P.taskManager.Enqueue(() => FindWeatherMission(), "Checking to see if weather mission avaialable");
            P.taskManager.Enqueue(() => BasicMissionButton(), "Selecting Basic Missions");
            P.taskManager.EnqueueDelay(100);
            P.taskManager.Enqueue(() => FindBasicMission(), "Finding Basic Mission");
            P.taskManager.Enqueue(() => FindResetMission(), "Checking for abandon mission");
            P.taskManager.Enqueue(() => GrabMission(), "Grabbing the mission");
            P.taskManager.Enqueue(() => AbandonMission(), "Checking to see if need to leave mission"); //
        }

        private static uint ScoreMission(WKSMission.StellarMissions mission)
        {
            if (MissionInfoDict[mission.MissionId].JobId != GetClassJobId())
            {
                return 0;
            }
            uint score = 0;
            int type = 1;
            foreach (var target in C.TargetResearch)
            {
                if (target)
                {
                    if (MissionInfoDict[mission.MissionId].ExperienceRewards.Any(reward => reward.Type==type)) {
                        score += 1;
                    }
                }

    
                type++;
            }

            if (C.EnabledMission.Any(e => e.Id == mission.MissionId))
            {
                score += 5;
            }
            return score;
        }

        internal static bool? UpdateValues()
        {
            SchedulerMain.Abandon = false;
            SchedulerMain.MissionName = string.Empty;
            MissionId = 0;
            MissionScore = 0;

            return true;
        }

        internal unsafe static bool? WeatherButton()
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                x.ProvisionalMissions();
                return true;
            }
            return false;
        }

        internal unsafe static bool? BasicMissionButton()
        {
            if (MissionId != 0)
            {
                return true;
            }

            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                x.BasicMissions();
                return true;
            }
            return false;
        }

        internal unsafe static bool? OpenMissionFinder()
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var mission) && mission.IsAddonReady)
            {
                return true;
            }

            if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
            {
                if (EzThrottler.Throttle("Opening Mission Hud", 1000))
                {
                    hud.Mission();
                }
            }

            return false;
        }

        internal unsafe static bool? FindWeatherMission()
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                x.ProvisionalMissions();
                foreach (var m in x.StellerMissions)
                {
                    var score = ScoreMission(m);
                    if (score <= MissionScore)
                        continue;

                    PluginLog.Debug($"Mission Name: {m.Name} | SchedulerMain.MissionId: {m.MissionId} has been found. Setting value for sending");
                    SchedulerMain.MissionName = m.Name;
                    MissionId = m.MissionId;
                    MissionScore = score;
                    if (EzThrottler.Throttle("Selecting Weather Mission"))
                    {
                        m.Select();
                    }
                }
                return true;
            }

            if (MissionId == 0)
            {
                PluginLog.Debug("No mission was found under weather, continuing on");
                return true;
            }

            return false;
        }

        internal unsafe static bool? FindBasicMission()
        {
            PluginLog.Debug($"[Basic Mission Start] | Mission Name: {SchedulerMain.MissionName} | MissionId: {MissionId}");
            if (MissionId != 0)
            {
                PluginLog.Debug("You already have a mission found, skipping finding a basic mission");
                return true;
            }


            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                foreach (var m in x.StellerMissions)
                {
                    var score = ScoreMission(m);
                    if (score <= MissionScore)
                        continue;

                    PluginLog.Debug($"Mission Name: {m.Name} | SchedulerMain.MissionId: {m.MissionId} has been found. Setting value for sending");
                    SchedulerMain.MissionName = m.Name;
                    MissionId = m.MissionId;
                    MissionScore = score;
                    if (EzThrottler.Throttle("Selecting Basic Mission"))
                    {
                        m.Select();
                    }
                }
                return true;
            }

            if (MissionId == 0)
            {
                PluginLog.Debug("No mission was found under basic missions, continuing on");
                return true;
            }

            return false;
        }

        internal unsafe static bool? FindResetMission()
        {
            PluginLog.Debug($"[Reset Mission Finder] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}");
            if (MissionId != 0)
            {
                PluginLog.Debug("You already have a mission found, skipping finding a basic mission");
                return true;
            }

            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                PluginLog.Debug("found mission was false");
                var currentClassJob = GetClassJobId();
                var ranks = C.EnabledMission
                    .Where(e => MissionInfoDict[e.Id].JobId == currentClassJob)
                    .Select(e => MissionInfoDict[e.Id].Rank)
                    .ToList();
                if (ranks.Count == 0 && !C.TargetResearch.Any(e => e))
                {
                    PluginLog.Debug("No missions selected in UI, would abandon every mission");
                    SchedulerMain.DisablePlugin();
                    return false;
                }

                var rankToReset = ranks.Max();

                foreach (var m in x.StellerMissions)
                {
                    var missionEntry = MissionInfoDict.FirstOrDefault(e => e.Key == m.MissionId);

                    if (missionEntry.Value == null || missionEntry.Value.JobId != currentClassJob)
                        continue;

                    PluginLog.Debug($"Mission: {m.Name} | Mission rank: {missionEntry.Value.Rank} | Rank to reset: {rankToReset}");
                    if (missionEntry.Value.Rank == rankToReset || (missionEntry.Value.Rank >= 4 && rankToReset >= 4))
                    {
                        if (EzThrottler.Throttle("Selecting Abandon Mission"))
                        {
                            PluginLog.Debug($"Setting SchedulerMain.MissionName = {m.Name}");
                            m.Select();
                            SchedulerMain.MissionName = m.Name;
                            MissionId = missionEntry.Key;
                            SchedulerMain.Abandon = true;

                            PluginLog.Debug($"Mission Name: {SchedulerMain.MissionName}");

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal unsafe static bool? GrabMission()
        {
            PluginLog.Debug($"[Grabbing Mission] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId} | SchedulerMain.MissionScore {MissionScore}");
            if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
            {
                if (EzThrottler.Throttle("Selecting Yes", 250))
                {
                    select.Yes();
                }
            }
            else if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                if (!MissionInfoDict.ContainsKey(MissionId))
                {
                    PluginLog.Debug($"No values were found for mission id {MissionId}... which is odd. Stopping the process");
                    P.taskManager.Abort();
                }

                if (EzThrottler.Throttle("Firing off to initiate quest"))
                {
                    Callback.Fire(x.Base, true, 13, MissionId);
                }
            }
            else if (!IsAddonActive("WKSMission"))
            {
                return true;
            }

            return false;
        }

        internal unsafe static bool? AbandonMission()
        {
            if (SchedulerMain.Abandon == false)
            {
                return true;
            }
            else
            {
                if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Confirming Abandon"))
                    {
                        select.Yes();
                        return true;
                    }
                }
                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var addon) && addon.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Abandoning the mission"))
                        addon.Abandon();
                }
                else if (TryGetAddonMaster<WKSHud>("WKSHud", out var SpaceHud) && SpaceHud.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Opening the mission hud"))
                        SpaceHud.Mission();
                }
            }

            return false;
        }
    }
}
