using Newtonsoft.Json;

namespace System
{
    /// <summary>
    /// JSON-response helpers used by every <c>/api/*</c> endpoint.
    ///
    /// In the ASP.NET version, these reached for <c>HttpContext.Current</c>
    /// to find the response. In the console version, the per-request
    /// <see cref="Ctx"/> is passed in explicitly — same pattern, no
    /// ambient state.
    /// </summary>
    public static class ApiHelper
    {
        public static void WriteJson(Ctx ctx, object obj)
        {
            ctx.ContentType = "application/json; charset=utf-8";
            ctx.Out.Clear();
            ctx.Out.Append(JsonConvert.SerializeObject(obj));
        }

        public static void WriteSuccess(Ctx ctx, string message = "Success")
        {
            WriteJson(ctx, new { success = true, message });
        }

        public static void WriteError(Ctx ctx, string message, int statusCode = 400)
        {
            ctx.StatusCode = statusCode;
            WriteJson(ctx, new { success = false, message });
        }
    }
}
