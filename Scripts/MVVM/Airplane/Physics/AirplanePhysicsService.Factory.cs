using UnityEngine;
using Zenject;

namespace Game.Airplane
{
    public sealed partial class AirplanePhysicsService
    {
        public sealed class Factory : PlaceholderFactory< Transform, AirplanePhysicsService > { }
    }
}
