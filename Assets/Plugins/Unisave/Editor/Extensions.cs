using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unisave.Editor
{
    public static class Extensions
    {
        // Code taken from:
        // https://answers.unity.com/questions/960413/editor-window-how-to-center-a-window.html
        
        private static Type[] GetAllDerivedTypes(
            this AppDomain aAppDomain,
            Type aType
        )
        {
            var result = new List<Type>();
            var assemblies = aAppDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.IsSubclassOf(aType))
                        result.Add(type);
                }
            }
            return result.ToArray();
        }
 
        public static Rect GetEditorMainWindowPos()
        {
            var containerWinType = AppDomain.CurrentDomain
                .GetAllDerivedTypes(typeof(ScriptableObject))
                .FirstOrDefault(t => t.Name == "ContainerWindow");
            if (containerWinType == null)
                throw new MissingMemberException(
                    "Can't find internal type ContainerWindow. Maybe " +
                    "something has changed inside Unity"
                );
            
            var showModeField = containerWinType.GetField(
                "m_ShowMode",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );
            var positionProperty = containerWinType.GetProperty(
                "position",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance
            );
            if (showModeField == null || positionProperty == null)
                throw new MissingFieldException(
                    "Can't find internal fields 'm_ShowMode' or 'position'. " +
                    "Maybe something has changed inside Unity"
                );
            
            var windows = Resources.FindObjectsOfTypeAll(containerWinType);
            foreach (var win in windows)
            {
                var showmode = (int)showModeField.GetValue(win);
                if (showmode == 4) // main window
                {
                    var pos = (Rect)positionProperty.GetValue(win, null);
                    return pos;
                }
            }
            
            throw new NotSupportedException(
                "Can't find internal main window. Maybe something " +
                "has changed inside Unity"
            );
        }
 
        public static void CenterOnMainWin(this UnityEditor.EditorWindow aWin)
        {
            try
            {
                var main = GetEditorMainWindowPos();
                var pos = aWin.position;
                float w = (main.width - pos.width) * 0.5f;
                float h = (main.height - pos.height) * 0.5f;
                pos.x = main.x + w;
                pos.y = main.y + h;
                aWin.position = pos;
            }
            catch (NotSupportedException e)
            {
                Debug.LogWarning(
                    "The editor window wasn't centered: " + e
                );
            }
        }
    }
}