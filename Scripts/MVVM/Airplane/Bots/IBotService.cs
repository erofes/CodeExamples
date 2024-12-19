namespace Game.Airplane
{
    public interface IBotService
    {
        public BotConfig GetRandomBotConfig();
        IAirplanePilot GetPerlinBotPilot( BotConfig config );
        int GetBotsPerLevelCount();
    }
}
