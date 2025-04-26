using ICE.Scheduler;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ECommons.Throttlers;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui;

// This isn't currently wired up to anything. Can actually use this to place all the general settings for all the windows...
internal class SettingsWindow : Window
{
    public SettingsWindow() :
        base($"Ice's Cosmic Exploration Debug {P.GetType().Assembly.GetName().Version} ###ICESettingsWindow")
    {
        Flags = ImGuiWindowFlags.None;
        SizeConstraints = new()
        {
            MinimumSize = new Vector2(400, 400),
            MaximumSize = new Vector2(2000, 2000),
        };
        P.windowSystem.AddWindow(this);
        AllowPinning = false;
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("Mission Accept/Abandon Delay (ms):");
        var missionAbandonDelay = C.UIActionDelay;
        if (ImGui.SliderInt("##UIActionDelay", ref missionAbandonDelay, 0, 5000))
        {
            C.UIActionDelay = missionAbandonDelay;
            C.Save(); // Save the configuration change
        }
        ImGui.SameLine();
        if (ImGui.Button("Reset##UIActionDelay"))
        {
            C.UIActionDelay = 1000; // Reset to default
            C.Save();
        }
        ImGui.Separator();
    }
}
