using UnityEngine;

namespace Game.Airplane
{
    public class BotService : IBotService
    {
        private readonly BotsConfig _botsConfig;
        private int _botCount;

        public BotService( BotsConfig botsConfig )
        {
            _botsConfig = botsConfig;
        }

        public BotConfig GetRandomBotConfig()
        {
            return _botsConfig.Configs[ Random.Range( 0, _botsConfig.Configs.Count ) ];
        }

        public int GetBotsPerLevelCount()
        {
            return _botsConfig.BotsPerLevelCount;
        }

        public IAirplanePilot GetPerlinBotPilot( BotConfig config )
        {
            return new PerlinNoiseBotPilot( config, ++_botCount );
        }
    }
}
