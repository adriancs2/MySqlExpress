// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: WHATWG URL Standard (application/x-www-form-urlencoded parser)
// Specification: RFC 3986 Section 3.4 (query component)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System.Text;

namespace CsHttp
{
    /// <summary>
    /// Parses query strings into key-value collections.
    /// 
    /// The algorithm follows the WHATWG URL Standard's
    /// application/x-www-form-urlencoded parser:
    ///   1. Split on '&amp;' (or ';' — some servers accept both)
    ///   2. For each segment, split on the first '='
    ///   3. Percent-decode both name and value with formMode=true ('+' → space)
    /// 
    /// Input: the raw query string AFTER the '?' delimiter.
    /// For a RequestTarget of "/search?q=hello&amp;page=2", the input is "q=hello&amp;page=2".
    /// The '?' itself is NOT included — it is a delimiter, not part of the query.
    /// 
    /// Security:
    ///   - Enforces configurable limits on parameter count and value length
    ///   - Delegates percent-decoding to PercentDecoder (overlong UTF-8, null byte checks)
    ///   - No regex — character-by-character scanning
    /// </summary>
    public static class QueryStringParser
    {
        /// <summary>
        /// Parses a raw query string into a key-value collection.
        /// </summary>
        /// <param name="queryString">
        /// The raw query string without the leading '?'.
        /// Example: "q=hello%20world&amp;page=2"
        /// </param>
        /// <param name="options">Content parser options. Null for defaults.</param>
        /// <returns>A ContentParseResult containing the parsed collection or an error.</returns>
        public static ContentParseResult Parse(string queryString, ContentParserOptions options = null)
        {
            var opts = options ?? ContentParserOptions.Default;

            if (queryString == null || queryString.Length == 0)
                return ContentParseResult.Ok(HttpContentCollection.Empty);

            // Length limit
            if (queryString.Length > opts.MaxQueryStringLength)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.InputTooLarge, 0,
                    "Query string length " + queryString.Length +
                    " exceeds maximum of " + opts.MaxQueryStringLength + ".");
            }

            var collection = new HttpContentCollection();
            int paramCount = 0;

