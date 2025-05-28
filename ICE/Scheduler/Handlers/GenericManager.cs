using ECommons.Automation.LegacyTaskManager;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;

namespace ICE.Scheduler.Handlers
{
    internal static unsafe class GenericManager
    {
        internal static TaskManager taskManager = new();
        static TaskManager TaskManager => taskManager;
        private static bool? ConfirmOrAbort(AddonRequest* addon)
        {
            if (addon->HandOverButton != null && addon->HandOverButton->IsEnabled)
            {
                new AddonMaster.Request((IntPtr)addon).HandOver();
                return true;
            }
            return false;
        }
        /// <summary>
        /// Checks to see if CurrentGP + Cordial usage is > MaxGP
        /// </summary>
        /// <param name="recoveryGP"></param>
        /// <returns>Bool true/false</returns>
        private static bool WillOvercap(int recoveryGP)
        {
            return ((PlayerHelper.GetGp() + recoveryGP) > PlayerHelper.MaxGp());
        }
        private static bool UseCordial()
        {
            if (Svc.ClientState.LocalPlayer is null)
                return false;

            if ((!CosmicHelper.GatheringJobList.Contains((int)PlayerHelper.GetClassJobId()))
             || (PlayerHelper.GetClassJobId() == 18 && !C.UseOnFisher)
             || (PlayerHelper.GetGp() >= C.CordialMinGp))
                return false;
            else 
            {
                return true;
            }
        }

        internal static void Tick()
        {
            if (!SchedulerMain.State.HasFlag(IceState.ManualMode))
            {
                if (SchedulerMain.State.HasFlag(IceState.Gather))
                {
                    var featureEnabled = (P.Pandora.GetFeatureEnabled("Pandora Quick Gather") ?? false);

                    if (featureEnabled)
                    {
                        if (EzThrottler.Throttle("Disabling Pandora Gathering", 1000))
                        {
                            P.Pandora.PauseFeature("Pandora Quick Gather", 1000);
                        }
                    }
                }

                bool CosmicZone = PlayerHelper.IsInCosmicZone();
                if (C.AutoCordial && CosmicZone)
                {
                    var pandoraCordial = (P.Pandora.GetFeatureEnabled("Auto-Cordial") ?? false);
                    if (pandoraCordial && (C.UseOnlyInMission && SchedulerMain.State.HasFlag(IceState.Gather)))
                    {
                        if (EzThrottler.Throttle("Disabling Pandora Cordial", 1000))
                        {
                            P.Pandora.PauseFeature("Auto-Cordial", 1000);
                        }
                    }
                    else if (pandoraCordial)
                    {
                        if (EzThrottler.Throttle("Disabling Pandora Cordial", 1000))
                        {
                            P.Pandora.PauseFeature("Auto-Cordial", 1000);
                        }
                    }

                    bool useCordial = true;
                    if (Svc.ClientState.LocalPlayer == null)
                    {
                        // IceLogging.Debug("Player was null");
                        useCordial = false;
                    }
                    if (PlayerHelper.GetClassJobId() is not (16 or 17 or 18))
                    {
                        // IceLogging.Debug("Player is not a gathering job");
                        useCordial = false;
                    }
                    if (PlayerHelper.GetClassJobId() == 18 && !C.UseOnFisher)
                    {
                        // IceLogging.Debug("Player is a fisher, but fishing job not enabled");
                        useCordial = false;
                    }
                    if (C.CordialMinGp <= PlayerHelper.GetGp())
                    {
                        // IceLogging.Debug($"Current GP: {C.CordialMinGp} is < {PlayerHelper.GetGp()}");
                        useCordial = false;
                    }
                    if (!PlayerHelper.IsInCosmicZone())
                    {
                        // IceLogging.Debug("Player is not in cosmic zone");
                        useCordial = false;
                    }
                    if (C.UseOnlyInMission && !SchedulerMain.State.HasFlag(IceState.Gather))
                    {
                        // IceLogging.Debug("Use only in mission, but mission doesn't have gathering state");
                        useCordial = false;
                    }

                    if (useCordial)
                    {
                        Dictionary<uint, int> cordials = new()
                            {
                                { 12669, 400}, // Hi
                                { 1006141, 350}, // HQ Regular
                                { 6141, 300}, // NQ Regular
                                { 1016911, 200}, // HQ Watered
                                { 16911, 150} // HQ Watered
                            };

                        foreach (var cordial in C.inverseCordialPrio ? cordials.Reverse() : cordials)
                        {
                            bool hq = cordial.Key >= 1_000_000;
                            if (PlayerHelper.GetItemCount((int)cordial.Key, out var amount, hq, !hq) && amount > 0)
                            {
                                if (ActionManager.Instance()->GetActionStatus(ActionType.Item, cordial.Key) == 0)
                                {
                                    if (!C.PreventOvercap || (C.PreventOvercap && !WillOvercap(cordial.Value)))
                                    {
                                        ActionManager.Instance()->UseAction(ActionType.Item, cordial.Key, extraParam: 65535);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (EzThrottler.Throttle("DelayedTick"))
                if (AddonHelper.IsAddonActive("WKSLottery") && C.GambaEnabled && SchedulerMain.State == IceState.Idle)
                    SchedulerMain.EnablePlugin();
        }
    }
}
