using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;

namespace ICE.Scheduler.Handlers
{
    internal static class GearsetHandler // Borrowed from Artisan
    {
        internal unsafe static void TaskClassChange(Job job)
        {
            if (job == (Job)PlayerHelper.GetClassJobId() || !EzThrottler.Throttle("Gearset", 250) || Player.IsBusy)
                return;
            var gearsets = RaptureGearsetModule.Instance();
            foreach (ref var gs in gearsets->Entries)
            {
                if (!RaptureGearsetModule.Instance()->IsValidGearset(gs.Id)) continue;
                if ((Job)gs.ClassJob == job)
                {
                    if (gs.Flags.HasFlag(RaptureGearsetModule.GearsetFlag.MainHandMissing))
                    {
                        if (GenericHelpers.TryGetAddonMaster<SelectYesno>("SelectYesno", out var select) && select.IsAddonReady)
                        {
                            select.Yes();
                        }
                        else
                        {
                            gearsets->EquipGearset(gs.Id);
                        }
                    }

                    var result = gearsets->EquipGearset(gs.Id);
                    IceLogging.Debug($"Tried to equip gearset {gs.Id} for {job}, result={result}, flags={gs.Flags}");
                    return;
                }
            }
            return;
        }
    }
}