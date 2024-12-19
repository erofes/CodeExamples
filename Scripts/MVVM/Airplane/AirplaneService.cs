using System.Collections.Generic;
using System.Linq;
using Game.Base;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Utilities.Collections;
using Progress = Game.Base.Progress;

namespace Game.Airplane
{
    public sealed class AirplaneService : IAirplaneService
    {
        private readonly IGameLoopService _gameLoopService;
        private readonly ITrackService _trackService;
        private readonly ICollisionRegistryService _collisionRegistryService;
        private readonly IAirplanePhysicsService _airplanePhysicsService;
        private readonly IAirplaneViewService _airplaneViewService;
        private readonly IAirplaneSoundService _airplaneSoundService;
        private readonly IInterpolationService _interpolationService;
        private readonly IGameObjectPoolService _gameObjectPoolService;

        private readonly List< AirplaneLogic > _logics = new List< AirplaneLogic >();
        private IGameLoopHandler _registerCollisionsHandler;

        private readonly Transform _layoutParent;

        public AirplaneService(
            AirplanePhysicsService.Factory airplanePhysicsServiceFactory,
            AirplaneViewService.Factory airplaneViewServiceFactory,
            AirplaneSoundService.Factory airplaneSoundServiceFactory,
            ITrackService trackService,
            IGameLoopService gameLoopService,
            ICollisionRegistryService collisionRegistryService,
            IInterpolationService interpolationService,
            IGameObjectPoolService gameObjectPoolService
        )
        {
            _layoutParent = new GameObject( nameof( AirplaneService ) ).transform;

            _trackService = trackService;
            _gameLoopService = gameLoopService;
            _collisionRegistryService = collisionRegistryService;
            _airplanePhysicsService = airplanePhysicsServiceFactory.Create( _layoutParent );
            _airplaneViewService = airplaneViewServiceFactory.Create( _layoutParent );
            _airplaneSoundService = airplaneSoundServiceFactory.Create();
            _interpolationService = interpolationService;
            _gameObjectPoolService = gameObjectPoolService;

            _registerCollisionsHandler = _gameLoopService.AddFixedUpdate(
                IGameLoopService.EOrderName.AirplaneRegisterCollisions,
                HandleRegisterCollisions
            );
        }

        public void Dispose()
        {
            foreach ( AirplaneLogic logic in _logics )
            {
                if ( logic.State == AirplaneLogic.EState.Disposed )
                    continue;

                _airplanePhysicsService.DestroyController( logic.PhysicsController );
                _airplaneViewService.LogicDestroyed( logic );
                logic.Dispose();
            }
            _logics.Clear();

            _airplaneViewService.Dispose();
            _airplanePhysicsService.Dispose();

            _gameLoopService.RemoveFixedUpdate( _registerCollisionsHandler );
            _registerCollisionsHandler = null;

            Object.Destroy( _layoutParent );
        }

        public async UniTask Initialize( IProgressReceiver progressReceiver )
        {
            Progress progress1 = new Progress();
            AggregatedProgress aggregatedProgress = new AggregatedProgress( progress1 );
            aggregatedProgress.AddListeners( progressReceiver );

            await _airplaneViewService.Initialize( progress1 );
        }

        public void CreateWalls()
        {
            _airplanePhysicsService.CreateWalls();
        }

        public void DestroyWalls()
        {
            _airplanePhysicsService.DestroyWalls();
        }

        public async UniTask< IAirplaneLogic > CreateLogic(
            AirplaneConfigId configId,
            IAirplanePilot airplanePilot,
            AirplaneViewConfig viewConfig,
            AirplanePhysConfig physicsConfig,
            bool isSrDebug
        )
        {
            TransformReference transformReference = new TransformReference
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity
            };

            SpeedReference speedReference = new SpeedReference();

            IAirplanePhysicsController physicsController = await _airplanePhysicsService.CreateController(
                airplanePilot,
                physicsConfig,
                speedReference,
                transformReference,
                isSrDebug
            );

            IAirplaneSoundController soundController = await _airplaneSoundService.CreateController(
                configId,
                airplanePilot,
                speedReference,
                transformReference
            );

