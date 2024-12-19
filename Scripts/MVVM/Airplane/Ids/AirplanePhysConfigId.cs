using System;
using Game.Base;
using Newtonsoft.Json;

namespace Game.Airplane
{
    [ Serializable ]
    public struct AirplanePhysConfigId : IEquatable< AirplanePhysConfigId >
    {
        public static readonly AirplanePhysConfigId Invalid = default;

        [ JsonProperty ] public string Id;

        [ JsonIgnore ] public bool IsValid => !string.IsNullOrWhiteSpace( Id );

        public AirplanePhysConfigId( string id )
        {
            Id = id;
            Log.Assert( !string.IsNullOrWhiteSpace( Id ) );
        }

        public bool Equals( AirplanePhysConfigId other )
        {
            return Id == other.Id;
        }

        public override bool Equals( object obj )
        {
            return obj is AirplanePhysConfigId other && Equals( other );
        }

        public override int GetHashCode()
        {
            return string.IsNullOrWhiteSpace( Id ) ? 0 : Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id;
        }

        public static bool operator ==( AirplanePhysConfigId left, AirplanePhysConfigId right )
        {
            return left.Id == right.Id;
        }

        public static bool operator !=( AirplanePhysConfigId left, AirplanePhysConfigId right )
        {
            return left.Id != right.Id;
        }
    }
}
