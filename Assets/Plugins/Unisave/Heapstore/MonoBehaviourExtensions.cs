using Unisave.Arango;
using UnityEngine;

namespace Unisave.Heapstore
{
    /// <summary>
    /// Allows you to interact with heapstore from mono behaviours
    /// </summary>
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Work with the given collection
        /// </summary>
        /// <param name="caller">The MonoBehaviour instance calling this API</param>
        /// <param name="name">Collection name</param>
        /// <returns>Collection reference with which actions can be performed</returns>
        public static CollectionReference Collection(
            this MonoBehaviour caller,
            string name
        ) => new CollectionReference(name, caller);

        public static DocumentReference Document(
            this MonoBehaviour caller,
            string id
        )
        {
            DocumentId docId = DocumentId.Parse(id);
            return new DocumentReference(docId.Collection, docId.Key, caller);
        }
    }
}