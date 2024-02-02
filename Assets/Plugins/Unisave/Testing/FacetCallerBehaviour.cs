using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LightJson;
using Unisave.Entities;
using Unisave.Facets;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using Unisave.Serialization.Unisave;
using Unisave.Testing.FullstackBackend;
using UnityEngine;

namespace Unisave.Testing
{
    /// <summary>
    /// Mono behaviour script used for calling facets and other unisave
    /// operations from within test methods.
    /// Used by the <see cref="FullstackFixture"/>
    /// </summary>
    public class FacetCallerBehaviour : MonoBehaviour
    {
        // later, when facet calls can contain configuration,
        // you can add a way to set up this configuration
        // from outside this class (from inside a test method)

        #region "DB facade"
        
        public UnisaveOperation DB_Clear()
        {
            return this.CallFacet(
                (FullstackDatabaseFacet f) => f.DB_Clear()
            );
        }
        
        public async Task<TDocument> DB_Find<TDocument>(string documentId)
            where TDocument : class
        {
            JsonObject document = await this.CallFacet(
                (FullstackDatabaseFacet f) => f.DB_Find(documentId)
            );

            if (document == null)
                return null;

            return Serializer.FromJson<TDocument>(document);
        }

        public async Task<List<T>> DB_QueryGet<T>(
            string aql,
            JsonObject bindings = null
        )
        {
            List<JsonValue> values = await this.CallFacet(
                (FullstackDatabaseFacet f) => f.DB_Query(aql, bindings)
            );

            return values
                .Select(v => Serializer.FromJson<T>(v))
                .ToList();
        }
        
        public async Task<T> DB_QueryFirst<T>(
            string aql,
            JsonObject bindings = null
        )
        {
            return (await DB_QueryGet<T>(aql, bindings)).FirstOrDefault();
        }
        
        #endregion
        
        #region "Entity 'facade'"

        public async Task Entity_Save<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            Entity response = await this.CallFacet(
                (FullstackDatabaseFacet f) => f.Entity_Save(entity)
            );
            
            // fill the entity instance with refreshed values
            EntitySerializer.SetAttributes(
                entity,
                Serializer.ToJson(
                    response,
                    SerializationContext.ServerToServerStorage
                ).AsJsonObject,
                DeserializationContext.ServerStorageToServer,
                onlyFillables: false
            );
        }

        public async Task Entity_Refresh<TEntity>(TEntity entity)
            where TEntity : Entity
        {
            Entity response = await this.CallFacet(
                (FullstackDatabaseFacet f) => f.Entity_Refresh(entity)
            );

            // fill the entity instance with refreshed values
            EntitySerializer.SetAttributes(
                entity,
                Serializer.ToJson(
                    response,
                    SerializationContext.ServerToServerStorage
                ).AsJsonObject,
                DeserializationContext.ServerStorageToServer,
                onlyFillables: false
            );
        }
        
        #endregion
        
        #region "Auth facade"

        public UnisaveOperation Auth_Login(string documentId)
        {
            return this.CallFacet(
                (FullstackAuthFacet f) => f.Login(documentId)
            );
        }
        
        public UnisaveOperation<bool> Auth_Check()
        {
            return this.CallFacet(
                (FullstackAuthFacet f) => f.Check()
            );
        }

        public async Task<TPlayer> Auth_GetPlayer<TPlayer>()
            where TPlayer : class
        {
            JsonObject json = await this.CallFacet(
                (FullstackAuthFacet f) => f.GetPlayer()
            );

            return Serializer.FromJson<TPlayer>(json);
        }
        
        public UnisaveOperation Auth_Logout()
        {
            return this.CallFacet(
                (FullstackAuthFacet f) => f.Logout()
            );
        }
        
        #endregion
    }
}