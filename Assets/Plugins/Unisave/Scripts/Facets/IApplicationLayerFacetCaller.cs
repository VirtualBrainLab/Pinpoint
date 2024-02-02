using System.Reflection;
using System.Threading.Tasks;

namespace Unisave.Facets
{
    /// <summary>
    /// Defines the facet calling API at the application-level
    /// </summary>
    public interface IApplicationLayerFacetCaller
    {
        Task<TReturn> CallFacetMethodAsync<TReturn>(
            MethodInfo method,
            object[] arguments
        );
        
        Task CallFacetMethodAsync(
            MethodInfo method,
            object[] arguments
        );
    }
}