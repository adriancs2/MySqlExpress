using System;
using System.Collections.Generic;
using System.Text;

namespace CsHttp
{
    /// <summary>
    /// HTTP/1.1 message parser implementing RFC 9112.
    /// 
    /// This is the core engine of cshttp. It transforms raw bytes into
    /// structured HttpRequestMessage or HttpResponseMessage objects.
    /// 
    /// Thread-safety: instances are stateless. The static methods are thread-safe.
    /// All state lives in the ParseContext during a single parse invocation.
    /// </summary>
    public static class HttpParser
    {
        // ASCII constants
        private const byte CR = 0x0D;
        private const byte LF = 0x0A;
        private const byte SP = 0x20;
        private const byte HTAB = 0x09;
        private const byte COLON = 0x3A; // ':'
        private const byte SEMICOLON = 0x3B; // ';'
        private const byte EQUALS = 0x3D; // '='
        private const byte QUESTION = 0x3F; // '?'
        private const byte SLASH = 0x2F; // '/'
        private const byte ASTERISK = 0x2A; // '*'
        private const byte DOT = 0x2E; // '.'
        private const byte VT = 0x0B;
        private const byte FF = 0x0C;

        // "HTTP" as bytes
        private static readonly byte[] HTTP_PREFIX = { 0x48, 0x54, 0x54, 0x50 }; // H T T P

        // ─── Internal parsing context ───────────────────────────────────

        private sealed class ParseContext
        {
            public byte[] Data;
            public int Pos;
            public int Length;
            public ParserOptions Options;
            public List<ParseWarning> Warnings;

            public ParseContext(byte[] data, int offset, int length, ParserOptions options)
            {
                Data = data;
                Pos = offset;
                Length = offset + length;
                Options = options ?? ParserOptions.Default;
                Warnings = new List<ParseWarning>();
            }

            public bool HasMore => Pos < Length;
            public int Remaining => Length - Pos;

            public byte Current => Data[Pos];

            public byte Peek(int ahead)
            {
                int idx = Pos + ahead;
                return idx < Length ? Data[idx] : (byte)0;
            }

            public void Warn(ParseWarningKind kind, string message)
            {
                Warnings.Add(new ParseWarning(kind, Pos, message));
            }

            public void Warn(ParseWarningKind kind, int position, string message)
            {
                Warnings.Add(new ParseWarning(kind, position, message));
            }
        }

        // ─── Public API ─────────────────────────────────────────────────

        /// <summary>
        /// Parses an HTTP/1.1 request message from raw bytes.
        /// </summary>
        /// <param name="data">The raw byte buffer containing the HTTP message.</param>
        /// <param name="options">Parser configuration. Null for defaults.</param>
        /// <returns>A ParseResult indicating success, failure, or incomplete.</returns>
        public static ParseResult ParseRequest(byte[] data, ParserOptions options = null)
        {
            return ParseRequest(data, 0, data?.Length ?? 0, options);
        }

        /// <summary>
        /// Parses an HTTP/1.1 request message from a region of a byte buffer.
        /// </summary>
        public static ParseResult ParseRequest(byte[] data, int offset, int length, ParserOptions options = null)
        {
            if (data == null || length == 0)
                return ParseResult.Failure(ParseErrorKind.EmptyInput, 0, "Input is null or empty.", null);

            var ctx = new ParseContext(data, offset, length, options);

            // Section 2.2: a server SHOULD ignore at least one empty line before request-line
            SkipLeadingEmptyLines(ctx);

            if (!ctx.HasMore)
                return ParseResult.Failure(ParseErrorKind.EmptyInput, ctx.Pos,
                    "Input contains only empty lines.", ctx.Warnings);

            // Phase 1: Parse request-line
            var request = new HttpRequestMessage();
            var err = ParseRequestLine(ctx, request);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            // Callback: OnStartLineParsed
            if (ctx.Options.OnStartLineParsed != null)
            {
                if (!ctx.Options.OnStartLineParsed(request.Method, request.RequestTarget, request.Version))
                    return ParseResult.Failure(ParseErrorKind.RejectedByCallback, ctx.Pos,
                        "Start-line rejected by callback.", ctx.Warnings);
            }

            // Section 2.2: reject/consume whitespace between start-line and first header
            err = HandleWhitespaceAfterStartLine(ctx);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            // Phase 2: Parse headers
            err = ParseHeaders(ctx, request.Headers);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            // Phase 3: Determine body framing (request rules)
            err = ResolveRequestBody(ctx, request);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            return ParseResult.SuccessRequest(request, ctx.Warnings, ctx.Pos);
        }

        /// <summary>
        /// Parses an HTTP/1.1 response message from raw bytes.
        /// </summary>
        /// <param name="data">The raw byte buffer containing the HTTP message.</param>
        /// <param name="requestMethod">The method of the request that elicited this response.
        /// Needed for body-length determination (Section 6.3 rules 1-2).
        /// Pass null if unknown.</param>
        /// <param name="options">Parser configuration. Null for defaults.</param>
        public static ParseResult ParseResponse(byte[] data, string requestMethod = null, ParserOptions options = null)
        {
            return ParseResponse(data, 0, data?.Length ?? 0, requestMethod, options);
        }

