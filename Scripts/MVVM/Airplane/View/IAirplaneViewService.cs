using System;
using Game.Base;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;

namespace Game.Airplane
{
    public interface IAirplaneViewService : IInitializable, IDisposable
    {
        UniTask LogicCreated( IAirplaneLogic logic, AirplaneViewConfig airplaneViewConfig, string pilotName );

        void LogicDestroyed( IAirplaneLogic logic );
    }
}
