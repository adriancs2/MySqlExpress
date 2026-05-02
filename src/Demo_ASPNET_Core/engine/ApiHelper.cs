using System;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Demo_ASPNET_Core.engine
{
    /// <summary>
    /// JSON + response-ending helpers for handlers.
    ///
    /// In the Web Forms sibling these methods read HttpContext.Current
    /// implicitly. ASP.NET Core has no ambient context, so every helper
    /// takes the HttpContext explicitly. Same shape, one extra argument.
    ///
    /// EndResponse() is gone — in ASP.NET Core, returning from the handler
    /// short-circuits the pipeline naturally. No "complete request" call is
    /// needed; do the writes you want, then return.
    ///
    /// Sync edition: every helper writes via ctx.Response.Body.Write(),
    /// which is the synchronous Stream API. AllowSynchronousIO must be
    /// enabled on the server (see Program.cs).
    /// </summary>
    public static class ApiHelper
    {
        // System.Text.Json defaults to PascalCase for property names. The
        // Web Forms version used Newtonsoft with default settings, which
        // also emits PascalCase. We keep that behaviour explicitly.
        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
        };

        public static string GetBaseUrl(HttpContext ctx)
        {
            HttpRequest req = ctx.Request;
            string host = req.Host.HasValue ? req.Host.Value : "localhost";
            return $"{req.Scheme}://{host}";
        }

        /// <summary>
        /// Synchronous write helper — encodes the string as UTF-8 and
        /// writes it directly to the response body. Used by every page
        /// and JSON endpoint.
        /// </summary>
        public static void WriteText(HttpContext ctx, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? "");
            ctx.Response.Body.Write(bytes, 0, bytes.Length);
        }

        public static void WriteHtml(HttpContext ctx, string html)
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            WriteText(ctx, html);
        }

        public static void WriteJson(HttpContext ctx, object obj)
        {
            ctx.Response.ContentType = "application/json; charset=utf-8";
            string json = JsonSerializer.Serialize(obj, JsonOptions);
            WriteText(ctx, json);
        }

        public static void WriteSuccess(HttpContext ctx, string message = "Success")
        {
            WriteJson(ctx, new { success = true, message });
        }

        public static void WriteError(HttpContext ctx, string message, int statusCode = 400)
        {
            ctx.Response.StatusCode = statusCode;
            WriteJson(ctx, new { success = false, message });
        }
    }
}