        /// <summary>
        /// Parses an HTTP/1.1 response message from a region of a byte buffer.
        /// </summary>
        public static ParseResult ParseResponse(byte[] data, int offset, int length,
            string requestMethod = null, ParserOptions options = null)
        {
            if (data == null || length == 0)
                return ParseResult.Failure(ParseErrorKind.EmptyInput, 0, "Input is null or empty.", null);

            var ctx = new ParseContext(data, offset, length, options);

            if (!ctx.HasMore)
                return ParseResult.Failure(ParseErrorKind.EmptyInput, ctx.Pos,
                    "Input is empty.", ctx.Warnings);

            // Phase 1: Parse status-line
            var response = new HttpResponseMessage();
            var err = ParseStatusLine(ctx, response);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            // Callback: OnStartLineParsed
            if (ctx.Options.OnStartLineParsed != null)
            {
                if (!ctx.Options.OnStartLineParsed(
                    response.StatusCode.ToString(), response.ReasonPhrase, response.Version))
                    return ParseResult.Failure(ParseErrorKind.RejectedByCallback, ctx.Pos,
                        "Status-line rejected by callback.", ctx.Warnings);
            }

            // Section 2.2: reject/consume whitespace between start-line and first header
            err = HandleWhitespaceAfterStartLine(ctx);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            // Phase 2: Parse headers
            err = ParseHeaders(ctx, response.Headers);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            // Phase 3: Determine body framing (response rules)
            err = ResolveResponseBody(ctx, response, requestMethod);
            if (err != null)
                return ParseResult.Failure(err, ctx.Warnings, ctx.Pos);

            return ParseResult.SuccessResponse(response, ctx.Warnings, ctx.Pos);
        }

        // ─── Phase 1: Start-line Parsing ────────────────────────────────

        /// <summary>
        /// Section 2.2: Skip leading empty lines before request-line.
        /// A server SHOULD ignore at least one empty line (CRLF) received 
        /// prior to the request-line.
        /// </summary>
        private static void SkipLeadingEmptyLines(ParseContext ctx)
        {
            bool skipped = false;
            while (ctx.HasMore)
            {
                if (ctx.Current == CR && ctx.Peek(1) == LF)
                {
                    ctx.Pos += 2;
                    skipped = true;
                }
                else if (ctx.Current == LF)
                {
                    ctx.Pos += 1;
                    skipped = true;
                }
                else
                {
                    break;
                }
            }
            if (skipped)
            {
                ctx.Warn(ParseWarningKind.LeadingEmptyLines, "Empty lines preceded the start-line.");
            }
        }

        /// <summary>
        /// Section 3: Parse request-line = method SP request-target SP HTTP-version CRLF
        /// </summary>
        private static ParseError ParseRequestLine(ParseContext ctx, HttpRequestMessage request)
        {
            int lineStart = ctx.Pos;

            // Find the end of the line
            int lineEnd = FindLineEnd(ctx, ctx.Options.MaxStartLineLength);
            if (lineEnd < 0)
            {
                if (ctx.Remaining > ctx.Options.MaxStartLineLength)
                    return new ParseError(ParseErrorKind.StartLineTooLong, lineStart,
                        "Request-line exceeds maximum length of " + ctx.Options.MaxStartLineLength + " bytes.");
                return new ParseError(ParseErrorKind.Incomplete, lineStart,
                    "Request-line is incomplete (no CRLF found).");
            }

            // Save raw start-line bytes
            int lineLength = lineEnd - lineStart;
            request.RawStartLine = new byte[lineLength];
            Buffer.BlockCopy(ctx.Data, lineStart, request.RawStartLine, 0, lineLength);

            // Extract the line content as ASCII (exclude the CRLF)
            // The actual line content is from lineStart to lineEnd (before line terminator)
            int contentEnd = lineEnd;

            // Split by SP into exactly 3 parts: method SP request-target SP HTTP-version
            // Section 3: recipients MAY parse on whitespace-delimited word boundaries
            int firstSP = FindByte(ctx.Data, lineStart, contentEnd, SP);
            if (firstSP < 0)
                return new ParseError(ParseErrorKind.InvalidStartLine, lineStart,
                    "Request-line has no SP separator.");

            int lastSP = FindLastByte(ctx.Data, firstSP + 1, contentEnd, SP);
            if (lastSP < 0 || lastSP == firstSP)
                return new ParseError(ParseErrorKind.InvalidStartLine, lineStart,
                    "Request-line does not have three SP-separated components.");

            // Extract components
            string method = Ascii(ctx.Data, lineStart, firstSP - lineStart);
            string target = Ascii(ctx.Data, firstSP + 1, lastSP - firstSP - 1);
            string version = Ascii(ctx.Data, lastSP + 1, contentEnd - lastSP - 1);

            // Validate method: must be a token (Section 3.1)
            if (method.Length == 0 || !IsToken(method))
                return new ParseError(ParseErrorKind.InvalidMethod, lineStart,
                    "Method is empty or contains invalid characters: '" + method + "'");

            // Validate request-target: must not be empty, must not contain whitespace (Section 3.2)
            if (target.Length == 0)
                return new ParseError(ParseErrorKind.InvalidRequestTarget, lineStart,
                    "Request-target is empty.");

            // Validate HTTP-version (Section 2.3)
            var versionErr = ValidateHttpVersion(version, lineStart);
            if (versionErr != null) return versionErr;

            request.Method = method;
            request.RequestTarget = target;
            request.Version = version;
            request.VersionMajor = version[5] - '0';
            request.VersionMinor = version[7] - '0';
            request.RequestTargetForm = DetermineRequestTargetForm(target);

            // Advance past the line terminator
            AdvancePastLineTerminator(ctx, lineEnd);

            return null;
        }

