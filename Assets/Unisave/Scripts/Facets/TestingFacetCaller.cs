using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LightJson;
using Microsoft.Owin;
using RSG;
using Unisave.Foundation;
using Unisave.Logging;
using Unisave.Serialization;
using Unisave.Serialization.Context;

namespace Unisave.Facets
{
    public class TestingFacetCaller : FacetCaller
    {
        private readonly BackendApplication app;
        
        public TestingFacetCaller(BackendApplication app, ClientApplication clientApp)
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
            var ctx = new OwinContext();

            // prepare request
            ctx.Request.Path = new PathString($"/{facetName}/{methodName}");
            ctx.Request.Headers["X-Unisave-Request"] = "Facet";
            ctx.Request.Headers["Content-Type"] = "application/json";

            JsonObject requestBody = new JsonObject() {
                ["arguments"] = arguments
            };
            byte[] requestBodyBytes = Encoding.UTF8.GetBytes(requestBody.ToString());
            ctx.Request.Body = new MemoryStream(requestBodyBytes, writable: false);

            // prepare response stream for writing
            var responseStream = new MemoryStream(10 * 1024); // 10 KB
            ctx.Response.Body = responseStream;
            
            // run the app delegate
            app.Invoke(ctx).GetAwaiter().GetResult();
            
            // prepare response stream for reading
            // (the writing stream was disposed which closes it for operations)
            int receivedBytes = int.Parse(ctx.Response.Headers["Content-Length"]);
            string jsonString = Encoding.UTF8.GetString(
                responseStream.GetBuffer(), 0, receivedBytes
            );
            JsonObject body = Serializer.FromJsonString<JsonObject>(jsonString);
            
            // store session ID
            string returnedSessionId = ExtractSessionIdFromCookies(ctx.Response);
            if (returnedSessionId != null)
                SessionId = returnedSessionId;
            
            // print the logs
            LogPrinter.PrintLogsFromFacetCall(body["logs"]);
            
            // handle exceptions
            if (body["status"].AsString == "exception")
            {
                var e = Serializer.FromJson<Exception>(
                    body["exception"],
                    DeserializationContext.ServerToClient
                );
                UnisaveFacetCaller.PreserveStackTrace(e);
                return Promise<JsonValue>.Rejected(e);
            }
            
            // handle returned value
            return Promise<JsonValue>.Resolved(body["returned"]);
        }
        
        /// <summary>
        /// Extracts session ID from Set-Cookie headers and
        /// returns null if that fails.
        /// </summary>
        private static string ExtractSessionIdFromCookies(IOwinResponse response)
        {
            const string prefix = "unisave_session_id=";
            
            IList<string> setCookies = response.Headers.GetValues("Set-Cookie");

            string sessionCookie = setCookies?.FirstOrDefault(
                c => c.Contains(prefix)
            );

            sessionCookie = sessionCookie?.Split(';')?.FirstOrDefault(
                c => c.StartsWith(prefix)
            );

            string sessionId = sessionCookie?.Substring(prefix.Length);

            if (sessionId == null)
                return null;
            
            return Uri.UnescapeDataString(sessionId);
        }
    }
}