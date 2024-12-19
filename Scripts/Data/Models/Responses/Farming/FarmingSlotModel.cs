using Newtonsoft.Json;

namespace Game.Data.Models
{
    public class FarmingSlotModel
    {
        [JsonProperty("worker")]
        public AssetModel<WorkerAttributesModel> Worker { get; set; }

        [JsonProperty("speed")]
        public int Speed { get; set; }

        [JsonProperty("createdUtc")]
        public string CreatedUtc { get; set; }

        [JsonProperty("nextCollectionUtc")]
        public string NextCollectionUtc { get; set; }
    }
}
