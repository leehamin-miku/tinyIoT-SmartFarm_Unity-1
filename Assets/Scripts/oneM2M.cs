using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net;

namespace IoT
{
    public class OneM2M : MonoBehaviour
    {
        //public static string baseUrl = "http://203.250.148.89:3000/TinyIoT";
        public static string baseUrl = "http://127.0.0.1:3000/TinyIoT";
        public static bool checkCommand = false;

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {                 
            return true;
        }

        private static void ConfigureHttps()
        {            
            if (baseUrl.StartsWith("https"))
            {
                ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
        }

        public static IEnumerator PostDataCoroutine(string origin, int type, string body, string token = "", string url = "", Action<string> callback = null)
        {
            string endpoint = url == "" ? baseUrl : $"{baseUrl}/{url}";
            Debug.Log(endpoint);
            System.Random rand = new System.Random();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

            using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", $"application/json;ty={type}");
                request.SetRequestHeader("X-M2M-Origin", origin);
                request.SetRequestHeader("X-M2M-RI", rand.Next().ToString());
                request.SetRequestHeader("X-M2M-RVI", "3");

                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
                }

                yield return request.SendWebRequest();

                string jsonResponse = request.downloadHandler.text;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Server response: {jsonResponse}");
                }
                else
                {
                    Debug.LogError($"Server error: {jsonResponse}");
                }

                callback?.Invoke(jsonResponse);
            }
        }

        public static IEnumerator GetDataCoroutine(string origin, string token = "", string url = "", Action<string> callback = null)
        {
            string endpoint = url == "" ? baseUrl : $"{baseUrl}/{url}";
            Debug.Log(endpoint);
            System.Random rand = new System.Random();

            using (UnityWebRequest request = UnityWebRequest.Get(endpoint))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("X-M2M-Origin", origin);
                request.SetRequestHeader("X-M2M-RI", rand.Next().ToString());
                request.SetRequestHeader("X-M2M-RVI", "3");

                if (!string.IsNullOrEmpty(token))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");
                }

                yield return request.SendWebRequest();

                string jsonResponse = request.downloadHandler.text;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Server response: {jsonResponse}");
                }
                else
                {
                    Debug.LogError($"Server error: {jsonResponse}");
                }

                callback?.Invoke(jsonResponse);
            }
        }
    }

    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {           
            return true;
        }
    }
}