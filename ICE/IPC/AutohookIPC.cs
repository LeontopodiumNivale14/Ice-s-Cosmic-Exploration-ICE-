using ECommons.EzIpcManager;
using ECommons.Automation;

namespace ICE.IPC;

public class AutoHookIPC
{
    public const string Name = "AutoHook";
    public AutoHookIPC() => EzIPC.Init(this, Name, SafeWrapper.AnyException);
    public bool Installed => Utils.HasPlugin(Name);

    [EzIPC] public Action<bool> SetPluginState;
    [EzIPC] public Action<bool> SetAutoGigState;
    [EzIPC] public Action<string> SetPreset;
    [EzIPC] public Action<string> SetPresetAutogig;
    [EzIPC] public Action<string> CreateAndSelectAnonymousPreset;
    [EzIPC] public Action<string> ImportAndSelectPreset;
    [EzIPC] public Action DeleteSelectedPreset;
    [EzIPC] public Action DeleteAllAnonymousPresets;
    public void StartFishing()
    {
        Chat.SendMessage("/ahstart");
    }
}
