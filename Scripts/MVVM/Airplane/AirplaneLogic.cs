using System;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class AirplaneLogic : IAirplaneLogic, IDisposable
    {
        public enum EState
        {
            NotInited,
            Inited,
            Disposed
        }

        private readonly ICollisionRegistryService _collisionRegistryService;
        private readonly IInterpolationService _interpolationService;
        private readonly ICollisionRegistryOwner _collisionRegistryOwner;
        private readonly IInterpolationController _interpolationController;

        public ISpeedReference SpeedReference { get; }

        public IAirplanePhysicsController PhysicsController { get; }
        public IAirplaneSoundController SoundController { get; }
        public ITransformReference InterpolatedTransformReference => _interpolationController.InterpolatedTransformReference;
        public AirplaneConfigId ConfigId { get; }
        public IAirplanePilot AirplanePilot { get; }

        ITransformReference IGameplayCameraTarget.TransformReference => _interpolationController.InterpolatedTransformReference;
        ISpeedReference IGameplayCameraTarget.SpeedReference => SpeedReference;
        float IGameplayCameraTarget.CameraOffsetFactor => 1f;

        public EState State { get; private set; }

        public AirplaneLogic(
            AirplaneConfigId configId,
            ICollisionRegistryService collisionRegistryService,
            IInterpolationService interpolationService,
            IAirplanePilot airplanePilot,
            IAirplanePhysicsController physicsController,
            IAirplaneSoundController soundController,
            ISpeedReference speedReference,
            ITransformReference transformReference
        )
        {
            ConfigId = configId;
            AirplanePilot = airplanePilot;
            PhysicsController = physicsController;
            SoundController = soundController;
            SpeedReference = speedReference;

            _collisionRegistryService = collisionRegistryService;
            _interpolationService = interpolationService;
            _collisionRegistryOwner = _collisionRegistryService.BindColliderOwner(
                PhysicsController.ObstacleController.Collider,
                ( IAirplaneLogic )this
            );

            _interpolationController = _interpolationService.CreateInterpolation( transformReference );

            State = EState.Inited;
        }

        public void Dispose()
        {
            if ( State == EState.Disposed )
                return;

            State = EState.Disposed;

            _interpolationService.RemoveInterpolation( _interpolationController );
            _collisionRegistryService.UnbindColliderOwner( _collisionRegistryOwner, ( IAirplaneLogic )this );
        }

        public void SaveData( out AirplaneLogicSaveData logicSaveData )
        {
            MovementPhysicsController movementController = PhysicsController.MovementController;

            logicSaveData = new AirplaneLogicSaveData(
                movementController.LayoutPosition,
                movementController.LayoutVelocity,
                ConfigId
            );
        }

        public void ResetSimulation( Vector3 movementPosition, Vector3? movementVelocity = null )
        {
            PhysicsController.ResetSimulation( movementPosition, movementVelocity );
            _interpolationController.Set( PhysicsController.Position, PhysicsController.Rotation, null );
        }

        public void RegisterCollisions()
        {
            ObstaclesPhysicsController obstacleController = PhysicsController.ObstacleController;
            foreach ( Collider other in obstacleController.TriggeredColliders )
            {
                _collisionRegistryService.AddCollision( obstacleController.Collider, other );
            }

            PhysicsController.ClearTriggeredColliders();
        }
    }
}
