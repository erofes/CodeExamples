namespace Game.Airplane
{
    public struct AirplaneMaxSpeedModifable
    {
        public float Value { get; }

        public AirplaneMaxSpeedModifable( float value )
        {
            Value = value;
        }

        public static AirplaneMaxSpeedModifable operator *( AirplaneMaxSpeedModifable a, AirplaneMaxSpeedModifier b )
        {
            return new AirplaneMaxSpeedModifable( a.Value * b.Value );
        }
    }
}
