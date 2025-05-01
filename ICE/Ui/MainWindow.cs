// File: ICE/Ui/MainWindow.cs
using ICE.Scheduler;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using ICE.Enums;
using Dalamud.Interface.Colors;
using System.Drawing;
using System.Reflection;

namespace ICE.Ui
{
    internal class MainWindow : Window
    {
        /// <summary>
        /// Constructor for the main window. Adjusts window size, flags, and initializes data.
        /// </summary>
        public MainWindow() :
            base($"Ice's Cosmic Exploration {P.GetType().Assembly.GetName().Version} ###ICEMainWindow")
        {
            // No special window flags by default.
            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes.
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(500, 500),
                MaximumSize = new Vector2(2000, 2000)
            };

            // Register this window with Dalamud's window system.
            P.windowSystem.AddWindow(this);

            // Disable pinning of this window.
            AllowPinning = false;
        }

        /// <summary>
        /// Dispose of the window by removing it from the window system.
        /// </summary>
        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        // Available jobs and their IDs.
        private static List<(string Name, uint Id)> jobOptions = new()
        {
            ("CRP", 9),
            ("BSM", 10),
            ("ARM", 11),
            ("GSM", 12),
            ("LTW", 13),
            ("WVR", 14),
            ("ALC", 15),
            ("CUL", 16),
            ("MIN", 17),
            ("BTN", 18),
            ("FSH", 19),
        };

        // Available mission ranks and their identifiers.
        private static List<(uint RankId, string RankName)> rankOptions = new()
        {
            (1, "D"),
            (2, "C"),
            (3, "B"),
            (4, "A")
        };

