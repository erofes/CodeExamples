using System.Collections.Generic;

namespace Game.Airplane
{
    public class AirplaneDecelerationForceModifiers
    {
        private readonly List< AirplaneDecelerationForceModifier > _modifiers = new List< AirplaneDecelerationForceModifier >();

        public void AddModifier( AirplaneDecelerationForceModifier modifier )
        {
            _modifiers.Add( modifier );
        }

        public void RemoveModifier( AirplaneDecelerationForceModifier modifier )
        {
            _modifiers.Remove( modifier );
        }

        public void RemoveModifiers()
        {
            _modifiers.Clear();
        }

        public AirplaneDecelerationForceModifable Modify( in AirplaneDecelerationForceModifable modifable )
        {
            float factor = 1f;
            foreach ( AirplaneDecelerationForceModifier modifier in _modifiers )
            {
                factor += modifier.Value;
            }

            return new AirplaneDecelerationForceModifable( modifable.Value * factor );
        }
    }
}
