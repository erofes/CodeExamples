using Game.Airplane.Utilities;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class MovementPhysicsLayout : MonoBehaviour
    {
        public Rigidbody Rigidbody;

        public string OriginName { get; private set; }

        private void Awake()
        {
            OriginName = name.WithoutCloneNamePart();
        }
    }
}
