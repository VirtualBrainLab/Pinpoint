using System;
using System.IO;
using System.Net;
using System.Text;
using LightJson;
using LightJson.Serialization;
using Unisave.Exceptions;

namespace Unisave.Editor.BackendUploading
{
    /// <summary>
    /// Wrapper around blocking HTTP requests.
    /// Intended for use exclusively inside the backend uploader.
    /// </summary>
    public static class Http
    {
        /// <summary>
        /// Make a POST request at a given URL with JSON communication
        /// </summary>
        /// <param name="url"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        /// <exception cref="UnisaveException"></exception>
        /// <exception cref="Exception"></exception>
        public static JsonObject Post(string url, JsonObject payload)
        {
            byte[] payloadBytes = new UTF8Encoding().GetBytes(payload.ToString());

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.ContentLength = payloadBytes.LongLength;

            request.GetRequestStream().Write(payloadBytes, 0, payloadBytes.Length);

            string responseString = null;

            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                        responseString = sr.ReadToEnd();

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
                    if ((int)((HttpWebResponse)((WebException)e).Response).StatusCode == 401)
                        throw new UnisaveException(
                            "Server response to backend uploader was 401 unauthorized.\n"
                            + "Check that your game token and editor key are correctly set up."
                        );

                    if ((int)((HttpWebResponse)((WebException)e).Response).StatusCode == 429)
                        throw new UnisaveException(
                            "Server refuses backend uploader requests due to their amount. This happens when Unity editor does a lot of recompiling.\n"
                            + "Simply wait for a while and this problem will go away."
                        );
                }

                throw;
            }
        }

        /// <summary>
        /// Test server connection
        /// </summary>
        public static bool UrlReachable(string url)
        {
            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Timeout = 15000;
            request.Method = "HEAD";
            try
            {
                using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
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