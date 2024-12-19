using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.MVVM;
using BGDatabase.CodeGen;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Airplane
{
    public sealed partial class AirplaneViewService : IAirplaneViewService
    {
        private readonly IGameObjectPoolService _gameObjectPoolService;
        private readonly IViewBehaviourFactory _viewBehaviourFactory;
        private readonly IPreprocessorPool _preprocessorPool;
        private readonly IGameLoopService _gameLoopService;

        private readonly Dictionary< IAirplaneLogic, Data > _data = new Dictionary< IAirplaneLogic, Data >();

        private IGameLoopHandler _updateHandler;

        private readonly Transform _layoutParent;

        public AirplaneViewService(
            Transform layoutParent,
            IGameObjectPoolService gameObjectPoolService,
            IViewBehaviourFactory viewBehaviourFactory,
            IPreprocessorPool preprocessorPool,
            IGameLoopService gameLoopService
        )
        {
            _gameObjectPoolService = gameObjectPoolService;
            _viewBehaviourFactory = viewBehaviourFactory;
            _preprocessorPool = preprocessorPool;
            _gameLoopService = gameLoopService;

            _updateHandler = _gameLoopService.AddUpdate(
                IGameLoopService.EOrderName.AirplaneViewService,
                HandleOnUpdate
            );

            _layoutParent = new GameObject( nameof( AirplaneViewService ) ).transform;
            _layoutParent.SetParent( layoutParent );
        }

        public async UniTask Initialize( IProgressReceiver progressReceiver )
        {
            await UniTask.CompletedTask;
        }

        public async UniTask LogicCreated( IAirplaneLogic logic, AirplaneViewConfig airplaneViewConfig, string pilotName )
        {
            Data data = new Data();
            data.Model = new AirplaneViewModel( logic );

            AirplaneViewLayout viewLayout = await _gameObjectPoolService.GetAsync< AirplaneViewLayout >(
                airplaneViewConfig.AssetKey,
                data.Model.Position,
                data.Model.Rotation,
                null
            );

            data.Behaviour = _viewBehaviourFactory.Create< AirplaneViewBehaviour >( data.Model, viewLayout );
            data.Behaviour.InitLayout( _layoutParent, pilotName );

            data.Skin = await _preprocessorPool.GetAsync< Transform >(
                airplaneViewConfig.ModelAssetKey,
                viewLayout.gameObject.transform
            ); // TODO: Logic pos

            data.Validate();

            _data.Add( logic, data );

            await data.Behaviour.Initialize();
        }

        public void LogicDestroyed( IAirplaneLogic logic )
        {
            if ( logic.State == AirplaneLogic.EState.Disposed )
                return;

            Data data = _data[ logic ];

            if ( data.Skin != null )
                _preprocessorPool.TryReturnToPool( data.Skin );

            data.Behaviour.DeInitialize();

            _data.Remove( logic );
        }

        public void Dispose()
        {
            foreach ( KeyValuePair< IAirplaneLogic, Data > pair in _data.ToArray() )
            {
                LogicDestroyed( pair.Key );
            }

            _gameLoopService.RemoveUpdate( _updateHandler );
            _updateHandler = null;

            Object.Destroy( _layoutParent );
        }

        private void HandleOnUpdate( float deltaTime )
        {
            foreach ( Data data in _data.Values )
            {
                data.Behaviour.OnUpdate( deltaTime );
            }
        }
    }
}
