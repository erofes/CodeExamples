using System;

namespace Game.Airplane
{
    [ Serializable ]
    public readonly struct PilotSaveData
    {
        public readonly bool IsUser;
        public readonly BotConfig BotConfig;

        public PilotSaveData( bool isUser, BotConfig botConfig )
        {
            IsUser = isUser;
            BotConfig = botConfig;
        }
    }
}
