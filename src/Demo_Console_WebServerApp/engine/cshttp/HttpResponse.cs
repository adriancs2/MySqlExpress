// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 9112 Section 4 (status-line), RFC 9110 Section 15 (status codes)
// Specification: RFC 6266 (Content-Disposition)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Collections.Generic;
using System.Text;

namespace CsHttp
{
    /// <summary>
    /// Builds HTTP/1.1 response messages as byte arrays ready for socket writing.
    /// 
    /// This is the complement to HttpParser. Where the parser turns bytes into
    /// structured objects, this class turns structured intent back into bytes.
    /// 
    /// Not a framework. Not middleware. Just a byte array assembler that writes
    /// correct HTTP/1.1 response syntax per RFC 9112.
    /// 
    /// Two usage patterns:
    ///   1. One-liner static methods for common responses:
    ///      byte[] resp = HttpResponse.Html("&lt;h1&gt;Hello&lt;/h1&gt;");
    ///      byte[] resp = HttpResponse.Json("{\"ok\":true}");
    ///      byte[] resp = HttpResponse.Redirect("/login");
    ///   2. Builder pattern for custom headers and fine-grained control:
    ///      var resp = new HttpResponse(200);
    ///      resp.Header("Content-Type", "text/html; charset=utf-8");
    ///      resp.SetCookie("sid", "abc123", httpOnly: true);
    ///      resp.Body("&lt;h1&gt;Hello&lt;/h1&gt;");
    ///      byte[] bytes = resp.ToBytes();
    /// </summary>
    public sealed class HttpResponse
    {
        private int _statusCode;
        private string _reasonPhrase;
        private readonly List<KeyValuePair<string, string>> _headers;
        private byte[] _body;

        /// <summary>
        /// Creates a new HTTP response with the given status code.
        /// </summary>
        /// <param name="statusCode">The 3-digit HTTP status code.</param>
        /// <param name="reasonPhrase">
        /// Optional reason phrase. If null, the standard phrase for the status code is used.
        /// </param>
        public HttpResponse(int statusCode, string reasonPhrase = null)
        {
            _statusCode = statusCode;
            _reasonPhrase = reasonPhrase ?? GetDefaultReasonPhrase(statusCode);
            _headers = new List<KeyValuePair<string, string>>();
            _body = null;
        }

        /// <summary>
        /// Adds a header to the response.
        /// Multiple calls with the same name add multiple header lines.
        /// </summary>
        public HttpResponse Header(string name, string value)
        {
            _headers.Add(new KeyValuePair<string, string>(name, value));
            return this;
        }

        /// <summary>
        /// Sets the response body from raw bytes.
        /// Content-Length is automatically calculated in ToBytes().
        /// </summary>
        public HttpResponse Body(byte[] body)
        {
            _body = body;
            return this;
        }

        /// <summary>
        /// Sets the response body from a string (UTF-8 encoded).
        /// Content-Length is automatically calculated in ToBytes().
        /// </summary>
        public HttpResponse Body(string body)
        {
            _body = body != null ? Encoding.UTF8.GetBytes(body) : null;
            return this;
        }

        /// <summary>
        /// Adds a Set-Cookie header with the specified attributes.
        /// </summary>
        public HttpResponse SetCookie(
            string name, string value,
            int? maxAge = null,
            string domain = null,
            string path = null,
            bool secure = false,
            bool httpOnly = false,
            string sameSite = null,
            DateTime? expires = null)
        {
            var sb = new StringBuilder();
            sb.Append(name).Append('=').Append(value);

            if (expires.HasValue)
                sb.Append("; Expires=").Append(expires.Value.ToUniversalTime().ToString("R"));
            if (maxAge.HasValue)
                sb.Append("; Max-Age=").Append(maxAge.Value);
            if (domain != null)
                sb.Append("; Domain=").Append(domain);
            if (path != null)
                sb.Append("; Path=").Append(path);
            if (secure)
                sb.Append("; Secure");
            if (httpOnly)
                sb.Append("; HttpOnly");
            if (sameSite != null)
                sb.Append("; SameSite=").Append(sameSite);

            return Header("Set-Cookie", sb.ToString());
        }

