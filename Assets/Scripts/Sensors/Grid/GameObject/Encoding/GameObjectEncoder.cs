using System.Collections.Generic;

namespace MBaske.Sensors.Grid
{
    public abstract class GameObjectEncoder : Encoder
    {
        // TODO Encoders aren't really gameobject specific...
        // Move to shared?

        public IEncodingSettings Settings
        {
            set 
            { 
                m_Settings = value;
                m_ModifiersByTag = ShapeModifier.CreateModifiers(value, m_Grid);
            }
        }
        protected IEncodingSettings m_Settings;
        protected IDictionary<string, ShapeModifier> m_ModifiersByTag;
    }
}