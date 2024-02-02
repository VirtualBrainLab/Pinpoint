using System.Collections.Generic;
using System.Linq;
using LightJson;
using Unisave.Arango;
using Unisave.Contracts;
using Unisave.Entities;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Foundation;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using Unisave.Sessions;
using Unisave.Sessions.Storage;

namespace Unisave.Testing.FullstackBackend
{
    /// <summary>
    /// Facet used by fullstack test fixture to perform various database
    /// operations and assertions.
    /// </summary>
    public class FullstackDatabaseFacet : Facet
    {
        private readonly IContainer services;

        public FullstackDatabaseFacet(IContainer services)
        {
            this.services = services;
            
            // this facet does not modify session
            PreventSessionFromCreatingCollection();
        }
        
        private void PreventSessionFromCreatingCollection()
        {
            // automatically a per-request singleton,
            // since this is the request-scoped container
            
            services.RegisterSingleton<ISessionStorage, InMemorySessionStorage>();

            services.RegisterSingleton<ISession>(
                container => new SessionFrontend(
                    container.Resolve<ISessionStorage>(),
                    0
                )
            );
        }
        
        /// <summary>
        /// Deletes all the non-system collections
        /// </summary>
        public void DB_Clear()
        {
            var arango = (ArangoConnection) services.Resolve<IArango>();

            var collections = arango.Get("/_api/collection")["result"].AsJsonArray;

            var nonSystemCollectionNames = collections
                .Select(c => c["name"].AsString)
                .Where(c => c[0] != '_')
                .ToList();
            
            foreach (string name in nonSystemCollectionNames)
                arango.DeleteCollection(name);
        }

        public JsonObject DB_Find(string documentId)
        {
            return DB.Query(@"RETURN DOCUMENT(@id)")
                .Bind("id", documentId)
                .FirstAs<JsonObject>();
        }

        public List<JsonValue> DB_Query(string aql, JsonObject bindings)
        {
            var query = DB.Query(aql);

            if (bindings != null)
                foreach (var pair in bindings)
                    query.Bind(pair.Key, pair.Value);

            return query.Get();
        }

        public Entity Entity_Save(Entity entity)
        {
            // treat it like a server-created entity
            entity = Serializer.FromJson<Entity>(
                Serializer.ToJson<Entity>(
                    entity,
                    SerializationContext.ServerToServer
                ),
                DeserializationContext.ServerToServer
            );

            entity.Save();

            return entity;
        }
        
        public Entity Entity_Refresh(Entity entity)
        {
            // treat it like a server-created entity
            entity = Serializer.FromJson<Entity>(
                Serializer.ToJson<Entity>(
                    entity,
                    SerializationContext.ServerToServerStorage
                ),
                DeserializationContext.ServerStorageToServer
            );

            entity.Refresh();

            return entity;
        }
    }
}