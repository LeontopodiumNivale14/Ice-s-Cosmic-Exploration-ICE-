using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Scheduler;
using ICE.Scheduler.Tasks;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Ui;

internal class DebugWindow : Window
{
    public DebugWindow() :
        base($"ICE {P.GetType().Assembly.GetName().Version} ###IceCosmicDebug1")
    {
        Flags = ImGuiWindowFlags.NoCollapse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(100, 100),
            MaximumSize = new Vector2(3000, 3000)
        };
        P.windowSystem.AddWindow(this);
    }

    public void Dispose() { }

    // variables that hold the "ref"s for ImGui

    public override unsafe void Draw()
    {
        var sheet = Svc.Data.GetExcelSheet<WKSMissionRecipe>();

        if (ImGui.TreeNode("Main Hud"))
        {
            if (TryGetAddonMaster<WKSHud>("WKSHud", out var HudAddon))
            {
                if (ImGui.Button("Mission"))
                {
                    HudAddon.Mission();
                }

                ImGui.SameLine();

                if (ImGui.Button("Mech"))
                {
                    HudAddon.Mech();
                }

                ImGui.SameLine();
                
                if (ImGui.Button("Steller"))
                {
                    HudAddon.Steller();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Infrastructor"))
                {
                    HudAddon.Infrastructor();
                }

                ImGui.SameLine();

                if (ImGui.Button("Research"))
                {
                    HudAddon.Research();
                }

                ImGui.SameLine();

                if (ImGui.Button("ClassTracker"))
                {
                    HudAddon.ClassTracker();
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Research"))
        {
            if (TryGetAddonMaster<WKSToolCustomize>("WKSToolCustomize", out var x) && x.IsAddonReady)
            {
                ImGui.Text("List of Visible Missions");
                ImGui.Text($"Selected Class: {x.SelectedClass}");

                
                ImGui.Text("Current Research: ");
                foreach (var research in x.CurrentResearch)
                {
                    ImGui.SameLine();
                    ImGui.Text($"{research} ");
                }

                ImGui.Text("Target Research: ");
                foreach (var research in x.TargetResearch)
                {
                    ImGui.SameLine();
                    ImGui.Text($"{research} ");
                }

                ImGui.Text("Max Research: ");
                foreach (var research in x.MaxResearch)
                {
                    ImGui.SameLine();
                    ImGui.Text($"{research} ");
                }

                ImGui.Text("Need Research: ");
                foreach (var research in x.CurrentResearch.Zip(x.TargetResearch, (cur, targ) => cur<targ))
                {
                    ImGui.SameLine();
                    ImGui.Text($"{research} ");
                }

            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Missions"))
        {
            if (TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
            {
                ImGui.Text("List of Visible Missions");
                ImGui.Text($"Selected Mission: {x.SelectedMission}");

                if (ImGui.Button("Help"))
                {
                    x.Help();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Selection"))
                {
                    x.MissionSelection();
                }
                ImGui.SameLine();

                if (ImGui.Button("Mission Log"))
                {
                    x.MissionLog();
                }
                ImGui.SameLine();

                if (ImGui.Button("Basic Missions"))
                {
                    x.BasicMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Provisional Missions"))
                {
                    x.ProvisionalMissions();
                }
                ImGui.SameLine();

                if (ImGui.Button("Critical Missions"))
                {
                    x.CriticalMissions();
                }

                foreach (var m in x.StellerMissions)
                {
                    ImGui.Text($"{m.Name}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Select###Select + {m.Name}"))
                    {
                        m.Select();
                    }
                }

            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Mission Info"))
        {
            uint currentScore = 0;
            uint silverScore = 0;
            uint goldScore = 0;

            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                currentScore = x.CurrentScore;
                silverScore = x.SilverScore;
                goldScore = x.GoldScore;

                ImGui.Text($"Addon Ready: {IsAddonActive("WKSMissionInfomation")}");
                if (IsAddonActive("WKSMissionInfomation"))
                {
                    ImGui.Text($"Node Text: {GetNodeText("WKSMissionInfomation", 27)}");
                }

                if (ImGui.BeginTable("Mission Info", 2))
                {
                    ImGui.TableSetupColumn("###Info", ImGuiTableColumnFlags.WidthFixed, 150);
                    ImGui.TableSetupColumn("###UiInfo", ImGuiTableColumnFlags.WidthFixed, 100);

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Current Score:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{currentScore}");

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Silver Score:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{silverScore}");

                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text("Gold Score:");

                    ImGui.TableNextColumn();
                    ImGui.Text($"{goldScore}");

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Cosmo Pouch"))
                    {
                        x.CosmoPouch();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Cosmo Crafting Log"))
                    {
                        x.CosmoCraftingLog();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Steller Reduction"))
                    {
                        x.StellerReduction();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Report"))
                    {
                        x.Report();
                    }

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);
                    if (ImGui.Button("Abandon"))
                    {
                        x.Abandon();
                    }


                    ImGui.EndTable();
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Moon Recipe Notebook"))
        {
            if (TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var x) && x.IsAddonReady)
            {
                ImGui.Text(x.SelectedCraftingItem);

                if (ImGui.Button("Fill NQ"))
                {
                    x.NQItemInput();
                }
                ImGui.SameLine();

                if (ImGui.Button("Fill HQ"))
                {
                    x.HQItemInput();
                }
                ImGui.SameLine();
                
                if (ImGui.Button("Fill Both"))
                {
                    x.NQItemInput();
                    x.HQItemInput();
                }
                ImGui.SameLine();

                if (ImGui.Button("Synthesize"))
                {
                    x.Synthesize();
                }

                foreach (var m in x.CraftingItems)
                {
                    if (ImGui.Button($"Select ###Select + {m.Name}"))
                    {
                        m.Select();
                    }
                    ImGui.SameLine();
                    ImGui.Text($"{m.Name}");
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Crafting Table"))
        {
            var sheetRow = sheet.GetRow(27);
            ImGui.Text($"Unknown 0: {sheetRow.Unknown0} | Unknown 1: {sheetRow.Unknown1}");
            ImGui.Text($"Unknown 2: {sheetRow.Unknown2} | Unknown 3: {sheetRow.Unknown3}");
            ImGui.Text($"Unknown 4: {sheetRow.Unknown4}");

            Table();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Moon Recipies"))
        {
            Table2();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Test Buttons"))
        {
            ImGui.Text($"Current Mission: {CurrentLunarMission}");
            ImGui.Text($"Artisan Endurance: {P.artisan.GetEnduranceStatus()}");

            var ExpSheet = Svc.Data.GetExcelSheet<WKSMissionReward>();
            //  4 - Col 2  - Unknown 7
            //  8 - Col 3  - Unknown 0
            // 10 - Col 4  - Unknown 1
            //  3 - Col 7  - Unknown 12
            //  7 - Col 8  - Unknown 2
            //  2 - Col 10 - Unknown 13
            //  5 - Col 11 - Unknown 3
            //  1 - Col 13 - Unknown 14
            //  5 - Col 14 - Unknown 4
            //  0          - Unknown 5
            //  0          - Unknown 6
            //  0          - Unknown 8
            //  1          - Unknown 9 
            //  1          - Unknown 10
            //  1          - Unknown 11

            ImGui.Text($"{WKSManager.Instance()->CurrentMissionUnitRowId}");

            if (ImGui.Button("Find Mission"))
            {
                TaskMissionFind.Enqueue();
            }
            if (ImGui.Button("Clear Task"))
            {
                P.taskManager.Abort();
            }
            if (ImGui.Button("Artisan Craft"))
            {
                P.artisan.CraftItem(36176, 1);
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Test Section"))
        {
            var MoonMissionSheet = Svc.Data.GetExcelSheet<WKSMissionUnit>();
            var moonRow = MoonMissionSheet.GetRow(26);
            ImGui.Text($"{moonRow.Unknown1} \n" +
                       $"{moonRow.Unknown2} \n" +
                       $"{moonRow.Unknown3} \n" +
                       $"{moonRow.Unknown4} \n" +
                       $"{moonRow.Unknown5} \n" +
                       $"{moonRow.Unknown6} \n" +
                       $"{moonRow.Unknown7} \n" +
                       $"{moonRow.Unknown8} \n" +
                       $"{moonRow.Unknown9} \n" +
                       $"{moonRow.Unknown10} \n" +
                       $"{moonRow.Unknown11} \n" +
                       $"{moonRow.Unknown12} \n" +
                       $"{moonRow.Unknown13} \n" +
                       $"{moonRow.Unknown14} \n" +
                       $"{moonRow.Unknown15} \n" +
                       $"{moonRow.Unknown16} \n" +
                       $"{moonRow.Unknown17} \n" +
                       $"{moonRow.Unknown18} \n" +
                       $"{moonRow.Unknown19} \n" +
                       $"{moonRow.Unknown20} \n");

            var toDoSheet = Svc.Data.GetExcelSheet<WKSMissionToDo>();
            var toDoRow = toDoSheet.GetRow(168);

            ImGui.Text($"     TODO         \n" +
                       $"{toDoRow.Unknown0}\n" +
                       $"{toDoRow.Unknown1}\n" +
                       $"{toDoRow.Unknown2}\n" +
                       $"{toDoRow.Unknown3}\n" + // need Item 1
                       $"{toDoRow.Unknown4}\n" + // Item 2
                       $"{toDoRow.Unknown5}\n" + // Item 3
                       $"{toDoRow.Unknown6}\n" + // Item 1 Amount
                       $"{toDoRow.Unknown7}\n" + // Item 2 Amount
                       $"{toDoRow.Unknown8}\n" + // Item Amount 3 end
                       $"{toDoRow.Unknown9}\n" +
                       $"{toDoRow.Unknown10}\n" +
                       $"{toDoRow.Unknown11}\n" +
                       $"{toDoRow.Unknown12}\n" +
                       $"{toDoRow.Unknown13}\n" +
                       $"{toDoRow.Unknown14}\n" +
                       $"{toDoRow.Unknown15}\n" +
                       $"{toDoRow.Unknown16}\n" +
                       $"{toDoRow.Unknown17}\n" +
                       $"{toDoRow.Unknown18}\n");

            ImGui.Spacing();
            var moonItemSheet = Svc.Data.GetExcelSheet<WKSItemInfo>();
            var moonItemRow = moonItemSheet.GetRow(523);

            ImGui.Text($"  WKS Item Info\n" +
                       $"{moonItemRow.Unknown0}\n" +
                       $"{moonItemRow.Unknown1}\n" +
                       $"{moonItemRow.Unknown2}\n" +
                       $"{moonItemRow.Unknown3}\n");

            ImGui.TreePop();
        }


    }

    private void Table()
    {
        var itemSheet = Svc.Data.GetExcelSheet<Item>();

        if (ImGui.BeginTable("Mission Info List", 17, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, 25);
            ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("2nd Job", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("RecipeID", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("MainItem", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Required Item", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Amount###SubItem1Amount", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("SubItem 2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Amount###SubItem2Amount", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Type 1###MissionExpType1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Amount 1###MissionExpAmount1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Type 2###MissionExpType2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Amount 2###MissionExpAmount2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Type 3###MissionExpType3", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Exp Amount 3###MissionExpAmount3", ImGuiTableColumnFlags.WidthFixed, 100);

            ImGui.TableHeadersRow();

            foreach (var entry in MissionInfoDict)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{entry.Key}");

                // Mission Name
                ImGui.TableNextColumn();
                ImGui.Text(entry.Value.Name);

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.JobId}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.JobId2}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.Rank}");

                ImGui.TableNextColumn();
                var RecipeSearch = entry.Value.RecipeId;
                ImGui.Text($"{RecipeSearch}");

            }

            ImGui.EndTable();
        }
    }

    private void Table2()
    {
        if (ImGui.BeginTable("Mission Info List", 11, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Bool", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Main Item 1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Main Item 2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Main Item 3", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Subcraft 1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Subcraft 2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Subcraft 3", ImGuiTableColumnFlags.WidthFixed, 100);

            ImGui.TableHeadersRow();

            foreach (var entry in MoonRecipies)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.Text($"{entry.Key}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.PreCrafts}");

                ImGui.TableNextColumn();
                if (entry.Value.PreCrafts == true)
                {
                    foreach (var sub in entry.Value.PreCraftDict)
                    {
                        ImGui.Text($"Recipe: {sub.Key} | Amount: {sub.Value}");
                        ImGui.TableNextColumn();
                    }
                }
            }

            ImGui.EndTable();
        }
    }
}