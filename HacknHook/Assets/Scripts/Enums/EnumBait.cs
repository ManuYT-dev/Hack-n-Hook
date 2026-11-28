using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum BaitType
{
    Level0,
    Level1,
    Level2,
    Level3,
    Level4,
    Level5,
    Level6,
    Level7,
    Level8
}

