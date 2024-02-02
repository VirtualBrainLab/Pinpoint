using System;
using TinyIoC;
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
    public class ClientApplication
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
        
        /// <summary>
        /// Service container
        /// </summary>
        public IContainer Services { get; }
        
        public ClientApplication(UnisavePreferences preferences)
        {
            Services = new TinyIoCAdapter(new TinyIoCContainer());
            
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
            Services.RegisterSingleton<AssetHttpClient>(
                _ => new AssetHttpClient(this)
            );
            Services.RegisterSingleton<ApiUrl>(
                _ => new ApiUrl(Preferences.ServerUrl)
            );
            Services.RegisterSingleton<ClientSessionIdRepository>(
                _ => new ClientSessionIdRepository()
            );
            Services.RegisterSingleton<DeviceIdRepository>(
                _ => new DeviceIdRepository()
            );
            Services.RegisterSingleton<FacetCaller>(
                _ => new UnisaveFacetCaller(this)
            );
            Services.RegisterSingleton<ClientBroadcastingManager>(
                _ => new ClientBroadcastingManager(this)
            );
        }
        
        public void Dispose()
        {
            Services.Dispose();
            
            ClientFacade.UnsetIfEqualsGiven(this);
            
            // NOTE: the game object will be destroyed by Unity
            GameObject = null;
        }
    }
}