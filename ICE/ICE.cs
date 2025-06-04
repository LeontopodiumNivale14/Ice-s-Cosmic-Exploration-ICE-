using ECommons.Automation.NeoTaskManager;
using ECommons.Configuration;
using ICE.Ui;
using ICE.IPC;
using System.Collections.Generic;
using static ICE.Utilities.CosmicHelper;

namespace ICE;

public sealed partial class ICE : IDalamudPlugin
{
    public static string Name => "ICE";
    public static Config C => P.Config;

    internal static ICE P = null!;
    private readonly Config Config;

    // Window's that I use, base window to the settings... need these to actually show shit 
    internal WindowSystem windowSystem;
    internal MainWindow mainWindow;
    internal MainWindowV2 mainWindow2;
    internal SettingsWindow settingWindow;
    internal OverlayWindow overlayWindow;
    internal DebugWindow debugWindow;

    // Taskmanager from Ecommons
    internal TaskManager TaskManager;

    // Internal IPC's that I use for... well plugins. 
    internal LifestreamIPC Lifestream;
    internal NavmeshIPC Navmesh;
    internal PandoraIPC Pandora;
    internal ArtisanIPC Artisan;
    internal VislandIPC Visland;
    internal AutoHookIPC AutoHook;

    public ICE(IDalamudPluginInterface pi)
    {
        P = this;
        ECommonsMain.Init(pi, P, Module.DalamudReflector, ECommons.Module.ObjectFunctions);

        EzConfig.Migrate<Config>();
        Config = EzConfig.Init<Config>();

        //IPC's that are used
        TaskManager = new();
        Lifestream = new();
        Navmesh = new();
        Pandora = new();
        Artisan = new();
        Visland = new();
        AutoHook = new();

        // all the windows
        windowSystem = new();
        mainWindow = new();
        mainWindow2 = new();
        settingWindow = new();
        overlayWindow = new();
        debugWindow = new();

        EzCmd.Add("/icecosmic", OnCommand, """
            Open plugin interface
            /ice clear - Removes all missions
            /ice stop - Stops ICE
            /ice start - Starts ICE
            /ice add | remove | toggle | only 
            	(Ex. /ice add 405 406 410)
            /ice flag [id] - Opens the map and marks where the area of gathering is.
                (Ex. /ice flag 301)
            """);
        EzCmd.Add("/ice", OnCommand);
        EzCmd.Add("/IceCosmic", OnCommand);
        Init();
        Svc.Framework.Update += Tick;

        TaskManager = new(new(showDebug: false));
        Svc.PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        Svc.PluginInterface.UiBuilder.OpenMainUi += () =>
        {
            mainWindow2.IsOpen = true;
        };
        Svc.PluginInterface.UiBuilder.OpenConfigUi += () =>
        {
            settingWindow.IsOpen = true;
        };
        DictionaryCreation();
        TaskGamba.EnsureGambaWeightsInitialized();
    }

    private static void Init()
    {
        ExcelHelper.Init();
    }

    private void Tick(object _)
    {
        if (Svc.ClientState.LocalPlayer != null)
        {
            PlayerHandlers.Tick();
            SchedulerMain.Tick();
            WeatherForecastHandler.Tick();
        }
        else
        {
            PlayerHandlers.DisablePlugin();
        }
        GenericManager.Tick();
        TextAdvancedManager.Tick();
        YesAlreadyManager.Tick();
    }

    public void Dispose()
    {
        GenericHelpers.Safe(() => Svc.Framework.Update -= Tick);
        GenericHelpers.Safe(() => Svc.PluginInterface.UiBuilder.Draw -= windowSystem.Draw);
        GenericHelpers.Safe(TextAdvancedManager.UnlockTA);
        GenericHelpers.Safe(YesAlreadyManager.Unlock);
        ECommonsMain.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        var subcommands = args.Split(' ');

        if (subcommands.Length == 0 || args == "")
        {
            mainWindow2.IsOpen = !mainWindow2.IsOpen;
            return;
        }

        var firstArg = subcommands[0];

        if (firstArg.ToLower() == "old")
        {
            mainWindow.IsOpen = !mainWindow.IsOpen;
            return;
        }

        if (firstArg.ToLower() == "d" || firstArg.ToLower() == "debug")
        {
            debugWindow.IsOpen = true;
            return;
        }
        else if (firstArg.ToLower() == "s" || firstArg.ToLower() == "settings")
        {
            settingWindow.IsOpen = !settingWindow.IsOpen;
            return;
        }
        else if (firstArg.ToLower() == "clear")
        {
            C.Missions.ForEach(x => x.Enabled = false);
            C.Save();
        }
        else if (firstArg.ToLower() == "stop")
        {
            SchedulerMain.DisablePlugin();
        }
        else if (firstArg.ToLower() == "start")
        {
            SchedulerMain.EnablePlugin();
        }
        else if (firstArg.ToLower() == "add")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            C.Missions.Where(item => idSet.Contains(item.Id))
                .ToList()
                .ForEach(item => item.Enabled = true);
            C.Save();
        }
        else if (firstArg.ToLower() == "remove")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            C.Missions.Where(item => idSet.Contains(item.Id))
                .ToList()
                .ForEach(item => item.Enabled = false);
            C.Save();
        }
        else if (firstArg.ToLower() == "toggle")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            C.Missions.Where(item => idSet.Contains(item.Id))
                .ToList()
                .ForEach(item => item.Enabled = !item.Enabled);
            C.Save();
        }
        else if (firstArg.ToLower() == "only")
        {
            uint[] ids = [.. subcommands.Skip(1).Select(uint.Parse)];
            var idSet = new HashSet<uint>(ids);
            if (ids.Length == 0) return;

            C.Missions.ForEach(item => item.Enabled = idSet.Contains(item.Id));
            C.Save();
        }
        else if (firstArg.ToLower() == "flag")
        {
            if (subcommands.Length != 2) return;
            if (!PlayerHelper.IsInCosmicZone()) return;

            int missionId = int.Parse(subcommands[1]);
            var info = MissionInfoDict.FirstOrDefault(mission => mission.Key == missionId);
            if (info.Value == default) return;
            if (info.Value.MarkerId == 0) return;

            Utils.SetGatheringRing(info.Value.TerritoryId, info.Value.X, info.Value.Y, info.Value.Radius, info.Value.Name);
        }
    }
}
