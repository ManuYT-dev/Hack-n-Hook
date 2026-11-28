using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum EngineType
{
    Level0,
    Level1,
    Level2,
    Level3
}

