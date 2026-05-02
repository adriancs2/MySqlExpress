// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 9112 (HTTP/1.1 Message Syntax)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System.Collections.Generic;
using System.Text;

namespace CsHttp
{
    /// <summary>
    /// Base class for a parsed HTTP/1.1 message.
    /// 
    /// Per RFC 9112 Section 2.1, both request and response share the same
    /// structure: start-line CRLF *(field-line CRLF) CRLF [message-body]
    /// They differ only in the start-line format and body-length determination.
    /// </summary>
    public abstract class HttpMessage
    {
        /// <summary>
        /// The HTTP version as received (e.g., "HTTP/1.1", "HTTP/1.0").
        /// Per RFC 9112 Section 2.3, this is case-sensitive.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// The major version number extracted from the HTTP-version field.
        /// </summary>
        public int VersionMajor { get; set; }

        /// <summary>
        /// The minor version number extracted from the HTTP-version field.
        /// </summary>
        public int VersionMinor { get; set; }

        /// <summary>
        /// The header field collection, preserving wire order with case-insensitive keyed access.
        /// </summary>
        public HttpHeaderCollection Headers { get; set; }

        /// <summary>
        /// The decoded message body content (after chunked decoding if applicable).
        /// Null if no message body is present.
        /// </summary>
        public byte[] Body { get; set; }

        /// <summary>
        /// The raw message body as received on the wire (before chunked decoding).
        /// For Content-Length framed messages, this is identical to Body.
        /// For chunked messages, this is the raw chunked stream; Body is the decoded content.
        /// Null if no message body is present.
        /// </summary>
        public byte[] RawBody { get; set; }

        /// <summary>
        /// Trailer fields received after a chunked message body.
        /// Empty if no trailers were present.
        /// Per RFC 9112 Section 7.1.2.
        /// </summary>
        public HttpHeaderCollection Trailers { get; set; }

        /// <summary>
        /// How the message body length was determined.
        /// Maps to the 8 rules in RFC 9112 Section 6.3.
        /// </summary>
        public BodyFrameKind BodyFraming { get; set; }

        /// <summary>
        /// The raw bytes of the complete start-line as received.
        /// Available for security auditing.
        /// </summary>
        public byte[] RawStartLine { get; set; }

        /// <summary>
        /// Total number of bytes consumed from the input to parse this message.
        /// Useful for pipelined request parsing where multiple messages 
        /// are concatenated in a single byte stream.
        /// </summary>
        public int BytesConsumed { get; set; }

        protected HttpMessage()
        {
            Headers = new HttpHeaderCollection();
            Trailers = new HttpHeaderCollection();
        }
    }

    /// <summary>
    /// A parsed HTTP/1.1 request message.
    /// 
    /// RFC 9112 Section 3:
    ///   request-line = method SP request-target SP HTTP-version
    /// 
    /// Content accessors (Path, QueryString, Form, Files, Cookies) are lazy-parsed:
    /// they are not computed until first access, then cached for all subsequent access.
    /// A developer who only reads Method and Headers pays zero cost for content parsing.
    /// </summary>
    public sealed class HttpRequestMessage : HttpMessage
    {
        /// <summary>
        /// The request method token (case-sensitive).
        /// e.g., "GET", "POST", "DELETE".
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// The request-target as received (not decoded or normalized).
        /// e.g., "/api/users?page=2", "http://example.com/path", "*"
        /// </summary>
        public string RequestTarget { get; set; }

        /// <summary>
        /// The form of the request-target as determined during parsing.
        /// </summary>
        public RequestTargetForm RequestTargetForm { get; set; }

        // ─── Content Parser Options ──────────────────────────────────

        /// <summary>
        /// Options for content-level parsing (query string, form, multipart, cookies).
        /// Set this before accessing any lazy-parsed properties to control limits
        /// and policies. If not set, ContentParserOptions.Default is used.
        /// </summary>
        public ContentParserOptions ContentOptions { get; set; }

        // ─── Lazy-Parsed Content Properties ──────────────────────────

        // Backing fields for lazy parsing (null = not yet parsed)
        private string _path;
        private bool _pathParsed;
        private string _rawQueryString;
        private bool _rawQueryStringParsed;
        private HttpContentCollection _queryString;
        private bool _queryStringParsed;
        private HttpContentCollection _form;
        private bool _formParsed;
        private HttpPostedFileCollection _files;
        private HttpContentCollection _multipartFields;
        private bool _multipartParsed;
        private HttpContentCollection _cookies;
        private bool _cookiesParsed;

