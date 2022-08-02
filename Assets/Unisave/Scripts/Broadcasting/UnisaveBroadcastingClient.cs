using System;
using System.Collections;
using System.Collections.Generic;
using Unisave.Facades;
using UnityEngine;

namespace Unisave.Broadcasting
{
    /// <summary>
    /// Parent class for any MonoBehaviour that wants
    /// to subscribe to broadcasting channels
    /// </summary>
    public abstract class UnisaveBroadcastingClient : MonoBehaviour
    {
        /// <summary>
        /// Subscription this client owns
        /// </summary>
        private readonly HashSet<ChannelSubscription> subscriptions
            = new HashSet<ChannelSubscription>();

        /// <summary>
        /// Are the connection hooks registered or not?
        /// </summary>
        private bool hooksRegistered = false;

        /// <summary>
        /// The broadcasting manager instance
        /// </summary>
        private ClientBroadcastingManager Manager
        {
            get
            {
                if (manager == null)
                {
                    manager = ClientFacade.ClientApp
                        .Resolve<ClientBroadcastingManager>();
                }

                return manager;
            }
        }
        
        // backing field
        private ClientBroadcastingManager manager;
 
        /// <summary>
        /// Status of the connection to the Unisave broadcasting server
        /// </summary>
        public BroadcastingConnection ConnectionState
            => Manager.Tunnel.ConnectionState;
        
        /// <summary>
        /// Receive messages from a channel subscription
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        protected MessageRouterBuilder FromSubscription(
            ChannelSubscription subscription
        )
        {
            if (subscriptions.Contains(subscription))
                throw new InvalidOperationException(
                    "You cannot handle a subscription twice"
                );

            subscriptions.Add(subscription);

            var messageRouter = new MessageRouter();
            
            StartCoroutine(
                RegisterSubscriptionHandlerAfterDelay(
                    subscription,
                    messageRouter
                )
            );

            return new MessageRouterBuilder(messageRouter);
        }

        private IEnumerator RegisterSubscriptionHandlerAfterDelay(
            ChannelSubscription subscription,
            MessageRouter messageRouter
        )
        {
            // skip a frame to make sure the router is fully built
            // before handling any messages
            yield return null;
            
            // make sure the subscription is still active
            // (we haven't been killed in the mean time)
            if (!subscriptions.Contains(subscription))
                yield break;
            
            // register the handler
            Manager.SubscriptionRouter.HandleSubscription(
                subscription,
                messageRouter.RouteMessage
            );
            
            // register hooks
            if (!hooksRegistered)
            {
                Manager.Tunnel.OnConnectionLost += OnConnectionLost;
                Manager.Tunnel.OnConnectionRegained += OnConnectionRegained;
                hooksRegistered = true;
            }
        }

        /// <summary>
        /// Cancels all subscriptions for this script
        /// </summary>
        protected virtual void OnDisable()
        {
            Manager.SubscriptionRouter.EndSubscriptions(subscriptions);

            subscriptions.Clear();
            
            // remove hooks
            if (hooksRegistered)
            {
                Manager.Tunnel.OnConnectionLost -= OnConnectionLost;
                Manager.Tunnel.OnConnectionRegained -= OnConnectionRegained;
                hooksRegistered = false;
            }
        }

        /// <summary>
        /// Called when the broadcasting connection is broken
        /// </summary>
        protected virtual void OnConnectionLost()
        {
            // override hook
        }

        /// <summary>
        /// Called when the broadcasting connection is established again
        /// </summary>
        protected virtual void OnConnectionRegained()
        {
            // override hook
        }
    }
}