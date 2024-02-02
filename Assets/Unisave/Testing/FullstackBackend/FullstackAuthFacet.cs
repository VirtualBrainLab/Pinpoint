using LightJson;
using Unisave.Facades;
using Unisave.Facets;

namespace Unisave.Testing.FullstackBackend
{
    public class FullstackAuthFacet : Facet
    {
        public void Login(string documentId)
        {
            Auth.Login(documentId);
        }
        
        public bool Check()
        {
            return Auth.Check();
        }
        
        public JsonObject GetPlayer()
        {
            return Auth.GetPlayer<JsonObject>();
        }
        
        public void Logout()
        {
            Auth.Logout();
        }
    }
}