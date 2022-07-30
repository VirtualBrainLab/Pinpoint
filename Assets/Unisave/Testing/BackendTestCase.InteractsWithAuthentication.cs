using Unisave.Authentication;
using Unisave.Authentication.Middleware;
using Unisave.Contracts;
using Unisave.Entities;
using Unisave.Facades;
using Unisave.Sessions;
using Unisave.Utils;

namespace Unisave.Testing
{
    public partial class BackendTestCase
    {
        /// <summary>
        /// Make the given player the authenticated player
        /// </summary>
        protected BackendTestCase ActingAs(Entity player)
        {
            var manager = App.Resolve<AuthenticationManager>();
            manager.SetPlayer(player);
            
            // HACK TO STORE THE UPDATED SESSION:
            // I need to figure out how to properly merge test facade access
            // with middleware logic so that it does not interfere.
            var sessionRepo = ClientApp.Resolve<ClientSessionIdRepository>();
            if (sessionRepo.GetSessionId() == null)
                sessionRepo.StoreSessionId(Str.Random(16));
            Session.Set(AuthenticationManager.SessionKey, player?.EntityId);
            App.Resolve<ISession>().StoreSession(sessionRepo.GetSessionId());
            
            return this;
        }
    }
}