        /// <summary>
        /// Section 4: Parse status-line = HTTP-version SP status-code SP [reason-phrase] CRLF
        /// </summary>
        private static ParseError ParseStatusLine(ParseContext ctx, HttpResponseMessage response)
        {
            int lineStart = ctx.Pos;

            int lineEnd = FindLineEnd(ctx, ctx.Options.MaxStartLineLength);
            if (lineEnd < 0)
            {
                if (ctx.Remaining > ctx.Options.MaxStartLineLength)
                    return new ParseError(ParseErrorKind.StartLineTooLong, lineStart,
                        "Status-line exceeds maximum length of " + ctx.Options.MaxStartLineLength + " bytes.");
                return new ParseError(ParseErrorKind.Incomplete, lineStart,
                    "Status-line is incomplete (no CRLF found).");
            }

            int lineLength = lineEnd - lineStart;
            response.RawStartLine = new byte[lineLength];
            Buffer.BlockCopy(ctx.Data, lineStart, response.RawStartLine, 0, lineLength);

            int contentEnd = lineEnd;

            // HTTP-version SP status-code SP [reason-phrase]
            // Find first SP after HTTP-version
            int firstSP = FindByte(ctx.Data, lineStart, contentEnd, SP);
            if (firstSP < 0)
                return new ParseError(ParseErrorKind.InvalidStartLine, lineStart,
                    "Status-line has no SP separator.");

            string version = Ascii(ctx.Data, lineStart, firstSP - lineStart);
            var versionErr = ValidateHttpVersion(version, lineStart);
            if (versionErr != null) return versionErr;

            // Status code: exactly 3 digits starting after first SP
            int codeStart = firstSP + 1;
            if (codeStart + 3 > contentEnd)
                return new ParseError(ParseErrorKind.InvalidStatusCode, codeStart,
                    "Status-code is too short.");

            int statusCode = 0;
            for (int i = 0; i < 3; i++)
            {
                byte b = ctx.Data[codeStart + i];
                if (b < 0x30 || b > 0x39) // not DIGIT
                    return new ParseError(ParseErrorKind.InvalidStatusCode, codeStart + i,
                        "Status-code contains non-digit character.");
                statusCode = statusCode * 10 + (b - 0x30);
            }

            // After status-code: expect SP then optional reason-phrase, or end of line
            string reasonPhrase = "";
            int afterCode = codeStart + 3;
            if (afterCode < contentEnd)
            {
                // Section 4: server MUST send the SP even when reason-phrase is absent
                if (ctx.Data[afterCode] != SP)
                    return new ParseError(ParseErrorKind.InvalidStartLine, afterCode,
                        "Expected SP after status-code.");
                int reasonStart = afterCode + 1;
                if (reasonStart < contentEnd)
                    reasonPhrase = Ascii(ctx.Data, reasonStart, contentEnd - reasonStart);
            }
            else
            {
                // No SP after status code — tolerate in lenient mode
                if (ctx.Options.StrictMode)
                    return new ParseError(ParseErrorKind.InvalidStartLine, afterCode,
                        "Missing SP after status-code (strict mode).");
                ctx.Warn(ParseWarningKind.EmptyReasonPhrase, "Status-line has no SP after status-code.");
            }

            response.Version = version;
            response.VersionMajor = version[5] - '0';
            response.VersionMinor = version[7] - '0';
            response.StatusCode = statusCode;
            response.ReasonPhrase = reasonPhrase;

            AdvancePastLineTerminator(ctx, lineEnd);
            return null;
        }

        // ─── Phase 2: Header Parsing ────────────────────────────────────

        /// <summary>
        /// Section 2.2: Handle whitespace between start-line and first header.
        /// A sender MUST NOT send whitespace there.
        /// A recipient MUST either reject or consume whitespace-preceded lines.
        /// </summary>
        private static ParseError HandleWhitespaceAfterStartLine(ParseContext ctx)
        {
            if (!ctx.HasMore) return null;

            byte b = ctx.Current;
            bool hasWhitespace = (b == SP || b == HTAB);
            if (!hasWhitespace) return null;

            if (ctx.Options.StrictMode)
                return new ParseError(ParseErrorKind.WhitespaceAfterStartLine, ctx.Pos,
                    "Whitespace between start-line and first header field (rejected in strict mode).");

            // Lenient: consume each whitespace-preceded line until a proper header or empty line
            ctx.Warn(ParseWarningKind.WhitespaceAfterStartLineConsumed,
                "Whitespace-preceded lines after start-line were consumed and discarded.");

            while (ctx.HasMore)
            {
                b = ctx.Current;
                if (b != SP && b != HTAB)
                    break;

                // Skip this entire line
                int lineEnd = FindLineEnd(ctx, ctx.Options.MaxHeaderLineLength);
                if (lineEnd < 0)
                    return new ParseError(ParseErrorKind.Incomplete, ctx.Pos,
                        "Whitespace-preceded line is incomplete.");
                AdvancePastLineTerminator(ctx, lineEnd);
            }

            return null;
        }

