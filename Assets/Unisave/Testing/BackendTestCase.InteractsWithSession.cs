using Unisave.Runtime.Kernels;
using Unisave.Sessions;
using Unisave.Utils;

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
            => ClientApp.Resolve<ClientSessionIdRepository>().GetSessionId();

        /// <summary>
        /// Generates a session ID and stores it in the client application
        /// </summary>
        protected void GenerateSessionId()
        {
            ClientApp.Resolve<ClientSessionIdRepository>().StoreSessionId(
                ServerSessionIdRepository.GenerateSessionId()
            );
        }
    }
}