            AirplaneLogic logic = new AirplaneLogic(
                configId,
                _collisionRegistryService,
                _interpolationService,
                airplanePilot,
                physicsController,
                soundController,
                speedReference,
                transformReference
            );

            airplanePilot.InitializeFlight( logic, _trackService );

            await _airplaneViewService.LogicCreated( logic, viewConfig, airplanePilot.DebugName );

            _logics.Add( logic );

            return logic;
        }

        public void DestroyAll()
        {
            foreach ( IAirplaneLogic airplaneLogic in GetAirplanesReversed() )
            {
                DestroyLogic( airplaneLogic );
            }
        }

        public bool IsFinishReached( IAirplaneLogic airplaneLogic )
        {
            return airplaneLogic.PhysicsController.TraveledDistance >= _trackService.TrackLength;
        }

        public ReversedEnumerable< IAirplaneLogic > GetAirplanesReversed()
        {
            return new ReversedEnumerable< IAirplaneLogic >( _logics );
        }

        public void StartGame() { }

        public void DestroyLogic( IAirplaneLogic airplaneLogic )
        {
            AirplaneLogic logic = _logics.FirstOrDefault( a => a == airplaneLogic );
            if ( logic == null )
            {
                Log.Error( "Logic does not exist!" );

                return;
            }

            _logics.Remove( logic );
            _airplanePhysicsService.DestroyController( logic.PhysicsController );
            _airplaneSoundService.DestroyController( logic.SoundController );
            _airplaneViewService.LogicDestroyed( logic );
            logic.Dispose();
        }

        public void ResetSimulation( List< AirplaneLogicSaveData > saveData = null )
        {
            IReadOnlyList< Vector3 > startMovementPositions = _trackService.GetMovementStartPoints();

            if ( saveData.IsNullOrEmpty() )
            {
                saveData = new List< AirplaneLogicSaveData >( _logics.Count );

                for ( int i = 0; i < startMovementPositions.Count; i++ )
                {
                    Vector3 position = startMovementPositions[ i ];
                    AirplaneLogic logic = _logics[ i ];

                    saveData.Add(
                        new AirplaneLogicSaveData(
                            position,
                            null,
                            logic.ConfigId
                        )
                    );
                }
            }

            for ( int i = 0; i < _logics.Count; i++ )
            {
                AirplaneLogic logic = _logics[ i ];

                AirplaneLogicSaveData airplaneLogicSaveData = saveData![ i ];
                logic.ResetSimulation( airplaneLogicSaveData.MovementPosition, airplaneLogicSaveData.MovementVelocity );
            }
        }

        public IAirplaneLogic GetUserAirplaneLogic()
        {
            foreach ( AirplaneLogic logic in _logics )
            {
                if ( logic.AirplanePilot.IsUser )
                    return logic;
            }

            return null;
        }

        public GameObject GetAirplaneView( AirplaneViewConfigId airplaneViewConfigId, string pilotName )
        {
            string assetKey = AirplaneConfigs.GetAirplaneViewId( airplaneViewConfigId ).ModelAssetKey;

            GameObject result = _gameObjectPoolService.Get( assetKey, null );
            result.name = assetKey + "  " + pilotName;
            result.transform.SetParent( _layoutParent );

            return result;
        }

        public AirplaneViewConfig GetAirplaneViewConfig( AirplaneConfigId id )
        {
            AirplaneViewConfig viewConfig = AirplaneConfigs.GetAirplaneViewId( id.ViewId );
            if ( viewConfig == null )
            {
                Log.Error( $"{nameof( AirplaneViewConfig )} for '{id}' wasn't found!" );
            }

            return viewConfig;
        }

        public AirplanePhysConfig GetAirplanePhysicsConfig( AirplaneConfigId id )
        {
            AirplanePhysConfig physicsConfig = AirplaneConfigs.GetAirplanePhysIds( id.PhysId );
            if ( physicsConfig == null )
            {
                Log.Error( $"{nameof( AirplanePhysConfig )} for '{id}' wasn't found!" );
            }

            return physicsConfig;
        }

        private void HandleRegisterCollisions( float deltaTime )
        {
            foreach ( AirplaneLogic logic in _logics )
            {
                logic.RegisterCollisions();
            }
        }
    }
}
