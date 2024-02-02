using Unisave.Sessions;

namespace Unisave.Testing
{
    public partial class BackendTestCase
    {
        /// <summary>
        /// Returns the current session ID
        /// (as seen by the client, since the server is not running when
        /// we are inside the test method in between facet calls)
        /// </summary>
        protected string SessionId
            => ClientApp.Services.Resolve<ClientSessionIdRepository>().GetSessionId();

        /// <summary>
        /// Generates a session ID and stores it in the client application
        /// </summary>
        protected void GenerateSessionId()
        {
            ClientApp.Services.Resolve<ClientSessionIdRepository>().StoreSessionId(
                ServerSessionIdRepository.GenerateSessionId()
            );
        }
    }
}