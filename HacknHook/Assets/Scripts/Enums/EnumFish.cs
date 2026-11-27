using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum FishType
{
    Barsch,
    Forelle,
    Hecht
}