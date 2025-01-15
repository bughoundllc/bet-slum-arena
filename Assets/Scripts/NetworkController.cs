using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace bet_slum
{
    public static class NetworkController
    {
        private const string AuthorizationHeaderKey = "Cookie";
        private static string _sessionToken = "";

        private const string _productionRelayURL = "http://34.174.66.135:5000";
        private const string _developmentRelayURL = "http://localhost:5001";
        private const string _baseURL = _developmentRelayURL;
        private static string _apiURL = $"{_baseURL}/api";

        public struct ResponseData
        {
            public string Text;
            public string? Error;
            public Dictionary<string, string> Headers;
        }

        public static async Awaitable<ResponseData> POST(string controller, string endpoint, WWWForm body)
        {
            using(UnityWebRequest request = UnityWebRequest.Post($"{_apiURL}/{controller}/{endpoint}", body))
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

        public static async Awaitable<ResponseData> GET(string controller, string endpoint)
        {
            using(var request = UnityWebRequest.Get($"{_apiURL}/{controller}/{endpoint}"))
            {
                request.SetRequestHeader(AuthorizationHeaderKey, _sessionToken);
                await request.SendWebRequest();
                return new()
                {
                    Text = request.downloadHandler.text,
                    Headers = request.GetResponseHeaders(),
                    Error = request.error
                };
            }
        }

        public static void SetSessionToken(string token)
        {
            _sessionToken = token;
        }
    }
}