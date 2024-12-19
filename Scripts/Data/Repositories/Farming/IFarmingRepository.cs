using Cysharp.Threading.Tasks;
using Game.Data.Models;
using System;

namespace Game.Data.Repositories
{
    public interface IFarmingRepository
    {
        UniTask<decimal> GetWarehouseLimit(string currency);
        UniTask<int> GetSlotsLimit(string currency);
        UniTask<FarmingSlotModel[]> GetSlots(string currency);
        UniTask<AddFarmingWorkerResponse> AddWorkerToSlot(Guid workerId, string currency);
        UniTask RemoveWorkerFromSlot(Guid workerId, string currency);
    }
}