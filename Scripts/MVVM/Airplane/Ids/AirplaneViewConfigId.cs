using System;
using Game.Base;
using Newtonsoft.Json;

namespace Game.Airplane
{
    [ Serializable ]
    public struct AirplaneViewConfigId : IEquatable< AirplaneViewConfigId >
    {
        public static readonly AirplaneViewConfigId Invalid = default;

        [ JsonProperty ] public string Id;

        [ JsonIgnore ] public bool IsValid => !string.IsNullOrWhiteSpace( Id );

        public AirplaneViewConfigId( string id )
        {
            Id = id;
            Log.Assert( !string.IsNullOrWhiteSpace( Id ) );
        }

        public bool Equals( AirplaneViewConfigId other )
        {
            return Id == other.Id;
        }

        public override bool Equals( object obj )
        {
            return obj is AirplaneViewConfigId other && Equals( other );
        }

        public override int GetHashCode()
        {
            return string.IsNullOrWhiteSpace( Id ) ? 0 : Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id;
        }

        public static bool operator ==( AirplaneViewConfigId left, AirplaneViewConfigId right )
        {
            return left.Id == right.Id;
        }

        public static bool operator !=( AirplaneViewConfigId left, AirplaneViewConfigId right )
        {
            return left.Id != right.Id;
        }
    }
}
