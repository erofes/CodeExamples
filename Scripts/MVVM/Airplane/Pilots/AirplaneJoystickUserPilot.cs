namespace Game.Airplane
{
    public sealed class AirplaneJoystickUserPilot : AirplanePilot
    {
        private readonly IAirplaneJoystickUserInput _airplaneJoystickUserInput;

        public override bool IsUser => true;
        public override string DebugName => "UserJoystickPilot";
        public override BotConfig BotConfig => null;

        public AirplaneJoystickUserPilot( IAirplaneJoystickUserInput airplaneJoystickUserInput )
        {
            _airplaneJoystickUserInput = airplaneJoystickUserInput;
        }

        protected override void OnDispose() { }

        public override float GetYaw()
        {
            return _airplaneJoystickUserInput.GetYaw();
        }

        public override float GetAcceleration()
        {
            return _airplaneJoystickUserInput.GetAcceleration();
        }
    }
}
