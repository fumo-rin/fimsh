using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;

namespace Pinwheel.Poseidon
{
    public enum UILocation
    {
        Inspector
    }

    public static class NetUtils
    {
        public static void TrackClick(string button_name, UILocation location)
        {
            const string ENDPOINT_URL = "https://api.pinwheelstud.io/pwi/editor/btn-click/";

            string buttonId = $"{button_name.ToLower().Replace(" ", "_")}__{location.ToString().ToLower()}";
            if (string.IsNullOrEmpty(buttonId))
                return;

            var payload =
                "{\"product\":\"" + Escape(PVersionInfo.ProductNameShort) +
                "\",\"button_id\":\"" + Escape(buttonId) + "\"}";

            var request = new UnityWebRequest(ENDPOINT_URL, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload)),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.disposeUploadHandlerOnDispose = true;
            request.disposeDownloadHandlerOnDispose = true;

            request.SetRequestHeader("Content-Type", "application/json");
            var ops = request.SendWebRequest();
            ops.completed += _ => { request.Dispose(); };
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
