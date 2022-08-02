using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unisave.Utils
{
    /// <summary>
    /// Helper for getting unisave api urls
    /// </summary>
    public class ApiUrl
    {
        private string serverUrl;

        public ApiUrl(string serverUrl)
        {
            this.serverUrl = serverUrl;

            if (!this.serverUrl.EndsWith("/"))
                this.serverUrl += "/";
        }

        public string Index() => serverUrl;

        private string Url(string relativeUrl)
        {
            return serverUrl + "_api/" + relativeUrl;
        }
        
        private string BroadcastingUrl(string relativeUrl)
        {
            return serverUrl + "_broadcasting/" + relativeUrl;
        }

        public string CallFacet() => Url("call-facet");
        
        public string BackendUpload_Start() => Url("backend-upload/start");
        public string BackendUpload_File() => Url("backend-upload/file");
        public string BackendUpload_Finish() => Url("backend-upload/finish");

        public string RegisterBuild() => Url("register-build");

        public string BroadcastingListen() => BroadcastingUrl("listen");
        public string BroadcastingUnsubscribe() => BroadcastingUrl("unsubscribe");
    }
}
