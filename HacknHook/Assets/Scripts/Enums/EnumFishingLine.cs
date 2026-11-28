using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum FishingLineType
{
    Level0,
    Level1,
    Level2,
    Level3
}

