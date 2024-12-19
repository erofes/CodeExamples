using Game.Base;
using UnityEngine;

namespace Game.Airplane
{
    public sealed partial class AirplaneViewService
    {
        private struct Data
        {
            public AirplaneViewBehaviour Behaviour;
            public Transform Skin;
            public IAirplaneViewModel Model;

            public void Validate()
            {
                Log.Assert( Behaviour != null && Skin != null && Model != null );
            }
        }
    }
}
