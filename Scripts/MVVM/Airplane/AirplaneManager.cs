using System.Collections.Generic;

namespace Game.Airplane
{
    public class AirplaneManager : IAirplaneManager
    {
        private readonly IAirplaneService _airplaneService;
        private readonly List< IAirplanePilot > _pilots = new List< IAirplanePilot >();

        public AirplaneManager( IAirplaneService airplaneService )
        {
            _airplaneService = airplaneService;
        }

        public void DisposeAirplanesAndPilots()
        {
            _airplaneService.DestroyAll();
            foreach ( IAirplanePilot pilot in _pilots )
            {
                pilot?.Dispose();
            }

            _pilots.Clear();
        }
    }
}
