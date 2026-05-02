using System.Collections.Generic;

namespace CsHttp
{
    /// <summary>
    /// The result of parsing an HTTP/1.1 message from raw bytes.
    /// 
    /// A ParseResult always has a definitive state:
    /// - Success: a complete message was parsed. Request or Response is populated.
    /// - Failure: a fatal error was encountered. Error describes what went wrong.
    /// - Incomplete: the input ended before a complete message could be formed.
    ///              The consumer should supply more data and retry.
    /// 
    /// Warnings may be present regardless of success or failure.
    /// They record tolerated deviations from strict RFC compliance.
    /// </summary>
    public sealed class ParseResult
    {
        /// <summary>True if parsing completed successfully.</summary>
        public bool Success { get; }

        /// <summary>
        /// The error that caused parsing to fail. Null on success.
        /// </summary>
        public ParseError Error { get; }

        /// <summary>
        /// The parsed request message, if the input was a request and parsing succeeded.
        /// Null if the message was a response or if parsing failed.
        /// </summary>
        public HttpRequestMessage Request { get; }

        /// <summary>
        /// The parsed response message, if the input was a response and parsing succeeded.
        /// Null if the message was a request or if parsing failed.
        /// </summary>
        public HttpResponseMessage Response { get; }

        /// <summary>
        /// The parsed message regardless of type. Same object as Request or Response.
        /// Convenience accessor for code that processes both types generically.
        /// </summary>
        public HttpMessage Message => (HttpMessage)Request ?? Response;

        /// <summary>
        /// True if the message was a request (start-line was a request-line).
        /// </summary>
        public bool IsRequest => Request != null;

        /// <summary>
        /// True if the message was a response (start-line was a status-line).
        /// </summary>
        public bool IsResponse => Response != null;

        /// <summary>
        /// Non-fatal deviations tolerated during parsing.
        /// May contain entries even on success.
        /// </summary>
        public IReadOnlyList<ParseWarning> Warnings { get; }

        /// <summary>
        /// Total bytes consumed from the input.
        /// Valid on both success and failure.
        /// On success, this indicates where the next message (if any) begins.
        /// </summary>
        public int BytesConsumed { get; }

        private ParseResult(
            bool success,
            ParseError error,
            HttpRequestMessage request,
            HttpResponseMessage response,
            IReadOnlyList<ParseWarning> warnings,
            int bytesConsumed)
        {
            Success = success;
            Error = error;
            Request = request;
            Response = response;
            Warnings = warnings ?? new ParseWarning[0];
            BytesConsumed = bytesConsumed;
        }

        // --- Factory methods ---

        internal static ParseResult SuccessRequest(
            HttpRequestMessage request,
            List<ParseWarning> warnings,
            int bytesConsumed)
        {
            request.BytesConsumed = bytesConsumed;
            return new ParseResult(true, null, request, null, warnings, bytesConsumed);
        }

        internal static ParseResult SuccessResponse(
            HttpResponseMessage response,
            List<ParseWarning> warnings,
            int bytesConsumed)
        {
            response.BytesConsumed = bytesConsumed;
            return new ParseResult(true, null, null, response, warnings, bytesConsumed);
        }

        internal static ParseResult Failure(
            ParseError error,
            List<ParseWarning> warnings,
            int bytesConsumed)
        {
            return new ParseResult(false, error, null, null, warnings, bytesConsumed);
        }

        internal static ParseResult Failure(
            ParseErrorKind kind,
            int position,
            string message,
            List<ParseWarning> warnings)
        {
            return new ParseResult(
                false,
                new ParseError(kind, position, message),
                null, null, warnings, position);
        }
    }
}
