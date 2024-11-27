using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace bet_slum
{
    public static class NetworkController
    {
        private const string AuthorizationHeaderKey = "Cookie";
        private static string _sessionToken = "";

        private const string _baseURL = "https://localhost:7208";
        private static string _apiURL = $"{_baseURL}/api";

        public struct ResponseData
        {
            public string Text;
            public Dictionary<string, string> Headers;
        }

        public static async Awaitable<ResponseData> POST(string endpoint, WWWForm body)
        {
            using(UnityWebRequest request = UnityWebRequest.Post($"{_apiURL}/game/{endpoint}", body))
            {
                request.SetRequestHeader(AuthorizationHeaderKey, _sessionToken);
                await request.SendWebRequest();
                return new() 
                { 
                    Text = request.downloadHandler.text,
                    Headers = request.GetResponseHeaders()
                };
            }
        }

        public static async Awaitable<ResponseData> GET(string url)
        {
            using(var request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader(AuthorizationHeaderKey, _sessionToken);
                await request.SendWebRequest();
                return new()
                {
                    Text = request.downloadHandler.text,
                    Headers = request.GetResponseHeaders()
                };
            }
        }

        public static void SetSessionToken(string token)
        {
            _sessionToken = token;
        }
    }
}