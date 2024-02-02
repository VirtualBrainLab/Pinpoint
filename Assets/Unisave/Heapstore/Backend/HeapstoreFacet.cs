using System;
using System.Collections.Generic;
using LightJson;
using Unisave.Arango;
using Unisave.Contracts;
using Unisave.Facades;
using Unisave.Facets;
using Unisave.Foundation;

namespace Unisave.Heapstore.Backend
{
    public class HeapstoreFacet : Facet
    {
        #region "Query API"
        
        
        /////////////////////
        // Query execution //
        /////////////////////

        public List<JsonObject> ExecuteQuery(QueryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            
            request.Validate();
            
            try
            {
                return request.BuildAqlQuery().GetAs<JsonObject>();
            }
            catch (ArangoException e) when (e.ErrorNumber == 1203)
            {
                return new List<JsonObject>();
            }
        }
        
        #endregion
        
        #region "Document API"
        
        
        ///////////////////
        // Get operation //
        ///////////////////
        
        public JsonObject GetDocument(DocumentId id)
        {
            try
            {
                return DB.Query(@"
                    RETURN DOCUMENT(@id)
                ")
                    .Bind("id", id.Id)
                    .FirstAs<JsonObject>();
            }
            catch (ArangoException e) when (e.ErrorNumber == 1203)
            {
                return null;
            }
        }
        
        
        ///////////////////
        // Set operation //
        ///////////////////
        
        public JsonObject SetDocument(
            DocumentId id,
            JsonObject document,
            bool throwIfMissing
        )
        {
            try
            {
                return TrySetDocument(id, document, throwIfMissing);
            }
            catch (ArangoException e) when (e.ErrorNumber == 1203)
            {
                if (throwIfMissing)
                {
                    // ERROR_DOCUMENT_MISSING
                    throw new HeapstoreException(
                        1000, "Setting a document that does not exist."
                    );
                }
                
                CreateCollection(id.Collection);
                return TrySetDocument(id, document, false);
            }
        }

        private JsonObject TrySetDocument(
            DocumentId id,
            JsonObject document,
            bool throwIfMissing
        )
        {
            document["_key"] = id.Key;
            document.Remove("_id");

            if (throwIfMissing)
            {
                try
                {
                    return DB.Query(@"
                    REPLACE @document IN @@collection
                    RETURN NEW
                ")
                        .Bind("document", document)
                        .Bind("@collection", id.Collection)
                        .FirstAs<JsonObject>();
                }
                catch (ArangoException e) when (e.ErrorNumber == 1202)
                {
                    // ERROR_DOCUMENT_MISSING
                    throw new HeapstoreException(
                        1000, "Setting a document that does not exist."
                    );
                }
            }
            else
            {
                return DB.Query(@"
                    INSERT @document
                        INTO @@collection
                        OPTIONS { overwrite: true }
                    RETURN NEW
                ")
                    .Bind("document", document)
                    .Bind("@collection", id.Collection)
                    .FirstAs<JsonObject>();
            }
            
        }
        
        
        //////////////////////
        // Update operation //
        //////////////////////
        
        public JsonObject UpdateDocument(
            DocumentId id,
            JsonObject document,
            bool throwIfMissing
        )
        {
            try
            {
                return TryUpdateDocument(id, document, throwIfMissing);
            }
            catch (ArangoException e) when (e.ErrorNumber == 1203)
            {
                if (throwIfMissing)
                {
                    // ERROR_DOCUMENT_MISSING
                    throw new HeapstoreException(
                        1000, "Setting a document that does not exist."
                    );
                }
                
                CreateCollection(id.Collection);
                return TryUpdateDocument(id, document, false);
            }
        }

        private JsonObject TryUpdateDocument(
            DocumentId id,
            JsonObject document,
            bool throwIfMissing
        )
        {
            document.Remove("_id");
            document.Remove("_key");

            if (throwIfMissing)
            {
                try
                {
                    return DB.Query(@"
                        UPDATE @key WITH @document IN @@collection
                        RETURN NEW
                    ")
                        .Bind("document", document)
                        .Bind("key", id.Key)
                        .Bind("@collection", id.Collection)
                        .FirstAs<JsonObject>();
                }
                catch (ArangoException e) when (e.ErrorNumber == 1202)
                {
                    // ERROR_DOCUMENT_MISSING
                    throw new HeapstoreException(
                        1000, "Setting a document that does not exist."
                    );
                }
            }
            else
            {
                return DB.Query(@"
                        UPSERT { _key: @key }
                            INSERT MERGE(@document, { _key: @key })
                            UPDATE @document
                        IN @@collection
                        RETURN NEW
                ")
                    .Bind("document", document)
                    .Bind("key", id.Key)
                    .Bind("@collection", id.Collection)
                    .FirstAs<JsonObject>();
            }
        }


        ///////////////////
        // Add operation //
        ///////////////////
        
        public JsonObject AddDocument(
            string collection,
            JsonObject document,
            bool throwIfMissing
        )
        {
            try
            {
                return TryAddDocument(collection, document);
            }
            catch (ArangoException e) when (e.ErrorNumber == 1203)
            {
                if (throwIfMissing)
                {
                    // ERROR_COLLECTION_MISSING
                    throw new HeapstoreException(
                        1001,
                        "Adding a document to a collection that does not exist."
                    );
                }
                
                CreateCollection(collection);
                return TryAddDocument(collection, document);
            }
        }

        private JsonObject TryAddDocument(
            string collection,
            JsonObject document
        )
        {
            document.Remove("_id");
            document.Remove("_key");
            document.Remove("_rev");
            
            return DB.Query(@"
                INSERT @document INTO @@collection
                RETURN NEW
            ")
                .Bind("document", document)
                .Bind("@collection", collection)
                .FirstAs<JsonObject>();
        }
        
        //////////////////////
        // Delete operation //
        //////////////////////

        public bool DeleteDocument(DocumentId id)
        {
            try
            {
                DB.Query(@"
                    REMOVE @key IN @@collection
                ")
                    .Bind("key", id.Key)
                    .Bind("@collection", id.Collection)
                    .Run();
                
                return true;
            }
            catch (ArangoException e)
                when (e.ErrorNumber == 1203 || e.ErrorNumber == 1202)
            {
                return false;
            }
        }
        
        
        ///////////////
        // Utilities //
        ///////////////

        private void CreateCollection(string name)
        {
            var arango = (ArangoConnection) RequestContext.Current.Services
                .Resolve<IArango>();
            
            arango.CreateCollection(name, CollectionType.Document);
        }
        
        #endregion
    }
}