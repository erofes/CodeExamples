using UnityEngine;
using Zenject;

namespace Game.Airplane
{
    public sealed partial class AirplaneViewService
    {
        public sealed class Factory : PlaceholderFactory< Transform, AirplaneViewService > { }
    }
}
