namespace Game.Airplane
{
    public struct AirplaneAccelerationForceModifable
    {
        public float Value { get; }

        public AirplaneAccelerationForceModifable( float value )
        {
            Value = value;
        }

        public static AirplaneAccelerationForceModifable operator *( AirplaneAccelerationForceModifable a, AirplaneAccelerationForceModifier b )
        {
            return new AirplaneAccelerationForceModifable( a.Value * b.Value );
        }
    }
}
