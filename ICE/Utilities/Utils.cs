using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Textures;
using ECommons;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices.Legacy;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.Logging;
using ECommons.Reflection;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ICE.Enums;
using Lumina;
using Lumina.Excel.Sheets;
using System.Collections.Generic;
using static Dalamud.Game.Text.SeStringHandling.Payloads.ItemPayload;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Utilities;

public static unsafe class Utils
{
    public const string ExecuteCommandSignature = "E8 ?? ?? ?? ?? 8D 46 0A";
    internal delegate nint ExecuteCommandDelegate(int command, int a1 = 0, int a2 = 0, int a3 = 0, int a4 = 0);
    internal static ExecuteCommandDelegate? ExecuteCommand = EzDelegate.Get<ExecuteCommandDelegate>(ExecuteCommandSignature);
    [EzHook(ExecuteCommandSignature, false)]
    internal static readonly EzHook<ExecuteCommandDelegate> ExecuteCommandHook = null!;

    #region Plugin/Ecoms stuff

    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    internal static bool GenericThrottle => FrameThrottler.Throttle("AutoRetainerGenericThrottle", 10);
    internal static bool LogThrottle => FrameThrottler.Throttle("AutoRetainerGenericThrottle", 2000);
    public static TaskManagerConfiguration DConfig => new(timeLimitMS: 10 * 60 * 3000, abortOnTimeout: false);

    public static void PluginVerbos(string message) => PluginLog.Verbose(message);
    public static void PluginInfo(string message) => PluginLog.Information(message);

    public static void PluginDebug(string message)
    {
        if (EzThrottler.Throttle(message, 1000))
            PluginLog.Debug(message);
    }

    public static void PluginWarning(string message)
    {
        if (EzThrottler.Throttle(message, 1000))
            PluginLog.Warning(message);
    }

    public static void OpenStellaMission()
    {
        if (TryGetAddonMaster<WKSHud>("WKSHud", out var hud) && hud.IsAddonReady && !IsAddonActive("WKSMissionInfomation"))
        {
            if (EzThrottler.Throttle("Opening Steller Missions"))
            {
                PluginLog.Debug("Opening Mission Menu");
                hud.Mission();
            }
        }
    }

    #endregion

    #region Player Information

    public static uint GetClassJobId() => Svc.ClientState.LocalPlayer!.ClassJob.RowId;
    public static unsafe int GetLevel(int expArrayIndex = -1)
    {
        if (expArrayIndex == -1) expArrayIndex = Svc.ClientState.LocalPlayer?.ClassJob.Value.ExpArrayIndex ?? 0;
        return UIState.Instance()->PlayerState.ClassJobLevels[expArrayIndex];
    }
    internal static unsafe short GetCurrentLevelFromSheet(Job? job = null)
    {
        PlayerState* playerState = PlayerState.Instance();
        return playerState->ClassJobLevels[Svc.Data.GetExcelSheet<ClassJob>().GetRowOrDefault((uint)(job ?? (Player.Available ? Player.Object.GetJob() : 0)))?.ExpArrayIndex ?? 0];
    }

    public static bool IsInZone(uint zoneID) => Svc.ClientState.TerritoryType == zoneID;
    public static uint CurrentTerritory() => GameMain.Instance()->CurrentTerritoryTypeId;

