using Cysharp.Threading.Tasks;

namespace Game.Airplane
{
    public interface IBotManager
    {
        UniTask CreateBots();
        UniTask RecreateBotAirplane( IAirplaneLogic airplaneLogic );
    }
}
