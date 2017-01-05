using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ProjectGiants.Editor
{
    public class CustomProperties
    {
        private Dictionary<string, SerializedProperty> _properties = new Dictionary<string, SerializedProperty>();
        private List<CProperty> _timingProperties = new List<CProperty>();


        public void DisplayField(CProperty property)
        {
            EditorGUILayout.PropertyField(_properties[property.name], new GUIContent(property.text));
        }

        /// <summary>
        /// Add this in the OnEnable() Unity Function
        /// </summary>
        public void RefreshProperites(SerializedObject serializedObject)
        {
            _properties.Clear();
            SerializedProperty property = serializedObject.GetIterator();

            while (property.NextVisible(true))
            {
                _properties[property.name] = property.Copy();
            }
        }

        public void ClearTimingProperties()
        {
            _timingProperties.Clear();
        }

    }

    public class CProperty
    {
        public string name;
        public string text;

        public CProperty(string n, string t)
        {
            name = n;
            text = t;
        }
    }
}
