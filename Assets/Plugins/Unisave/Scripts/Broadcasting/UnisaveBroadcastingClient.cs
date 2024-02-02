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
        /// Status of the connection to the Unisave broadcasting server
        /// </summary>
        public BroadcastingConnection ConnectionState
        {
            get
            {
                ClientBroadcastingManager manager = TryGetManager();

                if (manager == null)
                    return BroadcastingConnection.Disconnected;
                
                return manager.Tunnel.ConnectionState;
            }
        }

        /// <summary>
        /// Tries to get the manager instance,
        /// returns null if the instance does not exist
        /// </summary>
        private ClientBroadcastingManager TryGetManager()
        {
            if (!ClientFacade.HasApp)
                return null;
            
            return GetOrCreateManager();
        }

        /// <summary>
        /// Gets or creates new manager instance.
        /// Do not use this in OnDispose/OnDisable/OnApplicationQuit
        /// since it would re-create the app instance when it may have
        /// been already disposed of.
        /// </summary>
        private ClientBroadcastingManager GetOrCreateManager()
        {
            return ClientFacade.ClientApp.Services.Resolve<ClientBroadcastingManager>();
        }
        
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
            
            ClientBroadcastingManager manager = GetOrCreateManager();
            
            // register the handler
            manager.SubscriptionRouter.HandleSubscription(
                subscription,
                messageRouter.RouteMessage
            );
            
            // register hooks
            if (!hooksRegistered)
            {
                manager.Tunnel.OnConnectionLost += OnConnectionLost;
                manager.Tunnel.OnConnectionRegained += OnConnectionRegained;
                hooksRegistered = true;
            }
        }

        /// <summary>
        /// Cancels all subscriptions for this script
        /// </summary>
        protected virtual void OnDisable()
        {
            ClientBroadcastingManager manager = TryGetManager();

            // the manager has already been disposed,
            // because the whole application is quitting
            // (the order of game object destruction is not defined)
            if (manager == null)
                return;
            
            manager.SubscriptionRouter.EndSubscriptions(subscriptions);

            subscriptions.Clear();
            
            // remove hooks
            if (hooksRegistered)
            {
                manager.Tunnel.OnConnectionLost -= OnConnectionLost;
                manager.Tunnel.OnConnectionRegained -= OnConnectionRegained;
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