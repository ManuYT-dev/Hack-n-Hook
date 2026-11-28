using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum BoatType
{
    FisherBoat,
    MidBoat,
    Yacht
}

