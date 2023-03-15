using Unisave.Editor.BackendUploading;
using Unisave.Foundation;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Unisave.Editor.Tutorial
{
    public class PlayerAuthenticationTutorial : Tutorial
    {
        private const string BackendFolderPath
            = "Unisave/Examples/PlayerAuthentication/Backend";
        
        private UnisavePreferences preferences;
        
        public PlayerAuthenticationTutorial()
        {
            AddSlide(IntroductionSlide);
            AddSlide(RegistrationSlide);
            AddSlide(TokensSlide);
            AddSlide(WhatNextSlide);
        }

        public override bool ShouldCloseImmediately()
        {
            if (preferences == null)
                preferences = UnisavePreferences.LoadOrCreate();

            return preferences.BackendFolder == BackendFolderPath;
        }

        private void IntroductionSlide()
        {
            TutorialGUI.Paragraph(
                "Welcome to the player authentication example scene. " +
                "Before you start exploring you need to set up the cloud " +
                "part of the example. Please follow this the quick tutorial " +
                "on how to do so."
            );
                    
            TutorialGUI.Paragraph(
                "Click the 'Next >>' below to go the next slide."
            );
        }

        private void RegistrationSlide()
        {
            TutorialGUI.Paragraph(
                "Register yourself at the Unisave web to be able to access " +
                "the web app."
            );
                    
            TutorialGUI.Form(() => {
                if (GUILayout.Button("Open https://unisave.cloud/"))
                    Application.OpenURL("https://unisave.cloud/");
            });
                    
            TutorialGUI.Paragraph(
                "Then create a new game in the web app."
            );
        }

        private void TokensSlide()
        {
            TutorialGUI.Paragraph(
                "Copy the game token and editor key you will see after " +
                "game creation into these fields and click the button."
            );
            
            TutorialGUI.Form(() => {
                if (preferences == null)
                    preferences = UnisavePreferences.LoadOrCreate();
                
                preferences.GameToken = EditorGUILayout.TextField(
                    "Game token", preferences.GameToken
                );
                preferences.EditorKey = EditorGUILayout.TextField(
                    "Editor key", preferences.EditorKey
                );
                
                if (GUILayout.Button("Save preferences & upload the backend"))
                {
                    preferences.BackendFolder = BackendFolderPath;
                    preferences.Save();
                    
                    // Uploader
                    //     .GetDefaultInstance()
                    //     .UploadBackend(
                    //         verbose: true,
                    //         useAnotherThread: true
                    //     );
                }
            });
        }

        private void WhatNextSlide()
        {
            TutorialGUI.Paragraph(
                "Now you can freely explore the example, it should be " +
                "all configured to talk with the cloud as the game " +
                "you've just created."
            );
            
            TutorialGUI.Paragraph(
                "- try registering players and logging in"
            );
            
            TutorialGUI.Paragraph(
                "- open the database and view the registered players"
            );
            
            TutorialGUI.Paragraph(
                "- this example was created from the <i>Email " +
                "authentication</i> template. Go explore its documentation " +
                "and learn more at:"
            );

            TutorialGUI.Form(() => {
                string url = "https://unisave.cloud/docs/email-authentication";
                if (GUILayout.Button(url))
                    Application.OpenURL(url);
            });
        }
    }
}