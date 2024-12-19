using Game.Audio.Generated;
using Game.Audio.Interfaces;
using UnityEngine;

namespace Game.Airplane
{
    public class AirplaneSoundController : IAirplaneSoundController
    {
        private readonly IAudioService _audioService;
        private readonly IAudioHandler _engineSound;
        private readonly IAirplanePilot _airplanePilot;
        private readonly SpeedReference _speedReference;
        private readonly ITransformReference _transformReference;

        public AirplaneSoundController(
            AirplaneConfigId airplaneId,
            IAirplanePilot airplanePilot,
            SpeedReference speedReference,
            ITransformReference transformReference,
            IAudioService audioService,
            AirplaneSoundsConfig soundsConfig
        )
        {
            _airplanePilot = airplanePilot;
            _speedReference = speedReference;
            _transformReference = transformReference;
            _audioService = audioService;

            _engineSound = _audioService.GetAudioHandler( soundsConfig.AirplaneEngineSamplesDict[ airplaneId.ViewId ] );
            _engineSound.Play();

            State = EState.Inited;
        }

        public enum EState
        {
            NotInited,
            Inited,
            Disposed
        }

        public EState State { get; private set; }

        public void UpdateLocation()
        {
            _engineSound.SetLocation(
                _transformReference.Position,
                _speedReference.WorldSpeedDirection.Current * _speedReference.LinearSpeed.Current
            );
            if ( _airplanePilot.IsUser )
            {
                _audioService.SetListenerLocation(
                    _transformReference.Position,
                    _transformReference.Rotation,
                    _speedReference.WorldSpeedDirection.Current * _speedReference.LinearSpeed.Current
                );
            }
        }

        public void UpdateEngineSound()
        {
            _engineSound.SetFloat(
                AudioIdentifiers.speed,
                _airplanePilot.GetAcceleration()
            );
        }

        public void PlayHit()
        {
            _audioService.PlayEvent(
                AudioIdentifiers.jet_hit,
                _transformReference.Position,
                _speedReference.WorldSpeedDirection.Current * _speedReference.LinearSpeed.Current
            );
        }

        public void PlayDestroy()
        {
            _audioService.PlayEvent(
                AudioIdentifiers.jet_explode,
                _transformReference.Position,
                Vector3.zero
            );
        }

        public void Dispose()
        {
            _engineSound.Dispose();
            State = EState.Disposed;
        }
    }
}
