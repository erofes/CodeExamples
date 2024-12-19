namespace Game.Airplane
{
    public struct AirplaneDecelerationForceModifable
    {
        public float Value { get; }

        public AirplaneDecelerationForceModifable( float value )
        {
            Value = value;
        }

        public static AirplaneDecelerationForceModifable operator *( AirplaneDecelerationForceModifable a, AirplaneDecelerationForceModifier b )
        {
            return new AirplaneDecelerationForceModifable( a.Value * b.Value );
        }
    }
}
