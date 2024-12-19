using Game.MVVM;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class AirplaneViewBehaviour : ViewBehaviour< AirplaneViewLayout, IAirplaneViewModel >
    {
        protected override UniTask AssembleSubViewBehaviours()
        {
            return UniTask.CompletedTask;
        }

        protected override UniTask InitializeInternal()
        {
            return UniTask.CompletedTask;
        }

        protected override void DeInitializeInternal()
        {
            ViewModel.Dispose();
        }

        public void InitLayout( Transform layoutParent, string pilotName )
        {
            ViewLayout.transform.SetParent( layoutParent );
            ViewLayout.name = ViewLayout.OriginName + "  " + pilotName;
        }

        public void OnUpdate( float deltaTime )
        {
            Transform transform = ViewLayout.transform;
            transform.position = ViewModel.Position;
            transform.rotation = ViewModel.Rotation;
        }
    }
}
