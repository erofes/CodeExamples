using System.Collections.Generic;

namespace Game.Airplane
{
    public class AirplaneBaseSpeedModifiers
    {
        private readonly List< AirplaneBaseSpeedModifier > _modifiers = new List< AirplaneBaseSpeedModifier >();

        public void AddModifier( AirplaneBaseSpeedModifier modifier )
        {
            _modifiers.Add( modifier );
        }

        public void RemoveModifier( AirplaneBaseSpeedModifier modifier )
        {
            _modifiers.Remove( modifier );
        }

        public void RemoveModifiers()
        {
            _modifiers.Clear();
        }

        public AirplaneBaseSpeedModifable Modify( in AirplaneBaseSpeedModifable modifable )
        {
            float factor = 1f;
            foreach ( AirplaneBaseSpeedModifier modifier in _modifiers )
            {
                factor += modifier.Value;
            }

            return new AirplaneBaseSpeedModifable( modifable.Value * factor );
        }
    }
}