        /// <summary>
        /// Section 5: Parse header field lines until the empty CRLF.
        /// field-line = field-name ":" OWS field-value OWS
        /// </summary>
        private static ParseError ParseHeaders(ParseContext ctx, HttpHeaderCollection headers)
        {
            int headerCount = 0;
            int totalHeaderBytes = 0;

            while (ctx.HasMore)
            {
                // Check for the empty line that terminates the header section
                if (ctx.Current == CR && ctx.Peek(1) == LF)
                {
                    ctx.Pos += 2;
                    break;
                }
                if (ctx.Current == LF)
                {
                    ctx.Warn(ParseWarningKind.LFWithoutCR, "Header section terminated by bare LF.");
                    ctx.Pos += 1;
                    break;
                }

                int lineStart = ctx.Pos;
                int lineEnd = FindLineEnd(ctx, ctx.Options.MaxHeaderLineLength);
                if (lineEnd < 0)
                {
                    if (ctx.Remaining > ctx.Options.MaxHeaderLineLength)
                        return new ParseError(ParseErrorKind.HeaderLineTooLong, lineStart,
                            "Header line exceeds maximum length of " + ctx.Options.MaxHeaderLineLength + " bytes.");
                    return new ParseError(ParseErrorKind.Incomplete, lineStart,
                        "Header line is incomplete (no line terminator found).");
                }

                int lineLen = lineEnd - lineStart;
                totalHeaderBytes += lineLen + 2; // +2 for CRLF

                // Check limits
                if (ctx.Options.MaxHeaderSectionSize > 0 && totalHeaderBytes > ctx.Options.MaxHeaderSectionSize)
                    return new ParseError(ParseErrorKind.HeaderSectionTooLarge, lineStart,
                        "Header section exceeds maximum size of " + ctx.Options.MaxHeaderSectionSize + " bytes.");

                // Check for obs-fold (Section 5.2): line starting with SP or HTAB
                // (after the first header line)
                // obs-fold means this line is a continuation of the previous field value
                if (headerCount > 0 && (ctx.Data[lineStart] == SP || ctx.Data[lineStart] == HTAB))
                {
                    if (ctx.Options.RejectObsoleteLineFolding)
                        return new ParseError(ParseErrorKind.ObsoleteLineFolding, lineStart,
                            "Obsolete line folding detected (rejected by configuration).");

                    // Replace obs-fold: append to previous header value with SP
                    ctx.Warn(ParseWarningKind.ObsLineFoldingReplaced,
                        "Obsolete line folding replaced with SP.");
                    string foldedValue = AsciiTrimOWS(ctx.Data, lineStart, lineLen);
                    var lastHeader = headers[headers.Count - 1];
                    lastHeader.Value = lastHeader.Value + " " + foldedValue;
                    AdvancePastLineTerminator(ctx, lineEnd);
                    continue;
                }

                // Parse: field-name ":" OWS field-value OWS
                int colonPos = FindByte(ctx.Data, lineStart, lineEnd, COLON);
                if (colonPos < 0)
                    return new ParseError(ParseErrorKind.InvalidHeaderLine, lineStart,
                        "Header line has no colon separator.");

                // Section 5.1: No whitespace allowed between field-name and colon
                int nameEnd = colonPos;
                if (nameEnd > lineStart && IsOWS(ctx.Data[nameEnd - 1]))
                    return new ParseError(ParseErrorKind.WhitespaceBeforeColon, lineStart,
                        "Whitespace between header field name and colon.");

                string fieldName = Ascii(ctx.Data, lineStart, nameEnd - lineStart);
                if (fieldName.Length == 0)
                    return new ParseError(ParseErrorKind.InvalidHeaderLine, lineStart,
                        "Header field name is empty.");

                // Section 2.2: Check for bare CR in the field name
                if (ContainsBareCR(ctx.Data, lineStart, nameEnd - lineStart))
                    return new ParseError(ParseErrorKind.BareCR, lineStart,
                        "Bare CR in header field name.");

                // Field value: everything after colon, with leading/trailing OWS trimmed
                int valueStart = colonPos + 1;
                string fieldValue = AsciiTrimOWS(ctx.Data, valueStart, lineEnd - valueStart);

                // Section 2.2: Check for bare CR in field value
                if (ContainsBareCR(ctx.Data, valueStart, lineEnd - valueStart))
                {
                    if (ctx.Options.StrictMode)
                        return new ParseError(ParseErrorKind.BareCR, valueStart,
                            "Bare CR in header field value.");
                    // Lenient: replace bare CR with SP
                    fieldValue = fieldValue.Replace('\r', ' ');
                    ctx.Warn(ParseWarningKind.BareCRReplaced, lineStart,
                        "Bare CR in header field value replaced with SP.");
                }

                // Save raw bytes
                byte[] rawBytes = new byte[lineLen];
                Buffer.BlockCopy(ctx.Data, lineStart, rawBytes, 0, lineLen);

                var header = new HttpHeader(fieldName, fieldValue, rawBytes);
                headers.Add(header);
                headerCount++;

                // Check header count limit
                if (ctx.Options.MaxHeaderCount > 0 && headerCount > ctx.Options.MaxHeaderCount)
                    return new ParseError(ParseErrorKind.TooManyHeaders, lineStart,
                        "Number of headers exceeds maximum of " + ctx.Options.MaxHeaderCount + ".");

                // Callback: OnHeaderParsed
                if (ctx.Options.OnHeaderParsed != null)
                {
                    if (!ctx.Options.OnHeaderParsed(fieldName, fieldValue, headerCount - 1))
                        return new ParseError(ParseErrorKind.RejectedByCallback, lineStart,
                            "Header rejected by callback: " + fieldName);
                }

                AdvancePastLineTerminator(ctx, lineEnd);
            }

            return null;
        }

        // ─── Phase 3: Body Framing ──────────────────────────────────────

