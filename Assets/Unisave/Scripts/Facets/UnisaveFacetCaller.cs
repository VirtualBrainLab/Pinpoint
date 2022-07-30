using System;
using System.Net.Http;
using System.Runtime.Serialization;
using LightJson;
using RSG;
using Unisave.Utils;
using Unisave.Exceptions;
using Unisave.Foundation;
using Unisave.HttpClient;
using Unisave.Logging;
using Unisave.Serialization;
using Unisave.Serialization.Context;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Unisave.Facets
{
    public class UnisaveFacetCaller : FacetCaller
    {
		private readonly ClientApplication app;

		private readonly AssetHttpClient http;

        public UnisaveFacetCaller(ClientApplication app) : base(app)
        {
	        this.app = app;
	        
	        http = app.Resolve<AssetHttpClient>();
        }

		protected override IPromise<JsonValue> PerformFacetCall(
            string facetName,
            string methodName,
            JsonArray arguments
        )
		{
			var promise = new Promise<JsonValue>();
			
			http.Post(
				app.Resolve<ApiUrl>().CallFacet(),
				new JsonObject()
					.Add("facetName", facetName)
					.Add("methodName", methodName)
					.Add("arguments", arguments)
					.Add("sessionId", SessionId)
					.Add("deviceId", DeviceIdRepository.GetDeviceId())
					.Add("device", DeviceIdRepository.GetDeviceInfo())
					.Add("gameToken", app.Preferences.GameToken)
					.Add("editorKey", app.Preferences.EditorKey)
					.Add("client", new JsonObject()
						.Add("backendHash", app.Preferences.BackendHash)
						.Add("frameworkVersion", FrameworkMeta.Version)
						.Add("assetVersion", AssetMeta.Version)
						.Add("buildGuid", Application.buildGUID)
						.Add("versionString", Application.version)
					),
				response => {
					
					if (response.IsOk)
						HandleSuccessfulRequest(response, promise);
					else
						HandleFailedRequest(response, promise);
					
				}
			);
			
			return promise;
		}
		
		/// <summary>
		/// The HTTP response was 200
		/// </summary>
		private void HandleSuccessfulRequest(
			Response response,
			Promise<JsonValue> promise
		)
		{
			JsonObject executionResult = response["executionResult"];

			JsonObject specialValues = executionResult["special"].AsJsonObject
			                           ?? new JsonObject();
				
			// remember the session id
			string givenSessionId = specialValues["sessionId"].AsString;
			if (givenSessionId != null)
				SessionId = givenSessionId;
				
			// print logs
			LogPrinter.PrintLogsFromFacetCall(specialValues["logs"]);
				
			switch (executionResult["result"].AsString)
			{
				case "ok":
					promise.Resolve(executionResult["returned"]);
					break;

				case "exception":
					var e = Serializer.FromJson<Exception>(
						executionResult["exception"],
						DeserializationContext.ServerToClient
					);
					PreserveStackTrace(e);
					promise.Reject(e);
					break;
					
				default:
					promise.Reject(
						new UnisaveException(
							"Server sent unknown response for facet call:\n"
							+ response.Body()
						)
					);
					break;
			}
		}

		/// <summary>
		/// The HTTP response wasn't 200
		/// </summary>
		private void HandleFailedRequest(
			Response response,
			Promise<JsonValue> promise
		)
		{
			var e = new HttpRequestException(
				$"[Status {response.Status}] Facet call failed:\n" +
				response.Body()
			);
			
			promise.Reject(e);
		}
		
		// magic
		// https://stackoverflow.com/a/2085377
		private static void PreserveStackTrace(Exception e)
		{
			var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
			var mgr = new ObjectManager(null, ctx);
			var si = new SerializationInfo(e.GetType(), new FormatterConverter());

			e.GetObjectData(si, ctx);
			mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
			mgr.DoFixups(); // ObjectManager calls SetObjectData

			// voila, e is unmodified save for _remoteStackTraceString
		}
    }
}
