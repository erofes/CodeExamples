using Cysharp.Threading.Tasks;
using Game.Data.Models;
using System;

namespace Game.Data.Repositories
{
    public class FarmingRepository : BaseHttpRepository, IFarmingRepository
    {
        public async UniTask<decimal> GetWarehouseLimit(string currency)
        {
            var request = new FarmingSlotsRequest(currency);
            var httpContent = SerializeHttpContent(request);
            var response = await GetHttpClient().PostAsync("/api/v1/farming/warehouse-limit", httpContent);
            var result = await DeserializeApiResultResponse<WarehouseLimitResponse>(response);
            return result.Limit;
        }

        public async UniTask<int> GetSlotsLimit(string currency)
        {
            var request = new FarmingSlotsRequest(currency);
            var httpContent = SerializeHttpContent(request);
            var response = await GetHttpClient().PostAsync("/api/v1/farming/max-available-slot-count", httpContent);
            var result = await DeserializeApiResultResponse<FarmingSlotsLimitResponse>(response);
            return result.Limit;
        }

        public async UniTask<FarmingSlotModel[]> GetSlots(string currency)
        {
            var request = new FarmingSlotsRequest(currency);
            var httpContent = SerializeHttpContent(request);
            var response = await GetHttpClient().PostAsync("/api/v1/farming/list-slots", httpContent);
            var result = await DeserializeApiResultResponse<ListFarmingSlotResponse>(response);
            return result.Slots;
        }

        public async UniTask<AddFarmingWorkerResponse> AddWorkerToSlot(Guid workerId, string currency)
        {
            var request = new FarmingWorkerRequest(workerId, currency);
            var httpContent = SerializeHttpContent(request);
            var response = await GetHttpClient().PostAsync("/api/v1/farming/add-worker-to-slot", httpContent);
            var result = await DeserializeApiResultResponse<AddFarmingWorkerResponse>(response);
            return result;
        }

        public async UniTask RemoveWorkerFromSlot(Guid workerId, string currency)
        {
            var request = new FarmingWorkerRequest(workerId, currency);
            var httpContent = SerializeHttpContent(request);
            var response = await GetHttpClient().PostAsync("/api/v1/farming/remove-worker-from-slot", httpContent);
            await DeserializeApiResultResponse<EmptyModel>(response);
        }
    }
}