        /// <summary>
        /// Assembles the complete HTTP response as a byte array.
        /// 
        /// Format (RFC 9112 Section 4):
        ///   HTTP/1.1 {statusCode} {reasonPhrase}\r\n
        ///   {headers}\r\n
        ///   \r\n
        ///   {body}
        /// 
        /// Content-Length is automatically added if a body is present
        /// and no Content-Length header has been explicitly set.
        /// </summary>
        public byte[] ToBytes()
        {
            var sb = new StringBuilder();

            // Status line
            sb.Append("HTTP/1.1 ");
            sb.Append(_statusCode);
            sb.Append(' ');
            sb.Append(_reasonPhrase);
            sb.Append("\r\n");

            // Check if Content-Length was already set
            bool hasContentLength = false;
            foreach (var h in _headers)
            {
                if (string.Equals(h.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    hasContentLength = true;
                    break;
                }
            }

            // Auto-add Content-Length if body is present
            int bodyLen = _body != null ? _body.Length : 0;
            if (!hasContentLength && _body != null)
            {
                sb.Append("Content-Length: ");
                sb.Append(bodyLen);
                sb.Append("\r\n");
            }

            // Headers
            foreach (var h in _headers)
            {
                sb.Append(h.Key);
                sb.Append(": ");
                sb.Append(h.Value);
                sb.Append("\r\n");
            }

            // Blank line (end of headers)
            sb.Append("\r\n");

            // Convert header section to bytes
            byte[] headerBytes = Encoding.ASCII.GetBytes(sb.ToString());

            if (_body == null || _body.Length == 0)
                return headerBytes;

            // Combine header and body
            byte[] result = new byte[headerBytes.Length + _body.Length];
            Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
            Buffer.BlockCopy(_body, 0, result, headerBytes.Length, _body.Length);
            return result;
        }

        // ─── Static Convenience Methods ──────────────────────────────

        /// <summary>
        /// Builds an HTML response (200 OK, text/html; charset=utf-8).
        /// </summary>
        public static byte[] Html(string html)
        {
            return new HttpResponse(200)
                .Header("Content-Type", "text/html; charset=utf-8")
                .Body(html)
                .ToBytes();
        }

        /// <summary>
        /// Builds a JSON response (200 OK, application/json; charset=utf-8).
        /// </summary>
        public static byte[] Json(string json)
        {
            return new HttpResponse(200)
                .Header("Content-Type", "application/json; charset=utf-8")
                .Body(json)
                .ToBytes();
        }

        /// <summary>
        /// Builds a plain text response (200 OK, text/plain; charset=utf-8).
        /// </summary>
        public static byte[] Text(string text)
        {
            return new HttpResponse(200)
                .Header("Content-Type", "text/plain; charset=utf-8")
                .Body(text)
                .ToBytes();
        }

        /// <summary>
        /// Builds a binary response with the specified content type.
        /// </summary>
        public static byte[] Bytes(byte[] data, string contentType)
        {
            return new HttpResponse(200)
                .Header("Content-Type", contentType)
                .Body(data)
                .ToBytes();
        }

        /// <summary>
        /// Builds a status-only response with no body (e.g., 204 No Content, 404 Not Found).
        /// </summary>
        public static byte[] Status(int statusCode)
        {
            return new HttpResponse(statusCode)
                .ToBytes();
        }

        /// <summary>
        /// Builds a redirect response (302 Found) to the specified URL.
        /// </summary>
        public static byte[] Redirect(string url)
        {
            return new HttpResponse(302)
                .Header("Location", url)
                .ToBytes();
        }

        /// <summary>
        /// Builds a permanent redirect response (301 Moved Permanently).
        /// </summary>
        public static byte[] RedirectPermanent(string url)
        {
            return new HttpResponse(301)
                .Header("Location", url)
                .ToBytes();
        }

        /// <summary>
        /// Builds a file download response with Content-Disposition: attachment.
        /// Per RFC 6266, the filename is quoted in the header value.
        /// </summary>
        public static byte[] File(byte[] data, string fileName, string contentType)
        {
            return new HttpResponse(200)
                .Header("Content-Type", contentType)
                .Header("Content-Disposition", "attachment; filename=\"" + fileName + "\"")
                .Body(data)
                .ToBytes();
        }

        // ─── Standard Reason Phrases ─────────────────────────────────

        /// <summary>
        /// Returns the standard reason phrase for a status code per RFC 9110 Section 15.
        /// Returns "Unknown" for unrecognized status codes.
        /// </summary>
        public static string GetDefaultReasonPhrase(int statusCode)
        {
            switch (statusCode)
            {
                // 1xx Informational
                case 100: return "Continue";
                case 101: return "Switching Protocols";
                case 102: return "Processing";
                case 103: return "Early Hints";

                // 2xx Success
                case 200: return "OK";
                case 201: return "Created";
                case 202: return "Accepted";
                case 203: return "Non-Authoritative Information";
                case 204: return "No Content";
                case 205: return "Reset Content";
                case 206: return "Partial Content";
                case 207: return "Multi-Status";
                case 208: return "Already Reported";
                case 226: return "IM Used";

                // 3xx Redirection
                case 300: return "Multiple Choices";
                case 301: return "Moved Permanently";
                case 302: return "Found";
                case 303: return "See Other";
                case 304: return "Not Modified";
                case 305: return "Use Proxy";
                case 307: return "Temporary Redirect";
                case 308: return "Permanent Redirect";

                // 4xx Client Error
                case 400: return "Bad Request";
                case 401: return "Unauthorized";
                case 402: return "Payment Required";
                case 403: return "Forbidden";
                case 404: return "Not Found";
                case 405: return "Method Not Allowed";
                case 406: return "Not Acceptable";
                case 407: return "Proxy Authentication Required";
                case 408: return "Request Timeout";
                case 409: return "Conflict";
                case 410: return "Gone";
                case 411: return "Length Required";
                case 412: return "Precondition Failed";
                case 413: return "Content Too Large";
                case 414: return "URI Too Long";
                case 415: return "Unsupported Media Type";
                case 416: return "Range Not Satisfiable";
                case 417: return "Expectation Failed";
                case 418: return "I'm a Teapot";
                case 421: return "Misdirected Request";
                case 422: return "Unprocessable Content";
                case 423: return "Locked";
                case 424: return "Failed Dependency";
                case 425: return "Too Early";
                case 426: return "Upgrade Required";
                case 428: return "Precondition Required";
                case 429: return "Too Many Requests";
                case 431: return "Request Header Fields Too Large";
                case 451: return "Unavailable For Legal Reasons";

                // 5xx Server Error
                case 500: return "Internal Server Error";
                case 501: return "Not Implemented";
                case 502: return "Bad Gateway";
                case 503: return "Service Unavailable";
                case 504: return "Gateway Timeout";
                case 505: return "HTTP Version Not Supported";
                case 506: return "Variant Also Negotiates";
                case 507: return "Insufficient Storage";
                case 508: return "Loop Detected";
                case 510: return "Not Extended";
                case 511: return "Network Authentication Required";

                default: return "Unknown";
            }
        }
    }
}