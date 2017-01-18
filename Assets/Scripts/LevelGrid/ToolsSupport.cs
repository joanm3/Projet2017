using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace EditorSupport
{
    public static class ToolsSupport
    {

        public static bool UnityHandlesHidden
        {
            get
            {
                Type type = typeof(Tools);
                FieldInfo field = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
                return ((bool)field.GetValue(null));
            }
            set
            {
                Type type = typeof(Tools);
                FieldInfo field = type.GetField("s_Hidden", BindingFlags.NonPublic | BindingFlags.Static);
                field.SetValue(null, value);
            }
        }
    }
}