    public static bool IsBetweenAreas => Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51];

    public static bool PlayerNotBusy()
    {
        return Player.Available
               && Player.Object.CastActionId == 0
               && !IsOccupied()
               && !Player.IsJumping
               && Player.Object.IsTargetable
               && !Player.IsAnimationLocked;
    }

    public static unsafe bool HasStatusId(params uint[] statusIDs)
    {
        var statusID = Svc.ClientState.LocalPlayer!.StatusList
            .Select(se => se.StatusId)
            .ToList().Intersect(statusIDs)
            .FirstOrDefault();

        return statusID != default;
    }

    public static int GetGp()
    {
        var gp = Svc.ClientState.LocalPlayer?.CurrentGp ?? 0;
        return (int)gp;
    }

    internal static unsafe float GetDistanceToPlayer(Vector3 v3) => Vector3.Distance(v3, Player.GameObject->Position);
    internal static unsafe float GetDistanceToPlayer(IGameObject gameObject) => GetDistanceToPlayer(gameObject.Position);

    public static unsafe int GetItemCount(int itemID, bool includeHq = true)
    => includeHq ? InventoryManager.Instance()->GetInventoryItemCount((uint)itemID, true)
    + InventoryManager.Instance()->GetInventoryItemCount((uint)itemID) + InventoryManager.Instance()->GetInventoryItemCount((uint)itemID + 500_000)
    : InventoryManager.Instance()->GetInventoryItemCount((uint)itemID) + InventoryManager.Instance()->GetInventoryItemCount((uint)itemID + 500_000);

    public static Vector3 NavDestination = Vector3.Zero;

    public static unsafe uint CurrentLunarMission => WKSManager.Instance()->CurrentMissionUnitRowId;

    #endregion

    #region Target Information

    internal static bool? TargetgameObject(IGameObject? gameObject)
    {
        var x = gameObject;
        if (Svc.Targets.Target != null && Svc.Targets.Target.DataId == x.DataId)
            return true;

        if (!IsOccupied())
        {
            if (x != null)
            {
                if (EzThrottler.Throttle($"Throttle Targeting {x.DataId}"))
                {
                    Svc.Targets.SetTarget(x);
                    ECommons.Logging.PluginLog.Information($"Setting the target to {x.DataId}");
                }
            }
        }
        return false;
    }
    internal static bool TryGetObjectByDataId(ulong dataId, out IGameObject? gameObject) => (gameObject = Svc.Objects.OrderBy(GetDistanceToPlayer).FirstOrDefault(x => x.DataId == dataId)) != null;
    internal static unsafe void InteractWithObject(IGameObject? gameObject)
    {
        try
        {
            if (gameObject == null || !gameObject.IsTargetable)
                return;
            var gameObjectPointer = (GameObject*)gameObject.Address;
            TargetSystem.Instance()->InteractWithObject(gameObjectPointer, false);
        }
        catch (Exception ex)
        {
            Svc.Log.Info($"InteractWithObject: Exception: {ex}");
        }
    }

    #endregion

    #region Addon Information

    public static bool IsAddonActive(string AddonName) // Used to see if the addon is active/ready to be fired on
    {
        var addon = RaptureAtkUnitManager.Instance()->GetAddonByName(AddonName);
        return addon != null && addon->IsVisible && addon->IsReady;
    }

    public static unsafe bool IsNodeVisible(string addonName, params int[] ids)
    {
        var ptr = Svc.GameGui.GetAddonByName(addonName, 1);
        if (ptr == nint.Zero)
            return false;

        var addon = (AtkUnitBase*)ptr;
        var node = GetNodeByIDChain(addon->GetRootNode(), ids);
        return node != null && node->IsVisible();
    }

    public static unsafe string GetNodeText(string addonName, params int[] nodeNumbers)
    {

        var ptr = Svc.GameGui.GetAddonByName(addonName, 1);

        var addon = (AtkUnitBase*)ptr;
        var uld = addon->UldManager;

        AtkResNode* node = null;
        var debugString = string.Empty;
        for (var i = 0; i < nodeNumbers.Length; i++)
        {
            var nodeNumber = nodeNumbers[i];

            var count = uld.NodeListCount;

            node = uld.NodeList[nodeNumber];
            debugString += $"[{nodeNumber}]";

            // More nodes to traverse
            if (i < nodeNumbers.Length - 1)
            {
                uld = ((AtkComponentNode*)node)->Component->UldManager;
            }
        }

        if (node->Type == NodeType.Counter)
            return ((AtkCounterNode*)node)->NodeText.ToString();

        var textNode = (AtkTextNode*)node;
        return textNode->NodeText.GetText();
    }

    private static unsafe AtkResNode* GetNodeByIDChain(AtkResNode* node, params int[] ids)
    {
        if (node == null || ids.Length <= 0)
            return null;

        if (node->NodeId == ids[0])
        {
            if (ids.Length == 1)
                return node;

            var newList = new List<int>(ids);
            newList.RemoveAt(0);

            var childNode = node->ChildNode;
            if (childNode != null)
                return GetNodeByIDChain(childNode, [.. newList]);

            if ((int)node->Type >= 1000)
            {
                var componentNode = node->GetAsAtkComponentNode();
                var component = componentNode->Component;
                var uldManager = component->UldManager;
                childNode = uldManager.NodeList[0];
                return childNode == null ? null : GetNodeByIDChain(childNode, [.. newList]);
            }

            return null;
        }

        //check siblings
        var sibNode = node->PrevSiblingNode;
        return sibNode != null ? GetNodeByIDChain(sibNode, ids) : null;
    }

    #endregion

    #region LoadOnBoot

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

        foreach (var item in MoonMissionSheet)
        {
            List<(int Type, int Amount)> Exp = new List<(int Type, int Amount)>();
            Dictionary<ushort, int> MainItems = new Dictionary<ushort, int>();
            Dictionary<ushort, int> PreCrafts = new Dictionary<ushort, int>();
            uint keyId = item.RowId;
            string LeveName = item.Unknown0.ToString();
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

            uint silver = item.Unknown5;
            uint gold = item.Unknown6;

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

            uint RecipeId = item.Unknown12;

            uint toDoValue = item.Unknown7;
            if (CrafterJobList.Contains(JobId))
            {
                bool preCraftsbool = false;

                var toDoRow = ToDoSheet.GetRow(toDoValue);
                if (toDoRow.Unknown3 != 0) // shouldn't be 0, 1st item entry
                {
                    var item1Amount = toDoRow.Unknown6;
                    var item1Id = MoonItemInfo.GetRow(toDoRow.Unknown3).Unknown0;
                    var item1Name = ItemSheet.GetRow(item1Id).Name.ToString();
                    var item1RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item1Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown0 || 
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown1 || 
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown2 || 
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown3 || 
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown4)
                                                    .First();
                    var craftingType = item1RecipeRow.CraftType.Value.RowId;
                    PluginDebug($"Recipe Row ID: {item1RecipeRow.RowId} | for item: {item1Id}");
                    for (var i = 0; i <= 5; i++)
                    {
                        var subitem = item1RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                            PluginDebug($"subItemId: {subitem} slot [{i}]");

                        if (subitem != 0)
                        {
                            var subitemRecipe = RecipeSheet.Where(x => x.ItemResult.RowId == subitem)
                                                           .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown0 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown1 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown2 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown3 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown4)
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
                    var item2Id = MoonItemInfo.GetRow(toDoRow.Unknown4).Unknown0;
                    var item2Name = ItemSheet.GetRow(item2Id).Name.ToString();

                    var item2RecipeRow = RecipeSheet.FirstOrDefault(e => e.ItemResult.RowId == item2Id);
                    PluginDebug($"Recipe Row ID: {item2RecipeRow.RowId} | for item: {item2Id}");
                    for (var i = 0; i <= 5; i++)
                    {
                        var subitem = item2RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                            PluginDebug($"subItemId: {subitem} slot [{i}]");

                        if (subitem != 0)
                        {
                            var subitemRecipe = RecipeSheet.Where(e => e.ItemResult.RowId == item2Id)
                                                           .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown0 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown1 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown2 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown3 ||
                                                                       e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown4)
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
                    var item3Id = MoonItemInfo.GetRow(toDoRow.Unknown5).Unknown0;
                    var item3Name = ItemSheet.GetRow(item3Id).Name.ToString();

                    var item3RecipeRow = RecipeSheet.Where(e => e.ItemResult.RowId == item3Id)
                                                    .Where(e => e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown0 ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown1 ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown2 ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown3 ||
                                                                e.RowId == MoonRecipeSheet.GetRow(RecipeId).Unknown4)
                                                    .First();
                    PluginDebug($"Recipe Row ID: {item3RecipeRow.RowId} | for item: {item3Id}");
                    for (var i = 0; i <= 5; i++)
                    {
                        var subitem = item3RecipeRow.Ingredient[i].Value.RowId;
                        if (subitem != 0)
                            PluginDebug($"subItemId: {subitem} slot [{i}]");

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
                    ExperienceRewards = Exp
                };
            }
        }
        C.CriticalMissions = MissionInfoDict
                                .Where(m => m.Value.IsCriticalMission)
                                .Select(mission => (Id: mission.Key, Name: mission.Value.Name))
                                .ToList();
        C.TimedMissions = MissionInfoDict
                                .Where(m => m.Value.Time != 0)
                                .Select(mission => (Id: mission.Key, Name: mission.Value.Name))
                                .ToList();
        C.WeatherMissions = MissionInfoDict
                                .Where(m => m.Value.Weather != CosmicWeather.FairSkies)
                                .Where(m => !m.Value.IsCriticalMission)
                                .Select(mission => (Id: mission.Key, Name: mission.Value.Name))
                                .ToList();
        C.SequenceMissions = MissionInfoDict
                                .Where(m => SequentialMissions.Contains((int) m.Key))
                                .Select(mission => (Id: mission.Key, Name: mission.Value.Name))
                                .ToList();
        C.StandardMissions = MissionInfoDict
                                .Where(m => Ranks.Contains(m.Value.Rank) || ARankIds.Contains(m.Value.Rank))
                                .Where(m => !m.Value.IsCriticalMission)
                                .Where(m => m.Value.Time == 0)
                                .Where(m => m.Value.Weather == CosmicWeather.FairSkies)
                                .Select(mission => (Id: mission.Key, Name: mission.Value.Name))
                                .ToList();
        C.Save();
    }

    #endregion

    #region Useful Functions

    public static Vector3 RoundVector3(Vector3 v, int decimals)
    {
        return new Vector3(
            (float)Math.Round(v.X, decimals),
            (float)Math.Round(v.Y, decimals),
            (float)Math.Round(v.Z, decimals)
        );
    }


    #endregion

    #region Cosmic Exploration Display

    public static Dictionary<int, String> ExpDictionary = new Dictionary<int, String>
    {
        { 1, "I" },
        { 2, "II" },
        { 3, "III" },
        { 4, "IV" }
    };

    #endregion
}
