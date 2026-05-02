// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: WHATWG URL Standard (application/x-www-form-urlencoded parser)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Text;

namespace CsHttp
{
    /// <summary>
    /// Parses application/x-www-form-urlencoded request bodies into key-value collections.
    /// 
    /// This uses the same algorithm as QueryStringParser — the WHATWG URL Standard
    /// defines one parser for both query strings and form bodies. The only differences:
    ///   - Input source: Body bytes (not RequestTarget substring)
    ///   - Content-Type validation: expects "application/x-www-form-urlencoded"
    ///   - Size limits: separate from query string limits (typically larger)
    /// 
    /// Per the HTML specification, form submissions with method="POST" and
    /// enctype="application/x-www-form-urlencoded" encode the form data as
    /// the request body using the same key=value&amp;key=value format.
    /// 
    /// Security:
    ///   - Validates Content-Type before parsing
    ///   - Enforces configurable limits on body size, parameter count, value length
    ///   - Delegates percent-decoding to PercentDecoder
    ///   - No regex
    /// </summary>
    public static class FormParser
    {
        private const string ExpectedContentType = "application/x-www-form-urlencoded";

        /// <summary>
        /// Parses form-encoded body bytes into a key-value collection.
        /// </summary>
        /// <param name="body">The raw body bytes from HttpRequestMessage.Body.</param>
        /// <param name="contentType">
        /// The Content-Type header value. Must start with
        /// "application/x-www-form-urlencoded" (charset parameter is permitted).
        /// </param>
        /// <param name="options">Content parser options. Null for defaults.</param>
        /// <returns>A ContentParseResult containing the parsed collection or an error.</returns>
        public static ContentParseResult Parse(byte[] body, string contentType, ContentParserOptions options = null)
        {
            var opts = options ?? ContentParserOptions.Default;

            // Validate Content-Type
            if (!IsFormContentType(contentType))
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.ContentTypeMismatch, 0,
                    "Expected Content-Type '" + ExpectedContentType +
                    "' but received '" + (contentType ?? "(null)") + "'.");
            }

            // Null or empty body → empty collection
            if (body == null || body.Length == 0)
                return ContentParseResult.Ok(HttpContentCollection.Empty);

            // Body size limit
            if (body.Length > opts.MaxFormBodySize)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.InputTooLarge, 0,
                    "Form body size " + body.Length +
                    " exceeds maximum of " + opts.MaxFormBodySize + ".");
            }

            // Determine charset from Content-Type (default UTF-8)
            // e.g., "application/x-www-form-urlencoded; charset=utf-8"
            Encoding encoding = ExtractCharset(contentType);

            // Convert body bytes to string using the declared charset
            string formData;
            try
            {
                formData = encoding.GetString(body);
            }
            catch (Exception)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.DecodingFailed, 0,
                    "Failed to decode form body bytes using charset '" + encoding.WebName + "'.");
            }

            // Parse using the same algorithm as query strings,
            // but with form-specific limits
            return ParseFormString(formData, opts);
        }

        /// <summary>
        /// Parses a form-encoded string (already converted from body bytes).
        /// Same algorithm as QueryStringParser, with form-specific limits.
        /// </summary>
        private static ContentParseResult ParseFormString(string formData, ContentParserOptions opts)
        {
            var collection = new HttpContentCollection();
            int paramCount = 0;

            int segStart = 0;
            for (int i = 0; i <= formData.Length; i++)
            {
                bool atEnd = (i == formData.Length);
                bool isSep = !atEnd && formData[i] == '&';

                if (atEnd || isSep)
                {
                    if (i > segStart)
                    {
                        paramCount++;
                        if (paramCount > opts.MaxFormParameters)
                        {
                            return ContentParseResult.Fail(
                                ContentParseErrorKind.TooManyParameters, segStart,
                                "Form parameter count exceeds maximum of " +
                                opts.MaxFormParameters + ".");
                        }

                        var err = ParseSegment(formData, segStart, i, collection, opts);
                        if (err != null)
                            return err;
                    }
                    segStart = i + 1;
                }
            }

            return ContentParseResult.Ok(collection);
        }

        /// <summary>
        /// Parses a single "name=value" or "name" segment.
        /// Identical logic to QueryStringParser.ParseSegment.
        /// </summary>
        private static ContentParseResult ParseSegment(
            string input, int start, int end,
            HttpContentCollection collection,
            ContentParserOptions opts)
        {
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
                rawName = input.Substring(start, end - start);
                rawValue = "";
            }

            // Percent-decode with formMode=true
            var nameResult = PercentDecoder.Decode(rawName, formMode: true, options: opts);
            if (!nameResult.Success)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.DecodingFailed, start,
                    "Failed to decode form parameter name: " + nameResult.Error.Message);
            }

            var valueResult = PercentDecoder.Decode(rawValue, formMode: true, options: opts);
            if (!valueResult.Success)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.DecodingFailed, eqPos + 1,
                    "Failed to decode form parameter value: " + valueResult.Error.Message);
            }

            // Value length limit
            if (valueResult.Value != null && valueResult.Value.Length > opts.MaxFormParameterValueLength)
            {
                return ContentParseResult.Fail(
                    ContentParseErrorKind.ValueTooLarge, start,
                    "Form parameter value length " + valueResult.Value.Length +
                    " exceeds maximum of " + opts.MaxFormParameterValueLength +
                    " for key '" + (nameResult.Value ?? "") + "'.");
            }

            collection.Add(nameResult.Value ?? "", valueResult.Value ?? "", opts.DuplicatePolicy);
            return null;
        }

        // ─── Content-Type Helpers ────────────────────────────────────

        /// <summary>
        /// Checks whether the Content-Type header indicates form-encoded data.
        /// Matches "application/x-www-form-urlencoded" with optional parameters.
        /// Case-insensitive per RFC 9110.
        /// </summary>
        public static bool IsFormContentType(string contentType)
        {
            if (contentType == null) return false;

            // Trim and extract the media type (before any ';' parameters)
            string mediaType = contentType;
            int semiPos = contentType.IndexOf(';');
            if (semiPos >= 0)
                mediaType = contentType.Substring(0, semiPos);

            mediaType = mediaType.Trim();
            return string.Equals(mediaType, ExpectedContentType, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts the charset from a Content-Type header value.
        /// Returns UTF-8 if no charset is specified or if the charset is unknown.
        /// 
        /// Example: "application/x-www-form-urlencoded; charset=utf-8" → UTF-8
        /// </summary>
        private static Encoding ExtractCharset(string contentType)
        {
            if (contentType == null) return Encoding.UTF8;

            // Look for "charset=" parameter (case-insensitive)
            int idx = contentType.IndexOf("charset=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return Encoding.UTF8;

            int valueStart = idx + 8; // length of "charset="
            int valueEnd = contentType.IndexOf(';', valueStart);
            if (valueEnd < 0) valueEnd = contentType.Length;

            string charset = contentType.Substring(valueStart, valueEnd - valueStart).Trim();

            // Remove surrounding quotes if present
            if (charset.Length >= 2 && charset[0] == '"' && charset[charset.Length - 1] == '"')
                charset = charset.Substring(1, charset.Length - 2);

            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch (Exception)
            {
                // Unknown charset — fall back to UTF-8
                return Encoding.UTF8;
            }
        }
    }
}
