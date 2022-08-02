using System;
using Unisave.Serialization;
using UnityEngine;

namespace Unisave.Broadcasting
{
    /// <summary>
    /// Wrapper class around a message router that exposes only the building API
    /// </summary>
    public class MessageRouterBuilder
    {
        private readonly MessageRouter router;
        
        public MessageRouterBuilder(MessageRouter router)
        {
            this.router = router;

            ElseLogWarning();
        }
        
        /// <summary>
        /// Send messages of exact given type to a given handler
        /// </summary>
        /// <param name="action">Handler for the messages</param>
        /// <typeparam name="TMessage">Type of messages</typeparam>
        /// <returns>Itself to allow for chaining</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MessageRouterBuilder Forward<TMessage>(Action<TMessage> action)
            where TMessage : BroadcastingMessage
        {
            if (action == null)
                throw new ArgumentNullException();
            
            router.SetRule(typeof(TMessage), (BroadcastingMessage message) => {
                action.Invoke((TMessage) message);
            });
            
            return this;
        }
        
        /// <summary>
        /// Define a handler for unmatched messages
        /// </summary>
        /// <param name="action">The message handler</param>
        /// <returns></returns>
        public MessageRouterBuilder Else(Action<BroadcastingMessage> action)
        {
            router.SetDefault(action);
            
            return this;
        }

        /// <summary>
        /// Log a warning, when an unmatched message is received
        /// </summary>
        /// <returns></returns>
        public MessageRouterBuilder ElseLogWarning()
        {
            return Else(message => {
                Debug.LogWarning(
                    $"[Unisave] Received a broadcasting message of unhandled " +
                    $"type {message.GetType()}\n" +
                    Serializer.ToJson(message).ToString(true)
                );
            });
        }
        
        /// <summary>
        /// Do nothing when an unmatched message is received
        /// </summary>
        /// <returns></returns>
        public MessageRouterBuilder ElseDoNothing()
        {
            return Else(null);
        }
    }
}