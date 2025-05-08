using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using ICE.Enums;
using Lumina.Excel.Sheets;
using static ICE.Utilities.CosmicHelper;

namespace ICE;

public sealed partial class ICE
{
    public static unsafe void DictionaryCreation()
    {
        MoonRecipies = [];
        Svc.Data.GameData.Options.PanicOnSheetChecksumMismatch = false;

        var MoonMissionSheet = Svc.Data.GetExcelSheet<WKSMissionUnit>();
        var MoonRecipeSheet = Svc.Data.GetExcelSheet<WKSMissionRecipe>();
        var RecipeSheet = Svc.Data.GetExcelSheet<Recipe>();
        var ItemSheet = Svc.Data.GetExcelSheet<Item>();
        var ExpSheet = Svc.Data.GetExcelSheet<WKSMissionReward>();
        var ToDoSheet = Svc.Data.GetExcelSheet<WKSMissionToDo>();
        var MoonItemInfo = Svc.Data.GetExcelSheet<WKSItemInfo>();
        var MarkerSheet = Svc.Data.GetExcelSheet<WKSMissionMapMarker>();

        var wk = WKSManager.Instance();

        foreach (var item in MoonMissionSheet)
        {
            List<(int Type, int Amount)> Exp = [];
            Dictionary<ushort, int> MainItems = [];
            Dictionary<ushort, int> PreCrafts = [];
            uint keyId = item.RowId;
            string LeveName = item.Item.ToString();
            LeveName = LeveName.Replace("<nbsp>", " ");
            LeveName = LeveName.Replace("<->", "");

            if (LeveName == "")
                continue;

            int JobId = item.Unknown1 - 1;
            int Job2 = item.Unknown2;
            if (item.Unknown2 != 0)
            {
                Job2 = Job2 - 1;
            }

            uint silver = item.SilverStarRequirement;
            uint gold = item.GoldStarRequirement;
            uint previousMissionId = item.Unknown10;

            uint timeAndWeather = item.Unknown18;
            uint time = 0;
            CosmicWeather weather = CosmicWeather.FairSkies;
            if (timeAndWeather <= 12)
            {
                time = timeAndWeather;
            }
            else
            {
                weather = (CosmicWeather)(timeAndWeather - 12);
            }

            uint rank = item.Unknown17;
            bool isCritical = item.Unknown20;

            uint RecipeId = item.WKSMissionRecipe;

            uint toDoValue = item.Unknown7;

            var todo = ToDoSheet.GetRow((uint)(item.Unknown7 + *((byte*)wk + 0xC62)));
            var marker = MarkerSheet.GetRow(todo.Unknown13);

            Vector2 Pos = new Vector2(marker.Unknown1 - 1024, marker.Unknown2 - 1024);
            ushort Radius = marker.Unknown3;

            if (CrafterJobList.Contains(JobId))
            {
                bool preCraftsbool = false;

                var toDoRow = ToDoSheet.GetRow(toDoValue);
                if (toDoRow.Unknown3 != 0) // shouldn't be 0, 1st item entry
                {
                    var item1Amount = toDoRow.Unknown6;
                    var item1Id = MoonItemInfo.GetRow(toDoRow.Unknown3).Item;
                    var item1Name = ItemSheet.GetRow(item1Id).Name.ToString();
                    var item1RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item1Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[3].Value.RowId ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[4].Value.RowId)
                                                    .First();
                    var craftingType = item1RecipeRow.CraftType.Value.RowId;
                    IceLogging.Debug($"Recipe Row ID: {item1RecipeRow.RowId} | for item: {item1Id}", true);
                    for (var i = 0; i <= 5; i++)
                    {
                        var subitem = item1RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                            IceLogging.Debug($"subItemId: {subitem} slot [{i}]", true);

                        if (subitem != 0)
                        {
                            var subitemRecipe = RecipeSheet.Where(x => x.ItemResult.RowId == subitem)
                                                           .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[3].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[4].Value.RowId)
                                                           .FirstOrDefault();
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = item1RecipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * item1Amount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }
                    var item1RecipeId = item1RecipeRow.RowId;
                    MainItems.Add(((ushort)item1RecipeId), item1Amount);
                }
                if (toDoRow.Unknown4 != 0) // 2nd item entry
                {
                    var item2Amount = toDoRow.Unknown7;
                    var item2Id = MoonItemInfo.GetRow(toDoRow.Unknown4).Item;
                    var item2Name = ItemSheet.GetRow(item2Id).Name.ToString();

                    var item2RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item2Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[3].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[4].Value.RowId)
                                                    .First();
                    IceLogging.Debug($"Recipe Row ID: {item2RecipeRow.RowId} | for item: {item2Id}", true);
                    for (var i = 0; i <= 5; i++)
                    {
                        var subitem = item2RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                            IceLogging.Debug($"subItemId: {subitem} slot [{i}]", true);

                        if (subitem != 0)
                        {
                            var subitemRecipe = RecipeSheet.Where(e => e.ItemResult.RowId == item2Id)
                                                           .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[3].Value.RowId ||
                                                                  e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[4].Value.RowId)
                                                           .First();
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = item2RecipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * item2Amount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }
                    var item2RecipeId = item2RecipeRow.RowId;
                    MainItems.Add(((ushort)item2RecipeId), item2Amount);
                }
                if (toDoRow.Unknown5 != 0) // 3rd item entry
                {
                    var item3Amount = toDoRow.Unknown8;
                    var item3Id = MoonItemInfo.GetRow(toDoRow.Unknown5).Item;
                    var item3Name = ItemSheet.GetRow(item3Id).Name.ToString();

                    var item3RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item3Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[0].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[1].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[2].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[3].Value.RowId ||
                                                           e.RowId == MoonRecipeSheet.GetRow(RecipeId).Recipe[4].Value.RowId)
                                                    .First();
                    IceLogging.Debug($"Recipe Row ID: {item3RecipeRow.RowId} | for item: {item3Id}", true);
                    for (var i = 0; i <= 5; i++)
                    {
                        var subitem = item3RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                            IceLogging.Debug($"subItemId: {subitem} slot [{i}]", true);

                        if (subitem != 0)
                        {
                            var subitemRecipe = RecipeSheet.FirstOrDefault(x => x.ItemResult.RowId == subitem);
                            if (subitemRecipe.RowId != 0)
                            {
                                var subItemAmount = item3RecipeRow.AmountIngredient[i].ToInt();
                                subItemAmount = subItemAmount * item3Amount;
                                PreCrafts.Add(((ushort)subitemRecipe.RowId), subItemAmount);
                                preCraftsbool = true;
                            }
                        }
                    }
                    var item3RecipeId = item3RecipeRow.RowId;
                    MainItems.Add(((ushort)item3RecipeId), item3Amount);
                }

