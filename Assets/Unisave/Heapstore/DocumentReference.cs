using System.Threading.Tasks;
using LightJson;
using Unisave.Facets;
using Unisave.Heapstore.Backend;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using UnityEngine;

namespace Unisave.Heapstore
{
    /// <summary>
    /// References a specific database document (existing or not)
    /// and allows you to perform actions with it (Get, Set, Update, Delete)
    /// </summary>
    public class DocumentReference
    {
        /// <summary>
        /// Name of the referenced collection
        /// </summary>
        private string CollectionName { get; }
        
        /// <summary>
        /// Key of the referenced document
        /// </summary>
        private string DocumentKey { get; }

        /// <summary>
        /// Id of the referenced document
        /// </summary>
        private string DocumentId => DocumentKey == null
            ? null : CollectionName + "/" + DocumentKey;
        
        /// <summary>
        /// To whom facet calls will be attached
        /// </summary>
        private MonoBehaviour Caller { get; }

        public DocumentReference(
            string collectionName,
            string documentKey,
            MonoBehaviour caller = null
        )
        {
            CollectionName = collectionName;
            DocumentKey = documentKey;
            Caller = caller;
        }
        
        
        /////////////////////////
        // Document operations //
        /////////////////////////

        /// <summary>
        /// Fetches the document from the database
        /// </summary>
        /// <param name="throwIfMissing">Throw if the document does not exist</param>
        /// <returns>The document or null if missing</returns>
        public UnisaveOperation<Document> Get(bool throwIfMissing = false)
        {
            return new UnisaveOperation<Document>(Caller, GetAsync(throwIfMissing));
        }

        /// <summary>
        /// Fetches the document from the database and converts
        /// it to the requested type
        /// </summary>
        /// <param name="throwIfMissing">Throw if the document does not exist</param>
        /// <typeparam name="T">Type to which to convert the document</typeparam>
        /// <returns>The document or null if missing</returns>
        public UnisaveOperation<T> GetAs<T>(bool throwIfMissing = false)
        {
            return new UnisaveOperation<T>(Caller, async () => {
                Document document = await GetAsync(throwIfMissing);
                return document.As<T>();
            });
        }

        private async Task<Document> GetAsync(bool throwIfMissing)
        {
            var id = Arango.DocumentId.Parse(DocumentId);
            var transport = new TransportLayer(Caller);
            
            JsonObject fetchedJson = await transport.Call(f =>
                f.GetDocument(id)
            );

            if (throwIfMissing && fetchedJson == null)
            {
                throw new HeapstoreException(
                    1000,
                    "Getting a document that does not exist."
                );
            }

            if (fetchedJson == null)
                return null;

            return new Document(fetchedJson);
        }
        
        /// <summary>
        /// Overwrites or creates the document
        /// </summary>
        /// <param name="value">New value of the document</param>
        /// <param name="throwIfMissing">Throw if the document does not exist</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UnisaveOperation<Document> Set<T>(T value, bool throwIfMissing = false)
        {
            return new UnisaveOperation<Document>(Caller, async () => {
                JsonObject jsonToWrite = Serializer.ToJson<T>(
                    value,
                    SerializationContext.ClientToClient
                );

                var id = Arango.DocumentId.Parse(DocumentId);
                var transport = new TransportLayer(Caller);

                JsonObject writtenJson = await transport.Call(f =>
                    f.SetDocument(id, jsonToWrite, throwIfMissing)
                );

                return new Document(writtenJson);
            });
        }

        /// <summary>
        /// Updates only specified fields in the document.
        /// If that document is missing, it creates it.
        /// </summary>
        /// <param name="value">How to set the fields</param>
        /// <param name="throwIfMissing">Throw if the document does not exist</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UnisaveOperation<Document> Update<T>(
            T value,
            bool throwIfMissing = false
        )
        {
            return new UnisaveOperation<Document>(Caller, async () => {
                JsonObject jsonToWrite = Serializer.ToJson<T>(
                    value,
                    SerializationContext.ClientToClient
                );

                var id = Arango.DocumentId.Parse(DocumentId);
                var transport = new TransportLayer(Caller);

                JsonObject writtenJson = await transport.Call(f =>
                    f.UpdateDocument(id, jsonToWrite, throwIfMissing)
                );

                return new Document(writtenJson);
            });
        }

        public UnisaveOperation<bool> Delete()
        {
            return new UnisaveOperation<bool>(Caller, async () => {
                var id = Arango.DocumentId.Parse(DocumentId);
                var transport = new TransportLayer(Caller);

                bool wasDeleted = await transport.Call(f =>
                    f.DeleteDocument(id)
                );

                return wasDeleted;
            });
        }
    }
}