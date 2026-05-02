// cshttp - A C# Native HTTP/1.1 Request/Response Parser
// Specification: RFC 9112 (https://www.rfc-editor.org/rfc/rfc9112)
// License: Public Domain
// Target: C# 7.3 / .NET Framework 4.8

namespace CsHttp
{
    /// <summary>
    /// Categories of parse failure, mapped to RFC 9112 sections.
    /// </summary>
    public enum ParseErrorKind
    {
        /// <summary>No error.</summary>
        None = 0,

        // --- Start-line errors (Sections 2, 3, 4) ---

        /// <summary>The input is empty or contains no start-line.</summary>
        EmptyInput,

        /// <summary>The start-line does not match request-line or status-line grammar.</summary>
        InvalidStartLine,

        /// <summary>The method token is missing or contains invalid characters. (Section 3.1)</summary>
        InvalidMethod,

        /// <summary>The request-target is missing or contains invalid characters. (Section 3.2)</summary>
        InvalidRequestTarget,

        /// <summary>The HTTP-version field is missing or malformed. (Section 2.3)</summary>
        InvalidVersion,

        /// <summary>The status-code is not a 3-digit integer. (Section 4)</summary>
        InvalidStatusCode,

        // --- Header errors (Section 5) ---

        /// <summary>Whitespace found between field-name and colon. (Section 5.1)</summary>
        WhitespaceBeforeColon,

        /// <summary>A header field line is malformed.</summary>
        InvalidHeaderLine,

        /// <summary>Obsolete line folding detected and not permitted. (Section 5.2)</summary>
        ObsoleteLineFolding,

        /// <summary>Bare CR found in protocol element. (Section 2.2)</summary>
        BareCR,

        // --- Body framing errors (Section 6) ---

        /// <summary>Both Transfer-Encoding and Content-Length present. (Section 6.3 rule 3)</summary>
        ConflictingFraming,

        /// <summary>Content-Length value is not a valid decimal integer. (Section 6.2)</summary>
        InvalidContentLength,

        /// <summary>Transfer-Encoding not understood or chunked not final. (Section 6.3 rule 4)</summary>
        InvalidTransferEncoding,

        /// <summary>Chunked encoding is malformed. (Section 7.1)</summary>
        InvalidChunkedEncoding,

        /// <summary>Chunk size overflows representable integer range. (Section 7.1)</summary>
        ChunkSizeOverflow,

        // --- Limit exceeded errors (Kind 2 security) ---

        /// <summary>Request-line or status-line exceeds configured maximum length.</summary>
        StartLineTooLong,

        /// <summary>A header field line exceeds configured maximum length.</summary>
        HeaderLineTooLong,

        /// <summary>Total number of header fields exceeds configured maximum.</summary>
        TooManyHeaders,

        /// <summary>Total header section size exceeds configured maximum.</summary>
        HeaderSectionTooLarge,

        /// <summary>Message body exceeds configured maximum size.</summary>
        BodyTooLarge,

        /// <summary>A chunk size exceeds configured maximum.</summary>
        ChunkTooLarge,

        // --- Callback rejection ---

        /// <summary>A validation callback returned false, aborting the parse.</summary>
        RejectedByCallback,

        // --- Incomplete message (Section 8) ---

        /// <summary>The message is incomplete — data ended before parsing finished.</summary>
        Incomplete,

        /// <summary>Whitespace found between start-line and first header. (Section 2.2)</summary>
        WhitespaceAfterStartLine,
    }

    /// <summary>
    /// Describes a parse failure with location and detail.
    /// </summary>
    public sealed class ParseError
    {
        /// <summary>The category of error.</summary>
        public ParseErrorKind Kind { get; }

        /// <summary>Byte offset in the input where the error was detected.</summary>
        public int Position { get; }

        /// <summary>Human-readable description of the error.</summary>
        public string Message { get; }

        public ParseError(ParseErrorKind kind, int position, string message)
        {
            Kind = kind;
            Position = position;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("[{0}] at byte {1}: {2}", Kind, Position, Message);
        }
    }
}
