using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;

namespace Game.Airplane
{
    public class BotManager : IBotManager
    {
        private readonly IBotService _botService;
        private readonly IAirplaneService _airplaneService;

        public BotManager( IBotService botService, IAirplaneService airplaneService )
        {
            _botService = botService;
            _airplaneService = airplaneService;
        }

        public async UniTask CreateBots()
        {
            int botsCount = _botService.GetBotsPerLevelCount();
            if ( botsCount > 0 )
            {
                UniTask< IAirplaneLogic >[] createBotAirplaneTasks = new UniTask< IAirplaneLogic >[botsCount];

                for ( int i = 0; i < botsCount; i++ )
                {
                    createBotAirplaneTasks[ i ] = CreateBotAirplane( _botService.GetRandomBotConfig() );
                }

                await UniTask.WhenAll( createBotAirplaneTasks );
            }
        }

        public async UniTask RecreateBotAirplane( IAirplaneLogic airplaneLogic )
        {
            BotConfig config = airplaneLogic.AirplanePilot.BotConfig;
            airplaneLogic.AirplanePilot.Dispose();
            _airplaneService.DestroyLogic( airplaneLogic );

            //todo create new bot-airplane more interesting way 
            airplaneLogic = await CreateBotAirplane( config );
        }

        private async UniTask< IAirplaneLogic > CreateBotAirplane( BotConfig config )
        {
            AirplaneConfigId configId = config.AirplaneConfigId;
            AirplaneViewConfig viewConfig = _airplaneService.GetAirplaneViewConfig( config.AirplaneConfigId );
            AirplanePhysConfig physicsConfig = _airplaneService.GetAirplanePhysicsConfig( config.AirplaneConfigId );

            IAirplanePilot pilot = _botService.GetPerlinBotPilot( config );

            IAirplaneLogic airplaneLogic = await _airplaneService.CreateLogic(
                configId,
                pilot,
                viewConfig,
                physicsConfig,
                false
            );

            return airplaneLogic;
        }
    }
}
