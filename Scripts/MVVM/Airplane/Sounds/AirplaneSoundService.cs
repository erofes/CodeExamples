using System.Collections.Generic;
using Game.Audio.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Airplane
{
    public sealed partial class AirplaneSoundService : IAirplaneSoundService
    {
        private readonly IAudioService _audioService;
        private readonly IGameLoopService _gameLoopService;
        private readonly AirplaneSoundsConfig _soundsConfig;

        private IGameLoopHandler _fixedUpdateHandler;
        private IGameLoopHandler _lateUpdateHandler;

        private readonly List< AirplaneSoundController > _controllers = new List< AirplaneSoundController >();

        public AirplaneSoundService(
            IAudioService audioService,
            IGameLoopService gameLoopService,
            AirplaneSoundsConfig soundsConfig
        )
        {
            _audioService = audioService;
            _gameLoopService = gameLoopService;
            _soundsConfig = soundsConfig;

            _fixedUpdateHandler = _gameLoopService.AddFixedUpdate(
                IGameLoopService.EOrderName.AirplaneSoundService,
                HandleFixedUpdate
            );
            _lateUpdateHandler = _gameLoopService.AddLateUpdate(
                IGameLoopService.EOrderName.AirplaneSoundService,
                HandleLateUpdate
            );
        }

        public UniTask< IAirplaneSoundController > CreateController(
            AirplaneConfigId airplaneId,
            IAirplanePilot airplanePilot,
            SpeedReference speedReference,
            ITransformReference transformReference
        )
        {
            AirplaneSoundController controller = new AirplaneSoundController(
                airplaneId,
                airplanePilot,
                speedReference,
                transformReference,
                _audioService,
                _soundsConfig
            );
            _controllers.Add( controller );

            return UniTask.FromResult( ( IAirplaneSoundController )controller );
        }

        public void DestroyController( IAirplaneSoundController controller )
        {
            if ( controller == null ||
                controller.State == AirplaneSoundController.EState.Disposed )
            {
                return;
            }
            _controllers.Remove( ( AirplaneSoundController )controller );
            controller.Dispose();
        }

        private void HandleFixedUpdate( float deltaTime )
        {
            foreach ( AirplaneSoundController controller in _controllers )
            {
                controller.UpdateEngineSound();
            }
        }

        private void HandleLateUpdate( float deltaTime )
        {
            foreach ( AirplaneSoundController controller in _controllers )
            {
                controller.UpdateLocation();
            }
        }

        public void Dispose()
        {
            foreach ( AirplaneSoundController item in _controllers )
            {
                item.Dispose();
            }
            _controllers.Clear();

            _gameLoopService.RemoveFixedUpdate( _fixedUpdateHandler );
            _gameLoopService.RemoveLateUpdate( _lateUpdateHandler );
            _fixedUpdateHandler = null;
            _lateUpdateHandler = null;
        }
    }
}
