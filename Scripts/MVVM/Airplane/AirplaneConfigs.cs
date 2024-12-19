using System.Collections.Generic;
using BGDatabase.CodeGen;

namespace Game.Airplane
{
    public struct AirplaneConfigs
    {
        public static AirplaneConfig GetAirplaneId( AirplaneConfigId id )
        {
            return AirplaneConfig.FindEntity( Filter );

            bool Filter( AirplaneConfig config ) => config.ViewConfigId == id.ViewId.Id && config.PhysConfigId == id.PhysId.Id;
        }

        public static bool ContainsAirplaneId( AirplaneConfigId id )
        {
            return GetAirplaneId( id ) != default;
        }

        public static List< AirplaneConfigId > GetAirplaneIds()
        {
            List< AirplaneConfigId > ids = new List< AirplaneConfigId >();

            AirplaneConfig.ForEachEntity(
                e =>
                {
                    AirplaneConfigId airplaneConfigId = new AirplaneConfigId(
                        new AirplaneViewConfigId( e.ViewConfigId ),
                        new AirplanePhysConfigId( e.PhysConfigId )
                    );

                    ids.Add( airplaneConfigId );
                }
            );

            return ids;
        }

        public static AirplaneViewConfig GetAirplaneViewId( AirplaneViewConfigId id )
        {
            return AirplaneViewConfig.FindEntity( Filter );

            bool Filter( AirplaneViewConfig config ) => config.name == id.Id;
        }

        public static bool ContainsAirplaneViewId( AirplaneViewConfigId id )
        {
            return GetAirplaneViewId( id ) != default;
        }

        public static List< AirplaneViewConfigId > GetAirplaneViewIds()
        {
            List< AirplaneViewConfigId > ids = new List< AirplaneViewConfigId >();
            AirplaneViewConfig.ForEachEntity( e => ids.Add( new AirplaneViewConfigId( e.name ) ) );

            return ids;
        }

        public static AirplanePhysConfig GetAirplanePhysIds( AirplanePhysConfigId id )
        {
            return AirplanePhysConfig.FindEntity( Filter );

            bool Filter( AirplanePhysConfig config ) => config.name == id.Id;
        }

        public static bool ContainsAirplanePhysId( AirplanePhysConfigId id )
        {
            return GetAirplanePhysIds( id ) != default;
        }

        public static List< AirplanePhysConfigId > GetAirplanePhysIds()
        {
            List< AirplanePhysConfigId > ids = new List< AirplanePhysConfigId >();
            AirplanePhysConfig.ForEachEntity( e => ids.Add( new AirplanePhysConfigId( e.name ) ) );

            return ids;
        }
    }
}
