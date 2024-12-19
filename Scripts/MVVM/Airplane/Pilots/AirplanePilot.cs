using System;

namespace Game.Airplane
{
    public abstract class AirplanePilot : IAirplanePilot
    {
        public event Action OnDisposed;

        public bool HasDisposed { get; private set; }
        public IAirplaneLogic CurrentAirplane { get; private set; }
        public ITrackService TrackService { get; private set; }
        public abstract bool IsUser { get; }
        public abstract string DebugName { get; }

        public void InitializeFlight( IAirplaneLogic logic, ITrackService trackService )
        {
            CurrentAirplane = logic;
            TrackService = trackService;
        }

        public abstract BotConfig BotConfig { get; }
        protected abstract void OnDispose();
        public abstract float GetYaw();
        public abstract float GetAcceleration();

        public virtual void Dispose()
        {
            if ( HasDisposed )
                return;

            HasDisposed = true;

            CurrentAirplane = null;
            TrackService = null;

            OnDispose();
            OnDisposed?.Invoke();
        }

        public void SaveData( out PilotSaveData data )
        {
            data = new PilotSaveData( IsUser, BotConfig );
        }
    }
}
