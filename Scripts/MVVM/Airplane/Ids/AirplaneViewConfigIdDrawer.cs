﻿#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

namespace Game.Airplane
{
    public class AirplaneViewConfigIdDrawer : OdinValueDrawer< AirplaneViewConfigId >
    {
        protected override void DrawPropertyLayout( GUIContent label )
        {
            ValueEntry.SmartValue = SirenixEditorFields.Dropdown( label, ValueEntry.SmartValue, AirplaneConfigs.GetAirplaneViewIds() );
            ValueEntry.ApplyChanges();
        }
    }
}
#endif
