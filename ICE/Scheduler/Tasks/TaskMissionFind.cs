using ECommons.Automation;
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
            P.taskManager.Enqueue(() => AbandonMission(), "Checking to see if need to leave mission");

        }

        internal static bool? UpdateValues()
        {
            SchedulerMain.Abandon = false;
            SchedulerMain.MissionName = string.Empty;
            SchedulerMain.MissionId = 0;

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
            if (SchedulerMain.MissionId != 0)
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
                    var weatherMissionEntry = C.EnabledMission.FirstOrDefault(e => e.Id == m.MissionId);

                    if (weatherMissionEntry == default)
                        continue;

                    if (EzThrottler.Throttle("Selecting Weather Mission"))
                    {
                        PluginLog.Debug($"Mission Name: {m.Name} | SchedulerMain.MissionId: {weatherMissionEntry.Id} has been found. Setting value for sending");
                        m.Select();
                        SchedulerMain.MissionName = m.Name;
                        SchedulerMain.MissionId = weatherMissionEntry.Id;
                        return true;
                    }
                }
            }

            if (SchedulerMain.MissionId == 0)
            {
                PluginLog.Debug("No mission was found under weather, continuing on");
                return true;
            }

            return false;
        }

        internal unsafe static bool? FindBasicMission()
        {
            PluginLog.Debug($"[Basic Mission Start] | Mission Name: {SchedulerMain.MissionName} | SchedulerMain.MissionId: {SchedulerMain.MissionId}");
            if (SchedulerMain.MissionId != 0)
            {
                PluginLog.Debug("You already have a mission found, skipping finding a basic mission");
                return true;
            }


            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                foreach (var m in x.StellerMissions)
                {
                    var basicMissionEntry = C.EnabledMission.FirstOrDefault(e => e.Id == m.MissionId);

                    if (basicMissionEntry == default)
                        continue;

                    if (EzThrottler.Throttle("Selecting Basic Mission"))
                    {
                        PluginLog.Debug($"Mission Name: {basicMissionEntry.Name} | SchedulerMain.MissionId: {basicMissionEntry.Id} has been found. Setting values for sending");
                        SchedulerMain.MissionName = basicMissionEntry.Name;
                        SchedulerMain.MissionId = basicMissionEntry.Id;
                        m.Select();
                        return true;
                    }
                }
            }

            if (SchedulerMain.MissionId == 0)
            {
                PluginLog.Debug("No mission was found under basic missions, continuing on");
                return true;
            }

            return false;
        }

        internal unsafe static bool? FindResetMission()
        {
            PluginLog.Debug($"[Reset Mission Finder] Mission Name: {SchedulerMain.MissionName} | SchedulerMain.MissionId {SchedulerMain.MissionId}");
            if (SchedulerMain.MissionId != 0)
            {
                PluginLog.Debug("You already have a mission found, skipping finding a basic mission");
                return true;
            }

            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                PluginLog.Debug("found mission was false");
                var entry = C.EnabledMission.FirstOrDefault();
                var rank = MissionInfoDict[entry.Id].Rank;

                foreach (var m in x.StellerMissions)
                {
                    var missionEntry = MissionInfoDict.FirstOrDefault(e => e.Key == m.MissionId);

                    if (missionEntry.Value == null)
                        continue;

                    PluginLog.Debug($"Mission: {m.Name} | Mission rank: {missionEntry.Value.Rank} | Rank: {rank}");
                    if (missionEntry.Value.Rank == rank)
                    {
                        if (EzThrottler.Throttle("Selecting Abandon Mission"))
                        {
                            PluginLog.Debug($"Setting SchedulerMain.MissionName = {m.Name}");
                            m.Select();
                            SchedulerMain.MissionName = m.Name;
                            SchedulerMain.MissionId = missionEntry.Key;
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
            PluginLog.Debug($"[Grabbing Mission] Mission Name: {SchedulerMain.MissionName} | SchedulerMain.MissionId {SchedulerMain.MissionId}");
            if (TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
            {
                if (EzThrottler.Throttle("Selecting Yes", 250))
                {
                    select.Yes();
                }
            }
            else if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                if (!MissionInfoDict.ContainsKey(SchedulerMain.MissionId))
                {
                    PluginLog.Debug($"No values were found for mission id {SchedulerMain.MissionId}... which is odd. Stopping the process");
                    P.taskManager.Abort();
                }

                if (EzThrottler.Throttle("Firing off to initiate quest", C.UIActionDelay))
                {
                    Callback.Fire(x.Base, true, 13, SchedulerMain.MissionId);
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
                    if (EzThrottler.Throttle("Confirming Abandon", C.UIActionDelay))
                    {
                        select.Yes();
                        return true;
                    }
                }
                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var addon) && addon.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Abandoning the mission", C.UIActionDelay))
                        addon.Abandon();
                }
                else if (TryGetAddonMaster<WKSHud>("WKSHud", out var SpaceHud) && SpaceHud.IsAddonReady)
                {
                    if (EzThrottler.Throttle("Opening the mission hud", C.UIActionDelay))
                        SpaceHud.Mission();
                }
            }

            return false;
        }
    }
}
