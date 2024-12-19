using System;
using UnityEngine;

namespace Game.Airplane
{
    public interface IAirplanePhysicsController : IDisposable
    {
        AirplanePhysicsController.EState State { get; }
        IAirplanePilot AirplanePilot { get; }
        Vector3 Position { get; }
        Quaternion Rotation { get; }
        float TraveledDistance { get; }
        float Strafe { get; }

        MovementPhysicsController MovementController { get; }
        ObstaclesPhysicsController ObstacleController { get; }

        void Simulate( float deltaTime );

        void ResetSimulation( Vector3 movementPosition, Vector3? movementVelocity = null );
        void ClearTriggeredColliders();
    }
}
