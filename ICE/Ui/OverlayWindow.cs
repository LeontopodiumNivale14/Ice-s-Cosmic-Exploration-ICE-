using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Dalamud.Interface.Utility.Raii;
using ICE.Scheduler;

namespace ICE.Ui
{
    internal class OverlayWindow : Window
    {
        public OverlayWindow() : base("ICE Overlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize)
        {
            P.windowSystem.AddWindow(this);
        }

        public void Dispose()
        {
            P.windowSystem.RemoveWindow(this);
        }

        public override void Draw()
        {
            ImGui.Text($"Current state: " + SchedulerMain.State.ToString());
            ImGui.Spacing();

            if (ImGuiEx.IconButton("\uf013##Config", "Open ICE"))
            {
                P.mainWindow.IsOpen = true;
            }
            ImGui.SameLine();

            // Start button (disabled while already ticking).
            using (ImRaii.Disabled(SchedulerMain.State != IceState.Idle || !UsingSupportedJob()))
            {
                if (ImGui.Button("Start"))
                {
                    SchedulerMain.EnablePlugin();
                }
            }

            ImGui.SameLine();

            // Stop button (disabled while not ticking).
            using (ImRaii.Disabled(SchedulerMain.State == IceState.Idle))
            {
                if (ImGui.Button("Stop"))
                {
                    SchedulerMain.DisablePlugin();
                }

                ImGui.SameLine();
                ImGui.Checkbox("Stop after current mission", ref SchedulerMain.StopBeforeGrab);
            }
        }
    }
}
