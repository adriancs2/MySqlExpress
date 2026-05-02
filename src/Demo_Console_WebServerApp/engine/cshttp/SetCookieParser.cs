// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 6265 Section 4.1 (Set-Cookie), Section 5.1.1 (date parsing)
// Specification: RFC 6265bis (SameSite attribute)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CsHttp
{
    /// <summary>
    /// Parses Set-Cookie response headers into structured SetCookie objects.
    /// 
    /// RFC 6265 Section 4.1 defines the Set-Cookie header format:
    ///   Set-Cookie: name=value [; attribute]...
    /// 
    /// Attributes: Expires, Max-Age, Domain, Path, Secure, HttpOnly, SameSite.
    /// 
    /// Unlike the Cookie request header (simple name=value pairs), Set-Cookie
    /// is complex: it contains a single cookie with multiple attributes, and
    /// the Expires attribute requires lenient date parsing per Section 5.1.1.
    /// 
    /// A response may have multiple Set-Cookie headers — each sets one cookie.
    /// Per RFC 9110, Set-Cookie is an exception to the comma-joining rule:
    /// multiple Set-Cookie headers MUST NOT be combined into one field line.
    /// 
    /// Security:
    ///   - No regex — character-by-character attribute scanning
    ///   - Lenient date parsing per RFC 6265 Section 5.1.1
    /// </summary>
    public static class SetCookieParser
    {
        // Common date formats for the Expires attribute
        // RFC 6265 Section 5.1.1 requires lenient parsing
        private static readonly string[] DateFormats = new[]
        {
            "ddd, dd MMM yyyy HH:mm:ss 'GMT'",     // RFC 1123: Thu, 01 Dec 1994 16:00:00 GMT
            "ddd, dd-MMM-yyyy HH:mm:ss 'GMT'",     // RFC 1036 (Netscape): Thu, 01-Dec-1994 16:00:00 GMT
            "ddd, dd MMM yyyy HH:mm:ss",            // Without timezone
            "ddd, d MMM yyyy HH:mm:ss 'GMT'",      // Single-digit day
            "ddd, dd-MMM-yy HH:mm:ss 'GMT'",       // Two-digit year
            "dddd, dd-MMM-yy HH:mm:ss 'GMT'",      // Full weekday name
            "ddd MMM d HH:mm:ss yyyy",              // ANSI C: Thu Dec  1 16:00:00 1994
            "ddd, dd MMM yy HH:mm:ss 'GMT'",       // Two-digit year, no dash
        };

        /// <summary>
        /// Parses a single Set-Cookie header value into a SetCookie object.
        /// </summary>
        /// <param name="headerValue">
        /// The raw Set-Cookie header value (not including the "Set-Cookie: " prefix).
        /// Example: "sid=abc123; Path=/; HttpOnly; Secure; SameSite=Lax"
        /// </param>
        /// <returns>The parsed SetCookie, or null if the header is malformed
        /// (missing name=value pair).</returns>
        public static SetCookie Parse(string headerValue)
        {
            if (string.IsNullOrEmpty(headerValue))
                return null;

            // Split on ';' to get the name=value pair and attributes
            // The first segment is always the cookie name=value
            int firstSemi = headerValue.IndexOf(';');
            string nameValuePart = firstSemi >= 0
                ? headerValue.Substring(0, firstSemi)
                : headerValue;

            // Parse name=value
            int eqPos = nameValuePart.IndexOf('=');
            if (eqPos < 0)
                return null; // no '=' means malformed

            string name = nameValuePart.Substring(0, eqPos).Trim();
            string value = nameValuePart.Substring(eqPos + 1).Trim();

            if (name.Length == 0)
                return null; // empty cookie name

            // Remove surrounding quotes from value if present
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2);

            var cookie = new SetCookie(name, value, headerValue);

            // Parse attributes (everything after the first ';')
            if (firstSemi >= 0 && firstSemi + 1 < headerValue.Length)
            {
                ParseAttributes(headerValue, firstSemi + 1, cookie);
            }

            return cookie;
        }

        /// <summary>
        /// Parses all Set-Cookie headers from an HttpResponseMessage into
        /// a collection keyed by cookie name.
        /// </summary>
        /// <param name="headers">The response's header collection.</param>
        /// <returns>A dictionary of parsed SetCookie objects keyed by name.</returns>
        public static SetCookieCollection ParseAll(HttpHeaderCollection headers)
        {
            var collection = new SetCookieCollection();

            if (headers == null) return collection;

            // Set-Cookie headers must NOT be comma-joined (RFC 9110 exception).
            // GetValues() returns each Set-Cookie header value separately.
            string[] values = headers.GetValues("Set-Cookie");
            foreach (string val in values)
            {
                var cookie = Parse(val);
                if (cookie != null)
                    collection.Add(cookie);
            }

            return collection;
        }

        /// <summary>
        /// Parses the attributes portion of a Set-Cookie header.
        /// </summary>
        private static void ParseAttributes(string header, int start, SetCookie cookie)
        {
            int pos = start;
            while (pos < header.Length)
            {
                // Skip whitespace
                while (pos < header.Length && (header[pos] == ' ' || header[pos] == '\t'))
                    pos++;

                if (pos >= header.Length) break;

                // Find the end of this attribute (next ';' or end of string)
                int attrEnd = header.IndexOf(';', pos);
                if (attrEnd < 0) attrEnd = header.Length;

                string attribute = header.Substring(pos, attrEnd - pos).Trim();
                if (attribute.Length > 0)
                    ApplyAttribute(attribute, cookie);

                pos = attrEnd + 1;
            }
        }

        /// <summary>
        /// Applies a single attribute to a SetCookie object.
        /// Attribute may be "name=value" or just "name" (boolean flag).
        /// </summary>
        private static void ApplyAttribute(string attribute, SetCookie cookie)
        {
            int eqPos = attribute.IndexOf('=');
            string attrName;
            string attrValue;

            if (eqPos >= 0)
            {
                attrName = attribute.Substring(0, eqPos).Trim();
                attrValue = attribute.Substring(eqPos + 1).Trim();
            }
            else
            {
                attrName = attribute.Trim();
                attrValue = null;
            }

            // Case-insensitive attribute matching per RFC 6265 Section 5.2
            if (string.Equals(attrName, "Expires", StringComparison.OrdinalIgnoreCase))
            {
                if (attrValue != null)
                {
                    DateTime dt;
                    if (TryParseDate(attrValue, out dt))
                        cookie.Expires = dt;
                    // If parsing fails, the attribute is silently ignored per RFC 6265
                }
            }
            else if (string.Equals(attrName, "Max-Age", StringComparison.OrdinalIgnoreCase))
            {
                if (attrValue != null)
                {
                    int maxAge;
                    if (int.TryParse(attrValue, out maxAge))
                        cookie.MaxAge = maxAge;
                }
            }
            else if (string.Equals(attrName, "Domain", StringComparison.OrdinalIgnoreCase))
            {
                if (attrValue != null)
                {
                    // RFC 6265 Section 5.2.3: strip leading dot
                    string domain = attrValue;
                    if (domain.Length > 0 && domain[0] == '.')
                        domain = domain.Substring(1);
                    cookie.Domain = domain;
                }
            }
            else if (string.Equals(attrName, "Path", StringComparison.OrdinalIgnoreCase))
            {
                cookie.Path = attrValue;
            }
            else if (string.Equals(attrName, "Secure", StringComparison.OrdinalIgnoreCase))
            {
                cookie.Secure = true;
            }
            else if (string.Equals(attrName, "HttpOnly", StringComparison.OrdinalIgnoreCase))
            {
                cookie.HttpOnly = true;
            }
            else if (string.Equals(attrName, "SameSite", StringComparison.OrdinalIgnoreCase))
            {
                cookie.SameSite = attrValue;
            }
            // Unknown attributes are silently ignored per RFC 6265 Section 5.2
        }

        /// <summary>
        /// Attempts to parse a date string using the lenient formats
        /// required by RFC 6265 Section 5.1.1.
        /// </summary>
        private static bool TryParseDate(string dateStr, out DateTime result)
        {
            return DateTime.TryParseExact(
                dateStr,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out result);
        }
    }

    // ─── SetCookie Collection ────────────────────────────────────────

    /// <summary>
    /// A collection of parsed Set-Cookie headers, keyed by cookie name.
    /// If multiple Set-Cookie headers set the same cookie name, the last
    /// one wins (consistent with browser behavior).
    /// </summary>
    public sealed class SetCookieCollection : System.Collections.Generic.IEnumerable<SetCookie>
    {
        private readonly List<SetCookie> _ordered;
        private readonly Dictionary<string, int> _index;

        public SetCookieCollection()
        {
            _ordered = new List<SetCookie>();
            _index = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        /// <summary>Total number of Set-Cookie entries.</summary>
        public int Count => _ordered.Count;

        /// <summary>
        /// Adds a Set-Cookie entry. If a cookie with the same name already
        /// exists, it is replaced (last wins).
        /// </summary>
        internal void Add(SetCookie cookie)
        {
            if (_index.TryGetValue(cookie.Name, out int existing))
            {
                _ordered[existing] = cookie;
            }
            else
            {
                _index[cookie.Name] = _ordered.Count;
                _ordered.Add(cookie);
            }
        }

        /// <summary>
        /// Gets the Set-Cookie entry with the given cookie name.
        /// Returns null if not present.
        /// </summary>
        public SetCookie this[string name]
        {
            get
            {
                if (_index.TryGetValue(name, out int pos))
                    return _ordered[pos];
                return null;
            }
        }

        /// <summary>
        /// Gets a Set-Cookie entry by index (insertion order).
        /// </summary>
        public SetCookie this[int index] => _ordered[index];

        /// <summary>
        /// Returns true if a cookie with the given name exists.
        /// </summary>
        public bool Contains(string name) => _index.ContainsKey(name);

        /// <summary>
        /// Returns all cookie names in insertion order.
        /// </summary>
        public string[] AllKeys
        {
            get
            {
                var keys = new string[_ordered.Count];
                for (int i = 0; i < _ordered.Count; i++)
                    keys[i] = _ordered[i].Name;
                return keys;
            }
        }

        public IEnumerator<SetCookie> GetEnumerator() => _ordered.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
