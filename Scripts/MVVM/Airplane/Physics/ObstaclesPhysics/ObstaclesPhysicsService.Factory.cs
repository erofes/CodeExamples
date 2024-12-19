using UnityEngine;
using Zenject;

namespace Game.Airplane
{
    public sealed partial class ObstaclesPhysicsService
    {
        public sealed class Factory : PlaceholderFactory< Transform, ObstaclesPhysicsService > { }
    }
}
