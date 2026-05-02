using CsHttp;
using System.Collections.Generic;
using System.Text;

namespace System
{
    /// <summary>
    /// Per-request context.
    ///
    /// In ASP.NET, every handler reaches up into <c>HttpContext.Current</c>
    /// to find Request and Response. There is no <c>HttpContext.Current</c>
    /// in a console app — each socket connection is just bytes coming and
    /// going. So we do the same job explicitly: every handler takes a
    /// <see cref="Ctx"/> parameter, reads the parsed request from it, and
    /// writes the response back into it. The dispatcher then turns that
    /// response into bytes and sends it down the socket.
    ///
    /// This is the "implicit ambient context" of ASP.NET, made explicit.
    /// </summary>
    public sealed class Ctx
    {
        /// <summary>The cshttp-parsed request. Read-only after construction.</summary>
        public HttpRequestMessage Request { get; }

        /// <summary>The lower-cased, trailing-slash-stripped path used by the dispatcher.</summary>
        public string Path { get; }

        /// <summary>Output buffer for HTML/JSON/text bodies. Handlers append into this.</summary>
        public StringBuilder Out { get; } = new StringBuilder();

        /// <summary>Raw byte body — used when a handler wants to write binary (e.g. a file download).</summary>
        public byte[] OutBytes { get; set; }

        /// <summary>HTTP status code. Defaults to 200 OK.</summary>
        public int StatusCode { get; set; } = 200;

        /// <summary>Content-Type header. Defaults to text/html for HTML pages.</summary>
        public string ContentType { get; set; } = "text/html; charset=utf-8";

        /// <summary>Extra response headers (Location, Set-Cookie, etc.).</summary>
        public List<KeyValuePair<string, string>> Headers { get; } =
            new List<KeyValuePair<string, string>>();

        /// <summary>True once a redirect or short-circuit response has been written.</summary>
        public bool ResponseFinalized { get; set; }

        public Ctx(HttpRequestMessage request, string path)
        {
            Request = request;
            Path = path;
        }

        /// <summary>
        /// Build the final response bytes ready for the socket.
        /// Mirrors the role <c>HttpResponse.End</c> plays in ASP.NET, but explicit.
        /// </summary>
        public byte[] BuildResponseBytes()
        {
            var resp = new HttpResponse(StatusCode);
            resp.Header("Content-Type", ContentType);
            resp.Header("Server", "Demo_Console_WebServerApp");
            resp.Header("Connection", "close");

            foreach (var h in Headers)
                resp.Header(h.Key, h.Value);

            if (OutBytes != null)
            {
                resp.Body(OutBytes);
            }
            else if (Out.Length > 0)
            {
                resp.Body(Out.ToString());
            }

            return resp.ToBytes();
        }

        // ─── Convenience shortcuts that handlers use frequently ────────

        /// <summary>Write a 302 redirect. The handler should return immediately after.</summary>
        public void Redirect(string url)
        {
            StatusCode = 302;
            Headers.Add(new KeyValuePair<string, string>("Location", url));
            ContentType = "text/plain; charset=utf-8";
            Out.Clear();
            ResponseFinalized = true;
        }

        /// <summary>
        /// Returns the value of a form-field, query-string param, or cookie,
        /// in that order — same fallthrough as ASP.NET's <c>Request[key]</c>.
        /// Always returns a non-null string (empty string when missing).
        /// </summary>
        public string GetParam(string key)
        {
            // cshttp's HttpRequestMessage already provides this via the indexer,
            // but we wrap it here so handlers can pass through Ctx without
            // touching cshttp types directly when they don't need to.
            string v = Request[key];
            return v ?? "";
        }
    }
}
