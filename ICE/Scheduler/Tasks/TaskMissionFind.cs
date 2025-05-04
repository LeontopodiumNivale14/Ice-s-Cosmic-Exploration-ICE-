using ECommons.Automation;
using ECommons.Logging;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using ICE.Scheduler.Handlers;
using ICE.Ui;
using System.Collections.Generic;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskMissionFind
    {
        private static uint MissionId = 0;
        private static uint? currentClassJob => GetClassJobId();
        private static bool isGatherer => currentClassJob >= 16 && currentClassJob <= 18;
        private static bool hasCritical => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Critical && x.Enabled);
        private static bool hasWeather => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Weather && x.Enabled);
        private static bool hasTimed => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Timed && x.Enabled);
        private static bool hasSequence => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Where(x => x.Enabled).Any(x => x.Type == MissionType.Sequential && C.Missions.Any(y => y.PreviousMissionId == x.Id)); // might be bad logic but should work and these fields arent used rn anyway
        private static bool hasStandard => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Standard && x.Enabled);
        private static bool HasA2 => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Standard && MissionInfoDict[x.Id].Rank == 5 && x.Enabled);
        private static bool HasA1 => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Standard && MissionInfoDict[x.Id].Rank == 4 && x.Enabled);
        private static bool HasB => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Standard && MissionInfoDict[x.Id].Rank == 3 && x.Enabled);
        private static bool HasC => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Standard && MissionInfoDict[x.Id].Rank == 2 && x.Enabled);
        private static bool HasD => C.Missions.Where(x => !UnsupportedMissions.Ids.Contains(x.Id)).Where(x => x.JobId == currentClassJob).Any(x => x.Type == MissionType.Standard && MissionInfoDict[x.Id].Rank == 1 && x.Enabled);

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
            if (SchedulerMain.StopOnceHitCosmoCredits)
            {
                if (TryGetAddonMaster<AddonMaster.WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
                    if (hud.CosmoCredit >= 30000)
                    {
                        PluginLog.Debug($"[SchedulerMain] Stopping the plugin as you have {hud.CosmoCredit} Cosmocredits");
                        SchedulerMain.StopBeforeGrab = false;
                        SchedulerMain.StopOnceHitCosmoCredits = false;
                        SchedulerMain.State = IceState.Idle;
                        return;
                    }
                }
            }

            if (SchedulerMain.StopOnceHitLunarCredits)
            {
                if (TryGetAddonMaster<AddonMaster.WKSHud>("WKSHud", out var hud) && hud.IsAddonReady)
                {
                    if (hud.LunarCredit >= 10000)
                    {
                        PluginLog.Debug($"[SchedulerMain] Stopping the plugin as you have {hud.LunarCredit} Lunar Credits");
                        SchedulerMain.StopBeforeGrab = false;
                        SchedulerMain.StopOnceHitLunarCredits = false;
                        SchedulerMain.State = IceState.Idle;
                        return;
                    }
                }
            }

            if (SchedulerMain.StopBeforeGrab)
            {
                SchedulerMain.StopBeforeGrab = false;
                SchedulerMain.State = IceState.Idle;
                return;
            }

            if (SchedulerMain.State != IceState.GrabMission)
                return;

            SchedulerMain.State = IceState.GrabbingMission;

            // Attempt to swap jobs
            P.TaskManager.Enqueue(() => JobSwap(), "Job Swapping");
            P.TaskManager.EnqueueDelay(200);

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
                            if (!ModeChangeCheck(gatherer))
                            {
                                SchedulerMain.State = IceState.StartCraft;
                            }
                        }
                    }
                }
            });
        }

        internal static bool? JobSwap()
        {
            bool needSwap = false;
            uint swapId = 0;
            if (C.EnableWeatherJobSwap)
            {
                var currentWeatherId = WeatherForecastHandler.GetCurrentWeatherId();
                if (currentWeatherId == 49)
                {
                    needSwap = true;
                    swapId = C.UmbralWindJobId;
                } else if (currentWeatherId == 148)
                {
                    needSwap = true;
                    swapId = C.MoonDustJobId;
                }
            }
            if (C.EnableTimeJobSwap && !needSwap)
            {
                var currentHour = (int)PlayerHandlers.GetCurrentHour() / 2;
                needSwap = true;
                swapId = C.JobSwapTable[currentHour];
            }
            if (needSwap && GetClassJobId() != swapId)
            {
                PluginLog.Debug($"[Job Swap] Swapping to {swapId}");
                GearsetHandler.SwapJob(swapId);
                return false;
            }
            return true;
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
                    var criticalMissionEntry = C.Missions.Where(x => x.Enabled && x.JobId == currentClassJob).FirstOrDefault(e => e.Id == m.MissionId);

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

                var weatherIds = C.Missions.Where(x => x.Type == MissionType.Weather).Select(w => w.Id).ToHashSet();
                var sequenceIds = C.Missions.Where(x => x.Type == MissionType.Sequential).Select(s => s.Id).ToHashSet();
                var timedIds = C.Missions.Where(x => x.Type == MissionType.Timed).Select(t => t.Id).ToHashSet();

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
                    var weatherMissionEntry = C.Missions.Where(x => x.Enabled && x.JobId == currentClassJob).FirstOrDefault(e => e.Id == m.MissionId && MissionInfoDict[e.Id].JobId == currentClassJob);

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
                    var basicMissionEntry = C.Missions.Where(x => x.Enabled && x.JobId == currentClassJob).FirstOrDefault(e => e.Id == m.MissionId);

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
                var ranks = C.Missions.Where(x => x.Enabled && x.JobId == currentClassJob)
                    .Select(e => MissionInfoDict[e.Id].Rank)
                    .ToList();
                if (ranks.Count == 0)
                {
                    PluginLog.Debug("No missions selected in UI, would abandon every mission");
                    return false;
                }

                int A2 = x.StellerMissions.Where(x => MissionInfoDict[x.MissionId].Rank == 5).Count();
                int A1 = x.StellerMissions.Where(x => MissionInfoDict[x.MissionId].Rank == 4).Count();
                var missionRanks = new List<(bool hasMission, uint rank)>
                    {
                        (A2 !=0 && HasA2, 5),
                        (A2 == 0 && HasA2 || HasA1, 4),
                        (HasB, 3),
                        (HasC, 2),
                        (HasD, 1),
                    }
                        .Where(x => x.hasMission)
                        .Select(x => x.rank)
                        .ToArray();

                var rankToReset = missionRanks.Max();
                var r = new Random();
                foreach (var m in x.StellerMissions.OrderBy(x => r.Next()))
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
                string[] commenceStrings = ["選択したミッションを開始します。よろしいですか？","Commence selected mission?","Ausgewählte Mission wird gestartet.Fortfahren?","Commencer la mission sélectionnée ?"];

                if (commenceStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                {
                    PluginDebug("[SelectYesNo] Looks like a Commence window");
                    if (EzThrottler.Throttle("Selecting Yes", 250))
                    {
                        select.Yes();
                    }
                }
                else
                {
                    PluginDebug("[SelectYesNo] Looks like a Fake window");
                    select.No();
                    if (EzThrottler.Throttle("Selecting No", 250))
                    {
                        select.No();
                    }
                    return false;
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
                    string[] abandonStrings = ["受注中のミッションを破棄します。よろしいですか？","Abandon mission?","Aktuelle Mission abbrechen?","Êtes-vous sûr de vouloir abandonner la mission en cours ?"];

                    if (abandonStrings.Any(select.Text.Contains) || !C.RejectUnknownYesno)
                    {
                        PluginDebug("[SelectYesNo] Looks like a Abandon window");
                        if (EzThrottler.Throttle("Confirming Abandon"))
                        {
                            select.Yes();
                            return true;
                        }
                    }
                    else
                    {
                        PluginDebug("[SelectYesNo] Looks like a Fake window");
                        if (EzThrottler.Throttle("Selecting No", 250))
                        {
                            select.No();
                        }
                        return false;
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
