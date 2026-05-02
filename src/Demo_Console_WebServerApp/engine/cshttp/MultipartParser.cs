// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 7578 (multipart/form-data), RFC 2046 (MIME multipart)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

using System;
using System.Collections.Generic;
using System.Text;

namespace CsHttp
{
    /// <summary>
    /// Parses multipart/form-data request bodies into form fields and uploaded files.
    /// 
    /// This is the most complex content parser in cshttp. The multipart format
    /// uses boundary strings to delimit parts, where each part has its own
    /// headers and body:
    /// 
    ///   --boundary\r\n
    ///   Content-Disposition: form-data; name="field1"\r\n
    ///   \r\n
    ///   value1\r\n
    ///   --boundary\r\n
    ///   Content-Disposition: form-data; name="file1"; filename="photo.jpg"\r\n
    ///   Content-Type: image/jpeg\r\n
    ///   \r\n
    ///   [binary file bytes]\r\n
    ///   --boundary--\r\n
    /// 
    /// RFC 7578 defines the form-data specific rules.
    /// RFC 2046 Section 5.1 defines the underlying MIME multipart boundary syntax.
    /// 
    /// Security:
    ///   - Boundary extracted from Content-Type and validated
    ///   - Configurable limits on part count, part size, header size
    ///   - Only recognizes Content-Disposition and Content-Type in part headers
    ///   - Filenames reported raw — application must sanitize
    ///   - No regex — byte-level boundary scanning
    /// </summary>
    public static class MultipartParser
    {
        /// <summary>
        /// Parses a multipart/form-data body into form fields and uploaded files.
        /// </summary>
        /// <param name="body">The raw body bytes from HttpRequestMessage.Body.</param>
        /// <param name="contentType">
        /// The Content-Type header value. Must contain "multipart/form-data"
        /// and a "boundary" parameter.
        /// Example: "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW"
        /// </param>
        /// <param name="options">Content parser options. Null for defaults.</param>
        /// <returns>A MultipartParseResult containing form fields and files.</returns>
        public static MultipartParseResult Parse(byte[] body, string contentType, ContentParserOptions options = null)
        {
            var opts = options ?? ContentParserOptions.Default;

            // Extract boundary from Content-Type
            string boundary = ExtractBoundary(contentType);
            if (boundary == null)
            {
                return MultipartParseResult.Fail(
                    ContentParseErrorKind.InvalidBoundary, 0,
                    "Cannot extract boundary from Content-Type: '" + (contentType ?? "(null)") + "'.");
            }

            if (boundary.Length == 0)
            {
                return MultipartParseResult.Fail(
                    ContentParseErrorKind.InvalidBoundary, 0,
                    "Multipart boundary is empty.");
            }

            if (boundary.Length > opts.MaxMultipartBoundaryLength)
            {
                return MultipartParseResult.Fail(
                    ContentParseErrorKind.InvalidBoundary, 0,
                    "Multipart boundary length " + boundary.Length +
                    " exceeds maximum of " + opts.MaxMultipartBoundaryLength + ".");
            }

            if (body == null || body.Length == 0)
            {
                return MultipartParseResult.Ok(
                    HttpContentCollection.Empty,
                    HttpPostedFileCollection.Empty);
            }

            // Build the boundary markers as byte arrays
            // Part delimiter: CRLF "--" boundary
            // First delimiter may omit leading CRLF
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("--" + boundary);
            byte[] terminator = Encoding.ASCII.GetBytes("--" + boundary + "--");

            var formFields = new HttpContentCollection();
            var files = new HttpPostedFileCollection();
            int partCount = 0;

            // Find the first boundary
            int pos = FindBytes(body, 0, body.Length, boundaryBytes);
            if (pos < 0)
            {
                return MultipartParseResult.Fail(
                    ContentParseErrorKind.InvalidBoundary, 0,
                    "Initial boundary not found in body.");
            }

            // Skip past the first boundary line
            pos += boundaryBytes.Length;
            // Check for terminator (empty body with just boundary--)
            if (pos + 2 <= body.Length && body[pos] == (byte)'-' && body[pos + 1] == (byte)'-')
            {
                return MultipartParseResult.Ok(formFields, files);
            }
            // Skip CRLF after boundary
            pos = SkipCRLF(body, pos);

            // Parse each part
            while (pos < body.Length)
            {
                partCount++;
                if (partCount > opts.MaxMultipartParts)
                {
                    return MultipartParseResult.Fail(
                        ContentParseErrorKind.TooManyParts, pos,
                        "Multipart part count exceeds maximum of " + opts.MaxMultipartParts + ".");
                }

                // Find the next boundary (which marks the end of this part's body)
                // The boundary is preceded by CRLF: \r\n--boundary
                byte[] partDelimiter = Encoding.ASCII.GetBytes("\r\n--" + boundary);
                int nextBoundary = FindBytes(body, pos, body.Length, partDelimiter);

                if (nextBoundary < 0)
                {
                    // No closing boundary — try without leading CRLF as last resort
                    nextBoundary = FindBytes(body, pos, body.Length, boundaryBytes);
                    if (nextBoundary < 0)
                    {
                        return MultipartParseResult.Fail(
                            ContentParseErrorKind.MalformedPart, pos,
                            "Part " + partCount + ": no closing boundary found.");
                    }
                    // Boundary found without CRLF prefix — the part body ends here
                }

                // Parse part headers (Content-Disposition, Content-Type)
                int headerEnd = FindHeaderEnd(body, pos, nextBoundary);
                if (headerEnd < 0)
                {
                    return MultipartParseResult.Fail(
                        ContentParseErrorKind.MalformedPart, pos,
                        "Part " + partCount + ": headers are unterminated (no blank line found).");
                }

                // Check header size limit
                int headerSize = headerEnd - pos;
                if (headerSize > opts.MaxMultipartHeaderSize)
                {
                    return MultipartParseResult.Fail(
                        ContentParseErrorKind.MalformedPart, pos,
                        "Part " + partCount + ": headers exceed maximum size of " +
                        opts.MaxMultipartHeaderSize + ".");
                }

                // Parse the part headers
                string disposition = null;
                string partContentType = null;
                ParsePartHeaders(body, pos, headerEnd, out disposition, out partContentType);

                // Part body starts after the blank line (CRLF CRLF)
                int bodyStart = headerEnd + 4; // skip the \r\n\r\n
                int bodyEnd = nextBoundary;

                // The part body is between bodyStart and bodyEnd
                // (nextBoundary points to the \r\n before --boundary, so bodyEnd
                //  is the actual end of content)
                int partBodyLength = bodyEnd - bodyStart;

                // Part size limit
                if (partBodyLength > opts.MaxMultipartPartSize)
                {
                    return MultipartParseResult.Fail(
                        ContentParseErrorKind.PartTooLarge, bodyStart,
                        "Part " + partCount + ": body size " + partBodyLength +
                        " exceeds maximum of " + opts.MaxMultipartPartSize + ".");
                }

                // Extract part body bytes
                byte[] partBody = new byte[partBodyLength];
                if (partBodyLength > 0)
                    Buffer.BlockCopy(body, bodyStart, partBody, 0, partBodyLength);

                // Parse Content-Disposition to get name and filename
                string fieldName = null;
                string fileName = null;
                if (disposition != null)
                {
                    fieldName = ExtractDispositionParam(disposition, "name");
                    fileName = ExtractDispositionParam(disposition, "filename");
                }

                // Classify: file upload vs. form field
                if (fileName != null)
                {
                    // File upload
                    files.Add(new HttpPostedFile(
                        fieldName, fileName,
                        partContentType ?? "application/octet-stream",
                        partBody));
                }
                else
                {
                    // Regular form field — decode body as string
                    string fieldValue;
                    try
                    {
                        fieldValue = Encoding.UTF8.GetString(partBody);
                    }
                    catch (Exception)
                    {
                        fieldValue = Encoding.ASCII.GetString(partBody);
                    }

                    formFields.Add(fieldName ?? "", fieldValue);
                }

                // Move past the boundary
                int afterPart = nextBoundary + partDelimiter.Length;

                // Check for terminator (--boundary--)
                if (afterPart + 2 <= body.Length &&
                    body[afterPart] == (byte)'-' && body[afterPart + 1] == (byte)'-')
                {
                    break; // final boundary — done
                }

                // Skip CRLF after boundary
                pos = SkipCRLF(body, afterPart);
            }

            return MultipartParseResult.Ok(formFields, files);
        }

