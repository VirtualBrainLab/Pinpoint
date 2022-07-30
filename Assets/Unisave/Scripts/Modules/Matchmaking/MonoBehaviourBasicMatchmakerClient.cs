using System.Threading.Tasks;
using Unisave.Entities;
using Unisave.Modules.Matchmaking.Exceptions;
using Unisave.Exceptions;
using Unisave.Facades;
using UnityEngine;

namespace Unisave.Modules.Matchmaking
{
    public abstract class MonoBehaviourBasicMatchmakerClient<
        TPlayerEntity, TMatchEntity, TMatchmakerFacet, TMatchmakerTicket
    > : MonoBehaviour
        where TMatchmakerTicket : BasicMatchmakerTicket
        where TPlayerEntity : Entity, new()
        where TMatchEntity : BasicMatchEntity<TPlayerEntity>, new()
        where TMatchmakerFacet : BasicMatchmakerFacet<
            TPlayerEntity, TMatchmakerTicket, TMatchEntity
        >
    {
        /// <summary>
        /// What's the delay between individual server polls
        /// </summary>
        private const int PollingPeriodSeconds = 6;
        
        /// <summary>
        /// How many times to retry joining the matchmaker before
        /// we conclude that there's something wrong with the server
        /// </summary>
        private const int MaxRetryCount = 3;
        
        /// <summary>
        /// Are we waiting for a match, being registered inside the matchmaker?
        /// </summary>
        private bool isWaitingForMatch;
        
        /// <summary>
        /// Is the user waiting for cancellation?
        /// </summary>
        private bool cancelWaiting;

        /// <summary>
        /// Should the polling be immediately killed
        /// because the mono behaviour has been destroyed
        /// (does not leave gently, but right now)
        /// </summary>
        private bool killPolling;
        
        /// <summary>
        /// Registers a ticket into the matchmaker and waits for a match.
        ///
        /// Calling this while already waiting does nothing.
        /// </summary>
        public async void StartWaitingForMatch(TMatchmakerTicket ticket)
        {
            if (isWaitingForMatch)
                return;
            
            isWaitingForMatch = true;
            
            await OnFacet<TMatchmakerFacet>.CallAsync(
                "JoinMatchmaker", ticket
            );

            int retryCount = 0;

            while (true)
            {
                if (killPolling)
                    return;

                bool attemptingCancellation = cancelWaiting;
                
                TMatchEntity match = null;
                try
                {
                    match = await OnFacet<TMatchmakerFacet>
                        .CallAsync<TMatchEntity>(
                            "PollMatchmaker",
                            attemptingCancellation
                        );
                }
                catch (UnknownPlayerPollingException)
                {
                    if (!attemptingCancellation)
                    {
                        // server has for some reason forgotten about us
                        // so just retry matchmaker joining
                        await OnFacet<TMatchmakerFacet>.CallAsync(
                            "JoinMatchmaker", ticket
                        );
                        retryCount++;

                        // something went wrong, just blow up
                        if (retryCount > MaxRetryCount)
                        {
                            isWaitingForMatch = false;
                            cancelWaiting = false;
                            
                            throw new UnisaveException(
                                "Unable to join the matchmaker. " +
                                $"Retried {MaxRetryCount} times, but still failing."
                            );
                        }

                        continue;
                    }
                }

                // we were matched
                if (match != null)
                {
                    cancelWaiting = false;
                    isWaitingForMatch = false;
                    JoinedMatch(match);
                    return;
                }
                
                // cancellation finished
                if (attemptingCancellation)
                {
                    cancelWaiting = false;
                    isWaitingForMatch = false;
                    WaitingCanceled();
                    return;
                }
                
                // poll waiting
                await Task.Delay(PollingPeriodSeconds * 1000);
            }
        }

        /// <summary>
        /// Stops waiting for a match
        ///
        /// Does nothing when not waiting
        /// </summary>
        public void StopWaitingForMatch()
        {
            if (!isWaitingForMatch)
                return;
        
            cancelWaiting = true;
        }

        protected virtual void WaitingCanceled()
        {
            // override this hook
        }

        protected virtual void JoinedMatch(TMatchEntity match)
        {
            // override this hook
        }
        
        protected virtual void OnDestroy()
        {
            // stop all background tasks
            killPolling = true;
        }
    }
}