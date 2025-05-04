using ECommons.ExcelServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using static FFXIVClientStructs.FFXIV.Client.Game.NameCache;

namespace ICE.Scheduler.Handlers;
internal static unsafe class GearsetHandler
{
    public static unsafe void SwapJob(uint jobId)
    {
        if (jobId == (Svc.ClientState.LocalPlayer?.ClassJob.Value.RowId ?? 0))
        {
            PluginLog.Debug($"Job is already {jobId}");
            return;
        }
        else
        {
            var gearsets = RaptureGearsetModule.Instance();
            foreach (ref var gs in gearsets->Entries)
            {
                if (!RaptureGearsetModule.Instance()->IsValidGearset(gs.Id)) continue;
                if ((uint)gs.ClassJob == jobId)
                {
                    var result = gearsets->EquipGearset(gs.Id);
                    PluginLog.Debug($"Tried to equip gearset {gs.Id} for {jobId}, result={result}, flags={gs.Flags}");
                }
            }
        }
    }
}