        // ─── Boundary Extraction ─────────────────────────────────────

        /// <summary>
        /// Extracts the boundary string from a Content-Type header value.
        /// 
        /// Example input: "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW"
        /// Returns: "----WebKitFormBoundary7MA4YWxkTrZu0gW"
        /// </summary>
        public static string ExtractBoundary(string contentType)
        {
            if (contentType == null) return null;

            // Find "boundary=" parameter (case-insensitive)
            int idx = contentType.IndexOf("boundary=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            int valueStart = idx + 9; // length of "boundary="

            // Check for quoted boundary
            if (valueStart < contentType.Length && contentType[valueStart] == '"')
            {
                int closeQuote = contentType.IndexOf('"', valueStart + 1);
                if (closeQuote < 0) return null; // unterminated quote
                return contentType.Substring(valueStart + 1, closeQuote - valueStart - 1);
            }

            // Unquoted: read until ';', space, or end
            int valueEnd = valueStart;
            while (valueEnd < contentType.Length)
            {
                char c = contentType[valueEnd];
                if (c == ';' || c == ' ' || c == '\t')
                    break;
                valueEnd++;
            }

            return contentType.Substring(valueStart, valueEnd - valueStart);
        }

        /// <summary>
        /// Checks whether the Content-Type indicates multipart/form-data.
        /// </summary>
        public static bool IsMultipartContentType(string contentType)
        {
            if (contentType == null) return false;

            string mediaType = contentType;
            int semiPos = contentType.IndexOf(';');
            if (semiPos >= 0)
                mediaType = contentType.Substring(0, semiPos);

            return string.Equals(mediaType.Trim(), "multipart/form-data",
                StringComparison.OrdinalIgnoreCase);
        }

        // ─── Part Header Parsing ─────────────────────────────────────

        /// <summary>
        /// Finds the end of part headers (the position of the \r\n\r\n sequence).
        /// Returns the position of the first \r of the blank line, or -1 if not found.
        /// </summary>
        private static int FindHeaderEnd(byte[] data, int start, int limit)
        {
            for (int i = start; i + 3 < limit; i++)
            {
                if (data[i] == 0x0D && data[i + 1] == 0x0A &&
                    data[i + 2] == 0x0D && data[i + 3] == 0x0A)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Parses part headers, extracting Content-Disposition and Content-Type.
        /// Only these two headers are recognized per RFC 7578.
        /// </summary>
        private static void ParsePartHeaders(
            byte[] data, int start, int end,
            out string disposition, out string contentType)
        {
            disposition = null;
            contentType = null;

            string headerBlock = Encoding.ASCII.GetString(data, start, end - start);
            string[] lines = headerBlock.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (line.Length == 0) continue;

                int colonPos = line.IndexOf(':');
                if (colonPos < 0) continue;

                string name = line.Substring(0, colonPos).Trim();
                string value = line.Substring(colonPos + 1).Trim();

                if (string.Equals(name, "Content-Disposition", StringComparison.OrdinalIgnoreCase))
                    disposition = value;
                else if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    contentType = value;
                // All other part headers are intentionally ignored (security)
            }
        }

        /// <summary>
        /// Extracts a parameter value from a Content-Disposition header.
        /// 
        /// Example: from 'form-data; name="field1"; filename="photo.jpg"'
        ///   ExtractDispositionParam(value, "name") → "field1"
        ///   ExtractDispositionParam(value, "filename") → "photo.jpg"
        /// </summary>
        private static string ExtractDispositionParam(string disposition, string paramName)
        {
            // Look for: paramName="value" or paramName=value
            string search = paramName + "=";
            int idx = -1;

            // Search for the parameter (case-insensitive parameter name)
            int searchFrom = 0;
            while (searchFrom < disposition.Length)
            {
                idx = disposition.IndexOf(search, searchFrom, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) return null;

                // Ensure it's preceded by ';' or whitespace (not part of another param name)
                if (idx == 0 || disposition[idx - 1] == ';' || disposition[idx - 1] == ' ' ||
                    disposition[idx - 1] == '\t')
                    break;

                searchFrom = idx + 1;
                idx = -1;
            }

            if (idx < 0) return null;

            int valueStart = idx + search.Length;
            if (valueStart >= disposition.Length) return "";

            // Quoted value
            if (disposition[valueStart] == '"')
            {
                int closeQuote = disposition.IndexOf('"', valueStart + 1);
                if (closeQuote < 0)
                    return disposition.Substring(valueStart + 1); // unterminated — take rest
                return disposition.Substring(valueStart + 1, closeQuote - valueStart - 1);
            }

            // Unquoted value: read until ';' or end
            int valueEnd = valueStart;
            while (valueEnd < disposition.Length && disposition[valueEnd] != ';' && disposition[valueEnd] != ' ')
                valueEnd++;

            return disposition.Substring(valueStart, valueEnd - valueStart);
        }

        // ─── Byte-Level Utilities ────────────────────────────────────

        /// <summary>
        /// Finds a byte sequence within a byte array.
        /// Returns the starting position, or -1 if not found.
        /// </summary>
        private static int FindBytes(byte[] data, int start, int end, byte[] pattern)
        {
            if (pattern.Length == 0) return start;
            int limit = end - pattern.Length;

            for (int i = start; i <= limit; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        /// <summary>
        /// Advances past a CRLF or bare LF at the given position.
        /// Returns the new position.
        /// </summary>
        private static int SkipCRLF(byte[] data, int pos)
        {
            if (pos < data.Length && data[pos] == 0x0D) pos++; // CR
            if (pos < data.Length && data[pos] == 0x0A) pos++; // LF
            return pos;
        }
    }

    // ─── Multipart Parse Result ──────────────────────────────────────

    /// <summary>
    /// The result of parsing a multipart/form-data body.
    /// Contains both form fields (text parameters) and uploaded files.
    /// </summary>
    public sealed class MultipartParseResult
    {
        /// <summary>True if parsing succeeded.</summary>
        public bool Success { get; }

        /// <summary>Form fields (text-only parts without a filename).</summary>
        public HttpContentCollection FormFields { get; }

        /// <summary>Uploaded files (parts with a filename).</summary>
        public HttpPostedFileCollection Files { get; }

        /// <summary>The error that caused failure. Null on success.</summary>
        public ContentParseError Error { get; }

        private MultipartParseResult(
            bool success,
            HttpContentCollection formFields,
            HttpPostedFileCollection files,
            ContentParseError error)
        {
            Success = success;
            FormFields = formFields;
            Files = files;
            Error = error;
        }

        internal static MultipartParseResult Ok(HttpContentCollection fields, HttpPostedFileCollection files)
        {
            return new MultipartParseResult(true, fields, files, null);
        }

        internal static MultipartParseResult Fail(ContentParseErrorKind kind, int position, string message)
        {
            return new MultipartParseResult(false, null, null,
                new ContentParseError(kind, position, message));
        }
    }
}
