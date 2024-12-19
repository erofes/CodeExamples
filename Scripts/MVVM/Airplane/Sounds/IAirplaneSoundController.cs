using System;

namespace Game.Airplane
{
    public interface IAirplaneSoundController : IDisposable
    {
        AirplaneSoundController.EState State { get; }
        void UpdateLocation();
        void UpdateEngineSound();
        void PlayHit();
        void PlayDestroy();
    }
}
