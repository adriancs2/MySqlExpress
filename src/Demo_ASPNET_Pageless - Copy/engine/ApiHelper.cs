using Newtonsoft.Json;
using System;
using System.Web;

namespace System
{
    public static class ApiHelper
    {
        static HttpRequest Request
        {
            get
            {
                if (HttpContext.Current == null)
                    throw new InvalidOperationException("ApiHelper called outside of an HTTP request context.");
                return HttpContext.Current.Request;
            }
        }

        static HttpResponse Response
        {
            get
            {
                if (HttpContext.Current == null)
                    throw new InvalidOperationException("ApiHelper called outside of an HTTP request context.");
                return HttpContext.Current.Response;
            }
        }

        public static string GetBaseUrl()
        {
            Uri url = Request.Url;
            return $"{url.Scheme}://{url.Host}{(url.IsDefaultPort ? "" : ":" + url.Port)}";
        }

        public static void EndResponse()
        {
            // So IIS will skip handling custom errors
            Response.TrySkipIisCustomErrors = true;

            try
            {
                Response.Flush();
            }
            catch { /* client already disconnected — ignore */ }

            Response.SuppressContent = true;

            // The most reliable way in WebForms / IIS-integrated pipeline
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        public static void WriteJson(object obj)
        {
            Response.ContentType = "application/json";
            Response.Write(JsonConvert.SerializeObject(obj));
        }

        public static void WriteSuccess(string message = "Success")
        {
            WriteJson(new { success = true, message });
        }

        public static void WriteError(string message, int statusCode = 400)
        {
            Response.StatusCode = statusCode;
            WriteJson(new { success = false, message });
        }
    }
}