        /// <summary>
        /// The decoded URL path from the RequestTarget.
        /// Percent-decoded per RFC 3986 Section 2.1.
        /// 
        /// For "/search?q=hello", returns "/search".
        /// For "/path%2Fto%2Fpage", returns "/path/to/page".
        /// 
        /// Lazy-parsed on first access.
        /// </summary>
        public string Path
        {
            get
            {
                if (!_pathParsed)
                {
                    _path = QueryStringParser.ExtractPath(RequestTarget, ContentOptions);
                    _pathParsed = true;
                }
                return _path;
            }
        }

        /// <summary>
        /// The raw query string from the RequestTarget, NOT percent-decoded.
        /// Does not include the leading '?' delimiter.
        /// 
        /// For "/search?q=hello%20world&amp;page=2", returns "q=hello%20world&amp;page=2".
        /// For "/path", returns null.
        /// 
        /// Lazy-parsed on first access.
        /// </summary>
        public string RawQueryString
        {
            get
            {
                if (!_rawQueryStringParsed)
                {
                    _rawQueryString = QueryStringParser.ExtractFromTarget(RequestTarget);
                    _rawQueryStringParsed = true;
                }
                return _rawQueryString;
            }
        }

        /// <summary>
        /// The parsed query string parameters as a key-value collection.
        /// Keys and values are percent-decoded.
        /// 
        /// Access: req.QueryString["page"] → "2"
        /// All values: req.QueryString.GetValues("tag") → ["news", "tech"]
        /// All keys: req.QueryString.AllKeys → ["q", "page"]
        /// 
        /// Lazy-parsed on first access.
        /// </summary>
        public HttpContentCollection QueryString
        {
            get
            {
                if (!_queryStringParsed)
                {
                    string raw = RawQueryString;
                    if (raw == null)
                    {
                        _queryString = HttpContentCollection.Empty;
                    }
                    else
                    {
                        var result = QueryStringParser.Parse(raw, ContentOptions);
                        _queryString = result.Success ? result.Collection : HttpContentCollection.Empty;
                    }
                    _queryStringParsed = true;
                }
                return _queryString;
            }
        }

        /// <summary>
        /// The parsed form body parameters (application/x-www-form-urlencoded).
        /// Keys and values are percent-decoded.
        /// 
        /// Access: req.Form["email"] → "user@example.com"
        /// 
        /// Returns an empty collection if the Content-Type is not
        /// application/x-www-form-urlencoded, or if no body is present.
        /// 
        /// For multipart/form-data, text-only form fields are also accessible
        /// here (merged from the multipart parser).
        /// 
        /// Lazy-parsed on first access.
        /// </summary>
        public HttpContentCollection Form
        {
            get
            {
                if (!_formParsed)
                {
                    ParseFormAndFiles();
                    _formParsed = true;
                }
                return _form;
            }
        }

        /// <summary>
        /// The uploaded files from a multipart/form-data request body.
        /// 
        /// Access: req.Files["avatar"] → HttpPostedFile
        ///         req.Files["avatar"].FileName → "photo.jpg"
        ///         req.Files["avatar"].Bytes → byte[]
        /// 
        /// Returns an empty collection if the Content-Type is not
        /// multipart/form-data or if no files were uploaded.
        /// 
        /// Lazy-parsed on first access. Parsing Form also triggers file parsing.
        /// </summary>
        public HttpPostedFileCollection Files
        {
            get
            {
                if (!_multipartParsed && !_formParsed)
                {
                    ParseFormAndFiles();
                    _formParsed = true;
                }
                return _files ?? HttpPostedFileCollection.Empty;
            }
        }

        /// <summary>
        /// The request cookies parsed from the Cookie header.
        /// 
        /// Access: req.Cookies["sid"] → "abc123"
        ///         req.Cookies.AllKeys → ["sid", "theme"]
        /// 
        /// Returns an empty collection if no Cookie header is present.
        /// 
        /// Lazy-parsed on first access.
        /// </summary>
        public HttpContentCollection Cookies
        {
            get
            {
                if (!_cookiesParsed)
                {
                    string cookieHeader = Headers["Cookie"];
                    if (cookieHeader == null)
                    {
                        _cookies = HttpContentCollection.Empty;
                    }
                    else
                    {
                        var result = CookieParser.Parse(cookieHeader, ContentOptions);
                        _cookies = result.Success ? result.Collection : HttpContentCollection.Empty;
                    }
                    _cookiesParsed = true;
                }
                return _cookies;
            }
        }

