using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices.Legacy;
using ECommons.Reflection;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace ICE.Utilities;

/// <summary>
/// Misc unused (yet) or weird functions i didn't know where to put - Chika
/// </summary>
public static unsafe class Utils
{
    public static bool HasPlugin(string name) => DalamudReflector.TryGetDalamudPlugin(name, out _, false, true);
    public static TaskManagerConfiguration TaskConfig => new(timeLimitMS: 10 * 60 * 3000, abortOnTimeout: false);


    public static unsafe void SetFlagForNPC(uint territoryId, float x, float y)
    {
        var terSheet = Svc.Data.GetExcelSheet<TerritoryType>();
        var map = terSheet.GetRow(territoryId).Map.Value;

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

    internal static bool? TargetgameObject(IGameObject? gameObject)
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
            IceLogging.Error($"InteractWithObject: Exception: {ex}");
        }
    }

    public static unsafe void SetGatheringRing(uint teri, float x, float y, int radius)
    {
        var agent = AgentMap.Instance();
        var debugText = "Current teri/map: {currentTeri} {currentMap}" + ", " + agent->CurrentTerritoryId
                       + ", " + agent->CurrentMapId;
        IceLogging.Debug(debugText);

        var terSheet = Svc.Data.GetExcelSheet<TerritoryType>();
        var mapId = terSheet.GetRow(teri).Map.Value.RowId;

        agent->IsFlagMarkerSet = false;
        agent->SetFlagMapMarker(teri, mapId, x, y);
        agent->TempMapMarkerCount = 0;
        agent->AddGatheringTempMarker((int)x, (int)y, radius, tooltip: "Node Location");
        agent->OpenMap(agent->CurrentMapId, teri, "Node Location", FFXIVClientStructs.FFXIV.Client.UI.Agent.MapType.GatheringLog);
    }
}
