using System.Collections.Generic;
using Game.Airplane.Utilities;
using UnityEngine;

namespace Game.Airplane
{
    public sealed class ObstaclesPhysicsLayout : MonoBehaviour
    {
        [ SerializeField ] private bool _isDrawGizmos;

        public Rigidbody Rigidbody;
        public Collider Collider;

        private readonly List< Collider > _triggeredColliders = new List< Collider >();
        private Vector3 _previousPosition;

        public string OriginName { get; private set; }
        public IReadOnlyList< Collider > TriggeredColliders => _triggeredColliders;

        private void Awake()
        {
            OriginName = name.WithoutCloneNamePart();
        }

        private void OnEnable()
        {
            ClearTriggeredColliders();
        }

        private void OnDrawGizmos()
        {
            if ( _isDrawGizmos )
            {
                Gizmos.DrawSphere( _previousPosition, 1f );
            }
        }

        private void FixedUpdate()
        {
            _previousPosition = transform.position;
        }

        private void OnTriggerEnter( Collider other )
        {
            _triggeredColliders.Add( other );
        }

        public void ClearTriggeredColliders()
        {
            _triggeredColliders.Clear();
        }
    }
}
