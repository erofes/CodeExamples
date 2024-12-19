using Game.Airplane.Utilities;
using Game.MVVM;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class AirplaneViewLayout : ViewLayout
    {
        [ ReadOnly ] public Transform Model;
        public GameObject EngineEffect;

        public string OriginName { get; private set; }

        private void Awake()
        {
            OriginName = name.WithoutCloneNamePart();
        }
    }
}
