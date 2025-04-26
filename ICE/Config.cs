using System.Collections.Generic;
using System.Text.Json.Serialization;
using ECommons.Configuration;

namespace ICE;
public class Config : IEzConfig
{
    [JsonIgnore]
    public const int CurrentConfigVersion = 1;

    public List<(uint Id, string Name)> EnabledMission = new List<(uint Id, string Name)>();

    public int UIActionDelay { get; set; } = 1000; // Default delay in milliseconds for mission accept/abandon

    public void Save()
    {
        EzConfig.Save();
    }
}