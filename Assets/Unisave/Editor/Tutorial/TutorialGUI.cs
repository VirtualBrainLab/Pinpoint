using System;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.Tutorial
{
    public static class TutorialGUI
    {
        static GUIStyle paragraphStyle = new GUIStyle(EditorStyles.label) {
            wordWrap = true,
            richText = true,
            fontSize = (int)(EditorStyles.label.fontSize * 2f)
        };
        
        public static void Paragraph(string text)
        {
            GUILayout.Label(text, paragraphStyle);
        }

        public static void Form(Action content, int width = 300)
        {
            GUILayout.BeginVertical(GUILayout.Width(width));
            
            content?.Invoke();
            
            GUILayout.EndVertical();
        }
    }
}