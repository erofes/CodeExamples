using System;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class AirplaneAccelerationZone : IDisposable
    {
        public enum EState
        {
            NotInited,
            Started,
            Increasing,
            Maximized,
            Decreasing,
            Lost,
            Disposed
        }

        private float _affectionNormalized;

        private readonly Vector2 _zonePart;
        private readonly float _lerpEnterDuration;
        private readonly float _lerpExitDuration;

        private readonly float _baseSpeedFactor;
        private readonly float _maxSpeedFactor;
        private readonly float _accelerationFactor;
        private readonly float _decelerationFactor;

        public EState State { get; private set; }

        private AirplaneBaseSpeedModifier AirplaneBaseSpeedModifier =>
            new AirplaneBaseSpeedModifier( _affectionNormalized * _baseSpeedFactor );

        private AirplaneMaxSpeedModifier AirplaneMaxSpeedModifier => new AirplaneMaxSpeedModifier( _affectionNormalized * _maxSpeedFactor );

        private AirplaneAccelerationForceModifier AirplaneAccelerationForceModifier =>
            new AirplaneAccelerationForceModifier( _affectionNormalized * _accelerationFactor );

        private AirplaneDecelerationForceModifier AirplaneDecelerationForceModifier =>
            new AirplaneDecelerationForceModifier( _affectionNormalized * _decelerationFactor );

        public AirplaneAccelerationZone( AccelerationZoneSerializedBGConfig zone )
        {
            _zonePart = zone.TrackPart;
            _lerpEnterDuration = zone.ZoneLerpEnterDuration;
            _lerpExitDuration = zone.ZoneLerpExitDuration;
            _baseSpeedFactor = zone.BaseSpeedFactor;
            _maxSpeedFactor = zone.MaxSpeedFactor;
            _accelerationFactor = zone.AccelerationForceFactor;
            _decelerationFactor = zone.DecelerationForceFactor;

            if ( _lerpEnterDuration <= 0f )
                throw new GameException( "lerpEnterDuration passed has invalid value of zero or is negative!" );

            if ( _lerpExitDuration <= 0f )
                throw new GameException( "lerpExitDuration passed has invalid value of zero or is negative!" );
        }

        public void Dispose()
        {
            if ( State == EState.Disposed )
                throw new GameException( "Acceleration zone was already disposed, check for duplication code" );

            State = EState.Disposed;
        }

        public void AddModifiers( MovementPhysicsModifiers modifiers )
        {
            modifiers.AddModifier( AirplaneBaseSpeedModifier );
            modifiers.AddModifier( AirplaneMaxSpeedModifier );
            modifiers.AddModifier( AirplaneAccelerationForceModifier );
            modifiers.AddModifier( AirplaneDecelerationForceModifier );
        }

        public void SetSimulationParameters( bool isActive )
        {
            switch ( State )
            {
                case EState.NotInited:
                {
                    if ( !isActive )
                        throw new GameException( "Airplane acceleration zone not inited, you should not try to deactivate it!" );

                    State = EState.Started;

                    break;
                }
                case EState.Started:
                {
                    if ( !isActive )
                        throw new GameException( "Airplane exited zone right after enter, probably this should not ever happen" );

                    State = EState.Increasing;

                    break;
                }
                case EState.Increasing:
                case EState.Maximized:
                {
                    if ( !isActive )
                        State = EState.Decreasing;

                    break;
                }
                case EState.Decreasing:
                {
                    if ( isActive )
                        throw new GameException(
                            "Airplane somehow enters same acceleration zone, you should not apply remaining speed up, just drop it"
                        );

                    break;
                }
                case EState.Lost:
                {
                    if ( isActive )
                        throw new GameException(
                            "Trying to activate already lost effect of acceleration zone, probably your data not flushed out properly"
                        );

                    break;
                }
                case EState.Disposed:
                {
                    throw new GameException( "Acceleration zone is disposed, but you are trying to set simulation parameters for it" );
                }
                default:
                {
                    throw new NotImplementedException( "Not implemented state!" );
                }
            }
        }

        public void Simulate( float deltaTime )
        {
            switch ( State )
            {
                case EState.NotInited:
                {
                    throw new GameException( "Trying to simulate not initialized airplane acceleration zone" );
                }
                case EState.Disposed:
                {
                    throw new GameException( "Can't simulate disposed acceleration zone!" );
                }
                case EState.Started:
                case EState.Increasing:
                {
                    _affectionNormalized = Mathf.Clamp01( _affectionNormalized + deltaTime / _lerpEnterDuration );
                    if ( _affectionNormalized >= _lerpEnterDuration )
                    {
                        State = EState.Maximized;
                    }

                    break;
                }
                case EState.Decreasing:
                {
                    _affectionNormalized = Mathf.Clamp01( _affectionNormalized - deltaTime / _lerpExitDuration );
                    if ( _affectionNormalized <= 0f )
                    {
                        State = EState.Lost;
                    }

                    break;
                }
                case EState.Maximized:
                case EState.Lost:
                    break;
                default: throw new NotImplementedException();
            }
        }

        public bool IsSameZone( AccelerationZoneSerializedBGConfig zonePart )
        {
            return _zonePart == zonePart.TrackPart;
        }
    }
}
