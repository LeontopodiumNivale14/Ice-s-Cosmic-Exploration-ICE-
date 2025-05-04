using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Collections.Generic;

namespace ICE.Scheduler.Handlers;

internal static unsafe class PlayerHandlers
{
    public static readonly Dictionary<(int start, int end), string[]> timeMap = new Dictionary<(int start, int end), string[]>
        {
            { (0, 1), new[] { "CRP", "ALC" } },
            { (2, 3), new[] { "MIN" } },
            { (4, 5), new[] { "BSM", "CUL" } },
            { (6, 7), new[] { "FSH" } },
            { (8, 9), new[] { "ARM" } },
            { (10, 11), new[] { "BTN" } },
            { (12, 13), new[] { "GSM" } },
            { (16, 17), new[] { "LTW" } },
            { (20, 21), new[] { "WVR" } }
        };
    private static readonly uint stellarSprintID = 4398;

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

        if (C.EnableAutoSprint && IsInZone(1237) && !HasStatusId(stellarSprintID) && Svc.Condition[ConditionFlag.NormalConditions] && IsMoving()) UseSprint(); 
    }

    private static void UseSprint()
    {
        var am = ActionManager.Instance();
        var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;

        if (isSprintReady) am->UseAction(ActionType.GeneralAction, 4);
    }

    private static (long, long) GetEorzeaTime()
    {
        var eorzeaTime = Framework.Instance()->ClientTime.EorzeaTime;
        long hours = eorzeaTime / 3600 % 24;
        long minutes = eorzeaTime / 60 % 60;
        return (hours, minutes);
    }

    internal static long GetCurrentHour()
    {
        var (hours, minutes) = GetEorzeaTime();
        return hours;
    }

    internal static (KeyValuePair<(int start, int end), string[]>, KeyValuePair<(int start, int end), string[]>) GetTimedJob()
    {
        KeyValuePair<(int start, int end), string[]> currentTimeBonus = default;
        KeyValuePair<(int start, int end), string[]> nextTimeBonus = default;

        (long hours, _) = GetEorzeaTime();
        var currentTime = timeMap.FirstOrDefault(time => hours >= time.Key.start && hours <= time.Key.end);
        if (!currentTime.Equals(default(KeyValuePair<(int, int), string[]>))) currentTimeBonus = currentTime;

        var nextTime = timeMap.FirstOrDefault(time => hours < time.Key.start);
        if (!nextTime.Equals(default(KeyValuePair<(int, int), string[]>))) nextTimeBonus = nextTime;
        else nextTimeBonus = timeMap.First();

        return (currentTimeBonus, nextTimeBonus);
    }
}