                if (preCraftsbool)
                {
                    foreach (var preItem in PreCrafts)
                    {
                        if (MainItems.ContainsKey(preItem.Key))
                            PreCrafts.Remove(preItem.Key);
                    }

                    if (PreCrafts.Count == 0)
                    {
                        preCraftsbool = false;
                    }
                }

                if (!MoonRecipies.ContainsKey(keyId))
                {
                    MoonRecipies[keyId] = new MoonRecipieInfo()
                    {
                        MainCraftsDict = MainItems,
                        PreCraftDict = PreCrafts,
                        PreCrafts = preCraftsbool
                    };
                }

            }

            // Col 3 -> Cosmocredits - Unknown 0
            // Col 4 -> Lunar Credits - Unknown 1
            // Col 7 ->  Lv. 1 Type - Unknown 12
            // Col 8 ->  Lv. 1 Exp - Unknown 2
            // Col 10 -> Lv. 2 Type - Unknown 13
            // Col 11 -> Lv. 2 Exp - Unknown 3
            // Col 13 -> Lv. 3 Type - Unknown 14
            // Col 14 -> Lv. 3 Exp - Unknown 4

            uint Cosmo = ExpSheet.GetRow(keyId).Unknown0;
            uint Lunar = ExpSheet.GetRow(keyId).Unknown1;

            if (ExpSheet.GetRow(keyId).Unknown2 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown12, ExpSheet.GetRow(keyId).Unknown2));
            }
            if (ExpSheet.GetRow(keyId).Unknown3 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown13, ExpSheet.GetRow(keyId).Unknown3));
            }
            if (ExpSheet.GetRow(keyId).Unknown4 != 0)
            {
                Exp.Add((ExpSheet.GetRow(keyId).Unknown14, ExpSheet.GetRow(keyId).Unknown4));
            }

            if (!MissionInfoDict.ContainsKey(keyId))
            {
                MissionInfoDict[keyId] = new MissionListInfo()
                {
                    Name = LeveName,
                    JobId = ((uint)JobId),
                    JobId2 = ((uint)Job2),
                    ToDoSlot = toDoValue,
                    Rank = rank,
                    IsCriticalMission = isCritical,
                    Time = time,
                    Weather = weather,
                    RecipeId = RecipeId,
                    SilverRequirement = silver,
                    GoldRequirement = gold,
                    CosmoCredit = Cosmo,
                    LunarCredit = Lunar,
                    ExperienceRewards = Exp,
                    PreviousMissionID = previousMissionId,
                    MarkerId = marker.RowId,
                    X = Pos.X,
                    Y = Pos.X,
                    Radius = Radius,
                };
            }
        }

        if (C.Missions.Count == 0)
        {
            // fresh install?
            C.Missions = [.. MissionInfoDict.Select(x => new CosmicMission()
        {
            Id = x.Key,
            Name = x.Value.Name,
            Type = GetMissionType(x.Value),
            PreviousMissionId = x.Value.PreviousMissionID,
            JobId = x.Value.JobId,
        })];
            C.Save();
        }
        else
        {
            var newMissions = MissionInfoDict.Where(x => !C.Missions.Any(y => y.Id == x.Key)).Select(x => new CosmicMission()
            {
                Id = x.Key,
                Name = x.Value.Name,
                Type = GetMissionType(x.Value),
                PreviousMissionId = x.Value.PreviousMissionID,
                JobId = x.Value.JobId,
            });

            if (newMissions.Any())
            {
                C.Missions.AddRange(newMissions);
                C.Save();
            }
        }
    }
    private static MissionType GetMissionType(MissionListInfo mission)
    {
        if (mission.IsCriticalMission)
        {
            return MissionType.Critical;
        }
        else if (mission.Time != 0)
        {
            return MissionType.Timed;
        }
        else if (mission.Weather != CosmicWeather.FairSkies)
        {
            return MissionType.Weather;
        }
        else if (mission.PreviousMissionID != 0)
        {
            return MissionType.Sequential;
        }

        return MissionType.Standard;
    }
}
