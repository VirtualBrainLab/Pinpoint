using System;
using System.Collections.Generic;
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

namespace Unisave.Testing
{
    /// <summary>
    /// Base class for Unisave backend tests
    /// </summary>
    public abstract partial class BackendTestCase
    {
        /// <summary>
        /// The client application behind client facades
        /// </summary>
        protected ClientApplication ClientApp { get; private set; }
        
        /// <summary>
        /// The server application behind server facades
        /// </summary>
        protected Application App { get; private set; }
        
        [SetUp]
        public virtual void SetUp()
        {
            // create testing client application
            ClientApp = new ClientApplication(
                UnisavePreferences.LoadOrCreate()
            );
            
            // prepare environment
            var env = new EnvStore();
            DownloadEnvFile(env);
            
            // create testing backend application
            App = Bootstrap.Boot(
                GetGameAssemblyTypes(),
                env,
                new SpecialValues()
            );
            
            // execute backend code locally
            ClientApp.Singleton<FacetCaller>(
                _ => new TestingFacetCaller(App, ClientApp)
            );
            
            // logging should go direct, we don't want to wait for app disposal
            // for writing logs to special values
            // HACK: this is a hack, see the ClientSideLog class for more
            App.Singleton<ILog>(_ => new ClientSideLog());
            
            // bind facades
            Facade.SetApplication(App);
            ClientFacade.SetApplication(ClientApp);
            
            // start with a blank slate
            ClientApp.Resolve<ClientSessionIdRepository>().StoreSessionId(null);
            ClearDatabase();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Facade.SetApplication(null);
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
            
            env["SESSION_DRIVER"] = "arango";
            env["ARANGO_DRIVER"] = "http";
            env["ARANGO_BASE_URL"] = "http://arango.unisave.local/";
            env["ARANGO_DATABASE"] = "db_YZtrs0Lc";
            env["ARANGO_USERNAME"] = "db_user_YZtrs0Lc";
            env["ARANGO_PASSWORD"] = "73uCMhHY7WG5stQ+gC3L24kD";
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
            var arango = (ArangoConnection)App.Resolve<IArango>();
            
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