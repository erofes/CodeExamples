using System.Collections.Generic;
using UnityEngine;

namespace Game.Airplane
{
    [ CreateAssetMenu(
        fileName = nameof( BotsConfig ),
        menuName = MenuNames.Game + nameof( BotsConfig )
    ) ]
    public class BotsConfig : ScriptableObject
    {
        [ SerializeField ][ Range( 0, 3 ) ] public int BotsPerLevelCount;
        [ SerializeField ] public List< BotConfig > Configs = new List< BotConfig >();
    }
}
