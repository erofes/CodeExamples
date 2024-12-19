using System;
using Newtonsoft.Json;

namespace Game.Data.Models
{
    public class FarmingWorkerRequest
    {
        public FarmingWorkerRequest(Guid workerId, string currency)
        {
            WorkerId = workerId;
            Currency = currency;
        }

        [JsonProperty("workerId")]
        public readonly Guid WorkerId;
        [JsonProperty("currency")]
        public readonly string Currency;
    }
}
