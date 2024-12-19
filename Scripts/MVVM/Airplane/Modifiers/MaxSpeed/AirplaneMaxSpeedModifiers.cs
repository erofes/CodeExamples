using System.Collections.Generic;

namespace Game.Airplane
{
    public class AirplaneMaxSpeedModifiers
    {
        private readonly List< AirplaneMaxSpeedModifier > _modifiers = new List< AirplaneMaxSpeedModifier >();

        public void AddModifier( AirplaneMaxSpeedModifier modifier )
        {
            _modifiers.Add( modifier );
        }

        public void RemoveModifier( AirplaneMaxSpeedModifier modifier )
        {
            _modifiers.Remove( modifier );
        }

        public void RemoveModifiers()
        {
            _modifiers.Clear();
        }

        public AirplaneMaxSpeedModifable Modify( in AirplaneMaxSpeedModifable modifable )
        {
            float factor = 1f;
            foreach ( AirplaneMaxSpeedModifier modifier in _modifiers )
            {
                factor += modifier.Value;
            }

            return new AirplaneMaxSpeedModifable( modifable.Value * factor );
        }
    }
}
