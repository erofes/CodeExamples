using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Airplane
{
    [ CreateAssetMenu(
        fileName = nameof( BotConfig ),
        menuName = MenuNames.Game + nameof( BotConfig )
    ) ]
    public class BotConfig : ScriptableObject
    {
        [ SerializeField ] public AirplaneConfigId AirplaneConfigId;

        [ SerializeField ][ MinMaxSlider( -1f, 1f ) ]
        public Vector2 JawMinMax = new Vector2( -1f, 1f );

        [ SerializeField ][ MinMaxSlider( 0f, 1f ) ]
        public Vector2 AccelerationMinMax = new Vector2( -0f, 1f );
    }
}
