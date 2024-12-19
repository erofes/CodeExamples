using System;
using System.Collections.Generic;
using BGDatabase.CodeGen;
using Dreamteck.Splines;
using UnityEngine;

namespace Game.Airplane
{
    public sealed partial class ObstaclesPhysicsController
    {
        public enum EState
        {
            NotInited,
            Inited,
            Disposed
        }

        private readonly AirplanePhysConfig _config;
        private readonly IGameObjectPoolService _poolService;
        private readonly ITrackService _trackService;
        private readonly TransformReference _transformReference;
        private readonly SpeedReference _speedReference;
        private ObstaclesPhysicsLayout _layout;
        private PhysicsData _physicsData;

        private float _roll;
        private float _yaw;

        private Rigidbody Rigidbody => _layout.Rigidbody;
        public EState State { get; private set; }
        public Collider Collider => _layout.Collider;
        public IReadOnlyList< Collider > TriggeredColliders => _layout.TriggeredColliders;

        public Vector3 LayoutPosition
        {
            get => Rigidbody.position;
            private set
            {
                Rigidbody.position = value;
                _layout.transform.position = value;
            }
        }

        public Quaternion LayoutRotation
        {
            get => Rigidbody.rotation;
            private set
            {
                Rigidbody.rotation = value;
                _layout.transform.rotation = value;
            }
        }

        public ObstaclesPhysicsController(
            AirplanePhysConfig config,
            IGameObjectPoolService poolService,
            ITrackService trackService,
            TransformReference transformReference,
            SpeedReference speedReference,
            Transform layoutParent,
            string pilotName
        )
        {
            _config = config;
            _poolService = poolService;
            _trackService = trackService;
            _transformReference = transformReference;
            _speedReference = speedReference;

            _layout = _poolService.Get< ObstaclesPhysicsLayout >( config.ObstaclesAssetKey, null );

            InitLayout( layoutParent, pilotName );
        }

        public void SetSimulationParameters(
            Vector3 movementControllerPosition,
            Vector3 movementControllerVelocity
        )
        {
            _physicsData = new PhysicsData(
                movementControllerPosition,
                movementControllerVelocity
            );
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
            ComputeNextPosition( deltaTime );

            Rigidbody.Move(
                _transformReference.Position,
                _transformReference.Rotation
            );
        }

        public void ResetSimulation( Vector3 movementControllerPosition, Vector3 movementControllerVelocity )
        {
            _yaw = 0;
            _roll = 0;

            SetSimulationParameters( movementControllerPosition, movementControllerVelocity );
            ComputeNextPosition( 0 );

            LayoutPosition = _transformReference.Position;
            LayoutRotation = _transformReference.Rotation;

            _speedReference.WorldSpeedDirection.Reset( SampleCurrentDirection() );
        }

        public void ClearTriggeredColliders()
        {
            _layout.ClearTriggeredColliders();
        }

        private double GetSplinePercentById( int id )
        {
            SplineComputer splineComputer = _trackService.Spline;
            double percent = id / ( double )( splineComputer.SampleCollection.Count - 1 );
            percent = Math.Clamp( percent, 0, 1d );

            return percent;
        }

        private Vector3 SampleCurrentDirection()
        {
            SplineComputer splineComputer = _trackService.Spline;
            double trackProgress = _trackService.GetTrackProgress( _physicsData.Travel );
            splineComputer.GetSamplingValues( trackProgress, out int id, out _ );

            Vector3 currentPos = _transformReference.Position;
            Vector3 nextPos = default;

            if ( id + 1 < splineComputer.SampleCollection.Count )
            {
                currentPos = _transformReference.Position;
                nextPos = SamplePositionOnTrack( GetSplinePercentById( id + 1 ), _physicsData.Strafe, out _ );
            }
            else
            {
                currentPos = SamplePositionOnTrack( GetSplinePercentById( id - 1 ), _physicsData.Strafe, out _ );
                nextPos = _transformReference.Position;
            }

            return ( nextPos - currentPos ).normalized;
        }

        private Vector3 SamplePositionOnTrack( double progress, float strafe, out SplineSample sample )
        {
            sample = _trackService.Spline.Evaluate( progress );
            Vector3 offset = sample.right * strafe;
            Vector3 position = sample.position + offset;

            return position;
        }

        private void ComputeNextPosition( float deltaTime )
        {
            double trackProgress = _trackService.GetTrackProgress( _physicsData.Travel );
            Vector3 position = SamplePositionOnTrack( trackProgress, _physicsData.Strafe, out SplineSample sample );

            Quaternion rotation = Quaternion.identity;
            rotation = CalculateRoll( deltaTime, _physicsData.MovementVelocity ) * rotation;
            rotation = CalculateYaw( deltaTime, _physicsData.MovementVelocity ) * rotation;
            rotation = Quaternion.LookRotation( sample.forward, sample.up ) * rotation;

            _transformReference.Position = position;
            _transformReference.Rotation = rotation;

            _speedReference.WorldSpeedDirection.SetNext( SampleCurrentDirection() );
        }

        private Quaternion CalculateYaw( float deltaTime, Vector3 movementVelocity )
        {
            float horizontalVelocityNorm = movementVelocity.x / _config.MaxHorizontalSpeed;
            float angle = _config.YawAngle * horizontalVelocityNorm;
            _yaw = Mathf.Lerp( _yaw, angle, deltaTime * _config.LerpFactorYawAngleSpeed );

            return Quaternion.Euler( 0f, _yaw, 0f );
        }

        private Quaternion CalculateRoll( float deltaTime, Vector3 movementVelocity )
        {
            float horizontalVelocityNorm = movementVelocity.x / _config.MaxHorizontalSpeed;
            float angle = _config.RollAngle * horizontalVelocityNorm * -1f;
            _roll = Mathf.Lerp( _roll, angle, deltaTime * _config.LerpFactorRollAngleSpeed );

            return Quaternion.Euler( 0f, 0f, _roll );
        }

        private void InitLayout( Transform layoutParent, string pilotName )
        {
            _layout.name = _layout.OriginName + "  " + pilotName;

            _layout.transform.SetParent( layoutParent );
            ClearTriggeredColliders();

            LayoutPosition = _transformReference.Position;
            LayoutRotation = _transformReference.Rotation;

            Rigidbody.isKinematic = true;
            _roll = 0f;
            _yaw = 0f;

            foreach ( Transform transform in _layout.GetComponentsInChildren< Transform >() )
            {
                transform.gameObject.layer = SharedConstants.Layers.AirplaneTrigger;
            }
        }
    }
}
