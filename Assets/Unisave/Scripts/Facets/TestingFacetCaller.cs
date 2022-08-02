using LightJson;
using RSG;
using Unisave.Foundation;
using Unisave.Logging;
using Unisave.Runtime;
using Unisave.Runtime.Kernels;

namespace Unisave.Facets
{
    public class TestingFacetCaller : FacetCaller
    {
        private readonly Application app;
        
        public TestingFacetCaller(Application app, ClientApplication clientApp)
            : base(clientApp)
        {
            this.app = app;
        }

        protected override IPromise<JsonValue> PerformFacetCall(
            string facetName,
            string methodName,
            JsonArray arguments
        )
        {
            JsonValue returnedJson;
            try
            {
                var methodParameters = new FacetCallKernel.MethodParameters(
                    facetName,
                    methodName,
                    arguments,
                    SessionId
                );

                var kernel = app.Resolve<FacetCallKernel>();

                returnedJson = kernel.Handle(methodParameters);
            }
            finally
            {
                // remember session id
                var specialValues = app.Resolve<SpecialValues>();
                SessionId = specialValues.Read("sessionId").AsString;
                
                // print logs that went through the Log facade
                LogPrinter.PrintLogsFromFacetCall(specialValues.Read("logs"));
            }
            
            return Promise<JsonValue>.Resolved(returnedJson);
        }
    }
}