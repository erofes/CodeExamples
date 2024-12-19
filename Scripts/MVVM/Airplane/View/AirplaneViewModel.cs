using Game.MVVM;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class AirplaneViewModel : BaseViewModel, IAirplaneViewModel
    {
        private readonly IAirplaneLogic _airplaneLogic;

        public Vector3 Position => _airplaneLogic.InterpolatedTransformReference.Position;
        public Quaternion Rotation => _airplaneLogic.InterpolatedTransformReference.Rotation;

        public AirplaneViewModel( IAirplaneLogic airplaneLogic )
        {
            _airplaneLogic = airplaneLogic;
        }

        protected override void OnDispose() { }
    }
}