        private static List<(uint Id, string SortOptionName, Func<IEnumerable<KeyValuePair<uint, MissionListInfo>>, IEnumerable<KeyValuePair<uint, MissionListInfo>>> SortFunc)> sortOptions = new()
        {
            (0, "", missions => missions),
            (1, "Name", missions => missions.OrderBy(x => x.Value.Name)),
            (2, "Mission ID", missions => missions),
            (3, "Cosmocredits", missions => missions.OrderBy(x => x.Value.CosmoCredit)),
            (4, "Lunar Credits", missions => missions.OrderBy(x => x.Value.LunarCredit)),
            (5, "Research I", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => ExpDictionary[exp.Type] == "I").FirstOrDefault().Amount)),
            (6, "Research II", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => ExpDictionary[exp.Type] == "II").FirstOrDefault().Amount)),
            (7, "Research III", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => ExpDictionary[exp.Type] == "III").FirstOrDefault().Amount)),
            (8, "Research IV", missions => missions.OrderByDescending(x => x.Value.ExperienceRewards.Where(exp => ExpDictionary[exp.Type] == "IV").FirstOrDefault().Amount))
        };

        // Index of the currently selected job in jobOptions.
        private static int selectedJobIndex = 0;
        // ID of the currently selected crafting job.
        private static uint selectedJobId = jobOptions[selectedJobIndex].Id;
        private static uint currentJobId => GetClassJobId();
        private static bool isCrafter => currentJobId >= 8 && currentJobId <= 15;
        private static bool isGatherer => currentJobId >= 16 && currentJobId <= 18;
        private static bool usingSupportedJob => jobOptions.Any(job => job.Id == currentJobId + 1);

        // Index of the currently selected rank in rankOptions.
        private static int selectedRankIndex = 0;
        // Name of the currently selected rank (for displaying in header).
        private static string selectedRankName = rankOptions[selectedRankIndex].RankName;

        // Configuration booleans bound to checkboxes.
        private static bool delayGrab = C.DelayGrab;
        private static bool silverTurnin = C.TurninOnSilver;
        private static bool turninASAP = C.TurninASAP;
        private static bool hideUnsupported = C.HideUnsupportedMissions;
        private static bool onlyGrabMission = C.OnlyGrabMission;
        private static bool showOverlay = C.ShowOverlay;
        private static bool autoPickCurrentJob = C.AutoPickCurrentJob;
        private static int SortOption = C.TableSortOption;
        private static bool showExp = C.ShowExpColums;

        /// <summary>
        /// Primary draw method. Responsible for drawing the entire UI of the main window.
        /// </summary>
        public override void Draw()
        {
            using var tabbar = ImRaii.TabBar("ConfigTabs###GatherBuddyConfigTabs", ImGuiTabBarFlags.Reorderable);
            if (!tabbar)
                return;

            DrawMissionsTab();
            DrawConfigTab();
        }

        public void DrawMissionsTab()
        {
            var tab = ImRaii.TabItem("Missions");

            if (!tab)
                return;

            // Title text for the run controls.
            ImGui.Text("Run");
            // Help marker explaining how missions are selected and run.
            ImGuiEx.HelpMarker(
                "Please note: this will try and run based off of every rank that it can.\n" +
                "So if you have both C & D checkmarks, it will check C first -> Check D for potential Missions.\n" +
                "It will cycle through missions until it finds one that you have selected.\n" +
                "Unsupported missions will be disabled and shown in red; check 'Hide unsupported missions' to filter them out."
            );

            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());


            ImGui.Spacing();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !usingSupportedJob))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }

                ImGui.SameLine();
                ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);
                ImGui.SameLine();
                ImGui.Checkbox("Stop once hit lunar credits cap", ref SchedulerMain.StopOnceHitCredits);
            }

            ImGui.SameLine();
            ImGui.Spacing();

            if (C.AutoPickCurrentJob && usingSupportedJob)
            {    
                selectedJobIndex = jobOptions.IndexOf(job => job.Id == currentJobId + 1);
                selectedJobId = jobOptions[selectedJobIndex].Id;

                ImGui.Text($"Job: " + jobOptions[selectedJobIndex].Name);
            }
            else
            {
                // Crafting Job selection combo.
                ImGui.SetNextItemWidth(150);
                if (ImGui.BeginCombo("Job", jobOptions[selectedJobIndex].Name))
                {
                    for (int i = 0; i < jobOptions.Count; i++)
                    {
                        bool isSelected = (i == selectedJobIndex);
                        if (ImGui.Selectable(jobOptions[i].Name, isSelected))
                        {
                            selectedJobIndex = i;
                            selectedJobId = jobOptions[i].Id;
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }
            }

            if (ImGui.Checkbox("Auto Pick Current Job", ref autoPickCurrentJob))
            {
                C.AutoPickCurrentJob = autoPickCurrentJob;
                C.Save();
            }

            ImGui.Spacing();

            // Checkbox: Hide unsupported missions.
            if (ImGui.Checkbox("Hide unsupported missions", ref hideUnsupported))
            {
                C.HideUnsupportedMissions = hideUnsupported;
                C.Save();
            }

            ImGui.Spacing();

            if (ImGui.BeginCombo("Sort By", sortOptions[SortOption].SortOptionName))
            {
                for (int i = 0; i < sortOptions.Count; i++)
                {
                    bool isSelected = (i == SortOption);
                    if (ImGui.Selectable(sortOptions[i].SortOptionName, isSelected))
                    {
                        SortOption = i;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                    if (SortOption != C.TableSortOption)
                    {
                        C.TableSortOption = SortOption;
                        C.Save();
                    }
                }
                ImGui.EndCombo();
            }

            // Rank selection combo.
            ImGui.SetNextItemWidth(100);
            IEnumerable<KeyValuePair<uint, MissionListInfo>> criticalMissions =
                MissionInfoDict
            .Where(m => m.Value.JobId == selectedJobId - 1)
            .Where(m => m.Value.IsCriticalMission);
            criticalMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(criticalMissions);
            DrawMissionsDropDown($"Critical Missions - {criticalMissions.Count(x => C.EnabledMission.Any(y => y.Id == x.Key))} enabled", criticalMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> weatherRestrictedMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.Weather != CosmicWeather.FairSkies)
                        .Where(m => !m.Value.IsCriticalMission);
            weatherRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(weatherRestrictedMissions);
            DrawMissionsDropDown($"Weather-restricted Missions - {weatherRestrictedMissions.Count(x => C.EnabledMission.Any(y => y.Id == x.Key))} enabled", weatherRestrictedMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> timeRestrictedMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.Time != 0);
            timeRestrictedMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(timeRestrictedMissions);
            DrawMissionsDropDown($"Time-restricted Missions - {timeRestrictedMissions.Count(x => C.EnabledMission.Any(y => y.Id == x.Key))} enabled", timeRestrictedMissions);

            IEnumerable<KeyValuePair<uint, MissionListInfo>> sequentialMissions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1)
                        .Where(m => m.Value.PreviousMissionID != 0);
            sequentialMissions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(sequentialMissions);
            DrawMissionsDropDown($"Sequential Missions - {sequentialMissions.Count(x => C.EnabledMission.Any(y => y.Id == x.Key))} enabled", sequentialMissions);

            foreach (var rank in rankOptions.OrderBy(r => r.RankName))
            {
                IEnumerable<KeyValuePair<uint, MissionListInfo>> missions =
                    MissionInfoDict
                        .Where(m => m.Value.JobId == selectedJobId - 1 || m.Value.JobId2 == selectedJobId - 1)
                        .Where(m => (m.Value.Rank == rank.RankId) || (rank.RankName == "A" && ARankIds.Contains(m.Value.Rank)))
                        .Where(m => !m.Value.IsCriticalMission)
                        .Where(m => m.Value.Time == 0)
                        .Where(m => m.Value.PreviousMissionID == 0)
                        .Where(m => m.Value.Weather == CosmicWeather.FairSkies);
                missions = sortOptions.FirstOrDefault(s => s.Id == SortOption).SortFunc(missions);

                DrawMissionsDropDown($"Class {rank.RankName} Missions - {missions.Count(x => C.EnabledMission.Any(y => y.Id == x.Key))} enabled", missions);
            }

            ImGui.EndTabItem();
        }

        public void DrawMissionsDropDown(string tabName, IEnumerable<KeyValuePair<uint, MissionListInfo>> missions)
        {
            var tabId = tabName.Split('-')[0];
            if (ImGui.CollapsingHeader(string.Format("{0}###{1}", tabName, tabId)))
            {
                ImGui.Spacing();
                // Missions table with four columns: checkbox, ID, dynamic Rank header, Rewards.

                int columnAmount = 6;
                if (showExp)
                    columnAmount += 4;

                if (ImGui.BeginTable($"MissionList###{tabId}", columnAmount, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                {
                    var sortSpecs = ImGui.TableGetSortSpecs();
                    float col1Width = 0;
                    float col2Width = 0;
                    float col3Width = 15;
                    float col4Width = 0;

                    // used to keep track of where the column index is at, mainly for centering text
                    int columnIndex = 0;

                    // First column: checkbox (empty header)
                    ImGui.TableSetupColumn("Enable##Enable");
                    columnIndex++;

                    // Second column: ID
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, col1Width);
                    columnIndex++;

                    // Third column: dynamic header showing selected rank missions
                    ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, col2Width);
                    columnIndex++;

                    // Fourth column: Rewards
                    ImGui.TableSetupColumn("Cosmocredits");
                    columnIndex++;

                    ImGui.TableSetupColumn("Lunar Credits");
                    columnIndex++;

                    // Dynamic EXP columns
                    IOrderedEnumerable<KeyValuePair<int, string>> orderedExp = ExpDictionary.ToList().OrderBy(exp => exp.Key);
                    if (C.ShowExpColums) // Option to not show the EXP Columns if they'd like
                    {
                        foreach (var exp in orderedExp)
                        {
                            ImGui.TableSetupColumn($"###{exp.Value}", ImGuiTableColumnFlags.WidthFixed, col3Width);
                            col3Width = Math.Max(col3Width, ImGui.CalcTextSize(exp.Value).X + 5);
                            columnIndex++;
                        }
                    }

                    // Final column: Notes
                    ImGui.TableSetupColumn("Notes", ImGuiTableColumnFlags.WidthStretch);

                    // Render the header row (static headers get drawn here)
                    ImGui.TableHeadersRow();

                    // Manually center the EXP headers (more personal OCD here)
                    int dynamicStartCol = columnIndex - orderedExp.Count();
                    int dynamicCol = 0;

                    if (C.ShowExpColums)
                    {
                        foreach (var exp in orderedExp)
                        {
                            ImGui.TableSetColumnIndex(dynamicStartCol + dynamicCol++);
                            float colWidth = ImGui.GetColumnWidth();
                            Vector2 textSize = ImGui.CalcTextSize(exp.Value);
                            float offset = (colWidth - textSize.X) / 2;

                            if (offset > 0)
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                            ImGui.TextUnformatted(exp.Value);
                        }
                    }

                    foreach (var entry in missions)
                    {
                        // Skip unsupported missions if the user has chosen to hide them
                        bool unsupported = UnsupportedMissions.Ids.Contains(entry.Key);
                        if (unsupported && hideUnsupported)
                            continue;

                        // Start a new row
                        ImGui.TableNextRow();

                        // Column 0: Enable checkbox
                        ImGui.TableSetColumnIndex(0);
                        bool enabled = C.EnabledMission.Any(x => x.Id == entry.Key);
                        using (ImRaii.Disabled(unsupported))
                        {
                            // Estimate the width of the checkbox (label is invisible, so the box is all that matters)
                            float cellWidth = ImGui.GetContentRegionAvail().X;
                            float checkboxWidth = ImGui.GetFrameHeight(); // Width of the square checkbox only
                            float offset = (cellWidth - checkboxWidth) * 0.5f;

                            if (offset > 0f)
                                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

                            // Use an invisible label for the checkbox to avoid text spacing
                            if (ImGui.Checkbox($"###{entry.Value.Name}_{entry.Key}", ref enabled))
                            {
                                if (enabled)
                                    C.EnabledMission.Add((entry.Key, entry.Value.Name));
                                else
                                    C.EnabledMission.Remove((entry.Key, entry.Value.Name));

                                C.Save();
                            }

                        }

                        // Column 1: Mission ID
                        ImGui.TableNextColumn();
                        string MissionId = entry.Key.ToString();
                        col1Width = Math.Max(ImGui.CalcTextSize(MissionId).X + 10, col1Width);
                        CenterTextInTableCell($"{MissionId}");

                        // Column 2: Mission Name
                        ImGui.TableNextColumn();
                        string MissionName = entry.Value.Name;
                        if (unsupported)
                        {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), MissionName);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip("Currently not supported");
                            }
                        }
                        else
                        {
                            ImGui.Text($"{MissionName}");
                        }
                        col2Width = Math.Max(ImGui.CalcTextSize(MissionName).X + 10, col2Width);

                        // Column 3: Rewards
                        ImGui.TableNextColumn();
                        CenterTextInTableCell(entry.Value.CosmoCredit.ToString());
                        ImGui.TableNextColumn();
                        CenterTextInTableCell(entry.Value.LunarCredit.ToString());

                        if (showExp) // If show EXP Values are enabled, will show the exp values. 
                        {
                            foreach (var expType in orderedExp)
                            {
                                ImGui.TableNextColumn();
                                var relicXp = entry.Value.ExperienceRewards.Where(exp => exp.Type == expType.Key).FirstOrDefault().Amount.ToString();
                                if (relicXp == "0")
                                {
                                    relicXp = "-";
                                }
                                col3Width = Math.Max(ImGui.CalcTextSize(relicXp).X + 10, col3Width);
                                CenterTextInTableCell(relicXp);
                            }
                        }
                        
                        // debug
                        ImGui.TableNextColumn();
                        if (entry.Value.Weather != CosmicWeather.FairSkies)
                        {
                            ImGui.Text(entry.Value.Weather.ToString());
                        }
                        else if (entry.Value.Time != 0)
                        {

                            ImGui.Text($"{2 * (entry.Value.Time - 1)}:00 - {2 * (entry.Value.Time)}:00");
                        }
                        else if (entry.Value.PreviousMissionID != 0)
                        {
                            var (Id, Name) = MissionInfoDict.Where(m => m.Key == entry.Value.PreviousMissionID).Select(m => (Id: m.Key, Name: m.Value.Name)).FirstOrDefault();
                            ImGui.Text($"[{Id}] {Name}");
                        }
                    }

                    ImGui.EndTable();
                }
            }
        }

        public void DrawConfigTab()
        {
            var tab = ImRaii.TabItem("Config");

            if (!tab)
                return;

            DrawLink("Say no to global warming, support Ice today: ", "https://ko-fi.com/ice643269", "https://ko-fi.com/ice643269");

            // Checkbox: Add delay to grabbing mission.
            if (ImGui.Checkbox("Add delay to grabbing mission", ref delayGrab))
            {
                C.DelayGrab = delayGrab;
                C.Save();
            }

            // Checkbox: Turn in on silver.
            if (ImGui.Checkbox("Turnin on Silver", ref silverTurnin))
            {
                // Ensure mutual exclusivity with Turnin ASAP.
                if (turninASAP)
                {
                    turninASAP = false;
                    C.TurninASAP = false;
                }

                if (silverTurnin != C.TurninOnSilver)
                {
                    C.TurninOnSilver = silverTurnin;
                    C.Save();
                }


            }

            // Checkbox: Turn in ASAP.
            if (ImGui.Checkbox("Turnin ASAP", ref turninASAP))
            {
                // Ensure mutual exclusivity with Turnin on Silver.
                if (silverTurnin)
                {
                    silverTurnin = false;
                    C.TurninOnSilver = false;
                }

                C.TurninASAP = turninASAP;
                C.Save();
            }

            if (ImGui.Checkbox("Only Grab Mission", ref onlyGrabMission))
            {
                C.OnlyGrabMission = onlyGrabMission;
                C.Save();
            }

            if (ImGui.Checkbox("Show Overlay", ref showOverlay))
            {
                C.ShowOverlay = showOverlay;
                C.Save();
            }             
            
            ImGui.Spacing();
            if (ImGui.Checkbox($"Show EXP on Columns", ref showExp))
            {
                C.ShowExpColums = showExp;
                C.Save();
            }

            ImGui.EndTabItem();
        }

        public void DrawDebugTab()
        {
            var tab = ImRaii.TabItem("Debug");

            if (!tab)
                return;

            if(ImGui.Button("ExitCraft"))
            {
                ExecuteCommand(711);
            }
        }

        public static void DrawLink(string label, string link, string url)
        {
            ImGui.Text(label);
            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.TankBlue);
            ImGui.Text(link);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }

            if (ImGui.IsItemClicked())
            {
                Dalamud.Utility.Util.OpenLink(url);
            }
        }

        public static void CenterTextInTableCell(string text)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float textWidth = ImGui.CalcTextSize(text).X;
            float offset = (cellWidth - textWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            ImGui.TextUnformatted(text);
        }

        public static void CenterCheckboxInTableCell(string label, ref bool value, ref bool config)
        {
            float cellWidth = ImGui.GetContentRegionAvail().X;
            float checkboxWidth = ImGui.CalcTextSize(label).X + ImGui.GetFrameHeight(); // estimate checkbox width
            float offset = (cellWidth - checkboxWidth) * 0.5f;

            if (offset > 0f)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

            if (ImGui.Checkbox(label, ref value))
            {

            }
        }

    }
}

    

