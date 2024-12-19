using System;
using Newtonsoft.Json;

namespace Game.Data.Models
{
    public class FarmingSlotsRequest
    {
        public FarmingSlotsRequest(string currency)
        {
            Currency = currency;
        }

        [JsonProperty("currency")]
        public readonly string Currency;
    }
}