        /// <summary>
        /// RFC 9112 Section 6.3 rules for request message body.
        /// Rules 7 and portions of 3-6 apply.
        /// </summary>
        private static ParseError ResolveRequestBody(ParseContext ctx, HttpRequestMessage request)
        {
            string te = request.Headers["Transfer-Encoding"];
            string cl = request.Headers["Content-Length"];

            // Rule 3: Both TE and CL present
            if (te != null && cl != null)
            {
                if (ctx.Options.RejectConflictingFraming)
                    return new ParseError(ParseErrorKind.ConflictingFraming, ctx.Pos,
                        "Request has both Transfer-Encoding and Content-Length (Section 6.3 rule 3).");

                // Lenient: TE overrides CL, remove CL
                request.Headers.Remove("Content-Length");
                cl = null;
            }

            // Rule 4: Transfer-Encoding present
            if (te != null)
            {
                // Check that chunked is the final encoding
                string finalCoding = GetFinalTransferCoding(te);
                if (!string.Equals(finalCoding, "chunked", StringComparison.OrdinalIgnoreCase))
                    return new ParseError(ParseErrorKind.InvalidTransferEncoding, ctx.Pos,
                        "Transfer-Encoding in request does not end with chunked (Section 6.3 rule 4).");

                request.BodyFraming = BodyFrameKind.Chunked;
                return ReadChunkedBody(ctx, request);
            }

            // Rule 6: Content-Length present
            if (cl != null)
            {
                long contentLength;
                var clErr = ParseContentLength(cl, ctx.Pos, ctx, out contentLength);
                if (clErr != null) return clErr;

                request.BodyFraming = BodyFrameKind.ContentLength;
                return ReadContentLengthBody(ctx, request, contentLength);
            }

            // Rule 7: No TE, no CL => zero body
            request.BodyFraming = BodyFrameKind.ZeroByAbsence;
            request.Body = null;
            request.RawBody = null;
            return null;
        }

        /// <summary>
        /// RFC 9112 Section 6.3 rules for response message body.
        /// All 8 rules apply.
        /// </summary>
        private static ParseError ResolveResponseBody(
            ParseContext ctx, HttpResponseMessage response, string requestMethod)
        {
            int sc = response.StatusCode;
            bool isHead = string.Equals(requestMethod, "HEAD", StringComparison.OrdinalIgnoreCase);

            // Rule 1: HEAD response, or 1xx, 204, 304 => no body
            if (isHead || (sc >= 100 && sc < 200) || sc == 204 || sc == 304)
            {
                response.BodyFraming = BodyFrameKind.NoBodyByStatus;
                response.Body = null;
                response.RawBody = null;
                return null;
            }

            // Rule 2: 2xx response to CONNECT => tunnel
            bool isConnect = string.Equals(requestMethod, "CONNECT", StringComparison.OrdinalIgnoreCase);
            if (isConnect && sc >= 200 && sc < 300)
            {
                response.BodyFraming = BodyFrameKind.Tunnel;
                response.Body = null;
                response.RawBody = null;
                return null;
            }

            string te = response.Headers["Transfer-Encoding"];
            string cl = response.Headers["Content-Length"];

            // Rule 3: Both TE and CL
            if (te != null && cl != null)
            {
                if (ctx.Options.RejectConflictingFraming)
                    return new ParseError(ParseErrorKind.ConflictingFraming, ctx.Pos,
                        "Response has both Transfer-Encoding and Content-Length (Section 6.3 rule 3).");

                response.Headers.Remove("Content-Length");
                cl = null;
            }

            // Rule 4: Transfer-Encoding present
            if (te != null)
            {
                string finalCoding = GetFinalTransferCoding(te);
                if (string.Equals(finalCoding, "chunked", StringComparison.OrdinalIgnoreCase))
                {
                    response.BodyFraming = BodyFrameKind.Chunked;
                    return ReadChunkedBody(ctx, response);
                }
                else
                {
                    // Chunked is not final => read until close
                    response.BodyFraming = BodyFrameKind.UntilClose;
                    return ReadUntilCloseBody(ctx, response);
                }
            }

            // Rule 5: Invalid Content-Length (handled within ParseContentLength)
            // Rule 6: Valid Content-Length
            if (cl != null)
            {
                long contentLength;
                var clErr = ParseContentLength(cl, ctx.Pos, ctx, out contentLength);
                if (clErr != null) return clErr;

                response.BodyFraming = BodyFrameKind.ContentLength;
                return ReadContentLengthBody(ctx, response, contentLength);
            }

            // Rule 8: No TE, no CL => read until close
            response.BodyFraming = BodyFrameKind.UntilClose;
            return ReadUntilCloseBody(ctx, response);
        }

        // ─── Body Reading ───────────────────────────────────────────────

        /// <summary>
        /// Read a body of exactly contentLength bytes.
        /// </summary>
        private static ParseError ReadContentLengthBody(ParseContext ctx, HttpMessage msg, long contentLength)
        {
            if (contentLength == 0)
            {
                msg.Body = null;
                msg.RawBody = null;
                return null;
            }

            // Check max body size
            if (ctx.Options.MaxBodySize >= 0 && contentLength > ctx.Options.MaxBodySize)
                return new ParseError(ParseErrorKind.BodyTooLarge, ctx.Pos,
                    "Content-Length " + contentLength + " exceeds maximum body size of " + ctx.Options.MaxBodySize + ".");

            if (ctx.Remaining < contentLength)
                return new ParseError(ParseErrorKind.Incomplete, ctx.Pos,
                    "Message body is incomplete. Expected " + contentLength + " bytes, have " + ctx.Remaining + ".");

            int len = (int)contentLength;
            byte[] body = new byte[len];
            Buffer.BlockCopy(ctx.Data, ctx.Pos, body, 0, len);

            // Progress callback
            if (ctx.Options.OnBodyProgress != null)
            {
                if (!ctx.Options.OnBodyProgress(len, len))
                    return new ParseError(ParseErrorKind.RejectedByCallback, ctx.Pos,
                        "Body reading rejected by callback.");
            }

            msg.Body = body;
            msg.RawBody = body; // Same reference — no transfer coding to decode
            ctx.Pos += len;
            return null;
        }

