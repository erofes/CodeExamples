namespace Game.Airplane
{
    public struct AirplaneBaseSpeedModifable
    {
        public float Value { get; }

        public AirplaneBaseSpeedModifable( float value )
        {
            Value = value;
        }

        public static AirplaneBaseSpeedModifable operator *( AirplaneBaseSpeedModifable a, AirplaneBaseSpeedModifier b )
        {
            return new AirplaneBaseSpeedModifable( a.Value * b.Value );
        }
    }
}
