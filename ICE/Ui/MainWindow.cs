using ICE.Scheduler;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;

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

            Flags = ImGuiWindowFlags.None;

            // Set up size constraints to ensure window cannot be too small or too large.
            // Increased minimum size to better accommodate larger font sizes
            SizeConstraints = new()
            {
                MinimumSize = new Vector2(500, 500),
                MaximumSize = new Vector2(2000, 2000)
            };

            // Register this window with Dalamud's window system.
            P.windowSystem.AddWindow(this);

            AllowPinning = false;
        }

        public void Dispose() {  }

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
        };

        private static List<(uint RankId, string RankName)> rankOptions = new()
        {
            (1, "D"),
            (2, "C"),
            (3, "B"),
            (4, "A"),
        };

        private static int selectedIndex = 0; // Index of the currently selected job
        private static uint selectedJobId = jobOptions[selectedIndex].Id;

        private static int selectedRankIndex = 0;
        private static string selectedRankName = rankOptions[selectedRankIndex].RankName;

        private static bool delayGrab = C.DelayGrab;
        private static bool doFood = C.FoodMe;
        private static bool once = C.Once;
        private static bool areStopping = false;

        /// <summary>
        /// Primary draw method. Responsible for drawing the entire UI of the main window.
        /// </summary>
        public override void Draw()
        {
            ImGui.Text("Run");

            ImGuiEx.HelpMarker("Please note: this will try and run based off of every rank that it can.\n" +
                                "So if you have both C & D checkmarks, it will check C first -> Check D for potentional Missions.\n" +
                                "ALSO. It will cycle through missions until it finds one that you have selected\n" +
                                "Still a WIP. So there will be bugs. (I have a feeling other languages might have problems)");

            ImGui.Spacing();

            using (ImRaii.Disabled(SchedulerMain.AreWeTicking))
            {
                if (ImGui.Button("Start"))
                {
                    areStopping = false;
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(!SchedulerMain.AreWeTicking))
            {
                if (ImGui.Button("Stop"))
                {
                    areStopping = false;
                    SchedulerMain.DisablePlugin();
                }
            }

            ImGui.SameLine();

            using (ImRaii.Disabled(!SchedulerMain.AreWeTicking || areStopping))
            {
                if (ImGui.Button("Stop On Finish"))
                {
                    areStopping = true;
                    P.taskManager.Enqueue(SchedulerMain.DisablePlugin);
                }
            }

            if (areStopping)
            {
                ImGui.SameLine();
                ImGui.Text("Stopping on mission complete");
            }

            if (ImGui.Checkbox("Add delay to grabbing mission", ref delayGrab))
            {
                if (delayGrab != C.DelayGrab)
                {
                    C.DelayGrab = delayGrab;
                    C.Save();
                }
            }

            if (ImGui.Checkbox("Food me!", ref doFood))
            {
                if (doFood != C.FoodMe)
                {
                    C.FoodMe = doFood;
                    C.Save();
                }
            }

            if (ImGui.Checkbox("Once", ref once))
            {
                if (once != C.Once)
                {
                    C.Once = once;
                    C.Save();
                }
            }


            ImGui.Spacing();
            ImGui.Text("Target Research: ");

            if (ImGui.Checkbox("1", ref C.TargetResearch[0]))
            {
                C.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("2", ref C.TargetResearch[1]))
            {
                C.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("3", ref C.TargetResearch[2]))
            {
                C.Save();
            }
            ImGui.SameLine();
            if (ImGui.Checkbox("4", ref C.TargetResearch[3]))
            {
                C.Save();
            }

            if (ImGui.BeginCombo("Crafting Job", jobOptions[selectedIndex].Name))
            {
                for (int i = 0; i < jobOptions.Count; i++)
                {
                    bool isSelected = (i == selectedIndex);
                    if (ImGui.Selectable(jobOptions[i].Name, isSelected))
                    {
                        selectedIndex = i;
                        selectedJobId = jobOptions[i].Id;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if (ImGui.BeginCombo("Rank", rankOptions[selectedRankIndex].RankName))
            {
                for (int i = 0; i < rankOptions.Count; i++)
                {
                    bool isSelected = (i == selectedRankIndex);
                    if (ImGui.Selectable(rankOptions[i].RankName, isSelected))
                    {
                        selectedRankIndex = i;
                        selectedRankName = rankOptions[i].RankName;
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            if (ImGui.BeginTable("###MissionList", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
            {
                ImGui.TableSetupColumn("###Enable");
                ImGui.TableSetupColumn("###MissionName");

                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"Rank {selectedRankName} Missions");
                foreach (var entry in MissionInfoDict)
                {
                    if (entry.Value.JobId != selectedJobId - 1)
                        continue;

                    if (selectedRankIndex == 3)
                    {
                        if (!ARankIds.Contains(entry.Value.Rank))
                            continue;
                    }
                    else if (entry.Value.Rank != rankOptions[selectedRankIndex].RankId)
                        continue;

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    bool temp = C.EnabledMission.Any(x => x.Id == entry.Key);
                    if (ImGui.Checkbox($"###{entry.Value.Name} + {entry.Key}", ref temp))
                    {
                        if (temp)
                        {
                            if (!C.EnabledMission.Any(x => x.Id == entry.Key))
                            {
                                C.EnabledMission.Add((entry.Key, entry.Value.Name));
                                C.Save();
                            }
                        }
                        else if (!temp)
                        {
                            if (C.EnabledMission.Any(x => x.Id == entry.Key))
                            {
                                C.EnabledMission.Remove((entry.Key, entry.Value.Name));
                                C.Save();
                            }
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text($"{entry.Value.Name}");
                }

                ImGui.EndTable();
            }
        }
    }
}
