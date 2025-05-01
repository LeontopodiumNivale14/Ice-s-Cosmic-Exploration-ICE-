using System.Collections.Generic;
using System.Text.Json.Serialization;
using ECommons.Configuration;

namespace ICE
{
    public class Config : IEzConfig
    {
        [JsonIgnore]
        public const int CurrentConfigVersion = 1;

        // Missions the user has enabled
        public List<(uint Id, string Name)> EnabledMission { get; set; } = new List<(uint, string)>();
        public List<(uint Id, string Name)> CriticalMissions { get; set; } = new List<(uint, string)>();
        public List<(uint Id, string Name)> TimedMissions { get; set; } = new List<(uint, string)>();
        public List<(uint Id, string Name)> WeatherMissions { get; set; } = new List<(uint, string)>();
        public List<(uint Id, string Name)> SequenceMissions { get; set; } = new List<(uint, string)>();
        public List<(uint Id, string Name)> StandardMissions { get; set; } = new List<(uint, string)>();

        // Overlay settings
        public bool ShowOverlay { get; set; } = false;

        // Delay grabbing mission
        public bool DelayGrab { get; set; } = false;

        // Turnin options
        public bool TurninOnSilver { get; set; } = false;
        public bool TurninASAP { get; set; } = false;

        // Table settings
        public bool HideUnsupportedMissions { get; set; } = false;
        public bool OnlyGrabMission { get; set; } = false;
        public bool AutoPickCurrentJob { get; set; } = false;
        public int TableSortOption = 0;
        public bool ShowExpColums = true;

        public void Save()
        {
            EzConfig.Save();
        }
    }
}