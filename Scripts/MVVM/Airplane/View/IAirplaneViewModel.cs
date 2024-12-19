using Game.MVVM;
using UnityEngine;

namespace Game.Airplane
{
    public interface IAirplaneViewModel : IViewModel
    {
        Vector3 Position { get; }
        Quaternion Rotation { get; }
    }
}
