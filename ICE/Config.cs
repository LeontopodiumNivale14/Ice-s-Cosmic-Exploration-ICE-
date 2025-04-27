using System.Collections.Generic;
using System.Text.Json.Serialization;
using ECommons.Configuration;

namespace ICE;
public class Config : IEzConfig
{
    [JsonIgnore]
    public const int CurrentConfigVersion = 1;

    public List<(uint Id, string Name)> EnabledMission = new List<(uint Id, string Name)>();

    public bool DelayGrab = false;

    public bool FoodMe = false;
    public bool Once = false;

    public bool[] TargetResearch = [false, false, false, false];

    public void Save()
    {
        EzConfig.Save();
    }
}