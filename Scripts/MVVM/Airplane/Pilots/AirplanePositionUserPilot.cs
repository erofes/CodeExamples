using Game.Base;
using UnityEngine;

namespace Game.Airplane
{
    public class AirplanePositionUserPilot : AirplanePilot
    {
        private readonly IAirplanePositionUserInput _airplanePositionUserInput;

        public override string DebugName => "UserPositionPilot";
        public override BotConfig BotConfig => null;
        public override bool IsUser => true;

        public AirplanePositionUserPilot( IAirplanePositionUserInput airplanePositionUserInput )
        {
            _airplanePositionUserInput = airplanePositionUserInput;
        }

        protected override void OnDispose() { }

        public override float GetYaw()
        {
            float currentPosition = GetCurrentPosition();
            float targetPosition = GetTargetPosition();
            float delta = targetPosition - currentPosition;
            float abs = Mathf.Abs( delta );
            float sign = Mathf.Sign( delta );
            float yaw = EasingFunction
                .GetEasingFunction( EasingFunction.Ease.EaseOutCubic )
                .Invoke( 0f, 1f, abs );

            return yaw * sign;
        }

        public override float GetAcceleration()
        {
            return _airplanePositionUserInput.GetAcceleration();
        }

        private float GetCurrentPosition()
        {
            float position = CurrentAirplane.PhysicsController.Strafe;
            float halfWidth = TrackService.TrackWidth / 2f;

            return Mathf.InverseLerp( -halfWidth, halfWidth, position );
        }

        private float GetTargetPosition()
        {
            return _airplanePositionUserInput.GetPosition();
        }
    }
}
