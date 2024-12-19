using UnityEngine;

namespace Game.Airplane
{
    public sealed partial class ObstaclesPhysicsController
    {
        public readonly struct PhysicsData
        {
            public readonly float Travel;
            public readonly float Strafe;
            public readonly Vector3 MovementVelocity;

            public PhysicsData( Vector3 position, Vector3 velocity )
            {
                Travel = position.z;
                Strafe = position.x;
                MovementVelocity = velocity;
            }
        }
    }
}
