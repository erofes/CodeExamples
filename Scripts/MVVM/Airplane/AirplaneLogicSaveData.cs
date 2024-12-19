using System;
using UnityEngine;

namespace Game.Airplane
{
    [ Serializable ]
    public readonly struct AirplaneLogicSaveData
    {
        public readonly Vector3 MovementPosition;
        public readonly Vector3? MovementVelocity;
        public readonly AirplaneConfigId ConfigId;

        public AirplaneLogicSaveData(
            Vector3 movementPosition,
            Vector3? movementVelocity,
            AirplaneConfigId configId
        )
        {
            MovementPosition = movementPosition;
            MovementVelocity = movementVelocity;
            ConfigId = configId;
        }
    }
}
