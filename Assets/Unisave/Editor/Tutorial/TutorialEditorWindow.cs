using System;
using Unisave.Components;
using UnityEditor;
using UnityEngine;

namespace Unisave.Editor.Tutorial
{
    public class TutorialEditorWindow : EditorWindow
    {
        [SerializeField]
        private string tutorialName;
        
        [SerializeField]
        private int slideIndex;
        
        private Vector2 windowScroll = Vector3.zero;

        private Tutorial Tutorial
            => (tutorialName != null && TutorialRepository.tutorials.ContainsKey(tutorialName))
                ? TutorialRepository.tutorials[tutorialName] : null;
        private Action Slide => Tutorial?.slides?[slideIndex];
        private int SlideCount => Tutorial?.slides?.Count ?? 1;
        
        // called via reflection from the TutorialController.cs
        public static void ShowTutorial(string name)
        {
            var window = ShowWindow();
            window.tutorialName = name;
            window.slideIndex = 0;
            
            if (window.Tutorial != null)
                if (window.Tutorial.ShouldCloseImmediately())
                    window.Close();
        }
        
        [MenuItem("Window/Unisave/Tutorials", false, 2)]
        private static TutorialEditorWindow ShowWindow()
        {
            const int width = 400;
            const int height = 300;
            
            var window = GetWindow<TutorialEditorWindow>();
            window.titleContent = new GUIContent("Tutorial");
            window.position = new Rect(0, 0, width, height);
            window.CenterOnMainWin();
            window.Show();

            // display the list of all tutorials
            window.tutorialName = null;
            window.slideIndex = 0;

            return window;
        }

        private void OnGUI()
        {
            SanitizeState();
            
            DrawTopToolbar();
            
            windowScroll = GUILayout.BeginScrollView(windowScroll);

            if (tutorialName == null)
            {
                TutorialGUI.Paragraph(
                    "Here's the list of all available tutorials:"
                );

                foreach (var pair in TutorialRepository.tutorials)
                {
                    if (GUILayout.Button(pair.Key))
                    {
                        tutorialName = pair.Key;
                        slideIndex = 0;
                    }
                }
            }
            else
            {
                Slide?.Invoke();
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.FlexibleSpace();
            
            DrawBottomToolbar();
        }

        private void SanitizeState()
        {
            if (slideIndex < 0)
                slideIndex = 0;

            if (slideIndex >= SlideCount)
                slideIndex = SlideCount - 1;
        }

        private void DrawTopToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (tutorialName == null)
            {
                GUILayout.Label(
                    "All tutorials",
                    EditorStyles.label
                );
            }
            else
            {
                GUILayout.Label(
                    $"{tutorialName}",
                    EditorStyles.label
                );
            }
            
            GUILayout.FlexibleSpace();
            
            GUILayout.Label(
                $"Slide {slideIndex + 1}/{SlideCount}",
                EditorStyles.label
            );

            GUILayout.EndHorizontal();
        }

        private void DrawBottomToolbar()
        {
            GUILayout.BeginHorizontal(new GUIStyle {
                padding = new RectOffset(10, 10, 20, 20)
            });

            if (slideIndex > 0)
                if (GUILayout.Button("<< Previous"))
                    PreviousSlide();
            
            GUILayout.FlexibleSpace();
            
            if (slideIndex < SlideCount - 1)
                if (GUILayout.Button("Next >>"))
                    NextSlide();
            
            GUILayout.EndHorizontal();
        }

        private void NextSlide()
        {
            if (slideIndex >= SlideCount - 1)
                return;
            
            slideIndex++;
        }

        private void PreviousSlide()
        {
            if (slideIndex == 0)
                return;
            
            slideIndex--;
        }
    }
}