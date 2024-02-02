using System;
using System.Collections.Generic;

namespace Unisave.Broadcasting
{
    /// <summary>
    /// Routes broadcasting messages to a set of actions
    /// </summary>
    public class MessageRouter
    {
        private Dictionary<Type, Action<BroadcastingMessage>> rules
            = new Dictionary<Type, Action<BroadcastingMessage>>();

        private Action<BroadcastingMessage> defaultAction;

        /// <summary>
        /// Route a given message to the proper action
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void RouteMessage(BroadcastingMessage message)
        {
            if (message == null)
                throw new ArgumentNullException();
        
            Type type = message.GetType();

            if (rules.ContainsKey(type))
            {
                rules[type].Invoke(message);
            }
            else
            {
                defaultAction?.Invoke(message);
            }
        }

        /// <summary>
        /// Sets a routing rule for a given message type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        public void SetRule(Type type, Action<BroadcastingMessage> action)
        {
            rules[type] = action ?? throw new ArgumentNullException();
        }

        /// <summary>
        /// Sets the action to be performed in case no rule matches.
        /// Can be null to perform nothing.
        /// </summary>
        /// <param name="action"></param>
        public void SetDefault(Action<BroadcastingMessage> action)
        {
            defaultAction = action;
        }
    }
}