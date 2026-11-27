using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum BaitType
{
    Level1,
    Level2,
    Level3
}

