using System;
using System.Collections.Generic;
using Game.Base;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utilities.Collections;

namespace Game.Airplane
{
    public interface IAirplaneService : IDisposable, IInitializable
    {
        UniTask< IAirplaneLogic > CreateLogic(
            AirplaneConfigId airplaneConfigId,
            IAirplanePilot airplanePilot,
            AirplaneViewConfig viewConfig,
            AirplanePhysConfig physicsConfig,
            bool isSrDebug
        );

        IAirplaneLogic GetUserAirplaneLogic();
        void DestroyLogic( IAirplaneLogic airplaneLogic );
        void DestroyAll();
        bool IsFinishReached( IAirplaneLogic airplaneLogic );

        void ResetSimulation( List< AirplaneLogicSaveData > positions = null );

        /// <summary>
        ///     can be used to delete and add Logics while iterating!
        ///     newly added logics shall not be processed
        /// </summary>
        ReversedEnumerable< IAirplaneLogic > GetAirplanesReversed();

        void StartGame();
        void CreateWalls();
        void DestroyWalls();
        GameObject GetAirplaneView( AirplaneViewConfigId airplaneViewConfigId, string pilotName );
        AirplaneViewConfig GetAirplaneViewConfig( AirplaneConfigId id );
        AirplanePhysConfig GetAirplanePhysicsConfig( AirplaneConfigId id );
    }
}
