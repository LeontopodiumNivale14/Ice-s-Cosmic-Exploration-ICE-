using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Common.Component.Excel;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Dalamud.Interface.Utility.Raii.ImRaii;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Tasks
{
    internal static class TaskCrafting
    {
        private static ExcelSheet<Item>? ItemSheet;
        private static ExcelSheet<Recipe>? RecipeSheet;

        public static void TryEnqueueCrafts()
        {
            EnsureInit();
            if (CurrentLunarMission != 0)
                MakeCraftingTasks();
        }

        private static void EnsureInit()
        {
            ItemSheet ??= Svc.Data.GetExcelSheet<Item>(); // Only need to grab once, it won't change
            RecipeSheet ??= Svc.Data.GetExcelSheet<Recipe>(); // Only need to grab once, it won't change
        }

        internal static bool IsArtisanBusy()
        {
            if (!P.Artisan.IsBusy() && !P.Artisan.GetEnduranceStatus())
            {
                return true;
            }
            else
            {
                if (EzThrottler.Throttle("Waiting for Artisan to not be busy"))
                    PluginLog.Debug("Waiting for Artisan to not be busy");
            }

            return false;
        }

        internal static void MakeCraftingTasks()
        {
            EnsureInit();
            var (currentScore, silverScore, goldScore) = GetCurrentScores();

            if (currentScore == 0 && silverScore == 0 && goldScore == 0)
            {
                PluginLog.Error("Failed to get scores on first attempt retrying");
                (currentScore, silverScore, goldScore) = GetCurrentScores();
                if (currentScore == 0 && silverScore == 0 && goldScore == 0)
                {
                    PluginLog.Error("Failed to get scores on second attempt retrying");
                    (currentScore, silverScore, goldScore) = GetCurrentScores();
                    if (currentScore == 0 && silverScore == 0 && goldScore == 0)
                    {
                        PluginLog.Error("Failed to get scores on third attempt aborting");
                        SchedulerMain.State = IceState.Idle;
                        return;
                    }
                }
            }

            if (currentScore >= goldScore)
            {
                PluginLog.Error("We shouldn't be here, stopping and progressing");
                SchedulerMain.State = IceState.CheckScoreAndTurnIn;
                return;
            }


            if (!P.TaskManager.IsBusy) // ensure no pending tasks or manual craft while plogon enabled
            {
                SchedulerMain.State = IceState.CraftInProcess;

                OpenStellaMission();

                var needPreCraft = false;
                var itemsToCraft = new Dictionary<ushort, Tuple<int, int>>();
                var preItemsToCraft = new Dictionary<ushort, Tuple<int, int>>();

                foreach (var main in MoonRecipies[CurrentLunarMission].MainCraftsDict)
                {
                    var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                    var subItem = RecipeSheet.GetRow(main.Key).Ingredient[0].Value.RowId; // need to directly reference this in the future
                    var mainNeed = main.Value;
                    var subItemNeed = RecipeSheet.GetRow(main.Key).AmountIngredient[0].ToInt() * main.Value;
                    var currentAmount = GetItemCount((int)itemId);
                    var currentSubItemAmount = GetItemCount((int)subItem);
                    var mainItemName = ItemSheet.GetRow(itemId).Name.ToString();

                    PluginDebug($"RecipeID: {main.Key}");
                    PluginDebug($"ItemID: {itemId}");

                    PluginDebug($"[Main Item(s)] Main ItemID: {itemId} [{mainItemName}] | Current Amount: {currentAmount} | RecipeId {main.Key}");
                    PluginDebug($"[Main Item(s)] Required Items for Recipe: ItemID: {subItem} | Currently have: {currentSubItemAmount} | Amount Needed [Base]: {subItemNeed}");
                    if (currentAmount == mainNeed || C.CraftMultipleMissionItems)
                    {
                        subItemNeed = subItemNeed * 2;
                        mainNeed = mainNeed * 2;
                    }

                    if (currentAmount < mainNeed)
                    {
                        subItemNeed = subItemNeed - currentAmount;

                        PluginDebug($"[Main Item(s)] You currently don't have the required amount of item: {ItemSheet.GetRow(itemId).Name}]. Checking to see if you have enough pre-crafts");
                        if (currentSubItemAmount >= subItemNeed)
                        {
                            PluginDebug($"[Main Item(s) You have the required amount to make the necessary amount of main items. Continuing on");
                            int craftAmount = mainNeed - currentAmount;
                            itemsToCraft.Add(main.Key, new(craftAmount, mainNeed));
                        }
                        else
                        {
                            int craftAmount = mainNeed - currentAmount;
                            itemsToCraft.Add(main.Key, new(craftAmount, mainNeed));
                            needPreCraft = true;
                        }
                    }
                }

                if (needPreCraft)
                {
                    PluginDebug($"[Pre-craft Items] You need pre-craft items. Starting the process of finding pre-crafts");
                    foreach (var pre in MoonRecipies[CurrentLunarMission].PreCraftDict)
                    {
                        var itemId = RecipeSheet.GetRow(pre.Key).ItemResult.Value.RowId;
                        var currentAmount = GetItemCount((int)itemId);
                        var PreCraftItemName = ItemSheet.GetRow(itemId).Name.ToString();
                        PluginDebug($"[Pre-Crafts] Checking Pre-crafts to see if {itemId} [{PreCraftItemName}] has enough.");
                        PluginDebug($"[Pre-Crafts] Item Amount: {currentAmount} | Goal Amount: {pre.Value} | RecipeId: {pre.Key}");
                        var goalAmount = pre.Value;

                        PluginDebug($"[Pre-Crafts] Craft x 2 items state: {C.CraftMultipleMissionItems}");
                        if (C.CraftMultipleMissionItems)
                        {
                            goalAmount = pre.Value * 2;
                        }

                        if (currentAmount < goalAmount)
                        {
                            PluginDebug($"[Pre-Crafts] Found an item that needs to be crafted: {itemId} | Item Name: {PreCraftItemName}");
                            int craftAmount = goalAmount - currentAmount;
                            preItemsToCraft.Add(pre.Key, new(craftAmount, goalAmount));
                        }
                    }
                }

                P.TaskManager.BeginStack(); // Enable stack mode


                P.TaskManager.Enqueue(() => SchedulerMain.State = IceState.WaitForCrafts, "Change state to wait for crafts");

                if (preItemsToCraft.Count > 0)
                {
                    PluginDebug("Queuing up pre-craft items");
                    foreach (var pre in preItemsToCraft)
                    {
                        var item = ItemSheet.GetRow(RecipeSheet.GetRow(pre.Key).ItemResult.RowId);
                        P.TaskManager.InsertDelay(1000); // Delay between pre item tasks
                        P.TaskManager.Enqueue(() => Craft(pre.Key, pre.Value.Item1, item), "PreCraft item");
                        P.TaskManager.Enqueue(() => WaitTillActuallyDone(pre.Key, pre.Value.Item2, item), "Wait for item", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration()
                        {
                            TimeLimitMS = 240000, // 4 minute limit per craft
                        });
                    }
                }

                if (itemsToCraft.Count > 0)
                {
                    PluginDebug("Queuing up main craft items");
                    foreach (var main in itemsToCraft)
                    {
                        var item = ItemSheet.GetRow(RecipeSheet.GetRow(main.Key).ItemResult.RowId);
                        PluginDebug($"[Main Item(s)] Queueing up for {item.Name}");
                        P.TaskManager.InsertDelay(1000); // Delay between main item tasks
                        P.TaskManager.Enqueue(() => Craft(main.Key, main.Value.Item1, item), "Craft item");
                        P.TaskManager.Enqueue(() => WaitTillActuallyDone(main.Key, main.Value.Item2, item), "Wait for item", new ECommons.Automation.NeoTaskManager.TaskManagerConfiguration()
                        {
                            TimeLimitMS = 240000, // 4 minute limit per craft, maybe need to work out a reasonable time? experts more? maybe 1m 30s per item?
                        });
                    }
                }

                P.TaskManager.Enqueue(() =>
                {
                    PluginLog.Debug("Check score and turn in cause crafting is done.");
                    SchedulerMain.State = IceState.CheckScoreAndTurnIn;
                }, "Check score and turn in if complete");

                P.TaskManager.EnqueueStack();
            }
        }

        internal static (uint currentScore, uint silverScore, uint goldScore) GetCurrentScores()
        {
            EnsureInit();
            if (TryGetAddonMaster<WKSMissionInfomation>("WKSMissionInfomation", out var z) && z.IsAddonReady)
            {
                var goldScore = MissionInfoDict[CurrentLunarMission].GoldRequirement;
                var silverScore = MissionInfoDict[CurrentLunarMission].SilverRequirement;

                string currentScoreText = GetNodeText("WKSMissionInfomation", 27);
                currentScoreText = currentScoreText.Replace(",", ""); // English client comma's
                currentScoreText = currentScoreText.Replace(" ", ""); // French client spacing
                currentScoreText = currentScoreText.Replace(".", ""); // French client spacing
                if (uint.TryParse(currentScoreText, out uint tempScore))
                {
                    return (tempScore, silverScore, goldScore);
                }
                else
                {
                    return (0, silverScore, goldScore);
                }
            }

            return (0, 0, 0);
        }

        internal static void Craft(ushort id, int craftAmount, Item item)
        {
            PluginDebug($"[Main Item(s)] Telling Artisan to use recipe: {id} | {craftAmount} for {item.Name}");
            P.Artisan.CraftItem(id, craftAmount);
        }

        internal static bool? WaitTillActuallyDone(ushort id, int craftAmount, Item item)
        {
            var (currentScore, silverScore, goldScore) = GetCurrentScores(); // some scoring checks

            if (C.TurninOnSilver && currentScore >= silverScore)
            {
                P.Artisan.SetEnduranceStatus(false);
                return true;
            }
            else if (currentScore >= goldScore)
            {
                P.Artisan.SetEnduranceStatus(false);
                return true;
            }


            if (GetItemCount((int)item.RowId) != craftAmount)
            {
                if (P.Artisan.GetEnduranceStatus() == false && Svc.Condition[ConditionFlag.PreparingToCraft] && GetItemCount((int)item.RowId) != craftAmount)
                {
                    PluginLog.Error("Endurance is off, we are not doing anything but not complete?");
                    return true;
                }

                if (LogThrottle)
                {
                    PluginLog.Debug("Waiting for Artisan to finish crafting");
                    PluginLog.Debug("Returning false");
                }
                return false;
            }


            PluginLog.Debug("Returning true");
            return true;
        }

        internal static bool? WaitingForCrafting()
        {
            if (Svc.Condition[ConditionFlag.NormalConditions])
            {
                return true;
            }

            return false;
        }

        internal static bool HaveEnoughMain()
        {
            EnsureInit();
            if (LogThrottle)
            {
                PluginLog.Debug($"[Item(s) Check] Checking.");
            }

            foreach (var main in MoonRecipies[CurrentLunarMission].MainCraftsDict)
            {
                var itemId = RecipeSheet.GetRow(main.Key).ItemResult.Value.RowId;
                var mainNeed = main.Value;
                var currentAmount = GetItemCount((int)itemId);
                var mainItemName = ItemSheet.GetRow(itemId).Name.ToString();

                PluginLog.Debug($"[Item(s) Check] Curr: {currentAmount} - Need: {mainNeed}");
                if (currentAmount < mainNeed)
                {
                    if (LogThrottle)
                    {
                        PluginLog.Debug($"[Item(s) Check] You currently don't have the required amount of item: {ItemSheet.GetRow(itemId).Name}.");
                    }
                    return false;
                }
            }

            if (LogThrottle)
            {
                PluginLog.Debug($"[Item(s) Check] You currently have the required amount of items.");
            }
            return true;
        }
    }
}
