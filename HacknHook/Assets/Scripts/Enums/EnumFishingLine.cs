using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum FishingLineType
{
    Level1,
    Level2,
    Level3
}

