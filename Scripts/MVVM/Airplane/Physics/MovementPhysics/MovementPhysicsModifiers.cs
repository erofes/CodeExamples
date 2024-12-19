using System;
using System.Collections.Generic;
using BGDatabase.CodeGen;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class MovementPhysicsModifiers
    {
        private const float ResetConfigThreshold = 0.01f;

        private readonly AirplaneBaseSpeedModifiers _airplaneBaseSpeedModifiers = new AirplaneBaseSpeedModifiers();
        private readonly AirplaneMaxSpeedModifiers _airplaneMaxSpeedModifiers = new AirplaneMaxSpeedModifiers();
        private readonly AirplaneAccelerationForceModifiers _airplaneAccelerationForceModifiers = new AirplaneAccelerationForceModifiers();
        private readonly AirplaneDecelerationForceModifiers _airplaneDecelerationForceModifiers = new AirplaneDecelerationForceModifiers();

        private readonly AirplaneBaseSpeedModifable _airplaneBaseSpeedModifable;
        private readonly AirplaneMaxSpeedModifable _airplaneMaxSpeedModifable;
        private readonly AirplaneAccelerationForceModifable _airplaneAccelerationForceModifable;
        private readonly AirplaneDecelerationForceModifable _airplaneDecelerationForceModifable;

        private readonly AirplanePhysConfig _config;
        private readonly bool _isSrDebug;

        public AirplaneBaseSpeedModifable CalculatedBaseSpeed
        {
            get
            {
                AirplaneBaseSpeedModifable value;

                if ( _isSrDebug )
                {
                    if ( SROptions.Current.PlayerConfigBaseSpeed <= ResetConfigThreshold )
                        SROptions.Current.PlayerConfigBaseSpeed = _config.BaseSpeed;

                    value = _airplaneBaseSpeedModifiers.Modify( new AirplaneBaseSpeedModifable( SROptions.Current.PlayerConfigBaseSpeed ) );

                    SROptions.Current.PlayerBaseSpeed = value.Value;
                }
                else
                {
                    value = _airplaneBaseSpeedModifiers.Modify( _airplaneBaseSpeedModifable );
                }

                return value;
            }
        }

        public AirplaneMaxSpeedModifable CalculatedMaxSpeed
        {
            get
            {
                AirplaneMaxSpeedModifable value;

                if ( _isSrDebug )
                {
                    if ( SROptions.Current.PlayerConfigMaxSpeed <= ResetConfigThreshold )
                        SROptions.Current.PlayerConfigMaxSpeed = _config.MaxSpeed;

                    value = _airplaneMaxSpeedModifiers.Modify( new AirplaneMaxSpeedModifable( SROptions.Current.PlayerConfigMaxSpeed ) );

                    SROptions.Current.PlayerMaxSpeed = value.Value;
                }
                else
                {
                    value = _airplaneMaxSpeedModifiers.Modify( _airplaneMaxSpeedModifable );
                }

                return value;
            }
        }

        public AirplaneAccelerationForceModifable CalculatedAccelerationForce
        {
            get
            {
                AirplaneAccelerationForceModifable value;

                if ( _isSrDebug )
                {
                    if ( SROptions.Current.PlayerAccelerationForceConfig <= ResetConfigThreshold )
                        SROptions.Current.PlayerAccelerationForceConfig = _config.AccelerationForce;

                    value = _airplaneAccelerationForceModifiers.Modify(
                        new AirplaneAccelerationForceModifable( SROptions.Current.PlayerAccelerationForceConfig )
                    );

                    SROptions.Current.PlayerAccelerationForce = value.Value;
                }
                else
                {
                    value = _airplaneAccelerationForceModifiers.Modify( _airplaneAccelerationForceModifable );
                }

                return value;
            }
        }

        public AirplaneDecelerationForceModifable CalculatedDecelerationForce
        {
            get
            {
                AirplaneDecelerationForceModifable value;

                if ( _isSrDebug )
                {
                    if ( SROptions.Current.PlayerDecelerationForceConfig <= ResetConfigThreshold )
                        SROptions.Current.PlayerDecelerationForceConfig = _config.DecelerationForce;

                    value = _airplaneDecelerationForceModifiers.Modify(
                        new AirplaneDecelerationForceModifable( SROptions.Current.PlayerDecelerationForceConfig )
                    );

                    SROptions.Current.PlayerDecelerationForce = value.Value;
                }
                else
                {
                    value = _airplaneDecelerationForceModifiers.Modify( _airplaneDecelerationForceModifable );
                }

                return value;
            }
        }

        public MovementPhysicsModifiers( AirplanePhysConfig config, bool isSrDebug )
        {
            _config = config;
            _isSrDebug = isSrDebug;

            _airplaneBaseSpeedModifable = new AirplaneBaseSpeedModifable( _config.BaseSpeed );
            _airplaneMaxSpeedModifable = new AirplaneMaxSpeedModifable( _config.MaxSpeed );
            _airplaneAccelerationForceModifable = new AirplaneAccelerationForceModifable( _config.AccelerationForce );
            _airplaneDecelerationForceModifable = new AirplaneDecelerationForceModifable( _config.DecelerationForce );
        }

        public void ResetModifiers()
        {
            _airplaneBaseSpeedModifiers.RemoveModifiers();
            _airplaneMaxSpeedModifiers.RemoveModifiers();
            _airplaneAccelerationForceModifiers.RemoveModifiers();
            _airplaneDecelerationForceModifiers.RemoveModifiers();
        }

        public void AddModifier< T >( T modifier ) where T : struct
        {
            switch ( modifier )
            {
                case AirplaneBaseSpeedModifier airplaneBaseSpeedModifier:
                    _airplaneBaseSpeedModifiers.AddModifier( airplaneBaseSpeedModifier );

                    break;
                case AirplaneMaxSpeedModifier airplaneMaxSpeedModifier:
                    _airplaneMaxSpeedModifiers.AddModifier( airplaneMaxSpeedModifier );

                    break;
                case AirplaneAccelerationForceModifier airplaneAccelerationForceModifier:
                    _airplaneAccelerationForceModifiers.AddModifier( airplaneAccelerationForceModifier );

                    break;
                case AirplaneDecelerationForceModifier airplaneDecelerationForceModifier:
                    _airplaneDecelerationForceModifiers.AddModifier( airplaneDecelerationForceModifier );

                    break;
                default: throw new KeyNotFoundException( $"No cast found for {typeof( T ).Name} modifier" );
            }
        }

        public float GetCurrentSpeedNorm( float currentSpeed )
        {
            return Mathf.InverseLerp( CalculatedBaseSpeed.Value, CalculatedMaxSpeed.Value, currentSpeed );
        }

        public float GetTargetSpeedNorm( float accelerationInput )
        {
            return Mathf.Lerp( CalculatedBaseSpeed.Value, CalculatedMaxSpeed.Value, accelerationInput );
        }

        public float GetScalarForce( float maxAcceleration, float targetSpeed, float currentSpeed )
        {
            float speedDiff = Math.Abs( targetSpeed - currentSpeed );
            float speedDiffNorm = speedDiff / Math.Abs( CalculatedMaxSpeed.Value - CalculatedBaseSpeed.Value );
            float scalarForce = maxAcceleration * Linear( speedDiffNorm );

            return scalarForce;
        }

        public float GetAcceleration( bool isAccelerating, float speedNorm )
        {
            if ( isAccelerating )
                return CalculatedAccelerationForce.Value * _config.AccelerationCurve.Evaluate( speedNorm );

            return CalculatedDecelerationForce.Value * _config.DecelerationCurve.Evaluate( speedNorm );
        }

        private static float Linear( float value )
        {
            return value;
        }
    }
}
