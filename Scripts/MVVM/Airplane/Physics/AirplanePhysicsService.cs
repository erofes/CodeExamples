using System.Collections.Generic;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Airplane
{
    public sealed partial class AirplanePhysicsService : IAirplanePhysicsService
    {
        private readonly IGameObjectPoolService _gameObjectPoolService;
        private readonly IGameLoopService _gameLoopService;

        private readonly List< AirplanePhysicsController > _controllers = new List< AirplanePhysicsController >();

        private readonly MovementPhysicsService _movementPhysicsService;
        private readonly ObstaclesPhysicsService _obstaclesPhysicsService;

        private IGameLoopHandler _fixedUpdateHandler;

        public AirplanePhysicsService(
            Transform layoutParent,
            IGameObjectPoolService gameObjectPoolService,
            MovementPhysicsService.Factory movementPhysicsServiceFactory,
            ObstaclesPhysicsService.Factory obstaclesPhysicsServiceFactory,
            IGameLoopService gameLoopService
        )
        {
            _gameObjectPoolService = gameObjectPoolService;
            _gameLoopService = gameLoopService;

            _movementPhysicsService = movementPhysicsServiceFactory.Create( layoutParent );
            _obstaclesPhysicsService = obstaclesPhysicsServiceFactory.Create( layoutParent );

            _fixedUpdateHandler = _gameLoopService.AddFixedUpdate(
                IGameLoopService.EOrderName.AirplanePhysicsService,
                HandleOnFixedUpdate
            );
        }

        public void Dispose()
        {
            foreach ( AirplanePhysicsController item in _controllers )
            {
                item.Dispose();
            }
            _controllers.Clear();

            _gameLoopService.RemoveFixedUpdate( _fixedUpdateHandler );
            _fixedUpdateHandler = null;
        }

        public async UniTask< IAirplanePhysicsController > CreateController(
            IAirplanePilot airplanePilot,
            AirplanePhysConfig physicsConfig,
            SpeedReference speedReference,
            TransformReference transformReference,
            bool isSrDebug
        )
        {
            await _gameObjectPoolService.CacheAssetsAsync(
                new string[]
                {
                    physicsConfig.MovementAssetKey,
                    physicsConfig.ObstaclesAssetKey
                }
            );

            MovementPhysicsController movementController =
                _movementPhysicsService.CreateController( physicsConfig, speedReference, airplanePilot.DebugName, isSrDebug );

            ObstaclesPhysicsController obstacleController =
                _obstaclesPhysicsService.CreateController( physicsConfig, transformReference, speedReference, airplanePilot.DebugName );

            AirplanePhysicsController controller = new AirplanePhysicsController(
                airplanePilot,
                movementController,
                obstacleController
            );

            _controllers.Add( controller );

            return controller;
        }

        public void CreateWalls()
        {
            _movementPhysicsService.CreateWalls();
        }

        public void DestroyWalls()
        {
            _movementPhysicsService.DestroyWalls();
        }

        public void DestroyController( IAirplanePhysicsController controller )
        {
            if ( controller == null ||
                controller.State == AirplanePhysicsController.EState.Disposed )
            {
                return;
            }

            _controllers.Remove( ( AirplanePhysicsController )controller );
            _movementPhysicsService.DestroyController( controller.MovementController );
            _obstaclesPhysicsService.DestroyController( controller.ObstacleController );
            controller.Dispose();
        }

        private void HandleOnFixedUpdate( float deltaTime )
        {
            foreach ( AirplanePhysicsController controller in _controllers )
            {
                controller.MovementController.SetSimulationParameters(
                    controller.AirplanePilot.GetAcceleration(),
                    controller.AirplanePilot.GetYaw()
                );
            }

            _movementPhysicsService.Simulate( deltaTime );

            foreach ( AirplanePhysicsController controller in _controllers )
            {
                controller.ObstacleController.SetSimulationParameters(
                    controller.MovementController.LayoutPosition,
                    controller.MovementController.LayoutVelocity
                );
            }

            _obstaclesPhysicsService.Simulate( deltaTime );

            foreach ( AirplanePhysicsController controller in _controllers )
            {
                controller.Simulate( deltaTime );
            }
        }
    }
}
