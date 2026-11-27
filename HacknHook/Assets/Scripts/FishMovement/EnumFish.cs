using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.FishMovement
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FishType
    {
        Barsch,
        Forelle,
        Hecht
    }
}
