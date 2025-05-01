using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Scheduler.Handlers;

internal static unsafe class PlayerHandlers
{
    public static float Distance(this Vector3 v, Vector3 v2)
    {
        return new Vector2(v.X - v2.X, v.Z - v2.Z).Length();
    }
    public static unsafe bool IsMoving()
    {
        return AgentMap.Instance()->IsPlayerMoving;
    }

    internal static void Tick()
    {
        if (IsInZone(1237) && UsingSupportedJob() && C.ShowOverlay) ICE.P.overlayWindow.IsOpen = true;
        else ICE.P.overlayWindow.IsOpen = false;
    }
}
