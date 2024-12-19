using System;
using Newtonsoft.Json;

namespace Game.Airplane
{
    [ Serializable ]
    public struct AirplaneConfigId : IEquatable< AirplaneConfigId >
    {
        public static readonly AirplaneConfigId Invalid = default;

        [ JsonProperty ] public AirplaneViewConfigId ViewId;
        [ JsonProperty ] public AirplanePhysConfigId PhysId;

        [ JsonIgnore ] public bool IsValid => ViewId.IsValid && PhysId.IsValid;

        public AirplaneConfigId( AirplaneViewConfigId viewId, AirplanePhysConfigId physId )
        {
            ViewId = viewId;
            PhysId = physId;
        }

        public bool Equals( AirplaneConfigId other )
        {
            return ViewId == other.ViewId && PhysId == other.PhysId;
        }

        public override bool Equals( object obj )
        {
            return obj is AirplaneConfigId other && Equals( other );
        }

        public override int GetHashCode()
        {
            return ViewId.GetHashCode() + PhysId.GetHashCode();
        }

        public override string ToString()
        {
            return $"view {ViewId.ToString()} phys {PhysId.ToString()}";
        }

        public static bool operator ==( AirplaneConfigId left, AirplaneConfigId right )
        {
            return left.Equals( right );
        }

        public static bool operator !=( AirplaneConfigId left, AirplaneConfigId right )
        {
            return !left.Equals( right );
        }
    }
}
