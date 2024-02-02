using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading.Tasks;
using Unisave.Facets;
using Unisave.Heapstore.Backend;
using UnityEngine;

namespace Unisave.Heapstore
{
    /// <summary>
    /// Intercepts all facet calls to heapstore
    /// </summary>
    public class TransportLayer
    {
        private readonly MonoBehaviour caller;
        
        public TransportLayer(MonoBehaviour caller)
        {
            this.caller = caller;
        }

        /// <summary>
        /// Call a heapstore facet method
        /// </summary>
        public async Task<TReturn> Call<TReturn>(
            Expression<Func<HeapstoreFacet, TReturn>> lambda
        )
        {
            try
            {
                return await caller.CallFacet(lambda);
            }
            catch (FacetSearchException)
            {
                // ERROR_DISABLED
                throw new HeapstoreException(
                    4,
                    "Heapstore is disabled. Open the Unisave window, go to " +
                    "'Backend Code' tab, and enable the Heapstore backend."
                );
            }
            catch (HttpRequestException e)
            {
                // ERROR_CANNOT_CONNECT
                throw new HeapstoreException(
                    5, "Cannot connect to the server.\nReason: " + e
                );
            }
        }
    }
}