using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unisave.Components
{
    [ExecuteInEditMode]
    public class TutorialController : MonoBehaviour
    {
        public string tutorialName;
        
        void OnEnable()
        {
            // basically just call
            // TutorialEditorWindow.ShowTutorial(tutorialName)
            // but using a ton of reflection
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type window = assembly.GetType(
                    "Unisave.Editor.Tutorial.TutorialEditorWindow"
                );

                if (window == null)
                    continue;
                
                var methodInfo = window.GetMethod(
                    "ShowTutorial",
                    BindingFlags.Public |
                    BindingFlags.Static
                );
                
                methodInfo.Invoke(null, new object[] {tutorialName});
            }
        }
    }
}