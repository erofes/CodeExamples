using System.Collections.Generic;

namespace Game.Airplane
{
    public class AirplaneAccelerationForceModifiers
    {
        private readonly List< AirplaneAccelerationForceModifier > _modifiers = new List< AirplaneAccelerationForceModifier >();

        public void AddModifier( AirplaneAccelerationForceModifier modifier )
        {
            _modifiers.Add( modifier );
        }

        public void RemoveModifier( AirplaneAccelerationForceModifier modifier )
        {
            _modifiers.Remove( modifier );
        }

        public void RemoveModifiers()
        {
            _modifiers.Clear();
        }

        public AirplaneAccelerationForceModifable Modify( in AirplaneAccelerationForceModifable modifable )
        {
            float factor = 1f;
            foreach ( AirplaneAccelerationForceModifier modifier in _modifiers )
            {
                factor += modifier.Value;
            }

            return new AirplaneAccelerationForceModifable( modifable.Value * factor );
        }
    }
}
