using System;
using Cysharp.Threading.Tasks;

namespace Game.Airplane
{
    public interface IAirplaneSoundService : IDisposable
    {
        public UniTask< IAirplaneSoundController > CreateController(
            AirplaneConfigId airplaneId,
            IAirplanePilot airplanePilot,
            SpeedReference speedReference,
            ITransformReference transformReference
        );

        public void DestroyController( IAirplaneSoundController controller );
    }
}
