using System;
using Unisave.Foundation;
using Application = UnityEngine.Application;

namespace Unisave.Facades
{
    /// <summary>
    /// Represents all client-side facades
    /// </summary>
    public static class ClientFacade
    {
        /// <summary>
        /// Application instance that should be used by facades
        /// </summary>
        public static ClientApplication ClientApp
        {
            get
            {
                // create new instance
                if (clientApp == null)
                    return CreateDefaultApplication();

                // throw away edit mode instance in favor of a play mode instance
                if (Application.isPlaying && clientApp.InEditMode)
                {
                    clientApp.Dispose();
                    clientApp = null;
                    return CreateDefaultApplication();
                }

                // return the existing instance
                return clientApp;
            }
        }

        private static ClientApplication clientApp;
        
        /// <summary>
        /// True if an application instance is set and can be used
        /// </summary>
        public static bool HasApp => clientApp != null;
        
        /// <summary>
        /// Sets the application instance to be used by facades
        /// </summary>
        public static void SetApplication(ClientApplication newApp)
        {
            clientApp = newApp;
        }

        /// <summary>
        /// Sets a new application instance created from given preferences file
        /// </summary>
        public static void SetNewFromPreferences(UnisavePreferences preferences)
        {
            if (preferences == null)
                throw new ArgumentNullException();
            
            SetApplication(new ClientApplication(preferences));
        }

        /// <summary>
        /// Unsets the app instance if the given instance is the same
        /// (called from app dispose function)
        /// </summary>
        /// <param name="app"></param>
        public static void UnsetIfEqualsGiven(ClientApplication app)
        {
            if (ReferenceEquals(app, clientApp))
                clientApp = null;
        }

        private static ClientApplication CreateDefaultApplication()
        {
            SetNewFromPreferences(UnisavePreferences.Resolve());
            
            RegisterDisposeCaller(ClientApp);

            return clientApp;
        }

        /// <summary>
        /// Registers a dispose caller for a default app instance
        /// </summary>
        /// <param name="app"></param>
        private static void RegisterDisposeCaller(ClientApplication app)
        {
            if (app.InEditMode)
            {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.playModeStateChanged += (c) => {
                        if (c == UnityEditor.PlayModeStateChange.ExitingEditMode)
                            app.Dispose();
                    };
                #endif
            }
            else
            {
                var trigger = app.GameObject
                    .GetComponent<UnisaveDisposalTrigger>();
                
                trigger.OnDisposalTriggered += app.Dispose;
            }
        }
    }
}