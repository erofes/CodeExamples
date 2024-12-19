using System;
using Game.Base;
using BGDatabase.CodeGen;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class MovementPhysicsController
    {
        public enum EState : byte
        {
            NotInited,
            Inited,
            Disposed
        }

        private readonly AirplanePhysConfig _config;
        private readonly IGameObjectPoolService _poolService;
        private readonly ITrackService _trackService;
        private readonly MovementPhysicsModifiers _modifiers;

        private readonly SpeedReference _speedReference;
        private MovementPhysicsLayout _layout;

        private float _acceleration;
        private float _yaw;

        private AirplaneAccelerationZone _airplaneAccelerationZone;
        private AirplaneDecelerationZone _airplaneDecelerationZone;

        private readonly bool _isSrDebug;

        private Rigidbody Rigidbody => _layout.Rigidbody;

        // OLD
        private double CurrentTrackProgressNormalized => _trackService.GetTrackProgress( GetTraveledDistance() );

        private float CalculatedMaxHorizontalSpeed
        {
            get
            {
                float value;

                if ( _isSrDebug )
                {
                    if ( SROptions.Current.PlayerMaxHorizontalSpeedConfig <= 0.01f )
                        SROptions.Current.PlayerMaxHorizontalSpeedConfig = _config.MaxHorizontalSpeed;

                    value = SROptions.Current.PlayerMaxHorizontalSpeedConfig;

                    SROptions.Current.PlayerMaxHorizontalSpeed = value;
                }
                else
                    value = _config.MaxHorizontalSpeed;

                return value;
            }
        }

        private float CalculatedHorizontalAccelerationForce
        {
            get
            {
                float value;

                if ( _isSrDebug )
                {
                    if ( SROptions.Current.PlayerHorizontalAccelerationForceConfig <= 0.01f )
                        SROptions.Current.PlayerHorizontalAccelerationForceConfig = _config.HorizontalAccelerationForce;

                    value = SROptions.Current.PlayerHorizontalAccelerationForceConfig;

                    SROptions.Current.PlayerHorizontalAccelerationForce = value;
                }
                else
                    value = _config.HorizontalAccelerationForce;

                return value;
            }
        }

        public EState State { get; private set; }

        public Vector3 LayoutPosition
        {
            get => Rigidbody.position;
            private set
            {
                Rigidbody.position = value;
                _layout.transform.position = value;
            }
        }

        public Vector3 LayoutVelocity
        {
            get => Rigidbody.linearVelocity;
            private set
            {
                Rigidbody.linearVelocity = value;
                if ( _isSrDebug )
                    SROptions.Current.PlayerSpeed = Rigidbody.linearVelocity.z;
            }
        }

        public MovementPhysicsController(
            AirplanePhysConfig config,
            IGameObjectPoolService poolService,
            SpeedReference speedReference,
            ITrackService trackService,
            Transform layoutParent,
            string pilotName,
            bool isSrDebug
        )
        {
            _config = config;
            _poolService = poolService;
            _speedReference = speedReference;
            _trackService = trackService;
            _layout = _poolService.Get< MovementPhysicsLayout >( config.MovementAssetKey, null );

            _isSrDebug = isSrDebug;
            _modifiers = new MovementPhysicsModifiers( _config, _isSrDebug );

            _speedReference.BaseSpeed = _modifiers.CalculatedBaseSpeed;
            _speedReference.MaxSpeed = _modifiers.CalculatedMaxSpeed;
            _speedReference.LinearSpeed.Reset( _modifiers.CalculatedBaseSpeed.Value );

            InitLayout( layoutParent, pilotName );

            State = EState.Inited;
        }

        public void SetSimulationParameters( float acceleration, float yaw )
        {
            _acceleration = Mathf.Clamp01( acceleration );
            _yaw = Mathf.Clamp( yaw, -1f, 1f );
        }

        public void ResetSimulation( Vector3 movementPosition, Vector3? movementVelocity = null )
        {
            _yaw = 0;
            _acceleration = 0;

            LayoutPosition = movementPosition;
            LayoutVelocity = movementVelocity ?? Vector3.forward * _modifiers.CalculatedBaseSpeed.Value;
        }

        public void Dispose()
        {
            if ( State == EState.Disposed )
                return;

            State = EState.Disposed;

            if ( _layout == null || _layout.gameObject == null )
                return;

            _poolService.TryReturnToPool( _layout.gameObject );
            _layout = null;
        }

        public void Simulate( float deltaTime )
        {
            if ( State != EState.Inited )
                return;

            Vector3 velocity = LayoutVelocity;
            Vector3 force = Vector3.zero;

            ResetModifiers();
            CalculateAccelerationZone( deltaTime, _airplaneAccelerationZone );
            CalculateDecelerationZone( deltaTime, _airplaneDecelerationZone );
            CalculateSpeed( ref force );
            CalculateStrafe( ref velocity, ref force );
            CalculateBounds( ref velocity, deltaTime );

            LayoutVelocity = velocity;
            Rigidbody.AddForce( force );

            _speedReference.BaseSpeed = _modifiers.CalculatedBaseSpeed;
            _speedReference.MaxSpeed = _modifiers.CalculatedMaxSpeed;
            _speedReference.LinearSpeed.SetNext( GetCurrentSpeed() );
        }

        private void InitLayout( Transform layoutParent, string pilotName )
        {
            _layout.name = _layout.OriginName + "  " + pilotName;

            _layout.transform.SetParent( layoutParent );
            Rigidbody.mass = _config.Mass;
            LayoutPosition = Vector3.zero;
            Rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            Rigidbody.linearDamping = 0f;
            Rigidbody.useGravity = false;

            foreach ( Transform item in _layout.GetComponentsInChildren< Transform >() )
            {
                item.gameObject.layer = SharedConstants.Layers.AirplaneMovement;
            }
        }

        private void ResetModifiers()
        {
            _modifiers.ResetModifiers();
        }

        private void CalculateAccelerationZone( float deltaTime, AirplaneAccelerationZone accelerationZone )
        {
            bool isInsideZone = _trackService.TryGetAccelerationZone(
                CurrentTrackProgressNormalized,
                out AccelerationZoneSerializedBGConfig zone
            );

            if ( isInsideZone )
            {
                if ( accelerationZone == null )
                {
                    accelerationZone = new AirplaneAccelerationZone( zone );
                }
                else if ( !accelerationZone.IsSameZone( zone ) )
                {
                    accelerationZone?.Dispose();
                    accelerationZone = new AirplaneAccelerationZone( zone );
                }
            }

            if ( accelerationZone != null )
            {
                accelerationZone.SetSimulationParameters( isInsideZone );
                accelerationZone.Simulate( deltaTime );
                accelerationZone.AddModifiers( _modifiers );
            }
        }

        private void CalculateDecelerationZone( float deltaTime, AirplaneDecelerationZone decelerationZone )
        {
            bool isInsideZone = _trackService.TryGetDecelerationZone(
                CurrentTrackProgressNormalized,
                out DecelerationZoneSerializedBGConfig zone
            );

            if ( isInsideZone )
            {
                if ( decelerationZone == null )
                {
                    decelerationZone = new AirplaneDecelerationZone( zone );
                }
                else if ( !decelerationZone.IsSameZone( zone ) )
                {
                    decelerationZone?.Dispose();
                    decelerationZone = new AirplaneDecelerationZone( zone );
                }
            }

            if ( decelerationZone != null )
            {
                decelerationZone.SetSimulationParameters( isInsideZone );
                decelerationZone.Simulate( deltaTime );
                decelerationZone.AddModifiers( _modifiers );
            }
        }

        private void CalculateSpeed( ref Vector3 force )
        {
            float currentSpeed = GetCurrentSpeed();
            float currentSpeedNorm = _modifiers.GetCurrentSpeedNorm( currentSpeed );
            float targetSpeed = _modifiers.GetTargetSpeedNorm( _acceleration );
            float sign = Mathf.Sign( targetSpeed - currentSpeed );

            float maxAcceleration = _modifiers.GetAcceleration(
                sign > 0f,
                currentSpeedNorm
            );

            float scalarForce = _modifiers.GetScalarForce( maxAcceleration, targetSpeed, currentSpeed );
            force += Vector3.forward * scalarForce * sign;
        }

        private float GetCurrentSpeed()
        {
            return LayoutVelocity.z;
        }

        private float GetTraveledDistance()
        {
            return LayoutPosition.z;
        }

        private void CalculateStrafe( ref Vector3 velocity, ref Vector3 force )
        {
            Vector3 position = LayoutPosition;
            float offset = Math.Abs( position.x );
            float positionSign = Mathf.Sign( position.x );

            float currentSpeed = velocity.z;
            float currentSpeedNorm = _modifiers.GetCurrentSpeedNorm( currentSpeed );

            float currentStrafe = velocity.x;

            float targetStrafe = CalculatedMaxHorizontalSpeed * _yaw * _config.HorizontalSpeedCurve.Evaluate( currentSpeedNorm );
            float strafeDiff = Math.Abs( targetStrafe - currentStrafe );
            float strafeDiffNorm = strafeDiff / CalculatedMaxHorizontalSpeed;
            float strafeSign = Mathf.Sign( targetStrafe - currentStrafe );

            float maxForce = CalculatedHorizontalAccelerationForce * _config.HorizontalSpeedCurve.Evaluate( currentSpeedNorm );

            float scalarForce =
                maxForce *
                EasingFunction
                    .GetEasingFunction( EasingFunction.Ease.Linear )
                    .Invoke( 0, 1, strafeDiffNorm );

            if ( IsCoDirected( strafeSign, positionSign ) )
            {
                float halfTrackWidth = _trackService.TrackWidth / 2f;
                float softBorderMin = halfTrackWidth - _trackService.SoftBorderThickness;
                float softBorderMax = halfTrackWidth;
                float positionInSoftBorderNorm = Mathf.InverseLerp( softBorderMin, softBorderMax, offset );
                positionInSoftBorderNorm = Mathf.Clamp01( positionInSoftBorderNorm );

                float positionInSoftBorderFactor =
                    EasingFunction
                        .GetEasingFunction( EasingFunction.Ease.EaseInCubic )
                        .Invoke( 0f, 1f, positionInSoftBorderNorm );

                scalarForce *= 1f - positionInSoftBorderFactor;
            }

            force += Vector3.right * scalarForce * strafeSign;
        }

        private void CalculateBounds( ref Vector3 velocity, float deltaTime )
        {
            Vector3 position = LayoutPosition;
            float offset = Math.Abs( position.x );
            float sign = Mathf.Sign( position.x );

            float strafe = velocity.x;

            if ( IsCoDirected( strafe, sign ) )
            {
                float halfTrackWidth = _trackService.TrackWidth / 2f;

                float softBorderMin = halfTrackWidth - _trackService.SoftBorderThickness;
                float softBorderMax = halfTrackWidth;

                float positionInSoftBorderNorm = Mathf.InverseLerp( softBorderMin, softBorderMax, offset );
                positionInSoftBorderNorm = Mathf.Clamp01( positionInSoftBorderNorm );

                float positionInSoftBorderFactor =
                    EasingFunction
                        .GetEasingFunction( EasingFunction.Ease.EaseInCubic )
                        .Invoke( 0f, 1f, positionInSoftBorderNorm );

                float lerpFactor = positionInSoftBorderFactor * _trackService.SoftBorderLerpFactor * deltaTime;
                strafe = Mathf.Lerp( strafe, 0f, lerpFactor );
                velocity = velocity.SetX( strafe );
            }
        }

        private static bool IsCoDirected( float a, float b )
        {
            return a * b > 0f;
        }

        private static float Linear( float value )
        {
            return value;
        }
    }
}
