using System;
using Unisave.Broadcasting;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.HttpClient;
using Unisave.Sessions;
using Unisave.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unisave.Foundation
{
    /// <summary>
    /// Contains the entire client application
    /// </summary>
    public class ClientApplication : Container
    {
        /// <summary>
        /// Preferences that should be used by the application
        /// </summary>
        public UnisavePreferences Preferences { get; }
        
        /// <summary>
        /// The UnisaveHierarchyBinder object
        /// (null when running in edit mode)
        /// </summary>
        public GameObject GameObject { get; private set; }

        /// <summary>
        /// Is this instance existing in edit mode?
        /// </summary>
        public bool InEditMode { get; }
        
        public ClientApplication(UnisavePreferences preferences)
        {
            InEditMode = !UnityEngine.Application.isPlaying;
            
            Preferences = preferences;

            if (!InEditMode)
            {
                GameObject = new GameObject(
                    "Unisave",
                    typeof(UnisaveDisposalTrigger),
                    typeof(HttpClientComponent)
                );
                Object.DontDestroyOnLoad(GameObject);
            }

            RegisterServices();
        }

        /// <summary>
        /// Registers default services
        /// </summary>
        private void RegisterServices()
        {
            Singleton<AssetHttpClient>(_ => new AssetHttpClient(this));
            
            Singleton<ApiUrl>(_ => new ApiUrl(Preferences.ServerUrl));
            
            Singleton<ClientSessionIdRepository>(_ => new ClientSessionIdRepository());
            
            Singleton<DeviceIdRepository>(_ => new DeviceIdRepository());
            
            Singleton<FacetCaller>(_ => new UnisaveFacetCaller(this));
            
            Singleton<ClientBroadcastingManager>(_ => new ClientBroadcastingManager(this));
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            ClientFacade.UnsetIfEqualsGiven(this);
            
            // NOTE: the game object will be destroyed by Unity
            GameObject = null;
        }
    }
}