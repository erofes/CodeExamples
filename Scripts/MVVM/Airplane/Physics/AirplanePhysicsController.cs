using UnityEngine;

namespace Game.Airplane
{
    public sealed class AirplanePhysicsController : IAirplanePhysicsController
    {
        public enum EState
        {
            NotInited,
            Inited,
            Disposed
        }

        public EState State { get; private set; }
        public IAirplanePilot AirplanePilot { get; }
        public float TraveledDistance => MovementController.LayoutPosition.z;
        public float Strafe => MovementController.LayoutPosition.x;
        public MovementPhysicsController MovementController { get; }

        public ObstaclesPhysicsController ObstacleController { get; }

        public Vector3 Position => ObstacleController.LayoutPosition;
        public Quaternion Rotation => ObstacleController.LayoutRotation;

        public AirplanePhysicsController(
            IAirplanePilot airplanePilot,
            MovementPhysicsController movementController,
            ObstaclesPhysicsController obstacleController
        )
        {
            AirplanePilot = airplanePilot;
            MovementController = movementController;
            ObstacleController = obstacleController;
        }

        public void Dispose()
        {
            if ( State == EState.Disposed )
                return;

            State = EState.Disposed;
        }

        public void Simulate( float deltaTime ) { }

        public void ResetSimulation( Vector3 movementPosition, Vector3? movementVelocity = null )
        {
            MovementController.ResetSimulation( movementPosition, movementVelocity );
            ObstacleController.ResetSimulation( movementPosition, MovementController.LayoutVelocity );

            Simulate( 0 );
        }

        public void ClearTriggeredColliders()
        {
            ObstacleController.ClearTriggeredColliders();
        }
    }
}
