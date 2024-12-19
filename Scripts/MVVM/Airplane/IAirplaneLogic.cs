namespace Game.Airplane
{
    public interface IAirplaneLogic : IGameplayCameraTarget
    {
        public AirplaneConfigId ConfigId { get; }
        IAirplanePhysicsController PhysicsController { get; }
        IAirplaneSoundController SoundController { get; }
        IAirplanePilot AirplanePilot { get; }
        ITransformReference InterpolatedTransformReference { get; }
        AirplaneLogic.EState State { get; }

        void SaveData( out AirplaneLogicSaveData logicSaveData );
    }
}
