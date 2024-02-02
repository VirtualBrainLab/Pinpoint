using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LightJson;
using LightJson.Serialization;
using Unisave.Exceptions;

namespace Unisave.Editor.BackendUploading
{
    /// <summary>
    /// Wrapper around HTTP requests.
    /// Intended for use exclusively inside the backend uploader.
    /// </summary>
    public static class Http
    {
        public static JsonObject Post(string url, JsonObject payload)
        {
            return Task.Run(
                () => PostAsync(url, payload, CancellationToken.None)
            ).Result;
        }

        /// <summary>
        /// Make a POST request at a given URL with JSON communication
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="UnisaveException"></exception>
        /// <exception cref="Exception"></exception>
        public static async Task<JsonObject> PostAsync(
            string url,
            JsonObject payload,
            CancellationToken cancellationToken
        )
        {
            byte[] payloadBytes = new UTF8Encoding().GetBytes(payload.ToString());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.ContentLength = payloadBytes.LongLength;
            request.Timeout = 20_000; // 20s, default is 100s

            await request.GetRequestStream().WriteAsync(
                payloadBytes, 0, payloadBytes.Length, cancellationToken
            );

            string responseString = null;

            try
            {
                using (var response = (HttpWebResponse) await request.GetResponseAsync())
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                        responseString = await sr.ReadToEndAsync();

                    if ((int)response.StatusCode != 200)
                        throw new UnisaveException(
                            "Server responded with non 200 response:\n" +
                            responseString
                        );
                    
                    return JsonReader.Parse(responseString);
                }
            }
            catch (Exception e)
            {
                if (e is WebException)
                {
                    WebException we = (WebException) e;
                    HttpWebResponse response = (HttpWebResponse) we.Response;
                    
                    if ((int)response.StatusCode == 401)
                        throw new BackendUploadingException(
                            "Server response to backend uploader was 401 unauthorized.\n"
                            + "Check that your game token and editor key are correctly set up."
                        );

                    if ((int)response.StatusCode == 429)
                        throw new BackendUploadingException(
                            "Server refuses backend uploader requests due to their amount. This happens when Unity editor does a lot of recompiling.\n"
                            + "Simply wait for a while and this problem will go away."
                        );
                    
                    // read the response
                    using (var sr = new StreamReader(response.GetResponseStream()))
                        responseString = await sr.ReadToEndAsync();
                        
                    throw new BackendUploadingException(
                        "Server responded with non 200 response:\n" +
                        responseString
                    );
                }

                throw;
            }
        }

        public static bool UrlReachable(string url)
        {
            return Task.Run(
                () => UrlReachableAsync(url, CancellationToken.None)
            ).Result;
        }

        /// <summary>
        /// Test server connection
        /// </summary>
        public static async Task<bool> UrlReachableAsync(
            string url, CancellationToken cancellationToken
        )
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Timeout = 15_000; // 15s
            request.Method = "HEAD";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync())
                {
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }
    }
}