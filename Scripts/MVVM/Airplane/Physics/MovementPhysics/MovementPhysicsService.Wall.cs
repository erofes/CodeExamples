using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Airplane
{
    public sealed partial class MovementPhysicsService
    {
        private sealed class Wall : IDisposable
        {
            private readonly BoxCollider _box;

            public Wall( Vector3 position, Vector3 size, Transform parent )
            {
                GameObject go = new GameObject( SharedConstants.GameObjectNames.MovementWall );
                go.transform.SetParent( parent );
                go.layer = SharedConstants.Layers.AirplaneMovement;

                _box = go.AddComponent< BoxCollider >();
                _box.center = position;
                _box.size = size;
            }

            public void Dispose()
            {
                Object.Destroy( _box.gameObject );
            }
        }
    }
}
