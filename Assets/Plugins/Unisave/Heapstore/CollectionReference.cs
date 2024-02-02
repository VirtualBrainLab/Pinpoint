using LightJson;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using UnityEngine;

namespace Unisave.Heapstore
{
    /// <summary>
    /// References a database collection and allows you
    /// to reference documents or build document queries
    /// </summary>
    public class CollectionReference : Query
    {
        /// <summary>
        /// Name of the referenced collection
        /// </summary>
        private string CollectionName => request.collection;
        
        public CollectionReference(string collectionName)
            : base(collectionName) { }
        
        public CollectionReference(string collectionName, MonoBehaviour caller)
            : base(collectionName, caller) { }
        
        /// <summary>
        /// Creates a reference to a specific document so that you can
        /// then perform operations with it
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public DocumentReference Document(string key)
        {
            return new DocumentReference(CollectionName, key, caller);
        }
        
        /// <summary>
        /// Inserts a new document into the collection
        /// </summary>
        /// <param name="value">The inserted document</param>
        /// <param name="throwIfMissing">Throw if the collection does not exist</param>
        /// <typeparam name="T"></typeparam>
        /// <returns>The inserted document with its ID set</returns>
        public UnisaveOperation<Document> Add<T>(T value, bool throwIfMissing = false)
        {
            return new UnisaveOperation<Document>(caller, async () => {
                JsonObject jsonToWrite = Serializer.ToJson<T>(
                    value,
                    SerializationContext.ClientToClient
                );

                var transport = new TransportLayer(caller);

                JsonObject writtenJson = await transport.Call(f =>
                    f.AddDocument(CollectionName, jsonToWrite, throwIfMissing)
                );

                return new Document(writtenJson);
            });
        }
    }
}