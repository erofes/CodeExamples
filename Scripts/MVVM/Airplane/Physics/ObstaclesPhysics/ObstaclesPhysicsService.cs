using System.Collections.Generic;
using Game.Base;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Airplane
{
    /// <summary>
    ///     Track-space simulation service to:
    ///     - detect planes collision with obstacles and regions
    ///     - works in Unity-world-space
    /// </summary>
    public sealed partial class ObstaclesPhysicsService
    {
        private readonly Transform _layoutParent;
        private readonly ITrackService _trackService;
        private readonly IGameObjectPoolService _poolService;
        private readonly IGameLoopService _gameLoopService;
        private readonly List< ObstaclesPhysicsController > _controllers = new List< ObstaclesPhysicsController >();

        public IReadOnlyList< ObstaclesPhysicsController > Controllers => _controllers;

        public ObstaclesPhysicsService(
            Transform layoutParent,
            ITrackService trackService,
            IGameObjectPoolService poolService
        )
        {
            _trackService = trackService;
            _poolService = poolService;

            _layoutParent = new GameObject( nameof( ObstaclesPhysicsService ) ).transform;
            _layoutParent.SetParent( layoutParent );
        }

        public UniTask Initialize( IProgressReceiver progressReceiver )
        {
            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            foreach ( ObstaclesPhysicsController controller in Controllers )
            {
                controller.Dispose();
            }
            _controllers.Clear();

            Object.Destroy( _layoutParent );
        }

        public ObstaclesPhysicsController CreateController(
            AirplanePhysConfig config,
            TransformReference transformReference,
            SpeedReference speedReference,
            string pilotName
        )
        {
            ObstaclesPhysicsController controller = new ObstaclesPhysicsController(
                config,
                _poolService,
                _trackService,
                transformReference,
                speedReference,
                _layoutParent,
                pilotName
            );

            _controllers.Add( controller );

            return controller;
        }

        public void DestroyController( ObstaclesPhysicsController controller )
        {
            _controllers.Remove( controller );

            controller.Dispose();
        }

        public void Simulate( float deltaTime )
        {
            foreach ( ObstaclesPhysicsController controller in Controllers )
            {
                controller.Simulate( deltaTime );
            }
        }
    }
}
