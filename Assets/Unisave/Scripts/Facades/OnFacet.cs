using System;
using System.Threading.Tasks;
using RSG;
using Unisave.Facets;

namespace Unisave.Facades
{
    /// <summary>
    /// Facade for calling facet methods
    /// </summary>
    /// <typeparam name="TFacet">Target facet class</typeparam>
    public static class OnFacet<TFacet> where TFacet : Facet
    {
        /// <summary>
        /// Returns the facet caller instance that will be used
        /// </summary>
        private static FacetCaller GetFacetCaller()
        {
            return ClientFacade.ClientApp.Resolve<FacetCaller>();
        }
        
        /// <summary>
        /// Calls a facet method that has a return value
        /// </summary>
        /// <param name="methodName">Name of the facet method</param>
        /// <param name="arguments">Arguments for the method</param>
        /// <typeparam name="TReturn">Return type of the method</typeparam>
        /// <returns>Promise that will resolve once the call finishes</returns>
        public static IPromise<TReturn> Call<TReturn>(
            string methodName,
            params object[] arguments
        ) => GetFacetCaller().CallFacetMethod<TFacet, TReturn>(
            methodName,
            arguments
        );

        /// <summary>
        /// Same as Call, but used via the C# await async keywords
        /// </summary>
        public static Task<TReturn> CallAsync<TReturn>(
            string methodName,
            params object[] arguments
        )
        {
            var source = new TaskCompletionSource<TReturn>();

            Call<TReturn>(methodName, arguments)
                .Then(r => {
                    source.SetResult(r);
                })
                .Catch(e => {
                    source.SetException(e);
                });

            return source.Task;
        }

        /// <summary>
        /// Same as Call, but synchronous, blocking the thread
        /// (use only in tests, otherwise it will freeze your game)
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="arguments"></param>
        /// <typeparam name="TReturn"></typeparam>
        /// <returns></returns>
        public static TReturn CallSync<TReturn>(
            string methodName,
            params object[] arguments
        )
        {
            bool finished = false;
            
            TReturn result = default;
            Exception exception = null;
            
            Call<TReturn>(methodName, arguments)
                .Then(r => {
                    finished = true;
                    result = r;
                })
                .Catch(e => {
                    finished = true;
                    exception = e;
                });
            
            if (!finished)
                throw new InvalidOperationException(
                    "You can only use OnFacet<T>.CallSync() inside " +
                    "integration tests. It would freeze your game should " +
                    "it be used in your game code."
                );

            if (exception != null)
                throw exception;

            return result;
        }

        /// <summary>
        /// Calls a facet method that returns void
        /// </summary>
        /// <param name="methodName">Name of the facet method</param>
        /// <param name="arguments">Arguments for the method</param>
        /// <returns>Promise that will resolve once the call finishes</returns>
        public static IPromise Call(
            string methodName,
            params object[] arguments
        ) => GetFacetCaller().CallFacetMethod<TFacet>(
            methodName,
            arguments
        );

        /// <summary>
        /// Same as Call, but used via the C# await async keywords
        /// </summary>
        public static Task CallAsync(
            string methodName,
            params object[] arguments
        )
        {
            var source = new TaskCompletionSource<bool>(); // bool = void

            Call(methodName, arguments)
                .Then(() => {
                    source.SetResult(true);
                })
                .Catch(e => {
                    source.SetException(e);
                });

            return source.Task;
        }
        
        /// <summary>
        /// Same as Call, but synchronous, blocking the thread
        /// (use only in tests, otherwise it will freeze your game)
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static void CallSync(
            string methodName,
            params object[] arguments
        )
        {
            bool finished = false;
            
            Exception exception = null;
            
            Call(methodName, arguments)
                .Then(() => {
                    finished = true;
                })
                .Catch(e => {
                    finished = true;
                    exception = e;
                });
            
            if (!finished)
                throw new InvalidOperationException(
                    "You can only use OnFacet<T>.CallSync() inside " +
                    "integration tests. It would freeze your game should " +
                    "it be used in your game code."
                );

            if (exception != null)
                throw exception;
        }
    }
}