        /// <summary>
        /// Read and decode a chunked transfer-coded body (Section 7.1).
        /// </summary>
        private static ParseError ReadChunkedBody(ParseContext ctx, HttpMessage msg)
        {
            var content = new List<byte[]>();
            long totalLength = 0;
            int rawStart = ctx.Pos;

            while (true)
            {
                // Read chunk-size line: chunk-size [chunk-ext] CRLF
                int chunkLineEnd = FindLineEnd(ctx, -1);
                if (chunkLineEnd < 0)
                    return new ParseError(ParseErrorKind.Incomplete, ctx.Pos,
                        "Chunked body: chunk-size line is incomplete.");

                // Parse the hex chunk-size
                int hexStart = ctx.Pos;
                int hexEnd = hexStart;
                while (hexEnd < chunkLineEnd && IsHexDigit(ctx.Data[hexEnd]))
                    hexEnd++;

                if (hexEnd == hexStart)
                    return new ParseError(ParseErrorKind.InvalidChunkedEncoding, ctx.Pos,
                        "Chunked body: expected hex chunk-size.");

                long chunkSize = ParseHex(ctx.Data, hexStart, hexEnd - hexStart);
                if (chunkSize < 0)
                    return new ParseError(ParseErrorKind.ChunkSizeOverflow, hexStart,
                        "Chunked body: chunk-size overflows.");

                if (chunkSize > ctx.Options.MaxChunkSize)
                    return new ParseError(ParseErrorKind.ChunkTooLarge, hexStart,
                        "Chunk size " + chunkSize + " exceeds maximum of " + ctx.Options.MaxChunkSize + ".");

                // Chunk extensions (Section 7.1.1): skip, warn
                if (hexEnd < chunkLineEnd)
                {
                    // There might be chunk-ext or BWS before semicolon
                    // Per RFC: recipient MUST ignore unrecognized chunk extensions
                    ctx.Warn(ParseWarningKind.ChunkExtensionIgnored, hexEnd,
                        "Chunk extension ignored.");
                }

                // Callback: OnChunkHeader
                if (ctx.Options.OnChunkHeader != null)
                {
                    if (!ctx.Options.OnChunkHeader(chunkSize, totalLength))
                        return new ParseError(ParseErrorKind.RejectedByCallback, ctx.Pos,
                            "Chunk header rejected by callback.");
                }

                AdvancePastLineTerminator(ctx, chunkLineEnd);

                // Last chunk: size == 0
                if (chunkSize == 0)
                    break;

                // Check max body size
                totalLength += chunkSize;
                if (ctx.Options.MaxBodySize >= 0 && totalLength > ctx.Options.MaxBodySize)
                    return new ParseError(ParseErrorKind.BodyTooLarge, ctx.Pos,
                        "Chunked body exceeds maximum body size of " + ctx.Options.MaxBodySize + ".");

                // Read chunk-data
                int csz = (int)chunkSize;
                if (ctx.Remaining < csz + 2) // +2 for trailing CRLF
                    return new ParseError(ParseErrorKind.Incomplete, ctx.Pos,
                        "Chunked body: chunk-data is incomplete.");

                byte[] chunkData = new byte[csz];
                Buffer.BlockCopy(ctx.Data, ctx.Pos, chunkData, 0, csz);
                content.Add(chunkData);
                ctx.Pos += csz;

                // Expect CRLF after chunk-data
                if (!ExpectCRLF(ctx))
                    return new ParseError(ParseErrorKind.InvalidChunkedEncoding, ctx.Pos,
                        "Chunked body: expected CRLF after chunk-data.");

                // Progress callback
                if (ctx.Options.OnBodyProgress != null)
                {
                    if (!ctx.Options.OnBodyProgress(csz, totalLength))
                        return new ParseError(ParseErrorKind.RejectedByCallback, ctx.Pos,
                            "Chunk data reading rejected by callback.");
                }
            }

            // Read trailer section (Section 7.1.2)
            var trailerErr = ParseHeaders(ctx, msg.Trailers);
            if (trailerErr != null) return trailerErr;

            // Assemble decoded content
            int total = (int)totalLength;
            byte[] body = new byte[total];
            int offset = 0;
            foreach (var chunk in content)
            {
                Buffer.BlockCopy(chunk, 0, body, offset, chunk.Length);
                offset += chunk.Length;
            }

            // Raw body: the complete chunked stream as received
            int rawLen = ctx.Pos - rawStart;
            byte[] rawBody = new byte[rawLen];
            Buffer.BlockCopy(ctx.Data, rawStart, rawBody, 0, rawLen);

            msg.Body = body;
            msg.RawBody = rawBody;
            return null;
        }

        /// <summary>
        /// Read body until end of input (close-delimited, Section 6.3 rule 8).
        /// Used only for responses.
        /// </summary>
        private static ParseError ReadUntilCloseBody(ParseContext ctx, HttpMessage msg)
        {
            int remaining = ctx.Remaining;
            if (remaining == 0)
            {
                msg.Body = null;
                msg.RawBody = null;
                return null;
            }

            if (ctx.Options.MaxBodySize >= 0 && remaining > ctx.Options.MaxBodySize)
                return new ParseError(ParseErrorKind.BodyTooLarge, ctx.Pos,
                    "Close-delimited body exceeds maximum body size of " + ctx.Options.MaxBodySize + ".");

            byte[] body = new byte[remaining];
            Buffer.BlockCopy(ctx.Data, ctx.Pos, body, 0, remaining);

            if (ctx.Options.OnBodyProgress != null)
            {
                if (!ctx.Options.OnBodyProgress(remaining, remaining))
                    return new ParseError(ParseErrorKind.RejectedByCallback, ctx.Pos,
                        "Body reading rejected by callback.");
            }

            msg.Body = body;
            msg.RawBody = body;
            ctx.Pos += remaining;
            return null;
        }

