using System.Collections.Generic;
using BGDatabase.CodeGen;
using UnityEngine;

namespace Game.Airplane
{
    /// <summary>
    ///     Linear space simulation service to:
    ///     - accelerate planes,
    ///     - collide planes with planes
    ///     - collide planes with bounds (side-movement limitation)
    ///     - works in linear space:
    ///     - x (side offset to track)
    ///     - z (length of track)
    /// </summary>
    public sealed partial class MovementPhysicsService
    {
        public enum EState
        {
            NotInited,
            Inited,
            Disposed
        }

        private readonly Transform _layoutParent;
        private readonly ITrackService _trackService;
        private readonly IGameObjectPoolService _poolService;

        private readonly List< Wall > _walls = new List< Wall >();
        private readonly List< MovementPhysicsController > _controllers = new List< MovementPhysicsController >();

        public IReadOnlyList< MovementPhysicsController > Controllers => _controllers;
        public EState State { get; private set; }

        public MovementPhysicsService( Transform layoutParent, ITrackService trackService, IGameObjectPoolService poolService )
        {
            _trackService = trackService;
            _poolService = poolService;

            State = EState.Inited;

            _layoutParent = new GameObject( nameof( MovementPhysicsService ) ).transform;
            _layoutParent.SetParent( layoutParent );
        }

        public MovementPhysicsController CreateController(
            AirplanePhysConfig config,
            SpeedReference speedReference,
            string pilotName,
            bool isSrDebug
        )
        {
            MovementPhysicsController controller = new MovementPhysicsController(
                config,
                _poolService,
                speedReference,
                _trackService,
                _layoutParent,
                pilotName,
                isSrDebug
            );

            _controllers.Add( controller );

            return controller;
        }

        public void DestroyController( MovementPhysicsController controller )
        {
            _controllers.Remove( controller );
            controller.Dispose();
        }

        public void Simulate( float deltaTime )
        {
            foreach ( MovementPhysicsController controller in _controllers )
            {
                controller.Simulate( deltaTime );
            }
        }

        public void Dispose()
        {
            if ( State == EState.Disposed )
                return;

            State = EState.Disposed;

            foreach ( MovementPhysicsController controller in _controllers )
            {
                controller.Dispose();
            }
            _controllers.Clear();

            foreach ( Wall wall in _walls )
            {
                wall.Dispose();
            }
            _walls.Clear();

            Object.Destroy( _layoutParent );
        }

        public void CreateWalls()
        {
            float width = _trackService.TrackWidth;
            float length = ( float )_trackService.TrackLength;

            float halfLength = length * 0.5f;
            float wallLength = length * 1.2f;

            Wall wallRight = new Wall(
                new Vector3( width, 0f, halfLength ),
                new Vector3( width, width, wallLength ),
                _layoutParent
            );

            Wall wallLeft = new Wall(
                new Vector3( -width, 0f, halfLength ),
                new Vector3( width, width, wallLength ),
                _layoutParent
            );

            _walls.Add( wallLeft );
            _walls.Add( wallRight );
        }

        public void DestroyWalls()
        {
            foreach ( Wall wall in _walls )
            {
                wall.Dispose();
            }
            _walls.Clear();
        }
    }
}
