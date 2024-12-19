using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Models
{
    public class FarmingSlotsLimitResponse
    {
        [JsonProperty("limit")]
        public int Limit { get; set; }
    }
}