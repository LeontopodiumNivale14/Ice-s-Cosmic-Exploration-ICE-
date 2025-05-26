using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices.Legacy;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace ICE.Utilities;

/// <summary>
/// Misc unused (yet) or weird functions i didn't know where to put - Chika
/// </summary>
public static unsafe class Utils
{
    public static float enableColumnLength = 0.0f;
    public static float IDLength = 0.0f;
    public static float missionLength = 0.0f;
    public static float flagLength = 0.0f;
    public static float cosmicLength = 0.0f;
    public static float lunarLength = 0.0f;
    public static float XPLength = 0.0f;
    public static float TurninComboLength = 100.0f;
    public static float GatherConfigLength = 0.0f;
    public static float MissionNotesLength = 0.0f;

    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    public static TaskManagerConfiguration TaskConfig => new(timeLimitMS: 10 * 60 * 3000, abortOnTimeout: false);

    public static unsafe void SetFlagForNPC(uint territoryId, float x, float y)
    {
        var map = ExcelHelper.TerritorySheet.GetRow(territoryId).Map.Value;

        var agent = AgentMap.Instance();

        Vector2 pos = MapToWorld(new Vector2(x, y), map.SizeFactor, map.OffsetX, map.OffsetY);

        agent->IsFlagMarkerSet = false;
        agent->SetFlagMapMarker(territoryId, map.RowId, pos.X, pos.Y);
        agent->OpenMapByMapId(map.RowId, territoryId);
    }

    public static float MapToWorld(float value, uint scale, int offset) => -offset * (scale / 100.0f) + 50.0f * (value - 1) * (scale / 100.0f);

    public static Vector2 MapToWorld(Vector2 coordinates, ushort sizeFactor, short offsetX, short offsetY)
    {
        var scalar = sizeFactor / 100.0f;

        var xWorldCoord = MapToWorld(coordinates.X, sizeFactor, offsetX);
        var yWorldCoord = MapToWorld(coordinates.Y, sizeFactor, offsetY);

        var objectPosition = new Vector2(xWorldCoord, yWorldCoord);
        var center = new Vector2(1024.0f, 1024.0f);

        return objectPosition / scalar - center / scalar;
    }

    public static bool? TargetgameObject(IGameObject? gameObject)
    {
        var x = gameObject;
        if (Svc.Targets.Target != null && Svc.Targets.Target.DataId == x.DataId)
            return true;

        if (!GenericHelpers.IsOccupied())
        {
            if (x != null)
            {
                if (EzThrottler.Throttle($"Throttle Targeting {x.DataId}"))
                {
                    Svc.Targets.SetTarget(x);
                    IceLogging.Info($"Setting the target to {x.DataId}");
                }
            }
        }
        return false;
    }
    internal static bool TryGetObjectByDataId(ulong dataId, out IGameObject? gameObject) => (gameObject = Svc.Objects.OrderBy(PlayerHelper.GetDistanceToPlayer).FirstOrDefault(x => x.DataId == dataId)) != null;
    public static IGameObject? TryGetObjectNearestEventObject()
    {
        return Svc.Objects.OrderBy(PlayerHelper.GetDistanceToPlayer).FirstOrDefault(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj);
    }
    public static IGameObject? TryGetObjectCollectionPoint()
    {
        return Svc.Objects.OrderBy(PlayerHelper.GetDistanceToPlayer).FirstOrDefault(x => x.DataId == 2014616 || x.DataId == 2014618);
    }
    public static unsafe void InteractWithObject(IGameObject? gameObject)
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
            IceLogging.Error($"InteractWithObject: Exception: {ex}");
        }
    }

    public static unsafe void SetGatheringRing(uint territoryId, int x, int y, int radius, string? tooltip = "Node Location")
    {
        var map = ExcelHelper.TerritorySheet.GetRow(territoryId).Map.Value;
        var agent = AgentMap.Instance();

        Vector2 pos = MapToWorld(new Vector2(x, y), map.SizeFactor, map.OffsetX, map.OffsetY);
        IceLogging.Debug($"Current map: {map.RowId} {territoryId} | {map.PlaceName.Value.Name} | {pos.X} {pos.Y} | {x} {y} | {radius} | {tooltip}");

        agent->IsFlagMarkerSet = false;
        agent->SetFlagMapMarker(territoryId, map.RowId, x, y);
        agent->TempMapMarkerCount = 0;
        agent->AddGatheringTempMarker(x, y, radius, tooltip: tooltip);
        agent->OpenMap(map.RowId, territoryId, tooltip, MapType.GatheringLog);
    }
}