        // ─── Utility: Line finding ──────────────────────────────────────

        /// <summary>
        /// Find the end of the current line (position of the last content byte before CRLF or LF).
        /// Returns the index of the byte AFTER the last content byte (i.e., where CR or LF is).
        /// Returns -1 if no line terminator is found within maxLen bytes.
        /// </summary>
        private static int FindLineEnd(ParseContext ctx, int maxLen)
        {
            int limit = maxLen > 0 ? Math.Min(ctx.Length, ctx.Pos + maxLen) : ctx.Length;
            for (int i = ctx.Pos; i < limit; i++)
            {
                byte b = ctx.Data[i];
                if (b == LF)
                {
                    // Check if preceded by CR
                    if (i > ctx.Pos && ctx.Data[i - 1] == CR)
                        return i - 1;

                    // Bare LF — tolerated per Section 2.2
                    ctx.Warn(ParseWarningKind.LFWithoutCR, i, "Bare LF as line terminator.");
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Advance ctx.Pos past the line terminator (CRLF or bare LF) at lineEnd.
        /// lineEnd points to the start of the terminator (CR of CRLF, or the bare LF).
        /// </summary>
        private static void AdvancePastLineTerminator(ParseContext ctx, int lineEnd)
        {
            if (lineEnd < ctx.Length && ctx.Data[lineEnd] == CR)
            {
                ctx.Pos = lineEnd + 2; // Skip CR LF
            }
            else
            {
                ctx.Pos = lineEnd + 1; // Skip bare LF
            }
        }

        private static bool ExpectCRLF(ParseContext ctx)
        {
            if (ctx.Remaining >= 2 && ctx.Data[ctx.Pos] == CR && ctx.Data[ctx.Pos + 1] == LF)
            {
                ctx.Pos += 2;
                return true;
            }
            if (ctx.Remaining >= 1 && ctx.Data[ctx.Pos] == LF)
            {
                ctx.Pos += 1;
                return true;
            }
            return false;
        }

        // ─── Utility: Byte searching ────────────────────────────────────

        private static int FindByte(byte[] data, int start, int end, byte value)
        {
            for (int i = start; i < end; i++)
                if (data[i] == value) return i;
            return -1;
        }

        private static int FindLastByte(byte[] data, int start, int end, byte value)
        {
            for (int i = end - 1; i >= start; i--)
                if (data[i] == value) return i;
            return -1;
        }

        // ─── Utility: ASCII conversion ──────────────────────────────────

        private static string Ascii(byte[] data, int offset, int length)
        {
            if (length <= 0) return "";
            return Encoding.ASCII.GetString(data, offset, length);
        }

        /// <summary>
        /// Extract ASCII string with leading/trailing OWS (SP, HTAB) trimmed.
        /// Per Section 5.1.
        /// </summary>
        private static string AsciiTrimOWS(byte[] data, int offset, int length)
        {
            int start = offset;
            int end = offset + length;

            while (start < end && IsOWS(data[start])) start++;
            while (end > start && IsOWS(data[end - 1])) end--;

            return Ascii(data, start, end - start);
        }

        // ─── Utility: Character classification (RFC 5234 / RFC 9110) ────

        private static bool IsOWS(byte b) => b == SP || b == HTAB;

        private static bool IsHexDigit(byte b) =>
            (b >= 0x30 && b <= 0x39) ||  // 0-9
            (b >= 0x41 && b <= 0x46) ||  // A-F
            (b >= 0x61 && b <= 0x66);    // a-f

        /// <summary>
        /// token = 1*tchar
        /// tchar = "!" / "#" / "$" / "%" / "&amp;" / "'" / "*" / "+" / "-" / "." /
        ///         "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA
        /// Per RFC 9110 Section 5.6.2.
        /// </summary>
        private static bool IsToken(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!IsTChar(c)) return false;
            }
            return s.Length > 0;
        }

        private static bool IsTChar(char c)
        {
            if (c >= 'A' && c <= 'Z') return true;
            if (c >= 'a' && c <= 'z') return true;
            if (c >= '0' && c <= '9') return true;
            switch (c)
            {
                case '!': case '#': case '$': case '%': case '&':
                case '\'': case '*': case '+': case '-': case '.':
                case '^': case '_': case '`': case '|': case '~':
                    return true;
                default:
                    return false;
            }
        }

        // ─── Utility: HTTP-version validation ───────────────────────────

        /// <summary>
        /// Section 2.3: HTTP-version = "HTTP" "/" DIGIT "." DIGIT
        /// Case-sensitive. Exactly 8 characters: HTTP/X.Y
        /// </summary>
        private static ParseError ValidateHttpVersion(string version, int pos)
        {
            if (version.Length != 8)
                return new ParseError(ParseErrorKind.InvalidVersion, pos,
                    "HTTP-version must be exactly 8 characters: '" + version + "'");
            if (version[0] != 'H' || version[1] != 'T' || version[2] != 'T' || version[3] != 'P')
                return new ParseError(ParseErrorKind.InvalidVersion, pos,
                    "HTTP-version must start with 'HTTP': '" + version + "'");
            if (version[4] != '/')
                return new ParseError(ParseErrorKind.InvalidVersion, pos,
                    "HTTP-version missing '/' separator: '" + version + "'");
            if (version[5] < '0' || version[5] > '9')
                return new ParseError(ParseErrorKind.InvalidVersion, pos,
                    "HTTP-version major is not a digit: '" + version + "'");
            if (version[6] != '.')
                return new ParseError(ParseErrorKind.InvalidVersion, pos,
                    "HTTP-version missing '.' separator: '" + version + "'");
            if (version[7] < '0' || version[7] > '9')
                return new ParseError(ParseErrorKind.InvalidVersion, pos,
                    "HTTP-version minor is not a digit: '" + version + "'");
            return null;
        }

        // ─── Utility: Request-target form detection ─────────────────────

        /// <summary>
        /// Section 3.2: Determine the form of the request-target.
        /// </summary>
        private static RequestTargetForm DetermineRequestTargetForm(string target)
        {
            if (target == "*")
                return RequestTargetForm.Asterisk;

            // absolute-form starts with a scheme (e.g., "http://")
            if (target.Length > 4 &&
                ((target[0] == 'h' || target[0] == 'H') &&
                 (target[1] == 't' || target[1] == 'T') &&
                 (target[2] == 't' || target[2] == 'T') &&
                 (target[3] == 'p' || target[3] == 'P')))
            {
                // Could be "http://" or "https://"
                if (target.Contains("://"))
                    return RequestTargetForm.Absolute;
            }

            // origin-form starts with "/"
            if (target.Length > 0 && target[0] == '/')
                return RequestTargetForm.Origin;

            // authority-form: host:port (used with CONNECT)
            // If it contains a colon but doesn't start with "/" and isn't absolute
            if (target.Contains(":"))
                return RequestTargetForm.Authority;

            // Default to origin form for anything else
            return RequestTargetForm.Origin;
        }

        // ─── Utility: Content-Length parsing ─────────────────────────────

        /// <summary>
        /// Parse and validate a Content-Length header value.
        /// Section 6.3 rule 5: handles comma-separated duplicate values.
        /// Outputs the validated numeric value via the out parameter.
        /// </summary>
        private static ParseError ParseContentLength(string value, int pos, ParseContext ctx, out long contentLength)
        {
            contentLength = 0;
            value = value.Trim();

            // Check for comma-separated list (duplicate CL values)
            if (value.Contains(","))
            {
                string[] parts = value.Split(',');
                string first = null;
                foreach (var part in parts)
                {
                    string trimmed = part.Trim();
                    if (!IsValidDecimal(trimmed))
                        return new ParseError(ParseErrorKind.InvalidContentLength, pos,
                            "Content-Length contains non-decimal value: '" + trimmed + "'");
                    if (first == null)
                        first = trimmed;
                    else if (first != trimmed)
                        return new ParseError(ParseErrorKind.InvalidContentLength, pos,
                            "Content-Length has conflicting values: '" + first + "' vs '" + trimmed + "'");
                }

                ctx.Warn(ParseWarningKind.DuplicateContentLengthCollapsed, pos,
                    "Duplicate Content-Length values were identical and collapsed.");

                // Use the validated first value (all values are identical)
                contentLength = long.Parse(first);
                return null;
            }

            if (!IsValidDecimal(value))
                return new ParseError(ParseErrorKind.InvalidContentLength, pos,
                    "Content-Length is not a valid decimal: '" + value + "'");

            contentLength = long.Parse(value);
            return null;
        }

        private static bool IsValidDecimal(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] < '0' || s[i] > '9') return false;
            }
            return true;
        }

        // ─── Utility: Transfer-Encoding parsing ─────────────────────────

        /// <summary>
        /// Extract the final transfer coding name from a Transfer-Encoding value.
        /// The value may be a comma-separated list like "gzip, chunked".
        /// </summary>
        private static string GetFinalTransferCoding(string te)
        {
            int lastComma = te.LastIndexOf(',');
            string last = lastComma >= 0 ? te.Substring(lastComma + 1) : te;
            return last.Trim();
        }

        // ─── Utility: Hex parsing ───────────────────────────────────────

        /// <summary>
        /// Parse a hex string from raw bytes. Returns -1 on overflow.
        /// </summary>
        private static long ParseHex(byte[] data, int offset, int length)
        {
            long result = 0;
            for (int i = 0; i < length; i++)
            {
                byte b = data[offset + i];
                int digit;
                if (b >= 0x30 && b <= 0x39)
                    digit = b - 0x30;
                else if (b >= 0x41 && b <= 0x46)
                    digit = b - 0x41 + 10;
                else if (b >= 0x61 && b <= 0x66)
                    digit = b - 0x61 + 10;
                else
                    return -1;

                // Overflow check
                if (result > (long.MaxValue >> 4))
                    return -1;

                result = (result << 4) | (long)digit;
            }
            return result;
        }

        // ─── Utility: Bare CR detection ─────────────────────────────────

        /// <summary>
        /// Section 2.2: Detect bare CR (CR not immediately followed by LF).
        /// </summary>
        private static bool ContainsBareCR(byte[] data, int offset, int length)
        {
            int end = offset + length;
            for (int i = offset; i < end; i++)
            {
                if (data[i] == CR)
                {
                    if (i + 1 >= end || data[i + 1] != LF)
                        return true;
                }
            }
            return false;
        }
    }
}
