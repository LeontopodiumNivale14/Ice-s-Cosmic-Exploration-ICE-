using ECommons.Automation;
using ECommons.Logging;
using ECommons.Throttlers;
using ICE.Ui;
using System;
using System.Collections;
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
        private static uint currentClassJob => GetClassJobId();
        private static bool isGatherer => currentClassJob >= 16 && currentClassJob <= 18;
        private static bool hasCritical => C.EnabledMission
                                            .Where(e => MissionInfoDict[e.Id].JobId == currentClassJob)
                                            .Where(e => !UnsupportedMissions.Ids.Contains(e.Id))
                                            .Select(enabled => enabled.Id)
                                            .Intersect(C.CriticalMissions.Select(critical => critical.Id))
                                            .Any();
        private static bool hasWeather => C.EnabledMission
                                            .Where(e => MissionInfoDict[e.Id].JobId == currentClassJob)
                                            .Where(e => !UnsupportedMissions.Ids.Contains(e.Id))
                                            .Select(enabled => enabled.Id)
                                            .Intersect(C.WeatherMissions.Select(weather => weather.Id))
                                            .Any();
        private static bool hasTimed => C.EnabledMission
                                            .Where(e => MissionInfoDict[e.Id].JobId == currentClassJob)
                                            .Where(e => !UnsupportedMissions.Ids.Contains(e.Id))
                                            .Select(enabled => enabled.Id)
                                            .Intersect(C.TimedMissions.Select(timed => timed.Id))
                                            .Any();
        private static bool hasSequence => C.EnabledMission
                                            .Where(e => MissionInfoDict[e.Id].JobId == currentClassJob)
                                            .Where(e => !UnsupportedMissions.Ids.Contains(e.Id))
                                            .Select(enabled => enabled.Id)
                                            .Intersect(C.SequenceMissions.Select(sequence => sequence.Id))
                                            .Any();
        private static bool hasStandard => C.EnabledMission
                                            .Where(e => MissionInfoDict[e.Id].JobId == currentClassJob)
                                            .Where(e => !UnsupportedMissions.Ids.Contains(e.Id))
                                            .Select(enabled => enabled.Id)
                                            .Intersect(C.StandardMissions.Select(standard => standard.Id))
                                            .Any();

        public static void EnqueueResumeCheck()
        {
            if (CurrentLunarMission != 0)
            {
                if (!ModeChangeCheck(isGatherer))
                {
                    SchedulerMain.State = IceState.CheckScoreAndTurnIn;
                }
            }
            else
            {
                SchedulerMain.State = IceState.GrabMission;
            }
        }

        public static void Enqueue()
        {
            if (SchedulerMain.StopBeforeGrab)
            {
                SchedulerMain.StopBeforeGrab = false;
                SchedulerMain.State = IceState.Idle;
                return;
            }

            if (SchedulerMain.State != IceState.GrabMission)
                return;
            
            SchedulerMain.State = IceState.GrabbingMission;

            P.TaskManager.Enqueue(() => UpdateValues(), "Updating Task Mission Values");
            P.TaskManager.Enqueue(() => OpenMissionFinder(), "Opening the Mission finder");
            // if (hasCritical) {
            P.TaskManager.Enqueue(() => CriticalButton(), "Selecting Critical Mission");
            P.TaskManager.EnqueueDelay(200);
            P.TaskManager.Enqueue(() => FindCriticalMission(), "Checking to see if critical mission available");
            // }
            //if (hasWeather || hasTimed || hasSequence) // Skip Checks if enabled mission doesn't have weather, timed or sequence?
            //{
            P.TaskManager.Enqueue(() => WeatherButton(), "Selecting Weather");
            P.TaskManager.EnqueueDelay(200);
            P.TaskManager.Enqueue(() => FindWeatherMission(), "Checking to see if weather mission avaialable");
            //}
            P.TaskManager.Enqueue(() => BasicMissionButton(), "Selecting Basic Missions");
            P.TaskManager.EnqueueDelay(200);
            P.TaskManager.Enqueue(() => FindBasicMission(), "Finding Basic Mission");
            P.TaskManager.Enqueue(() => FindResetMission(), "Checking for abandon mission");
            P.TaskManager.Enqueue(() => GrabMission(), "Grabbing the mission");
            P.TaskManager.EnqueueDelay(250);
            P.TaskManager.Enqueue(() => AbandonMission(), "Checking to see if need to leave mission");
            P.TaskManager.Enqueue(() =>
            {
                if (SchedulerMain.Abandon)
                {
                    P.TaskManager.Enqueue(() => CurrentLunarMission == 0);
                    P.TaskManager.EnqueueDelay(250);
                    SchedulerMain.Abandon = false;
                    SchedulerMain.State = IceState.GrabMission;
                }
            }, "Checking if you are abandoning mission");
            P.TaskManager.Enqueue(async () =>
            {
                if (CurrentLunarMission != 0)
                {
                    if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation"))
                    {
                        var gatherer = isGatherer;
                        if (EzThrottler.Throttle("Opening Steller Missions"))
                        {
                            PluginLog.Debug("Opening Mission Menu");
                            hud.Mission();

                            while (!IsAddonActive("WKSMissionInfomation"))
                            {
                                PluginLog.Debug("Waiting for WKSMissionInfomation to be active");
                                await Task.Delay(500);
                            }
                            if (!ModeChangeCheck(gatherer)) {
                                SchedulerMain.State = IceState.StartCraft;
                            }
                        }
                    }
                }
            });
        }

        internal static bool? UpdateValues()
        {
            SchedulerMain.Abandon = false;
            SchedulerMain.MissionName = string.Empty;
            MissionId = 0;

            return true;
        }

        internal unsafe static bool? CriticalButton()
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                x.CriticalMissions();
                return true;
            }
            return false;
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

        internal unsafe static void FindCriticalMission()
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                x.ProvisionalMissions();
                var currentClassJob = GetClassJobId();
                foreach (var m in x.StellerMissions)
                {
                    var criticalMissionEntry = C.EnabledMission.FirstOrDefault(e => e.Id == m.MissionId && MissionInfoDict[e.Id].JobId == currentClassJob);

                    if (criticalMissionEntry == default)
                    {
                        PluginDebug($"critical mission entry is default. Which means id: {criticalMissionEntry}");
                        continue;
                    }

                    if (EzThrottler.Throttle("Selecting Critical Mission"))
                    {
                        PluginLog.Debug($"Mission Name: {m.Name} | MissionId: {criticalMissionEntry.Id} has been found. Setting value for sending");
                        SelectMission(m);
                    }
                }
            }

            if (MissionId == 0)
            {
                PluginLog.Debug("No mission was found under weather, continuing on");
            }
        }

        internal unsafe static void FindWeatherMission()
        {
            if (MissionId != 0)
            {
                PluginLog.Debug("You already have a mission found, skipping finding weather mission");
                return;
            }
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                x.ProvisionalMissions();
                var currentClassJob = GetClassJobId();

                var weatherIds = C.WeatherMissions.Select(w => w.Id).ToHashSet();
                var sequenceIds = C.SequenceMissions.Select(s => s.Id).ToHashSet();
                var timedIds = C.TimedMissions.Select(t => t.Id).ToHashSet();

                var sortedMissions = x.StellerMissions
                    .Where(m => weatherIds.Contains(m.MissionId))
                    .Concat(
                        x.StellerMissions.Where(m =>
                            !weatherIds.Contains(m.MissionId) && !sequenceIds.Contains(m.MissionId))
                    )
                    .Concat(
                        x.StellerMissions.Where(m =>
                            !weatherIds.Contains(m.MissionId) && !timedIds.Contains(m.MissionId))
                    )
                    .ToArray();

                foreach (var m in sortedMissions)
                {
                    var weatherMissionEntry = C.EnabledMission.FirstOrDefault(e => e.Id == m.MissionId && MissionInfoDict[e.Id].JobId == currentClassJob);

                    if (weatherMissionEntry == default)
                    {
                        PluginDebug($"weather mission entry is default. Which means id: {weatherMissionEntry}");
                        continue;
                    }

                    if (EzThrottler.Throttle("Selecting Weather Mission"))
                    {
                        PluginLog.Debug($"Mission Name: {m.Name} | MissionId: {weatherMissionEntry.Id} has been found. Setting value for sending");
                        SelectMission(m);
                    }
                }
            }

            if (MissionId == 0)
            {
                PluginLog.Debug("No mission was found under weather, continuing on");
            }
        }

        private static void SelectMission(WKSMission.StellarMissions m)
        {
            m.Select();
            SchedulerMain.MissionName = m.Name;
            MissionId = m.MissionId;
        }

        internal unsafe static void FindBasicMission()
        {
            PluginLog.Debug($"[Basic Mission Start] | Mission Name: {SchedulerMain.MissionName} | MissionId: {MissionId}");
            if (MissionId != 0)
            {
                PluginLog.Debug("You already have a mission found, skipping finding a basic mission");
                return;
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
                        PluginLog.Debug($"Mission Name: {basicMissionEntry.Name} | MissionId: {basicMissionEntry.Id} has been found. Setting values for sending");
                        SelectMission(m);
                    }
                }
            }

            if (MissionId == 0)
            {
                PluginLog.Debug("No mission was found under basic missions, continuing on");
            }
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
                if (ranks.Count == 0)
                {
                    PluginLog.Debug("No missions selected in UI, would abandon every mission");
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
            PluginLog.Debug($"[Grabbing Mission] Mission Name: {SchedulerMain.MissionName} | MissionId {MissionId}");
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
                    P.TaskManager.Abort();
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

        private static bool ModeChangeCheck(bool gatherer)
        {
            if (C.OnlyGrabMission || MissionInfoDict[CurrentLunarMission].JobId2 != 0) // Manual Mode for Only Grab Mission / Dual Class Mission
            {
                SchedulerMain.State = IceState.ManualMode;
                return true;
            }
            else if (gatherer)
            {
                //Change to GathererMode Later
                SchedulerMain.State = IceState.ManualMode;

                return true;
            }

            return false;
        }
    }
}
