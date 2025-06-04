using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using System.IO;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static ICE.Utilities.CosmicHelper;

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

    public void Dispose()
    {
        P.windowSystem.RemoveWindow(this);
    }

    // variables that hold the "ref"s for ImGui

    private int Radius = 10;
    private int XLoc = 0;
    private int YLoc = 0;
    private int TableRow = 1;
    private int posX = 0;
    private int posY = 0;
    private int posRadius = 0;
    private float wantedRotation = 0;

    private string CraftingTableSearchText = "";
    private string RecipeTableSearchText = "";

    public override unsafe void Draw()
    {
        if (ImGui.TreeNode("Player Info"))
        {
            if (ImGui.Button("Copy current POS"))
            {
                ImGui.SetClipboardText($"{Player.Position.X}f, {Player.Position.Y}f, {Player.Position.Z}f,");
            }
            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            Vector4 PositionRot = new(Player.Position, Player.Rotation);
            ImGui.Text($"{PositionRot}");
            ImGui.SliderAngle("Wanted Angle", ref wantedRotation);
            ImGui.SameLine();
            ImGui.Text(wantedRotation.ToString());
            ImGui.SameLine();
            if (ImGui.Button("Set"))
            {
                PlayerHelper.SetRotation(wantedRotation);
            }
            ImGui.Text($"GP: {PlayerHelper.GetGp()}");

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Main Hud"))
        {
            if (GenericHelpers.TryGetAddonMaster<WKSHud>("WKSHud", out var HudAddon))
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

        if (ImGui.TreeNode("Missions"))
        {
            if (GenericHelpers.TryGetAddonMaster<WKSMission>("WKSMission", out var x) && x.IsAddonReady)
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

            if (GenericHelpers.TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var x) && x.IsAddonReady)
            {
                currentScore = x.CurrentScore;
                silverScore = x.SilverScore;
                goldScore = x.GoldScore;

                var isAddonReady = AddonHelper.IsAddonActive("WKSMissionInfomation");
                ImGui.Text($"Addon Ready: {isAddonReady}");
                if (isAddonReady)
                {
                    ImGui.Text($"Node Text: {AddonHelper.GetNodeText("WKSMissionInfomation", 27)}");
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

        if (ImGui.TreeNode("Wheel of fortune!"))
        {
            if (GenericHelpers.TryGetAddonMaster<WKSLottery>("WKSLottery", out var lotto) && lotto.IsAddonReady)
            {
                ImGui.Text($"Lottery addon is visible!");

                if (ImGui.Button($"Left wheel select"))
                {
                    TaskGamba.SelectWheelLeft(lotto);
                }
                ImGui.SameLine();

                if (ImGui.Button($"Right wheel select"))
                {
                    TaskGamba.SelectWheelRight(lotto);
                }

                ImGui.SameLine();
                if (ImGui.Button($"Confirm"))
                {
                    lotto.ConfirmButton();
                }

                if (ImGui.Button($"Auto Gamba (Once)"))
                {
                    TaskGamba.TryHandleGamba();
                }

                ImGui.Text($"Items in left wheel");
                foreach (var l in lotto.LeftWheelItems)
                {
                    ImGui.Text($"Name: {l.itemName} | Id: {l.itemId} | Amount: {l.itemAmount}");
                }

                ImGui.Spacing();
                foreach (var m in lotto.RightWheelItems)
                {
                    ImGui.Text($"Name: {m.itemName} | Id: {m.itemId} | Amount: {m.itemAmount}");
                }
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Moon Recipe Notebook"))
        {
            if (GenericHelpers.TryGetAddonMaster<WKSRecipeNotebook>("WKSRecipeNotebook", out var x) && x.IsAddonReady)
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
            var sheetRow = ExcelHelper.MoonRecipeSheet.GetRow(27);

            Table();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Moon Recipies"))
        {
            Table2();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Gathering Table"))
        {
            uint missionId = 418;
            var Todo = ExcelHelper.MoonMissionSheet.GetRow(missionId).Unknown7;
            var PotentionalValue = ExcelHelper.ToDoSheet.GetRow(Todo).Unknown10;
            var EvaluationItem = ExcelHelper.EvalSheet.GetSubrowAt(PotentionalValue, 0);


            ImGui.Text($"Mission: 418 | Todo Spot: {Todo}");
            ImGui.Text($"Todo Row: {Todo} | Unknown 10 Value: {PotentionalValue}");
            ImGui.Text($"Evaluation SubRowID: {EvaluationItem.SubrowId}");
            ImGui.Text($"Evaluation Item: {EvaluationItem.Item}");

            Table3();

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Test Buttons"))
        {
            ImGui.Text($"Current Mission: {CosmicHelper.CurrentLunarMission}");
            ImGui.Text($"Artisan Endurance: {P.Artisan.GetEnduranceStatus()}");

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
                P.TaskManager.Abort();
            }
            if (ImGui.Button("Artisan Craft"))
            {
                P.Artisan.CraftItem(36176, 1);
            }
            if (ImGui.Button("RecipeNote"))
            {
                AddonHelper.OpenRecipeNote();
            }
            var gameObject = Utils.TryGetObjectNearestEventObject();
            float gameObjectDistance = 0;
            if (gameObject is not null)
                gameObjectDistance = PlayerHelper.GetDistanceToPlayer(gameObject);
            if (ImGui.Button("Click Nearest EventObject"))
            {
                Utils.TargetgameObject(gameObject);
                Utils.InteractWithObject(gameObject);
            }
            ImGui.SameLine();
            ImGui.Text($"Distance to nearest: {gameObjectDistance}");

            var collectionPoint = Utils.TryGetObjectCollectionPoint();
            float collectionPointDistance = 0;
            if (collectionPoint is not null)
                collectionPointDistance = PlayerHelper.GetDistanceToPlayer(collectionPoint);
            if (ImGui.Button("Click Nearest Collection Point"))
            {
                Utils.TargetgameObject(collectionPoint);
                Utils.InteractWithObject(collectionPoint);
            }
            ImGui.SameLine();
            ImGui.Text($"Distance to nearest: {collectionPointDistance}");

            if (ImGui.Button("Print GatheringPoint Info"))
            {
                var gatheringPoint = Svc.ClientState.LocalPlayer.TargetObject;
                if (gatheringPoint is not null)
                {
                    var nodeId = gatheringPoint.DataId;
                    var position = gatheringPoint.Position;
                    var landZone = gatheringPoint.Position;
                    var gatheringType = (Job)PlayerHelper.GetClassJobId() == Job.MIN ? 2 : 3;
                    var currentMission = CosmicHelper.CurrentMissionInfo;
                    var nodeSet = currentMission?.NodeSet ?? 0;

                    string info = $"new GathNodeInfo\n{{\n    ZoneId = 1237,\n    NodeId = {nodeId},\n    Position = new Vector3({position.X}f, {position.Y}f, {position.Z}f),\n    LandZone = new Vector3({landZone.X}f, {landZone.Y}f, {landZone.Z}f),\n    GatheringType = {gatheringType},\n    NodeSet = {nodeSet}\n}}";

                    ImGui.SetClipboardText(info);
                    Svc.Chat.Print(info);
                }
                else
                {
                    Svc.Chat.Print("No GatheringPoint targeted.");
                }
            }

            if (ImGui.Button("Switch class to CRP"))
            {
                GearsetHandler.TaskClassChange(Job.CRP);
            }
            if (ImGui.Button("Switch class to MIN"))
            {
                GearsetHandler.TaskClassChange(Job.MIN);
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Test Section"))
        {
            if (ImGui.Button("Export to CVS"))
            {
                ExportMissionInfoToCsv(MissionInfoDict, @"D:\Missions.csv");

            }

            var moonRow = ExcelHelper.MoonMissionSheet.GetRow(26);
            ImGui.Text($"{moonRow.Unknown1} \n" +
                       $"{moonRow.Unknown2} \n" +
                       $"{moonRow.Unknown3} \n" +
                       $"{moonRow.Unknown4} \n" +
                       $"{moonRow.SilverStarRequirement} \n" +
                       $"{moonRow.GoldStarRequirement} \n" +
                       $"{moonRow.Unknown7} \n" +
                       $"{moonRow.Unknown8} \n" +
                       $"{moonRow.Unknown9} \n" +
                       $"{moonRow.Unknown10} \n" +
                       $"{moonRow.WKSMissionSupplyItem} \n" +
                       $"{moonRow.WKSMissionRecipe} \n" +
                       $"{moonRow.Unknown13} \n" +
                       $"{moonRow.Unknown14} \n" +
                       $"{moonRow.Unknown15} \n" +
                       $"{moonRow.Unknown16} \n" +
                       $"{moonRow.Unknown17} \n" +
                       $"{moonRow.Unknown18} \n" +
                       $"{moonRow.Unknown19} \n" +
                       $"{moonRow.Unknown20} \n");

            var toDoRow = ExcelHelper.ToDoSheet.GetRow(168);

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
            var moonItemRow = ExcelHelper.MoonItemInfoSheet.GetRow(523);

            ImGui.Text($"  WKS Item Info\n" +
                       $"{moonItemRow.Item}\n" +
                       $"{moonItemRow.Unknown1}\n" +
                       $"{moonItemRow.Unknown2}\n" +
                       $"{moonItemRow.WKSItemSubCategory}\n");

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("IPC Testing"))
        {
            ImGui.Text($"Artisan Is Busy? {P.Artisan.IsBusy()}");
            ImGui.Text($"{EzThrottler.GetRemainingTime("[Main Item(s)] Starting Main Craft")}");
            if (ImGui.Button("Artisan, craft this"))
            {
                P.Artisan.CraftItem(36026, 1);
            }

            ImGui.SetNextItemWidth(125);
            ImGui.InputInt("Radius", ref Radius);
            ImGui.SetNextItemWidth(125);
            ImGui.InputInt("X Location", ref XLoc);
            ImGui.SetNextItemWidth(125);
            ImGui.InputInt("Y Location", ref YLoc);

            if (ImGui.Button($"Test Radius"))
            {
                var agent = AgentMap.Instance();

                Utils.SetGatheringRing(agent->CurrentTerritoryId, XLoc, YLoc, Radius);
            }

            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Map test"))
        {
            ImGui.InputInt("TableId", ref TableRow);

            var MapInfo = ExcelHelper.MarkerSheet;

            if (ImGui.Button($"Test Radius"))
            {
                var agent = AgentMap.Instance();

                int _x = MapInfo.GetRow((uint)TableRow).Unknown1.ToInt() - 1024;
                int _y = MapInfo.GetRow((uint)TableRow).Unknown2.ToInt() - 1024;
                int _radius = MapInfo.GetRow((uint)TableRow).Unknown3.ToInt();
                IceLogging.Debug($"X: {_x} Y: {_y} Radius: {_radius}");

                Utils.SetGatheringRing(1237, _x, _y, _radius);
            }
            ImGui.SetNextItemWidth(125);
            ImGui.InputInt("Map X (Sheet)", ref posX);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            ImGui.InputInt("Map Y (Sheet)", ref posY);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(125);
            ImGui.InputInt("Map Radius", ref posRadius);
            if (ImGui.Button($"Test Map Marker from coords"))
            {
                var agent = AgentMap.Instance();
                int _x = posX - 1024;
                int _y = posY - 1024;
                IceLogging.Debug($"X: {_x} Y: {_y}");

                Utils.SetGatheringRing(agent->CurrentTerritoryId, _x, _y, posRadius);
            }
        }
    }

    private unsafe void Table()
    {
        var itemSheet = ExcelHelper.ItemSheet;
        ImGui.SetNextItemWidth(250);
        ImGui.InputText("Search by Name", ref CraftingTableSearchText, 100);

        if (ImGui.BeginTable("Mission Info List", 17, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, -1);
            ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, -1);
            ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("2nd Job", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Rank", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("ToDo ID", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("RecipeID", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Silver", ImGuiTableColumnFlags.WidthFixed, -1);
            ImGui.TableSetupColumn("Gold", ImGuiTableColumnFlags.WidthFixed, -1);
            ImGui.TableSetupColumn("Attribute Flags", ImGuiTableColumnFlags.WidthFixed, -1);
            //ImGui.TableSetupColumn("Exp Type 1###MissionExpType1", ImGuiTableColumnFlags.WidthFixed, 100);
            //ImGui.TableSetupColumn("Exp Amount 1###MissionExpAmount1", ImGuiTableColumnFlags.WidthFixed, 100);
            //ImGui.TableSetupColumn("Exp Type 2###MissionExpType2", ImGuiTableColumnFlags.WidthFixed, 100);
            //ImGui.TableSetupColumn("Exp Amount 2###MissionExpAmount2", ImGuiTableColumnFlags.WidthFixed, 100);
            //ImGui.TableSetupColumn("Exp Type 3###MissionExpType3", ImGuiTableColumnFlags.WidthFixed, 100);
            //ImGui.TableSetupColumn("Exp Amount 3###MissionExpAmount3", ImGuiTableColumnFlags.WidthFixed, 100);

            IOrderedEnumerable<KeyValuePair<int, string>> orderedExp = ExpDictionary.ToList().OrderBy(exp => exp.Key);
            var agent = AgentMap.Instance();
            var wk = WKSManager.Instance();

            //_gatherCenter = new(marker.Unknown1 - 1024, marker.Unknown2 - 1024);
            //_gatherRadius = marker.Unknown3;

            foreach (var exp in orderedExp)
            {
                ImGui.TableSetupColumn($"{exp.Value}", ImGuiTableColumnFlags.WidthFixed, -1);
            }

            ImGui.TableSetupColumn("Test Flag", ImGuiTableColumnFlags.WidthFixed, -1);

            ImGui.TableHeadersRow();

            var missionList = MissionInfoDict.Where(mission => mission.Value.Name.ToLower().Contains(CraftingTableSearchText.ToLower()));

            foreach (var entry in missionList)
            {
                ImGui.TableNextRow();

                // Mission ID
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{entry.Key}");

                // Mission Name
                ImGui.TableNextColumn();
                ImGui.Text(entry.Value.Name);

                // JobId Attached to it
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.JobId}");

                // 2nd Job for quest
                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.JobId2}");

                // Rank of the mission
                ImGui.TableNextColumn();
                string rank = "";
                if (entry.Value.Rank == 1)
                    rank = "D";
                else if (entry.Value.Rank == 2)
                    rank = "C";
                else if (entry.Value.Rank == 3)
                    rank = "B";
                else if (entry.Value.Rank == 4)
                    rank = "A-1";
                else if (entry.Value.Rank == 5)
                    rank = "A-2";
                else if (entry.Value.Rank == 6)
                    rank = "A-3";
                else
                {
                    rank = entry.Value.Rank.ToString();
                }
                ImGui.Text($"{rank}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.ToDoSlot}");

                ImGui.TableNextColumn();
                var RecipeSearch = entry.Value.RecipeId;
                ImGui.Text($"{RecipeSearch}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.SilverRequirement}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.GoldRequirement}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.Value.Attributes}");

                foreach (var expType in orderedExp)
                {
                    var relicXp = entry.Value.ExperienceRewards.Where(exp => exp.Type == expType.Key).FirstOrDefault().Amount.ToString();
                    if (relicXp == "0")
                    {
                        relicXp = "-";
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text($"{relicXp}");
                }

                ImGui.TableNextColumn();
                if (entry.Value.MarkerId != 0)
                {
                    if (ImGui.Button($"Flag###Flag-{entry.Key}"))
                    {
                        Utils.SetGatheringRing(entry.Value.TerritoryId, entry.Value.X, entry.Value.Y, entry.Value.Radius);
                    }
                }
            }

            ImGui.EndTable();
        }
    }

    private void Table2()
    {
        ImGui.SetNextItemWidth(250);
        ImGui.InputText("Search by Name", ref RecipeTableSearchText, 100);

        if (ImGui.BeginTable("Mission Info List", 9, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Mission Name", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Bool", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Pre-Craft 1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Pre-Craft 2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Pre-Craft 3", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("MainCraft 1", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("MainCraft 2", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("MainCraft 3", ImGuiTableColumnFlags.WidthFixed, 100);

            ImGui.TableHeadersRow();

            var recipeList = MoonRecipies.Where(recipe => MissionInfoDict.First(x => x.Key == recipe.Key).Value.Name.ToLower().Contains(RecipeTableSearchText.ToLower()));
            foreach (var entry in recipeList)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);

                ImGui.Text($"{entry.Key}");

                ImGui.TableNextColumn();
                var missionName = MissionInfoDict.First(x => x.Key == entry.Key).Value.Name;
                ImGui.Text($"{missionName}");

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

                ImGui.TableSetColumnIndex(5);
                foreach (var main in entry.Value.MainCraftsDict)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text($"Recipe: {main.Key} | Amount: {main.Value}");
                }
            }

            ImGui.EndTable();
        }
    }

    private void Table3()
    {
        var itemName = ExcelHelper.ItemSheet;

        if (ImGui.BeginTable("Gathering Mission Dictionary", 10, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("MissionId");
            ImGui.TableSetupColumn("Mission Name");
            ImGui.TableSetupColumn("Flag Location");
            ImGui.TableSetupColumn("Nodeset");
            ImGui.TableSetupColumn("Item 1");
            ImGui.TableSetupColumn("Item Amount###Item1Amount");
            ImGui.TableSetupColumn("Item 2");
            ImGui.TableSetupColumn("Item Amount###Item2Amount");
            ImGui.TableSetupColumn("Item 3");
            ImGui.TableSetupColumn("Item Amount###Item3Amount");

            ImGui.TableHeadersRow();

            foreach (var entry in GatheringItemDict)
            {
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{entry.Key}");

                var mission = MissionInfoDict[entry.Key];
                ImGui.TableNextColumn();
                ImGui.Text($"{mission.Name}");

                ImGui.TableNextColumn();
                ImGui.Text($"{mission.X}, {mission.Y}");
                if (ImGui.IsItemHovered() && ImGui.IsItemClicked())
                {
                    ImGui.SetClipboardText($"new Vector2({mission.X}, {mission.Y}), ");
                }

                ImGui.TableNextColumn();
                ImGui.Text($"Nodeset: {mission.NodeSet}");

                foreach (var item in entry.Value.MinGatherItems)
                {
                    ImGui.TableNextColumn();
                    string name = itemName.GetRow(item.Key).Name.ToString();
                    ImGui.Text(name);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"{item.Key}");
                        ImGui.EndTooltip();
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text($"{item.Value}");
                }
            }

            ImGui.EndTable();
        }
    }

    public static void ExportMissionInfoToCsv(Dictionary<uint, MissionListInfo> dict, string filePath)
    {
        using (var writer = new StreamWriter(filePath))
        {
            // Write the header
            writer.Write("Key,Name,JobId,JobId2,JobId3,ToDoSlot,Rank,RecipeId,SilverRequirement,GoldRequirement,CosmoCredit,LunarCredit");

            // Find the max number of ExperienceRewards across all missions
            int maxRewards = dict.Values.Max(info => info.ExperienceRewards?.Count ?? 0);

            // Add headers for each possible reward
            for (int i = 1; i <= maxRewards; i++)
            {
                writer.Write($",Type{i},Amount{i}");
            }

            writer.WriteLine(); // End header line

            foreach (var kvp in dict)
            {
                var info = kvp.Value;

                // Escape name if needed
                string safeName = info.Name.Contains(",") ? $"\"{info.Name}\"" : info.Name;

                writer.Write($"{kvp.Key},{safeName},{info.JobId},{info.JobId2},{info.JobId3},{info.ToDoSlot},{info.Rank},{info.RecipeId},{info.SilverRequirement},{info.GoldRequirement},{info.CosmoCredit},{info.LunarCredit}");

                if (info.ExperienceRewards != null)
                {
                    foreach (var reward in info.ExperienceRewards)
                    {
                        writer.Write($",{reward.Type},{reward.Amount}");
                    }
                }

                // Fill missing cells if this mission has fewer rewards
                int rewardCount = info.ExperienceRewards?.Count ?? 0;
                for (int i = rewardCount; i < maxRewards; i++)
                {
                    writer.Write(",,");

                }

                writer.WriteLine();
            }
        }
    }

    private static float ConvertWorldCoordXzToMapCoord(float value, uint scale, int offset)
    {
        return (0.02f * offset) + (2048f / scale) + (0.02f * value) + 1f;
    }
}
