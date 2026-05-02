// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 6265 Section 4.1, RFC 6265bis (SameSite)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;

namespace CsHttp
{
    /// <summary>
    /// Represents a parsed Set-Cookie response header with all attributes.
    /// 
    /// RFC 6265 Section 4.1 defines Set-Cookie syntax:
    ///   Set-Cookie: name=value; Expires=...; Max-Age=...; Domain=...;
    ///               Path=...; Secure; HttpOnly; SameSite=...
    /// 
    /// The Name and Value are always present. All attributes are optional.
    /// 
    /// RFC 6265bis (draft, updates RFC 6265) adds the SameSite attribute
    /// with values Lax, Strict, and None.
    /// </summary>
    public sealed class SetCookie
    {
        /// <summary>
        /// The cookie name. Always present (non-null, non-empty).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The cookie value. May be empty but never null.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The Expires attribute — the date/time at which the cookie expires.
        /// Null if the Expires attribute was not present.
        /// 
        /// Per RFC 6265 Section 5.1.1, the date format is typically
        /// "Thu, 01 Dec 1994 16:00:00 GMT" but implementations must be
        /// lenient with date parsing.
        /// </summary>
        public DateTime? Expires { get; internal set; }

        /// <summary>
        /// The Max-Age attribute — the number of seconds until the cookie expires.
        /// Null if the Max-Age attribute was not present.
        /// 
        /// A value of 0 or negative means the cookie should be deleted immediately.
        /// When both Expires and Max-Age are present, Max-Age takes precedence
        /// per RFC 6265 Section 5.3 step 3.
        /// </summary>
        public int? MaxAge { get; internal set; }

        /// <summary>
        /// The Domain attribute — the domain to which the cookie applies.
        /// Null if the Domain attribute was not present.
        /// 
        /// A leading dot is stripped per RFC 6265 Section 5.2.3.
        /// </summary>
        public string Domain { get; internal set; }

        /// <summary>
        /// The Path attribute — the URL path prefix for which the cookie is valid.
        /// Null if the Path attribute was not present (the browser infers the
        /// default path from the request URI).
        /// </summary>
        public string Path { get; internal set; }

        /// <summary>
        /// Whether the Secure attribute is present.
        /// When true, the cookie is only sent over HTTPS connections.
        /// </summary>
        public bool Secure { get; internal set; }

        /// <summary>
        /// Whether the HttpOnly attribute is present.
        /// When true, the cookie is inaccessible to client-side JavaScript
        /// (protects against XSS cookie theft).
        /// </summary>
        public bool HttpOnly { get; internal set; }

        /// <summary>
        /// The SameSite attribute value — controls cross-site request behavior.
        /// Per RFC 6265bis:
        ///   "Strict" — only sent in first-party context
        ///   "Lax" — sent with top-level navigations and GET requests
        ///   "None" — sent in all contexts (requires Secure)
        /// 
        /// Null if the SameSite attribute was not present.
        /// </summary>
        public string SameSite { get; internal set; }

        /// <summary>
        /// The raw Set-Cookie header value as received.
        /// Available for debugging or custom attribute parsing.
        /// </summary>
        public string RawValue { get; }

        /// <summary>
        /// Creates a new SetCookie with the given name and value.
        /// Attributes are set via internal setters during parsing.
        /// </summary>
        public SetCookie(string name, string value, string rawValue)
        {
            Name = name;
            Value = value;
            RawValue = rawValue;
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Name);
            sb.Append('=');
            sb.Append(Value);

            if (Domain != null)     sb.Append("; Domain=").Append(Domain);
            if (Path != null)       sb.Append("; Path=").Append(Path);
            if (Expires.HasValue)   sb.Append("; Expires=").Append(Expires.Value.ToString("R"));
            if (MaxAge.HasValue)    sb.Append("; Max-Age=").Append(MaxAge.Value);
            if (Secure)             sb.Append("; Secure");
            if (HttpOnly)           sb.Append("; HttpOnly");
            if (SameSite != null)   sb.Append("; SameSite=").Append(SameSite);

            return sb.ToString();
        }
    }
}