        /// <summary>
        /// Combined indexer — searches QueryString → Form → Cookies → Headers.
        /// Returns the first non-null match, or null if not found in any collection.
        /// 
        /// This follows the ASP.NET Web Forms Request["key"] convention.
        /// The search order is: QueryString, Form, Cookies, Headers.
        /// 
        /// For unambiguous access, use the explicit collections directly:
        ///   req.QueryString["key"], req.Form["key"], req.Cookies["key"], req.Headers["key"]
        /// 
        /// Note: a cookie named "email" could shadow a missing form field,
        /// or a query string parameter could override a form POST value.
        /// Use explicit access when precision matters.
        /// </summary>
        /// <param name="key">The parameter name to search for.</param>
        /// <returns>The first matching value, or null.</returns>
        public string this[string key]
        {
            get
            {
                return QueryString[key]
                    ?? Form[key]
                    ?? Cookies[key]
                    ?? Headers[key];
            }
        }

        // ─── Private: Form + File Parsing ────────────────────────────

        /// <summary>
        /// Parses both form fields and uploaded files from the request body.
        /// Called lazily when either Form or Files is first accessed.
        /// 
        /// Handles two Content-Types:
        ///   - application/x-www-form-urlencoded → FormParser
        ///   - multipart/form-data → MultipartParser (fields + files)
        /// </summary>
        private void ParseFormAndFiles()
        {
            _files = HttpPostedFileCollection.Empty;
            _form = HttpContentCollection.Empty;
            _multipartParsed = true;

            string contentType = Headers["Content-Type"];
            if (contentType == null || Body == null || Body.Length == 0)
                return;

            if (FormParser.IsFormContentType(contentType))
            {
                // application/x-www-form-urlencoded
                var result = FormParser.Parse(Body, contentType, ContentOptions);
                if (result.Success)
                    _form = result.Collection;
            }
            else if (MultipartParser.IsMultipartContentType(contentType))
            {
                // multipart/form-data
                var result = MultipartParser.Parse(Body, contentType, ContentOptions);
                if (result.Success)
                {
                    _form = result.FormFields ?? HttpContentCollection.Empty;
                    _files = result.Files ?? HttpPostedFileCollection.Empty;
                }
            }
        }
    }

    /// <summary>
    /// A parsed HTTP/1.1 response message.
    /// 
    /// RFC 9112 Section 4:
    ///   status-line = HTTP-version SP status-code SP [reason-phrase]
    /// </summary>
    public sealed class HttpResponseMessage : HttpMessage
    {
        /// <summary>
        /// The 3-digit status code as an integer.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// The reason phrase as received. May be empty.
        /// Per RFC 9112 Section 4, clients SHOULD ignore this.
        /// </summary>
        public string ReasonPhrase { get; set; }

        // ─── Lazy-Parsed Content Properties ──────────────────────────

        private SetCookieCollection _setCookies;
        private bool _setCookiesParsed;

        /// <summary>
        /// The parsed Set-Cookie headers from the response.
        /// 
        /// Access: resp.SetCookies["sid"] → SetCookie object
        ///         resp.SetCookies["sid"].Value → "abc123"
        ///         resp.SetCookies["sid"].HttpOnly → true
        /// 
        /// Returns an empty collection if no Set-Cookie headers are present.
        /// 
        /// Lazy-parsed on first access.
        /// </summary>
        public SetCookieCollection SetCookies
        {
            get
            {
                if (!_setCookiesParsed)
                {
                    _setCookies = SetCookieParser.ParseAll(Headers);
                    _setCookiesParsed = true;
                }
                return _setCookies;
            }
        }
    }

    /// <summary>
    /// Identifies how the message body length was determined.
    /// Per RFC 9112 Section 6.3 (rules 1-8).
    /// </summary>
    public enum BodyFrameKind
    {
        /// <summary>No body present.</summary>
        None,

        /// <summary>Rule 1: HEAD response or 1xx/204/304 — no body possible.</summary>
        NoBodyByStatus,

        /// <summary>Rule 2: 2xx response to CONNECT — tunnel follows.</summary>
        Tunnel,

        /// <summary>Rule 4: Chunked transfer coding.</summary>
        Chunked,

        /// <summary>Rule 6: Content-Length specifies exact byte count.</summary>
        ContentLength,

        /// <summary>Rule 7: Request with no Content-Length or Transfer-Encoding — zero body.</summary>
        ZeroByAbsence,

        /// <summary>Rule 8: Response with no framing — read until connection close.</summary>
        UntilClose,
    }

    /// <summary>
    /// The four forms of request-target per RFC 9112 Section 3.2.
    /// </summary>
    public enum RequestTargetForm
    {
        /// <summary>Section 3.2.1: absolute-path ["?" query]</summary>
        Origin,

        /// <summary>Section 3.2.2: absolute-URI</summary>
        Absolute,

        /// <summary>Section 3.2.3: uri-host ":" port (CONNECT only)</summary>
        Authority,

        /// <summary>Section 3.2.4: "*" (server-wide OPTIONS only)</summary>
        Asterisk,
    }
}
