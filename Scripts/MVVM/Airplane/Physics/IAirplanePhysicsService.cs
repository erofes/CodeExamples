using System;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;

namespace Game.Airplane
{
    public interface IAirplanePhysicsService : IDisposable
    {
        UniTask< IAirplanePhysicsController > CreateController(
            IAirplanePilot airplanePilot,
            AirplanePhysConfig physicsConfig,
            SpeedReference speedReference,
            TransformReference transformReference,
            bool isSrDebug
        );

        void DestroyController( IAirplanePhysicsController controller );
        void CreateWalls();
        void DestroyWalls();
    }
}
