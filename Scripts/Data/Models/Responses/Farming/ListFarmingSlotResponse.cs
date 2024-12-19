using Newtonsoft.Json;

namespace Game.Data.Models
{
    public class ListFarmingSlotResponse
    {
        [JsonProperty("slots")]
        public FarmingSlotModel[] Slots { get; set; }
    }
}