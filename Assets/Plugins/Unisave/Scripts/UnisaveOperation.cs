using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Unisave
{
    /// <summary>
    /// Represents an asynchronous Unisave operation.
    /// It can either return a value or throw an exception.
    /// It is a wrapper around C# Task, that lets you specify callbacks,
    /// interface with coroutines, and await. Also, when a caller is specified,
    /// the operation checks that the caller is alive and if not, it won't
    /// invoke any callbacks (similar to how coroutines get cancelled).
    /// It also logs any uncaught exceptions and other problems that might arise.
    /// All of these features are the reason for this wrapper existing.
    /// </summary>
    /// <typeparam name="TReturn">Type of the returned value</typeparam>
    public class UnisaveOperation<TReturn> : IEnumerator
    {
        /// <summary>
        /// The MonoBehaviour script that invoked the operation,
        /// may be null for operations invoked outside Unity code
        /// </summary>
        private readonly MonoBehaviour caller;
        
        /// <summary>
        /// The task that performs the asynchronous operation
        /// </summary>
        private readonly Task<TReturn> operationTask;

        /// <summary>
        /// True when the operation finishes (returns or throws an exception)
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        /// Returned value from the operation when it finishes successfully.
        /// To determine a successful completion, check that the
        /// the <see cref="Exception"/> property is null.
        /// </summary>
        public TReturn Result { get; private set; }
        
        /// <summary>
        /// The thrown exception if the operation finishes with an exception,
        /// null otherwise. Compare this against null to test
        /// for successful completion.
        /// </summary>
        public Exception Exception { get; private set; }

        public UnisaveOperation(
            MonoBehaviour caller,
            Func<Task<TReturn>> callable
        ) : this(caller, callable.Invoke()) { }
        
        public UnisaveOperation(
            MonoBehaviour caller,
            Task<TReturn> operationTask
        )
        {
            this.caller = caller;
            this.operationTask = operationTask;
            
            operationTask.ContinueWith((Task<TReturn> _) => {
                try
                {
                    OnOperationTaskCompleted();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        
        /// <summary>
        /// Called when the given operation task completes,
        /// runs immediately if the task is already completed
        /// </summary>
        private void OnOperationTaskCompleted()
        {
            TaskStatus status = operationTask.Status;

            if (status == TaskStatus.RanToCompletion)
            {
                Result = operationTask.Result;
                IsDone = true;
                
                InvokeThenCallback();
                CompleteExternalTask();
                
                return;
            }

            if (status == TaskStatus.Faulted)
            {
                // unwraps AggregateException if there is just one
                Exception = operationTask.Exception?.GetBaseException();
                IsDone = true;
                
                InvokeCatchCallback();
                FaultExternalTask();
                
                // nobody to catch the exception --> log it
                if (catchCallback == null && externalTcs == null)
                    Debug.LogException(Exception);
                
                return;
            }
            
            Debug.LogError(
                $"[Unisave] {nameof(UnisaveOperation<TReturn>)} finished, " +
                $"but the task status was unexpected: {status}"
            );
        }
        
        /// <summary>
        /// If true, the operation finalization should be ignored.
        /// </summary>
        private bool IsCallerDisabled()
        {
            // if there was no caller, then finalization always happens
            if (caller == null)
                return false;
            
            // if the game object isn't active in the hierarchy,
            // the caller is disabled
            if (!caller.gameObject.activeInHierarchy)
                return true;
            
            // if the component itself is disabled,
            // the caller is disabled
            if (!caller.enabled)
                return true;
            
            // otherwise the caller is enabled
            return false;
        }
        
        
        //////////////////
        // Callback API //
        //////////////////

        /// <summary>
        /// Function to be called on success
        /// </summary>
        private Action<TReturn> thenCallback;
        
        /// <summary>
        /// Function to be called on exception
        /// </summary>
        private Action<Exception> catchCallback;
        
        private void InvokeThenCallback()
        {
            // do nothing if the caller is disabled
            if (IsCallerDisabled())
                return;
            
            if (thenCallback == null)
                return;

            try
            {
                thenCallback.Invoke(Result);
            }
            catch (Exception callbackException)
            {
                Debug.LogException(callbackException);
            }
        }
        
        private void InvokeCatchCallback()
        {
            // do nothing if the caller is disabled
            if (IsCallerDisabled())
                return;
            
            if (catchCallback == null)
                return;

            try
            {
                catchCallback.Invoke(Exception);
            }
            catch (Exception callbackException)
            {
                Debug.LogException(callbackException);
            }
        }
        
        /// <summary>
        /// Register a function that will be called when the operation succeeds
        /// </summary>
        public UnisaveOperation<TReturn> Then(Action<TReturn> callback)
        {
            if (callback == null)
                return this;
            
            // allow only one callback
            if (thenCallback != null)
            {
                throw new InvalidOperationException(
                    $"You can only register one {nameof(Then)} callback. " +
                    $"For complicated use cases use the async-await API."
                );
            }
            
            // remember the callback
            thenCallback = callback;
            
            // invoke if already done
            if (IsDone && Exception == null)
                InvokeThenCallback();
            
            // chainable API
            return this;
        }
        
        /// <summary>
        /// Register a function that will be called when the operation fails
        /// </summary>
        public UnisaveOperation<TReturn> Catch(
            Action<Exception> callback
        )
        {
            if (callback == null)
                return this;
            
            // allow only one callback
            if (catchCallback != null)
            {
                throw new InvalidOperationException(
                    $"You can only register one {nameof(Catch)} callback. " +
                    $"For complicated use cases use the async-await API."
                );
            }

            // remember the callback
            catchCallback = callback;
            
            // invoke if already done
            if (IsDone && Exception != null)
                InvokeCatchCallback();
            
            // chainable API
            return this;
        }
        
        
        ///////////////////
        // Coroutine API //
        ///////////////////
        
        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }

        void IEnumerator.Reset() { }

        object IEnumerator.Current => !IsDone;
        
        
        ////////////////////
        // AsyncAwait API //
        ////////////////////

        /// <summary>
        /// Controls the task presented externally
        /// </summary>
        private TaskCompletionSource<TReturn> externalTcs;

        public Task<TReturn> Async()
        {
            if (externalTcs == null)
                externalTcs = new TaskCompletionSource<TReturn>();
            
            // complete if already done
            if (IsDone && Exception == null)
                CompleteExternalTask();
            
            // fault if already done
            if (IsDone && Exception != null)
                FaultExternalTask();

            return externalTcs.Task;
        }

        public TaskAwaiter<TReturn> GetAwaiter()
        {
            return Async().GetAwaiter();
        }
        
        private void CompleteExternalTask()
        {
            // do nothing if the caller is disabled
            if (IsCallerDisabled())
                return;

            // nothing to complete
            if (externalTcs == null)
                return;

            // invoke all awaiters
            externalTcs.TrySetResult(Result);
        }

        private void FaultExternalTask()
        {
            // do nothing if the caller is disabled
            if (IsCallerDisabled())
                return;
            
            // nothing to fault
            if (externalTcs == null)
                return;

            // invoke all awaiters
            externalTcs.TrySetException(Exception);
        }
    }
    
    ///////////////////////////////////////////
    // Void return type, non-generic variant //
    ///////////////////////////////////////////
    
    /// <summary>
    /// Represents a non-returning asynchronous Unisave operation.
    /// It can either succeed or throw an exception.
    /// It is a wrapper around C# Task, that lets you specify callbacks,
    /// interface with coroutines, and await. Also, when a caller is specified,
    /// the operation checks that the caller is alive and if not, it won't
    /// invoke any callbacks (similar to how coroutines get cancelled).
    /// It also logs any uncaught exceptions and other problems that might arise.
    /// All of these features are the reason for this wrapper existing.
    /// </summary>
    public class UnisaveOperation : IEnumerator
    {
        /*
         * The void UnisaveOperation is implemented by wrapping an
         * object-returning operation, which will always return null.
         */
        private readonly UnisaveOperation<object> innerOperation;

        /// <summary>
        /// True when the operation finishes (returns or throws an exception)
        /// </summary>
        public bool IsDone => innerOperation.IsDone;

        /// <summary>
        /// The thrown exception if the operation finishes with an exception,
        /// null otherwise. Compare this against null to test
        /// for successful completion.
        /// </summary>
        public Exception Exception => innerOperation.Exception;

        public UnisaveOperation(
            MonoBehaviour caller,
            Func<Task> callable
        ) : this(caller, callable.Invoke()) { }
        
        public UnisaveOperation(
            MonoBehaviour caller,
            Task operationTask
        )
        {
            Task<object> wrappedTask = WrapOperationTask(operationTask);
            
            innerOperation = new UnisaveOperation<object>(caller, wrappedTask);
        }

        private async Task<object> WrapOperationTask(Task operationTask)
        {
            await operationTask;
            
            return null;
        }
        
        
        //////////////////
        // Callback API //
        //////////////////

        /// <summary>
        /// Register a function that will be called when the operation succeeds
        /// </summary>
        public UnisaveOperation Then(Action callback)
        {
            innerOperation.Then(_ => callback?.Invoke());
            return this;
        }

        /// <summary>
        /// Register a function that will be called when the operation fails
        /// </summary>
        public UnisaveOperation Catch(Action<Exception> callback)
        {
            innerOperation.Catch(callback);
            return this;
        }
        
        
        ///////////////////
        // Coroutine API //
        ///////////////////
        
        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }

        void IEnumerator.Reset() { }

        object IEnumerator.Current => !IsDone;
        
        
        ////////////////////
        // AsyncAwait API //
        ////////////////////

        public Task Async()
        {
            return innerOperation.Async();
        }

        public TaskAwaiter GetAwaiter()
        {
            return Async().GetAwaiter();
        }
    }
}