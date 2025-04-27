using Dalamud.Game.ClientState.Conditions;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskStartCrafting
    {
        public static void Enqueue()
        {
            if (P.artisan.GetEnduranceStatus() == true)
            {
                SetArtisanEndur(false);
            }
            P.taskManager.Enqueue(() => SetArtisanEndur(false), "Setting Artisan Endurance to false");
            P.taskManager.Enqueue(() => StartCrafting(), "Starting Crafting Process", DConfig);
            P.taskManager.Enqueue(() => CurrentLunarMission == 0);
        }

        internal unsafe static void SetArtisanEndur(bool enable)
        {
            P.artisan.SetEnduranceStatus(enable);
        }

        internal unsafe static bool? StartCrafting()
        {
            uint currentScore = 0;
            uint goldScore = 0;

            var itemSheet = Svc.Data.GetExcelSheet<Item>();

            if (P.artisan.GetEnduranceStatus() == false)
            {
                if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation"))
                {
                    if (EzThrottler.Throttle("Opening Steller Missions"))
                    {
                        PluginLog.Debug("Opening Mission Menu");
                        hud.Mission();
                    }
                }

                var RecipeSheet = Svc.Data.GetExcelSheet<Recipe>();
                var currentMission = CurrentLunarMission;
                bool foundPreCraft = false;
                bool allCrafted = true;

                PluginDebug($"Current Mission: {currentMission} | Found Pre-Craft? {foundPreCraft}");

                if (MoonRecipies[currentMission].PreCrafts)
                {
                    PluginDebug("Pre-crafts are part of the list, checking to see if any need crafting");
                    foreach (var pre in MoonRecipies[currentMission].PreCraftDict)
                    {
                        var itemId = RecipeSheet.GetRow(pre.Key).ItemResult.Value.RowId;
                        var currentAmount = GetItemCount((int)itemId);
                        PluginDebug($"Checking Pre-crafts to see if {itemId} has enough.");
                        PluginDebug($"Item Amount: {currentAmount} | Goal Amount: {pre.Value} | RecipeId: {pre.Key}");

                        if (currentAmount < pre.Value)
                        {
                            foundPreCraft = true; // <--- Mark that a pre-craft is needed!

                            if (EzThrottler.Throttle("Starting pre-craft", 4000))
                            {
                                PluginInfo($"Found an item that needs to be crafted: {itemId}");
                                int craftAmount = pre.Value - currentAmount;
                                P.artisan.CraftItem(pre.Key, craftAmount);
                            }
                            break; // <-- Important: break out after starting a pre-craft to avoid multiple crafts at once
                        }
                    }
                }

                if (!foundPreCraft)
                {
                    PluginDebug("No pre-crafts remaining! Crafting the main item");
                    foreach (var main in MoonRecipies[currentMission].MainCraftsDict)
                    {
                        var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                        var currentAmount = GetItemCount((int)itemId);
                        
                        PluginDebug($"[Main Item(s)] ItemId: {itemId} | Current Amount {currentAmount} | Amount Wanted: {main.Value} | RecipeId: {main.Key}");

                        if (currentAmount < main.Value)
                        {
                            if (EzThrottler.Throttle("Starting Main Craft", 4000))
                            {
                                int craftamount = main.Value - currentAmount;
                                PluginDebug($"[Main Item(s)] Telling Artisan to use recipe: {main.Key} | {craftamount}");
                                P.artisan.CraftItem(main.Key, main.Value);
                                allCrafted = false;
                                break;
                            }
                        }
                    }
                }



                if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady && allCrafted)
                {
                    uint tempScore = 0;
                    string currentScoreText = GetNodeText("WKSMissionInfomation", 27);
                    currentScoreText = currentScoreText.Replace(",", ""); // English client comma's
                    currentScoreText = currentScoreText.Replace(" ", ""); // French client spacing
                    currentScoreText = currentScoreText.Replace(".", ""); // French client spacing
                    if (uint.TryParse(currentScoreText, out tempScore))
                    {
                        currentScore = tempScore;
                    }
                    else
                    {
                        currentScore = 0;
                    }

                    goldScore = MissionInfoDict[CurrentLunarMission].GoldRequirement;

                    if (currentScore != 0)
                    {
                        PluginDebug("Score != 0");
                        if (PlayerNotBusy() && !Svc.Condition[ConditionFlag.PreparingToCraft])
                        {
                            if (EzThrottler.Throttle("Turning in item"))
                            {
                                if (C.Once && currentScore>=goldScore)
                                {
                                    PluginLog.Debug($"Turning in gold in another spot: {SchedulerMain.MissionName}");
                                    C.EnabledMission.RemoveAll(e => e.Name == SchedulerMain.MissionName);
                                    C.Save();
                                }
                                z.Report();
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}
