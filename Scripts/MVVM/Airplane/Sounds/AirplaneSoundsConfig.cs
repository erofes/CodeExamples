using System;
using Game.Audio.Identifiers;
using Game.Core;
using UnityEngine;

namespace Game.Airplane
{
    [ Serializable ]
    public class AirplaneSamplesDictionary : UnitySerializedDictionary< AirplaneViewConfigId, AudioEventID > { }

    [ CreateAssetMenu(
        fileName = nameof( AirplaneSoundsConfig ),
        menuName = MenuNames.Game + nameof( AirplaneSoundsConfig )
    ) ]
    public class AirplaneSoundsConfig : ScriptableObject
    {
        public AirplaneSamplesDictionary AirplaneEngineSamplesDict;
    }
}
