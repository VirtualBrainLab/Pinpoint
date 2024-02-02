using System;
using System.Collections.Generic;
using System.Text;
using LightJson;
using NUnit.Framework;
using Unisave.Arango;
using Unisave.Contracts;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Foundation;
using Unisave.Logging;
using Unisave.Runtime;
using Unisave.Sessions;
using Microsoft.Owin;

namespace Unisave.Testing
{
    /// <summary>
    /// Base class for Unisave backend tests
    /// </summary>
    [Obsolete("Use fullstack fixtures instead. This hacking fails for broadcasting etc...")]
    public abstract partial class BackendTestCase
    {
        /// <summary>
        /// The client application behind client facades
        /// </summary>
        protected ClientApplication ClientApp { get; private set; }
        
        /// <summary>
        /// The server application behind server facades
        /// </summary>
        protected BackendApplication App { get; private set; }

        private RequestContext fakeGlobalRequestContext;
        
        [SetUp]
        public virtual void SetUp()
        {
            // create testing client application
            ClientApp = new ClientApplication(
                UnisavePreferences.Resolve()
            );
            
            // prepare environment
            var env = new EnvStore();
            DownloadEnvFile(env);
            
            // create testing backend application
            App = BackendApplication.Start(
                GetGameAssemblyTypes(),
                env
            );
            
            // execute backend code locally
            ClientApp.Services.RegisterSingleton<FacetCaller>(
                _ => new TestingFacetCaller(App, ClientApp)
            );
            
            // logging should go direct, we don't want to wait for app disposal
            // for writing logs to special values
            // HACK: this is a hack, see the ClientSideLog class for more
            App.Services.RegisterSingleton<ILog>(_ => new ClientSideLog());
            
            // bind facades
            ClientFacade.SetApplication(ClientApp);
            
            // start with a blank slate
            ClientApp.Services.Resolve<ClientSessionIdRepository>().StoreSessionId(null);
            ClearDatabase();
            
            // create a fake request context for the test itself,
            // so that we can access the database and other facades
            fakeGlobalRequestContext = new RequestContext(
                App.Services,
                new OwinContext()
            );
        }

        [TearDown]
        public virtual void TearDown()
        {
            fakeGlobalRequestContext.Dispose();
            
            ClientFacade.SetApplication(null);
            
            App.Dispose();
        }

        private void DownloadEnvFile(EnvStore env)
        {
            // TODO: the .env file will be downloaded from the cloud
            
            // TODO: cache the env file between individual test runs
            // (download only once per the test suite execution
            // - use SetUpFixture, or OneTimeSetup)
            // cache only the downloaded string, but reset env for each test

            // just some muffling to prevent github scraping,
            // but really, it's just a public database for testing
            // (I really should set up something better,
            // but it's just for testing...)
            string url = Encoding.UTF8.GetString(
                Convert.FromBase64String("aHR0cHM6Ly9hcmFuZ28udW5pc2F2ZS5jbG91ZC8=")
            );
            string name = Encoding.UTF8.GetString(
                Convert.FromBase64String("YXNzZXRfdGVzdGluZw==")
            );
            string p = Encoding.UTF8.GetString(
                Convert.FromBase64String("cGFzc3dvcmQ=")
            );
            
            env["SESSION_DRIVER"] = "arango";
            env["ARANGO_DRIVER"] = "http";
            env["ARANGO_BASE_URL"] = url;
            env["ARANGO_DATABASE"] = name;
            env["ARANGO_USERNAME"] = name;
            env["ARANGO_PASSWORD"] = p;
        }
        
        private Type[] GetGameAssemblyTypes()
        {
            // NOTE: gets all possible types, since there might be asm-def files
            // that makes the situation more difficult
            
            List<Type> types = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                types.AddRange(asm.GetTypes());
            }

            return types.ToArray();
        }

        private void ClearDatabase()
        {
            var arango = (ArangoConnection)App.Services.Resolve<IArango>();
            
            JsonArray collections = arango.Get("/_api/collection")["result"];

            foreach (var c in collections)
            {
                if (c["isSystem"].AsBoolean)
                    continue;
                
                arango.DeleteCollection(c["name"]);
            }
        }
    }
}