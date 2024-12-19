using UnityEngine;
using Zenject;

namespace Game.Airplane
{
    public sealed partial class MovementPhysicsService
    {
        public sealed class Factory : PlaceholderFactory< Transform, MovementPhysicsService > { }
    }
}
