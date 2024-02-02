using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using RSG;

namespace Unisave.Facades
{
    public static class PromiseExtensions
    {
        /// <summary>
        /// Wait for the promise to get resolved inside a coroutine
        /// </summary>
        public static IEnumerator AsCoroutine(this IPromise promise)
        {
            bool done = false;
            Exception exception = null;

            promise
                .Then(() => { done = true; })
                .Catch(e => {
                    exception = e;
                    done = true;
                });

            while (!done)
                yield return null;

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
        }

        /// <summary>
        /// Wait for the promise to get resolved inside a coroutine
        /// </summary>
        public static IEnumerator AsCoroutine<TReturn>(
            this IPromise<TReturn> promise
        )
        {
            return AsCoroutine(
                // consume the return value since nobody requested it
                // and then work with it just like with a void promise
                promise.Then((ret) => {})
            );
        }
    }
}