// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 6265 Section 5.4 (Cookie header)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;

namespace CsHttp
{
    /// <summary>
    /// Parses the Cookie request header into name-value pairs.
    /// 
    /// RFC 6265 Section 5.4 defines the Cookie header format sent by user agents:
    ///   Cookie: name1=value1; name2=value2; name3=value3
    /// 
    /// Parsing rules:
    ///   - Pairs are separated by "; " (semicolon followed by space)
    ///   - Each pair is split on the first '=' into name and value
    ///   - Names and values are NOT percent-encoded in the Cookie header
    ///     (unlike query strings — cookies use their own encoding conventions)
    ///   - Empty names are rejected; empty values are permitted
    ///   - Duplicate cookie names are preserved (the browser may send
    ///     duplicates from different paths/domains)
    /// 
    /// Note: this parser handles the request-side Cookie header only.
    /// For response-side Set-Cookie parsing, see SetCookieParser.
    /// 
    /// Security:
    ///   - Enforces configurable limits on cookie count and total header size
    ///   - No regex — character-by-character scanning
    /// </summary>
    public static class CookieParser
    {
        /// <summary>
        /// Parses the value of a Cookie header into a key-value collection.
        /// </summary>
        /// <param name="cookieHeaderValue">
        /// The raw Cookie header value (not including the "Cookie: " prefix).
        /// Example: "sid=abc123; theme=dark; lang=en"
        /// </param>
        /// <param name="options">Content parser options. Null for defaults.</param>
        /// <returns>A ContentParseResult containing the parsed collection or an error.</returns>
        public static ContentParseResult Parse(string cookieHeaderValue, ContentParserOptions options = null)
        {
            var opts = options ?? ContentParserOptions.Default;

            if (cookieHeaderValue == null || cookieHeaderValue.Length == 0)
                return ContentParseResult.Ok(HttpContentCollection.Empty);

            // Total size limit
            if (cookieHeaderValue.Length > opts.MaxCookieHeaderSize)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.InputTooLarge, 0,
                    "Cookie header size " + cookieHeaderValue.Length +
                    " exceeds maximum of " + opts.MaxCookieHeaderSize + ".");
            }

            var collection = new HttpContentCollection();
            int cookieCount = 0;

            // Split on "; " (semicolon + space) per RFC 6265 Section 5.4
            // Also tolerate ";" without trailing space (common in the wild)
            int segStart = 0;
            for (int i = 0; i <= cookieHeaderValue.Length; i++)
            {
                bool atEnd = (i == cookieHeaderValue.Length);
                bool isSep = !atEnd && cookieHeaderValue[i] == ';';

                if (atEnd || isSep)
                {
                    // Trim leading whitespace from the segment
                    int trimmedStart = segStart;
                    while (trimmedStart < i && cookieHeaderValue[trimmedStart] == ' ')
                        trimmedStart++;

                    // Trim trailing whitespace
                    int trimmedEnd = i;
                    while (trimmedEnd > trimmedStart && cookieHeaderValue[trimmedEnd - 1] == ' ')
                        trimmedEnd--;

                    if (trimmedEnd > trimmedStart) // skip empty segments
                    {
                        cookieCount++;
                        if (cookieCount > opts.MaxCookieCount)
                        {
                            return ContentParseResult.Fail(
                                ContentParseErrorKind.TooManyParameters, segStart,
                                "Cookie count exceeds maximum of " + opts.MaxCookieCount + ".");
                        }

                        var err = ParseCookiePair(
                            cookieHeaderValue, trimmedStart, trimmedEnd,
                            collection, opts);
                        if (err != null)
                            return err;
                    }

                    segStart = i + 1;
                }
            }

            return ContentParseResult.Ok(collection);
        }

        /// <summary>
        /// Parses a single "name=value" cookie pair.
        /// </summary>
        private static ContentParseResult ParseCookiePair(
            string input, int start, int end,
            HttpContentCollection collection,
            ContentParserOptions opts)
        {
            // Find the first '='
            int eqPos = -1;
            for (int i = start; i < end; i++)
            {
                if (input[i] == '=')
                {
                    eqPos = i;
                    break;
                }
            }

            string name;
            string value;

            if (eqPos >= 0)
            {
                name = input.Substring(start, eqPos - start);
                value = input.Substring(eqPos + 1, end - eqPos - 1);
            }
            else
            {
                // No '=' — per RFC 6265, the entire string is the name
                // with an empty value. Some implementations treat this as
                // the value with an empty name. We follow RFC 6265.
                name = input.Substring(start, end - start);
                value = "";
            }

            // Trim whitespace from name and value
            name = name.Trim();
            value = value.Trim();

            // Skip entries with empty names
            if (name.Length == 0)
                return null; // silently skip, not an error

            // Remove surrounding double quotes from value if present
            // (RFC 6265 Section 4.1.1 permits quoted cookie values)
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                value = value.Substring(1, value.Length - 2);

            collection.Add(name, value, opts.DuplicatePolicy);
            return null; // success
        }
    }
}
