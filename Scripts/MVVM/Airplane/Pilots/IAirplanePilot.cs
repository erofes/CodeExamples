using System;

namespace Game.Airplane
{
    public interface IAirplanePilot : IDisposable
    {
        IAirplaneLogic CurrentAirplane { get; }
        void InitializeFlight( IAirplaneLogic logic, ITrackService trackService );
        BotConfig BotConfig { get; }
        bool IsUser { get; }

        string DebugName { get; }
        float GetYaw();
        float GetAcceleration();

        void SaveData( out PilotSaveData data );
    }
}
