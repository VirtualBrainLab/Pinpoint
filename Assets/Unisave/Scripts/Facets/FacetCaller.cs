using System;
using System.Reflection;
using RSG;
using LightJson;
using Unisave.Exceptions;
using Unisave.Foundation;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using Unisave.Sessions;

namespace Unisave.Facets
{
    /// <summary>
    /// Handles facet calling
    /// </summary>
    public abstract class FacetCaller
    {
        /// <summary>
        /// ID of the session held against the server
        /// </summary>
        protected string SessionId
        {
            get => sessionIdRepository.GetSessionId();
            set => sessionIdRepository.StoreSessionId(value);
        }

        private readonly ClientSessionIdRepository sessionIdRepository;
        
        /// <summary>
        /// Class that extracts information about the device
        /// </summary>
        protected DeviceIdRepository DeviceIdRepository { get; }
        
        public FacetCaller(ClientApplication clientApp)
        {
            sessionIdRepository = clientApp.Resolve<ClientSessionIdRepository>();
            DeviceIdRepository = clientApp.Resolve<DeviceIdRepository>();
        }
        
        /// <summary>
        /// Calls facet method that has a return value
        /// </summary>
        /// <typeparam name="TFacet">Facet class</typeparam>
        /// <typeparam name="TReturn">Method return type</typeparam>
        /// <returns>Promise that resolves when the call finishes</returns>
        public IPromise<TReturn> CallFacetMethod<TFacet, TReturn>(
            string methodName,
            params object[] arguments
        )
        {
            return CallFacetMethod(
                    typeof(TFacet),
                    typeof(TReturn),
                    methodName,
                    arguments
                )
                .Then((object ret) => (TReturn) (ret ?? default(TReturn)));
        }
        
        /// <summary>
        /// Calls facet method that returns void
        /// </summary>
        /// <typeparam name="TFacet">Facet class</typeparam>
        /// <returns>Promise that resolves when the call finishes</returns>
        public IPromise CallFacetMethod<TFacet>(
            string methodName,
            params object[] arguments
        ) => CallFacetMethod(typeof(TFacet), methodName, arguments);

        /// <summary>
        /// Calls facet method with return value in a non-generic way
        /// </summary>
        public IPromise<object> CallFacetMethod(
            Type facetType,
            Type returnType,
            string methodName,
            params object[] arguments
        )
        {
            MethodInfo methodInfo = Facet.FindMethodByName(
                facetType,
                methodName
            );

            if (methodInfo.ReturnType != returnType)
            {
                throw new UnisaveException(
                    $"OnFacet<{facetType.Name}>.Call<{returnType.Name}>" +
                    $"(\"{methodName}\", ...)" +
                    $" is incorrect (method returns different type), use:\n" +
                    $"OnFacet<{facetType.Name}>.Call" +
                    $"<{methodInfo.ReturnType.Name}>(...)"
                );
            }

            return PerformFacetCall(
                facetType.FullName,
                methodName,
                Facet.SerializeArguments(methodInfo, arguments)
            )
            .Then((JsonValue returnedValue) => {
                return Serializer.FromJson(
                    returnedValue,
                    returnType,
                    DeserializationContext.ServerToClient
                );
            });
        }

        /// <summary>
        /// Calls facet method that returns void in a non-generic way
        /// </summary>
        public IPromise CallFacetMethod(
            Type facetType, string methodName, params object[] arguments
        )
        {
            MethodInfo methodInfo = Facet.FindMethodByName(
                facetType,
                methodName
            );

            if (methodInfo.ReturnType != typeof(void))
            {
                throw new UnisaveException(
                    $"OnFacet<{facetType.Name}>.Call(\"{methodName}\", ...)" +
                    $" is incorrect (method doesn't return void), use:\n" +
                    $"OnFacet<{facetType.Name}>" +
                    $".Call<{methodInfo.ReturnType.Name}>(...)"
                );
            }

            return PerformFacetCall(
                facetType.Name,
                methodName,
                Facet.SerializeArguments(methodInfo, arguments)
            )
            .Then(v => {}); // forget the return value, which is null anyways
        }

        /// <summary>
        /// Performs the facet call
        /// </summary>
        protected abstract IPromise<JsonValue> PerformFacetCall(
            string facetName,
            string methodName,
            JsonArray arguments
        );
    }
}