            // Split on '&' — walk character by character
            int segStart = 0;
            for (int i = 0; i <= queryString.Length; i++)
            {
                bool atEnd = (i == queryString.Length);
                bool isSep = !atEnd && queryString[i] == '&';

                if (atEnd || isSep)
                {
                    if (i > segStart) // skip empty segments (&&)
                    {
                        // Check parameter count limit
                        paramCount++;
                        if (paramCount > opts.MaxQueryParameters)
                        {
                            return ContentParseResult.Fail(
                                ContentParseErrorKind.TooManyParameters, segStart,
                                "Query string parameter count exceeds maximum of " +
                                opts.MaxQueryParameters + ".");
                        }

                        // Parse the segment: name=value or name (no value)
                        var err = ParseSegment(
                            queryString, segStart, i,
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
        /// Extracts the raw query string from a RequestTarget.
        /// Returns null if no query string is present.
        /// 
        /// For "/search?q=hello&amp;page=2", returns "q=hello&amp;page=2".
        /// For "/path", returns null.
        /// For "/path?", returns "" (empty query string, distinct from absent).
        /// </summary>
        public static string ExtractFromTarget(string requestTarget)
        {
            if (requestTarget == null) return null;

            int qpos = requestTarget.IndexOf('?');
            if (qpos < 0) return null;

            // Fragment delimiter '#' — if present, query ends there
            // (Fragments are not sent to the server per RFC 9112, but
            //  absolute-form targets may contain them per RFC 3986.)
            int fpos = requestTarget.IndexOf('#', qpos + 1);
            if (fpos >= 0)
                return requestTarget.Substring(qpos + 1, fpos - qpos - 1);

            return requestTarget.Substring(qpos + 1);
        }

        /// <summary>
        /// Extracts the path component from a RequestTarget.
        /// The path is everything before the '?' delimiter, percent-decoded.
        /// 
        /// For "/search?q=hello", returns "/search".
        /// For "/path%2Fto%2Fpage", returns "/path/to/page" (decoded).
        /// For "/path", returns "/path".
        /// For "*", returns "*".
        /// </summary>
        public static string ExtractPath(string requestTarget, ContentParserOptions options = null)
        {
            if (requestTarget == null) return null;

            // Find where the path ends (at '?' or end of string)
            int qpos = requestTarget.IndexOf('?');
            string rawPath = qpos >= 0
                ? requestTarget.Substring(0, qpos)
                : requestTarget;

            // Percent-decode the path (NOT formMode — '+' is literal in paths)
            return PercentDecoder.DecodeLenient(rawPath, formMode: false, options: options);
        }

        // ─── Private: Segment Parsing ────────────────────────────────

        /// <summary>
        /// Parses a single "name=value" or "name" segment from the query string.
        /// </summary>
        private static ContentParseResult ParseSegment(
            string input, int start, int end,
            HttpContentCollection collection,
            ContentParserOptions opts)
        {
            // Find the first '=' within this segment
            int eqPos = -1;
            for (int i = start; i < end; i++)
            {
                if (input[i] == '=')
                {
                    eqPos = i;
                    break;
                }
            }

            string rawName;
            string rawValue;

            if (eqPos >= 0)
            {
                rawName = input.Substring(start, eqPos - start);
                rawValue = input.Substring(eqPos + 1, end - eqPos - 1);
            }
            else
            {
                // No '=' — the entire segment is the name, value is empty string
                // Per WHATWG: "If there is no =, then name is segment and value is empty"
                rawName = input.Substring(start, end - start);
                rawValue = "";
            }

            // Percent-decode with formMode=true ('+' → space)
            var nameResult = PercentDecoder.Decode(rawName, formMode: true, options: opts);
            if (!nameResult.Success)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.DecodingFailed, start,
                    "Failed to decode query parameter name: " + nameResult.Error.Message);
            }

            var valueResult = PercentDecoder.Decode(rawValue, formMode: true, options: opts);
            if (!valueResult.Success)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.DecodingFailed, eqPos + 1,
                    "Failed to decode query parameter value: " + valueResult.Error.Message);
            }

            // Value length limit (on decoded value)
            if (valueResult.Value != null && valueResult.Value.Length > opts.MaxQueryParameterValueLength)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.ValueTooLarge, start,
                    "Query parameter value length " + valueResult.Value.Length +
                    " exceeds maximum of " + opts.MaxQueryParameterValueLength +
                    " for key '" + (nameResult.Value ?? "") + "'.");
            }

            collection.Add(nameResult.Value ?? "", valueResult.Value ?? "", opts.DuplicatePolicy);
            return null; // success — no error
        }
    }

    // ─── Content Parse Result ────────────────────────────────────────

    /// <summary>
    /// The result of parsing content (query string, form body, cookies)
    /// into a structured collection.
    /// </summary>
    public sealed class ContentParseResult
    {
        /// <summary>True if parsing succeeded.</summary>
        public bool Success { get; }

        /// <summary>The parsed key-value collection. Null on failure.</summary>
        public HttpContentCollection Collection { get; }

        /// <summary>The error that caused failure. Null on success.</summary>
        public ContentParseError Error { get; }

        private ContentParseResult(bool success, HttpContentCollection collection, ContentParseError error)
        {
            Success = success;
            Collection = collection;
            Error = error;
        }

        internal static ContentParseResult Ok(HttpContentCollection collection)
        {
            return new ContentParseResult(true, collection, null);
        }

        internal static ContentParseResult Fail(ContentParseErrorKind kind, int position, string message)
        {
            return new ContentParseResult(false, null, new ContentParseError(kind, position, message));
        }
    }

    /// <summary>
    /// Describes a content parsing failure.
    /// </summary>
    public sealed class ContentParseError
    {
        /// <summary>The category of error.</summary>
        public ContentParseErrorKind Kind { get; }

        /// <summary>Position in the input where the error was detected.</summary>
        public int Position { get; }

        /// <summary>Human-readable description.</summary>
        public string Message { get; }

        public ContentParseError(ContentParseErrorKind kind, int position, string message)
        {
            Kind = kind;
            Position = position;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("[{0}] at position {1}: {2}", Kind, Position, Message);
        }
    }

    /// <summary>
    /// Categories of content parsing failure.
    /// </summary>
    public enum ContentParseErrorKind
    {
        /// <summary>The input exceeds configured size limits.</summary>
        InputTooLarge,

        /// <summary>The number of parameters exceeds configured maximum.</summary>
        TooManyParameters,

        /// <summary>A parameter value exceeds configured maximum length.</summary>
        ValueTooLarge,

        /// <summary>Percent-decoding failed (invalid encoding, null byte, overlong UTF-8).</summary>
        DecodingFailed,

        /// <summary>The Content-Type is missing or does not match expected type.</summary>
        ContentTypeMismatch,

        /// <summary>The multipart boundary is missing, empty, or too long.</summary>
        InvalidBoundary,

        /// <summary>A multipart part is malformed (missing headers, unterminated).</summary>
        MalformedPart,

        /// <summary>The number of multipart parts exceeds configured maximum.</summary>
        TooManyParts,

        /// <summary>A multipart part body exceeds configured maximum size.</summary>
        PartTooLarge,
    }
}
