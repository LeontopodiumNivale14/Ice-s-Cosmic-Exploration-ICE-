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
        public List<CosmicMission> Missions { get; set; } = [];

        // Overlay settings
        public bool ShowOverlay { get; set; } = false;
        public bool ShowSeconds { get; set; } = false;

        // Safety settings
        public bool DelayGrab { get; set; } = false;
        public bool StopOnAbort { get; set; } = true;
        public bool RejectUnknownYesno { get; set; } = true;

        // Table settings
        public bool HideUnsupportedMissions { get; set; } = false;
        public bool OnlyGrabMission { get; set; } = false;
        public bool AutoPickCurrentJob { get; set; } = false;
        public int TableSortOption = 0;
        public bool ShowExpColums { get; set; } = true;
        public bool ShowCreditsColumn { get; set; } = true;

        // Misc settings
        public bool EnableAutoSprint { get; set; } = true;

        // Job swap settings
        public bool EnableWeatherJobSwap { get; set; } = false;
        public uint UmbralWindJobId { get; set; } = 9;
        public uint MoonDustJobId { get; set; } = 9;
        public bool EnableTimeJobSwap { get; set; } = false;
        public Dictionary<int, uint> JobSwapTable = Enumerable.Range(0, 12).ToDictionary(i => i, i => (uint)9);

        public void Save()
        {
            EzConfig.Save();
        }
    }

    public class CosmicMission
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public MissionType Type { get; set; } = MissionType.Standard;
        public bool Enabled { get; set; } = false;
        public uint PreviousMissionId { get; set; } = 0;
        public uint JobId { get; set; }
        public bool TurnInSilver { get; set; } = false;
        public bool TurnInASAP { get; set; } = false;
    }

    public enum MissionType
    {
        Standard = 0,
        Sequential = 1,
        Weather = 2,
        Timed = 3,
        Critical = 4
    }
}